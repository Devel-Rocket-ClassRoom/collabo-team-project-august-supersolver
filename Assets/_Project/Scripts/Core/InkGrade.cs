namespace PPS.Core
{
    /// <summary>
    /// 잉크를 아낀 정도를 별 등급으로 바꾼다. 저장과 보상
    /// 화면이 같은 값을 보도록 규칙을 한 곳에만 둔다.
    /// </summary>
    public static class InkGrade
    {
        public const int Bronze = 0;
        public const int Silver = 1;
        public const int Gold = 2;

        /// 상한 대비 사용량(%)이 이 값 이하면 그 등급이다.
        const float GoldPercent = 50f;
        const float SilverPercent = 75f;

        /// 상한이 없는 판은 아낄 여지가 없어 가장 낮은 등급이다.
        public static int Of(float inkUsed, float inkLimit)
        {
            if (inkLimit <= 0f) return Bronze;

            float percent = inkUsed / inkLimit * 100f;

            if (percent <= GoldPercent) return Gold;
            if (percent <= SilverPercent) return Silver;

            return Bronze;
        }
    }
}
