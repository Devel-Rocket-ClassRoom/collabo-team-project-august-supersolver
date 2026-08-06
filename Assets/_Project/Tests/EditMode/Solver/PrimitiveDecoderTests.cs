using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 디코드의 최소 보장.
    /// 회전·이동·순서·닫힘을 못박는다.
    /// </summary>
    public class PrimitiveDecoderTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void 회전_뒤_이동_순서로_월드_좌표가_된다()
        {
            // 길이 4 인 직선을 90° 세우고 (3,4) 로 옮긴다.
            var primitive = new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(3f, 4f), Mathf.PI * 0.5f, 2f);

            var points = PrimitiveDecoder.ToStroke(primitive).Points;

            Assert.AreEqual(2, points.Count);
            Assert.AreEqual(3f, points[0].x, Eps);
            Assert.AreEqual(2f, points[0].y, Eps);
            Assert.AreEqual(3f, points[1].x, Eps);
            Assert.AreEqual(6f, points[1].y, Eps);
        }

        [Test]
        public void 회전해도_Center_까지의_거리는_Size다()
        {
            var primitive = new Primitive(PrimitiveShape.Triangle, ToolType.FreeBody,
                new Vector2(-5f, 2f), 0.7f, 3f);

            var points = PrimitiveDecoder.ToStroke(primitive).Points;

            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(3f, Vector2.Distance(points[i], primitive.Center), Eps,
                    $"{i}번 점");
        }

        [Test]
        public void 변환_뒤에도_닫힌_모양의_끝_점은_첫_점과_정확히_같다()
        {
            // 오차 허용 없이 == 여야 ColliderFactory 가
            // 폴리곤으로 만든다.
            var primitive = new Primitive(PrimitiveShape.Triangle, ToolType.FreeBody,
                new Vector2(1.3f, -2.7f), 1.1f, 2.4f);

            var points = PrimitiveDecoder.ToStroke(primitive).Points;

            Assert.AreEqual(points[0], points[points.Count - 1]);
        }

        [Test]
        public void Body가_그대로_스트로크의_도구가_된다()
        {
            var fixedLine = new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                Vector2.zero, 0f, 1f);
            var freeBody = new Primitive(PrimitiveShape.Line, ToolType.FreeBody,
                Vector2.zero, 0f, 1f);

            Assert.AreEqual(ToolType.FixedLine, PrimitiveDecoder.ToStroke(fixedLine).Tool);
            Assert.AreEqual(ToolType.FreeBody, PrimitiveDecoder.ToStroke(freeBody).Tool);
        }

        [Test]
        public void 리스트_순서가_그대로_유지된다()
        {
            // 순서가 곧 월드 구축 순서라 결정론이 걸린다.
            var primitives = new List<Primitive>
            {
                new Primitive(PrimitiveShape.Line, ToolType.FixedLine, new Vector2(0f, 0f), 0f, 1f),
                new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody, new Vector2(10f, 0f), 0f, 1f),
                new Primitive(PrimitiveShape.Triangle, ToolType.FixedLine, new Vector2(20f, 0f), 0f, 1f),
            };

            var strokes = PrimitiveDecoder.Decode(primitives);

            Assert.AreEqual(3, strokes.Count);
            for (int i = 0; i < strokes.Count; i++)
            {
                Assert.AreEqual(primitives[i].Body, strokes[i].Tool, $"{i}번 도구");
                Assert.AreEqual(PrimitiveShapeExtensions.Rule(primitives[i].Shape).PointCount,
                    strokes[i].Points.Count, $"{i}번 점 수");
            }
        }
    }
}
