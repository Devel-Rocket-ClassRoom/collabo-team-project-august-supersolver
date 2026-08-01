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

        /// <summary>
        /// 회전축 검증용 레벨. 평지에 공 하나가 놓여 곧 잠들고, 공중의 막대들이 회전축에 매달려 흔들린다.
        ///
        /// 목표는 닿을 수 없는 곳이고 KillY 도 멀다. 막대가 흔들리는 한 전 바디 sleep 이 성립하지 않아
        /// Stalled 로 조기 종료되지 않는다 — **조인트가 개입한 구간을 길게 확보해야**
        /// 스텝별 해시 비교가 의미를 가진다. 몇 스텝 만에 끝나면 비교할 것이 남지 않는다.
        /// </summary>
        public static LevelData PivotSwing()
        {
            return new LevelData
            {
                Id = "T_Pivot",
                InkLimit = 20f,
                BallStart = new Vector2(-6f, 1f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-8f, 0f), new Vector2(8f, 0f)),
                },
            };
        }

        /// <see cref="PivotSolution"/> 에서 정적 고정선에 막대를 매단 축. 스트로크 간 연결 형태.
        public static readonly Vector2 PivotOnFixedLine = new Vector2(0.4f, 2.95f);

        /// <see cref="PivotSolution"/> 에서 막대를 허공에 못박은 축. 월드 고정 형태.
        public static readonly Vector2 PivotOnWorld = new Vector2(4.4f, 2.9f);

        /// <summary>
        /// 회전축 **두 형태를 모두** 담은 솔루션. <c>WorldBuilder.CreatePivot</c> 의 분기를 전부 지나간다.
        ///
        /// - 스트로크 0(고정선) ↔ 1(자유 막대): 한쪽이 정적이므로 <c>PickHost</c> 가 정적 쪽을 버리고 동적 쪽을 고른다
        /// - 스트로크 2(자유 막대) ↔ 월드: <c>Resolve</c> 가 <c>PivotJoint.WorldIndex</c> 를 null 로 풀고
        ///   조인트의 상대 바디가 없는 경로로 간다
        ///
        /// 축은 둘 다 막대 중심에서 벗어난 곳에 둔다. 중심에 두면 중력 토크가 0 이라
        /// 막대가 그대로 떠 있고, 조인트가 물리에 개입했는지 결과로 드러나지 않는다.
        ///
        /// 두 막대의 회전 반경은 서로 겹치지 않게 떨어뜨렸다. 부딪히면 궤적이 달라진 원인이
        /// 조인트인지 충돌인지 구분할 수 없게 된다. 고정선은 축보다 위에 세워 둔다 —
        /// 수평에서 놓인 진자는 축 높이 위로 올라가지 못하므로 막대가 기둥에 닿지 않는다.
        /// </summary>
        public static Solution PivotSolution()
        {
            var solution = new Solution();

            // 0 — 정적 기둥. 축 위쪽으로 세운다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(0.4f, 3.05f),
                new Vector2(0.4f, 3.9f),
            }));

            // 1 — 기둥에 매달릴 막대. 중심은 (-0.2, 2.9) 라 축(0.4)에서 벗어나 있다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1.4f, 2.9f),
                new Vector2(1.0f, 2.9f),
            }));

            // 2 — 월드에 못박을 막대. 중심은 (5.2, 2.9) 라 축(4.4)에서 벗어나 있다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(4.0f, 2.9f),
                new Vector2(6.4f, 2.9f),
            }));

            solution.Pivots.Add(new PivotJoint(0, 1, PivotOnFixedLine));
            solution.Pivots.Add(new PivotJoint(2, PivotJoint.WorldIndex, PivotOnWorld));

            return solution;
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
