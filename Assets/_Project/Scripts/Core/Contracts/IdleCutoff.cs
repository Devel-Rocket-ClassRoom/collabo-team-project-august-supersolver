using System;

namespace PPS.Core
{
    /// <summary>
    /// 공이 제자리를 맴돌면 시뮬을 그만 본다.
    /// 물리 슬립은 속도가 거의 0 이어야 걸려서,
    /// 기어가는 공은 영영 안 잠기고 상한까지 돈다.
    /// </summary>
    public readonly struct IdleCutoff
    {
        /// 이만큼 벗어나면 움직인 것으로 친다.
        public readonly float Radius;

        /// 반경 안에 이만큼 머물면 끝으로 본다.
        public readonly int Steps;

        public IdleCutoff(float radius, int steps)
        {
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (steps <= 0)
                throw new ArgumentOutOfRangeException(nameof(steps));

            Radius = radius;
            Steps = steps;
        }
    }
}
