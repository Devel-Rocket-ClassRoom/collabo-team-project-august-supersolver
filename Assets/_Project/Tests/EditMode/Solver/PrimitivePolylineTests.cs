using NUnit.Framework;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 로컬 폴리라인의 최소 보장.
    /// 외접원 반지름·닫힘·변 길이를 못박는다.
    /// </summary>
    public class PrimitivePolylineTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void 직선은_두_점이고_외접원_반지름이_Size다()
        {
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Line, 2.5f);

            Assert.AreEqual(2, points.Count);
            Assert.AreEqual(2.5f, points[0].magnitude, Eps);
            Assert.AreEqual(2.5f, points[1].magnitude, Eps);
            Assert.AreEqual(5f, Vector2.Distance(points[0], points[1]), Eps, "길이 = 2R");
        }

        [Test]
        public void 삼각형은_네_점이고_외접원_반지름이_Size다()
        {
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Triangle, 3f);

            Assert.AreEqual(4, points.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(3f, points[i].magnitude, Eps, $"{i}번 점");
        }

        [Test]
        public void 삼각형의_끝_점은_첫_점과_정확히_같다()
        {
            // 오차 허용 없이 == 여야 닫힘 판정이 산다.
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Triangle, 1.7f);

            Assert.AreEqual(points[0], points[points.Count - 1]);
        }

        [Test]
        public void 정삼각형_세_변의_길이가_같다()
        {
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Triangle, 2f);

            float side = Mathf.Sqrt(3f) * 2f;
            for (int i = 1; i < points.Count; i++)
                Assert.AreEqual(side, Vector2.Distance(points[i - 1], points[i]), Eps,
                    $"{i}번 변 = √3·R");
        }

        [Test]
        public void 직선은_닫히지_않는다()
        {
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Line, 1f);

            Assert.AreNotEqual(points[0], points[points.Count - 1]);
        }

        [Test]
        public void Center와_Angle은_적용하지_않는다()
        {
            // 로컬 좌표라 외접원 중심은 항상 원점이다.
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Triangle, 4f);

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Count - 1; i++) sum += points[i];

            Assert.AreEqual(0f, (sum / 3f).magnitude, Eps);
        }
    }
}
