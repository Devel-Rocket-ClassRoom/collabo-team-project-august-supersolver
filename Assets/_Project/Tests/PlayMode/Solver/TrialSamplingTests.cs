using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 시행 러너에 궤적 버퍼를 물린 경로 검증.
    /// 수집은 선택이며 시뮬을 건드리지 않는다 —
    /// 켜고 끈 결과가 갈리면 궤적을 믿을 수 없다.
    /// </summary>
    public class TrialSamplingTests
    {
        const int Steps = 300;

        const int Interval = 10;

        [Test]
        public void 궤적을_켜도_시뮬_결과가_같다()
        {
            var level = Ground();
            var vector = new PrimitiveCodec(level).Encode(Placement());
            var trial = new PrimitiveTrial(level, 0);

            var off = trial.Run(vector, Steps);
            var on = trial.RunSampled(vector, NewBuffer(), Steps);

            AssertSameResult(off.Sim, on.Sim);
            Assert.AreEqual(off.Reject, on.Reject);
        }

        [Test]
        public void 같은_입력이면_궤적이_값까지_같다()
        {
            var level = Ground();
            var vector = new PrimitiveCodec(level).Encode(Placement());
            var trial = new PrimitiveTrial(level, 0);

            var first = NewBuffer();
            var second = NewBuffer();

            var a = trial.RunSampled(vector, first, Steps);
            var b = trial.RunSampled(vector, second, Steps);

            AssertSameResult(a.Sim, b.Sim);
            Assert.Greater(first.Count, 0, "궤적이 비어 있으면 비교가 무의미하다");
            Assert.AreEqual(first.Count, second.Count);

            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Step, second[i].Step, $"{i}번 스냅샷");
                Assert.AreEqual(first[i].Position, second[i].Position, $"{i}번 스냅샷");
                Assert.AreEqual(first[i].Velocity, second[i].Velocity, $"{i}번 스냅샷");
            }
        }

        [Test]
        public void 조기_종료한_시행의_마지막_스냅샷이_종료_스텝_이내다()
        {
            var buffer = NewBuffer();

            // 빈 벡터 — 아무것도 놓지 않아도 공이 목표로 떨어진다.
            var trial = new PrimitiveTrial(FallIntoGoal(), 0)
                .RunSampled(new float[0], buffer, Steps);

            Assert.IsTrue(trial.Cleared, "조기 종료를 보려면 클리어해야 한다");
            Assert.Less(trial.Sim.EndStep, Steps, "상한까지 돌면 조기 종료가 아니다");
            Assert.Greater(buffer.Count, 0);
            Assert.LessOrEqual(buffer[buffer.Count - 1].Step, trial.Sim.EndStep,
                "종료 뒤 스텝이 궤적에 남았다");
        }

        /// <summary>
        /// 시행에서 시뮬까지 세 번 넘어가는 인자라
        /// 중간에 흘리면 조용히 레벨 시작점으로 돈다.
        /// </summary>
        [Test]
        public void 시작_상태가_시뮬까지_전달된다()
        {
            var level = Ground();
            var start = new BallState(new Vector2(-2f, 6f), new Vector2(2f, 0f));
            var trial = new PrimitiveTrial(level, 0);

            var given = NewBuffer();
            var defaulted = NewBuffer();

            trial.RunSampled(new float[0], given, Steps, start);
            trial.RunSampled(new float[0], defaulted, Steps);

            Assert.Greater(given.Count, 0);
            Assert.Less(Vector2.Distance(given[0].Position, start.Position), 1f,
                "첫 스냅샷이 준 시작 위치 근처가 아니다");
            Assert.AreNotEqual(defaulted[0].Position, given[0].Position,
                "시작 상태가 무시되고 레벨 시작점으로 돌았다");
        }

        [Test]
        public void 거부된_시행은_버퍼를_비운_채_돌려준다()
        {
            var level = Ground();
            var trial = new PrimitiveTrial(level, 0);
            var buffer = NewBuffer();

            trial.RunSampled(new PrimitiveCodec(level).Encode(Placement()), buffer, Steps);
            Assert.Greater(buffer.Count, 0);

            var rejected = trial.RunSampled(new float[PrimitiveCodec.Dimensions + 1], buffer, Steps);

            Assert.AreEqual(PlacementReject.BadVector, rejected.Reject);
            Assert.AreEqual(0, buffer.Count, "지난 시행의 궤적이 남았다");
        }

        static TrajectoryBuffer NewBuffer() => new TrajectoryBuffer(Interval, Steps);

        static void AssertSameResult(in SimResult expected, in SimResult actual)
        {
            Assert.AreEqual(expected.Outcome, actual.Outcome);
            Assert.AreEqual(expected.EndStep, actual.EndStep);
            Assert.AreEqual(expected.Stars, actual.Stars);
            Assert.AreEqual(expected.InkUsed, actual.InkUsed);
            Assert.AreEqual(expected.MinGoalDist, actual.MinGoalDist);
        }

        /// 공이 굴러가는 길목에 발판 둘. PrimitiveTrialTests 와 같다.
        static Primitive[] Placement() => new[]
        {
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(-4f, 2f), -0.3f, 2f),
            new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(2f, 4f), 0.2f, 1f),
        };

        static LevelData Ground()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(-5f, 5f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(6f, 0.5f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-14f, 0f), new Vector2(14f, 0f)),
                },
            };
        }

        /// <summary>
        /// 아무것도 놓지 않아도 공이 목표로 떨어지는 판.
        /// 자유낙하 1초 남짓이라 상한 훨씬 전에 클리어한다.
        /// </summary>
        static LevelData FallIntoGoal()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(0f, 5f),
                BallRadius = 0.25f,
                GoalPosition = Vector2.zero,
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>(),
            };
        }
    }
}
