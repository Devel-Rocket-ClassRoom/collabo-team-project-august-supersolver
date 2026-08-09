using System;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공의 위상 상태를 셀 하나로 접는다.
    /// 폭이 곧 "이 정도 차이는 같은 상황" 선언이라
    /// 인스턴스 하나가 맵 하나의 격자를 정한다.
    /// 위치는 일정 폭으로, 속도는 방향 칸 × 크기 칸으로 나눈다.
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

        /// 1/√2. 대각 단위 벡터의 성분이다.
        const float Diagonal = 0.70710678f;

        /// <summary>
        /// 방향 칸의 단위 벡터. 0 이 +x 이고 반시계로 45° 씩이다.
        /// 삼각함수로 구하면 수직 칸이 정확히 0 이 안 나와
        /// 표로 박아 둔다.
        /// </summary>
        static readonly Vector2[] Headings =
        {
            new Vector2(1f, 0f),
            new Vector2(Diagonal, Diagonal),
            new Vector2(0f, 1f),
            new Vector2(-Diagonal, Diagonal),
            new Vector2(-1f, 0f),
            new Vector2(-Diagonal, -Diagonal),
            new Vector2(0f, -1f),
            new Vector2(Diagonal, -Diagonal),
        };

        public BallCell Quantize(Vector2 position, Vector2 velocity)
        {
            int mag = SpeedBand(velocity.magnitude);
            int dir = mag == 0 ? 0 : Direction(velocity);

            return new BallCell(Index(position.x), Index(position.y), dir, mag);
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
                cell.Mag == 0 ? Vector2.zero : Headings[cell.Dir] * BandSpeed(cell.Mag));

        /// 셀 중심. 경계에 두면 왕복이 옆 셀로 샌다.
        float Center(int index) => (index + 0.5f) * PositionStep;

        /// <summary>
        /// 구간의 대표 속력. 로그 축으로 나눈 구간이라
        /// 기하 중앙이 가운데다.
        /// 최상단은 위가 열려 있어 중앙이 없어,
        /// 실측 점유 구간의 가운데로 따로 잡는다.
        /// </summary>
        static float BandSpeed(int band)
        {
            float floor = SolverConfig.BandFloor(band);
            float exponent = band == SolverConfig.SpeedBands
                ? SolverConfig.ClampMidExponent
                : 0.5f;

            return floor * Mathf.Pow(SolverConfig.SpeedRatio, exponent);
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
        /// 속도를 45° 칸 하나로 접는다.
        /// 22.5° 경계는 흔한 자세가 아니라 스냅을 두지 않는다 —
        /// 대신 수평·수직이 칸 한가운데 오도록 잘라 뒀다.
        /// </summary>
        static int Direction(Vector2 velocity)
        {
            float ax = Mathf.Abs(velocity.x);
            float ay = Mathf.Abs(velocity.y);

            bool right = velocity.x >= 0f;
            bool up = velocity.y >= 0f;

            if (ay < ax * SolverConfig.DiagonalTangent) return right ? 0 : 4;
            if (ax < ay * SolverConfig.DiagonalTangent) return up ? 2 : 6;

            if (right) return up ? 1 : 7;
            return up ? 3 : 5;
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
    }
}
