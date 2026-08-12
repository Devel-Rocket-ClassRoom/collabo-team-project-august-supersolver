using System.Collections.Generic;
using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 완료조건 4-2. JSON 을 거친 그림이 같은 물리를
    /// 내야 저장한 판과 솔버가 검증한 판이 같은 판임을
    /// 말할 수 있다.
    /// </summary>
    public class ReplayJsonSimTests
    {
        [Test]
        public void JSON_왕복을_거친_그림이_같은_물리를_낸다()
        {
            Solution original = FeatureLevelFile.LoadSolution();

            ReplayData loaded = ReplayData.FromJson(
                ReplayData.Create(new StageData(), original).ToJson());

            Assert.IsNotNull(loaded, "저장한 JSON 을 다시 읽지 못했다.");

            var a = new List<ulong>();
            var b = new List<ulong>();

            // 레벨을 두 번 읽는다 — 한 인스턴스를 두 번
            // 돌리면 월드 구축이 남긴 흔적이 섞일 수 있다.
            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), original, 11, a, 600);
            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), loaded.Solution, 11, b, 600);

            Assert.Greater(a.Count, 0, "시뮬이 한 스텝도 진행되지 않았다.");
            CollectionAssert.AreEqual(a, b);
        }
    }
}
