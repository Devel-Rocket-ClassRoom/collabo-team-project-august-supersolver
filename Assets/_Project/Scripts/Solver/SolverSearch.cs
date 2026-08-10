using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 레벨 하나를 풀어 보는 깊이 제한 전수 순회.
    /// 순위를 매기지 않는다 — 순위를 매기면 실패가
    /// "후보에 없어서" 인지 "가지치기에 잘려서" 인지 갈리지 않고,
    /// 그러면 실패를 레벨에 대한 판정으로 쓸 수 없다.
    /// 끝까지 돌아 Exhausted 로 끝났을 때만 그 판정이 성립한다.
    /// 물리 씬을 걷으려면 프레임을 넘겨야 해 코루틴이다.
    /// </summary>
    public sealed class SolverSearch
    {
        readonly LevelData _level;
        readonly PrimitiveTrial _trial;
        readonly PrimitiveCandidates _candidates;
        readonly PrimitiveCodec _codec;
        readonly TrajectoryBuffer _buffer;
        readonly Rect _area;

        readonly int _maxDepth;
        readonly int _simBudget;
        readonly double _timeBudget;

        readonly Stopwatch _watch = new Stopwatch();
        readonly SimScenes _scenes = new SimScenes();

        /// <summary>
        /// 제자리를 맴도는 판을 끊는 기준.
        /// 위치 셀 하나를 IdleSteps 동안 못 벗어난 공은 목표에 못 간다.
        /// 안 걸면 그런 판이 상한 1800 스텝까지 돈다.
        /// Stalled 가 Timeout 으로 바뀌지만 탐색은 Cleared 만 본다.
        /// </summary>
        readonly IdleCutoff _idle;

        /// 지금 놓여 있는 선들. 깊이를 오르내리며 밀고 뺀다.
        readonly List<Primitive> _placed = new List<Primitive>();

        /// _placed 의 누적 잉크. 매번 다시 재면 검사 비용이 시뮬에 붙는다.
        float _ink;

        /// 깊이마다 후보 목록 하나. 자식으로 내려가도 부모 목록은 살아 있어야 한다.
        readonly List<List<Primitive>> _pools = new List<List<Primitive>>();

        /// 후보를 놓을 자리를 추릴 때 쓴다. 표본을 그대로 쓰면
        /// 같은 자리에 열 번씩 놓게 된다.
        readonly HashSet<Vector2Int> _sites = new HashSet<Vector2Int>();

        readonly float _positionStep;

        /// 풀렸거나 예산이 끝났다. 재귀를 통째로 접는다.
        bool _halt;

        public SolverReport Report { get; private set; }

        /// <param name="maxDepth">한 풀이에 쓸 선 개수의 상한.
        /// 유저가 한 번에 구상하는 획 수를 뜻한다.</param>
        /// <param name="simBudget">돌릴 시뮬 횟수 상한.</param>
        /// <param name="timeBudget">쓸 시간 상한(초).</param>
        public SolverSearch(
            LevelData level, int seed = 0,
            int maxDepth = SolverConfig.SearchMaxDepth,
            int simBudget = SolverConfig.SearchSimBudget,
            double timeBudget = SolverConfig.SearchSeconds)
        {
            _level = level;
            _trial = new PrimitiveTrial(level, seed);
            _candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
            _codec = new PrimitiveCodec(level);
            _buffer = new TrajectoryBuffer(SolverConfig.TrajectoryInterval);
            _area = LevelDataArea.Calculate(level);
            _positionStep = SolverConfig.PositionStep(level);
            _idle = new IdleCutoff(_positionStep, SolverConfig.IdleSteps);

            _maxDepth = maxDepth;
            _simBudget = simBudget;
            _timeBudget = timeBudget;

            for (int d = 0; d < maxDepth; d++)
                _pools.Add(new List<Primitive>());
        }

        /// <summary>
        /// 다 돌면 Report 에 결과가 들어 있다.
        /// 출발은 아무것도 안 놓은 빈 배치다 — 그냥 굴려도 이기는 판은
        /// 첫 판에서 끝나고 빈 Solution 이 나간다.
        /// </summary>
        public IEnumerator Run()
        {
            Report = new SolverReport();

            // 앞 단계가 남긴 언로드 대기분이 빠진 뒤에 기준선을 잡는다.
            var settle = _scenes.Settle();
            while (settle.MoveNext()) yield return settle.Current;

            if (Visit())
            {
                Finish();
                yield break;
            }

            var drain = _scenes.Drain();
            while (drain.MoveNext()) yield return drain.Current;

            var descend = Descend(0);
            while (descend.MoveNext()) yield return descend.Current;

            Finish();
        }

        /// <summary>
        /// 이 깊이에서 놓을 수 있는 후보를 하나씩 놓아 본다.
        /// 들어올 때 _buffer 에는 부모 배치의 궤적이 들어 있어야 한다 —
        /// 후보를 놓을 자리가 거기서 나온다.
        /// </summary>
        IEnumerator Descend(int depth)
        {
            if (depth >= _maxDepth) yield break;

            List<Primitive> pool = _pools[depth];
            Fill(pool);

            for (int i = 0; i < pool.Count; i++)
            {
                Primitive candidate = pool[i];

                // 잉크나 영역에 걸리는 후보는 시뮬 없이 여기서 버려진다.
                if (PrimitiveValidator.Validate(candidate, _level, _ink, _area)
                    != PlacementReject.None)
                {
                    Report.Rejected++;
                    continue;
                }

                _placed.Add(candidate);
                _ink += PrimitiveValidator.Ink(candidate);

                bool cleared = Visit();

                if (!cleared)
                {
                    var drain = _scenes.Drain();
                    while (drain.MoveNext()) yield return drain.Current;

                    var deeper = Descend(depth + 1);
                    while (deeper.MoveNext()) yield return deeper.Current;
                }

                _ink -= PrimitiveValidator.Ink(candidate);
                _placed.RemoveAt(_placed.Count - 1);

                if (_halt) yield break;
            }
        }

        /// <summary>
        /// 지금 배치를 굴려 궤적을 _buffer 에 받고 결과를 기록한다.
        /// 참을 내면 풀렸다는 뜻이고, 그때 _halt 가 선다.
        /// 레벨 시작점에서 전체 배치로 굴린다 — 중간 상태에서 새 선만 놓고
        /// 굴리면 반환한 풀이를 재실행했을 때 같은 결과가 난다는 보장이 없다.
        /// </summary>
        bool Visit()
        {
            _watch.Start();
            TrialResult result = _trial.RunSampled(
                _codec.Encode(_placed.ToArray()), _buffer,
                SimWorld.DefaultMaxSteps, null, _idle);
            _watch.Stop();

            Report.Sims++;
            Report.Steps += result.Sim.EndStep;
            Report.MinGoalDist = Mathf.Min(Report.MinGoalDist, result.Sim.MinGoalDist);
            Report.Deepest = Mathf.Max(Report.Deepest, _placed.Count);

            if (result.Cleared)
            {
                Report.Stop = SolverStop.Solved;
                Report.Solution = Rebuild(_placed.ToArray());
                _halt = true;
                return true;
            }

            _halt = OverBudget();
            return false;
        }

        /// <summary>
        /// 궤적 위의 서로 다른 자리마다 후보를 전부 펼친다.
        /// 순서를 손대지 않는다 — 정렬은 곧 순위이고, 순위는
        /// 전수 순회가 아니게 만든다.
        /// </summary>
        void Fill(List<Primitive> pool)
        {
            pool.Clear();
            _sites.Clear();

            for (int i = 0; i < _buffer.Count; i++)
            {
                BallSample sample = _buffer[i];

                var cell = new Vector2Int(
                    Mathf.FloorToInt(sample.Position.x / _positionStep),
                    Mathf.FloorToInt(sample.Position.y / _positionStep));

                if (!_sites.Add(cell)) continue;

                pool.AddRange(_candidates.At(new BallState(sample.Position, sample.Velocity)));
            }
        }

        void Finish()
        {
            Report.Elapsed = _watch.Elapsed;
            Report.PeakScenes = _scenes.Peak;
            Report.PeakTotalScenes = _scenes.PeakTotal;
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
    }
}
