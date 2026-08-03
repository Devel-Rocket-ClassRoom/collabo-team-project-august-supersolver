using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// StageData 계약 검사.
    /// 시드를 실행 인자에서 스테이지의 성질로
    /// 옮기는 것이 이 타입의 존재 이유다.
    /// </summary>
    public class StageContractTests
    {
        [Test]
        public void 스테이지는_JSON_왕복에서_보존된다()
        {
            var stage = new StageData
            {
                StageId = "S042",
                Seed = 4242,
                Level = new LevelData
                {
                    InkLimit = 13.5f,
                    BallStart = new Vector2(1f, 2f),
                    GoalPosition = new Vector2(3f, 4f),
                    KillY = -7f,
                },
            };

            var restored = StageData.FromJson(stage.ToJson());

            Assert.AreEqual(stage.StageId, restored.StageId);
            Assert.AreEqual(stage.Seed, restored.Seed, "시드가 왕복에서 사라지면 재현이 불가능해진다.");
            Assert.AreEqual(stage.Level.BallStart, restored.Level.BallStart);
            Assert.AreEqual(stage.Level.InkLimit, restored.Level.InkLimit, 1e-4f);
            Assert.AreEqual(stage.Level.GoalPosition, restored.Level.GoalPosition);
            Assert.AreEqual(stage.Level.KillY, restored.Level.KillY, 1e-4f);
        }

        [Test]
        public void 장치가_있는_스테이지도_JSON_왕복에서_보존된다()
        {
            var stage = TestLevels.FragBombPitStage(seed: 99);
            var restored = StageData.FromJson(stage.ToJson());

            Assert.AreEqual(99, restored.Seed);
            Assert.AreEqual(stage.Level.Devices.Count, restored.Level.Devices.Count);

            var original = stage.Level.Devices[0];
            var copy = restored.Level.Devices[0];

            Assert.AreEqual(DeviceType.FragBomb, copy.Type);
            Assert.AreEqual(original.DelaySteps, copy.DelaySteps);
            Assert.AreEqual(original.Power, copy.Power, 1e-4f);
        }

        [Test]
        public void 같은_레벨이라도_시드가_다르면_다른_스테이지다()
        {
            // 레벨이 같아도 시드가 다르면
            // 장치 거동이 달라 풀이가 달라진다.
            var a = TestLevels.FragBombPitStage(seed: 1);
            var b = TestLevels.FragBombPitStage(seed: 2);

            Assert.AreEqual(a.Level.BallStart, b.Level.BallStart, "같은 레벨을 쓰는 두 스테이지여야 한다.");
            Assert.AreNotEqual(a.Seed, b.Seed);
        }

        [Test]
        public void 스테이지_기본값은_시드_0이다()
        {
            // 시드를 안 적은 스테이지가 조용히 무작위 값을 갖는 일이 없어야 한다.
            Assert.AreEqual(0, new StageData().Seed);
        }
    }
}
