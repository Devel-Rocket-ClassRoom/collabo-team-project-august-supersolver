using System;
using System.Collections.Generic;

namespace PPS.Solver
{
    /// <summary>
    /// 셀마다 "목표까지 선을 최소 몇 개 더 그어야 하는가" 를 담은 표.
    /// 탐색이 다음에 파볼 곳을 고르는 데만 쓴다 —
    /// 양자화 오차가 홉마다 쌓이고, 간선이 표본에서 나오고,
    /// 이미 놓인 선을 반영하지 않아 과대평가를 막지 못한다.
    /// 순서를 정하는 데는 써도 가지를 자르는 데는 절대 쓰면 안 된다.
    /// 어기면 풀리는 레벨을 못 푼다고 판정하게 된다.
    /// </summary>
    public sealed class HeuristicMap
    {
        readonly Dictionary<BallCell, int> _cost;

        HeuristicMap(Dictionary<BallCell, int> cost, int furthest)
        {
            _cost = cost;
            Furthest = furthest;
            Penalty = furthest + 1;
        }

        /// 아는 셀 중 가장 먼 값.
        public int Furthest { get; }

        /// <summary>
        /// 맵에 없는 셀에 줄 값. 아는 것 중 가장 먼 셀보다 딱 하나 더 멀다.
        /// 여기에 큰 수나 무한대를 넣으면 그 셀들이 영영 안 뽑혀
        /// 벌점이 아니라 사실상 가지치기가 된다.
        /// 모르는 셀끼리는 같은 값이라 아는 척을 하지 않는다.
        /// 이기는 셀을 하나도 못 찾은 맵에서는 모든 셀이 같은 값이 되는데,
        /// 그건 h 가 없는 것과 같아 탐색이 h = 0 일 때로 돌아간다.
        /// </summary>
        public int Penalty { get; }

        /// 값을 아는 셀 수.
        public int Count => _cost.Count;

        public bool Knows(BallCell cell) => _cost.ContainsKey(cell);

        /// 모르는 셀은 벌점을 낸다. 부르는 쪽이 없는 경우를 챙기지 않게 한다.
        public int Of(BallCell cell) => _cost.TryGetValue(cell, out int h) ? h : Penalty;

        /// <summary>
        /// 목표 셀에서 거꾸로 0-1 BFS 를 돌려 h 를 채운다.
        /// 간선은 앞으로만 만들어지므로(c 에서 굴려 봐야 c 가 어디로 가는지 안다)
        /// 역방향 인접을 한 번 뒤집어 두고 그 위를 훑는다.
        /// 물리를 안 건드리므로 코루틴이 아니다.
        /// </summary>
        /// <param name="edges">간선과 비용. 선을 놓았으면 1, 중력만이면 0.</param>
        /// <param name="seeds">h 를 시뮬 판정으로 이미 아는 셀과 그 값.
        /// 이기는 셀들이며, 나머지 h 는 전부 여기서 뒤로 퍼져 나온다.
        /// 비어 있으면 퍼뜨릴 곳이 없어 맵 전체가 미지로 남는다.</param>
        public static HeuristicMap Build(
            IReadOnlyDictionary<CellEdge, int> edges, IReadOnlyDictionary<BallCell, int> seeds)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));

            Dictionary<BallCell, List<Incoming>> incoming = Reverse(edges);

            var cost = new Dictionary<BallCell, int>();
            var queue = new LinkedList<Step>();

            // 출발 셀을 가까운 순서로 넣어야 한다. 덱은 값이 d 와 d+1 두 가지만
            // 순서대로 들어 있다는 전제로 도는데, 섞어 넣으면 그게 깨진다.
            var ordered = new List<KeyValuePair<BallCell, int>>(seeds);
            ordered.Sort((a, b) => a.Value.CompareTo(b.Value));

            foreach (var seed in ordered)
            {
                if (cost.ContainsKey(seed.Key)) continue;

                cost[seed.Key] = seed.Value;
                queue.AddLast(new Step(seed.Key, seed.Value));
            }

            while (queue.Count > 0)
            {
                Step step = queue.First.Value;
                queue.RemoveFirst();

                // 더 짧은 길이 나중에 발견돼 이 항목이 낡았을 수 있다.
                if (step.Cost > cost[step.Cell]) continue;

                if (!incoming.TryGetValue(step.Cell, out var sources)) continue;

                foreach (Incoming source in sources)
                {
                    int next = step.Cost + source.Cost;
                    if (cost.TryGetValue(source.Cell, out int known) && known <= next) continue;

                    cost[source.Cell] = next;

                    // 잉크를 안 쓰는 이동은 거리를 안 올리므로 앞으로 넣는다.
                    // 이 한 줄이 0-1 BFS 를 다익스트라 대신 쓸 수 있게 한다.
                    var entry = new Step(source.Cell, next);
                    if (source.Cost == 0) queue.AddFirst(entry);
                    else queue.AddLast(entry);
                }
            }

            // 완화 도중에 세면 나중에 짧아진 셀의 낡은 값이 남는다 —
            // 그러면 벌점이 간선 순회 순서를 타서 실행마다 갈린다.
            int furthest = 0;
            foreach (int h in cost.Values)
                if (h > furthest) furthest = h;

            return new HeuristicMap(cost, furthest);
        }

        /// <summary>
        /// 도착 셀 → 거기로 들어오는 (출발 셀, 비용) 목록.
        /// h 는 목표에서 뒤로 퍼지는데 간선은 앞을 보고 있어
        /// 한 번 뒤집어야 훑을 수 있다.
        /// </summary>
        static Dictionary<BallCell, List<Incoming>> Reverse(
            IReadOnlyDictionary<CellEdge, int> edges)
        {
            var reversed = new Dictionary<BallCell, List<Incoming>>();

            foreach (var pair in edges)
            {
                if (!reversed.TryGetValue(pair.Key.To, out var sources))
                {
                    sources = new List<Incoming>();
                    reversed[pair.Key.To] = sources;
                }

                sources.Add(new Incoming(pair.Key.From, pair.Value));
            }

            return reversed;
        }

        readonly struct Incoming
        {
            public readonly BallCell Cell;
            public readonly int Cost;

            public Incoming(BallCell cell, int cost)
            {
                Cell = cell;
                Cost = cost;
            }
        }

        /// 덱에 담기는 항목. 넣을 때의 거리를 같이 들고 있어야
        /// 낡은 항목을 팝에서 걸러낼 수 있다.
        readonly struct Step
        {
            public readonly BallCell Cell;
            public readonly int Cost;

            public Step(BallCell cell, int cost)
            {
                Cell = cell;
                Cost = cost;
            }
        }
    }
}
