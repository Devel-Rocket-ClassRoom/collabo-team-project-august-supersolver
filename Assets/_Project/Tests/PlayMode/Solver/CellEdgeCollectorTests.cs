using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 시뮬 1회의 궤적에서 간선을 모으는 경로 검증.
    /// 간선은 전부 시작 셀에서 뻗어야 한다 —
    /// 한 배치로 닿은 곳이라는 뜻이 거기서 나온다.
    /// </summary>
    public class CellEdgeCollectorTests
    {
        const int Steps = 300;

        const int Interval = 10;

        [Test]
        public void 시뮬_1회에서_간선이_2개_이상_모인다()
        {
            var collector = Collect(out _);

            Assert.GreaterOrEqual(collector.Edges.Count, 2);
        }

        [Test]
        public void 모인_간선의_시작_셀이_전부_같다()
        {
            var collector = Collect(out BallCell start);

            foreach (var edge in collector.Edges)
                Assert.AreEqual(start, edge.From, $"{edge}");
        }

        [Test]
        public void 같은_궤적을_두_번_넣어도_간선이_늘지_않는다()
        {
            var collector = Collect(out _);
            int once = collector.Edges.Count;

            collector.Collect(RunTrajectory());

            Assert.AreEqual(once, collector.Edges.Count);
        }

        /// <summary>궤적 1회를 모으고, 그 시작 셀을 함께 준다.</summary>
        static CellEdgeCollector Collect(out BallCell start)
        {
            var quantizer = NewQuantizer();
            var buffer = RunTrajectory();

            Assert.Greater(buffer.Count, 0, "궤적이 비어 있으면 검증이 무의미하다");
            start = quantizer.Quantize(buffer[0].Position, buffer[0].Velocity);

            var collector = new CellEdgeCollector(quantizer);
            collector.Collect(buffer);
            return collector;
        }

        /// 공이 발판을 타고 굴러가는 판. TrialSamplingTests 와 같다.
        static TrajectoryBuffer RunTrajectory()
        {
            var level = Ground();
            var buffer = new TrajectoryBuffer(Interval, Steps);

            new PrimitiveTrial(level, 0)
                .RunSampled(new PrimitiveCodec(level).Encode(Placement()), buffer, Steps);

            return buffer;
        }

        static BallQuantizer NewQuantizer() => new BallQuantizer(0.5f, 2f);

        static Primitive[] Placement() => new[]
        {
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(-4f, 2f), -0.3f, 2f),
            new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(2f, 4f), 0.2f, 1f),
        };

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
