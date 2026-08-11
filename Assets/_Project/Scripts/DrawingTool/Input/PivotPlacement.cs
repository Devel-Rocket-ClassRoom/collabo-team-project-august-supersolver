using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 탭 한 점을 회전축으로 바꾼다. 무상태 순수 함수 —
    /// 반경을 월드 단위로 받아 기기를 모른다.
    /// </summary>
    public static class PivotPlacement
    {

        /// <summary>
        /// 반경 안의 획을 최근에 그린 순으로 집어 핀을 만든다.
        /// 획 종류는 가리지 않는다 — 정적끼리 물린 핀은 당장
        /// 조인트가 안 생기지만, 그 자리를 지나는 자유물체가
        /// 나중에 그려지면 Rebind 가 되살린다.
        /// </summary>
        public static bool TryResolve(
            Solution solution,
            Vector2 anchor,
            float radius,
            bool worldAnchored,
            out PivotJoint pivot)
        {
            pivot = default;
            if (solution == null) return false;

            List<Stroke> strokes = solution.Strokes;

            int a = -1;
            int b = -1;

            // 최근에 그린 순 = 인덱스 역순. 정렬하지 않으니
            // 동점에서 순서가 흔들릴 여지가 없다.
            for (int i = strokes.Count - 1; i >= 0; i--)
            {
                if (Distance(strokes[i], anchor) > radius) continue;

                if (a < 0)
                {
                    a = i;
                    if (worldAnchored) break;
                    continue;
                }

                b = i;
                break;
            }

            // 단독 핀은 기준 획이 있어야 한다. 빈 곳에 놓으면
            // 두 칸이 다 비어 무엇을 이으려던 건지 남지 않는다.
            if (!worldAnchored && a < 0) return false;

            pivot = new PivotJoint(
                a < 0 ? PivotJoint.Unbound : a,
                worldAnchored ? PivotJoint.WorldIndex
                    : (b < 0 ? PivotJoint.Unbound : b),
                anchor);
            return true;
        }

        /// <summary>
        /// 방금 그은 획을 기다리던 핀에 물린다. 빈 칸이 있으면
        /// 채우고, 정적끼리 물려 물리에 없는 핀은 통째로
        /// 갈아끼운다.
        /// </summary>
        /// <param name="index">방금 추가된 획의 인덱스.</param>
        public static void Rebind(Solution solution, int index)
        {
            List<Stroke> strokes = solution.Strokes;
            if (index < 0 || index >= strokes.Count) return;

            List<PivotJoint> pivots = solution.Pivots;
            for (int i = 0; i < pivots.Count; i++)
            {
                PivotJoint pivot = pivots[i];
                if (Distance(strokes[index], pivot.Anchor) > DrawConstants.PivotRebindDistance)
                    continue;

                // 빈 칸은 획 종류를 안 가린다 —
                // (자유물체, 고정선) 도 멀쩡한 조인트다.
                if (pivot.StrokeA == PivotJoint.Unbound)
                {
                    pivots[i] = new PivotJoint(index, pivot.StrokeB, pivot.Anchor);
                    continue;
                }

                if (pivot.StrokeB == PivotJoint.Unbound)
                {
                    pivots[i] = new PivotJoint(pivot.StrokeA, index, pivot.Anchor);
                    continue;
                }

                // 여기부터는 두 칸이 다 찬 핀이다. 정적끼리라
                // 물리에 없는 것만, 자유물체로만 되살아난다.
                if (HasDynamic(strokes, pivot)) continue;
                if (strokes[index].Tool != ToolType.FreeBody) continue;

                // 물고 있던 고정선은 버린다. 동적 바디 입장에서
                // 정적 바디와 월드는 같은 구속이라 결과가 같다.
                pivots[i] = new PivotJoint(index, PivotJoint.WorldIndex, pivot.Anchor);
            }
        }

        /// <summary>
        /// 동적 바디를 물고 있는가. false 면 데이터에만 있고
        /// 물리에는 조인트가 안 생긴다.
        /// </summary>
        static bool HasDynamic(List<Stroke> strokes, in PivotJoint pivot) =>
            IsDynamic(strokes, pivot.StrokeA) || IsDynamic(strokes, pivot.StrokeB);

        static bool IsDynamic(List<Stroke> strokes, int index) =>
            index >= 0 && index < strokes.Count && strokes[index].Tool == ToolType.FreeBody;

        /// <summary>
        /// 점이 아니라 선분까지 잰다. 단순화가 곧은 획을
        /// 끝점 둘로 줄이므로, 점까지 재면 획 한가운데를
        /// 탭했을 때 안 잡힌다.
        /// </summary>
        static float Distance(in Stroke stroke, Vector2 point)
        {
            List<Vector2> points = stroke.Points;
            if (points == null || points.Count < 2) return float.MaxValue;

            float best = float.MaxValue;
            for (int i = 1; i < points.Count; i++)
            {
                float d = Geometry2D.SegmentDistance(points[i - 1], points[i], point);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
