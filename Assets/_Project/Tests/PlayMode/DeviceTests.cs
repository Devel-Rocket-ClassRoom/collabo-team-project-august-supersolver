using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 장치가 **레벨 데이터에서 나와** 실제로 도는지 확인한다.
    ///
    /// 이 테스트가 없으면 <see cref="SimWorld.Step"/> 의 로직 루프는 0 회 반복하고
    /// <see cref="Judge"/> 의 <c>HasPendingWork</c> 분기는 실행되지 않는다.
    /// 동결하려는 코어 안에 한 번도 실행된 적 없는 분기를 남기지 않는 것이 목적이다.
    ///
    /// 최종 <c>SimOutcome</c> 을 단정하지 않는 것은 의도적이다 — 발동 이후 공이 언제 멈추는가는
    /// 마찰·감쇠 설정에 달린 물리 품질 문제이고, 장치 경로의 정당성과는 무관하다.
    /// </summary>
    public class DeviceTests
    {
        [Test]
        public void 레벨_데이터의_장치가_로직으로_등록된다()
        {
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                Assert.IsTrue(world.AnyPendingWork(),
                    "폭탄이 하나 있는 레벨인데 대기 중인 로직이 없다 — WorldBuilder 가 등록하지 않았다.");
            }
        }

        [Test]
        public void 장치는_바디를_늘리지_않는다()
        {
            // 장치는 콜라이더를 만들지 않는다. 바디 목록이 늘어나면 해시 순서가 밀려
            // 기존 레벨의 결정론 기준선이 통째로 어긋난다.
            using (var withBomb = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            using (var without = WorldBuilder.Build(TestLevels.FlatRest(), null, 0))
            {
                Assert.AreEqual(without.Bodies.Count, withBomb.Bodies.Count);
            }
        }

        [Test]
        public void 대기_중인_장치가_있으면_전부_잠들어도_Stalled가_아니다()
        {
            // 이 보장이 없으면 "곧 터질 폭탄"이 있는 레벨을 솔버가 조기에 실패로 판정한다.
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                for (int i = 0; i < TestLevels.LateBombFireStep - 50; i++) world.Step();

                Assert.IsTrue(world.AllBodiesSleeping(), "공이 아직 잠들지 않았다 — 이 테스트의 전제가 깨졌다.");
                Assert.IsTrue(world.AnyPendingWork(), "폭탄이 아직 대기 중이어야 한다.");
                Assert.IsFalse(world.Judge.Stalled,
                    "전 바디가 잠들었다는 이유로 Stalled 가 확정됐다 — 대기 중인 장치를 보지 않았다.");
            }
        }

        [Test]
        public void 장치가_발동하면_대기가_끝나고_공이_움직인다()
        {
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                for (int i = 0; i < TestLevels.LateBombFireStep - 50; i++) world.Step();

                Vector2 before = world.Ball.position;

                // 발동 스텝을 지난다. 흔들림이 0 이라 정확히 LateBombFireStep 에서 터진다.
                for (int i = 0; i < 60; i++) world.Step();

                Assert.IsFalse(world.AnyPendingWork(), "발동했는데 대기 상태가 유지된다.");
                Assert.AreNotEqual(before.y, world.Ball.position.y,
                    "폭탄이 터졌는데 공이 움직이지 않았다 — 잠든 바디를 깨우지 못했을 가능성이 크다.");
            }
        }

        [Test]
        public void 반경_밖의_물체는_건드리지_않는다()
        {
            // 반경 판정이 없으면 폭탄이 화면 전체를 흔든다. 레벨 디자인이 성립하지 않는다.
            var level = TestLevels.FlatWithLateBomb();   // 폭탄 (0,0), 반경 3

            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(4.1f, 0.3f),
                new Vector2(4.9f, 0.3f),
            }));

            using (var world = WorldBuilder.Build(level, solution, 0))
            {
                var far = FindStroke(world, 0);

                for (int i = 0; i < TestLevels.LateBombFireStep - 50; i++) world.Step();
                Vector2 before = far.position;

                for (int i = 0; i < 60; i++) world.Step();

                Assert.IsFalse(world.AnyPendingWork(), "폭탄이 아직 안 터졌다 — 이 테스트의 전제가 깨졌다.");
                Assert.Less(Vector2.Distance(before, far.position), 0.05f,
                    $"반경 3 밖(거리 약 4.5)의 물체가 {Vector2.Distance(before, far.position):F3} 만큼 움직였다.");
            }
        }

        static Rigidbody2D FindStroke(SimWorld world, int strokeIndex)
        {
            string name = $"Stroke_{strokeIndex}";

            for (int i = 0; i < world.Bodies.Count; i++)
            {
                var body = world.Bodies[i];
                if (body != null && body.name == name) return body;
            }

            Assert.Fail($"{name} 바디를 찾지 못했다.");
            return null;
        }
    }
}
