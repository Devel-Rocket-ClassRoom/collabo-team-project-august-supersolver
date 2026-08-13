using System.Collections.Generic;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 지우개가 닿은 구간만 도형에서 덜어낸다.
    /// 열린 선의 가운데를 지우면 둘로 갈리므로
    /// 도형 하나가 여러 개로 늘어난다.
    /// </summary>
    public static class ShapeEraser
    {
        /// <summary>
        /// 지우고 남길 토막의 최소 길이.
        /// 이보다 짧은 것은 버린다 — 점으로 뭉갠 선분이
        /// 물리에서 터널링과 지터를 일으킨다.
        /// </summary>
        const float MinPieceLength = 0.25f;

        /// <summary>
        /// 지금 지울 것이 있는가.
        /// 되돌리기 스냅샷을 헛되이 남기지 않으려고 먼저 묻는다.
        /// </summary>
        public static bool Hits(List<ShapeData> shapes, Vector2 center, float radius)
        {
            for (int i = 0; i < shapes.Count; i++)
                if (Touches(shapes[i], center, radius)) return true;

            return false;
        }

        /// <summary>
        /// 원 안에 든 부분을 지운다. 열린 선만 손댄다 —
        /// 사각형·삼각형·원은 삭제 버튼으로 통째로 지운다.
        /// </summary>
        public static void Erase(List<ShapeData> shapes, Vector2 center, float radius)
        {
            var pieces = new List<ShapeData>();

            // 갈라진 조각을 그 자리에 넣으므로 뒤에서부터 돈다.
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                if (!Touches(shapes[i], center, radius)) continue;

                pieces.Clear();
                Split(shapes[i].Points, center, radius, pieces);

                shapes.RemoveAt(i);
                shapes.InsertRange(i, pieces);
            }
        }

        static bool Touches(ShapeData shape, Vector2 center, float radius)
        {
            if (shape.Kind != ShapeKind.Polyline) return false;

            var points = shape.Points;
            for (int i = 0; i + 1 < points.Count; i++)
                if (Overlap(points[i], points[i + 1], center, radius, out _, out _))
                    return true;

            return false;
        }

        /// <summary>선을 원 밖에 남은 토막들로 쪼갠다.</summary>
        static void Split(List<Vector2> points, Vector2 center, float radius, List<ShapeData> into)
        {
            var run = new List<Vector2>();

            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];

                if (!Overlap(a, b, center, radius, out float enter, out float exit))
                {
                    Append(run, a, b);
                    continue;
                }

                if (enter > 0f) Append(run, a, Vector2.Lerp(a, b, enter));
                Flush(run, into);
                if (exit < 1f) Append(run, Vector2.Lerp(a, b, exit), b);
            }

            Flush(run, into);
        }

        static void Append(List<Vector2> run, Vector2 a, Vector2 b)
        {
            if (run.Count == 0) run.Add(a);
            run.Add(b);
        }

        /// <summary>토막 하나를 확정한다.</summary>
        static void Flush(List<Vector2> run, List<ShapeData> into)
        {
            if (run.Count >= 2 && Length(run) >= MinPieceLength)
                into.Add(new ShapeData
                {
                    Kind = ShapeKind.Polyline,
                    Points = new List<Vector2>(run),
                });

            run.Clear();
        }

        static float Length(List<Vector2> points)
        {
            float sum = 0f;
            for (int i = 1; i < points.Count; i++)
                sum += Vector2.Distance(points[i - 1], points[i]);

            return sum;
        }

        /// <summary>
        /// 선분 a-b 가 원 안을 지나는 구간을 [0,1] 로 낸다.
        /// </summary>
        /// <returns>지우는 구간이 있으면 true.</returns>
        static bool Overlap(
            Vector2 a, Vector2 b, Vector2 center, float radius,
            out float enter, out float exit)
        {
            enter = 0f;
            exit = 1f;

            Vector2 d = b - a;
            float dd = Vector2.Dot(d, d);
            if (dd <= 0f) return Vector2.Distance(a, center) <= radius;

            Vector2 f = a - center;
            float half = Vector2.Dot(f, d);
            float outside = Vector2.Dot(f, f) - radius * radius;

            float discriminant = half * half - dd * outside;
            if (discriminant < 0f) return false;

            float root = Mathf.Sqrt(discriminant);
            float t0 = (-half - root) / dd;
            float t1 = (-half + root) / dd;

            if (t1 <= 0f || t0 >= 1f) return false;

            enter = Mathf.Max(t0, 0f);
            exit = Mathf.Min(t1, 1f);
            return true;
        }
    }
}
