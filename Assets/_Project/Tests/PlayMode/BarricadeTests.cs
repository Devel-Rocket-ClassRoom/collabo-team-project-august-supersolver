using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 바리게이트가 빠른 것과 느린 것을 가르는가.
    /// 부서지는 조건이 속도라 "부서진다"만으로는 부족하다 —
    /// 느리게 닿았을 때 막아 서는 것까지 본다.
    /// </summary>
    public class BarricadeTests
    {
        /// 굴러온 공이 부딪히고 남을 만큼.
        const int RollSteps = 200;

        /// 떨어뜨린 자유 물체가 닿고 남을 만큼.
        const int DropSteps = 100;

        /// 밀려난 공이 눈에 띄게 굴러갈 만큼.
        const int BlastSteps = 150;

        /// 공이 여기를 넘었으면 바리게이트를 지나간 것이다.
        const float PassedX = 1.3f;

        [Test]
        public void 느리게_굴러온_공은_막힌다()
        {
            var start = new BallState(new Vector2(-1f, 0.3f), new Vector2(2f, 0f));

            using (var world = WorldBuilder.Build(TestLevels.BarricadeWall(), null, 0, start))
            {
                for (int i = 0; i < RollSteps; i++) world.Step();

                Assert.IsNotNull(Barricade(world),
                    "부수는 속도에 못 미치는 공이 바리게이트를 부쉈다.");
                Assert.Less(world.Ball.position.x, PassedX,
                    $"공이 바리게이트를 지나갔다 (x {world.Ball.position.x:F2}).");
            }
        }

        [Test]
        public void 빠르게_부딪힌_공이_부순다()
        {
            var start = new BallState(new Vector2(-1f, 0.3f),
                new Vector2(TestLevels.BarricadeBreakSpeed * 2f, 0f));

            using (var world = WorldBuilder.Build(TestLevels.BarricadeWall(), null, 0, start))
            {
                for (int i = 0; i < RollSteps; i++) world.Step();

                Assert.IsNull(Barricade(world),
                    "부수는 속도를 넘겼는데 바리게이트가 남아 있다.");
            }
        }

        [Test]
        public void 떨어뜨린_자유_물체가_부순다()
        {
            using (var world = WorldBuilder.Build(
                TestLevels.BarricadeWall(), TestLevels.BarricadeDropSolution(), 0))
            {
                for (int i = 0; i < DropSteps; i++) world.Step();

                Assert.IsNull(Barricade(world),
                    "자유 물체를 떨어뜨렸는데 바리게이트가 남아 있다.");
            }
        }

        [Test]
        public void 부서지면_옆에_있던_공을_밀어낸다()
        {
            var start = new BallState(new Vector2(1f, 0.25f), Vector2.zero);

            using (var world = WorldBuilder.Build(
                TestLevels.BarricadeWall(), TestLevels.BarricadeDropSolution(), 0, start))
            {
                for (int i = 0; i < BlastSteps; i++) world.Step();

                float moved = start.Position.x - world.Ball.position.x;

                Assert.Greater(moved, 0.3f,
                    $"바리게이트가 부서졌는데 공이 안 밀렸다 (밀린 거리 {moved:F2}).");
            }
        }

        [Test]
        public void 부서지지_않으면_옆의_공은_그대로다()
        {
            // 대조군. 위 테스트의 밀림이 파괴 때문임을 말하려면 필요하다.
            var start = new BallState(new Vector2(1f, 0.25f), Vector2.zero);

            using (var world = WorldBuilder.Build(TestLevels.BarricadeWall(), null, 0, start))
            {
                for (int i = 0; i < BlastSteps; i++) world.Step();

                float moved = Mathf.Abs(start.Position.x - world.Ball.position.x);

                Assert.Less(moved, 0.05f,
                    $"아무도 안 건드렸는데 공이 움직였다 (움직인 거리 {moved:F2}).");
            }
        }

        /// 남아 있으면 바디, 부서졌으면 null.
        static Rigidbody2D Barricade(SimWorld world) => world.GetDevice(0).body;
    }
}
