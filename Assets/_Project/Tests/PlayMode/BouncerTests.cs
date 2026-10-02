using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 되튕김이 실제 물리로 나오는가.
    /// 조건(다가오는 속도)이 붙어 있어 "튕긴다"만으로는
    /// 부족하다 — 안 튕겨야 할 때 안 튕기는 것까지 본다.
    /// </summary>
    public class BouncerTests
    {
        /// 떨어뜨리는 높이. 바운서 표면까지 2.5wu 쯤 된다.
        const float DropFrom = 4f;

        [Test]
        public void 떨어지는_공이_위로_되튕긴다()
        {
            float rise = MaxRise(TestLevels.BouncerDrop(DropFrom), 240);

            Assert.Greater(rise, 1f,
                $"바운서에 떨어뜨렸는데 공이 위로 가지 않았다 (최대 상승 속도 {rise:F2}).");
        }

        [Test]
        public void 바운서가_없으면_되튕기지_않는다()
        {
            // 대조군. 지면은 공을 튕기지 않는다 —
            // 위 테스트의 상승이 바운서 때문임을 말하려면 필요하다.
            var level = TestLevels.BouncerDrop(DropFrom);
            level.Devices.Clear();

            float rise = MaxRise(level, 240);

            Assert.Less(rise, 0.5f,
                $"바운서가 없는데 공이 위로 튀었다 (최대 상승 속도 {rise:F2}).");
        }

        [Test]
        public void 얹힌_공은_튕기지_않고_머문다()
        {
            // 다가오는 속도가 없으면 그냥 벽이다.
            var level = TestLevels.BouncerDrop(TestLevels.BouncerRestY);

            using (var world = WorldBuilder.Build(level, null, 0))
            {
                float top = world.Ball.position.y;

                for (int i = 0; i < 300; i++)
                {
                    world.Step();
                    top = Mathf.Max(top, world.Ball.position.y);
                }

                // 굴러떨어지는 것은 상관없다 — 구 위는 원래 불안정하다.
                // 여기서 볼 것은 "쏘아 올려지지 않는다" 하나다.
                Assert.Less(top, TestLevels.BouncerRestY + 0.2f,
                    "가만히 놓은 공이 튀어 올랐다 — 최소 접근 속도가 걸리지 않았다.");
            }
        }

        /// <summary>시뮬을 돌리며 본 가장 큰 상승 속도.</summary>
        static float MaxRise(LevelData level, int steps)
        {
            using (var world = WorldBuilder.Build(level, null, 0))
            {
                float rise = 0f;

                for (int i = 0; i < steps; i++)
                {
                    world.Step();
                    rise = Mathf.Max(rise, world.Ball.linearVelocity.y);
                }

                return rise;
            }
        }
    }
}
