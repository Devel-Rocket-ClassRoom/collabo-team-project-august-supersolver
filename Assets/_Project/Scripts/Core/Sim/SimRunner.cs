using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// 코어가 소유하는 실행기.
    /// 솔버·리플레이·테스트가 이 진입점을 쓴다.
    /// </summary>
    public static class SimRunner
    {
        /// <summary>
        /// 스테이지를 그대로 돌린다.
        /// 시드는 스테이지가 들고 있다.
        /// </summary>
        public static SimResult Run(
            StageData stage,
            Solution solution,
            int maxSteps = SimWorld.DefaultMaxSteps)
        {
            if (stage == null) throw new System.ArgumentNullException(nameof(stage));
            return Run(stage.Level, solution, stage.Seed, maxSteps);
        }

        /// <summary>스테이지를 해시를 남기며 돌린다.</summary>
        public static SimResult RunTraced(
            StageData stage,
            Solution solution,
            List<ulong> trace,
            int maxSteps = SimWorld.DefaultMaxSteps)
        {
            if (stage == null) throw new System.ArgumentNullException(nameof(stage));
            return RunTraced(stage.Level, solution, stage.Seed, trace, maxSteps);
        }

        /// <summary>
        /// 시드를 직접 주는 저수준 경로.
        /// 조기 종료가 솔버 처리량을 좌우한다.
        /// </summary>
        public static SimResult Run(
            LevelData level,
            Solution solution,
            int seed,
            int maxSteps = SimWorld.DefaultMaxSteps)
        {
            using (var world = WorldBuilder.Build(level, solution, seed))
            {
                while (world.CurrentStep < maxSteps && !world.IsTerminal)
                    world.Step();

                return world.ToResult((solution ?? Solution.Empty).TotalInk());
            }
        }

        /// <summary>
        /// 스텝별 해시를 기록한다.
        /// 갈라진 첫 스텝을 알아야 디버깅이 된다.
        /// </summary>
        /// <param name="trace">i = i번째 Step 직후의 해시.</param>
        public static SimResult RunTraced(
            LevelData level,
            Solution solution,
            int seed,
            List<ulong> trace,
            int maxSteps = SimWorld.DefaultMaxSteps)
        {
            trace.Clear();

            using (var world = WorldBuilder.Build(level, solution, seed))
            {
                while (world.CurrentStep < maxSteps && !world.IsTerminal)
                {
                    world.Step();
                    trace.Add(WorldHasher.Hash(world));
                }

                return world.ToResult((solution ?? Solution.Empty).TotalInk());
            }
        }
    }
}
