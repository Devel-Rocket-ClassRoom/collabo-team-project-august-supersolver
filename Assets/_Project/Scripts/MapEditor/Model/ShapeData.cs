using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 도형의 종류. 정수로 직렬화되므로
    /// 재배열 금지. 추가는 뒤에만.
    /// </summary>
    public enum ShapeKind
    {
        /// 열린 선. 점 순서대로 잇는다.
        Polyline = 0,

        /// 닫힌 선. 마지막 점이 첫 점과 이어진다.
        Polygon = 1,

        /// 중심과 반지름. 구울 때 선분으로 쪼갠다.
        Circle = 2,
    }

    /// <summary>
    /// 편집 단위 도형 하나.
    /// 선분 목록에서 그룹을 되짚을 수 없어
    /// 연결 순서를 여기에 명시적으로 든다.
    /// </summary>
    [Serializable]
    public sealed class ShapeData
    {
        public ShapeKind Kind;

        /// 순서가 곧 연결 순서. 원은 [0] 이 중심이다.
        public List<Vector2> Points = new List<Vector2>();

        /// 원 전용.
        public float Radius;

        public bool IsClosed => Kind == ShapeKind.Polygon;

        /// <summary>
        /// 이동·스케일의 기준점.
        /// 원은 중심, 나머지는 점들의 평균이다.
        /// </summary>
        public Vector2 Center()
        {
            if (Kind == ShapeKind.Circle) return Points.Count > 0 ? Points[0] : Vector2.zero;
            if (Points.Count == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < Points.Count; i++) sum += Points[i];
            return sum / Points.Count;
        }

        /// <summary>
        /// 도형을 감싸는 사각형. 옮길 때 한계를 잴 때 쓴다.
        /// </summary>
        public Rect Bounds()
        {
            if (Kind == ShapeKind.Circle)
            {
                Vector2 c = Points.Count > 0 ? Points[0] : Vector2.zero;
                return Rect.MinMaxRect(c.x - Radius, c.y - Radius, c.x + Radius, c.y + Radius);
            }

            if (Points.Count == 0) return Rect.zero;

            Vector2 min = Points[0];
            Vector2 max = Points[0];

            for (int i = 1; i < Points.Count; i++)
            {
                min = Vector2.Min(min, Points[i]);
                max = Vector2.Max(max, Points[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public void Move(Vector2 delta)
        {
            for (int i = 0; i < Points.Count; i++) Points[i] += delta;
        }

        /// <summary>중심을 축으로 늘리고 줄인다.</summary>
        public void Scale(float factor)
        {
            if (Kind == ShapeKind.Circle)
            {
                Radius *= factor;
                return;
            }

            Vector2 center = Center();
            for (int i = 0; i < Points.Count; i++)
                Points[i] = center + (Points[i] - center) * factor;
        }

        /// <summary>
        /// 원의 현재 외형을 편집 가능한 폐곡선으로 바꾼다.
        /// 바꾼 뒤에는 각 점을 독립적으로 움직일 수 있다.
        /// </summary>
        public void ConvertCircleToPolygon(int segments)
        {
            if (Kind != ShapeKind.Circle || Points.Count == 0 || segments < 3) return;

            Vector2 center = Points[0];
            var polygon = new List<Vector2>(segments);

            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                polygon.Add(center + new Vector2(
                    Mathf.Cos(angle) * Radius,
                    Mathf.Sin(angle) * Radius));
            }

            Kind = ShapeKind.Polygon;
            Points = polygon;
            Radius = 0f;
        }

        /// <summary>
        /// 누른 곳과 가장 가까운 변에서 새 점 자리를 찾는다.
        /// 원은 반지름으로 편집하므로 점을 넣지 않는다.
        /// </summary>
        public bool TryFindVertexInsertion(
            Vector2 world, float maxDistance, float minGap,
            out int insertAt, out Vector2 position)
        {
            insertAt = -1;
            position = default;

            if (Kind == ShapeKind.Circle || Points.Count < 2) return false;

            int edgeCount = IsClosed ? Points.Count : Points.Count - 1;
            float bestDistance = maxDistance;

            for (int i = 0; i < edgeCount; i++)
            {
                Vector2 a = Points[i];
                Vector2 b = Points[(i + 1) % Points.Count];
                Vector2 candidate = ClosestPoint(world, a, b);
                float distance = Vector2.Distance(world, candidate);

                if (distance > bestDistance) continue;

                bestDistance = distance;
                insertAt = i + 1;
                position = candidate;
            }

            if (insertAt < 0) return false;

            int before = insertAt - 1;
            int after = insertAt % Points.Count;

            // 간격 기준은 손가락 정밀도지만, 그보다 짧은 변에서는
            // 어디를 찍어도 막혀 삽입 자체가 불가능해진다.
            // 짧은 변에서는 변 길이에 비례한 기준으로 내린다.
            float gap = Mathf.Min(
                minGap, Vector2.Distance(Points[before], Points[after]) * 0.25f);

            return Vector2.Distance(position, Points[before]) >= gap
                && Vector2.Distance(position, Points[after]) >= gap;
        }

        static Vector2 ClosestPoint(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-6f) return a;

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return a + ab * t;
        }

        public ShapeData Clone()
        {
            return new ShapeData
            {
                Kind = Kind,
                Points = new List<Vector2>(Points),
                Radius = Radius,
            };
        }
    }

    /// <summary>
    /// 한 판의 도형 전체.
    /// 레벨 파일 옆에 따로 저장한다 — 게임도 솔버도
    /// 그룹을 읽지 않는다.
    /// </summary>
    [Serializable]
    public sealed class MapShapes
    {
        /// 어느 판의 것인지. 짝이 어긋난 파일을 거른다.
        public string StageId = "";

        public List<ShapeData> Shapes = new List<ShapeData>();

        public static MapShapes FromJson(string json) => JsonUtility.FromJson<MapShapes>(json);

        public string ToJson(bool prettyPrint = true) => JsonUtility.ToJson(this, prettyPrint);
    }
}
