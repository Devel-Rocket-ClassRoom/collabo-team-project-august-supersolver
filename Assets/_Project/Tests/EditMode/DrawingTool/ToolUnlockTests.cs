using NUnit.Framework;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 해금 스테이지 번호는 기획이 준 세 개(고정선·자유물체·
    /// 월드핀)와 우리가 채운 두 개가 섞여 있다. 값이 바뀌면
    /// 여기가 먼저 깨져 어디를 고쳐야 하는지 드러난다.
    /// </summary>
    public class ToolUnlockTests
    {
        /// 지우개가 같이 열려 있는 것은 1스테이지
        /// 튜토리얼이 그 탭을 누르게 시키기 때문이다.
        [Test]
        public void 첫_스테이지에는_고정선과_지우개만_열린다()
        {
            Assert.IsTrue(ToolUnlock.IsUnlocked(DrawTool.FixedLine, 1));
            Assert.IsTrue(ToolUnlock.IsUnlocked(DrawTool.Erase, 1));

            Assert.IsFalse(ToolUnlock.IsUnlocked(DrawTool.FreeBody, 1));
            Assert.IsFalse(ToolUnlock.IsUnlocked(DrawTool.PivotSingle, 1));
            Assert.IsFalse(ToolUnlock.IsUnlocked(DrawTool.PivotWorld, 1));
        }

        [Test]
        public void 자유물체는_11스테이지에_열린다()
        {
            Assert.IsFalse(ToolUnlock.IsUnlocked(DrawTool.FreeBody, 10));
            Assert.IsTrue(ToolUnlock.IsUnlocked(DrawTool.FreeBody, 11));
        }

        [Test]
        public void 월드핀은_2챕터_7스테이지에_열린다()
        {
            Assert.IsFalse(ToolUnlock.IsUnlocked(DrawTool.PivotWorld, 26));
            Assert.IsTrue(ToolUnlock.IsUnlocked(DrawTool.PivotWorld, 27));
        }

        [Test]
        public void 한번_열린_도구는_뒤_스테이지에서도_열려_있다()
        {
            Assert.IsTrue(ToolUnlock.IsUnlocked(DrawTool.FreeBody, 20));
        }
    }
}
