using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// 코어가 소유하는 실행기 계약: <c>Run(level, solution, seed, maxSteps) → SimResult</c>.
    /// 솔버·리플레이 검증·테스트가 전부 이 진입점을 쓴다.
    /// </summary>
    public static class SimRunner
    {
        /// <summary>
        /// 한 판을 끝까지(또는 상한까지) 돌린다.
        ///
        /// 조기 종료(Clear/Fail/Stalled)가 솔버 처리량을 좌우한다. 실패 시도 대부분은
        /// 수 초 안에 결판나며, 그걸 잡지 못하면 매번 30초를 끝까지 시뮬하게 된다.
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
        /// 스텝별 월드 해시를 기록하며 실행한다.
        ///
        /// 결정론이 깨졌을 때 **어느 스텝에서 갈라졌는지**를 알아야 원인을 찾을 수 있다.
        /// 최종 상태만 비교하면 중간에 갈라졌다가 우연히 비슷해진 경우를 놓치고,
        /// 무엇보다 디버깅의 출발점이 없어진다.
        /// </summary>
        /// <param name="trace">인덱스 i = i번째 Step 직후의 월드 해시.</param>
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
