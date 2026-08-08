using System;
using PPS.Core;
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
        /// 셀을 대표 상태로 되돌린다. 맵 빌드가 셀마다
        /// 어디서 굴릴지를 여기서 얻는다.
        /// 대표 속도를 전부 0 으로 두면 속도가 0 이 아닌 셀에서
        /// 나가는 간선이 안 생겨 h 가 전부 ∞ 가 된다.
        /// </summary>
        public BallState Dequantize(BallCell cell)
            => new BallState(
                new Vector2(Center(cell.X), Center(cell.Y)),
                Velocity(cell.VX, cell.VY));

        /// 셀 중심. 경계에 두면 왕복이 옆 셀로 샌다.
        float Center(int index) => (index + 0.5f) * PositionStep;

        /// <summary>
        /// 방향 부호 × 크기 구간 라벨을 속도 벡터로 되돌린다.
        /// 부호쌍이 곧 8방향이라 정규화만 하면 방향이 나온다.
        /// </summary>
        static Vector2 Velocity(int vx, int vy)
        {
            int band = Mathf.Max(Mathf.Abs(vx), Mathf.Abs(vy));
            if (band == 0)
                return Vector2.zero;

            var direction = new Vector2(Unit(vx), Unit(vy)).normalized;
            return direction * BandSpeed(band);
        }

        /// <summary>
        /// 구간의 대표 속력. 로그 축으로 나눈 구간이라
        /// 기하 중앙이 가운데다.
        /// 최상단은 위가 열려 있어 중앙이 없다 — 실측에서
        /// 표본이 하한에 붙어 있어 하한을 그대로 쓴다.
        /// </summary>
        static float BandSpeed(int band)
        {
            float floor = SolverConfig.BandFloor(band);
            return band == SolverConfig.SpeedBands
                ? floor
                : floor * Mathf.Sqrt(SolverConfig.SpeedRatio);
        }

        /// Mathf.Sign 은 0 에 1 을 준다. 0 은 0 이어야 한다.
        static float Unit(int label) => label == 0 ? 0f : (label < 0 ? -1f : 1f);

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
