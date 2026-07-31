using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// "프레임 속도가 결과에 영향을 주면 안 됨" (미션 가이드) 을 구조로 충족하는지 확인한다.
    /// 누적기가 경과 시간을 인자로 받게 만든 이유가 이 테스트다.
    /// </summary>
    public class FrameIndependenceTests
    {
        const int TargetStep = 300;   // 5초

        [Test]
        public void 프레임레이트가_달라도_같은_스텝에서_같은_상태다()
        {
            ulong hash30 = DriveTo(TargetStep, 1f / 30f, 150, out int steps30);
            ulong hash60 = DriveTo(TargetStep, 1f / 60f, 300, out int steps60);
            ulong hash120 = DriveTo(TargetStep, 1f / 120f, 600, out int steps120);

            Assert.AreEqual(TargetStep, steps30, "30fps 에서 스텝 수가 어긋났다.");
            Assert.AreEqual(TargetStep, steps60, "60fps 에서 스텝 수가 어긋났다.");
            Assert.AreEqual(TargetStep, steps120, "120fps 에서 스텝 수가 어긋났다.");

            Assert.AreEqual(hash60, hash30, $"30fps 결과가 다르다. 60=0x{hash60:X16} 30=0x{hash30:X16}");
            Assert.AreEqual(hash60, hash120, $"120fps 결과가 다르다. 60=0x{hash60:X16} 120=0x{hash120:X16}");
        }

        [Test]
        public void 누적기로_돌린_결과가_직접_Step_한_결과와_같다()
        {
            ulong driven = DriveTo(TargetStep, 1f / 60f, 300, out _);

            ulong direct;
            using (var world = WorldBuilder.Build(TestLevels.LongRoll(), null, 0))
            {
                for (int i = 0; i < TargetStep; i++) world.Step();
                direct = WorldHasher.Hash(world);
            }

            Assert.AreEqual(direct, driven,
                "드라이버를 거치면 결과가 달라진다 — 드라이버가 시뮬에 개입하고 있다.");
        }

        [Test]
        public void 일시정지_중에는_전진하지_않는다()
        {
            using (var world = WorldBuilder.Build(TestLevels.LongRoll(), null, 0))
            {
                var accumulator = new SimAccumulator { Paused = true };

                for (int i = 0; i < 60; i++) accumulator.Advance(world, 1f / 60f);

                Assert.AreEqual(0, world.CurrentStep);
            }
        }

        [Test]
        public void 배속은_스텝_수만_늘리고_스텝_내용은_그대로다()
        {
            ulong normal = DriveTo(120, 1f / 60f, 120, out _);

            ulong doubled;
            int doubledSteps;
            using (var world = WorldBuilder.Build(TestLevels.LongRoll(), null, 0))
            {
                var accumulator = new SimAccumulator { SpeedMultiplier = 2f };

                // 절반의 프레임 수로 같은 스텝에 도달해야 한다.
                for (int i = 0; i < 60; i++) accumulator.Advance(world, 1f / 60f);

                doubledSteps = world.CurrentStep;
                doubled = WorldHasher.Hash(world);
            }

            Assert.AreEqual(120, doubledSteps, "2배속인데 스텝 수가 2배가 아니다.");
            Assert.AreEqual(normal, doubled,
                "배속이 물리 결과를 바꿨다 — dt 를 건드렸을 가능성이 크다.");
        }

        /// <summary>지정한 프레임 수만큼 누적기로 전진시키고, 도달한 스텝의 해시를 돌려준다.</summary>
        static ulong DriveTo(int expectedStep, float frameDelta, int frameCount, out int reachedStep)
        {
            using (var world = WorldBuilder.Build(TestLevels.LongRoll(), null, 0))
            {
                var accumulator = new SimAccumulator();

                for (int i = 0; i < frameCount; i++)
                {
                    if (world.CurrentStep >= expectedStep) break;
                    accumulator.Advance(world, frameDelta);
                }

                reachedStep = world.CurrentStep;
                return WorldHasher.Hash(world);
            }
        }
    }
}
