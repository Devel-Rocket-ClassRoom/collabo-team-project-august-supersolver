namespace PPS.Core
{
    /// <summary>
    /// 경과 시간을 고정 스텝으로 환산한다.
    /// 시간을 인자로 받는 이유는 테스트다.
    /// 배속·일시정지가 여기서 나온다.
    /// </summary>
    public sealed class SimAccumulator
    {
        /// <summary>
        /// 한 프레임 최대 스텝. 죽음의 나선 방지.
        /// 걸려도 결과는 안 바뀌고 느려질 뿐이다.
        /// </summary>
        public const int MaxStepsPerFrame = 8;

        float _accumulator;

        /// 배속. 1 = 실시간.
        public float SpeedMultiplier = 1f;

        /// 일시정지. 관찰 전용.
        public bool Paused;

        /// <returns>이번 호출에서 진행한 스텝 수.</returns>
        public int Advance(SimWorld world, float elapsedSeconds)
        {
            if (world == null || Paused || world.IsTerminal) return 0;

            _accumulator += elapsedSeconds * SpeedMultiplier;

            int stepped = 0;
            while (_accumulator >= SimWorld.FixedDt && stepped < MaxStepsPerFrame)
            {
                world.Step();
                _accumulator -= SimWorld.FixedDt;
                stepped++;

                if (world.IsTerminal) break;
            }

            return stepped;
        }

        /// 월드를 새로 구축할 때 함께 부른다.
        public void Reset()
        {
            _accumulator = 0f;
        }
    }
}
