using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 지렛대가 공을 얼마나 띄우는지 격자로 훑어 본다.
    /// 자유도를 미리 고정하지 않는다 — 어느 축이 실제로 높이를
    /// 만드는지 모르는 채로 고정하면 그 값이 그대로 굳는다.
    /// </summary>
    public class LeverSweepTests
    {
        /// 6초. 가장 높은 낙차에서도 추가 닿고 공이 최고점까지 간다.
        const int Steps = 360;

        /// <summary>
        /// 추가 차지하는 자리. 무게를 줄 수로 정하므로 고정이다 —
        /// 상자가 같아야 무거운 추와 가벼운 추를 같은 자리에 놓고 견준다.
        /// </summary>
        static readonly Vector2 WeightSize = new Vector2(1f, 0.8f);

        /// 띄웠다고 볼 높이. 공 지름의 네 배다.
        const float Launched = 1f;

        /// 로그에 남길 상위 조합 수.
        const int Report = 30;

        static readonly Vector2 Fulcrum = Vector2.zero;

        [Test]
        public void 지렛대가_공을_띄우는_조합이_있다()
        {
            float[] ballArms = Axis(0.5f, 3f, 5);
            float[] weightArms = Axis(0.5f, 4f, 5);
            float[] angles = Axis(-0.4f, 0.4f, 3);
            int[] weightRows = { 1, 3, 5, 7 };

            // 낙차를 크게 넓힌다. 추가 늦게 닿아야 판이 충분히 기운다 —
            // 뒤로 튀는 것이 여기서 갈린다.
            float[] drops = Axis(0.5f, 10f, 6);

            var results = new List<Row>();

            for (int a = 0; a < ballArms.Length; a++)
            for (int w = 0; w < weightArms.Length; w++)
            for (int g = 0; g < angles.Length; g++)
            for (int m = 0; m < weightRows.Length; m++)
            for (int d = 0; d < drops.Length; d++)
            {
                var lever = new Lever(
                    Fulcrum, ballArms[a], weightArms[w],
                    angles[g], WeightSize, weightRows[m], drops[d]);

                results.Add(new Row(lever, Fly(lever)));
            }

            int launched = 0;
            for (int i = 0; i < results.Count; i++)
                if (results[i].Flight.Rise >= Launched) launched++;

            Debug.Log($"지렛대 실측 — {results.Count} 조합, "
                      + $"{Launched} 이상 띄운 것 {launched} 개");

            results.Sort((x, y) => y.Flight.Rise.CompareTo(x.Flight.Rise));
            float best = results[0].Flight.Rise;
            Debug.Log(Table("상승 큰 순", results));

            // 앞으로 보내는 조합을 따로 본다 — 상승만으로 줄을 세우면
            // 뒤로 넘겨버리는 것들이 표를 차지해 쓸 조합이 안 보인다.
            results.Sort((x, y) => y.Flight.RunAtApex.CompareTo(x.Flight.RunAtApex));
            Debug.Log(Table("수평 이동 큰 순", results));

            Assert.Greater(launched, 0,
                $"{results.Count} 개 조합 중 공을 {Launched} 이상 띄운 것이 없다. "
                + $"가장 높이 뜬 것이 {best:F3} 이다.");
        }

        static MeasureStage.Flight Fly(in Lever lever)
        {
            var solution = new Solution();
            lever.AppendTo(solution);

            return MeasureStage.Fly(
                ProbeStage.Empty(lever.BallSeat),
                solution,
                new BallState(lever.BallSeat, Vector2.zero),
                Steps);
        }

        /// <summary>min 에서 max 까지 count 개. count 가 1 이면 min 하나.</summary>
        static float[] Axis(float min, float max, int count)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = count == 1 ? min : Mathf.Lerp(min, max, (float)i / (count - 1));

            return values;
        }

        readonly struct Row
        {
            public readonly Lever Lever;
            public readonly MeasureStage.Flight Flight;

            public Row(in Lever lever, in MeasureStage.Flight flight)
            {
                Lever = lever;
                Flight = flight;
            }
        }

        static string Table(string title, List<Row> results)
        {
            var text = new StringBuilder();
            text.AppendLine($"[{title}]");
            text.AppendLine("ballArm weightArm angle rows mass drop | rise run outcome");

            int lines = Mathf.Min(Report, results.Count);
            for (int i = 0; i < lines; i++)
            {
                Lever lever = results[i].Lever;
                MeasureStage.Flight flight = results[i].Flight;

                text.AppendLine(
                    $"{lever.BallArm:F2} {lever.WeightArm:F2} {lever.Angle:F2} "
                    + $"{lever.WeightRows} {lever.Weight.Mass:F2} {lever.Drop:F2} | "
                    + $"{flight.Rise:F3} {flight.RunAtApex:F3} {flight.Outcome}");
            }

            return text.ToString();
        }
    }
}
