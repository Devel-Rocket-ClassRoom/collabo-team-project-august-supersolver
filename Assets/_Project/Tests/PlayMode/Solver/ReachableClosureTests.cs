using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 시작점에서 실제로 도달 가능한 셀 집합이 얼마나 큰지 잰다.
    /// 전체 격자는 물리적으로 불가능한 위치·속도 조합을 잔뜩 품는데,
    /// 그 셀들의 h 는 아무도 물어보지 않는다.
    /// 이 집합이 작으면 그 안에서는 선빌드가 성립한다.
    /// </summary>
    public class ReachableClosureTests
    {
        /// <summary>
        /// 후보 Size 분할. 1 이라 후보가 60 개다.
        /// 적게 잡을수록 덜 퍼지므로 결과는 C* 의 **하한**이다.
        /// 깊이를 보려면 폭을 줄여야 한다.
        /// </summary>
        const int SizeSteps = 1;

        /// 한 셀을 굴리는 스텝 상한.
        const int RollSteps = 180;

        const int TrajectoryInterval = 10;

        /// <summary>
        /// 안전장치. 넘으면 그 자리에서 멈추고 곡선만 보고한다 —
        /// 폭주하는지 아닌지가 알고 싶은 것이지
        /// 끝까지 도는 것이 목적이 아니다.
        /// </summary>
        const int MaxSims = 100000;

        const int MaxDepth = 6;

        [Test]
        public void 도달_가능_셀의_증가_곡선을_찍는다()
        {
            var level = SampleLevelFile.Load();
            var quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            var grid = new BallGrid(level, quantizer);

            long allCells = (long)grid.CellCount * SolverConfig.VelocityCellCount;

            var text = new StringBuilder();
            text.AppendLine($"[격자] 전체 셀 {allCells:N0} " +
                            $"(위치 {grid.CellCount} × 속도 {SolverConfig.VelocityCellCount})");
            text.AppendLine($"[설정] 후보 {new PrimitiveCandidates(level, SizeSteps).Count} 개 · " +
                            $"굴림 {RollSteps} 스텝 · 시뮬 상한 {MaxSims:N0}");

            var explorer = new Explorer(level, quantizer);
            var frontier = explorer.Seed();

            text.AppendLine($"깊이 0: 셀 {explorer.Visited.Count,7:N0} " +
                            $"(무배치 궤적) 새 셀 {frontier.Count}");

            for (int depth = 1; depth <= MaxDepth && frontier.Count > 0; depth++)
            {
                frontier = explorer.Expand(frontier);

                text.AppendLine(
                    $"깊이 {depth}: 셀 {explorer.Visited.Count,7:N0} " +
                    $"({(double)explorer.Visited.Count / allCells:P2}) " +
                    $"새 셀 {frontier.Count,7:N0} · " +
                    $"누적 시뮬 {explorer.Sims,7:N0} · 간선 {explorer.Edges,7:N0} · " +
                    $"{explorer.Elapsed.TotalSeconds,6:F1}초");

                if (explorer.Stopped)
                {
                    text.AppendLine($"  ⚠ 시뮬 상한 {MaxSims:N0} 에서 중단. 위 값은 미완성이다.");
                    break;
                }
            }

            if (!explorer.Stopped && frontier.Count == 0)
                text.AppendLine("  ✔ 더 나올 셀이 없다 — 고정점에 도달했다.");

            Debug.Log(text.ToString());

            Assert.Greater(explorer.Visited.Count, 0, "무배치 궤적조차 셀을 못 냈다");
            Assert.Greater(explorer.Sims, 0, "한 판도 굴리지 못했다");
        }

        /// <summary>
        /// 워크리스트로 도달 집합을 넓힌다.
        /// 넓히려고 굴리는 시뮬이 곧 간선을 만드는 시뮬이라
        /// 도달 집합 산출과 맵 빌드가 같은 한 번이다.
        /// </summary>
        sealed class Explorer
        {
            readonly LevelData _level;
            readonly BallQuantizer _quantizer;
            readonly PrimitiveCandidates _candidates;
            readonly PrimitiveCodec _codec;
            readonly PrimitiveTrial _trial;
            readonly TrajectoryBuffer _buffer;
            readonly CellEdgeCollector _collector;
            readonly Stopwatch _watch = new Stopwatch();

            /// 후보는 셀의 위치에만 달렸다. 속도 셀들이 나눠 쓴다.
            readonly Dictionary<Vector2Int, List<Primitive>> _byPosition =
                new Dictionary<Vector2Int, List<Primitive>>();

            readonly Primitive[] _one = new Primitive[1];

            public readonly HashSet<BallCell> Visited = new HashSet<BallCell>();

            public int Sims { get; private set; }

            public bool Stopped { get; private set; }

            public int Edges => _collector.Edges.Count;

            public System.TimeSpan Elapsed => _watch.Elapsed;

            public Explorer(LevelData level, BallQuantizer quantizer)
            {
                _level = level;
                _quantizer = quantizer;
                _candidates = new PrimitiveCandidates(level, SizeSteps);
                _codec = new PrimitiveCodec(level);
                _trial = new PrimitiveTrial(level, seed: 0);
                _buffer = new TrajectoryBuffer(TrajectoryInterval, RollSteps);
                _collector = new CellEdgeCollector(quantizer);
            }

            /// <summary>
            /// 씨앗은 아무것도 안 놓고 굴린 기저 궤적이다.
            /// 프리미티브 0 개로 닿는 곳이라 깊이 0 이다.
            /// </summary>
            public List<BallCell> Seed()
            {
                _watch.Start();
                _trial.RunSampled(new float[0], _buffer, RollSteps);
                _watch.Stop();
                Sims++;

                _collector.CollectFree(_buffer);
                return TakeNew();
            }

            /// <summary>
            /// 프론티어의 각 셀에서 무배치 1회 + 후보마다 1회 굴린다.
            /// 처음 보는 셀만 다음 프론티어로 넘긴다.
            /// </summary>
            public List<BallCell> Expand(List<BallCell> frontier)
            {
                var next = new List<BallCell>();

                foreach (var cell in frontier)
                {
                    BallState start = _quantizer.Dequantize(cell);

                    // 벽에 박힌 채 출발한 공은 튕겨 나가 없는 이동을 만든다.
                    if (BallSpawn.Blocked(_level, start.Position)) continue;

                    Roll(new float[0], start, placed: false, next);

                    foreach (var candidate in CandidatesAt(cell, start.Position))
                    {
                        _one[0] = candidate;
                        Roll(_codec.Encode(_one), start, placed: true, next);

                        if (Sims >= MaxSims)
                        {
                            Stopped = true;
                            return next;
                        }
                    }
                }

                return next;
            }

            void Roll(float[] vector, BallState start, bool placed, List<BallCell> next)
            {
                _watch.Start();
                var result = _trial.RunSampled(vector, _buffer, RollSteps, start);
                _watch.Stop();
                Sims++;

                // 거부된 시행은 시뮬이 없어 버퍼가 비어 온다.
                if (result.Reject != PlacementReject.None) return;

                if (placed) _collector.CollectPlaced(_buffer);
                else _collector.CollectFree(_buffer);

                next.AddRange(TakeNew());
            }

            /// 이번 궤적이 지난 셀 중 처음 보는 것.
            List<BallCell> TakeNew()
            {
                var found = new List<BallCell>();

                for (int i = 0; i < _buffer.Count; i++)
                {
                    BallCell cell = _quantizer.Quantize(_buffer[i].Position, _buffer[i].Velocity);
                    if (Visited.Add(cell)) found.Add(cell);
                }

                return found;
            }

            List<Primitive> CandidatesAt(BallCell cell, Vector2 center)
            {
                var key = new Vector2Int(cell.X, cell.Y);
                if (_byPosition.TryGetValue(key, out var cached)) return cached;

                var list = new List<Primitive>(_candidates.At(center));
                _byPosition[key] = list;
                return list;
            }
        }
    }
}
