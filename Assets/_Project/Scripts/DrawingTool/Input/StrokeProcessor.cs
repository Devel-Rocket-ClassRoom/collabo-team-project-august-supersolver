using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 획 확정 단계. RDP 로 단순화한 뒤 Stroke 를 만든다.
    /// RDP 는 전역 알고리즘이라 실시간 적용이 불가능하고
    /// 손을 뗀 시점이 유일한 호출 지점이다.
    /// </summary>
    public sealed class StrokeProcessor : IStrokeProcessor
    {
        public Stroke Process(ToolType tool, List<Vector2> rawPoints)
        {
            if (rawPoints == null || rawPoints.Count < 2) return Rejected(tool);

            var stroke = new Stroke(tool, Simplify(rawPoints, WorldMetrics.RdpEpsilon));

            // 미세 콜라이더는 물리에서 터널링·지터를 일으킨다.
            if (stroke.Length() < WorldMetrics.MinStrokeLength) return Rejected(tool);

            return stroke;
        }

        /// <summary>
        /// 계약에 거절 사유 필드가 없다. 점을 비워
        /// IsValid 가 false 가 되게 하는 게 유일한 신호다.
        /// </summary>
        static Stroke Rejected(ToolType tool) => new Stroke(tool, null);

        static List<Vector2> Simplify(List<Vector2> points, float epsilon)
        {
            int last = points.Count - 1;

            var keep = new bool[points.Count];
            keep[0] = true;
            keep[last] = true;
            MarkFarthest(points, 0, last, epsilon, keep);

            var result = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
                if (keep[i]) result.Add(points[i]);
            return result;
        }

        static void MarkFarthest(List<Vector2> points, int first, int last, float epsilon, bool[] keep)
        {
            if (last - first < 2) return;

            int farthest = -1;
            float maxDist = 0f;

            for (int i = first + 1; i < last; i++)
            {
                float d = Geometry2D.SegmentDistance(points[first], points[last], points[i]);

                // 동점이면 앞쪽이 이긴다. 비교를 느슨하게
                // 두면 같은 입력에서 다른 점이 뽑힐 수 있다.
                if (d > maxDist)
                {
                    maxDist = d;
                    farthest = i;
                }
            }

            if (farthest < 0 || maxDist <= epsilon) return;

            keep[farthest] = true;
            MarkFarthest(points, first, farthest, epsilon, keep);
            MarkFarthest(points, farthest, last, epsilon, keep);
        }
    }
}
