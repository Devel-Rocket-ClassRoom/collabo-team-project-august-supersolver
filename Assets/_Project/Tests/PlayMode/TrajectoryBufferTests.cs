using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 궤적 버퍼의 최소 보장.
    /// 간격대로 쌓이는가, 시간 순인가,
    /// 재사용해도 깨끗한가.
    /// </summary>
    public class TrajectoryBufferTests
    {
        const int Steps = SimWorld.DefaultMaxSteps;   // 1800

        static readonly int[] Intervals = { 10, 60, 300 };

        [Test]
        public void 스냅샷_수는_스텝수를_간격으로_나눈_값이다()
        {
            foreach (int interval in Intervals)
            {
                var buffer = new TrajectoryBuffer(interval, Steps);

                var result = SimRunner.RunSampled(NeverEnding(), null, 0, buffer, Steps);

                Assert.AreEqual(SimOutcome.Timeout, result.Outcome, "끝까지 돌아야 세는 의미가 있다");
                Assert.AreEqual(Steps / interval, buffer.Count, interval.ToString());
            }
        }

        [Test]
        public void 스냅샷이_스텝_오름차순으로_들어간다()
        {
            var buffer = new TrajectoryBuffer(60, Steps);

            SimRunner.RunSampled(NeverEnding(), null, 0, buffer, Steps);

            for (int i = 0; i < buffer.Count; i++)
            {
                Assert.AreEqual((i + 1) * buffer.Interval, buffer[i].Step);
                if (i > 0) Assert.Greater(buffer[i].Step, buffer[i - 1].Step);
            }
        }

        [Test]
        public void 버퍼를_재사용해도_이전_시행_값이_남지_않는다()
        {
            var buffer = new TrajectoryBuffer(10, Steps);

            SimRunner.RunSampled(NeverEnding(), null, 0, buffer, Steps);
            Assert.AreEqual(Steps / 10, buffer.Count);

            SimRunner.RunSampled(NeverEnding(), null, 0, buffer, 100);

            Assert.AreEqual(10, buffer.Count);
            for (int i = 0; i < buffer.Count; i++)
                Assert.LessOrEqual(buffer[i].Step, 100, "지난 시행의 뒷부분이 남아 있다");

            Assert.Throws<System.ArgumentOutOfRangeException>(() => { var _ = buffer[buffer.Count]; });
        }

        [Test]
        public void 위치와_속도가_실제로_담긴다()
        {
            var buffer = new TrajectoryBuffer(10, Steps);

            SimRunner.RunSampled(NeverEnding(), null, 0, buffer, 100);

            // 자유낙하라 아래로 가속한다.
            Assert.Less(buffer[0].Position.y, 0f);
            Assert.Less(buffer[buffer.Count - 1].Position.y, buffer[0].Position.y);
            Assert.Less(buffer[buffer.Count - 1].Velocity.y, buffer[0].Velocity.y);
        }

        /// <summary>
        /// 끝나지 않는 판. 받칠 지형도 목표도 닿지 않고
        /// KillY 는 30초 낙하 거리보다 훨씬 아래다.
        /// 스텝 상한까지 가야 샘플 수를 셀 수 있다.
        /// </summary>
        static LevelData NeverEnding()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = Vector2.zero,
                BallRadius = 0.25f,
                GoalPosition = new Vector2(1000f, 1000f),
                GoalRadius = 0.5f,
                KillY = -100000f,
                Terrain = new List<StaticSegment>(),
            };
        }
    }
}
