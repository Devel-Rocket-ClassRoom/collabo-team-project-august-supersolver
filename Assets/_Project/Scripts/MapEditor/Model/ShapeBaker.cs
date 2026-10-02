using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집용 도형을 코어가 먹는 선분 목록으로 굽는다.
    /// 도형 순서와 점 순서가 곧 선분 등록 순서다 —
    /// 순서가 흔들리면 시뮬 결과가 달라진다.
    /// </summary>
    public static class ShapeBaker
    {
        /// <summary>
        /// 원을 근사하는 선분 수.
        /// 도형마다 다르게 두면 솔버 부하가 레벨
        /// 내용에 좌우된다. 상수로 박는다.
        /// </summary>
        public const int CircleSegments = 24;

        /// <summary>도형 목록으로 지형을 다시 만든다.</summary>
        public static void Bake(MapShapes shapes, LevelData level)
        {
            level.Terrain.Clear();
            if (shapes == null) return;

            for (int i = 0; i < shapes.Shapes.Count; i++)
                Append(shapes.Shapes[i], level.Terrain);
        }

        public static void Append(ShapeData shape, List<StaticSegment> into)
        {
            if (shape == null) return;

            if (shape.Kind == ShapeKind.Circle)
            {
                AppendCircle(shape, into);
                return;
            }

            var points = shape.Points;
            if (points.Count < 2) return;

            for (int i = 0; i + 1 < points.Count; i++)
                into.Add(new StaticSegment(points[i], points[i + 1]));

            // 닫힌 도형은 마지막 점과 첫 점을 잇는다.
            if (shape.IsClosed && points.Count >= 3)
                into.Add(new StaticSegment(points[points.Count - 1], points[0]));
        }

        static void AppendCircle(ShapeData shape, List<StaticSegment> into)
        {
            if (shape.Points.Count == 0 || shape.Radius <= 0f) return;

            Vector2 center = shape.Points[0];
            Vector2 first = center + new Vector2(shape.Radius, 0f);
            Vector2 previous = first;

            for (int i = 1; i < CircleSegments; i++)
            {
                float angle = 2f * Mathf.PI * i / CircleSegments;
                Vector2 point = center + new Vector2(
                    Mathf.Cos(angle) * shape.Radius,
                    Mathf.Sin(angle) * shape.Radius);

                into.Add(new StaticSegment(previous, point));
                previous = point;
            }

            // 마지막은 계산값이 아니라 첫 점으로 닫는다.
            // cos(2π) 가 정확히 1 이 아니라 틈이 남는다.
            into.Add(new StaticSegment(previous, first));
        }

        /// <summary>
        /// 도형이 없는 예전 레벨을 편집할 수 있게
        /// 선분 하나를 도형 하나로 본다.
        /// </summary>
        public static MapShapes FromTerrain(LevelData level, string stageId)
        {
            var shapes = new MapShapes { StageId = stageId };

            for (int i = 0; i < level.Terrain.Count; i++)
            {
                var segment = level.Terrain[i];
                shapes.Shapes.Add(new ShapeData
                {
                    Kind = ShapeKind.Polyline,
                    Points = new List<Vector2> { segment.A, segment.B },
                });
            }

            return shapes;
        }
    }
}
