using System;
using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 완료조건 5-1. 점 밀도가 샘플레이트를 타면 안 된다.
    /// 레이트를 2배로 올려도 점 개수가 2배로 가지 않고
    /// 곡률이 정한 값에 수렴하는지를 본다.
    /// </summary>
    public class StrokePipelineTests
    {
        /// 실기 터치 패널이 보고하는 범위. 480 은 수렴 확인용.
        static readonly int[] SampleRates = { 120, 240, 480 };

        /// 레이트에 비례하면 2.0 이 된다. 정수 양자화로
        /// 한두 점이 갈리는 건 여기 안에서 흡수한다.
        const float MaxCountRatio = 1.5f;

        [Test]
        public void 빠른_직선의_점_개수는_샘플레이트를_타지_않는다()
        {
            AssertCountConverges("빠른 직선", 0.5f,
                t => Vector2.Lerp(new Vector2(-3f, -1f), new Vector2(3f, 1f), t));
        }

        [Test]
        public void 반원의_점_개수는_샘플레이트를_타지_않는다()
        {
            AssertCountConverges("반원", 1f, Arc);
        }

        [Test]
        public void 빠른_반원의_점_개수는_샘플레이트를_타지_않는다()
        {
            AssertCountConverges("빠른 반원", 0.35f, Arc);
        }

        [Test]
        public void 사인파의_점_개수는_샘플레이트를_타지_않는다()
        {
            AssertCountConverges("사인파", 1.5f, Sine);
        }

        [Test]
        public void 지그재그의_점_개수는_샘플레이트를_타지_않는다()
        {
            AssertCountConverges("지그재그", 1f, Zigzag);
        }

        [Test]
        public void 확정된_점_간격은_어떤_샘플레이트에서도_최소_거리_이상이다()
        {
            // 밀도의 진짜 상한. 이게 깨지면 필터가 아니라
            // 샘플레이트가 점 개수를 정하고 있다는 뜻이다.
            // 60Hz 를 포함해 하한까지 본다.
            int[] rates = { 60, 120, 240, 480 };

            foreach (int hz in rates)
            foreach (var stroke in AllTrajectories(hz))
            {
                var points = stroke.Points;
                for (int i = 1; i < points.Count; i++)
                {
                    float gap = Vector2.Distance(points[i - 1], points[i]);
                    Assert.GreaterOrEqual(gap, WorldMetrics.MinPointDistance - 0.0001f,
                        $"{hz}Hz — {i - 1}번과 {i}번 점이 {gap:F4}wu 밖에 안 떨어져 있다.");
                }
            }
        }

        [Test]
        public void 파이프라인_전체가_같은_입력에_대해_비트_일치한다()
        {
            var samples = Sample(1f, 240, Arc);

            var first = Run(samples);
            var second = Run(samples);

            Assert.AreEqual(first.Points.Count, second.Points.Count);
            for (int i = 0; i < first.Points.Count; i++)
            {
                Assert.IsTrue(first.Points[i].x.Equals(second.Points[i].x));
                Assert.IsTrue(first.Points[i].y.Equals(second.Points[i].y));
            }
        }

        static void AssertCountConverges(string name, float seconds, Func<float, Vector2> path)
        {
            int min = int.MaxValue;
            int max = 0;
            var counts = new List<int>();

            foreach (int hz in SampleRates)
            {
                int count = Run(Sample(seconds, hz, path)).Points.Count;
                counts.Add(count);
                if (count < min) min = count;
                if (count > max) max = count;
            }

            Assert.LessOrEqual(max / (float)min, MaxCountRatio,
                $"{name} — {string.Join("/", counts)}점 ({string.Join("/", SampleRates)}Hz)");
        }

        static IEnumerable<Stroke> AllTrajectories(int hz)
        {
            yield return Run(Sample(0.5f, hz, t => Vector2.Lerp(new Vector2(-3f, -1f), new Vector2(3f, 1f), t)));
            yield return Run(Sample(0.35f, hz, Arc));
            yield return Run(Sample(1.5f, hz, Sine));
            yield return Run(Sample(1f, hz, Zigzag));
        }

        static Vector2 Arc(float t) =>
            new Vector2(Mathf.Cos(t * Mathf.PI) * 2f, Mathf.Sin(t * Mathf.PI) * 2f);

        static Vector2 Sine(float t) =>
            new Vector2(-3f + t * 6f, Mathf.Sin(t * Mathf.PI * 4f) * 0.5f);

        static Vector2 Zigzag(float t) =>
            new Vector2(-3f + t * 6f, Mathf.Abs((t * 8f) % 2f - 1f) * 0.8f);

        static List<Vector2> Sample(float seconds, int hz, Func<float, Vector2> path)
        {
            int steps = Mathf.RoundToInt(seconds * hz);
            var samples = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
                samples.Add(path(i / (float)steps));
            return samples;
        }

        static Stroke Run(List<Vector2> samples)
        {
            var builder = new StrokeBuilder();
            builder.Begin(1000f);
            for (int i = 0; i < samples.Count; i++)
                builder.AddPoint(samples[i]);

            return new StrokeProcessor().Process(ToolType.FixedLine, builder.Points);
        }
    }
}
