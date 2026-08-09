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
    /// 맵 빌드가 감당 가능한 규모인지 잰다.
    /// 산출물은 통과 여부가 아니라 로그의 수치이고,
    /// 그 수치로 C10 이 선빌드냐 지연 생성이냐를 고른다.
    /// 빌더를 다 만든 뒤 규모가 안 되면 구조를 다시 짜야 한다.
    /// </summary>
    public class MapBuildCostTests
    {
        /// 비용을 재려고 실제로 굴리는 판 수.
        /// 전수는 이 측정 자체가 맵 빌드가 되어 버린다.
        const int SampleRolls = 150;

        /// <summary>
        /// 셀 하나를 굴리는 스텝 상한. **C10 이 정할 값이라 가정이다.**
        /// 시간은 스텝 수에 거의 비례하므로 스텝당 값도 함께 낸다.
        /// </summary>
        const int RollSteps = 180;

        /// 궤적 표본 간격. 이것도 C10 소관이며 간선 수를 직접 좌우한다.
        const int TrajectoryInterval = 10;

        /// 후보 격자의 Size 축 분할. 후보 수의 배율이라 따로 뽑아 본다.
        static readonly int[] SizeStepsToReport = { 1, 2, 4 };

        /// 실제로 굴려 볼 때 쓰는 분할.
        const int SizeSteps = 4;

        static IEnumerable<TestCaseData> Levels()
        {
            yield return new TestCaseData("L001_Ramp", SampleLevelFile.Load());
            yield return new TestCaseData("L002_Feature", FeatureLevelFile.LoadLevel());
        }

        [Test]
        public void 맵_빌드_비용을_실측한다()
        {
            var text = new StringBuilder();

            text.AppendLine("[가정] 아래 세 값은 C10 이 정한다. 지금은 가정치다.");
            text.AppendLine($"  굴림 스텝 상한 {RollSteps} · 궤적 간격 {TrajectoryInterval} · " +
                            "후보 배치 위치 = 셀 중심");
            text.AppendLine();

            foreach (var data in Levels())
            {
                var measured = Measure((string)data.Arguments[0], (LevelData)data.Arguments[1]);
                text.AppendLine(measured.Text);

                // 측정이 헛돌면 로그만 비고 조용히 초록불이 된다.
                Assert.Greater(measured.Rolled, 0, "한 판도 굴리지 못했다");
                Assert.Greater(measured.MsPerRoll, 0d, "시간이 0 으로 측정됐다");
                Assert.Greater(measured.Edges, 0, "간선이 하나도 안 모였다");
            }

            Debug.Log(text.ToString());
        }

        readonly struct Measurement
        {
            public readonly string Text;
            public readonly int Rolled;
            public readonly int Edges;
            public readonly double MsPerRoll;

            public Measurement(string text, int rolled, int edges, double msPerRoll)
            {
                Text = text;
                Rolled = rolled;
                Edges = edges;
                MsPerRoll = msPerRoll;
            }
        }

        static Measurement Measure(string name, LevelData level)
        {
            var quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            var grid = new BallGrid(level, quantizer);
            var codec = new PrimitiveCodec(level);
            var trial = new PrimitiveTrial(level, seed: 0);
            var buffer = new TrajectoryBuffer(TrajectoryInterval, RollSteps);
            var collector = new CellEdgeCollector(quantizer);

            var text = new StringBuilder();
            text.AppendLine($"=== {name} ===");

            // ── 셀 수 ──
            // 막힘은 위치에만 달렸으므로 표본이 아니라 전수로 센다.
            int blocked = 0;
            for (int cx = 0; cx < grid.Columns; cx++)
            {
                for (int cy = 0; cy < grid.Rows; cy++)
                {
                    var cell = new BallCell(grid.MinX + cx, grid.MinY + cy, 0, 0);
                    if (BallSpawn.Blocked(level, quantizer.Dequantize(cell).Position))
                        blocked++;
                }
            }

            int usablePositions = grid.CellCount - blocked;
            long usableCells = (long)usablePositions * SolverConfig.VelocityCellCount;

            text.AppendLine(
                $"[격자] 폭 {quantizer.PositionStep:F3} · 위치 {grid.Columns}×{grid.Rows}={grid.CellCount}" +
                $" · 속도 {SolverConfig.VelocityCellCount}");
            text.AppendLine(
                $"[막힘] 위치 셀 {blocked}/{grid.CellCount} ({(float)blocked / grid.CellCount:P1})" +
                $" → 유효 셀 {usableCells:N0}");

            // ── 후보 수 ──
            var sizes = new StringBuilder();
            foreach (int division in SizeStepsToReport)
                sizes.Append($" s={division}→{new PrimitiveCandidates(level, division).Count}");
            text.AppendLine($"[후보] Size 분할별 개수:{sizes}");

            // ── 시뮬 1회 ──
            var candidateGrid = new PrimitiveCandidates(level, SizeSteps);
            var candidates = new List<Primitive>();
            var watch = new Stopwatch();
            var growth = new StringBuilder();

            long simSteps = 0;
            int rolled = 0;
            int rejected = 0;
            int index = 0;

            foreach (var cell in SampleCells(grid))
            {
                BallState start = quantizer.Dequantize(cell);
                if (BallSpawn.Blocked(level, start.Position)) continue;

                candidates.Clear();
                candidates.AddRange(candidateGrid.At(start));

                float[] vector = codec.Encode(new[] { candidates[index % candidates.Count] });

                watch.Start();
                var result = trial.RunSampled(vector, buffer, RollSteps, start);
                watch.Stop();

                if (result.Reject != PlacementReject.None) rejected++;
                else simSteps += result.Sim.EndStep;

                collector.CollectPlaced(buffer);
                rolled++;
                index++;

                // 간선이 포화하는지 보려면 누적 곡선이 있어야 한다.
                if (rolled % 25 == 0)
                    growth.Append($" {rolled}판→{collector.Edges.Count}");

                if (rolled >= SampleRolls) break;
            }

            double msPerRoll = watch.Elapsed.TotalMilliseconds / rolled;
            double avgSteps = rolled == rejected ? 0 : (double)simSteps / (rolled - rejected);

            text.AppendLine(
                $"[시뮬] {rolled} 판 · 거부 {rejected} ({(float)rejected / rolled:P1}, 시뮬 안 함)" +
                $" · 평균 {avgSteps:F0} 스텝 · 1판 {msPerRoll:F2} ms");
            text.AppendLine($"[간선] 누적{growth} (판당 새 간선 {(float)collector.Edges.Count / rolled:F1})");

            // ── 추정 ──
            int candidateCount = candidateGrid.Count;
            double liveRate = 1.0 - (double)rejected / rolled;
            double sims = usableCells * (candidateCount + 1.0);

            // 표본의 거부 비율이 전체와 같다고 보므로
            // 판당 평균에 거부분이 이미 섞여 있다. 또 곱하면 두 번 깎인다.
            double hours = sims * msPerRoll / 1000.0 / 3600.0;
            double edges = sims * ((double)collector.Edges.Count / rolled);

            text.AppendLine(
                $"[추정] s={SizeSteps} 기준 시뮬 {sims:N0} 회 (거부 제외 {sims * liveRate:N0})");
            text.AppendLine($"       예상 시간 {hours:N1} 시간 (단일 스레드)");
            text.AppendLine(
                $"       예상 간선 {edges:N0} 개 × {BytesPerEdge} B ≈ {edges * BytesPerEdge / 1e9:N1} GB" +
                " (중복 제거 전 상한)");

            return new Measurement(text.ToString(), rolled, collector.Edges.Count, msPerRoll);
        }

        /// <summary>
        /// Dictionary&lt;CellEdge,int&gt; 항목 하나의 대략적 크기.
        /// 키 32(int 8개) + 값 4 + 해시 4 + 링크 4, 버킷·여유분 포함.
        /// </summary>
        const int BytesPerEdge = 56;

        /// 고정 간격으로 훑어 실행마다 같은 표본을 뽑는다.
        static IEnumerable<BallCell> SampleCells(BallGrid grid)
        {
            var velocities = new List<Vector2Int>(VelocityCells());
            int total = grid.CellCount * velocities.Count;
            int stride = Mathf.Max(1, total / (SampleRolls * 2));

            int index = 0;
            for (int cx = 0; cx < grid.Columns; cx++)
            {
                for (int cy = 0; cy < grid.Rows; cy++)
                {
                    foreach (var velocity in velocities)
                    {
                        if (index++ % stride != 0) continue;
                        yield return new BallCell(
                            grid.MinX + cx, grid.MinY + cy, velocity.x, velocity.y);
                    }
                }
            }
        }

        /// 속도 축의 셀 전부. 정지 1 + 방향 × 크기.
        static IEnumerable<Vector2Int> VelocityCells()
        {
            yield return Vector2Int.zero;

            for (int mag = 1; mag <= SolverConfig.SpeedBands; mag++)
                for (int dir = 0; dir < SolverConfig.VelocityDirections; dir++)
                    yield return new Vector2Int(dir, mag);
        }
    }
}
