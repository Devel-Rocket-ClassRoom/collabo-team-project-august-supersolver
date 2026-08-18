namespace PPS.DrawingTool
{
    /// <summary>
    /// 화면에 겹치는 것들의 앞뒤. 전부 z 가 같아
    /// 이 값이 없으면 순서가 정해지지 않는다 —
    /// 씬을 다시 열면 바뀔 수 있다.
    /// </summary>
    public static class RenderOrder
    {
        /// 테마 배경 한 장. 맨 밑이다.
        public const int Background = -200;

        /// 그릴 수 있는 영역을 두르는 점선.
        public const int PlayArea = -100;

        public const int KillLine = -50;
        public const int Terrain = -40;
        public const int Device = -30;
        public const int Goal = -20;
        public const int Star = -10;

        /// 확정 획은 여기서 획 인덱스만큼 올라간다.
        /// 나중에 그린 획이 위다.
        public const int Stroke = 0;

        public const int PivotMarker = 1000;

        /// 공이 획에 가리면 어디 있는지 알 수 없다.
        public const int Ball = 2000;

        /// 그리는 중인 선은 항상 맨 위다.
        public const int Preview = short.MaxValue;
    }
}
