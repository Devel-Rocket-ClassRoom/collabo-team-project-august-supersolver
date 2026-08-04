using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 파편이 Fail 을 만드는가,
    /// 그리고 수명이 조기 종료를 막지 않는가.
    /// 뒤쪽이 곧 솔버 처리량이다.
    /// </summary>
    public class FragBombTests
    {
        const int Steps = SimWorld.DefaultMaxSteps;

        [Test]
        public void 파편이_생성되어_위험_바디로_등록된다()
        {
            var stage = TestLevels.FragBombPitStage();

            using (var world = WorldBuilder.Build(stage, null))
            {
                Assert.AreEqual(0, LiveHazards(world), "터지기 전인데 위험 바디가 있다.");

                for (int i = 0; i < TestLevels.FragBombFireStep + 2; i++) world.Step();

                Assert.AreEqual(FragBombDevice.FragmentCount, LiveHazards(world),
                    $"파편 {FragBombDevice.FragmentCount}개가 위험 목록에 등록되어야 한다.");
            }
        }

        [Test]
        public void 파편이_공에_닿으면_Fail()
        {
            // 구덩이라 어느 방향으로 튀든 닿는다.
            // 안 나면 Judge 가 위험 바디를 안 본다.
            var stage = TestLevels.FragBombPitStage();
            var result = SimRunner.Run(stage, null);

            Assert.AreEqual(SimOutcome.Fail, result.Outcome,
                $"파편이 갇힌 구덩이인데 {result.Outcome} 이 나왔다. " +
                "Judge 의 위험 바디 검사나 파편 등록을 확인할 것.");

            Assert.Greater(result.EndStep, TestLevels.FragBombFireStep,
                "터지기도 전에 실패했다 — 다른 원인으로 Fail 이 난 것이다.");
        }

        [Test]
        public void 파편이_닿지_않으면_실패로_잡지_않는다()
        {
            // 위험 바디가 "존재하기만 해도" 실패가 되면 안 된다. 닿아야 실패다.
            var result = SimRunner.Run(TestLevels.FragBombFarAway(), null, 0, Steps);

            Assert.AreNotEqual(SimOutcome.Fail, result.Outcome,
                "파편이 멀리 있는데 실패로 판정했다 — 접촉 검사가 아니라 존재 검사를 하고 있다.");
        }

        [Test]
        public void 파편_수명이_다하면_사라지고_Stalled_가_난다()
        {
            // 수명이 없으면 계속 튀어 Timeout 까지 간다.
            var result = SimRunner.Run(TestLevels.FragBombFarAway(), null, 0, Steps);

            Assert.AreEqual(SimOutcome.Stalled, result.Outcome,
                $"{result.Outcome} 이 나왔다. Timeout 이면 파편이 걷히지 않아 조기 종료가 막힌 것이다.");

            Assert.Less(result.EndStep, Steps,
                "상한까지 갔다 — 조기 종료가 동작하지 않았다.");
        }

        [Test]
        public void 파괴된_파편은_바디_목록에서_자리를_유지한다()
        {
            using (var world = WorldBuilder.Build(TestLevels.FragBombFarAway(), null, 0))
            {
                for (int i = 0; i < TestLevels.FragBombFireStep + 2; i++) world.Step();

                int countAfterSpawn = world.Bodies.Count;
                Assert.AreEqual(FragBombDevice.FragmentCount, LiveHazards(world));

                // 수명이 다할 때까지 돌린다.
                for (int i = 0; i < 200 && LiveHazards(world) > 0 && !world.IsTerminal; i++)
                    world.Step();

                Assert.AreEqual(0, LiveHazards(world), "파편이 수명 뒤에도 남아 있다.");

                // 목록에서 빼면 뒤 인덱스가 밀려 해시 구조가 통째로 바뀐다.
                Assert.AreEqual(countAfterSpawn, world.Bodies.Count,
                    "파괴된 파편을 바디 목록에서 빼버렸다 — 뒤 인덱스가 전부 밀린다.");
            }
        }

        [Test]
        public void 스테이지_진입점은_스테이지_시드를_쓴다()
        {
            var stage = TestLevels.FragBombPitStage(seed: 12345);

            var viaStage = new List<ulong>();
            SimRunner.RunTraced(stage, null, viaStage, Steps);

            var viaSeed = new List<ulong>();
            SimRunner.RunTraced(stage.Level, null, stage.Seed, viaSeed, Steps);

            CollectionAssert.AreEqual(viaSeed, viaStage,
                "스테이지로 돌린 결과와 그 시드로 돌린 결과가 다르다 — 스테이지가 시드를 흘리고 있다.");
        }

        [Test]
        public void 같은_스테이지는_스텝별로_같은_결과를_낸다()
        {
            var first = new List<ulong>();
            SimRunner.RunTraced(TestLevels.FragBombPitStage(), null, first, Steps);

            var second = new List<ulong>();
            SimRunner.RunTraced(TestLevels.FragBombPitStage(), null, second, Steps);

            CollectionAssert.AreEqual(first, second,
                "같은 스테이지를 두 번 돌렸는데 궤적이 갈렸다 — 파편 생성이 결정론을 깨고 있다.");
        }

        [Test]
        public void 시드가_다르면_파편_궤적이_갈린다()
        {
            // 이 테스트가 없으면 "시드를 무시하는 파편"이 나머지를 전부 통과한다.
            int[] seeds = { 1, 2, 3, 4 };

            var baseline = Trace(seeds[0]);
            for (int i = 1; i < seeds.Length; i++)
                if (!Same(baseline, Trace(seeds[i]))) return;   // 하나라도 갈리면 시드는 살아 있다

            Assert.Fail($"시드 {string.Join(", ", seeds)} 가 전부 같은 궤적을 냈다 — " +
                        "파편 방향이 rng 를 소비하지 않는다.");
        }

        static List<ulong> Trace(int seed)
        {
            var trace = new List<ulong>();
            SimRunner.RunTraced(TestLevels.FragBombPitStage(seed), null, trace, Steps);
            return trace;
        }

        static bool Same(List<ulong> a, List<ulong> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        static int LiveHazards(SimWorld world)
        {
            int count = 0;
            for (int i = 0; i < world.Hazards.Count; i++)
                if (world.Hazards[i] != null) count++;
            return count;
        }
    }
}
