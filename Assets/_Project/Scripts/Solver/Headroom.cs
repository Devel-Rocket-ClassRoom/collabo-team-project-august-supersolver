using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 어떤 자리 위로 지형까지 얼마나 비어 있는지.
    /// 추를 얼마나 높이 들어 올릴 수 있는지가 여기서 정해진다 —
    /// 떨어지는 길에 지형이 있으면 추가 판까지 오지 못한다.
    /// </summary>
    public static class Headroom
    {
        /// <summary>
        /// 폭 넓이짜리 물체를 from 에서 곧게 위로 올릴 때 닿기까지의 거리.
        /// 막는 것이 없으면 fallback 을 낸다 — 천장이 없는 레벨에서
        /// 무한대를 내면 부르는 쪽이 전부 그 값을 검사해야 한다.
        /// </summary>
        public static float Above(
            List<StaticSegment> terrain, Vector2 from, float width, float fallback)
        {
            if (terrain == null || terrain.Count == 0) return fallback;

            // 물체의 양 끝과 가운데를 본다. 한 점만 쏘면
            // 기둥 사이로 빠져나가 없는 여유를 있다고 읽는다.
            float half = width * 0.5f;
            float least = fallback;

            for (int s = 0; s <= Samples; s++)
            {
                float x = from.x + Mathf.Lerp(-half, half, (float)s / Samples);

                for (int i = 0; i < terrain.Count; i++)
                {
                    float gap = GapUp(terrain[i], x, from.y);
                    if (gap < least) least = gap;
                }
            }

            return Mathf.Max(0f, least);
        }

        /// <summary>
        /// 선분 a~b 가 지형에서 margin 이상 떨어져 있는가.
        /// 놓는 순간부터 지형에 박혀 있으면 물리가 밀어내며 튄다 —
        /// 그렇게 나온 결과는 실측한 프리셋과 아무 상관이 없다.
        /// </summary>
        public static bool Clear(
            List<StaticSegment> terrain, Vector2 a, Vector2 b, float margin)
        {
            if (terrain == null) return true;

            for (int i = 0; i < terrain.Count; i++)
                if (SegmentDistance(a, b, terrain[i].A, terrain[i].B) < margin) return false;

            return true;
        }

        /// 물체 폭을 몇 군데서 재는지. 양 끝을 포함해 Samples + 1 군데다.
        const int Samples = 4;

        /// <summary>
        /// 세로선 x 가 이 지형 조각을 위쪽에서 만나기까지의 거리.
        /// 만나지 않으면 무한대다.
        /// </summary>
        static float GapUp(in StaticSegment segment, float x, float y)
        {
            float left = Mathf.Min(segment.A.x, segment.B.x);
            float right = Mathf.Max(segment.A.x, segment.B.x);

            if (x < left || x > right) return float.PositiveInfinity;

            float span = segment.B.x - segment.A.x;

            // 수직 조각은 x 구간이 한 점이라 높은 쪽 끝을 본다.
            float at = Mathf.Abs(span) <= Mathf.Epsilon
                ? Mathf.Max(segment.A.y, segment.B.y)
                : Mathf.Lerp(segment.A.y, segment.B.y, (x - segment.A.x) / span);

            return at <= y ? float.PositiveInfinity : at - y;
        }

        /// <summary>선분 둘 사이의 거리. 만나면 0 이다.</summary>
        public static float Gap(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
            => SegmentDistance(a0, a1, b0, b1);

        static float SegmentDistance(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            if (Crosses(a0, a1, b0, b1)) return 0f;

            return Mathf.Min(
                Mathf.Min(PointToSegment(a0, b0, b1), PointToSegment(a1, b0, b1)),
                Mathf.Min(PointToSegment(b0, a0, a1), PointToSegment(b1, a0, a1)));
        }

        static bool Crosses(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            float d1 = Cross(b1 - b0, a0 - b0);
            float d2 = Cross(b1 - b0, a1 - b0);
            float d3 = Cross(a1 - a0, b0 - a0);
            float d4 = Cross(a1 - a0, b1 - a0);

            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static float PointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 along = b - a;
            float lengthSq = along.sqrMagnitude;

            if (lengthSq <= Mathf.Epsilon) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / lengthSq);
            return Vector2.Distance(point, a + along * t);
        }
    }
}
