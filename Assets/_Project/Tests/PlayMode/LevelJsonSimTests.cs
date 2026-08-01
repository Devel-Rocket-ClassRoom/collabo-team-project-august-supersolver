using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// **"레벨 추가는 데이터만으로"의 증명.** 저장소에 커밋된 JSON 파일 하나가
    /// 코드 수정 없이 월드가 되고, 시뮬이 돌고, 판정까지 나오는지 확인한다.
    ///
    /// 파일 내용의 파싱 검증은 EditMode 쪽 <c>LevelJsonTests</c> 에 있다.
    /// 여기는 그 뒤 — 데이터가 실제로 물리에 도달하는지를 본다.
    /// </summary>
    public class LevelJsonSimTests
    {
        [Test]
        public void 파일에서_읽은_레벨로_월드가_구축된다()
        {
            using (var world = WorldBuilder.Build(SampleLevelFile.Load(), null, 0))
            {
                Assert.IsNotNull(world.Ball, "공이 만들어지지 않았다.");
                Assert.Greater(world.Bodies.Count, 1, "공 말고는 아무것도 만들어지지 않았다 — 지형이 비었다.");
            }
        }

        [Test]
        public void JSON의_값이_물리에_그대로_반영된다()
        {
            // 파싱만 맞고 월드 구축에서 흘리면 소용이 없다. 파일의 숫자가 바디에 닿았는지 본다.
            var level = SampleLevelFile.Load();

            using (var world = WorldBuilder.Build(level, null, 0))
            {
                Assert.AreEqual(SampleLevelFile.ExpectedBallStart.x, world.Ball.position.x, 1e-3f);
                Assert.AreEqual(SampleLevelFile.ExpectedBallStart.y, world.Ball.position.y, 1e-3f);

                var circle = world.Ball.GetComponent<CircleCollider2D>();
                Assert.IsNotNull(circle);
                Assert.AreEqual(SampleLevelFile.ExpectedBallRadius, circle.radius, 1e-4f,
                    "JSON 의 공 반지름이 콜라이더에 반영되지 않았다.");
            }
        }

        [Test]
        public void 파일에서_읽은_레벨이_Clear_된다()
        {
            // 데이터만으로 "풀리는 레벨"이 성립하는지. 여기까지 와야 솔버에 넘길 수 있다.
            var result = SimRunner.Run(SampleLevelFile.Load(), null, 0);

            Assert.AreEqual(SimOutcome.Clear, result.Outcome,
                $"샘플 레벨이 클리어되지 않았다 (MinGoalDist {result.MinGoalDist:F3}).");
            Assert.Less(result.EndStep, SimWorld.DefaultMaxSteps);
        }

        [Test]
        public void 파일을_거쳐도_시뮬_결과가_같다()
        {
            // 파일 → 객체 → JSON → 객체 왕복이 물리를 바꾸지 않아야
            // 에디터가 저장한 레벨과 솔버가 검증한 레벨이 같은 것임을 보장할 수 있다.
            var fromFile = SampleLevelFile.Load();
            var roundTripped = LevelData.FromJson(fromFile.ToJson());

            var a = new List<ulong>();
            var b = new List<ulong>();

            SimRunner.RunTraced(fromFile, null, 0, a, 300);
            SimRunner.RunTraced(roundTripped, null, 0, b, 300);

            CollectionAssert.AreEqual(a, b);
        }
    }
}
