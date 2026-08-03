using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 조인트가 실제로 바디를 붙잡는가.
    /// 조인트 생성에 실패해도 해시는 재현되므로
    /// 결정론 테스트로는 이걸 잡지 못한다.
    /// </summary>
    public class PivotTests
    {
        const int Steps = 120;   // 2초. 조인트가 없으면 바닥까지 떨어진다.

        /// 조인트가 살아 있으면 이 거리는 변하지 않는다.
        const float AnchorDistanceTolerance = 0.05f;

        [Test]
        public void 정적_스트로크에_매단_회전축이_바디를_붙잡는다()
        {
            // PickHost 가 정적 고정선을 버리고 막대를 고르는 경로.
            AssertHeldByAnchor(strokeIndex: 1, anchor: TestLevels.PivotOnFixedLine);
        }

        [Test]
        public void 월드_고정_회전축이_바디를_붙잡는다()
        {
            // Resolve 가 WorldIndex 를 null 로 푸는 경로.
            AssertHeldByAnchor(strokeIndex: 2, anchor: TestLevels.PivotOnWorld);
        }

        [Test]
        public void 회전축이_없으면_막대는_그대로_떨어진다()
        {
            // 대조군. 없으면 위 검사가 조인트 없이도 통과한다.
            var loose = TestLevels.PivotSolution().Clone();
            loose.Pivots.Clear();

            using (var world = WorldBuilder.Build(TestLevels.PivotSwing(), loose, 0))
            {
                for (int i = 0; i < Steps; i++) world.Step();

                float held = Vector2.Distance(FindStroke(world, 1).position, TestLevels.PivotOnFixedLine);
                float free = Vector2.Distance(FindStroke(world, 2).position, TestLevels.PivotOnWorld);

                Assert.Greater(held, 1f, "축을 지웠는데도 막대 1 이 제자리에 있다.");
                Assert.Greater(free, 1f, "축을 지웠는데도 막대 2 가 제자리에 있다.");
            }
        }

        [Test]
        public void 회전축은_해시_궤적을_바꾼다()
        {
            // 조인트 생성이 조용히 실패하면 두 궤적이 같아진다.
            var jointed = TestLevels.PivotSolution();
            var loose = jointed.Clone();
            loose.Pivots.Clear();

            var withJoints = new System.Collections.Generic.List<ulong>();
            var withoutJoints = new System.Collections.Generic.List<ulong>();

            SimRunner.RunTraced(TestLevels.PivotSwing(), jointed, 7, withJoints, Steps);
            SimRunner.RunTraced(TestLevels.PivotSwing(), loose, 7, withoutJoints, Steps);

            CollectionAssert.AreNotEqual(withJoints, withoutJoints,
                "회전축이 있든 없든 궤적이 같다 — 조인트가 만들어지지 않았다.");
        }

        [Test]
        public void 회전축은_바디를_늘리지_않는다()
        {
            // 바디가 늘면 해시 순서가 밀려 기준선이 어긋난다.
            var jointed = TestLevels.PivotSolution();
            var loose = jointed.Clone();
            loose.Pivots.Clear();

            using (var withJoints = WorldBuilder.Build(TestLevels.PivotSwing(), jointed, 0))
            using (var without = WorldBuilder.Build(TestLevels.PivotSwing(), loose, 0))
            {
                Assert.AreEqual(without.Bodies.Count, withJoints.Bodies.Count);
            }
        }

        /// <summary>
        /// 축에서 막대 중심까지의 거리를 본다.
        /// 조인트가 붙어 있으면 돌기만 해서
        /// 이 거리가 변하지 않는다.
        /// </summary>
        static void AssertHeldByAnchor(int strokeIndex, Vector2 anchor)
        {
            using (var world = WorldBuilder.Build(TestLevels.PivotSwing(), TestLevels.PivotSolution(), 0))
            {
                var bar = FindStroke(world, strokeIndex);
                float initial = Vector2.Distance(bar.position, anchor);

                Assert.Greater(initial, 0.1f,
                    "축이 막대 중심과 겹쳐 있다 — 중력 토크가 없어 회전이 없다.");

                float maxDrift = 0f;
                for (int i = 0; i < Steps; i++)
                {
                    world.Step();
                    float drift = Mathf.Abs(Vector2.Distance(bar.position, anchor) - initial);
                    if (drift > maxDrift) maxDrift = drift;
                }

                Assert.Less(maxDrift, AnchorDistanceTolerance,
                    $"막대가 축에서 최대 {maxDrift:F3} 만큼 벗어났다 — 조인트가 못 붙잡고 있다.");
            }
        }

        /// <summary>인덱스가 아니라 이름으로 찾는다.</summary>
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
