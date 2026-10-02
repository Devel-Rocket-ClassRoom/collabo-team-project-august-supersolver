using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 획을 다루는 데 쓰는 평면 기하.
    /// 단순화와 핀 히트 판정이 같은 식을 쓴다 —
    /// 갈라지면 화면의 선과 잡히는 선이 달라진다.
    /// </summary>
    public static class Geometry2D
    {
        /// <summary>선분 a-b 와 p 사이의 거리.</summary>
        public static float SegmentDistance(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.x * ab.x + ab.y * ab.y;

            // 닫힌 획은 양 끝이 같은 자리라 선분이
            // 정의되지 않는다. 점 거리로 잰다.
            if (lengthSq <= 0f) return Vector2.Distance(a, p);

            Vector2 ap = p - a;
            float t = (ap.x * ab.x + ap.y * ab.y) / lengthSq;

            // 무한 직선으로 재면 왔던 길로 되돌아온 획에서
            // 바깥 구간이 통째로 지워진다.
            if (t <= 0f) return Vector2.Distance(a, p);
            if (t >= 1f) return Vector2.Distance(b, p);

            return Vector2.Distance(a + ab * t, p);
        }
    }
}
