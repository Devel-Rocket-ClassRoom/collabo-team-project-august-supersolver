using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 순서와 저장. 사전식 비교가 깨지면 해금 판정이,
    /// JsonUtility 왕복이 깨지면 진척도가 날아간다.
    /// </summary>
    public class StageEntryTests
    {
        [Serializable]
        class Holder
        {
            public StageEntry Entry;
        }

        [Serializable]
        class Row
        {
            public StageEntry Entry;
            public int Stars;
        }

        [Serializable]
        class RowList
        {
            public List<Row> Rows = new List<Row>();
        }

        [Test]
        public void 테마가_다르면_테마로_먼저_비교한다()
        {
            Assert.IsTrue(new StageEntry(0, 19) < new StageEntry(1, 0));
            Assert.IsTrue(new StageEntry(1, 0) > new StageEntry(0, 19));
        }

        [Test]
        public void 테마가_같으면_스테이지로_비교한다()
        {
            Assert.IsTrue(new StageEntry(1, 6) > new StageEntry(1, 5));
            Assert.IsTrue(new StageEntry(1, 5) <= new StageEntry(1, 6));
        }

        [Test]
        public void 같은_자리는_서로_같다()
        {
            var a = new StageEntry(0, 0);
            var b = new StageEntry(0, 0);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreEqual(0, a.CompareTo(b));
        }

        [Test]
        public void 필드로_가진_클래스가_왕복해도_값이_남는다()
        {
            var source = new Holder { Entry = new StageEntry(2, 7) };

            var restored = JsonUtility.FromJson<Holder>(JsonUtility.ToJson(source));

            Assert.AreEqual(new StageEntry(2, 7), restored.Entry);
        }

        [Test]
        public void 리스트_안에_들어가도_왕복해서_값이_남는다()
        {
            var source = new RowList();
            source.Rows.Add(new Row { Entry = new StageEntry(0, 3), Stars = 1 });
            source.Rows.Add(new Row { Entry = new StageEntry(4, 11), Stars = 3 });

            var restored = JsonUtility.FromJson<RowList>(JsonUtility.ToJson(source));

            Assert.AreEqual(2, restored.Rows.Count);
            Assert.AreEqual(new StageEntry(0, 3), restored.Rows[0].Entry);
            Assert.AreEqual(1, restored.Rows[0].Stars);
            Assert.AreEqual(new StageEntry(4, 11), restored.Rows[1].Entry);
            Assert.AreEqual(3, restored.Rows[1].Stars);
        }
    }
}
