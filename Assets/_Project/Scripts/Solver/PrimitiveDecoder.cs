using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브를 월드 스트로크로 펼친다.
    /// 단방향이다 — Stroke 는 점만 남기므로
    /// 되돌리는 경로는 두지 않는다.
    /// </summary>
    public static class PrimitiveDecoder
    {
        /// <summary>
        /// 리스트 순서를 그대로 넘긴다.
        /// 이 순서가 월드 구축 순서라 바뀌면
        /// Box2D 결과가 달라진다.
        /// 축은 스트로크 인덱스를 가리키므로
        /// 둘을 따로 만들면 어긋난다.
        /// </summary>
        public static Solution Decode(IReadOnlyList<Primitive> primitives)
        {
            var solution = new Solution();
            for (int i = 0; i < primitives.Count; i++)
            {
                solution.Strokes.Add(ToStroke(primitives[i]));
                if (primitives[i].HasPivot)
                    solution.Pivots.Add(ToPivot(primitives[i], i));
            }
            return solution;
        }

        /// <summary>
        /// 로컬 폴리라인에 Angle 회전 → Center 이동.
        /// Body 는 그대로 스트로크의 도구가 된다.
        /// </summary>
        public static Stroke ToStroke(in Primitive primitive)
        {
            var points = PrimitivePolyline.LocalPoints(primitive.Shape, primitive.Size);

            float cos = Mathf.Cos(primitive.Angle);
            float sin = Mathf.Sin(primitive.Angle);

            bool closed = PrimitiveShapeExtensions.Rule(primitive.Shape).IsClosed;

            for (int i = 0; i < points.Count; i++)
                points[i] = ToWorld(points[i], cos, sin, primitive.Center);

            // 다시 계산하면 부동소수 오차로 닫힘이 깨진다.
            if (closed) points[points.Count - 1] = points[0];

            return new Stroke(primitive.Body, points);
        }

        /// <summary>
        /// 축 지점을 월드 좌표로 옮겨 조인트로 만든다.
        /// StrokeA 는 이번 단계에서 늘 월드(-1)다.
        /// </summary>
        /// <param name="index">이 프리미티브의 스트로크 인덱스.</param>
        public static PivotJoint ToPivot(in Primitive primitive, int index)
        {
            var rule = PrimitiveShapeExtensions.Rule(primitive.Shape);
            Vector2 local = primitive.Pivot == PivotSpot.Middle
                ? rule.MiddleAnchor
                : rule.StartAnchor;

            Vector2 anchor = ToWorld(local * primitive.Size,
                Mathf.Cos(primitive.Angle), Mathf.Sin(primitive.Angle), primitive.Center);

            return new PivotJoint(primitive.PivotLink, index, anchor);
        }

        /// 폴리라인과 앵커가 같은 식을 써야
        /// Start 축이 첫 점에 정확히 얹힌다.
        static Vector2 ToWorld(Vector2 local, float cos, float sin, Vector2 center) =>
            new Vector2(
                cos * local.x - sin * local.y + center.x,
                sin * local.x + cos * local.y + center.y);
    }
}
