using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 네 가지 Outcome 이 실제로 구분되어 나오는지, 그리고 MinGoalDist 가
    /// 실패한 시도에서도 채워지는지 확인한다. 후자가 없으면 CEM 은 탐색 초반에
    /// 아무 신호 없이 무작위로 헤매게 된다.
    /// </summary>
    public class JudgeTests
    {
        [Test]
        public void 목표에_닿으면_Clear()
        {
            var result = SimRunner.Run(TestLevels.RampToGoal(), null, 0);

            Assert.AreEqual(SimOutcome.Clear, result.Outcome);
            Assert.Greater(result.EndStep, 0);
            Assert.Less(result.EndStep, SimWorld.DefaultMaxSteps);
        }

        [Test]
        public void 낙사하면_Fail()
        {
            var result = SimRunner.Run(TestLevels.FreeFall(), null, 0);

            Assert.AreEqual(SimOutcome.Fail, result.Outcome);
        }

        [Test]
        public void 전부_잠들면_Stalled()
        {
            var result = SimRunner.Run(TestLevels.FlatRest(), null, 0);

            Assert.AreEqual(SimOutcome.Stalled, result.Outcome);
            Assert.Less(result.EndStep, SimWorld.DefaultMaxSteps,
                "Stalled 를 잡지 못하면 실패 시도마다 30초를 끝까지 돌게 된다.");
        }

        [Test]
        public void 상한에_걸리면_Timeout()
        {
            var result = SimRunner.Run(TestLevels.LongRoll(), null, 0, maxSteps: 60);

            Assert.AreEqual(SimOutcome.Timeout, result.Outcome);
            Assert.AreEqual(60, result.EndStep);
        }

        [Test]
        public void MinGoalDist는_모든_Outcome에서_채워진다()
        {
            var clear = SimRunner.Run(TestLevels.RampToGoal(), null, 0);
            var fail = SimRunner.Run(TestLevels.FreeFall(), null, 0);
            var stalled = SimRunner.Run(TestLevels.FlatRest(), null, 0);
            var timeout = SimRunner.Run(TestLevels.LongRoll(), null, 0, maxSteps: 60);

            foreach (var result in new[] { clear, fail, stalled, timeout })
            {
                Assert.IsFalse(float.IsInfinity(result.MinGoalDist), $"{result.Outcome} 에서 미계측");
                Assert.IsFalse(float.IsNaN(result.MinGoalDist), $"{result.Outcome} 에서 NaN");
                Assert.GreaterOrEqual(result.MinGoalDist, 0f);
            }

            Assert.Less(clear.MinGoalDist, TestLevels.RampToGoal().GoalRadius + 0.3f,
                "클리어했는데 최소 거리가 목표 반경보다 크다 — 계측 시점이 어긋나 있다.");
        }

        [Test]
        public void 잉크_사용량이_결과에_실린다()
        {
            var solution = TestLevels.BridgeSolution();
            var result = SimRunner.Run(TestLevels.Gap(), solution, 0, 600);

            Assert.AreEqual(2.4f, result.InkUsed, 0.001f, "다리 길이 2.4 가 그대로 실려야 한다.");
        }

        [Test]
        public void 판정은_한_번_확정되면_뒤집히지_않는다()
        {
            using (var world = WorldBuilder.Build(TestLevels.FreeFall(), null, 0))
            {
                while (!world.IsTerminal && world.CurrentStep < 600) world.Step();

                Assert.IsTrue(world.Judge.Failed);
                int decided = world.Judge.DecidedStep;

                for (int i = 0; i < 60; i++) world.Step();

                Assert.IsTrue(world.Judge.Failed);
                Assert.IsFalse(world.Judge.Cleared);
                Assert.AreEqual(decided, world.Judge.DecidedStep);
            }
        }
    }
}
