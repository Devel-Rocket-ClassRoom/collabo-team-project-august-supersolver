using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>확정 단계. RDP 와 최소 길이 판정.</summary>
    public class StrokeProcessorTests
    {
        static Stroke Process(params Vector2[] points) =>
            new StrokeProcessor().Process(ToolType.FixedLine, new List<Vector2>(points));

        [Test]
        public void 직선_위의_중간점은_전부_사라진다()
        {
            var stroke = Process(
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1.5f, 0f),
                new Vector2(2f, 0f));

            Assert.AreEqual(2, stroke.Points.Count);
            Assert.AreEqual(new Vector2(0f, 0f), stroke.Points[0]);
            Assert.AreEqual(new Vector2(2f, 0f), stroke.Points[1]);
        }

        [Test]
        public void 코너는_남는다()
        {
            var stroke = Process(
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 1f));

            Assert.AreEqual(3, stroke.Points.Count);
            Assert.AreEqual(new Vector2(1f, 0f), stroke.Points[1]);
        }

        [Test]
        public void 허용_오차보다_얕은_돌출은_지워진다()
        {
            var stroke = Process(
                new Vector2(0f, 0f),
                new Vector2(1f, DrawConstants.RdpEpsilon * 0.5f),
                new Vector2(2f, 0f));

            Assert.AreEqual(2, stroke.Points.Count);
        }

        [Test]
        public void 최소_길이_미만이면_무효를_반환한다()
        {
            var stroke = Process(
                new Vector2(0f, 0f),
                new Vector2(0.1f, 0f),
                new Vector2(0.2f, 0f));

            Assert.IsFalse(stroke.IsValid);
        }

        [Test]
        public void 점이_모자라면_무효를_반환한다()
        {
            Assert.IsFalse(new StrokeProcessor().Process(ToolType.FixedLine, null).IsValid);
            Assert.IsFalse(Process(new Vector2(1f, 1f)).IsValid);
        }

        [Test]
        public void 닫힌_획도_양_끝이_같은_채로_처리된다()
        {
            // 자기 교차·닫힌 도형이 허용돼 있어
            // 첫 점 == 끝 점이 실제로 들어온다.
            var stroke = Process(
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0f));

            Assert.AreEqual(5, stroke.Points.Count);
            Assert.AreEqual(stroke.Points[0], stroke.Points[4]);
        }

        [Test]
        public void 단순화는_길이를_늘리지_않는다()
        {
            // 점을 지우기만 하므로 확정 길이가 잉크
            // 상한을 넘는 일은 구조적으로 없다.
            var raw = new List<Vector2>();
            for (int i = 0; i <= 60; i++)
            {
                float t = i / 60f;
                raw.Add(new Vector2(t * 4f, Mathf.Sin(t * Mathf.PI * 6f) * 0.3f));
            }

            float rawLength = new Stroke(ToolType.FixedLine, raw).Length();
            float simplified = new StrokeProcessor().Process(ToolType.FixedLine, raw).Length();

            Assert.LessOrEqual(simplified, rawLength + 0.0001f);
        }

        [Test]
        public void 같은_입력_두_번은_비트_단위로_같다()
        {
            var raw = new List<Vector2>();
            for (int i = 0; i <= 80; i++)
            {
                float t = i / 80f;
                raw.Add(new Vector2(Mathf.Cos(t * 5f) * 1.7f, Mathf.Sin(t * 3f) * 2.3f));
            }

            var first = new StrokeProcessor().Process(ToolType.FixedLine, raw);
            var second = new StrokeProcessor().Process(ToolType.FixedLine, raw);

            Assert.AreEqual(first.Points.Count, second.Points.Count);
            for (int i = 0; i < first.Points.Count; i++)
            {
                Assert.IsTrue(first.Points[i].x.Equals(second.Points[i].x));
                Assert.IsTrue(first.Points[i].y.Equals(second.Points[i].y));
            }
        }
    }
}
