using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 회전축은 탭 둘로 갈라져 저마다 제 모드를 고른다.
    /// 한 슬롯을 다시 눌러 넘기던 토글을 지운 자리라
    /// 되살아나지 않게 여기서 못 박는다.
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
        public void 회전축_탭은_저마다_제_모드를_고른다()
        {
            _tools.OnClickSelectPivotSingle();
            Assert.AreEqual(DrawTool.PivotSingle, _tools.Current);

            _tools.OnClickSelectPivotWorld();
            Assert.AreEqual(DrawTool.PivotWorld, _tools.Current,
                "월드 고정 탭이 제 모드를 못 골랐다");
        }

        [Test]
        public void 같은_탭을_다시_눌러도_모드가_안_넘어간다()
        {
            _tools.OnClickSelectPivotSingle();
            _tools.OnClickSelectPivotSingle();

            Assert.AreEqual(DrawTool.PivotSingle, _tools.Current,
                "탭이 갈라졌는데 옛 토글이 살아 있다");
        }
    }
}
