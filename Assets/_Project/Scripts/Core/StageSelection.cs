namespace PPS.Core
{
    public static class StageSelection
    {
        /// 지금 고른 자리.
        public static StageEntry Current { get; private set; }

        /// <summary>
        /// 테마를 고른다. 스테이지는 처음으로 되돌린다 — 앞
        /// 테마에서 쓰던 번호가 남으면 두 축이 어긋난다.
        /// </summary>
        public static void SelectTheme(int theme)
        {
            Current = new StageEntry(theme, 0);
        }

        public static void SelectStage(int stage)
        {
            Current = new StageEntry(Current.Theme, stage);
        }
    }
}
