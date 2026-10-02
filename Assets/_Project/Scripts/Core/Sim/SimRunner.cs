using System.Collections.Generic;
using UnityEngine;

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
        /// <param name="start">공의 출발 상태. null 이면 레벨의 시작점.</param>
        public static SimResult Run(
            LevelData level,
            Solution solution,
            int seed,
            int maxSteps = SimWorld.DefaultMaxSteps,
            BallState? start = null)
        {
            using (var world = WorldBuilder.Build(level, solution, seed, start))
            {
                while (world.CurrentStep < maxSteps && !world.IsTerminal)
                    world.Step();

                return world.ToResult((solution ?? Solution.Empty).TotalInk());
            }
        }

        /// <summary>
        /// 공의 궤적을 샘플링하며 돌린다.
        /// 해시 추적과 별개다 — 해시는 결정론 검사라
        /// 위상 정보가 없다.
        /// </summary>
        /// <param name="buffer">시작할 때 비운다. 재사용해도 된다.</param>
        /// <param name="start">공의 출발 상태. null 이면 레벨의 시작점.</param>
        /// <param name="idle">제자리를 맴돌 때 끊을 기준. null 이면 안 끊는다.
        /// 끊긴 판은 Timeout 으로 끝나되 EndStep 이 상한보다 작다.</param>
        public static SimResult RunSampled(
            LevelData level,
            Solution solution,
            int seed,
            TrajectoryBuffer buffer,
            int maxSteps = SimWorld.DefaultMaxSteps,
            BallState? start = null,
            IdleCutoff? idle = null)
        {
            buffer.Clear();

            using (var world = WorldBuilder.Build(level, solution, seed, start))
            {
                Vector2 anchor = world.Ball.position;
                int still = 0;

                while (world.CurrentStep < maxSteps && !world.IsTerminal)
                {
                    world.Step();

                    if (buffer.IsSampleStep(world.CurrentStep))
                        buffer.Add(new BallSample(
                            world.CurrentStep, world.Ball.position, world.Ball.linearVelocity));

                    if (idle.HasValue && Idle(world, idle.Value, ref anchor, ref still))
                        break;
                }

                return world.ToResult((solution ?? Solution.Empty).TotalInk());
            }
        }

        /// <summary>
        /// 마지막으로 의미 있게 움직인 뒤 얼마나 지났는지 센다.
        /// 대기 중인 장치가 있으면 시계를 세우고 기준점만 따라간다 —
        /// 폭탄이 터지기 전에 끊으면 그 판의 결과가 통째로 달라진다.
        /// Judge 가 Stalled 를 미루는 것과 같은 이유다.
        /// </summary>
        static bool Idle(SimWorld world, in IdleCutoff cutoff, ref Vector2 anchor, ref int still)
        {
            if (world.AnyPendingWork() ||
                Vector2.Distance(world.Ball.position, anchor) > cutoff.Radius)
            {
                anchor = world.Ball.position;
                still = 0;
                return false;
            }

            return ++still >= cutoff.Steps;
        }

        /// <summary>
        /// 스텝별 해시를 기록한다.
        /// 갈라진 첫 스텝을 알아야 디버깅이 된다.
        /// </summary>
        /// <param name="trace">i = i번째 Step 직후의 해시.</param>
        /// <param name="start">공의 출발 상태. null 이면 레벨의 시작점.</param>
        public static SimResult RunTraced(
            LevelData level,
            Solution solution,
            int seed,
            List<ulong> trace,
            int maxSteps = SimWorld.DefaultMaxSteps,
            BallState? start = null)
        {
            trace.Clear();

            using (var world = WorldBuilder.Build(level, solution, seed, start))
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
