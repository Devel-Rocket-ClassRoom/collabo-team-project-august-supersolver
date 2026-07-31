using PPS.Core;
using UnityEngine;

namespace PPS.Game
{
    /// <summary>
    /// 게임 플레이용 얇은 드라이버. 하는 일은 하나뿐 —
    /// **엔진의 경과 시간을 <see cref="SimAccumulator"/> 에 넘겨주는 것.**
    ///
    /// 판정도 물리도 여기에 없다. 이 클래스가 하는 계산이 늘어나는 순간
    /// 게임과 솔버의 코드 경로가 갈라지고 검증의 정당성이 사라진다.
    ///
    /// FixedUpdate 를 쓰지 않는 이유: 호출 시점과 횟수를 Unity 가 관리해 통제할 수 없다.
    /// 수동 누적기는 배속·일시정지·솔버 재사용이 전부 같은 코드에서 나온다.
    /// </summary>
    public sealed class GameSimDriver : MonoBehaviour
    {
        readonly SimAccumulator _accumulator = new SimAccumulator();

        SimWorld _world;

        public SimWorld World => _world;

        public bool HasWorld => _world != null;

        /// <summary>편집 중(월드 없음) ↔ 시뮬 중(월드 있음) 2상태 중 어디인가.</summary>
        public bool IsSimulating => _world != null && !_world.IsTerminal;

        public bool Paused
        {
            get => _accumulator.Paused;
            set => _accumulator.Paused = value;
        }

        public float SpeedMultiplier
        {
            get => _accumulator.SpeedMultiplier;
            set => _accumulator.SpeedMultiplier = value;
        }

        /// <summary>그리기를 마치고 시작 버튼을 눌렀을 때. 이 시점에 비로소 월드가 생긴다.</summary>
        public void StartSimulation(LevelData level, Solution solution, int seed)
        {
            Stop();
            _accumulator.Reset();
            _accumulator.Paused = false;
            _world = WorldBuilder.Build(level, solution, seed);
        }

        /// <summary>초기화·재시도. 상태를 되감는 것이 아니라 월드를 통째로 버린다.</summary>
        public void Stop()
        {
            if (_world == null) return;
            _world.Dispose();
            _world = null;
        }

        public SimResult Result(float inkUsed)
            => _world == null
                ? new SimResult(SimOutcome.Timeout, 0, 0, inkUsed, float.PositiveInfinity)
                : _world.ToResult(inkUsed);

        void Update()
        {
            _accumulator.Advance(_world, Time.deltaTime);
        }

        void OnDestroy()
        {
            Stop();
        }
    }
}
