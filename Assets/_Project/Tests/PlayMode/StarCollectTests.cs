using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 별이 시뮬 안에서 세어지는가.
    /// 코어가 세지 않으면 솔버가 "별 3개 클리어"
    /// 가능한 맵인지 판정할 수 없다.
    /// </summary>
    public class StarCollectTests
    {
        [Test]
        public void 경로_위의_별을_모은다()
        {
            var level = TestLevels.RampToGoal();

            // 내리막 선분 위. 굴러가며 반드시 지난다.
            level.Stars.Add(new Vector2(0f, 1f));

            var result = SimRunner.Run(level, null, 0);

            Assert.AreEqual(SimOutcome.Clear, result.Outcome);
            Assert.AreEqual(1, result.Stars);
        }

        [Test]
        public void 경로_밖의_별은_안_모은다()
        {
            var level = TestLevels.RampToGoal();
            level.Stars.Add(new Vector2(0f, 20f));

            var result = SimRunner.Run(level, null, 0);

            Assert.AreEqual(0, result.Stars);
        }

        [Test]
        public void 같은_별을_두_번_세지_않는다()
        {
            var level = TestLevels.FlatRest();

            // 공이 멈추는 자리. 여러 스텝 동안 닿아 있다.
            level.Stars.Add(level.BallStart);

            var result = SimRunner.Run(level, null, 0);

            Assert.AreEqual(1, result.Stars,
                "닿아 있는 동안 매 스텝 세면 개수가 스텝 수만큼 늘어난다.");
        }

        [Test]
        public void 별은_공의_궤적을_바꾸지_않는다()
        {
            // 별에 콜라이더가 생기면 솔버가 검증한 답이
            // 게임에서 다르게 굴러간다.
            var plain = SimRunner.Run(TestLevels.RampToGoal(), null, 0);

            var starred = TestLevels.RampToGoal();
            starred.Stars.Add(new Vector2(0f, 1f));
            var withStar = SimRunner.Run(starred, null, 0);

            Assert.AreEqual(plain.EndStep, withStar.EndStep);
            Assert.AreEqual(plain.MinGoalDist, withStar.MinGoalDist, 1e-6f);
        }
    }
}
