using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 시작점에서 도달 가능한 셀과 그 사이 간선을 모은다.
    /// 휴리스틱 맵의 원재료다 — 여기서 나온 간선을 목표 셀에서
    /// 거꾸로 0-1 BFS 하면 h 가 채워진다.
    /// 넓히려고 굴리는 시뮬이 곧 간선을 만드는 시뮬이라
    /// 도달 집합 산출과 간선 수집이 워크리스트 하나로 같이 끝난다.
    /// 물리 씬을 걷으려면 프레임을 넘겨야 해 코루틴이다.
    /// </summary>
    public sealed class CellGraphBuilder
    {
        readonly LevelData _level;
        readonly BallQuantizer _quantizer;
        readonly PrimitiveCandidates _candidates;
        readonly PrimitiveCodec _codec;
        readonly PrimitiveTrial _trial;
        readonly TrajectoryBuffer _buffer;
        readonly CellEdgeCollector _collector;
        readonly IdleCutoff _idle;
        readonly Stopwatch _watch = new Stopwatch();
        readonly SimScenes _scenes = new SimScenes();

        readonly int _maxDepth;
        readonly int _simBudget;

        readonly Primitive[] _one = new Primitive[1];

        /// 버퍼는 배치 시뮬이 덮어쓰므로 후보를 미리 떠 둔다.
        readonly List<Primitive> _pool = new List<Primitive>();

        /// <summary>
        /// 셀 → 그 셀을 처음 낸 실제 표본.
        /// 셀 중심을 지어내는 대신 시뮬이 실제로 지나간 상태를 쓴다 —
        /// 지어낸 중심은 지형 안으로 들어가 버릴 수 있는데,
        /// 이 상태는 물리가 실제로 통과했으니 반드시 유효하다.
        /// </summary>
        readonly Dictionary<BallCell, BallState> _states =
            new Dictionary<BallCell, BallState>();

        /// <summary>
        /// 셀 → 그 셀에서 이기는 데 든 최소 선 개수. 0 아니면 1 이다.
        /// h 를 시뮬 판정으로 직접 알아낸 셀들이며, 나머지 셀의 h 는
        /// 전부 여기서 간선을 거꾸로 타고 계산된다 — 이게 비면
        /// 퍼뜨릴 곳이 없어 맵 전체가 미지로 남는다.
        /// 목표 셀을 좌표로 찾는 방법은 못 쓴다 — 시뮬이 목표에 닿는 순간
        /// 끝나서 그 근처 상태가 궤적 표본에 거의 안 남고,
        /// 따라서 목표 셀로 들어오는 간선이 없다.
        /// </summary>
        readonly Dictionary<BallCell, int> _wins = new Dictionary<BallCell, int>();

        public CellGraphBuilder(
            LevelData level, int seed = 0,
            int maxDepth = SolverConfig.MapDepth,
            int simBudget = SolverConfig.MapSimBudget)
        {
            _level = level;
            _quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            _candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
            _codec = new PrimitiveCodec(level);
            _trial = new PrimitiveTrial(level, seed);
            _buffer = new TrajectoryBuffer(SolverConfig.TrajectoryInterval, SolverConfig.RollSteps);
            _collector = new CellEdgeCollector(_quantizer);

            // 반경이 위치 셀 폭이라 "셀 하나도 못 벗어났다" 가 된다.
            _idle = new IdleCutoff(_quantizer.PositionStep, SolverConfig.IdleSteps);

            _maxDepth = maxDepth;
            _simBudget = simBudget;
        }

        /// 이 격자를 쓴 것이라 h 를 읽는 쪽도 같은 것을 써야 한다.
        public BallQuantizer Quantizer => _quantizer;

        public IReadOnlyDictionary<BallCell, BallState> States => _states;

        public IReadOnlyDictionary<CellEdge, int> Edges => _collector.Edges;

        /// <summary>
        /// 이기는 셀과 그때 든 선 개수. 무배치로 이기면 0, 선 하나면 1.
        /// HeuristicMap 이 여기서 h 를 뒤로 퍼뜨린다.
        /// </summary>
        public IReadOnlyDictionary<BallCell, int> Wins => _wins;

        public int Sims { get; private set; }

        /// 실제로 만든 물리 씬 수. 메모리를 먹는 것은 이쪽이다.
        public int Worlds { get; private set; }

        /// 우리가 얹은 시뮬 씬의 최고점과, 그때의 절대 개수.
        public int PeakScenes => _scenes.Peak;

        public int PeakTotalScenes => _scenes.PeakTotal;

        /// 실제로 끝낸 깊이. 상한에 걸려 멈추면 이것이 더 작다.
        public int ReachedDepth { get; private set; }

        /// 시뮬 예산을 다 써서 멈췄는가. 참이면 맵이 미완성이다.
        public bool Stopped { get; private set; }

        public System.TimeSpan Elapsed => _watch.Elapsed;

        /// <summary>
        /// 더 나올 셀이 없거나 깊이 상한에 닿을 때까지 넓힌다.
        /// 출발은 아무것도 안 놓고 굴린 기저 궤적이다 —
        /// 프리미티브 0 개로 닿는 곳이라 깊이 0 이다.
        /// </summary>
        public IEnumerator Build()
        {
            // 앞 테스트가 남긴 언로드 대기분이 빠진 뒤에 기준선을 잡는다.
            var settle = _scenes.Settle();
            while (settle.MoveNext()) yield return settle.Current;

            // 출발 상태를 먼저 등록한다. 간선이 여기서 뻗으므로
            // 도달 집합에 없으면 대표 상태 없는 셀이 h 를 받는다.
            // 궤적 표본은 10스텝째부터라 이 상태는 거기 안 들어 있다.
            var startState = new BallState(_level.BallStart, Vector2.zero);
            BallCell startCell = _quantizer.Quantize(startState.Position, startState.Velocity);
            _states[startCell] = startState;

            Roll(new float[0], startCell, null, placed: false);

            // 출발 셀도 넓힐 대상이다. 탐색의 루트가 바로 여기라
            // 이 셀의 h 가 비면 맵이 가장 중요한 자리에서 입을 다문다.
            var frontier = new List<BallCell> { startCell };
            frontier.AddRange(TakeNew());

            for (int depth = 1; depth <= _maxDepth && frontier.Count > 0; depth++)
            {
                var next = new List<BallCell>();

                var expand = Expand(frontier, next);
                while (expand.MoveNext()) yield return expand.Current;

                if (Stopped) yield break;

                ReachedDepth = depth;
                frontier = next;
            }
        }

        /// <summary>
        /// 프론티어의 각 셀에서 무배치 1회 + 후보마다 1회 굴린다.
        /// 처음 보는 셀만 next 에 담는다.
        /// </summary>
        IEnumerator Expand(List<BallCell> frontier, List<BallCell> next)
        {
            foreach (BallCell cell in frontier)
            {
                BallState start = _states[cell];

                TrialResult free = Roll(new float[0], cell, start, placed: false);
                next.AddRange(TakeNew());

                var drain = _scenes.Drain();
                while (drain.MoveNext()) yield return drain.Current;

                // 선 없이 이미 이기면 h = 0 이다. 더 놓아도 나아질 수 없고,
                // 이 상태를 지나는 풀이는 그 시점에 이미 끝난 것이다.
                // 무배치 판은 남긴다 — 비용 0 간선이 h = 0 을 뒤로 전파한다.
                if (free.Cleared)
                {
                    Win(cell, 0);
                    continue;
                }

                // 후보는 공을 둘러싼 고리 위다. 공 자리에 놓으면 박힌다.
                _pool.Clear();
                _pool.AddRange(_candidates.At(start));

                foreach (Primitive candidate in _pool)
                {
                    _one[0] = candidate;

                    // 선 하나로 이겼으면 이 셀의 h 는 1 이다.
                    // 이 판정을 버리면 h 를 아는 셀이 "무배치로 이기는 셀" 뿐인데,
                    // 그런 셀은 기저가 이미 이기는 판에만 있다.
                    if (Roll(_codec.Encode(_one), cell, start, placed: true).Cleared)
                        Win(cell, 1);

                    next.AddRange(TakeNew());

                    drain = _scenes.Drain();
                    while (drain.MoveNext()) yield return drain.Current;

                    if (Sims >= _simBudget)
                    {
                        Stopped = true;
                        yield break;
                    }
                }
            }
        }

        /// 같은 셀을 여러 번 이겨도 싼 쪽만 남긴다.
        void Win(BallCell cell, int lines)
        {
            if (_wins.TryGetValue(cell, out int known) && known <= lines) return;
            _wins[cell] = lines;
        }

        /// <summary>
        /// 한 판 굴리고 궤적을 간선으로 접어 넣는다.
        /// 거부된 시행은 월드를 아예 안 만든다 — 씬도 안 쌓인다.
        /// </summary>
        /// <param name="from">공이 출발한 셀. 간선이 전부 여기서 뻗는다.</param>
        TrialResult Roll(float[] vector, BallCell from, BallState? start, bool placed)
        {
            _watch.Start();
            TrialResult result =
                _trial.RunSampled(vector, _buffer, SolverConfig.RollSteps, start, _idle);
            _watch.Stop();

            Sims++;
            if (result.Rejected) return result;

            Worlds++;

            if (placed) _collector.CollectPlaced(from, _buffer);
            else _collector.CollectFree(from, _buffer);

            return result;
        }

        /// <summary>
        /// 이번 궤적이 지난 셀 중 처음 보는 것.
        /// 그 셀의 대표로 이 표본을 그대로 박아 둔다 —
        /// 먼저 본 것이 이기며, 탐색 순서가 결정적이라 재현된다.
        /// </summary>
        List<BallCell> TakeNew()
        {
            var found = new List<BallCell>();

            for (int i = 0; i < _buffer.Count; i++)
            {
                BallSample sample = _buffer[i];
                BallCell cell = _quantizer.Quantize(sample.Position, sample.Velocity);

                if (_states.ContainsKey(cell)) continue;

                _states[cell] = new BallState(sample.Position, sample.Velocity);
                found.Add(cell);
            }

            return found;
        }
    }
}
