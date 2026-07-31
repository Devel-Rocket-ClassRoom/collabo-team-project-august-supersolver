using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 테스트용 최소 레벨. 각 레벨은 **하나의 Outcome 을 안정적으로 만들어내는 것**만 목표로 한다.
    /// 실제 콘텐츠 레벨은 팀원 C 의 에디터에서 나온다.
    /// </summary>
    public static class TestLevels
    {
        /// <summary>내리막을 굴러 목표에 닿는다 → Clear.</summary>
        public static LevelData RampToGoal()
        {
            return new LevelData
            {
                Id = "T_Ramp",
                InkLimit = 20f,
                BallStart = new Vector2(-4.5f, 3.3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(4.5f, -0.5f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 3f), new Vector2(5f, -1f)),
                },
            };
        }

        /// <summary>
        /// 완만한 긴 내리막. 300스텝(5초) 뒤에도 여전히 굴러가는 중이라 조기 종료가 걸리지 않는다.
        /// 프레임 독립성 테스트처럼 "특정 스텝에서의 상태"를 비교해야 하는 곳에 쓴다 —
        /// 도중에 Clear/Stalled 가 나버리면 프레임레이트별로 도달 스텝이 달라져 비교 자체가 성립하지 않는다.
        /// </summary>
        public static LevelData LongRoll()
        {
            return new LevelData
            {
                Id = "T_LongRoll",
                InkLimit = 20f,
                BallStart = new Vector2(-19f, 4.3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(100f, 100f),
                GoalRadius = 0.5f,
                KillY = -50f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-20f, 4f), new Vector2(20f, -4f)),
                },
            };
        }

        /// <summary>평지에 떨어져 굴러가다 멈춘다. 목표는 닿을 수 없는 곳 → Stalled.</summary>
        public static LevelData FlatRest()
        {
            return new LevelData
            {
                Id = "T_Flat",
                InkLimit = 20f,
                BallStart = new Vector2(0f, 2f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 0f), new Vector2(5f, 0f)),
                },
            };
        }

        /// <summary>받칠 것이 없어 그대로 떨어진다 → Fail.</summary>
        public static LevelData FreeFall()
        {
            return new LevelData
            {
                Id = "T_Fall",
                InkLimit = 20f,
                BallStart = new Vector2(0f, 0f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -5f,
                Terrain = new List<StaticSegment>(),
            };
        }

        /// <summary>
        /// 발판 사이가 끊겨 있다. 아무것도 안 그리면 틈으로 떨어져 Fail,
        /// 다리를 그리면 그 위에 얹혀 Stalled. 스트로크가 실제로 물리에 투입되는지 보는 레벨.
        /// </summary>
        public static LevelData Gap()
        {
            return new LevelData
            {
                Id = "T_Gap",
                InkLimit = 20f,
                BallStart = new Vector2(0f, 3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -5f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 0f), new Vector2(-0.8f, 0f)),
                    new StaticSegment(new Vector2(0.8f, 0f), new Vector2(5f, 0f)),
                },
            };
        }

        /// <summary><see cref="Gap"/> 의 틈을 잇는 다리.</summary>
        public static Solution BridgeSolution()
        {
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(-1.2f, 0f),
                new Vector2(1.2f, 0f),
            }));
            return solution;
        }

        /// <summary>공중에 뜬 자유 물체 하나. 떨어지는지(질량·관성이 정상인지) 확인용.</summary>
        public static Solution FreeBodySolution()
        {
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-0.5f, 2.5f),
                new Vector2(0.5f, 2.5f),
            }));
            return solution;
        }
    }
}
