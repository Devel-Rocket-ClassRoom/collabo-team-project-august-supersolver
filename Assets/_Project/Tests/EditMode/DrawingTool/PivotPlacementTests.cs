using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 탭 한 점이 어느 획에 걸리는가.
    /// 반경을 월드로 받는 순수 함수라 기기가 필요 없다.
    /// </summary>
    public class PivotPlacementTests
    {
        /// 대표 기기의 탭 환산치와 같은 자릿수.
        const float Radius = 0.1f;

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Stroke Line(ToolType tool, params Vector2[] points) =>
            new Stroke(tool, new List<Vector2>(points));

        static Solution With(params Stroke[] strokes)
        {
            var solution = new Solution();
            solution.Strokes.AddRange(strokes);
            return solution;
        }

        /// <summary>x 자리에 세운 세로 막대. 탭 위치와 겹치기 좋다.</summary>
        static Stroke Bar(ToolType tool, float x) =>
            Line(tool, V(x, 0f), V(x, 1f));

        [Test]
        public void 긴_직선의_한가운데를_탭해도_그_획이_잡힌다()
        {
            // 단순화를 거치면 곧은 획은 끝점 둘만 남는다.
            // 점까지 거리로 재면 한가운데가 2wu 밖이라 못 잡는다.
            var solution = With(Line(ToolType.FreeBody, V(0f, 0f), V(4f, 0f)));

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(2f, 0.05f), Radius, worldAnchored: true, out PivotJoint pivot));

            Assert.AreEqual(0, pivot.StrokeA);
        }

        [Test]
        public void 월드_고정은_최근_획_하나를_WorldIndex_에_잇는다()
        {
            var solution = With(
                Bar(ToolType.FreeBody, 0f),
                Bar(ToolType.FreeBody, 0.02f));

            Vector2 anchor = V(0f, 0.5f);

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, anchor, Radius, worldAnchored: true, out PivotJoint pivot));

            Assert.AreEqual(1, pivot.StrokeA, "최근에 그린 획이 아니다");
            Assert.AreEqual(PivotJoint.WorldIndex, pivot.StrokeB);
            Assert.AreEqual(anchor, pivot.Anchor);
        }

        [Test]
        public void 단독_핀은_반경_안의_최근_두_획을_잇는다()
        {
            var solution = With(
                Bar(ToolType.FreeBody, 0f),
                Bar(ToolType.FreeBody, 0.02f),
                Bar(ToolType.FreeBody, 3f));   // 더 최근이지만 반경 밖

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint pivot));

            Assert.AreEqual(1, pivot.StrokeA, "반경 밖 획을 집었다");
            Assert.AreEqual(0, pivot.StrokeB);
        }

        [Test]
        public void 고정선끼리_겹친_자리에도_핀이_생긴다()
        {
            // 당장은 조인트가 안 생기지만, 나중에 자유물체가
            // 지나가면 되살아날 자리라 데이터로 남긴다.
            var solution = With(
                Bar(ToolType.FixedLine, 0f),
                Bar(ToolType.FixedLine, 0.02f));

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint solo), "단독 핀");
            Assert.AreEqual(1, solo.StrokeA);
            Assert.AreEqual(0, solo.StrokeB);

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: true, out PivotJoint world), "월드 고정");
            Assert.AreEqual(PivotJoint.WorldIndex, world.StrokeB);
        }

        /// <summary>고정선 둘에 핀을 박고 그 자리에 자유물체를 긋는다.</summary>
        static Solution DeadPivotThenStroke(ToolType later, float laterX)
        {
            var solution = With(
                Bar(ToolType.FixedLine, 0f),
                Bar(ToolType.FixedLine, 0.02f));

            PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint pivot);
            solution.Pivots.Add(pivot);

            solution.Strokes.Add(Bar(later, laterX));
            PivotPlacement.Rebind(solution, solution.Strokes.Count - 1);
            return solution;
        }

        [Test]
        public void 나중에_그은_자유물체가_죽어_있던_핀을_되살린다()
        {
            Solution solution = DeadPivotThenStroke(ToolType.FreeBody, 0.05f);

            Assert.AreEqual(2, solution.Pivots[0].StrokeA, "새 획으로 안 갈아꼈다");
            Assert.AreEqual(PivotJoint.WorldIndex, solution.Pivots[0].StrokeB,
                "정적 바디와 월드는 같은 구속이라 월드로 정리해야 한다");
            Assert.AreEqual(V(0f, 0.5f), solution.Pivots[0].Anchor, "앵커가 움직였다");
        }

        [Test]
        public void 고정선을_더_그어도_죽은_핀은_안_살아난다()
        {
            Solution solution = DeadPivotThenStroke(ToolType.FixedLine, 0.05f);

            Assert.AreEqual(1, solution.Pivots[0].StrokeA);
            Assert.AreEqual(0, solution.Pivots[0].StrokeB);
        }

        [Test]
        public void 반경_밖을_지나는_획은_핀을_되살리지_않는다()
        {
            Solution solution = DeadPivotThenStroke(ToolType.FreeBody, 2f);

            Assert.AreEqual(1, solution.Pivots[0].StrokeA);
            Assert.AreEqual(0, solution.Pivots[0].StrokeB);
        }

        [Test]
        public void 이미_살아_있는_핀은_새_획에_뺏기지_않는다()
        {
            var solution = With(
                Bar(ToolType.FreeBody, 0f),
                Bar(ToolType.FreeBody, 0.02f));

            PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint pivot);
            solution.Pivots.Add(pivot);

            solution.Strokes.Add(Bar(ToolType.FreeBody, 0.05f));
            PivotPlacement.Rebind(solution, 2);

            Assert.AreEqual(1, solution.Pivots[0].StrokeA, "멀쩡한 핀을 갈아꼈다");
            Assert.AreEqual(0, solution.Pivots[0].StrokeB);
        }

        [Test]
        public void 획이_하나뿐이면_단독_핀은_상대를_비워_둔다()
        {
            var solution = With(Bar(ToolType.FreeBody, 0f));

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint pivot));

            Assert.AreEqual(0, pivot.StrokeA);
            Assert.AreEqual(PivotJoint.Unbound, pivot.StrokeB, "상대를 월드로 굳히면 안 된다");
        }

        [Test]
        public void 빈_곳에_놓은_월드_고정은_대상을_비워_둔다()
        {
            var solution = With(Bar(ToolType.FreeBody, 3f));   // 반경 밖

            Assert.IsTrue(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: true, out PivotJoint pivot));

            Assert.AreEqual(PivotJoint.Unbound, pivot.StrokeA);
            Assert.AreEqual(PivotJoint.WorldIndex, pivot.StrokeB);
        }

        [Test]
        public void 빈_곳에_단독_핀은_무효다()
        {
            // 두 칸이 다 비면 무엇을 이으려던 건지 남지 않는다.
            var solution = With(Bar(ToolType.FreeBody, 3f));

            Assert.IsFalse(PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out _));
        }

        [Test]
        public void 기다리던_단독_핀이_나중에_그은_획을_받는다()
        {
            var solution = With(Bar(ToolType.FreeBody, 0f));

            PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: false, out PivotJoint pivot);
            solution.Pivots.Add(pivot);

            solution.Strokes.Add(Bar(ToolType.FixedLine, 0.05f));
            PivotPlacement.Rebind(solution, 1);

            Assert.AreEqual(0, solution.Pivots[0].StrokeA, "기준 획이 바뀌었다");
            Assert.AreEqual(1, solution.Pivots[0].StrokeB,
                "빈 칸은 고정선도 받아야 한다 — (자유물체, 고정선) 은 멀쩡한 조인트다");
        }

        [Test]
        public void 빈_월드_핀이_나중에_그은_획을_받는다()
        {
            var solution = With(Bar(ToolType.FreeBody, 3f));

            PivotPlacement.TryResolve(
                solution, V(0f, 0.5f), Radius, worldAnchored: true, out PivotJoint pivot);
            solution.Pivots.Add(pivot);

            solution.Strokes.Add(Bar(ToolType.FreeBody, 0.05f));
            PivotPlacement.Rebind(solution, 1);

            Assert.AreEqual(1, solution.Pivots[0].StrokeA);
            Assert.AreEqual(PivotJoint.WorldIndex, solution.Pivots[0].StrokeB,
                "월드 고정이 풀렸다");
        }

        [Test]
        public void 반경_밖_빈_곳에_단독_핀을_탭하면_아무것도_안_만든다()
        {
            var solution = With(
                Bar(ToolType.FreeBody, 0f),
                Bar(ToolType.FreeBody, 0.02f));

            Assert.IsFalse(PivotPlacement.TryResolve(
                solution, V(2f, 2f), Radius, worldAnchored: false, out _));
        }

        [Test]
        public void 지우기_대상은_반경_안의_최근_핀이다()
        {
            var solution = new Solution();
            solution.Pivots.Add(new PivotJoint(0, 1, V(0f, 0f)));
            solution.Pivots.Add(new PivotJoint(0, 1, V(0.02f, 0f)));
            solution.Pivots.Add(new PivotJoint(0, 1, V(3f, 0f)));   // 반경 밖

            Assert.AreEqual(1,
                PivotPlacement.PickPivot(solution.Pivots, V(0f, 0f), Radius),
                "최근에 놓은 핀이 아니다");
            Assert.AreEqual(-1,
                PivotPlacement.PickPivot(solution.Pivots, V(9f, 9f), Radius),
                "빈 곳인데 무언가를 집었다");
        }

        /// 재매핑 테스트의 핀 자리. 인덱스만 보므로 아무 데나 둔다.
        static readonly Vector2 Far = new Vector2(9f, 9f);

        /// <summary>
        /// 획 n개에 핀 하나를 놓고 획 하나를 지운다.
        /// 지우는 쪽과 같은 순서로 — 리스트에서 빼고 재매핑한다.
        /// </summary>
        static Solution Remapped(int strokeCount, PivotJoint pivot, int erased)
        {
            var solution = new Solution();
            for (int i = 0; i < strokeCount; i++)
                solution.Strokes.Add(Bar(ToolType.FreeBody, i));
            solution.Pivots.Add(pivot);

            solution.Strokes.RemoveAt(erased);
            PivotPlacement.Remap(solution, erased);
            return solution;
        }

        [Test]
        public void 지운_획보다_뒤의_인덱스만_당겨진다()
        {
            Solution solution = Remapped(3, new PivotJoint(0, 2, Far), erased: 1);

            Assert.AreEqual(0, solution.Pivots[0].StrokeA, "앞 인덱스가 움직였다");
            Assert.AreEqual(1, solution.Pivots[0].StrokeB, "뒤 인덱스가 안 당겨졌다");
        }

        [Test]
        public void 지운_획을_물던_칸은_비고_나머지는_당겨진다()
        {
            Solution solution = Remapped(3, new PivotJoint(1, 2, Far), erased: 1);

            Assert.AreEqual(PivotJoint.Unbound, solution.Pivots[0].StrokeA);
            Assert.AreEqual(1, solution.Pivots[0].StrokeB);
        }

        [Test]
        public void 월드_고정을_문_핀은_획을_지워도_월드를_유지한다()
        {
            Solution solution = Remapped(
                3, new PivotJoint(2, PivotJoint.WorldIndex, Far), erased: 2);

            Assert.AreEqual(1, solution.Pivots.Count, "상대를 기다릴 수 있는 핀을 지웠다");
            Assert.AreEqual(PivotJoint.Unbound, solution.Pivots[0].StrokeA);
            Assert.AreEqual(PivotJoint.WorldIndex, solution.Pivots[0].StrokeB,
                "-1 은 인덱스가 아니라 표식이라 당기면 안 된다");
        }

        [Test]
        public void 획_하나짜리_대기_핀은_그_획과_함께_사라진다()
        {
            Solution solution = Remapped(
                1, new PivotJoint(0, PivotJoint.Unbound, Far), erased: 0);

            Assert.AreEqual(0, solution.Pivots.Count, "이을 것이 없는 핀이 살아 있다");
        }

        [Test]
        public void 반쪽만_남은_핀은_남은_쪽을_마저_지울_때_사라진다()
        {
            // (Unbound, 실제인덱스) 는 한 번 지운 뒤에만 나오는 모양이다.
            Solution solution = Remapped(
                2, new PivotJoint(PivotJoint.Unbound, 1, Far), erased: 1);

            Assert.AreEqual(0, solution.Pivots.Count);
        }

        [Test]
        public void 핀_여럿을_한_번에_맞추고_빈_핀만_솎아낸다()
        {
            var solution = new Solution();
            for (int i = 0; i < 4; i++)
                solution.Strokes.Add(Bar(ToolType.FreeBody, i));

            solution.Pivots.Add(new PivotJoint(0, 1, Far));
            solution.Pivots.Add(new PivotJoint(PivotJoint.Unbound, 1, Far));
            solution.Pivots.Add(new PivotJoint(2, 3, Far));

            solution.Strokes.RemoveAt(1);
            PivotPlacement.Remap(solution, 1);

            Assert.AreEqual(2, solution.Pivots.Count, "빈 핀만 사라져야 한다");
            Assert.AreEqual(0, solution.Pivots[0].StrokeA);
            Assert.AreEqual(PivotJoint.Unbound, solution.Pivots[0].StrokeB);
            Assert.AreEqual(1, solution.Pivots[1].StrokeA, "역순 삭제가 뒤 핀을 건너뛰었다");
            Assert.AreEqual(2, solution.Pivots[1].StrokeB);
        }
    }
}
