using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 샘플 레벨 JSON 이 저장소에 있고, 파일의 값이 <see cref="LevelData"/> 로 **정확히** 들어오는가.
    ///
    /// 월드를 만들지 않으므로 EditMode 다. 시뮬을 돌리는 검사는 PlayMode 쪽
    /// <c>LevelJsonSimTests</c> 에 있다.
    ///
    /// 값을 하나하나 대조하는 이유는 JsonUtility 의 성질 때문이다 — **모르는 키를 조용히 무시하고
    /// 없는 키를 기본값으로 채운다.** 키 이름에 오타가 나도 예외 없이 엉뚱한 레벨이 만들어지므로,
    /// "파싱에 성공했다"만으로는 아무것도 보장되지 않는다.
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

            Assert.AreEqual(SampleLevelFile.ExpectedId, level.Id);
            Assert.AreEqual(SampleLevelFile.ExpectedInkLimit, level.InkLimit, 1e-4f);
            Assert.AreEqual(SampleLevelFile.ExpectedBallRadius, level.BallRadius, 1e-4f);
            Assert.AreEqual(SampleLevelFile.ExpectedGoalRadius, level.GoalRadius, 1e-4f);
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
            // 셋 중 하나라도 기본값으로 조용히 채워지면 솔버가 엉뚱한 자리에 부품을 놓는다.
            var level = SampleLevelFile.Load();

            Assert.AreNotEqual(Vector2.zero, level.BallStart, "공 시작점이 비어 있다.");
            Assert.AreNotEqual(Vector2.zero, level.GoalPosition, "목표 위치가 비어 있다.");
            Assert.Greater(level.InkLimit, 0f, "잉크 제한이 비어 있다.");
        }
    }
}
