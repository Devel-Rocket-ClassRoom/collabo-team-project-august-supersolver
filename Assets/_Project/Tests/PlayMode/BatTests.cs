using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 박쥐가 막힌 것과 밀어낸 것을 가르는가.
    /// 접촉이 아니라 어긋남으로 판정하므로 "사라진다"만으로는
    /// 부족하다 — 밀고 지나갈 때 안 사라지는 것까지 본다.
    /// </summary>
    public class BatTests
    {
        /// <summary>
        /// 벽에 닿고 나서 사라지기까지를 넉넉히 덮는다.
        /// 막힌 박쥐는 5초어치를 뒤처져야 죽으므로
        /// 300 스텝 넘게 버틴다.
        /// </summary>
        const int WallSteps = 420;

        /// 자유 물체를 밀고 난 뒤. 더 돌리면 물체가 땅에 닿는다.
        const int PushSteps = 30;

        /// 좁게 잡은 판의 오른쪽 끝을 지나고 남을 만큼.
        const int OffscreenSteps = 200;

        [Test]
        public void 막을_것이_없으면_예측한_자리로_난다()
        {
            const int steps = 60;

            using (var world = WorldBuilder.Build(TestLevels.BatFlight(), null, 0))
            {
                for (int i = 0; i < steps; i++) world.Step();

                Vector2 expected = TestLevels.BatFrom
                    + Vector2.right * (TestLevels.BatSpeed * steps * SimWorld.FixedDt);

                Rigidbody2D bat = BatBody(world);
                Assert.IsNotNull(bat, "빈 하늘을 나는데 박쥐가 사라졌다.");
                Assert.AreEqual(expected.x, bat.position.x, 0.01f, "날아간 거리");
                Assert.AreEqual(expected.y, bat.position.y, 0.01f, "나는 높이");
            }
        }

        [Test]
        public void 벽에_막히면_사라진다()
        {
            using (var world = WorldBuilder.Build(TestLevels.BatIntoWall(), null, 0))
            {
                for (int i = 0; i < WallSteps; i++) world.Step();

                Assert.IsNull(BatBody(world), "벽에 막혔는데 박쥐가 남아 있다.");
            }
        }

        [Test]
        public void 막히지_않으면_같은_시간_동안_사라지지_않는다()
        {
            // 대조군. 위 테스트의 소멸이 벽 때문임을 말하려면 필요하다.
            using (var world = WorldBuilder.Build(TestLevels.BatFlight(), null, 0))
            {
                for (int i = 0; i < WallSteps; i++) world.Step();

                Assert.IsNotNull(BatBody(world), "막은 것이 없는데 박쥐가 사라졌다.");
            }
        }

        [Test]
        public void 판_밖으로_나가면_사라진다()
        {
            // 판이 품는 자리는 공·목표·지형·장치가 정한다.
            // 목표를 가까이 옮겨 오른쪽 끝을 당긴다.
            var level = TestLevels.BatFlight();
            level.GoalPosition = new Vector2(2f, 0.5f);

            using (var world = WorldBuilder.Build(level, null, 0))
            {
                for (int i = 0; i < OffscreenSteps; i++) world.Step();

                Assert.IsNull(BatBody(world),
                    "판 밖으로 나간 박쥐가 남아 있다 — 잠들지 않아 판정이 안 난다.");
            }

            // 대조군. 같은 스텝이라도 판이 넓으면 아직 안 나간다.
            using (var world = WorldBuilder.Build(TestLevels.BatFlight(), null, 0))
            {
                for (int i = 0; i < OffscreenSteps; i++) world.Step();

                Assert.IsNotNull(BatBody(world), "판 안을 나는 박쥐가 사라졌다.");
            }
        }

        [Test]
        public void 자유_물체는_밀고_지나가며_사라지지_않는다()
        {
            using (var world = WorldBuilder.Build(
                TestLevels.BatFlight(), TestLevels.BatFreeBodySolution(), 0))
            {
                for (int i = 0; i < PushSteps; i++) world.Step();

                float moved = world.StrokeBodies[0].position.x - TestLevels.BatFreeBodyX;

                Assert.Greater(moved, 0.25f,
                    $"박쥐가 자유 물체를 밀지 못했다 (옮긴 거리 {moved:F2}).");
                Assert.IsNotNull(BatBody(world),
                    "자유 물체에 부딪혔다고 박쥐가 사라졌다.");
            }
        }

        [Test]
        public void 박쥐가_없으면_자유_물체는_제자리에서_떨어진다()
        {
            // 대조군. 위 테스트의 이동이 박쥐 때문임을 말하려면 필요하다.
            var level = TestLevels.BatFlight();
            level.Devices.Clear();

            using (var world = WorldBuilder.Build(level, TestLevels.BatFreeBodySolution(), 0))
            {
                for (int i = 0; i < PushSteps; i++) world.Step();

                float moved = world.StrokeBodies[0].position.x - TestLevels.BatFreeBodyX;

                Assert.Less(Mathf.Abs(moved), 0.05f,
                    $"박쥐가 없는데 자유 물체가 옆으로 갔다 (옮긴 거리 {moved:F2}).");
            }
        }

        /// 살아 있으면 바디, 사라졌으면 null.
        static Rigidbody2D BatBody(SimWorld world) => world.GetDevice(0).body;
    }
}
