namespace PPS.DrawingTool
{
    /// <summary>
    /// 인체공학 상수. 전부 dp 다 — 손가락 크기는
    /// 해상도가 아니라 화면의 물리 크기를 따른다.
    /// 데이터 품질 상수는 WorldMetrics 에 따로 있다.
    /// </summary>
    public static class TouchMetrics
    {
        /// 이보다 덜 움직이면 탭이다. 드로잉 도구는
        /// 탭을 무시한다 — 미세 콜라이더가 물리를 흔든다.
        public const float TapThresholdDp = 8f;

        /// 그리기 지점을 손가락보다 위로 띄우는 거리.
        public const float DrawOffsetDp = 24f;

        /// 기획 요구. 이보다 작은 버튼은 못 누른다.
        public const float MinTouchTargetDp = 48f;
    }
}
