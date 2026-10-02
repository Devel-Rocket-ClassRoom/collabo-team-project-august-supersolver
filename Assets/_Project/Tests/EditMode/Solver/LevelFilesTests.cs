using NUnit.Framework;
using PPS.Core;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 뷰어가 폴더에서 읽어 온 판이 실제 파일과 같은가.
    /// StageData.FromJson 을 거치지 않는 유일한 경로라
    /// 마이그레이션을 빼먹어도 컴파일도 다른 테스트도 안 잡는다.
    /// </summary>
    public class LevelFilesTests
    {
        /// 장치 3종이 한 판에 든 스테이지. 하나라도 빠지면 드러난다.
        const string StageWithDevices = "Stage15";
        const int ExpectedDeviceCount = 3;

        [Test]
        public void 폴더에서_읽은_판에_장치가_실려_있다()
        {
            var entry = Find(StageWithDevices);

            Assert.IsTrue(entry.Usable, $"{StageWithDevices} 를 읽지 못했다 — {entry.Problem}");
            Assert.AreEqual(ExpectedDeviceCount, entry.Stage.Level.Devices.Count,
                "장치가 비었다 — 옛 형식 파일이 마이그레이션을 타지 못했다.");
        }

        [Test]
        public void 폴더에서_읽은_판은_현재_형식이다()
        {
            Assert.AreEqual(StageData.CurrentVersion, Find(StageWithDevices).Stage.Version);
        }

        static LevelFiles.Entry Find(string name)
        {
            var entries = LevelFiles.LoadAll();

            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Name == name) return entries[i];

            Assert.Fail($"{name}.json 을 폴더에서 찾지 못했다.");
            return default;
        }
    }
}
