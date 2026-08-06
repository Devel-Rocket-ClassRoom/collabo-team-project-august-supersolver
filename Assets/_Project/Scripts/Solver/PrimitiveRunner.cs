using System.Collections.Generic;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브 배치 하나를 시뮬 한 번으로 바꾼다.
    /// 솔버가 쓰는 유일한 시뮬 진입점이다.
    /// 코어는 솔버를 모르므로 다리는 이쪽에 둔다.
    /// </summary>
    public static class PrimitiveRunner
    {
        /// <summary>
        /// 스테이지를 그대로 돌린다. 시드는 스테이지가 들고 있다.
        /// </summary>
        public static SimResult Run(
            StageData stage,
            IReadOnlyList<Primitive> primitives,
            int maxSteps = SimWorld.DefaultMaxSteps)
            => SimRunner.Run(stage, PrimitiveDecoder.Decode(primitives), maxSteps);

        /// <summary>
        /// 시드를 직접 주는 경로.
        /// 디코드 순서가 곧 월드 구축 순서라
        /// 여기서 정렬하거나 재배치하지 않는다.
        /// </summary>
        public static SimResult Run(
            LevelData level,
            IReadOnlyList<Primitive> primitives,
            int seed,
            int maxSteps = SimWorld.DefaultMaxSteps)
            => SimRunner.Run(level, PrimitiveDecoder.Decode(primitives), seed, maxSteps);
    }
}
