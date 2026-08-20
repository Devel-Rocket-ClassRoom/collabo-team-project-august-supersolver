
namespace PPS.Core
{
    public static class CurrentStageIndex
    {
        public const int StagePerRow = 3;
        public const int StagePerTheme = 20;

        public static int CurrentStage;
        public static int CurrentTheme;

        // return (theme, stage) index.
        public static (int, int) GetStageAndThemeIndex(int stageIndex)
        {
            return (stageIndex / StagePerTheme + 1, stageIndex % StagePerTheme + 1);
        }
    }
}
