using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 장치 실행 경로가 실제로 도는지 확인한다.
    ///
    /// 이 테스트가 없으면 <see cref="SimWorld.Step"/> 의 로직 루프는 0 회 반복하고
    /// <see cref="Judge"/> 의 <c>HasPendingWork</c> 분기는 실행되지 않는다.
    /// 동결하려는 코어 안에 한 번도 실행된 적 없는 분기를 남기지 않는 것이 목적이다.
    ///
    /// 여기서 최종 <c>SimOutcome</c> 을 단정하지 않는 것은 의도적이다 — 발동 이후 공이
    /// 언제 멈추는가는 마찰·감쇠 설정에 달린 물리 품질 문제이고, 장치 경로의 정당성과는 무관하다.
    /// </summary>
    public class DeviceTests
    {
        /// 폭탄이 터지는 스텝. 공이 먼저 잠들 만큼 넉넉히 뒤에 둔다.
        const int FireStep = 400;

        /// <summary>흔들림 없는 폭탄 하나를 얹은 월드. 발동 스텝이 정확히 <see cref="FireStep"/> 으로 고정된다.</summary>
        static SimWorld BuildWithBomb()
        {
            var bomb = new TestBomb(delaySteps: FireStep);
            var world = WorldBuilder.Build(TestLevels.FlatRest(), null, 0, new IStepLogic[] { bomb });
            bomb.Target = world.Ball;
            return world;
        }

        [Test]
        public void 주입한_장치가_로직으로_등록된다()
        {
            using (var world = BuildWithBomb())
            {
                Assert.IsTrue(world.AnyPendingWork(),
                    "폭탄을 하나 주입했는데 대기 중인 로직이 없다 — WorldBuilder 가 등록하지 않았다.");
            }
        }

        [Test]
        public void 장치는_바디를_늘리지_않는다()
        {
            // 장치는 콜라이더를 만들지 않는다. 바디 목록이 늘어나면 해시 순서가 밀려
            // 기존 레벨의 결정론 기준선이 통째로 어긋난다.
            using (var withBomb = BuildWithBomb())
            using (var without = WorldBuilder.Build(TestLevels.FlatRest(), null, 0))
            {
                Assert.AreEqual(without.Bodies.Count, withBomb.Bodies.Count);
            }
        }

        [Test]
        public void 대기_중인_장치가_있으면_전부_잠들어도_Stalled가_아니다()
        {
            // 이 보장이 없으면 "곧 터질 폭탄"이 있는 레벨을 솔버가 조기에 실패로 판정한다.
            using (var world = BuildWithBomb())
            {
                for (int i = 0; i < FireStep - 50; i++) world.Step();

                Assert.IsTrue(world.AllBodiesSleeping(), "공이 아직 잠들지 않았다 — 이 테스트의 전제가 깨졌다.");
                Assert.IsTrue(world.AnyPendingWork(), "폭탄이 아직 대기 중이어야 한다.");
                Assert.IsFalse(world.Judge.Stalled,
                    "전 바디가 잠들었다는 이유로 Stalled 가 확정됐다 — 대기 중인 장치를 보지 않았다.");
            }
        }

        [Test]
        public void 장치가_발동하면_대기가_끝나고_공이_움직인다()
        {
            using (var world = BuildWithBomb())
            {
                for (int i = 0; i < FireStep - 50; i++) world.Step();

                Vector2 before = world.Ball.position;

                // 발동 스텝을 지난다. 흔들림이 0 이라 정확히 FireStep 에서 터진다.
                for (int i = 0; i < 60; i++) world.Step();

                Assert.IsFalse(world.AnyPendingWork(), "발동했는데 대기 상태가 유지된다.");
                Assert.AreNotEqual(before.y, world.Ball.position.y,
                    "폭탄이 터졌는데 공이 움직이지 않았다 — 잠든 바디를 깨우지 못했을 가능성이 크다.");
            }
        }
    }
}
