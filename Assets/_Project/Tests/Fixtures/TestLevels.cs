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

        /// <summary>굴러가다 멈춘다. 목표는 못 닿는다 → Stalled.</summary>
        public static LevelData FlatRest()
        {
            return new LevelData
            {
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

        /// <summary>받칠 것이 없어 떨어진다 → Fail.</summary>
        public static LevelData FreeFall()
        {
            return new LevelData
            {
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
        /// 유일한 퍼즐 레벨. 안 그리면 못 풀고, 그리면 풀린다.
        /// 다른 픽스처는 전부 판정 하나를 안정적으로 내는 것이 목적이라
        /// 그냥 굴려도 Clear 이거나 애초에 목표가 닿을 수 없는 곳에 있다.
        /// 솔버를 재려면 "풀 것이 있는" 판이 있어야 한다.
        /// </summary>
        public static LevelData GapPuzzle()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(-5.5f, 3.5f),
                BallRadius = 0.25f,

                // 공 바로 아래. 경사로가 공을 오른쪽으로 데려가므로
                // 가만 두면 목표에서 멀어지기만 한다.
                GoalPosition = new Vector2(-5.5f, 0.1f),
                GoalRadius = 0.5f,
                KillY = -5f,
                Terrain = new List<StaticSegment>
                {
                    // 경사로 — 공에 오른쪽 속도를 준다.
                    new StaticSegment(new Vector2(-6f, 3f), new Vector2(-2f, 0.5f)),

                    // 왼쪽 바닥. 여기서 끊기고 건너편은 없다.
                    new StaticSegment(new Vector2(-2f, 0.5f), new Vector2(-1f, 0.5f)),
                },
            };
        }

        /// <summary>
        /// 공을 왼쪽으로 흘려 목표 아래로 돌려보내는 풀이.
        /// 목표가 경사로 밑에 있어 오른쪽으로 굴러가면 영영 못 온다.
        /// 세 선이 각각 방향을 한 번씩 꺾는다.
        /// </summary>
        public static Solution GapPuzzleSolution()
        {
            var solution = new Solution();

            // 1) 시작 바로 아래. 왼쪽으로 기울여 경사로를 건너뛰게 한다.
            //    경사로 왼쪽 끝(x=-6, y=3)보다 위로 지나가야 하고,
            //    벽과의 틈이 공 지름(0.5)보다 넓어야 안 낀다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(-4.8f, 3.25f),
                new Vector2(-6.6f, 2.95f),
            }));

            // 2) 왼쪽 벽. 없으면 왼쪽으로 흐르며 떨어져 영역 밖으로 나간다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(-7.4f, 3.2f),
                new Vector2(-7.4f, 0.4f),
            }));

            // 3) 받침. 오른쪽으로 기울여 목표 높이로 데려간다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(-7.5f, 0.5f),
                new Vector2(-5.0f, -0.2f),
            }));

            return solution;
        }

        /// <summary>
        /// 기둥 셋을 지나 오른쪽 목표까지 가는 판. 목표가 공과 같은 높이다.
        /// 기둥마다 위로 넘는 길과 아래로 도는 길이 있어 갈래가 여럿이고,
        /// 기둥이 공중에 떠 있어 그리지 않으면 어느 길도 못 간다.
        /// 솔버가 서로 다른 경로를 몇 개나 찾는지 보려고 만든 판이다.
        /// </summary>
        public static LevelData PillarRun() => PillarRun(0f);

        /// 목표가 공보다 높다. 올려 보내야 풀린다.
        public static LevelData PillarRunUp() => PillarRun(3f);

        /// 목표가 공보다 낮다. 떨어뜨려야 풀린다.
        public static LevelData PillarRunDown() => PillarRun(-3f);

        /// <param name="goalY">목표 발판의 높이. 공 발판은 언제나 0 이다.</param>
        static LevelData PillarRun(float goalY)
        {
            return new LevelData
            {
                InkLimit = 40f,
                BallStart = new Vector2(-8f, 0.3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(8f, goalY + 0.5f),
                GoalRadius = 0.5f,
                KillY = -8f,
                Terrain = new List<StaticSegment>
                {
                    // 공이 놓인 발판. 없으면 시작하자마자 떨어진다.
                    new StaticSegment(new Vector2(-9.5f, 0f), new Vector2(-6.5f, 0f)),

                    // 기둥 셋. 높이를 엇갈리게 두어 위로 넘는 길과
                    // 아래로 도는 길의 값이 기둥마다 달라진다.
                    new StaticSegment(new Vector2(-4f, -2f), new Vector2(-4f, 1f)),
                    new StaticSegment(new Vector2(0f, -1f), new Vector2(0f, 3f)),
                    new StaticSegment(new Vector2(4f, -2f), new Vector2(4f, 1f)),

                    // 목표가 놓인 발판.
                    new StaticSegment(new Vector2(6.5f, goalY), new Vector2(9.5f, goalY)),
                },
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
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),   // 못 닿는 곳
                GoalRadius = 0.5f,
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
