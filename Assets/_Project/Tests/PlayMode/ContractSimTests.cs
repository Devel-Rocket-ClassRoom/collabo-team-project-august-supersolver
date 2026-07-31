using System.Collections.Generic;
using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 계약 타입 중 **실제로 시뮬을 돌려봐야 확인되는 것**만 모은다.
    /// 월드를 만들지 않는 나머지 계약 검사는 EditMode 쪽 <c>ContractTests</c> 에 있다.
    /// </summary>
    public class ContractSimTests
    {
        [Test]
        public void 복원한_레벨데이터로_돌린_결과가_원본과_같다()
        {
            // "레벨 추가는 데이터만으로" 가 성립하려면 직렬화 왕복이 시뮬 결과를 바꾸면 안 된다.
            var original = TestLevels.RampToGoal();
            var restored = LevelData.FromJson(original.ToJson());

            var a = new List<ulong>();
            var b = new List<ulong>();
            SimRunner.RunTraced(original, null, 3, a, 300);
            SimRunner.RunTraced(restored, null, 3, b, 300);

            CollectionAssert.AreEqual(a, b);
        }
    }
}
