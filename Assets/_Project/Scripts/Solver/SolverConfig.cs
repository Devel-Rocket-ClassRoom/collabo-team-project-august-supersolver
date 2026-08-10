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

        /// <summary>
        /// 속도 방향을 몇 칸으로 나눌지. 45° 씩 8 칸이다.
        /// DiagonalTangent 가 이 칸의 경계라 둘은 함께 움직인다.
        /// </summary>
        public const int VelocityDirections = 8;

        /// 속도 축의 셀 수. 방향 × 크기 + 정지 1.
        public const int VelocityCellCount = SpeedBands * VelocityDirections + 1;

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

        /// <summary>
        /// 후보 Size 축을 몇 칸으로 쪼갤지.
        /// 등비라 2 면 공 반지름과 상한 둘뿐이고 그 사이가 통째로 빈다 —
        /// 사람이 그리는 풀이는 대개 그 중간에 있다.
        /// 1 은 상한 하나만 나오므로 더 쓸 수 없다.
        /// </summary>
        public const int CandidateSizeSteps = 4;

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
        /// 정체 판정이 맴도는 판을 걷어내므로 여기까지 오는 것은
        /// 꾸준히 멀리 가는 판뿐이다. 넉넉해도 값이 안 든다.
        /// 짧게 자르면 한 배치로 닿는 먼 셀을 놓쳐 h 가 비관이 된다.
        /// </summary>
        public const int RollSteps = 600;

        /// <summary>
        /// 공이 제자리를 맴돈다고 볼 시간.
        /// 위치 셀 하나를 이 시간 안에 못 벗어나면 그만 본다 —
        /// 2초에 제 지름도 못 가는 수는 유저가 볼 풀이가 아니다.
        /// StopSpeed 와 다른 것을 잰다. 그쪽은 방향이 정보가 되는지,
        /// 이쪽은 진행이 볼 만한지를 묻는다.
        /// </summary>
        public const int IdleSteps = 120;

        /// <summary>
        /// 궤적을 몇 스텝마다 뜰지. 시뮬 비용과는 무관하다 —
        /// 물리는 어차피 매 스텝 돌고 표본만 고르는 것이다.
        /// 중앙 속력에서 표본 간격이 위치 셀 하나 남짓이라
        /// 이보다 성기면 셀을 건너뛰어 간선을 잃는다.
        /// </summary>
        public const int TrajectoryInterval = 10;

        /// <summary>
        /// 도달 폐포를 몇 홉까지 넓힐지.
        /// 사실상 "풀이가 몇 개짜리라고 가정하는가" 다 — 깊이 D 로 지으면
        /// 마지막 확장이 "한 개 더로 이기는가" 를 물어 D 개짜리 풀이를 찾는다.
        /// 선이 D 개보다 더 필요한 레벨이면 이기는 셀이 하나도 안 나와
        /// h 가 통째로 빈다. 3 은 GapPuzzle 이 선 셋으로 풀려서 맞은 값이고,
        /// 4 는 300만 시뮬 안에 안 끝난다.
        /// </summary>
        public const int MapDepth = 3;

        /// <summary>
        /// 맵 빌드가 돌릴 시뮬 횟수 상한.
        /// 넘으면 그 자리에서 멈추고 미완성 맵을 내놓는다 —
        /// 컷이 아니라 벌점으로 쓰이므로 미완성이어도 탐색이 답을 잃지는 않는다.
        /// </summary>
        public const int MapSimBudget = 3000000;

        // ── 씬 상한 ──

        /// <summary>
        /// 동시에 로드해 둘 시뮬 씬의 상한.
        /// SimWorld 의 언로드는 프레임 끝에 처리되는데, 씬이 쌓이면
        /// 생성·해제 비용이 로드된 씬 수를 타서 전체가 제곱이 된다.
        /// 성능 손잡이가 아니라 구조 제약이다 — 맵 빌드와 탐색 둘 다
        /// 코루틴이어야 하는 이유가 이것이다.
        /// </summary>
        public const int MaxLoadedScenes = 16;

        /// 씬이 끝내 안 줄어들 때 포기하고 진행할 프레임 수.
        /// 없으면 조용히 멈춘 것과 구분이 안 된다.
        public const int MaxDrainFrames = 600;

        // ── 탐색 예산 ──

        /// <summary>
        /// 탐색이 돌릴 시뮬 횟수 상한.
        /// 잉크는 해답 하나당 예산이라 탐색을 멈추지 못한다 —
        /// 실제로 멈추는 것은 이 값과 시간이다.
        /// 근거는 아직 없다. 맵 없는 탐색이 얼마나 드는지를
        /// 재는 것이 지금 하는 일이고, 그 결과로 정한다.
        /// </summary>
        public const int SearchSimBudget = 200000;

        /// 탐색이 쓸 시간 상한(초). 근거는 위와 같다.
        public const double SearchSeconds = 600d;
    }
}
