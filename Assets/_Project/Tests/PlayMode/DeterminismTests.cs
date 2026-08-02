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
        public void 해시는_공_이외의_바디도_담는다()
        {
            // 위의 "아주 작은 차이도 잡아낸다"는 **공** 을 옮겨서 본다. 그것만으로는
            // 공만 해시하고 스트로크·장치 바디를 통째로 빠뜨린 구현도 전부 통과한다 —
            // 나머지 결정론 테스트는 전부 "같은가"만 보기 때문이다.
            //
            // 그래서 공이 닿지 않는 자유 물체만 미세하게 옮겨 해시에 나타나는지 본다.
            var baseline = TraceOfDistantBody(0f);
            var nudged = TraceOfDistantBody(0.0001f);

            // 길이가 다르면 CollectionAssert 가 내용과 무관하게 통과해 검사가 헐거워진다.
            // 그래서 조기 종료를 타지 않고 정확히 같은 스텝 수를 돌린다.
            Assert.AreEqual(baseline.Count, nudged.Count, "비교 구간이 달라졌다 — 이 테스트의 전제가 깨졌다.");

            CollectionAssert.AreNotEqual(baseline, nudged,
                "공에서 떨어진 자유 물체를 옮겼는데 해시 궤적이 완전히 같다 — 해시가 공만 담고 있다.");
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
            // 질량·관성 **값**의 정확성은 StrokeMassTests 가 본다. 여기서 보는 것은
            // 그 값들이 물리 엔진에 먹혀서 물체가 실제로 살아 움직이는가까지다.
            // 관성이 0 이면 각속도 계산이 0 으로 나뉘어 NaN 이 나고, NaN 은 해시를 타고 번진다.
            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), TestLevels.FreeBodySolution(), 0))
            {
                var freeBody = FindStroke(world, 0);
                float startY = freeBody.position.y;

                for (int i = 0; i < 30; i++) world.Step();

                Assert.IsFalse(float.IsNaN(freeBody.position.x), "자유 물체 위치가 NaN 이다 (질량/관성 0 의심).");
                Assert.IsFalse(float.IsNaN(freeBody.rotation), "자유 물체 회전이 NaN 이다 (관성 0 의심).");
                Assert.Less(freeBody.position.y, startY, "자유 물체가 중력을 받지 않는다.");
            }
        }

        [Test]
        public void 자유_물체가_있어도_같은_입력은_같은_결과다()
        {
            // 유저가 가장 흔히 그리는 것이고, 코어에서 가장 최근에 바뀐 코드
            // (선분별 다중 경로 PolygonCollider2D, 철사 모델 질량 특성)가 전부 여기를 지난다.
            //
            // **접촉이 있어야 의미가 있다.** 공중에 뜬 자유 물체는 접촉 해결 순서를 타지 않아
            // 결정론이 깨질 여지가 거의 없다. FreeBodySolution 은 공 바로 위에서 떨어져
            // 공과 부딪히고 지형에 얹힌다 — 회전축 케이스가 일부러 피한 상황이 여기 들어 있다.
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
            SimRunner.RunTraced(TestLevels.FlatWithJitteryBomb(), null, seed, trace, Steps);
            return trace;
        }

        /// <summary>
        /// 공에서 멀리 떨어진 자유 물체 하나를 <paramref name="offsetX"/> 만큼 옮겨 돌린 스텝별 해시.
        ///
        /// 조기 종료를 타지 않도록 <c>SimRunner</c> 대신 직접 <c>Step()</c> 을 돌린다 —
        /// 판정이 갈려 궤적 길이가 달라지면 내용 비교가 무의미해진다.
        /// </summary>
        static List<ulong> TraceOfDistantBody(float offsetX)
        {
            // FlatRest 의 공은 (0, 2). 막대는 x 3~4 에 두어 서로 닿지 않는다.
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

        /// <summary>솔루션의 n 번째 스트로크가 만든 바디. **인덱스 계산 대신 이름으로 찾는다** —
        /// 레벨에 지형이나 장치가 하나 늘면 인덱스 기반 조회는 엉뚱한 바디를 검사하면서 통과한다.</summary>
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
