using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
        /// 후보 Size 분할. 2 면 공 반지름과 최대 크기 둘 다 나온다.
        /// 1 로 두면 안 된다 — 규칙표가 마지막 단계에 최대 크기를
        /// 주므로 거대 프리미티브만 남아 거부율이 90% 로 뜬다.
        /// </summary>
        const int SizeSteps = 2;

        /// 한 셀을 굴리는 스텝 상한.
        const int RollSteps = 180;

        const int TrajectoryInterval = 10;

        /// <summary>
        /// 안전장치. 넘으면 그 자리에서 멈추고 곡선만 보고한다 —
        /// 폭주하는지 아닌지가 알고 싶은 것이지
        /// 끝까지 도는 것이 목적이 아니다.
        /// </summary>
        const int MaxSims = 400000;

        /// <summary>
        /// 동시에 로드해 둘 시뮬 씬의 상한.
        /// `SimWorld.Dispose` 의 언로드는 프레임 끝에 처리되는데,
        /// 씬이 쌓이면 생성·해제 비용이 로드된 씬 수를 타서
        /// 전체가 O(씬수 × 판수) 가 된다. 상수로 묶어 선형으로 만든다.
        /// 만든 개수가 아니라 **남아 있는 개수**를 눌러야 한다 —
        /// 얼마나 빠졌는지는 만든 쪽에서 알 수 없다.
        /// </summary>
        const int MaxLoadedScenes = 16;

        /// <summary>
        /// 씬이 끝내 안 줄어들 때 포기하고 진행할 프레임 수.
        /// 없으면 조용히 멈춘 것과 구분이 안 된다.
        /// </summary>
        const int MaxWaitFrames = 600;

        /// 깊이는 곧 프리미티브 개수다. 잉크 예산이
        /// 허용하는 것보다 깊은 셀은 답에 못 들어간다.
        const int MaxDepth = 4;

        /// 기본 상한 3분으로는 못 끝낸다. 일회성 측정이라 넉넉히 준다.
        [UnityTest, Timeout(1800000)]
        public IEnumerator 도달_가능_셀의_증가_곡선을_찍는다()
        {
            var level = SampleLevelFile.Load();
            var quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            var grid = new BallGrid(level, quantizer);

            long allCells = (long)grid.CellCount * SolverConfig.VelocityCellCount;

            // 깊이마다 바로 찍는다. 끝에 몰아서 찍으면
            // 상한이나 타임아웃에 걸릴 때 측정이 통째로 날아간다.
            Debug.Log($"[격자] 전체 셀 {allCells:N0} " +
                      $"(위치 {grid.CellCount} × 속도 {SolverConfig.VelocityCellCount})\n" +
                      $"[설정] 후보 {new PrimitiveCandidates(level, SizeSteps).Count} 개 · " +
                      $"굴림 {RollSteps} 스텝 · 시뮬 상한 {MaxSims:N0}");

            var explorer = new Explorer(level, quantizer);
            var frontier = explorer.Seed();

            Debug.Log($"깊이 0: 셀 {explorer.CellCount,7:N0} (무배치 궤적) 새 셀 {frontier.Count}");

            for (int depth = 1; depth <= MaxDepth && frontier.Count > 0; depth++)
            {
                var next = new List<BallCell>();

                // 씬 언로드가 프레임 끝에 처리되므로 중간중간 넘겨야 한다.
                var routine = explorer.Expand(frontier, next);
                while (routine.MoveNext()) yield return routine.Current;

                frontier = next;

                Debug.Log(
                    $"깊이 {depth}: 셀 {explorer.CellCount,7:N0} " +
                    $"({(double)explorer.CellCount / allCells:P2}) " +
                    $"새 셀 {frontier.Count,7:N0} · " +
                    $"시뮬 {explorer.Sims,7:N0} (월드 {explorer.Worlds,7:N0}) · " +
                    $"간선 {explorer.Edges,7:N0} · " +
                    $"거부 {(float)explorer.Rejected / explorer.Sims:P0} · " +
                    $"중심이면 잃을 셀 {explorer.CenterBlocked,5:N0} · " +
                    $"{explorer.Elapsed.TotalSeconds,6:F1}초 " +
                    $"(월드당 {explorer.MsPerWorld:F2}ms · 씬 최고 {explorer.PeakScenes})");

                if (explorer.Stopped)
                {
                    Debug.Log($"  ⚠ 시뮬 상한 {MaxSims:N0} 에서 중단. 위 값은 미완성이다.");
                    break;
                }
            }

            if (!explorer.Stopped && frontier.Count == 0)
                Debug.Log("  ✔ 더 나올 셀이 없다 — 고정점에 도달했다.");

            Assert.Greater(explorer.CellCount, 0, "무배치 궤적조차 셀을 못 냈다");
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

            /// <summary>
            /// 셀 → 그 셀을 처음 낸 실제 표본.
            /// 셀 중심을 지어내는 대신 시뮬이 실제로 지나간 상태를 쓴다 —
            /// 지어낸 중심은 지형 안으로 들어가 버릴 수 있는데,
            /// 이 상태는 물리가 실제로 통과했으니 반드시 유효하다.
            /// </summary>
            readonly Dictionary<BallCell, BallState> _states =
                new Dictionary<BallCell, BallState>();

            public int CellCount => _states.Count;

            public int Sims { get; private set; }

            /// 유효성 검사에서 걸려 시뮬을 안 돈 시행.
            public int Rejected { get; private set; }

            /// 실제로 만든 물리 씬 수. 메모리를 먹는 것은 이쪽이다.
            public int Worlds { get; private set; }

            /// 동시에 로드돼 있던 시뮬 씬의 최고점. 상한이 먹히는지 본다.
            public int PeakScenes { get; private set; }

            /// 월드 하나당 시뮬 시간. 깊이가 깊어져도 평평해야 한다.
            public double MsPerWorld => Worlds == 0 ? 0 : _watch.Elapsed.TotalMilliseconds / Worlds;

            readonly int _sceneBaseline = SceneManager.sceneCount;

            /// <summary>
            /// 셀 중심을 대표로 썼다면 지형에 박혀 버려졌을 셀 수.
            /// 실제 표본을 쓰면 잃지 않는다는 것을 재는 값이다.
            /// </summary>
            public int CenterBlocked { get; private set; }

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
                Worlds++;

                _collector.CollectFree(_buffer);
                return TakeNew();
            }

            /// <summary>
            /// 프론티어의 각 셀에서 무배치 1회 + 후보마다 1회 굴린다.
            /// 처음 보는 셀만 next 에 담는다.
            /// 물리 씬을 걷으려면 프레임을 넘겨야 해 코루틴이다.
            /// </summary>
            public IEnumerator Expand(List<BallCell> frontier, List<BallCell> next)
            {
                foreach (var cell in frontier)
                {
                    BallState start = _states[cell];
                    Vector2 center = _quantizer.Dequantize(cell).Position;

                    // 셀 중심을 대표로 썼다면 잃었을 셀. 실제 표본은 안 막힌다.
                    if (BallSpawn.Blocked(_level, center)) CenterBlocked++;

                    Roll(new float[0], start, placed: false, next);

                    var drain = Drain();
                    while (drain.MoveNext()) yield return drain.Current;

                    // 후보는 위치 셀에 붙는다. 공만 실제 표본에서 출발한다.
                    foreach (var candidate in CandidatesAt(cell, center))
                    {
                        _one[0] = candidate;
                        Roll(_codec.Encode(_one), start, placed: true, next);

                        drain = Drain();
                        while (drain.MoveNext()) yield return drain.Current;

                        if (Sims >= MaxSims)
                        {
                            Stopped = true;
                            yield break;
                        }
                    }
                }
            }

            /// <summary>
            /// 로드된 씬이 상한 아래로 내려올 때까지 프레임을 넘긴다.
            /// 언로드가 프레임당 몇 개나 빠지는지는 우리가 모르므로,
            /// 만든 개수로 세지 않고 남은 개수를 직접 본다.
            /// </summary>
            IEnumerator Drain()
            {
                int waited = 0;

                while (SceneManager.sceneCount > _sceneBaseline + MaxLoadedScenes)
                {
                    if (++waited > MaxWaitFrames)
                    {
                        Debug.LogWarning(
                            $"씬이 {MaxWaitFrames} 프레임 동안 안 줄었다 " +
                            $"({SceneManager.sceneCount} 개). 그대로 진행한다.");
                        yield break;
                    }

                    yield return null;
                }

                PeakScenes = Mathf.Max(PeakScenes, SceneManager.sceneCount - _sceneBaseline);
            }

            void Roll(float[] vector, BallState start, bool placed, List<BallCell> next)
            {
                _watch.Start();
                var result = _trial.RunSampled(vector, _buffer, RollSteps, start);
                _watch.Stop();
                Sims++;

                // 거부된 시행은 월드를 아예 안 만든다 — 씬도 안 쌓인다.
                if (result.Reject != PlacementReject.None)
                {
                    Rejected++;
                    return;
                }

                Worlds++;

                if (placed) _collector.CollectPlaced(_buffer);
                else _collector.CollectFree(_buffer);

                next.AddRange(TakeNew());
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
                    var sample = _buffer[i];
                    BallCell cell = _quantizer.Quantize(sample.Position, sample.Velocity);

                    if (_states.ContainsKey(cell)) continue;

                    _states[cell] = new BallState(sample.Position, sample.Velocity);
                    found.Add(cell);
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
