using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 실시간 단계. 필터·잉크 누적·절단만 본다.
    /// 샘플레이트 독립성은 RDP 까지 거쳐야 나오므로
    /// StrokePipelineTests 가 맡는다.
    /// </summary>
    public class StrokeBuilderTests
    {
        static StrokeBuilder Started(float ink = 100f)
        {
            var builder = new StrokeBuilder();
            builder.Begin(ink);
            return builder;
        }

        [Test]
        public void 첫_점은_필터를_거치지_않는다()
        {
            var builder = Started();

            Assert.IsTrue(builder.AddPoint(new Vector2(1f, 1f)));
            Assert.AreEqual(1, builder.Points.Count);
        }

        [Test]
        public void 최소_거리보다_가까운_점은_버린다()
        {
            var builder = Started();
            builder.AddPoint(Vector2.zero);

            Assert.IsFalse(builder.AddPoint(new Vector2(0.05f, 0f)));
            Assert.IsTrue(builder.AddPoint(new Vector2(0.07f, 0f)));
            Assert.AreEqual(2, builder.Points.Count);
        }

        [Test]
        public void 필터에_걸린_점은_잉크를_먹지_않는다()
        {
            var builder = Started(1f);
            builder.AddPoint(Vector2.zero);
            builder.AddPoint(new Vector2(0.03f, 0f));
            builder.AddPoint(new Vector2(0.5f, 0f));

            Assert.AreEqual(0.5f, builder.RemainingInk, 0.0001f);
        }

        [Test]
        public void 잉크가_끊기는_지점을_보간해_찍고_이후_점을_받지_않는다()
        {
            var builder = Started(0.9f);
            for (int i = 0; i <= 4; i++)
                builder.AddPoint(new Vector2(i * 0.25f, 0f));

            Assert.AreEqual(5, builder.Points.Count);
            Assert.AreEqual(0.9f, builder.Points[4].x, 0.0001f);
            Assert.AreEqual(0f, builder.RemainingInk);

            Assert.IsFalse(builder.AddPoint(new Vector2(1.25f, 0f)));
            Assert.AreEqual(5, builder.Points.Count);
        }

        [Test]
        public void 절단점이_필터_간격보다_가까우면_찍지_않는다()
        {
            // 붙은 절단점을 남기면 마지막 세그먼트가
            // 퇴화 콜라이더가 된다.
            var builder = Started(0.79f);
            for (int i = 0; i <= 4; i++)
                builder.AddPoint(new Vector2(i * 0.25f, 0f));

            Assert.AreEqual(4, builder.Points.Count);
            Assert.AreEqual(0.75f, builder.Points[3].x, 0.0001f);
        }

        [Test]
        public void 최소_획_길이도_못_채우는_잔량이면_획이_시작되지_않는다()
        {
            var builder = Started(DrawConstants.MinStrokeLength - 0.01f);

            Assert.IsFalse(builder.AddPoint(Vector2.zero));
            Assert.AreEqual(0, builder.Points.Count);
        }

        [Test]
        public void 새_획을_시작하면_이전_버퍼가_사라진다()
        {
            var builder = Started();
            builder.AddPoint(Vector2.zero);
            builder.AddPoint(new Vector2(1f, 0f));

            builder.Begin(100f);

            Assert.AreEqual(0, builder.Points.Count);
            Assert.AreEqual(100f, builder.RemainingInk);
        }
    }
}
