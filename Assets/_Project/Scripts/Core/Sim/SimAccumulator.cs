namespace PPS.Core
{
    /// <summary>
    /// 실시간 경과 시간을 고정 스텝으로 환산하는 누적기. **순수 C# 이며 프레임을 모른다.**
    ///
    /// 경과 시간을 인자로 받는 이유는 테스트 때문이다. 이 클래스가 직접 엔진에서
    /// 경과 시간을 읽으면 30/60/120fps 를 재현하는 테스트를 쓸 수 없고,
    /// 프레임 독립성은 "그럴 것이다"라는 믿음으로만 남는다.
    ///
    /// 배속과 일시정지가 여기서 공짜로 나온다 — dt 를 건드리지 않고
    /// 초당 <see cref="SimWorld.Step"/> 호출 횟수만 바꾸기 때문이다.
    /// </summary>
    public sealed class SimAccumulator
    {
        /// <summary>
        /// 한 프레임에 허용하는 최대 스텝 수. 죽음의 나선(느려짐 → 더 많은 스텝 → 더 느려짐) 방지.
        ///
        /// 이 상한에 걸려도 시뮬 결과는 달라지지 않는다. 스텝의 내용과 순서는 그대로이고
        /// "체감 속도가 느려질" 뿐이다. 프레임 독립성은 유지된다.
        /// </summary>
        public const int MaxStepsPerFrame = 8;

        float _accumulator;

        /// 배속. 1 = 실시간.
        public float SpeedMultiplier = 1f;

        /// 일시정지. 관찰 전용이며 그리기는 잠긴 상태다 (설계서 결정 1).
        public bool Paused;

        /// <summary>
        /// 경과 시간을 받아 월드를 전진시킨다.
        /// </summary>
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

        /// 월드를 새로 구축할 때 함께 초기화한다.
        public void Reset()
        {
            _accumulator = 0f;
        }
    }
}
