using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 시뮬을 돌려야 확인되는 계약만 모은다.
    /// 나머지는 ContractTests 에 있다.
    /// </summary>
    public class ContractSimTests
    {
        [Test]
        public void 복원한_레벨데이터로_돌린_결과가_원본과_같다()
        {
            // 직렬화 왕복이 시뮬 결과를 바꾸면 안 된다.
            var original = TestLevels.RampToGoal();
            var restored = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(original));

            var a = new List<ulong>();
            var b = new List<ulong>();
            SimRunner.RunTraced(original, null, 3, a, 300);
            SimRunner.RunTraced(restored, null, 3, b, 300);

            CollectionAssert.AreEqual(a, b);
        }
    }
}
