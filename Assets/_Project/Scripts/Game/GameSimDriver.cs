using System;
using PPS.Core;

namespace PPS.Game
{
    /// <summary>
    /// 게임 플레이용 얇은 드라이버.
    /// 경과 시간을 누적기에 넘기는 일만 한다.
    /// 여기 계산이 늘면 솔버와 경로가 갈라진다.
    /// </summary>
    public sealed class GameSimDriver : IDisposable
    {
        readonly SimAccumulator _accumulator = new SimAccumulator();

        SimWorld _world;

        public SimWorld World => _world;

        public bool HasWorld => _world != null;

        /// 편집 중(월드 없음) ↔ 시뮬 중(월드 있음).
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

        /// <summary>
        /// 시작 버튼을 눌렀을 때 월드가 생긴다.
        /// 시드는 받지 않는다 — 게임이 다른 시드로
        /// 돌리면 솔버가 푼 판과 다른 판이 된다.
        /// </summary>
        public void StartSimulation(StageData stage, Solution solution)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));

            Stop();
            _accumulator.Reset();
            _accumulator.Paused = false;
            _world = WorldBuilder.Build(stage, solution);
        }

        /// <summary>재시도는 되감기가 아니라 전파괴다.</summary>
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

        /// <summary>
        /// 소유자가 프레임마다 부른다. 스스로 Time 을 읽지
        /// 않아 뷰가 그리기 전에 스텝을 돌릴지 뒤에 돌릴지를
        /// 부르는 쪽이 정한다 — 실행 순서가 컴포넌트 정렬에
        /// 좌우되지 않는다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _accumulator.Advance(_world, deltaTime);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
