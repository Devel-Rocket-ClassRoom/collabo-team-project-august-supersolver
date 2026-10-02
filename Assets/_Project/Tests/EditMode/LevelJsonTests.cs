using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 파일의 값이 정확히 들어오는가.
    /// JsonUtility 는 모르는 키를 무시하고
    /// 없는 키를 기본값으로 채운다.
    /// </summary>
    public class LevelJsonTests
    {
        [Test]
        public void 샘플_레벨_JSON이_저장소에_있다()
        {
            Assert.IsTrue(SampleLevelFile.Exists,
                "샘플 레벨 파일을 찾을 수 없다: " + SampleLevelFile.FullPath);
        }

        [Test]
        public void 파일의_값이_그대로_들어온다()
        {
            var level = SampleLevelFile.Load();

            Assert.AreEqual(SampleLevelFile.ExpectedInkLimit, level.InkLimit, 1e-4f);
            Assert.AreEqual(SampleLevelFile.ExpectedKillY, level.KillY, 1e-4f);

            Assert.AreEqual(SampleLevelFile.ExpectedBallStart, level.BallStart);
            Assert.AreEqual(SampleLevelFile.ExpectedGoalPosition, level.GoalPosition);
        }

        [Test]
        public void 지형이_비어_있지_않다()
        {
            var level = SampleLevelFile.Load();

            Assert.IsNotNull(level.Terrain);
            Assert.AreEqual(1, level.Terrain.Count, "지형 세그먼트 개수가 파일과 다르다.");
            Assert.AreEqual(SampleLevelFile.ExpectedTerrainA, level.Terrain[0].A);
            Assert.AreEqual(SampleLevelFile.ExpectedTerrainB, level.Terrain[0].B);
        }

        [Test]
        public void 앵커_생성에_필요한_값이_전부_있다()
        {
            // 공 시작점·목표 위치·잉크 제한은 M03 앵커 샘플링의 원료다.
            // 기본값으로 채워지면 솔버가
            // 엉뚱한 자리에 부품을 놓는다.
            var level = SampleLevelFile.Load();

            Assert.AreNotEqual(Vector2.zero, level.BallStart, "공 시작점이 비어 있다.");
            Assert.AreNotEqual(Vector2.zero, level.GoalPosition, "목표 위치가 비어 있다.");
            Assert.Greater(level.InkLimit, 0f, "잉크 제한이 비어 있다.");
        }
    }
}
