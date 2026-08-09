using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PPS.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Solver
{
    /// <summary>
    /// 레벨 하나를 풀어 보는 최선우선 탐색.
    /// 맵이 없어도 돈다 — h = 0 이면 f = g 라
    /// 잉크가 적은 배치부터 훑는 탐색이 된다.
    /// 맵은 나중에 h 자리에 꽂고, 그때 줄어드는 시뮬 횟수가
    /// 맵이 값을 하는지에 대한 답이 된다.
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

        readonly int _simBudget;
        readonly double _timeBudget;

        readonly Heap _open = new Heap();
        readonly Stopwatch _watch = new Stopwatch();

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
        /// _pool 을 잉크 오름차순으로 훑는 순서.
        /// 부모가 싼 자식부터 내야 열린 목록이 잉크 순서를 지킨다.
        /// List.Sort 는 안정적이지 않아 번호로 한 번 더 가른다.
        /// </summary>
        readonly List<int> _order = new List<int>();

        /// 궤적에서 후보를 놓을 자리를 추릴 때 쓴다.
        readonly HashSet<BallCell> _sites = new HashSet<BallCell>();

        int _seq;
        int _baseScenes;

        public SolverReport Report { get; private set; }

        /// <param name="simBudget">돌릴 시뮬 횟수 상한.</param>
        /// <param name="timeBudget">쓸 시간 상한(초).</param>
        public SolverSearch(
            LevelData level, int seed = 0,
            int simBudget = SolverConfig.SearchSimBudget,
            double timeBudget = SolverConfig.SearchSeconds)
        {
            _level = level;
            _trial = new PrimitiveTrial(level, seed);
            _candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
            _codec = new PrimitiveCodec(level);
            _quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            _buffer = new TrajectoryBuffer(SolverConfig.TrajectoryInterval);
            _area = LevelDataArea.Calculate(level);

            _simBudget = simBudget;
            _timeBudget = timeBudget;
        }

        /// <summary>
        /// 다 돌면 Report 에 결과가 들어 있다.
        /// 씨앗은 아무것도 안 놓은 빈 배치다 — 그냥 굴려도 이기는 판은
        /// 첫 팝에서 끝나고 빈 Solution 이 나간다.
        /// </summary>
        public IEnumerator Run()
        {
            Report = new SolverReport();
            _baseScenes = SceneManager.sceneCount;

            Push(new SearchNode(new Primitive[0], 0f, _seq++));

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

                    var drain = Drain();
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
                    Primitive candidate = _pool[_order[node.Cursor++]];
                    Push(Child(node, candidate));
                }

                // 낼 것이 더 있으면 다음 자식의 f 를 달고 다시 줄에 선다.
                if (node.Cursor < _order.Count) Push(node);

                if (OverBudget()) break;
            }

            Report.Elapsed = _watch.Elapsed;
        }

        /// <summary>
        /// 이 배치를 굴려 궤적을 _buffer 에 받는다.
        /// 정체 판정은 걸지 않는다 — 맵 빌드와 달리 여기서 끊으면
        /// 판정 자체가 바뀌어, 늦게 이기는 풀이를 잃는다.
        /// </summary>
        TrialResult Roll(Primitive[] primitives)
        {
            _watch.Start();
            TrialResult result = _trial.RunSampled(_codec.Encode(primitives), _buffer);
            _watch.Stop();

            Report.Sims++;
            Report.MinGoalDist = Mathf.Min(Report.MinGoalDist, result.Sim.MinGoalDist);
            return result;
        }

        /// <summary>
        /// 이 노드가 낼 후보를 전부 펼쳐 잉크 순으로 세운다.
        /// 놓는 자리는 궤적을 셀로 접은 서로 다른 지점들이다 —
        /// 맵도 셀 대표 상태에 놓으므로 둘이 같은 자리를 본다.
        /// 표본을 그대로 쓰면 같은 셀에 열 번씩 놓게 된다.
        /// </summary>
        void Spread(SearchNode node)
        {
            _expanded = node;
            _pool.Clear();
            _inks.Clear();
            _order.Clear();
            _sites.Clear();

            for (int i = 0; i < _buffer.Count; i++)
            {
                BallSample sample = _buffer[i];
                if (!_sites.Add(_quantizer.Quantize(sample.Position, sample.Velocity)))
                    continue;

                _pool.AddRange(_candidates.At(new BallState(sample.Position, sample.Velocity)));
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                _inks.Add(PrimitiveValidator.Ink(_pool[i]));
                _order.Add(i);
            }

            _order.Sort(CompareByInk);
        }

        /// 잉크가 같으면 만든 순서를 따른다. 실행마다 같아야 한다.
        int CompareByInk(int a, int b)
        {
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
                    node.Key = node.Ink + _inks[_order[node.Cursor]];
                    return true;
                }

                Report.Rejected++;
                node.Cursor++;
            }

            return false;
        }

        /// <summary>
        /// 이 노드의 다음 자식이 열린 목록의 최소보다 비싼가.
        /// 비싸지면 물러나야 잉크 순서가 지켜진다.
        /// </summary>
        bool Costlier(SearchNode node)
            => _open.Count > 0 && node.Key > _open.Peek().Key;

        SearchNode Child(SearchNode parent, in Primitive candidate)
        {
            var primitives = new Primitive[parent.Primitives.Length + 1];
            parent.Primitives.CopyTo(primitives, 0);
            primitives[parent.Primitives.Length] = candidate;

            return new SearchNode(
                primitives, parent.Ink + PrimitiveValidator.Ink(candidate), _seq++);
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
        /// 로드된 씬이 상한 아래로 내려올 때까지 프레임을 넘긴다.
        /// SimWorld 의 언로드는 프레임 끝에 처리되는데, 씬이 쌓이면
        /// 생성·해제 비용이 로드된 씬 수를 타서 전체가 제곱이 된다.
        /// 만든 개수가 아니라 남아 있는 개수를 눌러야 한다 —
        /// 프레임당 몇 개나 빠지는지는 부르는 쪽이 알 수 없다.
        /// </summary>
        IEnumerator Drain()
        {
            int waited = 0;

            while (SceneManager.sceneCount > _baseScenes + SolverConfig.MaxLoadedScenes)
            {
                if (++waited > SolverConfig.MaxDrainFrames)
                {
                    UnityEngine.Debug.LogWarning(
                        $"씬이 {SolverConfig.MaxDrainFrames} 프레임 동안 안 줄었다 " +
                        $"({SceneManager.sceneCount} 개). 그대로 진행한다.");
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Key 가 작은 노드부터 내는 이진 힙.
        /// .NET Standard 2.1 에는 우선순위 큐가 없어 직접 둔다.
        /// 같은 Key 면 깊은 쪽이 먼저다 — 얕은 쪽을 먼저 내면
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

            static bool Precedes(SearchNode a, SearchNode b)
            {
                if (a.Key != b.Key) return a.Key < b.Key;
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
