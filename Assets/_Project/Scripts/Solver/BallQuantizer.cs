using System;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공의 위상 상태를 셀 하나로 접는다.
    /// 폭이 곧 "이 정도 차이는 같은 상황" 선언이라
    /// 인스턴스 하나가 맵 하나의 격자를 정한다.
    /// 위치와 속도는 단위가 달라 폭을 따로 잡는다.
    /// </summary>
    public sealed class BallQuantizer
    {
        /// 격자선에 붙일 폭. 격자 단위라 폭에 안 딸린다.
        /// 부동소수 오차(~1e-7)보다 훨씬 크고
        /// 물리적으로 유의미한 차이보다는 훨씬 작다.
        const float SnapEpsilon = 0.0001f;

        /// 폭은 실측 후 결정한다. 히트율과 정확도가
        /// 맞바뀌는 값이라 근거 없이 못 정한다.
        /// 지금은 기본값 없이 호출자가 전부 넣는다.
        public readonly float PositionStep;
        public readonly float VelocityStep;

        public BallQuantizer(float positionStep, float velocityStep)
        {
            if (positionStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(positionStep));
            if (velocityStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(velocityStep));

            PositionStep = positionStep;
            VelocityStep = velocityStep;
        }

        public BallCell Quantize(Vector2 position, Vector2 velocity) =>
            new BallCell(
                Index(position.x, PositionStep),
                Index(position.y, PositionStep),
                Index(velocity.x, VelocityStep),
                Index(velocity.y, VelocityStep));

        /// <summary>
        /// 격자선 바로 옆의 값은 선 위로 끌어다 놓고 자른다.
        /// 안 그러면 선에 걸친 값이 오차 한 번에
        /// 옆 셀로 넘어가 같은 상태가 두 키로 갈린다.
        /// </summary>
        static int Index(float value, float step)
        {
            float q = value / step;
            float line = Mathf.Round(q);
            if (Mathf.Abs(q - line) <= SnapEpsilon)
                q = line;

            return Mathf.FloorToInt(q);
        }
    }
}
