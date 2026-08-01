using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// **코어 완성의 정의.** 이 테스트가 초록불이 아니면 솔버의 어떤 판정도 근거가 없다.
    /// 이후에도 상시 회귀 감시로 돌려 팀원의 결정론 오염을 즉시 검출한다.
    /// </summary>
    public class DeterminismTests
    {
        const int Steps = 900;   // 15초. 전 구간 비교라 상한까지 갈 필요는 없다.

        [Test]
        public void 물리설정이_결정론을_깨뜨리지_않는다()
        {
            var violations = DeterminismGuard.FindViolations();
            Assert.IsEmpty(violations, string.Join("\n", violations));

            Debug.Log("Physics2D fingerprint: " + DeterminismGuard.SettingsFingerprint());
        }

        [Test]
        public void 같은_입력_두_번_실행하면_스텝별_해시가_전부_같다()
        {
            var level = TestLevels.RampToGoal();

            var traceA = new List<ulong>();
            var traceB = new List<ulong>();

            var resultA = SimRunner.RunTraced(level, TestLevels.BridgeSolution(), 42, traceA, Steps);
            var resultB = SimRunner.RunTraced(level, TestLevels.BridgeSolution(), 42, traceB, Steps);

            AssertTracesMatch(traceA, traceB);
            Assert.AreEqual(resultA.Outcome, resultB.Outcome);
            Assert.AreEqual(resultA.EndStep, resultB.EndStep);
            Assert.AreEqual(resultA.MinGoalDist, resultB.MinGoalDist);
        }

        [Test]
        public void 다른_월드를_사이에_끼워도_재구축_결과가_같다()
        {
            // 월드 하나를 만들었다 버린 것이 다음 월드에 영향을 주면(정적 캐시·풀·카운터 오염)
            // 솔버는 시도할수록 결과가 달라진다. 재시도·되돌리기가 성립하려면 이게 보장돼야 한다.
            var level = TestLevels.RampToGoal();

            var first = new List<ulong>();
            SimRunner.RunTraced(level, null, 7, first, Steps);

            var noise = new List<ulong>();
            SimRunner.RunTraced(TestLevels.Gap(), TestLevels.BridgeSolution(), 99, noise, 120);

            var second = new List<ulong>();
            SimRunner.RunTraced(level, null, 7, second, Steps);

            AssertTracesMatch(first, second);
        }

        [Test]
        public void 해시는_아주_작은_차이도_잡아낸다()
        {
            // 이 테스트가 없으면 "항상 같은 값을 뱉는 해시"도 위 테스트들을 통과해 버린다.
            var level = TestLevels.RampToGoal();

            var baseline = new List<ulong>();
            SimRunner.RunTraced(level, null, 1, baseline, 120);

            var nudged = TestLevels.RampToGoal();
            nudged.BallStart += new Vector2(0.0001f, 0f);

            var perturbed = new List<ulong>();
            SimRunner.RunTraced(nudged, null, 1, perturbed, 120);

            CollectionAssert.AreNotEqual(baseline, perturbed,
                "공 시작 위치를 0.0001 옮겼는데 해시 궤적이 완전히 같다 — 해시가 상태를 반영하지 못한다.");
        }

        [Test]
        public void 스트로크가_실제로_물리에_투입된다()
        {
            var level = TestLevels.Gap();

            var withoutBridge = SimRunner.Run(level, null, 0, 600);
            var withBridge = SimRunner.Run(level, TestLevels.BridgeSolution(), 0, 600);

            Assert.AreEqual(SimOutcome.Fail, withoutBridge.Outcome,
                "다리가 없으면 틈으로 떨어져야 한다.");
            Assert.AreEqual(SimOutcome.Stalled, withBridge.Outcome,
                "다리를 그렸으면 그 위에 얹혀 멈춰야 한다.");
        }

        [Test]
        public void 자유물체는_질량과_관성이_정상이라_떨어진다()
        {
            var level = TestLevels.FlatRest();

            using (var world = WorldBuilder.Build(level, TestLevels.FreeBodySolution(), 0))
            {
                var freeBody = world.Bodies[world.Bodies.Count - 1];
                float startY = freeBody.position.y;

                for (int i = 0; i < 30; i++) world.Step();

                Assert.IsFalse(float.IsNaN(freeBody.position.x), "자유 물체 위치가 NaN 이다 (질량/관성 0 의심).");
                Assert.Less(freeBody.position.y, startY, "자유 물체가 중력을 받지 않는다.");
            }
        }

        [Test]
        public void 회전축이_있어도_같은_입력은_같은_결과다()
        {
            // 조인트는 결정론이 가장 깨지기 쉬운 지점이다. Box2D 는 조인트가 만드는 아일랜드 구성에
            // 따라 제약 해결 순서가 달라지고, 그 순서가 곧 부동소수점 합산 순서다.
            // 이 케이스 없이 통과한 결정론은 유저가 회전축을 그리는 순간 무너질 수 있다.
            var level = TestLevels.PivotSwing();

            var traceA = new List<ulong>();
            var traceB = new List<ulong>();

            SimRunner.RunTraced(level, TestLevels.PivotSolution(), 7, traceA, Steps);
            SimRunner.RunTraced(level, TestLevels.PivotSolution(), 7, traceB, Steps);

            AssertTracesMatch(traceA, traceB);
        }

        [Test]
        public void 장치가_있어도_같은_입력은_같은_결과다()
        {
            // 장치가 도는 순간 rng 와 로직 루프가 결과에 개입하기 시작한다.
            // 장치 없는 레벨의 결정론은 이 경로를 전혀 지나가지 않는다.
            AssertTracesMatch(TraceWithBomb(42), TraceWithBomb(42));
        }

        [Test]
        public void 시드가_다르면_장치_동작이_갈린다()
        {
            // 시드가 실제로 시뮬에 반영되는지 본다. 이 검사가 없으면 "시드를 무시하는 코어"도
            // 위의 반복 실행 테스트를 완벽하게 통과한다 — 항상 같은 결과를 내기 때문이다.
            //
            // 장치가 없는 레벨에서는 이 테스트가 성립하지 않는다. 코어에서 rng 가 흘러가는 곳은
            // IStepLogic.Tick 하나뿐이라, 장치가 없으면 시드는 결과에 닿지 못한다.
            int[] seeds = { 1, 2, 3, 4 };
            var baseline = TraceWithBomb(seeds[0]);

            for (int i = 1; i < seeds.Length; i++)
            {
                if (!TracesEqual(baseline, TraceWithBomb(seeds[i]))) return;   // 하나라도 갈리면 시드는 살아 있다
            }

            Assert.Fail($"시드 {string.Join(", ", seeds)} 가 전부 같은 궤적을 냈다 — " +
                        "시드가 시뮬 결과에 반영되지 않는다 (폭탄이 rng 를 쓰지 않거나 등록되지 않았다).");
        }

        /// <summary>
        /// 발동 스텝에 rng 흔들림이 있는 폭탄을 얹고 돌린 스텝별 해시.
        /// 흔들림이 있으므로 시드가 다르면 궤적이 갈라져야 한다.
        /// </summary>
        static List<ulong> TraceWithBomb(int seed)
        {
            var trace = new List<ulong>();
            var bomb = new TestBomb(delaySteps: 20, jitterSteps: 120);

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), null, seed, new IStepLogic[] { bomb }))
            {
                bomb.Target = world.Ball;

                for (int i = 0; i < Steps && !world.IsTerminal; i++)
                {
                    world.Step();
                    trace.Add(WorldHasher.Hash(world));
                }
            }

            return trace;
        }

        static bool TracesEqual(List<ulong> a, List<ulong> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        static void AssertTracesMatch(List<ulong> a, List<ulong> b)
        {
            int common = Mathf.Min(a.Count, b.Count);
            for (int i = 0; i < common; i++)
            {
                if (a[i] != b[i])
                    Assert.Fail($"스텝 {i} 에서 갈라졌다. A=0x{a[i]:X16} B=0x{b[i]:X16} " +
                                $"(그 앞 {i} 스텝은 완전히 일치)");
            }

            Assert.AreEqual(a.Count, b.Count, "스텝 수가 다르다 — 조기 종료 판정이 갈렸다.");
            Assert.Greater(a.Count, 0, "시뮬이 한 스텝도 진행되지 않았다.");
        }
    }
}
