using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 벡터 → SimResult 단일 시행 경로 검증.
    /// 유효한 벡터는 시뮬까지 가고,
    /// 거부는 예외가 아니라 사유로 돌아온다.
    /// </summary>
    public class PrimitiveTrialTests
    {
        const int Steps = 300;

        [Test]
        public void 유효한_벡터는_시뮬까지_간다()
        {
            var level = Ground();
            var vector = new PrimitiveCodec(level).Encode(Placement());

            var trial = new PrimitiveTrial(level, 0).Run(vector, Steps);

            Assert.AreEqual(PlacementReject.None, trial.Reject);
            Assert.IsFalse(trial.Rejected);
            Assert.Greater(trial.Sim.EndStep, 0);
            Assert.IsFalse(float.IsInfinity(trial.Sim.MinGoalDist));
        }

        [Test]
        public void 시행은_러너와_같은_결과를_낸다()
        {
            var level = Ground();
            var codec = new PrimitiveCodec(level);
            var vector = codec.Encode(Placement());

            var trial = new PrimitiveTrial(level, 0).Run(vector, Steps);
            var direct = PrimitiveRunner.Run(level, codec.Decode(vector), 0, Steps);

            Assert.AreEqual(direct.Outcome, trial.Sim.Outcome);
            Assert.AreEqual(direct.EndStep, trial.Sim.EndStep);
            Assert.AreEqual(direct.Stars, trial.Sim.Stars);
            Assert.AreEqual(direct.InkUsed, trial.Sim.InkUsed);
            Assert.AreEqual(direct.MinGoalDist, trial.Sim.MinGoalDist);
        }

        [Test]
        public void 영역_밖_벡터는_시뮬_없이_거부된다()
        {
            var trial = new PrimitiveTrial(Ground(), 0).Run(OutOfAreaVector(), Steps);

            Assert.AreEqual(PlacementReject.OutOfArea, trial.Reject);
            Assert.AreEqual(0, trial.Sim.EndStep, "시뮬이 돌지 않았다");
            Assert.IsTrue(float.IsInfinity(trial.Sim.MinGoalDist));
        }

        [Test]
        public void 길이가_배수가_아니면_거부된다()
        {
            var vector = new float[PrimitiveCodec.Dimensions + 1];

            var trial = new PrimitiveTrial(Ground(), 0).Run(vector, Steps);

            Assert.AreEqual(PlacementReject.BadVector, trial.Reject);
        }

        /// 영역 왼쪽 끝에 아주 크게 놓은 Line 하나.
        /// 중심은 영역 안이지만 외접원이 밖으로 나간다.
        static float[] OutOfAreaVector() => new[]
        {
            0f,     // CenterX = 영역 왼쪽 끝
            0.5f,   // CenterY
            0.5f,   // AngleCos
            0.5f,   // AngleSin
            0.9f,   // Size = 잉크 상한 직전
            0.1f,   // Shape = Line
            0.1f,   // Body = FixedLine
            0.1f,   // Pivot = None
        };

        /// 공이 굴러가는 길목에 발판 둘을 놓는다.
        static Primitive[] Placement() => new[]
        {
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(-4f, 2f), -0.3f, 2f),
            new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(2f, 4f), 0.2f, 1f),
        };

        /// 평평한 바닥 하나. PrimitiveRunnerTests 와 같은 판.
        static LevelData Ground()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(-5f, 5f),
                GoalPosition = new Vector2(6f, 0.5f),
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-14f, 0f), new Vector2(14f, 0f)),
                },
            };
        }
    }
}
