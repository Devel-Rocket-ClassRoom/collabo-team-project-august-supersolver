using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 솔버가 놓아 보는 그림 하나.
    /// 스트로크를 몇 개 쓰든, 회전축이 있든 밖에서는 구분하지 않는다 —
    /// 지렛대처럼 여러 바디로 된 도구도 벽 한 줄과 같은 자리에 선다.
    /// </summary>
    public interface IPrimitive
    {
        /// <summary>
        /// 제 몸을 솔루션에 붙인다.
        /// 회전축이 스트로크 인덱스를 참조하므로, 넣는 쪽이 축까지 맡는다.
        /// </summary>
        void AppendTo(Solution solution);
    }
}
