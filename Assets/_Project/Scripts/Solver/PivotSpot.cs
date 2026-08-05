namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브에서 회전축이 걸리는 지점.
    /// 걸 곳이 두 곳뿐이라 연속값 대신 열거다 —
    /// 샘플링에서도 카테고리로 다룬다.
    /// </summary>
    public enum PivotSpot
    {
        None = 0,

        /// 시작점 (t = 0)
        Start = 1,

        /// 중점 (t = 0.5)
        Middle = 2,
    }
}
