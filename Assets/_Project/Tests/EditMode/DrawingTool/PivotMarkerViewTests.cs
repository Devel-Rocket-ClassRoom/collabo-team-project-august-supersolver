using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 핀은 화면에서 가장 놓치기 쉬운 것이다 — 안 보이면
    /// 사고로 생긴 축을 눈치채지 못한 채 정답이 바뀐다.
    /// 상태가 표시로 옮겨지는지를 잰다.
    /// </summary>
    public class PivotMarkerViewTests
    {
        GameObject _go;
        PivotMarkerView _view;

        [SetUp]
        public void 마커판을_만든다()
        {
            _go = new GameObject("Markers");
            _view = _go.AddComponent<PivotMarkerView>();
        }

        [TearDown]
        public void 마커판을_치운다() => Object.DestroyImmediate(_go);

        /// 단독 핀(대기) 하나 · 월드 고정(완성) 하나.
        static Solution MakeSolution()
        {
            var solution = new Solution();
            solution.Pivots.Add(new PivotJoint(0, PivotJoint.Unbound, new Vector2(1f, 2f)));
            solution.Pivots.Add(new PivotJoint(1, PivotJoint.WorldIndex, new Vector2(-3f, 0.5f)));

            return solution;
        }

        [Test]
        public void 채움_상태는_빈_칸으로_갈린다()
        {
            Assert.AreEqual(PivotFill.Bound,
                PivotMarkerView.FillOf(new PivotJoint(0, 1, Vector2.zero)),
                "두 획을 문 핀이 완성이 아니다");

            // 월드는 이미 정해진 값이라 기다릴 것이 없다.
            Assert.AreEqual(PivotFill.Bound,
                PivotMarkerView.FillOf(new PivotJoint(0, PivotJoint.WorldIndex, Vector2.zero)),
                "월드 고정은 획 하나로 완성이다");

            Assert.AreEqual(PivotFill.Waiting,
                PivotMarkerView.FillOf(new PivotJoint(0, PivotJoint.Unbound, Vector2.zero)),
                "빈 칸이 있는데 대기가 아니다");

            Assert.AreEqual(PivotFill.Empty,
                PivotMarkerView.FillOf(
                    new PivotJoint(PivotJoint.Unbound, PivotJoint.WorldIndex, Vector2.zero)),
                "붙은 획이 없는데 비어 있지 않다");
        }

        [Test]
        public void 핀_수만큼_마커가_Anchor_자리에_선다()
        {
            _view.Show(MakeSolution());

            Assert.AreEqual(2, _go.transform.childCount);

            Transform single = _go.transform.Find("Pivot_0");
            Assert.AreEqual(1f, single.position.x, 1e-5f);
            Assert.AreEqual(2f, single.position.y, 1e-5f);
        }

        /// <summary>
        /// 바깥 원이 종류를 가르는 유일한 표시다. 색은
        /// 채움 상태가 이미 쓰고 있어 겹칠 수 없다.
        /// </summary>
        [Test]
        public void 월드_고정만_바깥_원을_두른다()
        {
            _view.Show(MakeSolution());

            Transform single = _go.transform.Find("Pivot_0");
            Transform world = _go.transform.Find("Pivot_1");

            Assert.IsNull(single.Find("WorldRing"), "단독 핀에 바깥 원이 생겼다");
            Assert.IsNotNull(world.Find("WorldRing"), "월드 고정에 바깥 원이 없다");
        }

        /// <summary>
        /// 채움을 색에만 실으면 색약인 눈에 작동 여부가
        /// 안 보인다(WCAG 1.4.1). 바깥 원은 종류를 가르므로
        /// 축이 겹치지 않는다.
        /// </summary>
        [Test]
        public void 이을_것이_다_정해진_핀만_속이_찬다()
        {
            _view.Show(MakeSolution());

            Assert.AreEqual(ShapeSprites.Ring, DotOf("Pivot_0"),
                "상대를 기다리는 핀의 속이 찼다");
            Assert.AreEqual(ShapeSprites.Disc, DotOf("Pivot_1"),
                "완성된 핀의 속이 비었다");
        }

        Sprite DotOf(string marker) =>
            _go.transform.Find(marker).Find("Dot").GetComponent<SpriteRenderer>().sprite;

        [Test]
        public void 다시_보여도_늘어나지_않는다()
        {
            _view.Show(MakeSolution());
            int first = _go.transform.childCount;

            _view.Show(MakeSolution());

            Assert.AreEqual(first, _go.transform.childCount,
                "되돌리기로 사라진 핀이 화면에 남는다");
        }
    }
}
