using System.Collections.Generic;
using NUnit.Framework;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 역방향 0-1 BFS 의 계약.
    /// 손으로 짠 작은 그래프로 본다 — 실제 맵에서는 값이 맞는지
    /// 눈으로 확인할 방법이 없어 규칙이 깨져도 안 보인다.
    /// </summary>
    public class HeuristicMapTests
    {
        /// 셀의 좌표는 뜻이 없다. 서로 다르기만 하면 된다.
        static BallCell Cell(int x) => new BallCell(x, 0, 0, 1);

        static readonly BallCell Goal = Cell(0);
        static readonly BallCell Near = Cell(1);
        static readonly BallCell Free = Cell(2);
        static readonly BallCell Far = Cell(3);
        static readonly BallCell Lone = Cell(4);

        /// <summary>
        /// Free →(0)→ Near →(1)→ Goal.
        /// 중력만으로 되는 이동은 잉크를 안 쓰므로 h 를 올리면 안 된다.
        /// </summary>
        static HeuristicMap Sample()
        {
            var edges = new Dictionary<CellEdge, int>
            {
                { new CellEdge(Near, Goal), 1 },
                { new CellEdge(Free, Near), 0 },
                { new CellEdge(Far, Near), 1 },
            };

            return HeuristicMap.Build(edges, Seed(Goal, 0));
        }

        static Dictionary<BallCell, int> Seed(BallCell cell, int lines)
            => new Dictionary<BallCell, int> { { cell, lines } };

        [Test]
        public void 이기는_셀은_0이다()
        {
            Assert.AreEqual(0, Sample().Of(Goal));
        }

        [Test]
        public void 선을_놓아야_닿는_셀은_1이_붙는다()
        {
            Assert.AreEqual(1, Sample().Of(Near));
        }

        [Test]
        public void 중력만으로_되는_이동은_h를_안_올린다()
        {
            // Free 는 Near 보다 한 홉 멀지만 그 홉이 비용 0 이다.
            Assert.AreEqual(1, Sample().Of(Free), "비용 0 간선을 1 로 셌다");
        }

        [Test]
        public void 선_둘이_필요한_셀은_2다()
        {
            Assert.AreEqual(2, Sample().Of(Far));
        }

        /// <summary>
        /// 맵 밖은 컷이 아니라 벌점이다.
        /// 무한대를 넣으면 그 셀들이 영영 안 뽑혀 사실상 가지치기가 되고,
        /// 그러면 풀리는 레벨을 못 푼다고 판정하게 된다.
        /// </summary>
        [Test]
        public void 모르는_셀은_가장_먼_값보다_하나_더_먼_값을_받는다()
        {
            HeuristicMap map = Sample();

            Assert.AreEqual(2, map.Furthest);
            Assert.AreEqual(3, map.Penalty);
            Assert.AreEqual(3, map.Of(Lone));
            Assert.IsFalse(map.Knows(Lone));
        }

        /// <summary>
        /// 같은 셀에 짧은 길과 긴 길이 모두 있으면 짧은 쪽이 남아야 한다.
        /// 낡은 항목을 덱에서 안 걸러내면 나중에 팝된 긴 값이 덮어쓴다.
        /// </summary>
        [Test]
        public void 나중에_찾은_긴_길이_짧은_값을_덮지_않는다()
        {
            var edges = new Dictionary<CellEdge, int>
            {
                { new CellEdge(Near, Goal), 1 },
                { new CellEdge(Far, Near), 1 },

                // Far 로 가는 지름길. 위 경로(2)보다 짧은 1 이다.
                { new CellEdge(Far, Goal), 1 },
            };

            Assert.AreEqual(1, HeuristicMap.Build(edges, Seed(Goal, 0)).Of(Far));
        }

        /// <summary>
        /// 선 하나로 이긴 셀은 h 를 1 로 이미 아는 셀이다.
        /// 이걸 못 쓰면 기저가 이미 이기는 판에서만 h 가 생긴다 —
        /// 즉 정작 풀 것이 있는 판에서 맵이 통째로 비어 버린다.
        /// </summary>
        [Test]
        public void 선_하나로_이긴_셀에서도_뒤로_퍼진다()
        {
            var edges = new Dictionary<CellEdge, int>
            {
                { new CellEdge(Far, Near), 1 },
                { new CellEdge(Free, Near), 0 },
            };

            HeuristicMap map = HeuristicMap.Build(edges, Seed(Near, 1));

            Assert.AreEqual(1, map.Of(Near), "이미 아는 값이 그대로 남아야 한다");
            Assert.AreEqual(1, map.Of(Free), "비용 0 간선은 h 를 안 올린다");
            Assert.AreEqual(2, map.Of(Far));
        }

        /// <summary>
        /// 이기는 셀이 없으면 h 가 아무 말도 못 한다.
        /// 그때는 모든 셀이 같은 값이라 탐색이 h = 0 일 때로 돌아간다 —
        /// 조용히 이상한 순서를 내는 것보다 낫다.
        /// </summary>
        [Test]
        public void 이기는_셀이_없으면_모두_같은_값이다()
        {
            var edges = new Dictionary<CellEdge, int>
            {
                { new CellEdge(Near, Goal), 1 },
            };

            HeuristicMap map = HeuristicMap.Build(edges, new Dictionary<BallCell, int>());

            Assert.Zero(map.Count);
            Assert.AreEqual(map.Of(Near), map.Of(Goal));
        }
    }
}
