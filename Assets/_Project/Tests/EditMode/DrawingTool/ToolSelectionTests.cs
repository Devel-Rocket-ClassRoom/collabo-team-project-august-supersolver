using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 회전축 슬롯 하나가 두 모드를 쥔다.
    /// 나머지 도구는 값 대입뿐이라 여기서 재지 않는다.
    /// </summary>
    public class ToolSelectionTests
    {
        GameObject _go;
        ToolSelection _tools;

        [SetUp]
        public void 툴바를_만든다()
        {
            _go = new GameObject("Tools");
            _tools = _go.AddComponent<ToolSelection>();
        }

        [TearDown]
        public void 툴바를_치운다() => Object.DestroyImmediate(_go);

        [Test]
        public void 회전축_슬롯은_다시_누르면_다른_모드로_넘어간다()
        {
            _tools.OnClickSelectPivotSingle();
            Assert.AreEqual(DrawTool.PivotSingle, _tools.Current,
                "안 고른 슬롯을 처음 누르면 내보이던 모드를 고른다");

            _tools.OnClickSelectPivotSingle();
            Assert.AreEqual(DrawTool.PivotWorld, _tools.Current,
                "이미 고른 모드를 또 눌렀는데 안 넘어갔다");
            Assert.AreEqual(DrawTool.PivotWorld, _tools.PivotMode);
        }

        [Test]
        public void 다른_도구를_거쳐_와도_슬롯은_마지막_모드를_기억한다()
        {
            _tools.OnClickSelectPivotSingle();
            _tools.OnClickSelectPivotSingle();   // 월드 고정으로 넘어간다

            _tools.OnClickSelectFixedLine();
            Assert.AreEqual(DrawTool.PivotWorld, _tools.PivotMode,
                "도구를 바꿨다고 슬롯 아이콘이 되돌아갔다");

            // 슬롯이 내보이는 건 월드 고정이라 그 버튼이 눌린다.
            _tools.OnClickSelectPivotWorld();
            Assert.AreEqual(DrawTool.PivotWorld, _tools.Current,
                "돌아온 첫 탭이 모드를 넘겨버렸다");
        }
    }
}
