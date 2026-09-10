
namespace PPS.Core
{
    public static class CurrentStageIndex
    {
        public const int StagePerRow = 3;
        public const int StagePerTheme = 20;

        /// 지금 고른 스테이지. 테마 안에서의 번호다.
        public static int CurrentStage;

        /// 지금 고른 테마. 0 부터 센다.
        public static int CurrentTheme;

        /// 지금 스테이지의 전역 번호. 저장된 진척도와 비교할
        /// 때는 항상 이 축으로 올려서 비교한다.
        public static int CurrentGlobalIndex => GlobalIndexOf(CurrentTheme, CurrentStage);

        public static int GlobalIndexOf(int theme, int stage) => theme * StagePerTheme + stage;

        public static int ThemeOf(int globalIndex) => globalIndex / StagePerTheme;

        public static int StageOf(int globalIndex) => globalIndex % StagePerTheme;

        /// <summary>
        /// 전역 번호를 화면 표기용 1-base 로 바꾼다.
        /// 판정에 쓰면 안 된다 — 축이 어긋난다.
        /// </summary>
        public static (int theme, int stage) GetThemeAndStageNumber(int globalIndex)
        {
            return (ThemeOf(globalIndex) + 1, StageOf(globalIndex) + 1);
        }
    }
}
