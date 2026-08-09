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
    /// 그 시작 셀은 궤적에 없다. 첫 표본이 Interval 스텝 뒤라
    /// 출발 상태는 버퍼에 안 담기기 때문이다.
    /// 비용은 프리미티브를 놓았는지로 갈린다.
    /// </summary>
    public class CellEdgeCollectorTests
    {
        const int Steps = 300;

        const int Interval = 10;

        [Test]
        public void 시뮬_1회에서_간선이_2개_이상_모인다()
        {
            var collector = CollectPlaced(out _);

            Assert.GreaterOrEqual(collector.Edges.Count, 2);
        }

        [Test]
        public void 모인_간선의_시작_셀이_전부_같다()
        {
            var collector = CollectPlaced(out BallCell start);

            foreach (var edge in collector.Edges.Keys)
                Assert.AreEqual(start, edge.From, $"{edge}");
        }

        [Test]
        public void 같은_궤적을_두_번_넣어도_간선이_늘지_않는다()
        {
            var collector = CollectPlaced(out _);
            int once = collector.Edges.Count;

            collector.CollectPlaced(StartCell(), PlacedTrajectory());

            Assert.AreEqual(once, collector.Edges.Count);
        }

        /// <summary>
        /// 첫 표본까지의 이동도 간선이어야 한다.
        /// 궤적에서 시작 셀을 읽으면 이 한 홉이 통째로 빠지고,
        /// 그러면 굴린 자리에서 나가는 간선이 하나도 안 생긴다 —
        /// 나가는 간선이 없는 셀은 h 를 못 받는다.
        /// </summary>
        [Test]
        public void 출발_셀에서_첫_표본으로_가는_간선이_생긴다()
        {
            var collector = CollectPlaced(out BallCell start);
            var buffer = PlacedTrajectory();

            BallCell first = NewQuantizer().Quantize(buffer[0].Position, buffer[0].Velocity);
            Assert.AreNotEqual(start, first, "첫 표본이 출발 셀과 같으면 이 검증이 성립하지 않는다");

            Assert.IsTrue(collector.Edges.ContainsKey(new CellEdge(start, first)),
                "출발 셀에서 첫 표본으로 가는 간선이 없다");
        }

        [Test]
        public void 배치_시뮬의_간선은_비용_1이다()
        {
            var collector = CollectPlaced(out _);

            foreach (var pair in collector.Edges)
                Assert.AreEqual(1, pair.Value, $"{pair.Key}");
        }

        [Test]
        public void 무배치_시뮬의_간선은_비용_0이다()
        {
            var collector = new CellEdgeCollector(NewQuantizer());
            collector.CollectFree(StartCell(), FreeTrajectory());

            Assert.GreaterOrEqual(collector.Edges.Count, 2);
            foreach (var pair in collector.Edges)
                Assert.AreEqual(0, pair.Value, $"{pair.Key}");
        }

        /// <summary>
        /// 겹치는 간선의 비용 규칙만 보는 시험이다.
        /// 같은 궤적을 두 비용으로 넣는 건 실제 상황이 아니지만,
        /// 겹침을 확실히 만들려면 이 방법이 가장 짧다.
        /// </summary>
        [Test]
        public void 같은_간선이_무배치로도_나오면_0이_남는다()
        {
            var collector = CollectPlaced(out _);

            collector.CollectFree(StartCell(), PlacedTrajectory());

            foreach (var pair in collector.Edges)
                Assert.AreEqual(0, pair.Value, $"비싼 쪽이 남았다: {pair.Key}");
        }

        /// <summary>배치 궤적 1회를 모으고, 그 시작 셀을 함께 준다.</summary>
        static CellEdgeCollector CollectPlaced(out BallCell start)
        {
            var buffer = PlacedTrajectory();
            Assert.Greater(buffer.Count, 0, "궤적이 비어 있으면 검증이 무의미하다");

            start = StartCell();

            var collector = new CellEdgeCollector(NewQuantizer());
            collector.CollectPlaced(start, buffer);
            return collector;
        }

        /// 공이 출발한 셀. 레벨의 시작점이고 속도는 0 이다.
        static BallCell StartCell()
            => NewQuantizer().Quantize(Ground().BallStart, Vector2.zero);

        /// 공이 발판을 타고 굴러가는 판. TrialSamplingTests 와 같다.
        static TrajectoryBuffer PlacedTrajectory() => Run(new PrimitiveCodec(Ground()).Encode(Placement()));

        /// 아무것도 놓지 않고 굴린 기저 궤적.
        static TrajectoryBuffer FreeTrajectory() => Run(new float[0]);

        static TrajectoryBuffer Run(float[] vector)
        {
            var buffer = new TrajectoryBuffer(Interval, Steps);
            new PrimitiveTrial(Ground(), 0).RunSampled(vector, buffer, Steps);
            return buffer;
        }

        static BallQuantizer NewQuantizer() => new BallQuantizer(0.5f);

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
