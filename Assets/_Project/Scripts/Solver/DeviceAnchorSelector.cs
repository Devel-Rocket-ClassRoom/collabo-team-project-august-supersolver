using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 장치에서 앵커를 뽑는다. 장치 종류별 규칙의 기본값이다.
    /// 종류를 가리지 않고 말할 수 있는 것은 놓인 자리뿐이라
    /// 자리만 내고 그림은 비운다 — 무엇을 그릴지는 장치가
    /// 무슨 일을 하는지에 달렸고, 그것은 종류별 파생이 안다.
    /// </summary>
    public class DeviceAnchorSelector : SolverPrimitiveAnchorSelector
    {
        public virtual SolverAnchor[] Select(DeviceData device)
            => new[] { new SolverAnchor(device.Position, Primitives(device)) };

        protected virtual Primitive[] Primitives(DeviceData device)
            => new Primitive[0];
    }
}
