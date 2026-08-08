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

        // ── 후보 배치 ──

        /// <summary>
        /// 공이 가는 쪽으로 부챗살을 몇 갈래 펼칠지.
        /// 실측에서 진행 방향 ±90° 밖은 새 셀을 거의 못 열었다 —
        /// 공이 안 가는 쪽에 놓아 봐야 닿지 않는다.
        /// 그래서 부채는 180° 로 고정하고 갈래 수만 손잡이다.
        /// 홀수라야 정면이 한 갈래로 잡힌다. 정면이 압도적이다.
        /// </summary>
        public const int CandidateDirections = 5;

        /// 후보 Size 축을 몇 칸으로 쪼갤지.
        /// 1 은 최대 크기 하나만 나오므로 쓰지 않는다.
        public const int CandidateSizeSteps = 2;

        /// <summary>
        /// 후보로 낼 Shape. enum 에서 빼면 코덱과 직렬화가 딸려 오므로
        /// 목록으로 끄고 켠다.
        /// 그릇은 뺐다 — 같은 비용에 새 셀을 3분의 1만 열고,
        /// 클리어가 순간 판정이라 "받아서 가두기" 가 필요 없다.
        /// </summary>
        public static readonly PrimitiveShape[] CandidateShapes =
        {
            PrimitiveShape.Line,
            PrimitiveShape.Triangle,
        };

        // ── 맵 빌드 ──

        /// <summary>
        /// 셀 하나를 굴리는 스텝 상한.
        /// 시뮬은 공이 잠들거나 죽으면 알아서 끝나므로
        /// 이 값은 그때까지 안 끝난 판에만 문다.
        /// 짧게 자르면 한 배치로 닿는 먼 셀을 놓쳐 h 가 비관이 된다.
        /// </summary>
        public const int RollSteps = 180;

        /// <summary>
        /// 궤적을 몇 스텝마다 뜰지. 시뮬 비용과는 무관하다 —
        /// 물리는 어차피 매 스텝 돌고 표본만 고르는 것이다.
        /// 중앙 속력에서 표본 간격이 위치 셀 하나 남짓이라
        /// 이보다 성기면 셀을 건너뛰어 간선을 잃는다.
        /// </summary>
        public const int TrajectoryInterval = 10;

        /// <summary>
        /// 도달 폐포를 몇 홉까지 넓힐지. 곧 프리미티브 개수다.
        /// 실측에서 새 셀 생산이 여기서 정점을 찍고 꺾인다 —
        /// 한 홉 더 가면 값은 오르고 얻는 셀은 준다.
        /// 자른 너머는 h 가 ∞ 지만 컷이 아니라 페널티라
        /// 탐색이 답을 잃지는 않는다. ∞ 셀 비율로 사후 검증한다.
        /// </summary>
        public const int MapDepth = 4;
    }
}
