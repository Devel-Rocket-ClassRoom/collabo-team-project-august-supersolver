using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 테스트용 최소 레벨.
    /// 하나의 Outcome 을 안정적으로 내는 것만
    /// 목표로 한다.
    /// </summary>
    public static class TestLevels
    {
        /// <summary>내리막을 굴러 목표에 닿는다 → Clear.</summary>
        public static LevelData RampToGoal()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(-4.5f, 3.3f),
                GoalPosition = new Vector2(4.5f, -0.5f),
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 3f), new Vector2(5f, -1f)),
                },
            };
        }

        /// <summary>
        /// 완만한 긴 내리막. 300스텝 뒤에도 굴러가서
        /// 조기 종료가 걸리지 않는다. 특정 스텝의
        /// 상태를 비교해야 하는 곳에 쓴다.
        /// </summary>
        public static LevelData LongRoll()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(-19f, 4.3f),
                GoalPosition = new Vector2(100f, 100f),
                KillY = -50f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-20f, 4f), new Vector2(20f, -4f)),
                },
            };
        }

        /// <summary>굴러가다 멈춘다. 목표는 못 닿는다 → Stalled.</summary>
        public static LevelData FlatRest()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(0f, 2f),
                GoalPosition = new Vector2(50f, 50f),
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 0f), new Vector2(5f, 0f)),
                },
            };
        }

        /// <summary>받칠 것이 없어 떨어진다 → Fail.</summary>
        public static LevelData FreeFall()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(0f, 0f),
                GoalPosition = new Vector2(50f, 50f),
                KillY = -5f,
                Terrain = new List<StaticSegment>(),
            };
        }

        /// <summary>
        /// 발판 사이가 끊겨 있다.
        /// 안 그리면 Fail, 다리를 그리면 Stalled.
        /// </summary>
        public static LevelData Gap()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(0f, 3f),
                GoalPosition = new Vector2(50f, 50f),
                KillY = -5f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 0f), new Vector2(-0.8f, 0f)),
                    new StaticSegment(new Vector2(0.8f, 0f), new Vector2(5f, 0f)),
                },
            };
        }

        /// 테스트가 이 값으로 전/후를 나눈다.
        public const int LateBombFireStep = 400;

        /// <summary>
        /// 흔들림 없는 늦은 폭탄.
        /// 공이 먼저 잠들어, 대기 장치가 Stalled 를
        /// 미루는지 볼 수 있다.
        /// </summary>
        public static LevelData FlatWithLateBomb()
        {
            var level = FlatRest();
            level.Devices.Add(new DeviceData
            {
                Type = DeviceType.Bomb,
                // 공의 낙하 경로를 피한다.
                // 바로 밑이면 공이 얹혀 불안정해진다.
                Position = new Vector2(1.2f, BombDevice.BodyRadius),
                Radius = 3f,
                Power = 5f,
                DelaySteps = LateBombFireStep,
                JitterSteps = 0,
            });
            return level;
        }

        /// <summary>
        /// 발동 스텝에 흔들림이 있는 폭탄.
        /// 시드가 결과에 반영되는지 볼 때 쓴다.
        /// </summary>
        public static LevelData FlatWithJitteryBomb()
        {
            var level = FlatRest();
            level.Devices.Add(new DeviceData
            {
                Type = DeviceType.Bomb,
                // 공의 낙하 경로를 피한다.
                Position = new Vector2(1.2f, BombDevice.BodyRadius),
                Radius = 3f,
                Power = 4f,
                DelaySteps = 20,
                JitterSteps = 120,
            });
            return level;
        }

        /// 흔들림이 없어 정확히 이 스텝에 터진다.
        public const int FragBombFireStep = 40;

        /// <summary>
        /// 좁은 구덩이 바닥에 공, 위에 파편 폭탄 → Fail.
        /// 벽으로 가둬야 파편 방향과 무관하게
        /// 결과가 같아진다.
        /// </summary>
        public static LevelData FragBombPit()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(0f, 0.6f),
                GoalPosition = new Vector2(50f, 50f),   // 못 닿는 곳
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-0.6f, 0f), new Vector2(0.6f, 0f)),    // 바닥
                    new StaticSegment(new Vector2(-0.6f, 0f), new Vector2(-0.6f, 2.4f)), // 왼쪽 벽
                    new StaticSegment(new Vector2(0.6f, 0f), new Vector2(0.6f, 2.4f)),   // 오른쪽 벽
                },
                Devices = new List<DeviceData>
                {
                    new DeviceData
                    {
                        Type = DeviceType.FragBomb,
                        Position = new Vector2(0f, 1.4f),
                        Radius = 0f,          // 밀어내기를 하지 않는다
                        Power = 6f,           // 파편 초기 속도
                        DelaySteps = FragBombFireStep,
                        JitterSteps = 0,      // 판정 시점을 읽기 쉽게 고정
                    },
                },
            };
        }

        /// <summary>
        /// 파편이 공에서 멀다 → Stalled.
        /// 닿지 않으면 실패로 안 잡는지, 수명이
        /// 실제로 걷히는지 함께 본다.
        /// </summary>
        public static LevelData FragBombFarAway()
        {
            var level = FlatRest();
            level.Devices.Add(new DeviceData
            {
                Type = DeviceType.FragBomb,
                // 파편 고리가 지면 아래로 안 가게 띄운다.
                Position = new Vector2(4.2f, 0.6f),
                Radius = 0f,
                // 1.5m/s × 수명 1초 = 1.5m. 공까지는 4.2m.
                Power = 1.5f,
                DelaySteps = FragBombFireStep,
                JitterSteps = 0,
            });
            return level;
        }

        /// <summary>
        /// FragBombPit 을 스테이지로 감싼 것.
        /// 파편이 있는 레벨은 이 형태로 쓴다.
        /// </summary>
        public static StageData FragBombPitStage(int seed = 7)
        {
            return new StageData
            {
                StageId = "S_FragPit",
                Seed = seed,
                Level = FragBombPit(),
            };
        }

        /// <summary>
        /// 회전축 검증용. 막대가 흔들리는 한
        /// Stalled 가 나지 않아, 조인트가 개입한
        /// 구간을 길게 확보할 수 있다.
        /// </summary>
        public static LevelData PivotSwing()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(-6f, 1f),
                GoalPosition = new Vector2(50f, 50f),
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-8f, 0f), new Vector2(8f, 0f)),
                },
            };
        }

        /// 고정선에 막대를 매단 축. 스트로크 간 연결.
        public static readonly Vector2 PivotOnFixedLine = new Vector2(0.4f, 2.95f);

        /// 막대를 허공에 못박은 축. 월드 고정.
        public static readonly Vector2 PivotOnWorld = new Vector2(4.4f, 2.9f);

        /// <summary>
        /// 회전축 두 형태를 모두 담는다.
        /// 축을 막대 중심에서 벗어나게 둬야
        /// 조인트가 개입한 것이 결과로 드러난다.
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

            // 1 — 기둥에 매달릴 막대. 중심이 축에서 벗어나 있다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1.4f, 2.9f),
                new Vector2(1.0f, 2.9f),
            }));

            // 2 — 월드에 못박을 막대.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(4.0f, 2.9f),
                new Vector2(6.4f, 2.9f),
            }));

            solution.Pivots.Add(new PivotJoint(0, 1, PivotOnFixedLine));
            solution.Pivots.Add(new PivotJoint(2, PivotJoint.WorldIndex, PivotOnWorld));

            return solution;
        }

        /// <summary>Gap 의 틈을 잇는 다리.</summary>
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

        /// <summary>공중에 뜬 자유 물체. 떨어지는지 확인용.</summary>
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
