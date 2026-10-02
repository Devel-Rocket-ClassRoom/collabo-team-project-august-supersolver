using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 공을 지정한 위치·속도에서 출발시키는 경로 검증.
    /// 기존 경로와 갈리면 지금까지의 모든 판정이 무효가 되고,
    /// 결정론이 깨지면 맵을 두 번 만들 때 값이 달라진다.
    /// </summary>
    public class ArbitraryStartTests
    {
        const int Steps = 300;

        /// 결정론 반복 횟수. 궤적을 값까지 비교한다.
        const int Repeats = 100;

        [Test]
        public void 시작_상태를_레벨_시작점으로_주면_기존_경로와_해시가_같다()
        {
            var level = TestLevels.RampToGoal();

            var without = new List<ulong>();
            var with = new List<ulong>();

            SimRunner.RunTraced(level, TestLevels.BridgeSolution(), 42, without, Steps);
            SimRunner.RunTraced(level, TestLevels.BridgeSolution(), 42, with, Steps,
                new BallState(level.BallStart, Vector2.zero));

            AssertTracesMatch(without, with);
        }

        [Test]
        public void 시작_상태가_공에_그대로_들어간다()
        {
            var level = TestLevels.FlatRest();
            var start = new BallState(new Vector2(-3f, 3f), new Vector2(4f, -1f));

            // 한 스텝도 돌리기 전을 본다.
            // 돌린 뒤면 중력이 섞여 넣은 값과 달라진다.
            using (var world = WorldBuilder.Build(level, null, 0, start))
            {
                Assert.AreEqual(start.Position.x, world.Ball.position.x, 1e-5f, "위치 x");
                Assert.AreEqual(start.Position.y, world.Ball.position.y, 1e-5f, "위치 y");
                Assert.AreEqual(start.Velocity.x, world.Ball.linearVelocity.x, 1e-5f, "속도 x");
                Assert.AreEqual(start.Velocity.y, world.Ball.linearVelocity.y, 1e-5f, "속도 y");
            }
        }

        [Test]
        public void 같은_시작_상태로_백_번_돌리면_궤적이_값까지_같다()
        {
            var level = TestLevels.LongRoll();
            var start = new BallState(new Vector2(0f, 2f), new Vector2(3f, -1f));

            var baseline = Trace(level, start);

            for (int i = 1; i < Repeats; i++)
                AssertTracesMatch(baseline, Trace(level, start), $"{i} 회차");
        }

        [Test]
        public void 시작_속도가_다르면_궤적이_갈라진다()
        {
            // 없으면 속도를 버리는 구현도 위 검사를 전부 통과한다.
            var level = TestLevels.LongRoll();
            var position = new Vector2(0f, 2f);

            var still = Trace(level, new BallState(position, Vector2.zero));
            var moving = Trace(level, new BallState(position, new Vector2(6f, 0f)));

            CollectionAssert.AreNotEqual(still, moving,
                "시작 속도를 바꿨는데 해시 궤적이 완전히 같다.");
        }

        static List<ulong> Trace(LevelData level, BallState start)
        {
            var trace = new List<ulong>();
            SimRunner.RunTraced(level, null, 7, trace, Steps, start);
            return trace;
        }

        static void AssertTracesMatch(List<ulong> a, List<ulong> b, string label = "")
        {
            Assert.Greater(a.Count, 0, "시뮬이 한 스텝도 진행되지 않았다.");
            Assert.AreEqual(a.Count, b.Count, $"{label} 스텝 수가 다르다 — 조기 종료 판정이 갈렸다.");

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    Assert.Fail($"{label} 스텝 {i} 에서 갈라졌다. " +
                                $"A=0x{a[i]:X16} B=0x{b[i]:X16} (그 앞 {i} 스텝은 완전히 일치)");
            }
        }
    }
}
