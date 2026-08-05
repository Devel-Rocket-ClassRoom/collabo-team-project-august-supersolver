using System;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// Shape 규칙표의 최소 보장.
    /// 3 Shape × 5 규칙을 값으로 못박고,
    /// 표가 enum 을 빠짐없이 덮는지 본다.
    /// </summary>
    public class ShapeRuleTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void 직선_다섯_규칙()
        {
            var rule = PrimitiveShape.Line.Rule();

            Assert.AreEqual(Mathf.PI, rule.AnglePeriod, Eps, "주기 180°");
            Assert.AreEqual(new Vector2(-1f, 0f), rule.StartAnchor, "0 은 끝점");
            Assert.AreEqual(Vector2.zero, rule.MiddleAnchor, "0.5 는 무게중심");
            Assert.AreEqual(2f, rule.DimensionPerRadius, Eps, "길이 = 2R");
            Assert.AreEqual(2, rule.PointCount);
        }

        [Test]
        public void 삼각형_다섯_규칙()
        {
            var rule = PrimitiveShape.Triangle.Rule();

            Assert.AreEqual(2f * Mathf.PI / 3f, rule.AnglePeriod, Eps, "주기 120°");
            Assert.AreEqual(1f, rule.StartAnchor.magnitude, Eps, "0 은 외접원 위 꼭짓점");
            Assert.AreEqual(Vector2.zero, rule.MiddleAnchor, "0.5 는 무게중심");
            Assert.AreEqual(Mathf.Sqrt(3f), rule.DimensionPerRadius, Eps, "한 변 = √3·R");
            Assert.AreEqual(4, rule.PointCount, "닫힘");
            Assert.AreEqual(rule.UnitPoints[0], rule.UnitPoints[3]);
        }

        [Test]
        public void 그릇_다섯_규칙()
        {
            var rule = PrimitiveShape.Bowl.Rule();
            var last = rule.UnitPoints[rule.PointCount - 1];

            Assert.AreEqual(2f * Mathf.PI, rule.AnglePeriod, Eps, "주기 360°");

            // 호를 삼각함수로 만들어 성분에 1e-7 오차가 남는다.
            Assert.AreEqual(-1f, rule.StartAnchor.x, Eps, "0 은 한쪽 팔 끝");
            Assert.AreEqual(0f, rule.StartAnchor.y, Eps, "0 은 한쪽 팔 끝");
            Assert.AreEqual(0f, rule.MiddleAnchor.x, Eps, "0.5 는 바닥 중앙");
            Assert.AreEqual(-1f, rule.MiddleAnchor.y, Eps, "0.5 는 바닥 중앙");
            Assert.AreEqual(Vector2.Distance(rule.StartAnchor, last), rule.DimensionPerRadius, Eps,
                "치수는 팔 끝 간 거리");
            Assert.IsTrue(rule.PointCount >= 8 && rule.PointCount <= 12, "8~12 점");
        }

        [Test]
        public void 잉크는_외접원이_아니라_실제_폴리라인_길이다()
        {
            foreach (PrimitiveShape shape in Enum.GetValues(typeof(PrimitiveShape)))
            {
                var rule = shape.Rule();

                float sum = 0f;
                for (int i = 1; i < rule.PointCount; i++)
                    sum += Vector2.Distance(rule.UnitPoints[i - 1], rule.UnitPoints[i]);

                Assert.AreEqual(sum, rule.InkPerRadius, Eps, shape.ToString());
            }
        }

        [Test]
        public void 같은_R이어도_모양마다_잉크가_다르다()
        {
            float line = PrimitiveShape.Line.Rule().InkPerRadius;
            float triangle = PrimitiveShape.Triangle.Rule().InkPerRadius;
            float bowl = PrimitiveShape.Bowl.Rule().InkPerRadius;

            Assert.AreEqual(2f, line, Eps);
            Assert.AreEqual(3f * Mathf.Sqrt(3f), triangle, Eps, "정삼각형 둘레");
            Assert.Less(bowl, Mathf.PI, "현의 합이라 반원 호보다 짧다");
            Assert.Greater(bowl, 3f);
        }

        [Test]
        public void 각도_분할_수는_주기에_비례하고_조정된다()
        {
            Assert.AreEqual(8, PrimitiveShape.Line.Rule().AngleDivisions);
            Assert.AreEqual(6, PrimitiveShape.Triangle.Rule().AngleDivisions);
            Assert.AreEqual(16, PrimitiveShape.Bowl.Rule().AngleDivisions);

            var rule = PrimitiveShape.Line.Rule();
            int original = rule.AngleDivisions;
            rule.AngleDivisions = 12;
            Assert.AreEqual(12, PrimitiveShape.Line.Rule().AngleDivisions, "필드로 조정 가능");
            rule.AngleDivisions = original;
        }

        [Test]
        public void 표는_모든_Shape를_덮는다()
        {
            var shapes = Enum.GetValues(typeof(PrimitiveShape));

            Assert.AreEqual(shapes.Length, PrimitiveShapeExtensions.RuleCount,
                "Shape 를 늘렸다면 표에도 한 줄 넣어야 한다.");

            foreach (PrimitiveShape shape in shapes)
            {
                var rule = shape.Rule();
                Assert.IsNotNull(rule, shape.ToString());
                Assert.GreaterOrEqual(rule.PointCount, 2, shape.ToString());
                Assert.Greater(rule.AngleDivisions, 0, shape.ToString());
            }
        }
    }
}
