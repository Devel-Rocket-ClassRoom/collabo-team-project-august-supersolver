using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 솔버가 쓰는 튜닝 수치를 한곳에 모은다.
    /// </summary>
    public static class SolverConfig
    {
        // ── 자리 추리기 ──

        /// <summary>
        /// 후보를 놓을 자리를 접는 폭. 공 지름보다 잘게 쪼개면
        /// 물리적으로 구분되지 않는 자리를 나누게 된다.
        /// </summary>
        public static float PositionStep(LevelData level) => level.BallRadius * 2f;

        /// <summary>
        /// 이 아래는 멈춘 공으로 본다.
        /// 멈춘 공은 갈 곳이 없어 후보 부채의 기준을 물을 수 없다.
        /// 물리 슬립이 0 으로 스냅하는 값보다 넉넉히 위다.
        /// </summary>
        public const float StopSpeed = 0.125f;

        // ── 후보 배치 ──

        /// <summary>
        /// 공이 가는 쪽으로 부챗살을 몇 갈래 펼칠지.
        /// 실측에서 진행 방향 ±90° 밖은 새 자리를 거의 못 열었다 —
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
        /// 그릇은 뺐다 — 같은 비용에 새 자리를 3분의 1만 열고,
        /// 클리어가 순간 판정이라 "받아서 가두기" 가 필요 없다.
        /// </summary>
        public static readonly PrimitiveShape[] CandidateShapes =
        {
            PrimitiveShape.Line,
            PrimitiveShape.Triangle,
        };

        // ── 시뮬 ──

        /// <summary>
        /// 공이 제자리를 맴돈다고 볼 시간.
        /// 자리 폭 하나를 이 시간 안에 못 벗어나면 그만 본다 —
        /// 2초에 제 지름도 못 가는 수는 유저가 볼 풀이가 아니다.
        /// StopSpeed 와 다른 것을 잰다. 그쪽은 방향이 정보가 되는지,
        /// 이쪽은 진행이 볼 만한지를 묻는다.
        /// </summary>
        public const int IdleSteps = 120;

        /// <summary>
        /// 궤적을 몇 스텝마다 뜰지. 시뮬 비용과는 무관하다 —
        /// 물리는 어차피 매 스텝 돌고 표본만 고르는 것이다.
        /// 중앙 속력에서 표본 간격이 자리 폭 하나 남짓이라
        /// 이보다 성기면 자리를 건너뛴다.
        /// </summary>
        public const int TrajectoryInterval = 10;

        // ── 씬 상한 ──

        /// <summary>
        /// 동시에 로드해 둘 시뮬 씬의 상한.
        /// SimWorld 의 언로드는 프레임 끝에 처리되는데, 씬이 쌓이면
        /// 생성·해제 비용이 로드된 씬 수를 타서 전체가 제곱이 된다.
        /// 성능 손잡이가 아니라 구조 제약이다 —
        /// 탐색이 코루틴이어야 하는 이유가 이것이다.
        /// </summary>
        public const int MaxLoadedScenes = 16;

        /// 씬이 끝내 안 줄어들 때 포기하고 진행할 프레임 수.
        /// 없으면 조용히 멈춘 것과 구분이 안 된다.
        public const int MaxDrainFrames = 600;

        // ── 탐색 ──

        /// <summary>
        /// 한 풀이에 쓸 선 개수의 상한.
        /// 성능 손잡이가 아니라 "유저가 한 번에 구상하는 획 수" 다.
        /// 전수 순회라 후보 수의 이 제곱만큼 시뮬이 든다 —
        /// 올리려면 후보 집합을 먼저 줄여야 한다.
        /// </summary>
        public const int SearchMaxDepth = 3;

        /// <summary>
        /// 탐색이 돌릴 시뮬 횟수 상한.
        /// 잉크는 해답 하나당 예산이라 탐색을 멈추지 못한다 —
        /// 실제로 멈추는 것은 이 값과 시간이다.
        /// 여기 걸려 끝나면 Exhausted 가 아니라서, 그 실행은
        /// 레벨에 대해 아무것도 증명하지 못한다.
        /// </summary>
        public const int SearchSimBudget = 200000;

        /// 탐색이 쓸 시간 상한(초). 근거는 위와 같다.
        public const double SearchSeconds = 600d;
    }
}
