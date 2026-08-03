using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 코어 완성의 정의.
    /// 초록불이 아니면 솔버의 어떤 판정도
    /// 근거가 없다.
    /// </summary>
    public class DeterminismTests
    {
        const int Steps = 900;   // 15초. 전 구간 비교라 상한까지 갈 필요 없다.

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
            // 버린 월드가 다음 월드를 오염시키면
            // 솔버는 시도할수록 결과가 달라진다.
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
            // 없으면 항상 같은 값을 뱉는 해시도 통과한다.
            var level = TestLevels.RampToGoal();

            var baseline = new List<ulong>();
            SimRunner.RunTraced(level, null, 1, baseline, 120);

            var nudged = TestLevels.RampToGoal();
            nudged.BallStart += new Vector2(0.0001f, 0f);

            var perturbed = new List<ulong>();
            SimRunner.RunTraced(nudged, null, 1, perturbed, 120);

            CollectionAssert.AreNotEqual(baseline, perturbed,
                "공 시작 위치를 0.0001 옮겼는데 해시 궤적이 완전히 같다.");
        }

        [Test]
        public void 해시는_공_이외의_바디도_담는다()
        {
            // 위 검사는 공만 옮긴다. 그것만으로는
            // 공만 해시하는 구현도 통과한다.
            var baseline = TraceOfDistantBody(0f);
            var nudged = TraceOfDistantBody(0.0001f);

            // 길이가 다르면 내용과 무관하게 통과해 버린다.
            Assert.AreEqual(baseline.Count, nudged.Count, "비교 구간이 달라졌다.");

            CollectionAssert.AreNotEqual(baseline, nudged,
                "멀리 있는 자유 물체를 옮겼는데 궤적이 같다 — 해시가 공만 담고 있다.");
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
        public void 자유물체가_NaN_없이_중력을_받는다()
        {
            // 값의 정확성은 StrokeMassTests 가 본다.
            // 여기서는 그 값이 엔진에 먹히는지만 본다.
            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), TestLevels.FreeBodySolution(), 0))
            {
                var freeBody = FindStroke(world, 0);
                float startY = freeBody.position.y;

                for (int i = 0; i < 30; i++) world.Step();

                Assert.IsFalse(float.IsNaN(freeBody.position.x), "위치가 NaN 이다 (질량/관성 0 의심).");
                Assert.IsFalse(float.IsNaN(freeBody.rotation), "회전이 NaN 이다 (관성 0 의심).");
                Assert.Less(freeBody.position.y, startY, "자유 물체가 중력을 받지 않는다.");
            }
        }

        [Test]
        public void 자유_물체가_있어도_같은_입력은_같은_결과다()
        {
            // 접촉이 있어야 의미가 있다. 공중에 뜬
            // 물체는 접촉 해결 순서를 타지 않는다.
            var level = TestLevels.FlatRest();

            var traceA = new List<ulong>();
            var traceB = new List<ulong>();

            SimRunner.RunTraced(level, TestLevels.FreeBodySolution(), 3, traceA, Steps);
            SimRunner.RunTraced(level, TestLevels.FreeBodySolution(), 3, traceB, Steps);

            AssertTracesMatch(traceA, traceB);
        }

        [Test]
        public void 회전축이_있어도_같은_입력은_같은_결과다()
        {
            // 조인트가 가장 깨지기 쉽다. 아일랜드 구성이
            // 제약 해결 순서를 바꾸고, 그게 합산 순서다.
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
            // 장치가 없으면 rng 와 로직 루프를 지나지 않는다.
            AssertTracesMatch(TraceWithBomb(42), TraceWithBomb(42));
        }

        [Test]
        public void 시드가_다르면_장치_동작이_갈린다()
        {
            // 없으면 시드를 무시하는 코어도
            // 위 반복 실행 검사를 완벽히 통과한다.
            int[] seeds = { 1, 2, 3, 4 };
            var baseline = TraceWithBomb(seeds[0]);

            for (int i = 1; i < seeds.Length; i++)
            {
                if (!TracesEqual(baseline, TraceWithBomb(seeds[i]))) return;   // 하나라도 갈리면 통과
            }

            Assert.Fail($"시드 {string.Join(", ", seeds)} 가 전부 같은 궤적을 냈다 — " +
                        "시드가 시뮬 결과에 반영되지 않는다.");
        }

        /// <summary>흔들림 있는 폭탄을 얹고 돌린 해시.</summary>
        static List<ulong> TraceWithBomb(int seed)
        {
            var trace = new List<ulong>();
            SimRunner.RunTraced(TestLevels.FlatWithJitteryBomb(), null, seed, trace, Steps);
            return trace;
        }

        /// <summary>
        /// 공에서 먼 자유 물체를 옮겨 돌린 해시.
        /// 조기 종료로 길이가 갈리면 비교가 무의미해
        /// SimRunner 대신 직접 Step 한다.
        /// </summary>
        static List<ulong> TraceOfDistantBody(float offsetX)
        {
            // 공은 (0, 2). 막대는 x 3~4 라 닿지 않는다.
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(3f + offsetX, 3f),
                new Vector2(4f + offsetX, 3f),
            }));

            var trace = new List<ulong>();

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), solution, 0))
            {
                for (int i = 0; i < 200; i++)
                {
                    world.Step();
                    trace.Add(WorldHasher.Hash(world));
                }
            }

            return trace;
        }

        /// <summary>
        /// 인덱스가 아니라 이름으로 찾는다.
        /// 지형이 하나 늘면 인덱스 조회는
        /// 엉뚱한 바디를 검사하며 통과한다.
        /// </summary>
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
