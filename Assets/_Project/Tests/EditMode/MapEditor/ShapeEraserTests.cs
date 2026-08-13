using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.MapEditor.Tests
{
    /// <summary>
    /// 지우개가 닿은 만큼만 덜어내는가.
    /// 너무 덜어내면 그린 선이 사라지고, 덜 덜어내면
    /// 지운 자리에 점 같은 선분이 남는다.
    /// </summary>
    public class ShapeEraserTests
    {
        /// x 축 위의 곧은 선 하나.
        static List<ShapeData> Line(float from, float to) => new List<ShapeData>
        {
            new ShapeData
            {
                Kind = ShapeKind.Polyline,
                Points = new List<Vector2> { new Vector2(from, 0f), new Vector2(to, 0f) },
            },
        };

        [Test]
        public void 멀리_있는_선은_닿지_않는다()
        {
            var shapes = Line(0f, 10f);

            Assert.IsFalse(ShapeEraser.Hits(shapes, new Vector2(5f, 3f), 1f));
        }

        [Test]
        public void 가운데를_지우면_둘로_갈린다()
        {
            var shapes = Line(0f, 10f);

            ShapeEraser.Erase(shapes, new Vector2(5f, 0f), 1f);

            Assert.AreEqual(2, shapes.Count);
            Assert.AreEqual(4f, shapes[0].Points[1].x, 1e-3f);
            Assert.AreEqual(6f, shapes[1].Points[0].x, 1e-3f);
        }

        [Test]
        public void 끝을_지우면_선이_짧아진다()
        {
            var shapes = Line(0f, 10f);

            ShapeEraser.Erase(shapes, new Vector2(10f, 0f), 2f);

            Assert.AreEqual(1, shapes.Count);
            Assert.AreEqual(8f, shapes[0].Points[1].x, 1e-3f);
        }

        [Test]
        public void 통째로_덮으면_선이_사라진다()
        {
            var shapes = Line(0f, 1f);

            ShapeEraser.Erase(shapes, new Vector2(0.5f, 0f), 5f);

            Assert.AreEqual(0, shapes.Count);
        }

        [Test]
        public void 지우고_남은_토막이_너무_짧으면_버린다()
        {
            // 왼쪽에 0.1 만 남는다. 최소 획 길이에 못 미친다.
            var shapes = Line(0f, 10f);

            ShapeEraser.Erase(shapes, new Vector2(1.1f, 0f), 1f);

            Assert.AreEqual(1, shapes.Count);
            Assert.AreEqual(2.1f, shapes[0].Points[0].x, 1e-3f);
        }

        [Test]
        public void 놓은_도형은_지우개가_건드리지_않는다()
        {
            var shapes = new List<ShapeData>
            {
                new ShapeData
                {
                    Kind = ShapeKind.Polygon,
                    Points = new List<Vector2>
                    {
                        new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(0f, 2f),
                    },
                },
                new ShapeData
                {
                    Kind = ShapeKind.Circle,
                    Points = new List<Vector2> { Vector2.zero },
                    Radius = 1f,
                },
            };

            Assert.IsFalse(ShapeEraser.Hits(shapes, Vector2.one, 5f));

            ShapeEraser.Erase(shapes, Vector2.one, 5f);
            Assert.AreEqual(2, shapes.Count);
        }
    }
}
