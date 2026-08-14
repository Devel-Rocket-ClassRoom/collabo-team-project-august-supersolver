using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 잉크와 롤백이 겹치는 구간.
    /// 환급은 별도 로직이 아니라 잔량 재계산으로
    /// 성립한다 — 그 규약을 여기서 고정한다.
    /// </summary>
    public class InkRollbackTests
    {
        const float InkLimit = 20f;

        [Test]
        public void 거절된_획은_잉크를_한_톨도_쓰지_않는다()
        {
            var solution = new Solution();
            float before = InkLimit - solution.TotalInk();

            var builder = new StrokeBuilder();
            builder.Begin(before);
            builder.AddPoint(new Vector2(0f, 0f));
            builder.AddPoint(new Vector2(0.1f, 0f));
            builder.AddPoint(new Vector2(0.2f, 0f));

            var stroke = new StrokeProcessor().Process(ToolType.FixedLine, builder.Points);
            Assert.IsFalse(stroke.IsValid, "0.2wu 는 최소 획 길이 미만이라 거절돼야 한다");

            // 거절된 획은 Solution 에 넣지 않는다.
            float after = InkLimit - solution.TotalInk();

            Assert.AreEqual(before, after, 0f, "잔량은 재계산값이라 완전 일치해야 한다");
            Assert.Less(builder.RemainingInk, before,
                "빌더 잔량은 이미 깎여 있다 — 이 값을 다음 Begin 에 넘기면 환급이 사라진다");
        }

        [Test]
        public void 잉크로_끊긴_획이_최소_길이_이상이면_그대로_확정된다()
        {
            var builder = new StrokeBuilder();
            builder.Begin(0.4f);
            for (int i = 0; i <= 8; i++)
                builder.AddPoint(new Vector2(i * 0.1f, 0f));

            Assert.AreEqual(0f, builder.RemainingInk, "잉크가 끊겼어야 한다");
            Assert.AreEqual(0.4f, builder.Points[builder.Points.Count - 1].x, 0.0001f,
                "잔량이 0 이 되는 지점에서 멈춰야 한다");

            var stroke = new StrokeProcessor().Process(ToolType.FixedLine, builder.Points);

            Assert.IsTrue(stroke.IsValid, "끊긴 지점까지가 정상 Stroke 다 — 롤백이 아니다");
            Assert.AreEqual(0.4f, stroke.Length(), 0.0001f);
        }

        [Test]
        public void 잉크로_끊긴_획이_최소_길이_미만이면_롤백이_이긴다()
        {
            // 0.25wu 는 시작은 되는 잔량이다. 절단이
            // 0.2wu 에서 끊어 최소 길이 밑으로 내려간다.
            var builder = new StrokeBuilder();
            builder.Begin(WorldMetrics.MinStrokeLength);
            for (int i = 0; i <= 8; i++)
                builder.AddPoint(new Vector2(i * 0.1f, 0f));

            Assert.AreEqual(0f, builder.RemainingInk, "잉크가 끊겼어야 한다");

            var stroke = new StrokeProcessor().Process(ToolType.FixedLine, builder.Points);

            Assert.IsFalse(stroke.IsValid,
                "끊긴 길이가 최소 획 길이 미만이면 롤백 규칙이 우선한다");
        }
    }
}
