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
        static readonly int RollSteps = SolverConfig.RollSteps;

        static readonly int TrajectoryInterval = SolverConfig.TrajectoryInterval;

        /// <summary>
        /// 안전장치. 넘으면 그 자리에서 멈추고 곡선만 보고한다 —
        /// 폭주하는지 아닌지가 알고 싶은 것이지
        /// 끝까지 도는 것이 목적이 아니다.
        /// </summary>
        /// L002 는 위치 셀이 L001 의 2.75배라 상한을 넉넉히 잡는다.
        const int MaxSims = 3000000;

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

        /// <summary>
        /// 증가 배수가 2.4 → 1.8 → 1.2 로 꺾이는 중이다.
        /// 꼬리가 이어지는지 보려면 한 홉 더 봐야 한다.
        /// SolverConfig.MapDepth 는 이 곡선을 보고 정한다.
        /// </summary>
        const int MaxDepth = 5;

        /// 기본 상한 3분으로는 못 끝낸다. 일회성 측정이라 넉넉히 준다.
        [UnityTest, Timeout(3600000)]
        public IEnumerator L001_비탈의_도달_가능_셀_곡선()
            => Measure(SampleLevelFile.Load());

        /// <summary>
        /// L001 은 공이 비탈에 붙어 굴러 공중 구간이 없다.
        /// 이 판은 낙하·틈·폭탄이 있어 정반대 지형이다 —
        /// 정한 값들이 L001 에 과적합됐는지 여기서 드러난다.
        /// </summary>
        [UnityTest, Timeout(3600000)]
        public IEnumerator L002_낙하와_틈의_도달_가능_셀_곡선()
            => Measure(FeatureLevelFile.LoadLevel());

        /// <summary>
        /// 유일하게 풀 것이 있는 판. L001·L002 는 그냥 굴려도 Clear 라
        /// 가지치기가 첫 홉에서 전부 잘라내 폐포가 성립하지 않는다.
        /// </summary>
        [UnityTest, Timeout(3600000)]
        public IEnumerator 퍼즐_레벨의_도달_가능_셀_곡선()
            => Measure(TestLevels.GapPuzzle());

        static IEnumerator Measure(LevelData level)
        {
            var quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            var grid = new BallGrid(level, quantizer);

            long allCells = (long)grid.CellCount * SolverConfig.VelocityCellCount;

            // 깊이마다 바로 찍는다. 끝에 몰아서 찍으면
            // 상한이나 타임아웃에 걸릴 때 측정이 통째로 날아간다.
            Debug.Log($"[격자] 전체 셀 {allCells:N0} " +
                      $"(위치 {grid.CellCount} × 속도 {SolverConfig.VelocityCellCount})\n" +
                      $"[설정] 후보 {new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps).Count} 개 " +
                      $"(방향 {SolverConfig.CandidateDirections} · 크기 {SolverConfig.CandidateSizeSteps}) · " +
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

                // 위 줄은 누적, 아래 표는 이 깊이만.
                Debug.Log(
                    $"깊이 {depth}: 셀 {explorer.CellCount,7:N0} " +
                    $"({(double)explorer.CellCount / allCells:P2}) " +
                    $"새 셀 {frontier.Count,7:N0} · " +
                    $"누적 시뮬 {explorer.Sims,9:N0} (월드 {explorer.Worlds,9:N0}) · " +
                    $"간선 {explorer.Edges,8:N0} · " +
                    $"{explorer.Elapsed.TotalSeconds,6:F1}초 " +
                    $"(월드당 {explorer.MsPerWorld:F2}ms · 씬 최고 {explorer.PeakScenes})\n" +
                    explorer.Depth.Report());

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
            readonly IdleCutoff _idle;

            readonly Primitive[] _one = new Primitive[1];

            /// 버퍼는 배치 시뮬이 덮어쓰므로 후보를 미리 떠 둔다.
            readonly List<Primitive> _candidateBuffer = new List<Primitive>();

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

            /// 실제로 만든 물리 씬 수. 메모리를 먹는 것은 이쪽이다.
            public int Worlds { get; private set; }

            /// 동시에 로드돼 있던 시뮬 씬의 최고점. 상한이 먹히는지 본다.
            public int PeakScenes { get; private set; }

            static readonly int ShapeCount =
                System.Enum.GetValues(typeof(PrimitiveShape)).Length;

            /// <summary>
            /// 이번 깊이만의 성적표. 누적으로 재면 판이 몰린 깊은 쪽이
            /// 얕은 쪽을 덮어써서 값이 어디서 왔는지 안 보인다.
            /// </summary>
            public DepthStats Depth { get; private set; } = new DepthStats();

            public sealed class DepthStats
            {
                /// 이 깊이에서 확장한 프론티어 셀 수.
                public int Cells;

                /// 무배치가 이미 이겨서 후보를 건너뛴 셀 수.
                public int Pruned;

                public int Sims, Worlds, Rejected, Unchanged;

                /// 무배치 판만 따로 센다. 가지치기 절감폭이 여기서 나온다.
                public int FreeWorlds, FreeCleared;

                // 판이 왜 끝났는지. 판마다 사유는 하나뿐이다.
                public int Cleared, Failed, Stalled, Idled, Capped;

                // 헛방은 순서를 안 타지만 새 셀은 먼저 닿은 후보가 가져간다.
                public readonly int[] DirWorlds = new int[SolverConfig.CandidateDirections];
                public readonly int[] DirUnchanged = new int[SolverConfig.CandidateDirections];
                public readonly int[] DirNew = new int[SolverConfig.CandidateDirections];

                public readonly int[] ShapeWorlds = new int[ShapeCount];
                public readonly int[] ShapeUnchanged = new int[ShapeCount];
                public readonly int[] ShapeNew = new int[ShapeCount];

                public string Report()
                {
                    var text = new System.Text.StringBuilder();

                    text.AppendLine(
                        $"  [이 깊이] 프론티어 {Cells:N0} · 가지침 {Share(Pruned, Cells)} · " +
                        $"시뮬 {Sims:N0} (월드 {Worlds:N0}) · " +
                        $"거부 {Share(Rejected, Sims)} · 헛방 {Share(Unchanged, Worlds)}");

                    text.AppendLine(
                        $"  [종료] 클리어 {Share(Cleared, Worlds)} · 실패 {Share(Failed, Worlds)} · " +
                        $"잠듦 {Share(Stalled, Worlds)} · 정체 {Share(Idled, Worlds)} · " +
                        $"상한 {Share(Capped, Worlds)}");

                    text.AppendLine(
                        $"  [무배치] {FreeWorlds:N0} 판 · 클리어 {Share(FreeCleared, FreeWorlds)}" +
                        " ← 이 비율만큼 후보를 건너뛴다");

                    // 진행 방향 기준 상대각이다. 0° 가 정면.
                    text.AppendLine("  방향   월드      헛방     새 셀");
                    for (int d = 0; d < DirWorlds.Length; d++)
                    {
                        float degrees = DirWorlds.Length == 1
                            ? 0f
                            : -90f + 180f * d / (DirWorlds.Length - 1);
                        text.AppendLine(
                            $"  {degrees,+5:F0}° {DirWorlds[d],8:N0} " +
                            $"{Share(DirUnchanged[d], DirWorlds[d]),8} {DirNew[d],9:N0}");
                    }

                    text.AppendLine("  Shape         월드      헛방     새 셀");
                    for (int s = 0; s < ShapeWorlds.Length; s++)
                        text.AppendLine(
                            $"  {(PrimitiveShape)s,-10} {ShapeWorlds[s],8:N0} " +
                            $"{Share(ShapeUnchanged[s], ShapeWorlds[s]),8} {ShapeNew[s],9:N0}");

                    return text.ToString();
                }

                static string Share(int part, int whole)
                    => whole == 0 ? "-" : $"{(float)part / whole:P0}";
            }

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
                _candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
                _codec = new PrimitiveCodec(level);
                _trial = new PrimitiveTrial(level, seed: 0);
                _buffer = new TrajectoryBuffer(TrajectoryInterval, RollSteps);
                _collector = new CellEdgeCollector(quantizer);

                // 반경이 위치 셀 폭이라 "셀 하나도 못 벗어났다"가 된다.
                _idle = new IdleCutoff(quantizer.PositionStep, SolverConfig.IdleSteps);
            }

            /// <summary>
            /// 씨앗은 아무것도 안 놓고 굴린 기저 궤적이다.
            /// 프리미티브 0 개로 닿는 곳이라 깊이 0 이다.
            /// </summary>
            public List<BallCell> Seed()
            {
                _watch.Start();
                _trial.RunSampled(new float[0], _buffer, RollSteps, null, _idle);
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
                Depth = new DepthStats { Cells = frontier.Count };

                foreach (var cell in frontier)
                {
                    BallState start = _states[cell];
                    Vector2 center = _quantizer.Dequantize(cell).Position;

                    // 셀 중심을 대표로 썼다면 잃었을 셀. 실제 표본은 안 막힌다.
                    if (BallSpawn.Blocked(_level, center)) CenterBlocked++;

                    TrialResult free = Roll(new float[0], start, placed: false, next);

                    var drain = Drain();
                    while (drain.MoveNext()) yield return drain.Current;

                    // 선 없이 이미 이기면 h = 0 이다. 더 놓아도 나아질 수 없고,
                    // 이 상태를 지나는 풀이는 그 시점에 이미 끝난 것이다.
                    // 무배치 판은 남긴다 — 비용 0 간선이 h=0 을 뒤로 전파한다.
                    if (free.Cleared)
                    {
                        Depth.Pruned++;
                        continue;
                    }

                    // 후보는 공을 둘러싼 고리 위다. 공 자리에 놓으면 박힌다.
                    _candidateBuffer.Clear();
                    _candidateBuffer.AddRange(_candidates.At(start));

                    foreach (var candidate in _candidateBuffer)
                    {
                        _one[0] = candidate;

                        int before = next.Count;
                        TrialResult placed = Roll(_codec.Encode(_one), start, placed: true, next);

                        if (placed.Reject == PlacementReject.None)
                            Tally(candidate, start, next.Count - before,
                                  Same(placed.Sim, free.Sim));

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

            TrialResult Roll(float[] vector, BallState start, bool placed, List<BallCell> next)
            {
                _watch.Start();
                var result = _trial.RunSampled(vector, _buffer, RollSteps, start, _idle);
                _watch.Stop();
                Sims++;
                Depth.Sims++;

                // 거부된 시행은 월드를 아예 안 만든다 — 씬도 안 쌓인다.
                if (result.Reject != PlacementReject.None)
                {
                    Depth.Rejected++;
                    return result;
                }

                Worlds++;
                Depth.Worlds++;
                Ending(result.Sim);

                if (placed)
                {
                    _collector.CollectPlaced(_buffer);
                }
                else
                {
                    _collector.CollectFree(_buffer);
                    Depth.FreeWorlds++;
                    if (result.Cleared) Depth.FreeCleared++;
                }

                next.AddRange(TakeNew());
                return result;
            }

            /// 판이 왜 끝났는지. Timeout 인데 상한 전이면 정체로 끊은 것이다.
            void Ending(in SimResult sim)
            {
                switch (sim.Outcome)
                {
                    case SimOutcome.Clear: Depth.Cleared++; return;
                    case SimOutcome.Fail: Depth.Failed++; return;
                    case SimOutcome.Stalled: Depth.Stalled++; return;
                }

                if (sim.EndStep >= RollSteps) Depth.Capped++;
                else Depth.Idled++;
            }

            /// <summary>
            /// 어느 방향·Shape 이 값을 하는지 센다.
            /// 방향은 후보를 만든 순서가 아니라 좌표에서 되짚는다 —
            /// 생성 순서에 기대면 순서만 바뀌어도 조용히 어긋난다.
            /// </summary>
            void Tally(in Primitive candidate, in BallState ball, int found, bool unchanged)
            {
                int dir = DirectionOf(candidate.Center, ball);
                int shape = (int)candidate.Shape;

                Depth.DirWorlds[dir]++;
                Depth.DirNew[dir] += found;
                Depth.ShapeWorlds[shape]++;
                Depth.ShapeNew[shape] += found;

                if (!unchanged) return;

                Depth.Unchanged++;
                Depth.DirUnchanged[dir]++;
                Depth.ShapeUnchanged[shape]++;
            }

            static int DirectionOf(Vector2 center, in BallState ball)
            {
                int count = SolverConfig.CandidateDirections;
                if (count == 1) return 0;

                // 생성기와 같은 기준을 써야 한다 — 멈춘 공은 아래쪽.
                float facing =
                    ball.Velocity.sqrMagnitude > SolverConfig.StopSpeed * SolverConfig.StopSpeed
                        ? Mathf.Atan2(ball.Velocity.y, ball.Velocity.x)
                        : -Mathf.PI * 0.5f;

                float theta = Mathf.Atan2(center.y - ball.Position.y, center.x - ball.Position.x);
                float offset = Mathf.Repeat(theta - facing + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;

                int index = Mathf.RoundToInt(
                    (offset + Mathf.PI * 0.5f) / (Mathf.PI / (count - 1)));
                return Mathf.Clamp(index, 0, count - 1);
            }

            /// 배치가 궤적을 건드리지 않으면 결과가 글자 그대로 같다.
            static bool Same(in SimResult a, in SimResult b)
                => a.Outcome == b.Outcome && a.EndStep == b.EndStep && a.MinGoalDist == b.MinGoalDist;


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

        }
    }
}
