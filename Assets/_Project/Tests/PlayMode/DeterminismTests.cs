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
