using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// Pivot → PivotJoint 변환의 최소 보장.
    /// 앵커 좌표와 인덱스 짝을 못박는다.
    /// </summary>
    public class PrimitivePivotTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void 축이_없으면_조인트도_없다()
        {
            var primitives = new List<Primitive>
            {
                new Primitive(PrimitiveShape.Line, ToolType.FreeBody, Vector2.zero, 0f, 1f),
                new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody, Vector2.one, 0f, 1f),
            };

            Assert.AreEqual(0, PrimitiveDecoder.Decode(primitives).Pivots.Count);
        }

        [Test]
        public void 시작점_축의_앵커는_스트로크_첫_점과_정확히_같다()
        {
            var primitive = new Primitive(PrimitiveShape.Triangle, ToolType.FreeBody,
                new Vector2(1.3f, -2.7f), 1.1f, 2.4f, PivotSpot.Start);

            var stroke = PrimitiveDecoder.ToStroke(primitive);
            var pivot = PrimitiveDecoder.ToPivot(primitive, 0);

            // 오차 허용 없이 == 여야 축이 선 위에 얹힌다.
            Assert.AreEqual(stroke.Points[0], pivot.Anchor);
        }

        [Test]
        public void 중점_축의_앵커도_회전과_이동을_따른다()
        {
            // 그릇의 0.5 는 바닥 중앙 (0,-1). 90° 돌리면
            // (1,0) 방향이 되고 Size 2 라 Center + (2,0).
            var primitive = new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(5f, 5f), Mathf.PI * 0.5f, 2f, PivotSpot.Middle);

            var anchor = PrimitiveDecoder.ToPivot(primitive, 0).Anchor;

            Assert.AreEqual(7f, anchor.x, Eps);
            Assert.AreEqual(5f, anchor.y, Eps);
        }

        [Test]
        public void PivotLink가_StrokeA_리스트_인덱스가_StrokeB_가_된다()
        {
            var primitives = new List<Primitive>
            {
                new Primitive(PrimitiveShape.Line, ToolType.FreeBody, Vector2.zero, 0f, 1f),
                new Primitive(PrimitiveShape.Line, ToolType.FreeBody, Vector2.zero, 0f, 1f,
                    PivotSpot.Middle),
            };

            var pivots = PrimitiveDecoder.Decode(primitives).Pivots;

            Assert.AreEqual(1, pivots.Count);
            Assert.AreEqual(PivotJoint.WorldIndex, pivots[0].StrokeA);
            Assert.AreEqual(1, pivots[0].StrokeB);
        }

        [Test]
        public void 같은_좌표에_축이_여러_개여도_전부_남는다()
        {
            // 리스트라 겹침이 그대로 표현된다.
            var center = new Vector2(2f, 3f);
            var primitives = new List<Primitive>
            {
                new Primitive(PrimitiveShape.Line, ToolType.FreeBody, center, 0f, 1f,
                    PivotSpot.Middle),
                new Primitive(PrimitiveShape.Triangle, ToolType.FreeBody, center, 0.5f, 1f,
                    PivotSpot.Middle),
                new Primitive(PrimitiveShape.Line, ToolType.FreeBody, center, 1f, 1f,
                    PivotSpot.Middle),
            };

            var pivots = PrimitiveDecoder.Decode(primitives).Pivots;

            Assert.AreEqual(3, pivots.Count);
            for (int i = 0; i < pivots.Count; i++)
            {
                Assert.AreEqual(i, pivots[i].StrokeB, $"{i}번 축의 대상");
                Assert.AreEqual(center.x, pivots[i].Anchor.x, Eps, $"{i}번 축 x");
                Assert.AreEqual(center.y, pivots[i].Anchor.y, Eps, $"{i}번 축 y");
            }
        }
    }
}
