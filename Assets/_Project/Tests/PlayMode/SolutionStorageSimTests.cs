using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 완료조건 4-2. 저장을 거친 그림이 같은 물리를
    /// 내야 기기에서 그린 판과 솔버가 검증한 판이
    /// 같은 판임을 말할 수 있다.
    /// </summary>
    public class SolutionStorageSimTests
    {
        const string StageId = "T_SolutionStorageSim";

        [TearDown]
        public void 남긴_파일을_지운다()
        {
            string path = SolutionStorage.PathOf(StageId);
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void 저장하고_읽은_그림이_같은_물리를_낸다()
        {
            var original = FeatureLevelFile.LoadSolution();

            Assert.IsTrue(SolutionStorage.Save(StageId, original), "저장에 실패했다.");
            Assert.IsTrue(SolutionStorage.TryLoad(StageId, out Solution restored),
                "저장한 파일을 다시 읽지 못했다.");

            var a = new List<ulong>();
            var b = new List<ulong>();

            // 레벨을 두 번 읽는다 — 한 인스턴스를 두 번
            // 돌리면 월드 구축이 남긴 흔적이 섞일 수 있다.
            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), original, 11, a, 600);
            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), restored, 11, b, 600);

            Assert.Greater(a.Count, 0, "시뮬이 한 스텝도 진행되지 않았다.");
            CollectionAssert.AreEqual(a, b);
        }
    }
}
