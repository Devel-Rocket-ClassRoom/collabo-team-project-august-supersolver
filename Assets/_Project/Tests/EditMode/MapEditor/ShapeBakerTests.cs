using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor.Tests
{
    /// <summary>
    /// 도형이 선분으로 굽히는가.
    /// 코어는 선분만 먹으므로 이 변환이 어긋나면
    /// 편집 화면과 시뮬이 다른 맵을 본다.
    /// </summary>
    public class ShapeBakerTests
    {
        static ShapeData Polygon(params Vector2[] points) => new ShapeData
        {
            Kind = ShapeKind.Polygon,
            Points = new List<Vector2>(points),
        };

        [Test]
        public void 열린_선은_점보다_선분이_하나_적다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Polyline,
                Points = new List<Vector2> { Vector2.zero, Vector2.right, Vector2.up },
            };

            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);

            Assert.AreEqual(2, segments.Count);
        }

        [Test]
        public void 닫힌_도형은_마지막_점이_첫_점과_이어진다()
        {
            var shape = Polygon(
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f));

            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);

            Assert.AreEqual(3, segments.Count, "삼각형은 변이 셋이다.");
            Assert.AreEqual(shape.Points[2], segments[2].A);
            Assert.AreEqual(shape.Points[0], segments[2].B);
        }

        [Test]
        public void 원은_정해진_수로_쪼개진다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Circle,
                Points = new List<Vector2> { new Vector2(2f, 3f) },
                Radius = 1.5f,
            };

            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);

            Assert.AreEqual(ShapeBaker.CircleSegments, segments.Count);

            // 끊긴 곳이 있으면 공이 새어 나간다.
            Assert.AreEqual(segments[0].A, segments[segments.Count - 1].B);
        }

        [Test]
        public void 도형_순서가_선분_순서를_정한다()
        {
            var shapes = new MapShapes();
            shapes.Shapes.Add(Polygon(
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f)));
            shapes.Shapes.Add(new ShapeData
            {
                Kind = ShapeKind.Polyline,
                Points = new List<Vector2> { new Vector2(5f, 5f), new Vector2(6f, 5f) },
            });

            var level = new LevelData();
            ShapeBaker.Bake(shapes, level);

            Assert.AreEqual(4, level.Terrain.Count);
            Assert.AreEqual(new Vector2(5f, 5f), level.Terrain[3].A,
                "뒤 도형의 선분이 앞 도형보다 먼저 오면 등록 순서가 흔들린다.");
        }

        [Test]
        public void 다시_구워도_선분이_쌓이지_않는다()
        {
            var shapes = new MapShapes();
            shapes.Shapes.Add(Polygon(
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f)));

            var level = new LevelData();
            ShapeBaker.Bake(shapes, level);
            ShapeBaker.Bake(shapes, level);

            Assert.AreEqual(3, level.Terrain.Count);
        }

        [Test]
        public void 도형이_없는_예전_레벨은_선분_하나가_도형_하나가_된다()
        {
            var level = new LevelData();
            level.Terrain.Add(new StaticSegment(new Vector2(-5f, 0f), new Vector2(5f, 0f)));
            level.Terrain.Add(new StaticSegment(new Vector2(0f, 1f), new Vector2(2f, 3f)));

            MapShapes shapes = ShapeBaker.FromTerrain(level, "L001");

            Assert.AreEqual(2, shapes.Shapes.Count);
            Assert.AreEqual(ShapeKind.Polyline, shapes.Shapes[0].Kind);
            Assert.AreEqual(new Vector2(-5f, 0f), shapes.Shapes[0].Points[0]);
        }

        [Test]
        public void 저장하고_읽어도_연결_순서가_그대로다()
        {
            var shapes = new MapShapes { StageId = "M_TEST" };
            shapes.Shapes.Add(Polygon(
                new Vector2(0f, 0f), new Vector2(2f, 0f),
                new Vector2(2f, 2f), new Vector2(0f, 2f)));

            MapShapes restored = MapShapes.FromJson(shapes.ToJson());

            Assert.AreEqual("M_TEST", restored.StageId);
            Assert.AreEqual(1, restored.Shapes.Count);
            Assert.AreEqual(ShapeKind.Polygon, restored.Shapes[0].Kind);
            Assert.AreEqual(4, restored.Shapes[0].Points.Count);
            Assert.AreEqual(new Vector2(2f, 2f), restored.Shapes[0].Points[2]);
        }

        [Test]
        public void 도형을_옮기면_모든_점이_같은_만큼_움직인다()
        {
            var shape = Polygon(
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f));

            shape.Move(new Vector2(3f, -2f));

            Assert.AreEqual(new Vector2(3f, -2f), shape.Points[0]);
            Assert.AreEqual(new Vector2(4f, -2f), shape.Points[1]);
            Assert.AreEqual(new Vector2(3f, -1f), shape.Points[2]);
        }

        [Test]
        public void 크기를_바꿔도_중심이_그대로다()
        {
            var shape = Polygon(
                new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(0f, 2f));

            Vector2 before = shape.Center();
            shape.Scale(2f);

            Assert.AreEqual(before.x, shape.Center().x, 1e-4f);
            Assert.AreEqual(before.y, shape.Center().y, 1e-4f);
        }

        [Test]
        public void 열린_선의_중간에_버텍스를_넣을_수_있다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Polyline,
                Points = new List<Vector2> { Vector2.zero, new Vector2(2f, 0f) },
            };

            bool found = shape.TryFindVertexInsertion(
                new Vector2(1f, 0.1f), 0.2f, 0.1f, out int insertAt, out Vector2 position);

            Assert.IsTrue(found);
            Assert.AreEqual(1, insertAt);
            Assert.AreEqual(new Vector2(1f, 0f), position);

            shape.Points.Insert(insertAt, position);
            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);
            Assert.AreEqual(2, segments.Count);
        }

        [Test]
        public void 닫힌_도형의_마지막_변에도_버텍스를_넣을_수_있다()
        {
            var shape = Polygon(
                new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f));

            bool found = shape.TryFindVertexInsertion(
                new Vector2(1f, 1f), 0.1f, 0.1f, out int insertAt, out Vector2 position);

            Assert.IsTrue(found);
            Assert.AreEqual(3, insertAt, "마지막과 첫 점 사이에는 목록 끝에 넣는다.");

            shape.Points.Insert(insertAt, position);
            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);
            Assert.AreEqual(4, segments.Count);
            Assert.AreEqual(position, segments[3].A);
            Assert.AreEqual(shape.Points[0], segments[3].B);
        }

        [Test]
        public void 기존_점에_너무_가까우면_버텍스를_넣지_않는다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Polyline,
                Points = new List<Vector2> { Vector2.zero, new Vector2(2f, 0f) },
            };

            bool found = shape.TryFindVertexInsertion(
                new Vector2(0.05f, 0f), 0.2f, 0.1f, out _, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void 원에는_버텍스를_삽입하지_않는다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Circle,
                Points = new List<Vector2> { Vector2.zero },
                Radius = 1f,
            };

            Assert.IsFalse(shape.TryFindVertexInsertion(
                Vector2.right, 0.2f, 0.1f, out _, out _));
        }

        [Test]
        public void 원을_폴리곤으로_바꾸면_외형이_유지된다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Circle,
                Points = new List<Vector2> { new Vector2(2f, 3f) },
                Radius = 1.5f,
            };

            var before = new List<StaticSegment>();
            ShapeBaker.Append(shape, before);

            shape.ConvertCircleToPolygon(ShapeBaker.CircleSegments);

            var after = new List<StaticSegment>();
            ShapeBaker.Append(shape, after);

            Assert.AreEqual(ShapeKind.Polygon, shape.Kind);
            Assert.AreEqual(before.Count, after.Count);
            for (int i = 0; i < before.Count; i++)
            {
                Assert.AreEqual(before[i].A, after[i].A);
                Assert.AreEqual(before[i].B, after[i].B);
            }
        }

        [Test]
        public void 최소_간격보다_짧은_변에도_버텍스를_넣을_수_있다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Circle,
                Points = new List<Vector2> { Vector2.zero },
                Radius = 1f,
            };

            shape.ConvertCircleToPolygon(ShapeBaker.CircleSegments);

            // 기본 크기 원의 변은 0.26 정도라 손가락 기준(0.25)의
            // 두 배에 못 미친다. 절대 기준만 쓰면 영영 못 넣는다.
            float edge = Vector2.Distance(shape.Points[0], shape.Points[1]);
            Assert.Less(edge, 0.5f, "이 테스트는 변이 짧다는 전제 위에 있다.");

            Vector2 middle = (shape.Points[0] + shape.Points[1]) * 0.5f;

            Assert.IsTrue(shape.TryFindVertexInsertion(middle, 0.5f, 0.25f, out _, out _));
        }

        [Test]
        public void 폴리곤으로_바꾼_원에도_버텍스를_넣을_수_있다()
        {
            var shape = new ShapeData
            {
                Kind = ShapeKind.Circle,
                Points = new List<Vector2> { Vector2.zero },
                Radius = 1f,
            };

            shape.ConvertCircleToPolygon(ShapeBaker.CircleSegments);
            Vector2 edgeMiddle = (shape.Points[0] + shape.Points[1]) * 0.5f;

            Assert.IsTrue(shape.TryFindVertexInsertion(
                edgeMiddle, 0.1f, 0.05f, out int insertAt, out Vector2 position));
            Assert.AreEqual(1, insertAt);

            shape.Points.Insert(insertAt, position);
            var segments = new List<StaticSegment>();
            ShapeBaker.Append(shape, segments);
            Assert.AreEqual(ShapeBaker.CircleSegments + 1, segments.Count);
        }
    }
}
