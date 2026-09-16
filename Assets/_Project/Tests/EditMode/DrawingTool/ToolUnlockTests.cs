using NUnit.Framework;
using PPS.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 해금 규칙만 본다. 어느 도구가 어디서 열리는지는
    /// ToolUnlockTable.asset 의 기획값이라 여기서 고정하지
    /// 않는다 — 값이 바뀔 때마다 깨지면 쓸모가 없다.
    /// </summary>
    public class ToolUnlockTests
    {
        ToolUnlockTable _table;

        [SetUp]
        public void SetUp()
        {
            _table = ScriptableObject.CreateInstance<ToolUnlockTable>();
            _table.Entries = new[]
            {
                new ToolUnlockTable.Entry
                {
                    Tool = DrawTool.FreeBody,
                    At = new StageEntry(1, 5),
                },
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
        }

        [Test]
        public void 해금_자리에_정확히_서면_열린다()
        {
            Assert.IsTrue(_table.IsUnlocked(DrawTool.FreeBody, new StageEntry(1, 5)));
        }

        [Test]
        public void 해금_자리_한_칸_앞은_잠겨_있다()
        {
            Assert.IsFalse(_table.IsUnlocked(DrawTool.FreeBody, new StageEntry(1, 4)));
        }

        /// 앞 테마의 뒤 스테이지가 뒤 테마를 앞지르지
        /// 않는다는 것까지 같이 본다.
        [Test]
        public void 앞_테마에서는_스테이지가_커도_잠겨_있다()
        {
            Assert.IsFalse(_table.IsUnlocked(DrawTool.FreeBody, new StageEntry(0, 99)));
        }

        [Test]
        public void 한번_열린_도구는_뒤_테마에서도_열려_있다()
        {
            Assert.IsTrue(_table.IsUnlocked(DrawTool.FreeBody, new StageEntry(2, 0)));
        }

        [Test]
        public void 표에_없는_도구는_경고만_남기고_0_0_을_준다()
        {
            LogAssert.Expect(LogType.Warning, "ToolUnlockTable: 도구 Erase 가 표에 없다");

            Assert.AreEqual(new StageEntry(0, 0), _table.EntryOf(DrawTool.Erase));
        }
    }
}
