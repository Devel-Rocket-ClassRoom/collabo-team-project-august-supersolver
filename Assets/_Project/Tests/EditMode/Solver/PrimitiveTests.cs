using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 솔버 탐색 단위의 최소 보장.
    /// Shape × Body 조합과 직렬화 왕복을 본다.
    /// </summary>
    public class PrimitiveTests
    {
        static readonly PrimitiveShape[] Shapes =
        {
            PrimitiveShape.Line, PrimitiveShape.Bowl, PrimitiveShape.Triangle,
        };

        static readonly ToolType[] Bodies = { ToolType.FixedLine, ToolType.FreeBody };

        [Test]
        public void 모양_셋과_바디_둘로_여섯_조합이_모두_만들어진다()
        {
            var made = new HashSet<(PrimitiveShape, ToolType)>();

            foreach (var shape in Shapes)
            foreach (var body in Bodies)
            {
                var p = new Primitive(shape, body, Vector2.zero, 0f, 1f);

                Assert.AreEqual(shape, p.Shape);
                Assert.AreEqual(body, p.Body);
                made.Add((p.Shape, p.Body));
            }

            Assert.AreEqual(6, made.Count);
        }

        [Test]
        public void 직렬화_왕복에서_일곱_필드가_모두_보존된다()
        {
            var original = new Primitive(
                PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(1.25f, -3.5f), 0.75f, 2.5f,
                PivotSpot.Middle, 4);

            var restored = JsonUtility.FromJson<Primitive>(JsonUtility.ToJson(original));

            Assert.AreEqual(original.Shape, restored.Shape);
            Assert.AreEqual(original.Body, restored.Body);
            Assert.AreEqual(original.Center, restored.Center);
            Assert.AreEqual(original.Angle, restored.Angle);
            Assert.AreEqual(original.Size, restored.Size);
            Assert.AreEqual(original.Pivot, restored.Pivot);
            Assert.AreEqual(original.PivotLink, restored.PivotLink);
        }

        [Test]
        public void 여섯_조합_전부_직렬화_왕복을_견딘다()
        {
            foreach (var shape in Shapes)
            foreach (var body in Bodies)
            {
                var original = new Primitive(shape, body, new Vector2(0.5f, 2f), 1.5f, 0.8f);
                var restored = JsonUtility.FromJson<Primitive>(JsonUtility.ToJson(original));

                Assert.AreEqual(original.Shape, restored.Shape, $"{shape}/{body}");
                Assert.AreEqual(original.Body, restored.Body, $"{shape}/{body}");
                Assert.AreEqual(original.PivotLink, restored.PivotLink, $"{shape}/{body}");
            }
        }

        [Test]
        public void 축은_기본으로_없고_링크는_월드다()
        {
            var p = new Primitive(PrimitiveShape.Line, ToolType.FixedLine, Vector2.zero, 0f, 1f);

            Assert.IsFalse(p.HasPivot);
            Assert.AreEqual(Primitive.WorldLink, p.PivotLink);
            Assert.AreEqual(-1, Primitive.WorldLink);
        }

        [Test]
        public void 축_지점은_0_아니면_0점5다()
        {
            var start = new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                Vector2.zero, 0f, 1f, PivotSpot.Start);
            var middle = new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                Vector2.zero, 0f, 1f, PivotSpot.Middle);

            Assert.AreEqual(0f, start.PivotT);
            Assert.AreEqual(0.5f, middle.PivotT);
            Assert.IsTrue(start.HasPivot && middle.HasPivot);
        }

        [Test]
        public void 각도_주기는_모양이_해석한다()
        {
            Assert.AreEqual(Mathf.PI, PrimitiveShapeExtensions.AnglePeriod(PrimitiveShape.Line), 0.0001f,
                "직선은 뒤집어도 같은 선이다.");
            Assert.AreEqual(2f * Mathf.PI / 3f, PrimitiveShapeExtensions.AnglePeriod(PrimitiveShape.Triangle), 0.0001f,
                "정삼각형은 120° 마다 겹친다.");
            Assert.AreEqual(2f * Mathf.PI, PrimitiveShapeExtensions.AnglePeriod(PrimitiveShape.Bowl), 0.0001f);
        }
    }
}
