using System;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공의 위상 상태를 셀 하나로 접는다.
    /// 폭이 곧 "이 정도 차이는 같은 상황" 선언이라
    /// 인스턴스 하나가 맵 하나의 격자를 정한다.
    /// 위치는 일정 폭으로, 속도는 방향 8 × 크기 4 로 나눈다.
    /// </summary>
    public sealed class BallQuantizer
    {
        /// 격자선에 붙일 폭. 격자 단위라 폭에 안 딸린다.
        /// 부동소수 오차(~1e-7)보다 훨씬 크고
        /// 물리적으로 유의미한 차이보다는 훨씬 작다.
        const float SnapEpsilon = 0.0001f;

        /// 8방향 분류의 경계. tan(22.5°) 다.
        /// 이 값으로 자르면 수평·수직이 구간 한가운데 온다 —
        /// 평지를 구르거나 곧장 떨어지는 공이 가장 흔한데
        /// 그게 경계에 걸리면 오차 한 번에 셀이 갈린다.
        const float DiagonalTangent = 0.4142136f;

        /// 속도 구간. 실측 전 임시값이다.
        /// 등비인 이유는 0.5 와 2 의 차이는 크고
        /// 18 과 22 의 차이는 거의 없기 때문이다.
        /// 구간 경계는 0.5 · 1.5 · 4.5 · 13.5 이고
        /// 그 위는 전부 최상단으로 접는다.
        const float StopSpeed = 0.5f;
        const float SpeedRatio = 3f;
        const int SpeedBands = 4;

        /// 폭은 실측 후 결정한다. 히트율과 정확도가
        /// 맞바뀌는 값이라 근거 없이 못 정한다.
        public readonly float PositionStep;

        public BallQuantizer(float positionStep)
        {
            if (positionStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(positionStep));

            PositionStep = positionStep;
        }

        public BallCell Quantize(Vector2 position, Vector2 velocity)
        {
            Velocity(velocity, out int vx, out int vy);
            return new BallCell(Index(position.x), Index(position.y), vx, vy);
        }

        /// <summary>
        /// 격자선 바로 옆의 값은 선 위로 끌어다 놓고 자른다.
        /// 안 그러면 선에 걸친 값이 오차 한 번에
        /// 옆 셀로 넘어가 같은 상태가 두 키로 갈린다.
        /// </summary>
        int Index(float value)
        {
            float q = value / PositionStep;
            float line = Mathf.Round(q);
            if (Mathf.Abs(q - line) <= SnapEpsilon)
                q = line;

            return Mathf.FloorToInt(q);
        }

        /// <summary>
        /// 속도를 방향 부호 × 크기 구간으로 접는다.
        /// 멈춘 공은 방향이 의미가 없으므로 (0,0) 하나로 모은다 —
        /// 안 그러면 물리적으로 같은 정지 상태가 8칸으로 갈린다.
        /// </summary>
        static void Velocity(Vector2 velocity, out int vx, out int vy)
        {
            int band = SpeedBand(velocity.magnitude);
            if (band == 0)
            {
                vx = 0;
                vy = 0;
                return;
            }

            float ax = Mathf.Abs(velocity.x);
            float ay = Mathf.Abs(velocity.y);

            // 22.5° 경계는 흔한 자세가 아니라 스냅을 두지 않는다.
            bool horizontal = ay < ax * DiagonalTangent;
            bool vertical = ax < ay * DiagonalTangent;

            vx = vertical ? 0 : Sign(velocity.x) * band;
            vy = horizontal ? 0 : Sign(velocity.y) * band;
        }

        /// <summary>
        /// 등비 구간 인덱스. 0 이면 정지다.
        /// 경계 스냅은 위치와 같은 이유로 로그 축에서 한다.
        /// </summary>
        static int SpeedBand(float speed)
        {
            if (speed <= 0f)
                return 0;

            float q = Mathf.Log(speed / StopSpeed) / Mathf.Log(SpeedRatio);
            float line = Mathf.Round(q);
            if (Mathf.Abs(q - line) <= SnapEpsilon)
                q = line;

            int band = 1 + Mathf.FloorToInt(q);
            if (band <= 0)
                return 0;

            return band > SpeedBands ? SpeedBands : band;
        }

        static int Sign(float value) => value < 0f ? -1 : 1;
    }
}
