using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 레벨 하나를 풀어 보는 최선우선 탐색.
    /// 순위는 g + h 다. 둘 다 "선 개수" 라 더할 수 있고,
    /// 같은 값이면 잉크가 적은 쪽을 고른다.
    /// 맵이 없어도 돈다 — 그때는 1축이 통째로 0 이라
    /// 잉크만 보는 탐색이 되고, 그게 D0 기준선이다.
    /// 물리 씬을 걷으려면 프레임을 넘겨야 해 코루틴이다.
    /// </summary>
    public sealed class SolverSearch
    {
        readonly LevelData _level;
        readonly PrimitiveTrial _trial;
        readonly PrimitiveCandidates _candidates;
        readonly PrimitiveCodec _codec;
        readonly BallQuantizer _quantizer;
        readonly TrajectoryBuffer _buffer;
        readonly Rect _area;

        /// null 이면 h = 0 이다. 순위 1축이 통째로 0 이 되어 잉크만 남는다.
        readonly HeuristicMap _map;

        readonly int _simBudget;
        readonly double _timeBudget;

        readonly Heap _open = new Heap();
        readonly Stopwatch _watch = new Stopwatch();
        readonly SimScenes _scenes = new SimScenes();

        /// <summary>
        /// 제자리를 맴도는 판을 끊는 기준.
        /// 위치 셀 하나를 IdleSteps 동안 못 벗어난 공은 목표에 못 간다.
        /// 안 걸면 그런 판이 상한 1800 스텝까지 도는데, h 를 꽂은 뒤로는
        /// 공을 받아 살려 두는 배치가 뽑혀서 그게 시뮬 단가를 지배했다.
        /// Stalled 가 Timeout 으로 바뀌지만 탐색은 Cleared 만 본다.
        /// </summary>
        readonly IdleCutoff _idle;

        /// <summary>
        /// 궤적 캐시. 지금 후보를 펼쳐 둔 노드다.
        /// 한 칸뿐인 이유는 궤적이 노드당 메모리가 되면 안 되기 때문이다 —
        /// 빗나가면 그 노드를 다시 굴린다. 그 값은 Sims - Nodes 로 보인다.
        /// </summary>
        SearchNode _expanded;

        readonly List<Primitive> _pool = new List<Primitive>();

        /// _pool 과 짝인 잉크 값. 비교마다 다시 재면
        /// 정렬 비용이 시뮬 한 판과 맞먹는다.
        readonly List<float> _inks = new List<float>();

        /// <summary>
        /// _pool 과 짝인, 그 후보를 놓는 자리의 h.
        /// 자식의 순위를 굴려 보지 않고 매길 수 있는 유일한 값이다 —
        /// 셀 c 에 선 하나를 놓은 자식은 (g+1) + (h(c)-1) = g + h(c) 다.
        /// 맵이 없으면 전부 0 이라 정렬이 잉크로 떨어진다.
        /// </summary>
        readonly List<int> _siteH = new List<int>();

        /// <summary>
        /// _pool 을 훑는 순서. h 가 작은 자리부터, 같으면 잉크가 싼 것부터다.
        /// 부모가 싼 자식부터 내야 열린 목록이 순위를 지킨다.
        /// List.Sort 는 안정적이지 않아 번호로 한 번 더 가른다.
        /// </summary>
        readonly List<int> _order = new List<int>();

        /// 궤적에서 후보를 놓을 자리를 추릴 때 쓴다.
        readonly HashSet<BallCell> _sites = new HashSet<BallCell>();

        int _seq;

        public SolverReport Report { get; private set; }

        /// <param name="map">셀별 h. null 이면 h = 0 으로 돌아 D0 기준선이 된다.</param>
        /// <param name="simBudget">돌릴 시뮬 횟수 상한.</param>
        /// <param name="timeBudget">쓸 시간 상한(초).</param>
        public SolverSearch(
            LevelData level, HeuristicMap map = null, int seed = 0,
            int simBudget = SolverConfig.SearchSimBudget,
            double timeBudget = SolverConfig.SearchSeconds)
        {
            _map = map;
            _level = level;
            _trial = new PrimitiveTrial(level, seed);
            _candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
            _codec = new PrimitiveCodec(level);
            _quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            _buffer = new TrajectoryBuffer(SolverConfig.TrajectoryInterval);
            _area = LevelDataArea.Calculate(level);
            _idle = new IdleCutoff(_quantizer.PositionStep, SolverConfig.IdleSteps);

            _simBudget = simBudget;
            _timeBudget = timeBudget;
        }

        /// <summary>
        /// 다 돌면 Report 에 결과가 들어 있다.
        /// 출발 노드는 아무것도 안 놓은 빈 배치다 — 그냥 굴려도 이기는 판은
        /// 첫 팝에서 끝나고 빈 Solution 이 나간다.
        /// </summary>
        public IEnumerator Run()
        {
            Report = new SolverReport();

            // 앞 단계가 남긴 언로드 대기분이 빠진 뒤에 기준선을 잡는다.
            // 맵을 짓고 바로 돌리면 빌드가 남긴 씬을 통째로 물려받는다.
            var settle = _scenes.Settle();
            while (settle.MoveNext()) yield return settle.Current;

            Push(new SearchNode(new Primitive[0], 0f, _seq++, estimate: 0));

            while (_open.Count > 0)
            {
                SearchNode node = _open.Pop();
                bool rolled = node != _expanded;

                if (rolled)
                {
                    // 레벨 시작점에서 전체 배치로 굴린다. 중간 상태에서
                    // 새 선만 놓고 굴리면(맵 빌더가 그렇게 한다) 반환한 풀이를
                    // 재실행했을 때 같은 결과가 난다는 보장이 없다.
                    TrialResult result = Roll(node.Primitives);

                    if (!node.Rolled)
                    {
                        node.Rolled = true;
                        Report.Nodes++;
                        Report.Deepest = Mathf.Max(Report.Deepest, node.Depth);
                    }

                    if (result.Cleared)
                    {
                        Report.Stop = SolverStop.Solved;
                        Report.Solution = Rebuild(node.Primitives);
                        break;
                    }

                    Spread(node);

                    var drain = _scenes.Drain();
                    while (drain.MoveNext()) yield return drain.Current;
                }

                // 궤적이 손에 있는 동안 낼 수 있는 만큼 낸다.
                // 하나만 내고 물러나면 곧 캐시에서 밀려나, 다음 자식을 내려고
                // 같은 배치를 또 굴린다 — 첫 측정에서 시뮬의 절반이 그 재굴림이었다.
                // 열린 목록의 최소보다 비싼 자식에서 멈추므로 잉크 순서는 그대로다.
                // 굴린 값은 적어도 자식 하나로 회수한다.
                bool owed = rolled;
                while (Advance(node) && (owed || !Costlier(node)))
                {
                    owed = false;
                    int index = _order[node.Cursor++];
                    Push(Child(node, _pool[index], index));
                }

                // 낼 것이 더 있으면 다음 자식의 f 를 달고 다시 줄에 선다.
                if (node.Cursor < _order.Count) Push(node);

                if (OverBudget()) break;
            }

            Report.Elapsed = _watch.Elapsed;
            Report.PeakScenes = _scenes.Peak;
            Report.PeakTotalScenes = _scenes.PeakTotal;
        }

        /// <summary>
        /// 이 배치를 굴려 궤적을 _buffer 에 받는다.
        /// 스텝 상한은 맵 빌드보다 넉넉한 기본값을 쓴다 —
        /// 늦게 이기는 풀이를 길이로 잘라 버리면 안 된다.
        /// 대신 제자리를 맴도는 판만 정체 판정으로 끊는다.
        /// </summary>
        TrialResult Roll(Primitive[] primitives)
        {
            _watch.Start();
            TrialResult result = _trial.RunSampled(
                _codec.Encode(primitives), _buffer, SimWorld.DefaultMaxSteps, null, _idle);
            _watch.Stop();

            Report.Sims++;
            Report.Steps += result.Sim.EndStep;
            Report.MinGoalDist = Mathf.Min(Report.MinGoalDist, result.Sim.MinGoalDist);
            return result;
        }

        /// <summary>
        /// 이 노드가 낼 후보를 전부 펼쳐 순위대로 세운다.
        /// 놓는 자리는 궤적을 셀로 접은 서로 다른 지점들이다 —
        /// 맵도 셀 대표 상태에 놓으므로 둘이 같은 자리를 본다.
        /// 표본을 그대로 쓰면 같은 셀에 열 번씩 놓게 된다.
        /// </summary>
        void Spread(SearchNode node)
        {
            _expanded = node;
            _pool.Clear();
            _inks.Clear();
            _siteH.Clear();
            _order.Clear();
            _sites.Clear();

            for (int i = 0; i < _buffer.Count; i++)
            {
                BallSample sample = _buffer[i];
                BallCell cell = _quantizer.Quantize(sample.Position, sample.Velocity);
                if (!_sites.Add(cell)) continue;

                int h = _map == null ? 0 : _map.Of(cell);
                int before = _pool.Count;
                _pool.AddRange(_candidates.At(new BallState(sample.Position, sample.Velocity)));

                for (int p = before; p < _pool.Count; p++)
                {
                    _inks.Add(PrimitiveValidator.Ink(_pool[p]));
                    _siteH.Add(h);
                    _order.Add(p);
                }
            }

            _order.Sort(CompareCandidates);
        }

        /// <summary>
        /// 목표에 가까운 자리부터, 같으면 싼 것부터.
        /// 노드 하나가 수만 개의 자식을 내는데 그중 무엇을 먼저 볼지
        /// 고르는 근거가 이것뿐이다 — 맵이 값을 한다면 여기서 한다.
        /// 잉크까지 같으면 만든 순서를 따른다. 실행마다 같아야 한다.
        /// </summary>
        int CompareCandidates(int a, int b)
        {
            int byH = _siteH[a].CompareTo(_siteH[b]);
            if (byH != 0) return byH;

            int byInk = _inks[a].CompareTo(_inks[b]);
            return byInk != 0 ? byInk : a.CompareTo(b);
        }

        /// <summary>
        /// 놓을 수 있는 다음 후보까지 커서를 밀고 Key 를 갱신한다.
        /// 잉크나 영역에 걸리는 후보는 시뮬 없이 여기서 버려진다.
        /// 잉크 한도가 이 검사에 걸리므로 깊이 상한을 따로 두지 않는다.
        /// </summary>
        bool Advance(SearchNode node)
        {
            while (node.Cursor < _order.Count)
            {
                Primitive candidate = _pool[_order[node.Cursor]];

                if (PrimitiveValidator.Validate(candidate, _level, node.Ink, _area)
                    == PlacementReject.None)
                {
                    node.KeyLines = Estimate(node, _order[node.Cursor]);
                    node.KeyInk = node.Ink + _inks[_order[node.Cursor]];
                    return true;
                }

                Report.Rejected++;
                node.Cursor++;
            }

            return false;
        }

        /// <summary>
        /// 자리 c 에 선 하나를 더 놓은 자식이 최종적으로 쓸 선 개수 추정치.
        /// (g+1) + (h(c)-1) 이라 g + h(c) 로 떨어진다 — 자식을 굴려 보지 않고
        /// 순위를 매길 수 있는 이유가 이 상쇄다.
        /// 맵이 없으면 1축을 아예 안 쓴다. 깊이를 넣으면 얕은 쪽이
        /// 먼저 나와 D0 기준선과 다른 탐색이 된다.
        /// </summary>
        int Estimate(SearchNode parent, int poolIndex)
            => _map == null ? 0 : parent.Depth + _siteH[poolIndex];

        /// <summary>
        /// 이 노드의 다음 자식이 열린 목록의 최소보다 비싼가.
        /// 비싸지면 물러나야 순위가 지켜진다.
        /// 깊이·번호 동점 규칙은 보지 않는다 — 그것까지 보면
        /// 동점에서 바로 물러나 궤적 캐시가 매번 빗나간다.
        /// </summary>
        bool Costlier(SearchNode node)
            => _open.Count > 0 && Heap.CompareKeys(node, _open.Peek()) > 0;

        SearchNode Child(SearchNode parent, in Primitive candidate, int poolIndex)
        {
            var primitives = new Primitive[parent.Primitives.Length + 1];
            parent.Primitives.CopyTo(primitives, 0);
            primitives[parent.Primitives.Length] = candidate;

            return new SearchNode(
                primitives, parent.Ink + PrimitiveValidator.Ink(candidate), _seq++,
                Estimate(parent, poolIndex));
        }

        void Push(SearchNode node)
        {
            _open.Push(node);
            Report.OpenPeak = Mathf.Max(Report.OpenPeak, _open.Count);
        }

        /// <summary>
        /// 시뮬에 실제로 들어간 배치를 Solution 으로 되돌린다.
        /// 코덱을 한 번 왕복시키는 이유는, 굴린 것이 원본 후보가 아니라
        /// 인코드·디코드를 거친 값이기 때문이다. 여기서 원본을 쓰면
        /// 반환한 풀이와 Clear 를 받은 배치가 미세하게 달라진다.
        /// </summary>
        Solution Rebuild(Primitive[] primitives)
            => Rebuild(_codec, primitives);

        /// <summary>
        /// 코덱을 쥐고 있는 쪽에서 부르는 형태.
        /// 이 경로는 퍼즐이 풀려야만 도는데, 지금은 안 풀린다 —
        /// 그래서 테스트가 배치를 직접 넣어 여기만 따로 못박는다.
        /// </summary>
        public static Solution Rebuild(PrimitiveCodec codec, Primitive[] primitives)
            => PrimitiveDecoder.Decode(codec.Decode(codec.Encode(primitives)));

        /// <summary>
        /// 예산을 넘었으면 사유를 박고 참을 낸다.
        /// 잉크는 해답 하나당 예산이라 탐색을 멈추지 못한다 —
        /// 실제로 멈추는 것은 시뮬 횟수와 시간이다.
        /// </summary>
        bool OverBudget()
        {
            if (Report.Sims >= _simBudget)
            {
                Report.Stop = SolverStop.SimBudget;
                return true;
            }

            if (_watch.Elapsed.TotalSeconds >= _timeBudget)
            {
                Report.Stop = SolverStop.TimeBudget;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 순위가 앞선 노드부터 내는 이진 힙.
        /// .NET Standard 2.1 에는 우선순위 큐가 없어 직접 둔다.
        /// 완전히 동점이면 깊은 쪽이 먼저다 — 얕은 쪽을 먼저 내면
        /// 부모가 자식을 다 낳을 때까지 아무도 못 굴려
        /// 열린 목록이 한 노드의 후보 수만큼 부푼다.
        /// </summary>
        sealed class Heap
        {
            readonly List<SearchNode> _items = new List<SearchNode>();

            public int Count => _items.Count;

            public SearchNode Peek() => _items[0];

            public void Push(SearchNode node)
            {
                _items.Add(node);

                int child = _items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (!Precedes(_items[child], _items[parent])) break;

                    Swap(child, parent);
                    child = parent;
                }
            }

            public SearchNode Pop()
            {
                SearchNode top = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);

                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1;
                    if (left >= _items.Count) break;

                    int best = left;
                    int right = left + 1;
                    if (right < _items.Count && Precedes(_items[right], _items[left]))
                        best = right;

                    if (!Precedes(_items[best], _items[parent])) break;

                    Swap(best, parent);
                    parent = best;
                }

                return top;
            }

            /// <summary>
            /// 순위 두 축만 견준다. 1축은 선 개수(g + h), 2축은 잉크다.
            /// 더하지 않고 사전식으로 쌓는다 — 개수와 미터는 못 더한다.
            /// </summary>
            public static int CompareKeys(SearchNode a, SearchNode b)
            {
                int byLines = a.KeyLines.CompareTo(b.KeyLines);
                return byLines != 0 ? byLines : a.KeyInk.CompareTo(b.KeyInk);
            }

            static bool Precedes(SearchNode a, SearchNode b)
            {
                int byKey = CompareKeys(a, b);
                if (byKey != 0) return byKey < 0;
                if (a.Depth != b.Depth) return a.Depth > b.Depth;
                return a.Seq < b.Seq;
            }

            void Swap(int a, int b)
            {
                SearchNode t = _items[a];
                _items[a] = _items[b];
                _items[b] = t;
            }
        }
    }
}
