using PPS.Core;

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
        /// 실측에서 정지(0)와 최저 운동(0.13) 사이가 통째로 비어 있다 —
        /// 물리 슬립이 속도를 0 으로 스냅하기 때문이다.
        /// 그 빈 구간 안이라 어느 쪽 표본도 잘라내지 않는다.
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
    }
}
