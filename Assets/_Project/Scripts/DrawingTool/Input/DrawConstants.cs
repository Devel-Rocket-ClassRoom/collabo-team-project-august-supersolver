namespace PPS.DrawingTool
{
    /// <summary>
    /// 그리기 데이터 품질 상수. 전부 월드 유닛이다 —
    /// 저장되는 점 밀도가 기기 해상도·주사율과 무관해야
    /// 솔버가 푼 결과와 플레이어의 결과가 일치한다.
    /// </summary>
    public static class DrawConstants
    {
        /// 직전 확정점과 이만큼 떨어져야 점을 받는다.
        public const float MinPointDistance = 0.06f;

        /// RDP 허용 오차. 필터 격자보다 작아야 의미가 있다.
        public const float RdpEpsilon = 0.05f;

        /// 이보다 짧은 획은 만들지 않고 롤백한다.
        public const float MinStrokeLength = 0.25f;

        /// <summary>
        /// 새 획이 이 거리 안을 지나면 붙을 곳 없던 핀이
        /// 그 획에 걸린다. 탭 반경(dp)과 달리 월드 상수다 —
        /// 재결합 결과가 Solution 에 저장되므로 기기를 타면
        /// 같은 그림이 기기마다 다른 판이 된다.
        /// 탭 반경보다 넓어야 한다 — 좁으면 핀을 찍을 수 있던
        /// 자리에 그은 획을 그 핀이 못 받는다.
        /// </summary>
        public const float PivotRebindDistance = 0.25f;
    }
}
