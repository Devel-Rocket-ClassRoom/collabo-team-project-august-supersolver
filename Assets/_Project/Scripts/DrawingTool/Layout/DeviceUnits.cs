namespace PPS.DrawingTool
{
    /// <summary>
    /// dp ↔ 화면 픽셀 환산. Screen 을 직접 읽지 않고
    /// dpi 를 받는다 — 대표 기기를 합성해 기기 없이
    /// 검증하기 위함이다.
    /// </summary>
    public readonly struct DeviceUnits
    {
        /// dp 의 정의 그 자체. 1dp = 160dpi 에서 1px.
        public const float BaselineDpi = 160f;

        /// Screen.dpi 가 0·NaN 을 주는 안드로이드 기기가
        /// 있다. xhdpi 를 가정한다.
        public const float FallbackDpi = 320f;

        public readonly float Dpi;

        public DeviceUnits(float dpi)
        {
            Dpi = IsUsable(dpi) ? dpi : FallbackDpi;
        }

        public float ToPixels(float dp) => dp * Dpi / BaselineDpi;

        /// <summary>
        /// 호출자가 폴백 사실을 알아야 경고를 남길 수 있다.
        /// 생성자는 조용히 폴백만 한다.
        /// </summary>
        public static bool IsUsable(float dpi) =>
            dpi > 0f && !float.IsNaN(dpi) && !float.IsInfinity(dpi);
    }
}
