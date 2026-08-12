using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 한 판의 시뮬 상태 전체.
    /// Step() 이 유일한 시간 전진 수단이다.
    /// 이 클래스는 프레임을 모른다.
    /// </summary>
    public sealed class SimWorld : IDisposable
    {
        /// <summary>
        /// 고정 dt. 절대 변경 금지.
        /// 배속은 Step 호출 횟수로 낸다.
        /// </summary>
        public const float FixedDt = 1f / 60f;

        /// 솔버 시뮬 시간 상한 30초.
        public const int DefaultMaxSteps = 1800;

        readonly Scene _scene;
        readonly PhysicsScene2D _physics;
        readonly System.Random _rng;
        readonly List<IStepLogic> _logics;
        readonly List<Rigidbody2D> _bodies;
        readonly List<Collider2D> _hazards;

        bool _disposed;

        public LevelData Level { get; }
        public Judge Judge { get; }

        /// 표시 쪽이 구독한다. 시뮬레이션은 읽지 않는다.
        public SimEvents Events { get; }
        public Rigidbody2D Ball { get; }
        public int Seed { get; }

        /// 위험 바디 접촉 판정에 쓴다.
        public Collider2D BallCollider { get; }

        public int CurrentStep { get; private set; }

        /// <summary>
        /// 등록 순서 그대로다.
        /// 이 순서가 곧 해시 순서다.
        /// </summary>
        public IReadOnlyList<Rigidbody2D> Bodies => _bodies;

        /// <summary>
        /// 닿으면 실패하는 콜라이더.
        /// 장치가 도중에 뒤에 덧붙일 수 있다.
        /// 파괴되면 null 로 남으니 검사할 것.
        /// </summary>
        public IReadOnlyList<Collider2D> Hazards => _hazards;

        /// <summary>
        /// 장치의 데이터와 살아 있는 바디를 함께 준다.
        /// 바깥에서 장치를 볼 때는 Bodies 를 직접 뒤지지 말고
        /// 이것을 쓴다 — 바디 등록 순서를 아는 곳을
        /// 여기 하나로 모은다.
        /// </summary>
        /// <returns>
        /// body 는 바람처럼 바디가 없거나 이미 터졌으면 null.
        /// </returns>
        public (DeviceData data, Rigidbody2D body) GetDevice(int index)
        {
            var devices = Level.Devices;

            if (index < 0 || index >= devices.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(index), $"장치 {index} 는 없다. 레벨에 {devices.Count} 개뿐이다.");

            // 바디 순서는 공 하나 → 지형 → 장치다.
            int at = 1 + Level.Terrain.Count;

            // 바디를 만든 장치만 자리를 하나 쓴다.
            for (int i = 0; i < index; i++)
                if (DeviceFactory.MakesBody(devices[i].Type)) at++;

            bool hasBody = DeviceFactory.MakesBody(devices[index].Type);
            Rigidbody2D body = hasBody && at < _bodies.Count ? _bodies[at] : null;

            return (devices[index], body);
        }

        public bool IsTerminal => Judge.Cleared || Judge.Failed || Judge.Stalled;

        internal SimWorld(
            Scene scene,
            PhysicsScene2D physics,
            LevelData level,
            int seed,
            Rigidbody2D ball,
            Collider2D ballCollider,
            List<Rigidbody2D> bodies,
            List<Collider2D> hazards,
            List<IStepLogic> logics,
            Judge judge,
            SimEvents events)
        {
            _scene = scene;
            _physics = physics;
            _rng = new System.Random(seed);
            _bodies = bodies;
            _hazards = hazards;
            _logics = logics;

            Level = level;
            Seed = seed;
            Ball = ball;
            BallCollider = ballCollider;
            Judge = judge;

            // 판정보다 먼저 세운다. 0 스텝에 먹는 별도 알려야 한다.
            Events = events;

            // 첫 Step 이전의 초기 상태도 계측 대상이다.
            Judge.Initialize(this);
        }

        /// <summary>
        /// 한 스텝 전진.
        /// 순서는 박제된 계약이다:
        /// 로직 → 물리 → 판정 → 스텝 증가.
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

        /// <summary>대기 중인 장치가 있는가.</summary>
        public bool AnyPendingWork()
        {
            for (int i = 0; i < _logics.Count; i++)
                if (_logics[i].HasPendingWork) return true;
            return false;
        }

        /// <summary>정적 바디는 잠든 것으로 친다.</summary>
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
        /// 끝까지 돌린 뒤에 부르는 것을 전제한다.
        /// 도중에 부르면 진행 중이 Timeout 이 된다.
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
        /// 되돌리기·재시도는 되감기가 아니라
        /// 전파괴 → 재구축이다. 접촉·슬립 같은
        /// 엔진 내부 상태는 되돌릴 수 없다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_scene.IsValid()) return;

            // 언로드는 비동기라 프레임 끝에 일어난다.
            // 씬마다 물리가 격리되어 간섭은 없다.
            SceneManager.UnloadSceneAsync(_scene);
        }
    }
}
