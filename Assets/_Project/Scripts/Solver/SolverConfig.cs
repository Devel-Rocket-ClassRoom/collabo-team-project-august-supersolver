using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 솔버가 쓰는 튜닝 수치를 한곳에 모은다.
    /// 흩어 두면 맵 빌드와 탐색이 서로 다른 값을 쓰게 되고,
    /// 그러면 h 가 탐색이 낼 수 없는 배치를 전제한다.
    /// </summary>
    public static class SolverConfig
    {
        // ── 셀 양자화 ──

        /// <summary>
        /// 위치 셀의 폭. 공 지름보다 잘게 쪼개면
        /// 물리적으로 구분되지 않는 상태를 나누게 된다.
        /// </summary>
        public static float PositionStep(LevelData level) => level.BallRadius * 2f;

        /// 격자선에 붙일 폭. 격자 단위라 폭에 안 딸린다.
        /// 부동소수 오차(~1e-7)보다 훨씬 크고
        /// 물리적으로 유의미한 차이보다는 훨씬 작다.
        public const float SnapEpsilon = 0.0001f;

        /// 8방향 분류의 경계. tan(22.5°) 다.
        /// 이 값으로 자르면 수평·수직이 구간 한가운데 온다 —
        /// 평지를 구르거나 곧장 떨어지는 공이 가장 흔한데
        /// 그게 경계에 걸리면 오차 한 번에 셀이 갈린다.
        public const float DiagonalTangent = 0.4142136f;

        /// <summary>
        /// 이 아래는 방향 없는 정지 한 셀로 모은다.
        /// 셀 대표 상태 실측에서 이 아래가 8%, 그중 2/3 는
        /// 물리 슬립이 0 으로 스냅한 진짜 정지다.
        /// 더 낮추면 구간을 하나 더 늘려야 해
        /// 속도 상태 공간이 20% 는다.
        /// </summary>
        public const float StopSpeed = 0.125f;

        /// 크기 구간의 등비 비율. 0.5 와 2 의 차이는 크고
        /// 18 과 22 의 차이는 거의 없어 로그 축으로 나눈다.
        public const float SpeedRatio = 3f;

        /// <summary>
        /// 크기 구간 수. 최상단은 그 위를 전부 접는 클램프다.
        /// 경계는 0.125 · 0.375 · 1.125 · 3.375 · 10.125 이고
        /// 마지막이 실측 p95(9.4) 바로 위라 클램프에 몰리지 않는다.
        /// </summary>
        public const int SpeedBands = 5;

        /// 속도 축의 셀 수. 방향 8 × 크기 + 정지 1.
        public const int VelocityCellCount = SpeedBands * 8 + 1;

        /// <summary>
        /// 클램프 구간 대표 속력의 자리. 하한 × 비율^이 값이다.
        /// 위가 열려 있어 기하 중앙이 없어, 실측 점유 구간
        /// (10.1~18.3)의 가운데에 오도록 잡았다.
        /// 대표를 올리면 관측도 따라 오르는데, 13.3 부근에서
        /// 멎는다. 0 이면 하한 그대로다.
        /// </summary>
        public const float ClampMidExponent = 0.25f;

        /// <summary>
        /// 크기 구간 band 의 하한 속력. band 1 이면 정지 경계다.
        /// 양자화·역양자화·실측이 같은 식을 봐야 한다 —
        /// 따로 적으면 한쪽만 고쳤을 때 조용히 어긋난다.
        /// </summary>
        public static float BandFloor(int band)
            => StopSpeed * Mathf.Pow(SpeedRatio, band - 1);
    }
}
