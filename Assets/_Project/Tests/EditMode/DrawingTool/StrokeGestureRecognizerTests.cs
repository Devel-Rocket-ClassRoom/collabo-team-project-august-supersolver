using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 소유권·탭·경계·offset·멀티터치. 합성 포인터로
    /// 기기 없이 잰다. 점 밀도와 단순화는 파이프라인
    /// 테스트가 맡는다.
    /// </summary>
    public class StrokeGestureRecognizerTests
    {
        /// 1dp = 1px 이 되는 dpi. 환산을 눈으로 따라갈 수 있다.
        const float Dpi = DeviceUnits.BaselineDpi;

        /// 8dp → 0.08wu, 24dp → 0.24wu.
        const float PixelsPerUnit = 100f;

        const int FirstFinger = 1;
        const int SecondFinger = 2;

        /// 씬의 폴백 영역과 같다. x∈[-4,4] y∈[-6,6].
        static readonly Rect Area = new Rect(-4f, -6f, 8f, 12f);

        StrokeGestureRecognizer _recognizer;
        List<Stroke> _confirmed;
        List<Vector2> _pivots;
        DrawTool _tool;
        float _ink;
        float _offsetDp;

        [SetUp]
        public void 인식기를_만든다()
        {
            _tool = DrawTool.FixedLine;
            _ink = 100f;
            _offsetDp = 0f;
            Reset();
        }

        void Reset()
        {
            _confirmed = new List<Stroke>();
            _pivots = new List<Vector2>();
            _recognizer = new StrokeGestureRecognizer(new StrokeProcessor());
            _recognizer.StrokeConfirmed += _confirmed.Add;
            _recognizer.PivotRequested += (tool, anchor, radius) => _pivots.Add(anchor);
        }

        void Feed(PointerPhase phase, Vector2 world, bool overUI = false, int id = FirstFinger) =>
            _recognizer.Feed(
                new PointerSample(id, phase, world, overUI),
                new StrokeContext(_tool, _ink, PixelsPerUnit, Dpi, _offsetDp, Area));

        /// <summary>Down → Move… → 마지막 점에서 Up.</summary>
        void Drag(params Vector2[] path)
        {
            Feed(PointerPhase.Down, path[0]);
            for (int i = 1; i < path.Length - 1; i++) Feed(PointerPhase.Move, path[i]);
            Feed(PointerPhase.Up, path[path.Length - 1]);
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>
        /// Rect.Contains 는 최댓값을 제외해서 경계에 딱 붙은
        /// 점을 밖으로 친다. 클램프 결과를 재려면 못 쓴다.
        /// </summary>
        static bool Inside(Vector2 p) =>
            p.x >= Area.xMin && p.x <= Area.xMax && p.y >= Area.yMin && p.y <= Area.yMax;

        [Test]
        public void 캔버스에서_시작하면_UI_위를_지나도_획이_하나_나온다()
        {
            Feed(PointerPhase.Down, V(0f, 0f));
            Feed(PointerPhase.Move, V(0f, 0.3f), overUI: true);
            Feed(PointerPhase.Move, V(0f, 0.6f), overUI: true);
            Feed(PointerPhase.Up, V(0f, 0.9f), overUI: true);

            Assert.AreEqual(1, _confirmed.Count);
        }

        [Test]
        public void UI에서_시작하면_캔버스로_끌어도_획이_없다()
        {
            Feed(PointerPhase.Down, V(0f, 0f), overUI: true);
            Feed(PointerPhase.Move, V(0f, 0.3f));
            Feed(PointerPhase.Move, V(0f, 0.6f));
            Feed(PointerPhase.Up, V(0f, 0.9f));

            Assert.AreEqual(0, _confirmed.Count, "UI 에서 시작한 드래그가 획을 만들었다");

            // Up 이 소유권을 놓지 않으면 이후 입력이 전부 막힌다.
            Drag(V(0f, 0f), V(0f, 0.3f), V(0f, 0.6f));
            Assert.AreEqual(1, _confirmed.Count);
        }

        [Test]
        public void 탭_10회는_획을_만들지_않는다()
        {
            for (int i = 0; i < 10; i++)
                Drag(V(0f, 0f), V(0f, 0.05f));

            Assert.AreEqual(0, _confirmed.Count);
        }

        [Test]
        public void 탭_임계값을_넘어도_최소_획_길이_미만이면_획이_없다()
        {
            // 이동 0.2wu 는 탭(0.08wu)을 넘지만
            // 최소 획 길이(0.25wu)에는 못 미친다.
            Drag(V(0f, 0f), V(0f, 0.1f), V(0f, 0.2f));

            Assert.AreEqual(0, _confirmed.Count);
        }

        [Test]
        public void 플레이_영역_밖_구간은_경계에_붙어_기록된다()
        {
            // x=5, x=6 이 영역(xMax=4) 밖이다.
            Drag(V(0f, 0f), V(2f, 0f), V(5f, 1f), V(6f, 2f), V(3f, 1f), V(3.5f, 0f));

            Assert.AreEqual(1, _confirmed.Count, "밖으로 나갔다고 획이 끊겼다");

            List<Vector2> points = _confirmed[0].Points;

            foreach (Vector2 point in points)
                Assert.IsTrue(Inside(point), $"영역 밖 점이 남았다: {point}");

            // 버리는 방식이었다면 경계에 붙은 점이 하나도 없다.
            // 두 동작을 가르는 유일한 검사다.
            Assert.IsTrue(points.Exists(p => Mathf.Approximately(p.x, Area.xMax)),
                "밖 구간이 경계에 안 붙고 통째로 버려졌다");
        }

        [Test]
        public void offset_이_있어도_영역_바닥까지_그릴_수_있다()
        {
            _offsetDp = TouchMetrics.DrawOffsetDp;

            // 손가락이 바닥(yMin=-6) 아래로 내려간다. offset 이
            // 위로 미는 값이라 클램프가 없으면 바닥에 못 닿는다.
            Drag(V(0f, -5f), V(0f, -5.5f), V(0f, -6.5f), V(0f, -7f));

            Assert.AreEqual(1, _confirmed.Count);

            Assert.IsTrue(_confirmed[0].Points.Exists(p => Mathf.Approximately(p.y, Area.yMin)),
                "바닥에 닿지 못했다");
        }

        [Test]
        public void 획_진행_중_두_번째_손가락은_결과를_바꾸지_않는다()
        {
            Drag(V(0f, 0f), V(0f, 0.3f), V(0.3f, 0.6f), V(0.6f, 0.6f));
            List<Vector2> alone = _confirmed[0].Points;

            Reset();

            Feed(PointerPhase.Down, V(0f, 0f));
            Feed(PointerPhase.Move, V(0f, 0.3f));
            Feed(PointerPhase.Down, V(2f, 2f), id: SecondFinger);
            Feed(PointerPhase.Move, V(3f, 3f), id: SecondFinger);
            Feed(PointerPhase.Move, V(0.3f, 0.6f));
            Feed(PointerPhase.Up, V(3f, 3f), id: SecondFinger);
            Feed(PointerPhase.Up, V(0.6f, 0.6f));

            Assert.AreEqual(1, _confirmed.Count);
            CollectionAssert.AreEqual(alone, _confirmed[0].Points);
        }

        [Test]
        public void 영역_최상단에서_offset_을_줘도_점이_영역을_안_벗어난다()
        {
            _offsetDp = TouchMetrics.DrawOffsetDp;

            Drag(V(0f, 5f), V(0f, 5.4f), V(0f, 5.8f), V(0f, 5.9f));

            Assert.AreEqual(1, _confirmed.Count);

            foreach (Vector2 point in _confirmed[0].Points)
                Assert.LessOrEqual(point.y, Area.yMax, $"offset 이 점을 영역 밖으로 밀었다: {point}");
        }

        [Test]
        public void Canceled_는_획을_만들지_않고_다음_획을_막지도_않는다()
        {
            Feed(PointerPhase.Down, V(0f, 0f));
            Feed(PointerPhase.Move, V(0f, 0.3f));
            Feed(PointerPhase.Move, V(0f, 0.6f));
            Feed(PointerPhase.Canceled, V(0f, 0.6f));

            Assert.AreEqual(0, _confirmed.Count, "회수된 터치가 획을 남겼다");

            Drag(V(0f, 0f), V(0f, 0.3f), V(0f, 0.6f));
            Assert.AreEqual(1, _confirmed.Count);
        }

        [Test]
        public void 잔량이_0_이어도_회전축_탭은_핀을_요청한다()
        {
            _tool = DrawTool.PivotSingle;
            _ink = 0f;

            // 이동 0.02wu 는 탭 임계값(0.08wu) 안이다.
            Drag(V(1f, 2f), V(1f, 2.02f));

            Assert.AreEqual(1, _pivots.Count, "핀은 잉크를 쓰지 않는다");
            Assert.AreEqual(0, _confirmed.Count, "핀 도구가 획을 만들었다");
            Assert.AreEqual(V(1f, 2f), _pivots[0], "앵커는 손가락을 누른 자리다");
        }

        [Test]
        public void 회전축_도구로_끌면_핀도_획도_생기지_않는다()
        {
            _tool = DrawTool.PivotWorld;

            Drag(V(0f, 0f), V(0f, 0.3f), V(0f, 0.6f));

            Assert.AreEqual(0, _pivots.Count, "탭이 아니라 드래그다");
            Assert.AreEqual(0, _confirmed.Count, "핀 도구가 획을 만들었다");
        }

        [Test]
        public void 그리는_중_Abort_하면_획이_확정되지_않는다()
        {
            // 획을 그리는 중에 두 번째 손가락이 플레이를 누른다.
            // 인식기는 그 Down 을 무시하지만 버튼은 눌린다.
            Feed(PointerPhase.Down, V(0f, 0f));
            Feed(PointerPhase.Move, V(0f, 0.5f));
            Feed(PointerPhase.Move, V(0f, 1f));

            _recognizer.Abort();

            Assert.IsFalse(_recognizer.IsDrawing, "프리뷰가 화면에 남는다");

            // 손을 떼도 되살아나면 안 된다.
            Feed(PointerPhase.Up, V(0f, 1.5f));
            Assert.AreEqual(0, _confirmed.Count, "버린 획이 확정됐다");
        }

        [Test]
        public void 마우스와_터치는_offset_만큼만_다르다()
        {
            var path = new[] { V(0f, 0f), V(0f, 0.2f), V(0.2f, 0.4f), V(0.4f, 0.4f), V(0.4f, 0.1f) };

            Drag(path);
            List<Vector2> mouse = _confirmed[0].Points;

            Reset();
            _offsetDp = TouchMetrics.DrawOffsetDp;
            Drag(path);
            List<Vector2> touch = _confirmed[0].Points;

            Assert.AreEqual(mouse.Count, touch.Count, "offset 이 점 개수를 바꿨다");

            float offset = new DeviceUnits(Dpi).ToPixels(TouchMetrics.DrawOffsetDp) / PixelsPerUnit;
            for (int i = 0; i < mouse.Count; i++)
            {
                Assert.AreEqual(mouse[i].x, touch[i].x, 1e-4f);
                Assert.AreEqual(mouse[i].y + offset, touch[i].y, 1e-4f);
            }
        }
    }
}
