namespace PPS.Solver
{
    /// <summary>
    /// 레벨 구성 요소 하나에서 앵커를 뽑는 규칙.
    /// 요소마다 받는 타입이 달라 진입점은 파생 쪽에서 연다 —
    /// 이 계층이 있는 이유는 SelectService 의 딕셔너리가
    /// 한 종류만 담게 하기 위해서다.
    /// </summary>
    public abstract class SolverPrimitiveAnchorSelector
    {
    }
}
