using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 한 판의 시뮬 상태 전체. <see cref="Step"/> 이 **유일한 시간 전진 수단**이다.
    ///
    /// 이 클래스가 프로젝트에서 물리를 진행시키는 단 하나의 지점이며,
    /// 게임 플레이·솔버 탐색·리플레이 재생이 전부 여기를 통과한다.
    /// 다른 어디에서도 물리를 전진시키지 말 것 — 경로가 둘이 되는 순간
    /// "솔버 통과 = 유저도 클리어 가능"이 성립하지 않는다.
    ///
    /// 이 클래스는 프레임을 모른다. 초당 몇 번 <see cref="Step"/> 이 불리는지는
    /// 바깥(드라이버)의 사정이고, 결과에는 영향을 주지 않는다.
    /// </summary>
    public sealed class SimWorld : IDisposable
    {
        /// <summary>
        /// 고정 시간 간격. **절대 변경 금지.**
        /// 배속은 이 값을 키우는 것이 아니라 초당 <see cref="Step"/> 호출 횟수를 늘리는 것이다.
        /// dt 를 바꾸면 물리 결과가 바뀌고, 그 순간 솔버의 검증 결과가 전부 무효가 된다.
        /// </summary>
        public const float FixedDt = 1f / 60f;

        /// 솔버 시뮬 시간 상한 30초 (설계서 결정 8).
        public const int DefaultMaxSteps = 1800;

        readonly Scene _scene;
        readonly PhysicsScene2D _physics;
        readonly System.Random _rng;
        readonly List<IStepLogic> _logics;
        readonly List<Rigidbody2D> _bodies;

        bool _disposed;

        public LevelData Level { get; }
        public Judge Judge { get; }
        public Rigidbody2D Ball { get; }
        public int Seed { get; }

        public int CurrentStep { get; private set; }

        /// <summary>
        /// 월드의 모든 바디를 **등록 순서 그대로** 노출한다.
        /// 이 순서가 곧 <see cref="WorldHasher"/> 의 해시 순서이며,
        /// 순서가 흔들리면 같은 상태에서도 다른 해시가 나온다.
        /// </summary>
        public IReadOnlyList<Rigidbody2D> Bodies => _bodies;

        public bool IsTerminal => Judge.Cleared || Judge.Failed || Judge.Stalled;

        internal SimWorld(
            Scene scene,
            PhysicsScene2D physics,
            LevelData level,
            int seed,
            Rigidbody2D ball,
            List<Rigidbody2D> bodies,
            List<IStepLogic> logics,
            Judge judge)
        {
            _scene = scene;
            _physics = physics;
            _rng = new System.Random(seed);
            _bodies = bodies;
            _logics = logics;

            Level = level;
            Seed = seed;
            Ball = ball;
            Judge = judge;

            // 첫 Step 이전의 초기 상태도 계측 대상이다. 시작 지점이 이미 목표에 가까운
            // 레벨에서 MinGoalDist 가 실제보다 크게 잡히는 것을 막는다.
            Judge.Initialize(this);
        }

        /// <summary>
        /// 시간을 한 스텝 전진시킨다. **순서는 박제된 계약이다: 로직 → 물리 → 판정 → 스텝 증가.**
        ///
        /// 순서를 바꾸면 장치가 보는 세계가 한 스텝 어긋나고, 이미 저장된 리플레이가 전부 깨진다.
        /// </summary>
        public void Step()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SimWorld));

            for (int i = 0; i < _logics.Count; i++)
                _logics[i].Tick(CurrentStep, _rng);

            _physics.Simulate(FixedDt, Physics2D.AllLayers);

            Judge.Evaluate(CurrentStep, this);

            CurrentStep++;
        }

        /// <summary>대기 중인 장치가 하나라도 있는가. Stalled 판정 재료.</summary>
        public bool AnyPendingWork()
        {
            for (int i = 0; i < _logics.Count; i++)
                if (_logics[i].HasPendingWork) return true;
            return false;
        }

        /// <summary>모든 바디가 잠들었는가. Stalled 판정 재료. (정적 바디는 항상 잠든 것으로 취급된다)</summary>
        public bool AllBodiesSleeping()
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;
                if (body.bodyType == RigidbodyType2D.Static) continue;
                if (!body.IsSleeping()) return false;
            }
            return true;
        }

        /// <summary>
        /// 현재까지의 판정을 <see cref="SimResult"/> 로 확정한다.
        ///
        /// **시뮬을 끝까지 돌린 뒤에 부르는 것을 전제한다.** 아무 판정도 나지 않은 상태는
        /// <see cref="SimOutcome.Timeout"/> 으로 접히므로, 시뮬 도중에 부르면 "아직 진행 중"이
        /// "상한에 걸렸다"로 보고된다. 도중 상태가 필요하면 <see cref="Judge"/> 를 직접 읽을 것.
        /// </summary>
        public SimResult ToResult(float inkUsed)
        {
            SimOutcome outcome;
            if (Judge.Cleared) outcome = SimOutcome.Clear;
            else if (Judge.Failed) outcome = SimOutcome.Fail;
            else if (Judge.Stalled) outcome = SimOutcome.Stalled;
            else outcome = SimOutcome.Timeout;

            return new SimResult(outcome, CurrentStep, Judge.Stars, inkUsed, Judge.MinGoalDist);
        }

        /// <summary>
        /// 월드를 통째로 파괴한다.
        ///
        /// 되돌리기·초기화·재시도는 상태 되감기가 아니라 **전파괴 → 재구축**이다.
        /// 되감기는 접촉 상태·슬립 상태 같은 물리 엔진 내부 상태까지 되돌릴 수 없어
        /// 재현이 깨진다. 재구축 비용은 시뮬 비용에 비해 무시할 수 있다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_scene.IsValid()) return;

            // 에디터 모드 분기는 두지 않는다. 월드 생성에 쓰는 CreateScene 이 런타임 전용이라
            // 플레이 모드 밖에서는 애초에 월드가 만들어지지 않고, 따라서 여기도 도달하지 않는다.
            // 해제는 비동기라 실제 언로드는 프레임 끝에 일어난다 — 한 프레임 안에서 월드를
            // 여러 번 만들고 버리면 그 씬들이 잠시 함께 떠 있지만, 씬마다 물리가 격리되어 있어
            // 서로 간섭하지 않는다.
            SceneManager.UnloadSceneAsync(_scene);
        }
    }
}
