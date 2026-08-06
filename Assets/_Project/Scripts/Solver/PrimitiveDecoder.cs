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
        /// </summary>
        public static List<Stroke> Decode(IReadOnlyList<Primitive> primitives)
        {
            var strokes = new List<Stroke>(primitives.Count);
            for (int i = 0; i < primitives.Count; i++)
                strokes.Add(ToStroke(primitives[i]));
            return strokes;
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

            bool closed = points.Count >= 3 && points[0] == points[points.Count - 1];

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                points[i] = new Vector2(
                    cos * p.x - sin * p.y + primitive.Center.x,
                    sin * p.x + cos * p.y + primitive.Center.y);
            }

            // 다시 계산하면 부동소수 오차로 닫힘이 깨진다.
            if (closed) points[points.Count - 1] = points[0];

            return new Stroke(primitive.Body, points);
        }
    }
}
