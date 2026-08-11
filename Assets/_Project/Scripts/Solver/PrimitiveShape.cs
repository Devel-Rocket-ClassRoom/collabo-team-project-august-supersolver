namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브의 종류.
    /// 유저가 한 획에 그을 만한 모양만 둔다 —
    /// 여기 없는 모양은 솔버가 시도하지 않는다.
    /// </summary>
    public enum PrimitiveShape
    {
        /// 곧은 선분 하나.
        Line = 0,

        /// 정삼각형. 닫힌 고리다.
        Triangle = 1,
    }
}
