using System;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공의 위상 상태를 셀 하나로 접는다.
    /// 폭이 곧 "이 정도 차이는 같은 상황" 선언이라
    /// 인스턴스 하나가 맵 하나의 격자를 정한다.
    /// 위치는 일정 폭으로, 속도는 방향 8 × 크기 구간으로 나눈다.
    /// </summary>
    public sealed class BallQuantizer
    {
        /// 폭. 레벨마다 달라 인스턴스가 들고 있다.
        /// 기준값은 SolverConfig.PositionStep 이 정한다.
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
            if (Mathf.Abs(q - line) <= SolverConfig.SnapEpsilon)
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
            bool horizontal = ay < ax * SolverConfig.DiagonalTangent;
            bool vertical = ax < ay * SolverConfig.DiagonalTangent;

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

            float q = Mathf.Log(speed / SolverConfig.StopSpeed) / Mathf.Log(SolverConfig.SpeedRatio);
            float line = Mathf.Round(q);
            if (Mathf.Abs(q - line) <= SolverConfig.SnapEpsilon)
                q = line;

            int band = 1 + Mathf.FloorToInt(q);
            if (band <= 0)
                return 0;

            return band > SolverConfig.SpeedBands ? SolverConfig.SpeedBands : band;
        }

        static int Sign(float value) => value < 0f ? -1 : 1;
    }
}
