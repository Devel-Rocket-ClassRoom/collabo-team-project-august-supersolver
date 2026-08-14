using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 탭 한 점을 그림 위의 대상으로 푼다 — 회전축 생성,
    /// 지우개 대상 고르기, 핀 인덱스 유지가 여기 모인다.
    /// 무상태 순수 함수 — 반경이 월드 단위라 기기를 모른다.
    /// </summary>
    public static class SolutionEditing
    {

        /// <summary>
        /// 반경 안의 획을 최근에 그린 순으로 집어 핀을 만든다.
        /// 획 종류는 가리지 않는다 — 정적끼리 물린 핀은 당장
        /// 조인트가 안 생기지만, 그 자리를 지나는 자유물체가
        /// 나중에 그려지면 RebindPivots 가 되살린다.
        /// </summary>
        public static bool TryCreatePivot(
            Solution solution,
            Vector2 anchor,
            float radius,
            bool worldAnchored,
            out PivotJoint pivot)
        {
            pivot = default;
            if (solution == null) return false;

            List<Stroke> strokes = solution.Strokes;

            int a = FindStrokeNear(strokes, anchor, radius, strokes.Count - 1);
            int b = worldAnchored ? -1 : FindStrokeNear(strokes, anchor, radius, a - 1);

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
        /// 반경 안에서 가장 최근에 그린 획. 없으면 -1.
        /// 최근 순 = 인덱스 역순이고 정렬하지 않으니
        /// 동점에서 순서가 흔들릴 여지가 없다.
        /// </summary>
        /// <param name="from">여기서부터 거꾸로 훑는다.</param>
        public static int FindStrokeNear(
            List<Stroke> strokes, Vector2 point, float radius, int from)
        {
            for (int i = from; i >= 0; i--)
                if (Distance(strokes[i], point) <= radius) return i;
            return -1;
        }

        /// <summary>
        /// 반경 안에서 가장 최근에 놓은 핀. 없으면 -1.
        /// 마커는 앵커 한 점이라 선분이 아니라 점까지 잰다.
        /// </summary>
        public static int FindPivotNear(List<PivotJoint> pivots, Vector2 point, float radius)
        {
            for (int i = pivots.Count - 1; i >= 0; i--)
                if (Vector2.Distance(pivots[i].Anchor, point) <= radius) return i;
            return -1;
        }

        /// <summary>
        /// 획 하나가 빠진 뒤 핀 인덱스를 맞춘다. 지운 획을
        /// 물던 칸은 비우고, 두 칸이 다 비면 핀째 버린다.
        /// 안 하면 조용히 엉뚱한 획에 조인트가 붙는다.
        /// </summary>
        /// <param name="erased">방금 빠진 획의 인덱스.</param>
        public static void RemapPivots(Solution solution, int erased)
        {
            List<PivotJoint> pivots = solution.Pivots;

            // RemoveAt 하며 훑으므로 역순이다.
            for (int i = pivots.Count - 1; i >= 0; i--)
            {
                PivotJoint pivot = pivots[i];
                int a = Shift(pivot.StrokeA, erased);
                int b = Shift(pivot.StrokeB, erased);

                // 무효는 두 칸이 다 빈 핀뿐이다. 월드 고정은
                // 한 칸이 비어도 상대를 기다릴 수 있다.
                if (a == PivotJoint.Unbound && b == PivotJoint.Unbound)
                {
                    pivots.RemoveAt(i);
                    continue;
                }

                pivots[i] = new PivotJoint(a, b, pivot.Anchor);
            }
        }

        /// <summary>
        /// 음수는 인덱스가 아니라 표식이라 건드리면 안 된다 —
        /// 월드 고정(-1)·빈 칸(-2)은 획 순서와 무관하다.
        /// </summary>
        static int Shift(int index, int erased)
        {
            if (index < 0) return index;
            if (index == erased) return PivotJoint.Unbound;
            return index > erased ? index - 1 : index;
        }

        /// <summary>
        /// 방금 그은 획을 기다리던 핀에 물린다. 빈 칸이 있으면
        /// 채우고, 정적끼리 물려 물리에 없는 핀은 통째로
        /// 갈아끼운다.
        /// </summary>
        /// <param name="index">방금 추가된 획의 인덱스.</param>
        public static void RebindPivots(Solution solution, int index)
        {
            List<Stroke> strokes = solution.Strokes;
            if (index < 0 || index >= strokes.Count) return;

            List<PivotJoint> pivots = solution.Pivots;
            for (int i = 0; i < pivots.Count; i++)
            {
                PivotJoint pivot = pivots[i];
                if (Distance(strokes[index], pivot.Anchor) > WorldMetrics.PivotRebindDistance)
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
