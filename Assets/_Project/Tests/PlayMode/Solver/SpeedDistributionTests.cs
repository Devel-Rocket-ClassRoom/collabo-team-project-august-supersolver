using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 기저 시뮬의 속력 분포를 찍어 크기 구간 경계의 근거를 남긴다.
    /// 산출물은 통과 여부가 아니라 로그에 찍히는 수치다 —
    /// 경계값을 바꿀 때 이걸 다시 돌려 보고 정한다.
    /// 플레이 영역 밖 표본은 셀이 되지 못하므로 따로 센다.
    /// </summary>
    public class SpeedDistributionTests
    {
        /// 매 스텝을 본다. 분포를 재는 것이지 맵을 만드는 게 아니다.
        const int Interval = 1;

        /// 히스토그램 최하단. 물리 슬립 허용치(0.01)보다 한 자리 아래다.
        const float MinBin = 0.001f;

        /// 한 칸이 2배. 등비 구간을 고르는 자리라 로그 축이 맞다.
        const float BinRatio = 2f;

        const int BinCount = 20;

        /// <summary>
        /// 클램프 구간이 가져도 되는 최대 몫.
        /// 넘으면 그 위 속력 차이가 전부 한 셀로 접혀
        /// 맵이 빠른 상태를 구분하지 못한다.
        /// </summary>
        const float MaxClampShare = 0.10f;

        static IEnumerable<TestCaseData> Levels()
        {
            yield return new TestCaseData("L001_Ramp", SampleLevelFile.Load());
            yield return new TestCaseData("L002_Feature", FeatureLevelFile.LoadLevel());
            yield return new TestCaseData("RampToGoal", TestLevels.RampToGoal());
            yield return new TestCaseData("LongRoll", TestLevels.LongRoll());
            yield return new TestCaseData("FlatRest", TestLevels.FlatRest());
            yield return new TestCaseData("Gap", TestLevels.Gap());
        }

        [TestCaseSource(nameof(Levels))]
        public void 기저_시뮬의_속력_분포를_찍는다(string name, LevelData level)
        {
            var report = Measure(name, level);

            Debug.Log(report.ToString());

            Assert.Greater(report.InArea.Count, 0, "영역 안 표본이 없다");
            Assert.Greater(report.InArea[report.InArea.Count - 1], SolverConfig.StopSpeed,
                "공이 움직이지 않은 판이라 분포를 논할 수 없다");
        }

        /// <summary>
        /// 레벨 전체를 한 번에 본다. 구간 경계는 레벨 하나가 아니라
        /// 여러 판을 합친 분포에서 정해야 한다 —
        /// 속력 상한은 낙하 높이가 정하고 그건 레벨마다 다르다.
        /// </summary>
        [Test]
        public void 합산_분포가_크기_구간_경계를_뒷받침한다()
        {
            var all = new List<float>();
            var text = new StringBuilder();

            foreach (var data in Levels())
            {
                var report = Measure((string)data.Arguments[0], (LevelData)data.Arguments[1]);
                all.AddRange(report.InArea);
                text.AppendLine(report.ToString());
            }

            all.Sort();
            text.AppendLine(Format("합산(영역 안)", all));
            text.AppendLine(FormatBands(all));

            Debug.Log(text.ToString());

            // 비는 구간이 있으면 그 상태 공간은 값만 치르고 안 쓰인다.
            for (int band = 1; band <= SolverConfig.SpeedBands; band++)
                Assert.Greater(CountInBand(all, band), 0, $"크기 구간 {band} 이 비었다");

            Assert.LessOrEqual(ClampShare(all), MaxClampShare,
                "최상단 구간에 표본이 몰린다 — 빠른 상태가 한 셀로 뭉개진다");
        }

        // ── 셀 대표 상태에서 굴린 분포 ──
        // 기저 시뮬은 중력만으로 돌아 빠른 쪽으로 치우친다.
        // 맵 빌드는 모든 셀에서 굴리므로 그쪽을 따로 잰다.

        /// 레벨당 굴려 볼 셀 수. 전수 비용은 C11 의 몫이다.
        const int SampledCells = 120;

        /// 셀 하나당 굴리는 스텝. 3초면 분포를 보기 충분하다.
        const int CellRollSteps = 180;

        /// 지형이 서로 다른 판만 고른다. 비탈·다단·평지.
        static IEnumerable<TestCaseData> CellLevels()
        {
            yield return new TestCaseData("L001_Ramp", SampleLevelFile.Load());
            yield return new TestCaseData("L002_Feature", FeatureLevelFile.LoadLevel());
            yield return new TestCaseData("FlatRest", TestLevels.FlatRest());
        }

        [Test]
        public void 셀_대표_상태에서_구른_분포가_구간_경계를_뒷받침한다()
        {
            var all = new List<float>();
            var text = new StringBuilder();

            foreach (var data in CellLevels())
            {
                string name = (string)data.Arguments[0];
                var level = (LevelData)data.Arguments[1];

                var speeds = MeasureFromCells(level, out int rolled, out int skipped);
                all.AddRange(speeds);

                text.AppendLine($"=== {name} — 셀 {rolled} 판 (지형에 박혀 건너뜀 {skipped}) ===");
                text.Append(Format("영역 안", speeds));
            }

            all.Sort();
            text.AppendLine(Format("합산(셀 대표 상태)", all));
            text.AppendLine(FormatBands(all));

            Debug.Log(text.ToString());

            for (int band = 1; band <= SolverConfig.SpeedBands; band++)
                Assert.Greater(CountInBand(all, band), 0, $"크기 구간 {band} 이 비었다");

            Assert.LessOrEqual(ClampShare(all), MaxClampShare,
                "최상단 구간에 표본이 몰린다 — 빠른 상태가 한 셀로 뭉개진다");
        }

        /// <summary>
        /// 셀을 고정 간격으로 훑어 대표 상태에서 무배치로 굴린다.
        /// 전수는 셀 수 × 후보 수라 감당이 안 되고, 고정 간격이라
        /// 실행마다 같은 표본이 나온다.
        /// </summary>
        static List<float> MeasureFromCells(LevelData level, out int rolled, out int skipped)
        {
            var quantizer = new BallQuantizer(SolverConfig.PositionStep(level));
            var grid = new BallGrid(level, quantizer);
            var area = LevelDataArea.Calculate(level);
            var trial = new PrimitiveTrial(level, seed: 0);
            var buffer = new TrajectoryBuffer(Interval, CellRollSteps);

            var speeds = new List<float>();
            rolled = 0;
            skipped = 0;

            foreach (var cell in SampleCells(grid))
            {
                BallState start = quantizer.Dequantize(cell);

                // 벽 안에서 출발한 공은 밀려나며 튀어 분포를 오염시킨다.
                if (BallSpawn.Blocked(level, start.Position))
                {
                    skipped++;
                    continue;
                }

                trial.RunSampled(new float[0], buffer, CellRollSteps, start);
                rolled++;

                for (int i = 0; i < buffer.Count; i++)
                {
                    if (area.Contains(buffer[i].Position))
                        speeds.Add(buffer[i].Velocity.magnitude);
                }
            }

            speeds.Sort();
            return speeds;
        }

        static IEnumerable<BallCell> SampleCells(BallGrid grid)
        {
            var velocities = new List<Vector2Int>(VelocityCells());
            int total = grid.Columns * grid.Rows * velocities.Count;
            int stride = Mathf.Max(1, total / SampledCells);

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

        static int CountInBand(List<float> sorted, int band)
        {
            int above = CountBelow(sorted, SolverConfig.BandFloor(band));
            return band == SolverConfig.SpeedBands
                ? sorted.Count - above
                : CountBelow(sorted, SolverConfig.BandFloor(band + 1)) - above;
        }

        static float ClampShare(List<float> sorted)
            => (float)CountInBand(sorted, SolverConfig.SpeedBands) / sorted.Count;

        /// <summary>실제 구간 경계로 다시 센 것. 히스토그램 칸과 경계가 다르다.</summary>
        static string FormatBands(List<float> sorted)
        {
            var text = new StringBuilder();
            text.AppendLine($"[크기 구간] 정지 {CountBelow(sorted, SolverConfig.StopSpeed)} / {sorted.Count}");

            for (int band = 1; band <= SolverConfig.SpeedBands; band++)
            {
                string upper = band == SolverConfig.SpeedBands
                    ? "      ∞"
                    : $"{SolverConfig.BandFloor(band + 1),7:F3}";
                text.AppendLine(
                    $"  {band} [{SolverConfig.BandFloor(band),6:F3}, {upper}) " +
                    Bar(CountInBand(sorted, band), sorted.Count));
            }

            return text.ToString();
        }

        static Report Measure(string name, LevelData level)
        {
            var area = LevelDataArea.Calculate(level);
            var buffer = new TrajectoryBuffer(Interval);
            var trial = new PrimitiveTrial(level, seed: 0).RunSampled(new float[0], buffer);

            var all = new List<float>(buffer.Count);
            var inArea = new List<float>(buffer.Count);

            for (int i = 0; i < buffer.Count; i++)
            {
                float speed = buffer[i].Velocity.magnitude;
                all.Add(speed);
                if (area.Contains(buffer[i].Position))
                    inArea.Add(speed);
            }

            all.Sort();
            inArea.Sort();

            return new Report(name, trial.Sim, all, inArea);
        }

        readonly struct Report
        {
            public readonly string Name;
            public readonly SimResult Sim;
            public readonly List<float> All;
            public readonly List<float> InArea;

            public Report(string name, SimResult sim, List<float> all, List<float> inArea)
            {
                Name = name;
                Sim = sim;
                All = all;
                InArea = inArea;
            }

            public override string ToString()
            {
                var text = new StringBuilder();
                text.AppendLine(
                    $"=== {Name} — {Sim.Outcome} {Sim.EndStep}스텝, " +
                    $"표본 {All.Count} (영역 안 {InArea.Count}) ===");
                text.AppendLine(Format("전체", All));
                text.Append(Format("영역 안", InArea));
                return text.ToString();
            }
        }

        /// <summary>
        /// 백분위수와 로그 히스토그램. 경계를 고를 때 보는 건
        /// 평균이 아니라 꼬리라 백분위수를 함께 낸다.
        /// </summary>
        static string Format(string label, List<float> sorted)
        {
            if (sorted.Count == 0)
                return $"[{label}] 표본 없음";

            var text = new StringBuilder();
            text.AppendLine(
                $"[{label}] n={sorted.Count} " +
                $"p50={Percentile(sorted, 0.50f):F3} p75={Percentile(sorted, 0.75f):F3} " +
                $"p90={Percentile(sorted, 0.90f):F3} p95={Percentile(sorted, 0.95f):F3} " +
                $"p99={Percentile(sorted, 0.99f):F3} max={sorted[sorted.Count - 1]:F3}");

            int zero = CountBelow(sorted, MinBin);
            text.AppendLine($"  [    0, {MinBin:F3}) {Bar(zero, sorted.Count)}");

            float low = MinBin;
            for (int i = 0; i < BinCount; i++)
            {
                float high = low * BinRatio;
                int count = CountBelow(sorted, high) - CountBelow(sorted, low);
                if (count > 0)
                    text.AppendLine($"  [{low,6:F3}, {high,7:F3}) {Bar(count, sorted.Count)}");
                low = high;
            }

            int over = sorted.Count - CountBelow(sorted, low);
            if (over > 0)
                text.AppendLine($"  [{low,6:F3},      ∞) {Bar(over, sorted.Count)}");

            return text.ToString();
        }

        static string Bar(int count, int total)
        {
            float share = (float)count / total;
            return $"{count,6} {share,6:P2} {new string('#', Mathf.RoundToInt(share * 60f))}";
        }

        /// 오름차순 목록에서 value 미만인 표본 수.
        static int CountBelow(List<float> sorted, float value)
        {
            int index = sorted.BinarySearch(value);
            if (index >= 0)
            {
                // 같은 값이 여럿이면 가장 앞으로 물린다.
                while (index > 0 && sorted[index - 1] >= value) index--;
                return index;
            }
            return ~index;
        }

        static float Percentile(List<float> sorted, float ratio)
        {
            int index = Mathf.Clamp(
                Mathf.FloorToInt(ratio * (sorted.Count - 1)), 0, sorted.Count - 1);
            return sorted[index];
        }
    }
}
