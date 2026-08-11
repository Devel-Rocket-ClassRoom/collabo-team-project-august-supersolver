using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 스냅샷 되돌리기. 잉크 환급이 별도 로직이 아니라
    /// 잔량 재계산으로 성립하는지가 핵심이다.
    /// </summary>
    public class SolutionHistoryTests
    {
        const float InkLimit = 20f;

        /// <summary>x 자리에 선 하나. 길이가 곧 잉크다.</summary>
        static Stroke Line(float x, float length) =>
            new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(x, 0f),
                new Vector2(x, length),
            });

        static float Remaining(SolutionHistory history) =>
            InkLimit - history.Current.TotalInk();

        [Test]
        public void 획_3개를_3회_되돌리면_빈_그림이_되고_잉크가_초기값과_완전히_같다()
        {
            var history = new SolutionHistory();
            float before = Remaining(history);

            history.AddStroke(Line(0f, 1.3f));
            history.AddStroke(Line(1f, 2.7f));
            history.AddStroke(Line(2f, 0.9f));
            Assert.Less(Remaining(history), before, "잉크가 안 깎였다 — 검사가 헛돈다");

            for (int i = 0; i < 3; i++) history.Undo();

            Assert.AreEqual(0, history.Current.Strokes.Count, "획이 남았다");
            Assert.AreEqual(before, Remaining(history), 0f,
                "잔량은 재계산값이라 완전 일치해야 한다");
        }

        [Test]
        public void 초기화_직후_되돌리기_한_번이_이전_획을_전부_되살린다()
        {
            var history = new SolutionHistory();
            history.AddStroke(Line(0f, 1f));
            history.AddStroke(Line(1f, 2f));
            float drawn = Remaining(history);

            history.Clear();
            Assert.AreEqual(0, history.Current.Strokes.Count);

            history.Undo();

            Assert.AreEqual(2, history.Current.Strokes.Count, "초기화가 액션 하나로 안 돌아왔다");
            Assert.AreEqual(drawn, Remaining(history), 0f);
        }

        [Test]
        public void 핀_생성도_액션_하나라_되돌리고_다시_실행할_수_있다()
        {
            var history = new SolutionHistory();
            history.AddStroke(Line(0f, 1f));
            history.AddPivot(new PivotJoint(0, PivotJoint.WorldIndex, Vector2.zero));

            history.Undo();

            Assert.AreEqual(0, history.Current.Pivots.Count, "핀이 안 돌아갔다");
            Assert.AreEqual(1, history.Current.Strokes.Count, "핀만 되돌려야 하는데 획까지 지웠다");

            history.Redo();

            Assert.AreEqual(1, history.Current.Pivots.Count);
        }

        [Test]
        public void 스냅샷은_사본이라_이후의_변경에_오염되지_않는다()
        {
            var history = new SolutionHistory();
            history.AddStroke(Line(0f, 1f));

            Solution live = history.Current;
            history.AddStroke(Line(1f, 1f));

            // 참조를 쌓았다면 이 변형이 스냅샷까지 뚫는다.
            live.Strokes.Clear();

            history.Undo();

            Assert.AreEqual(1, history.Current.Strokes.Count,
                "되돌린 그림이 바깥 변형에 오염됐다 — Clone 없이 참조를 쌓고 있다");
        }

        [Test]
        public void 새_액션은_다시_실행_스택을_파기한다()
        {
            var history = new SolutionHistory();
            history.AddStroke(Line(0f, 1f));
            history.Undo();
            Assert.IsTrue(history.CanRedo, "되돌렸는데 다시 실행할 게 없다");

            history.AddStroke(Line(1f, 1f));

            Assert.IsFalse(history.CanRedo, "갈라진 가지를 다시 실행할 수 있으면 안 된다");
        }
    }
}
