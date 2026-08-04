using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 장치가 레벨 데이터에서 나와 도는지 본다.
    /// 최종 Outcome 은 단정하지 않는다 —
    /// 그건 마찰·감쇠에 달린 물리 품질 문제다.
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
        public void 폭탄이_바디를_가진다()
        {
            // 장치 바디는 지형 뒤·스트로크 앞에 들어간다.
            // 레벨과 유저 그림의 경계가 그 자리다.
            using (var withBomb = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            using (var without = WorldBuilder.Build(TestLevels.FlatRest(), null, 0))
            {
                Assert.AreEqual(without.Bodies.Count + 1, withBomb.Bodies.Count,
                    "폭탄 몸체가 바디 목록에 등록되지 않았다.");

                var body = FindDevice(withBomb, 0);
                Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
                Assert.IsNotNull(body.GetComponent<CircleCollider2D>(), "콜라이더가 없다.");
            }
        }

        [Test]
        public void 폭탄이_터지면_바디가_사라진다()
        {
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                int slot = IndexOfDevice(world, 0);
                int countBefore = world.Bodies.Count;

                for (int i = 0; i < TestLevels.LateBombFireStep - 10; i++) world.Step();
                Assert.IsNotNull(world.Bodies[slot], "아직 안 터졌는데 몸체가 사라졌다.");

                for (int i = 0; i < 30; i++) world.Step();

                Assert.IsTrue(world.Bodies[slot] == null, "터졌는데 몸체가 남아 있다.");

                // 자리는 유지된다. 빼면 뒤 인덱스가 밀린다.
                Assert.AreEqual(countBefore, world.Bodies.Count,
                    "파괴된 바디를 목록에서 빼버렸다 — 뒤 인덱스가 전부 밀린다.");
            }
        }

        [Test]
        public void 파괴_시점이_프레임_분할에_영향받지_않는다()
        {
            // Destroy 를 쓰면 여기서 걸린다.
            // 한 프레임에 도는 스텝 수가 바깥 사정이라
            // 사라지는 스텝이 프레임 경계를 탄다.
            const int Target = TestLevels.LateBombFireStep + 20;

            ulong direct;
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                for (int i = 0; i < Target; i++) world.Step();
                direct = WorldHasher.Hash(world);
            }

            ulong driven;
            using (var world = WorldBuilder.Build(TestLevels.FlatWithLateBomb(), null, 0))
            {
                var accumulator = new SimAccumulator();

                // 30fps 상당. 한 프레임에 2스텝씩.
                for (int i = 0; i < Target && world.CurrentStep < Target; i++)
                    accumulator.Advance(world, 1f / 30f);

                Assert.AreEqual(Target, world.CurrentStep, "목표 스텝에 도달하지 못했다.");
                driven = WorldHasher.Hash(world);
            }

            Assert.AreEqual(direct, driven,
                "프레임 분할에 따라 결과가 달라졌다 — 파괴가 지연되고 있을 가능성이 크다.");
        }

        [Test]
        public void 대기_중인_장치가_있으면_전부_잠들어도_Stalled가_아니다()
        {
            // 없으면 곧 터질 폭탄이 있는 레벨을
            // 솔버가 조기에 실패로 판정한다.
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

                // 흔들림이 0 이라 정확히 그 스텝에 터진다.
                for (int i = 0; i < 60; i++) world.Step();

                Assert.IsFalse(world.AnyPendingWork(), "발동했는데 대기 상태가 유지된다.");
                Assert.Greater(Vector2.Distance(before, world.Ball.position), 0.1f,
                    "폭탄이 터졌는데 공이 움직이지 않았다 — 잠든 바디를 깨우지 못했을 가능성이 크다.");
            }
        }

        [Test]
        public void 반경_밖의_물체는_건드리지_않는다()
        {
            // 반경 판정이 없으면 화면 전체가 흔들린다.
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
            => FindByName(world, $"Stroke_{strokeIndex}");

        static Rigidbody2D FindDevice(SimWorld world, int deviceIndex)
            => FindByName(world, $"Device_{deviceIndex}");

        static int IndexOfDevice(SimWorld world, int deviceIndex)
        {
            string name = $"Device_{deviceIndex}";

            for (int i = 0; i < world.Bodies.Count; i++)
            {
                var body = world.Bodies[i];
                if (body != null && body.name == name) return i;
            }

            Assert.Fail($"{name} 바디를 찾지 못했다.");
            return -1;
        }

        static Rigidbody2D FindByName(SimWorld world, string name)
        {
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
