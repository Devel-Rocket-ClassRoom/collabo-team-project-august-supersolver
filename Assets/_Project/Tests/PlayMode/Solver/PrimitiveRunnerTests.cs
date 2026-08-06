using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 프리미티브 → SimResult 진입점 검증.
    /// 같은 입력이 같은 결과를 내는지,
    /// 빈 배치가 무배치와 같은지를 본다.
    /// </summary>
    public class PrimitiveRunnerTests
    {
        const int Steps = 300;

        [Test]
        public void 같은_프리미티브를_두_번_돌리면_결과가_같다()
        {
            var primitives = Placement();

            var a = PrimitiveRunner.Run(Ground(), primitives, 0, Steps);
            var b = PrimitiveRunner.Run(Ground(), primitives, 0, Steps);

            AssertSame(a, b);
        }

        [Test]
        public void 빈_배치는_무배치_시뮬과_같다()
        {
            var empty = PrimitiveRunner.Run(Ground(), new Primitive[0], 0, Steps);
            var none = SimRunner.Run(Ground(), Solution.Empty, 0, Steps);

            AssertSame(empty, none);
        }

        static void AssertSame(in SimResult a, in SimResult b)
        {
            Assert.AreEqual(a.Outcome, b.Outcome);
            Assert.AreEqual(a.EndStep, b.EndStep);
            Assert.AreEqual(a.Stars, b.Stars);
            Assert.AreEqual(a.InkUsed, b.InkUsed);
            Assert.AreEqual(a.MinGoalDist, b.MinGoalDist);
        }

        /// 공이 굴러가는 길목에 발판 둘을 놓는다.
        static Primitive[] Placement() => new[]
        {
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(-4f, 2f), -0.3f, 2f),
            new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(2f, 4f), 0.2f, 1f),
        };

        /// 평평한 바닥 하나.
        /// 공은 발판 한가운데(x=-5)로 떨어져
        /// 기울기를 타고 오른쪽으로 굴러간다.
        static LevelData Ground()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(-5f, 5f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(6f, 0.5f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-14f, 0f), new Vector2(14f, 0f)),
                },
            };
        }
    }
}
