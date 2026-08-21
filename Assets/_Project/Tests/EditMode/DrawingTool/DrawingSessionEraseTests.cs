using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 지우기 한 번이 획·핀·잉크를 액션 하나로 묶는가.
    /// 재매핑이 되돌리기 스냅샷 뒤에 와야 한 번에 돌아온다.
    /// </summary>
    public class DrawingSessionEraseTests
    {
        static readonly Vector2 Anchor = new Vector2(9f, 9f);

        GameObject _go;
        DrawingSession _session;

        [SetUp]
        public void 세션을_만든다()
        {
            _go = new GameObject("Session");
            _session = _go.AddComponent<DrawingSession>();
        }

        [TearDown]
        public void 세션을_치운다() => Object.DestroyImmediate(_go);

        /// <summary>x 자리 세로 막대. 길이가 1 이라 잉크도 1 이다.</summary>
        static Stroke Bar(float x) =>
            new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(x, 0f),
                new Vector2(x, 1f),
            });

        void Draw(params float[] xs)
        {
            for (int i = 0; i < xs.Length; i++) _session.AddStroke(Bar(xs[i]));
        }

        static List<Vector2> Copy(Stroke stroke) => new List<Vector2>(stroke.Points);

        /// <summary>
        /// 델타 0 = 비트 일치. Vector2 의 == 는 오차를
        /// 허용해 이 비교에 못 쓴다.
        /// </summary>
        static void AssertSameBits(List<Vector2> expected, List<Vector2> actual, string where)
        {
            Assert.AreEqual(expected.Count, actual.Count, $"{where}: 점 개수가 달라졌다");

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].x, actual[i].x, 0f, $"{where}: 점 {i} 의 x");
                Assert.AreEqual(expected[i].y, actual[i].y, 0f, $"{where}: 점 {i} 의 y");
            }
        }

        [Test]
        public void 가운데_획을_지워도_남은_획의_점이_비트째_그대로다()
        {
            Draw(0f, 5f, 10f);
            List<Vector2> first = Copy(_session.Solution.Strokes[0]);
            List<Vector2> last = Copy(_session.Solution.Strokes[2]);

            _session.EraseStroke(1);

            Assert.AreEqual(2, _session.Solution.Strokes.Count);
            AssertSameBits(first, _session.Solution.Strokes[0].Points, "앞 획");
            AssertSameBits(last, _session.Solution.Strokes[1].Points, "뒤 획");
            Assert.AreEqual(2f, _session.Solution.TotalInk(), 0f,
                "잉크가 남은 두 획의 길이 합이 아니다");
        }

        [Test]
        public void 지우기_되돌리기_한_번이_획과_핀을_함께_되살린다()
        {
            Draw(0f, 5f, 10f);
            _session.AddPivot(new PivotJoint(0, 2, Anchor));

            _session.EraseStroke(1);
            Assert.AreEqual(1, _session.Solution.Pivots[0].StrokeB, "핀이 재매핑되지 않았다");

            _session.OnClickUndo();

            Assert.AreEqual(3, _session.Solution.Strokes.Count, "획이 안 돌아왔다");
            Assert.AreEqual(2, _session.Solution.Pivots[0].StrokeB, "핀 인덱스가 안 돌아왔다");
            Assert.AreEqual(3f + PivotJoint.InkCost, _session.Solution.TotalInk(), 0f,
                "잉크가 안 돌아왔다");
        }

        [Test]
        public void 핀만_지우면_획은_그대로다()
        {
            Draw(0f, 5f);
            _session.AddPivot(new PivotJoint(0, 1, Anchor));

            _session.ErasePivot(0);

            Assert.AreEqual(0, _session.Solution.Pivots.Count);
            Assert.AreEqual(2, _session.Solution.Strokes.Count, "핀을 지웠는데 획이 없어졌다");

            // 핀 잉크도 잔량 재계산이라 환급 로직이 없다.
            Assert.AreEqual(2f, _session.Solution.TotalInk(), 0f, "핀 잉크가 안 돌아왔다");
        }

        [Test]
        public void 지우기가_Changed_를_한_번_쏜다()
        {
            Draw(0f);
            int fired = 0;
            _session.Changed += () => fired++;

            _session.EraseStroke(0);

            Assert.AreEqual(1, fired, "렌더러가 못 듣는다 — 지운 획이 화면에 남는다");
        }

        /// <summary>
        /// 끌어서 여럿을 지워도 삭제 하나가 액션 하나다.
        /// 한 제스처를 통째로 묶으면 마지막에 지운 것만
        /// 되살릴 수 없다.
        /// </summary>
        [Test]
        public void 여럿을_지우면_되돌리기가_지운_역순으로_하나씩_살린다()
        {
            Draw(0f, 5f, 10f);

            // 붓이 지나간 순서. 뒤엣것부터 걷힌다.
            _session.EraseStroke(2);
            _session.EraseStroke(1);
            _session.EraseStroke(0);
            Assert.AreEqual(0, _session.Solution.Strokes.Count, "안 지워졌다 — 검사가 헛돈다");

            _session.OnClickUndo();
            Assert.AreEqual(1, _session.Solution.Strokes.Count, "한 번에 하나만 살아나야 한다");
            Assert.AreEqual(0f, _session.Solution.Strokes[0].Points[0].x,
                "마지막에 지운 것이 먼저 돌아와야 한다");

            _session.OnClickUndo();
            Assert.AreEqual(2, _session.Solution.Strokes.Count);
            Assert.AreEqual(5f, _session.Solution.Strokes[1].Points[0].x);

            _session.OnClickUndo();
            Assert.AreEqual(3, _session.Solution.Strokes.Count);
            Assert.AreEqual(10f, _session.Solution.Strokes[2].Points[0].x);
            Assert.AreEqual(3f, _session.Solution.TotalInk(), 0f, "잉크가 안 돌아왔다");
        }

        /// <summary>
        /// 핀 재매핑은 지울 때마다 돌아야 한다. 끌어서
        /// 연속으로 지울 때 몰아서 하면 중간 인덱스가 어긋난다.
        /// </summary>
        [Test]
        public void 연속으로_지우는_동안_핀이_그때그때_재매핑된다()
        {
            Draw(0f, 5f, 10f);
            _session.AddPivot(new PivotJoint(0, 2, Anchor));

            _session.EraseStroke(1);
            Assert.AreEqual(1, _session.Solution.Pivots[0].StrokeB, "지우는 즉시 안 당겨졌다");

            _session.EraseStroke(0);

            Assert.AreEqual(PivotJoint.Unbound, _session.Solution.Pivots[0].StrokeA,
                "지운 획을 문 칸이 안 비었다");
            Assert.AreEqual(0, _session.Solution.Pivots[0].StrokeB, "남은 획으로 안 당겨졌다");
        }
    }
}
