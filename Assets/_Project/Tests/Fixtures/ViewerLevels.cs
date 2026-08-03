using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 디버그 뷰어 전용 레벨. 테스트는 쓰지 않는다.
    /// 섞으면 테스트 레벨에 연출용 장치가 붙고
    /// 뷰어에서 본 것이 근거가 되지 못한다.
    /// </summary>
    public static class ViewerLevels
    {
        /// <summary>
        /// 스테이지 시드에 따라 44~81스텝에 터진다.
        /// 반경·세기는 그 창 전체에서 공이 밀리도록
        /// 잡은 값이다.
        /// </summary>
        static readonly DeviceData Bomb = new DeviceData
        {
            Type = DeviceType.Bomb,
            Position = new Vector2(-2.5f, 1.6f),
            Radius = 4f,
            Power = 7f,
            DelaySteps = 30,
            JitterSteps = 60,
        };

        // ── 뷰어 기본 ──
        // 장치가 있어야 rng 가 소비되는 것을 볼 수 있다.

        public static LevelData BombRamp()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(-4.5f, 3.3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(4.5f, -0.5f),
                GoalRadius = 0.5f,
                KillY = -8f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 3f), new Vector2(0f, 1f)),
                    new StaticSegment(new Vector2(2f, -1f), new Vector2(6f, -1f)),
                },
                Devices = new List<DeviceData> { Bomb },
            };
        }

        public static Solution BombRampSolution()
        {
            var solution = new Solution();

            // 끊긴 지형을 잇는다. 없으면 틈으로 떨어진다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(0f, 1f),
                new Vector2(2.2f, -0.9f),
            }));

            // 길목을 가로지르는 회전 막대.
            // 높이 -0.55 는 굴러오는 공의 몸통 안이다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(2.8f, -0.55f),
                new Vector2(4.2f, -0.55f),
            }));

            // 막대 가운데를 월드에 고정한다.
            solution.Pivots.Add(new PivotJoint(1, PivotJoint.WorldIndex, new Vector2(3.5f, -0.55f)));

            return solution;
        }

        // ── 자유 물체 전시장 ──

        /// <summary>
        /// 형태가 다른 자유 물체를 떨어뜨린다.
        /// 질량 특성이 틀리면 숫자가 아니라
        /// 움직임으로 드러난다.
        /// </summary>
        public static LevelData Showcase()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(-12f, 10f),
                BallRadius = 0.3f,
                GoalPosition = new Vector2(11.5f, 1.6f),
                GoalRadius = 0.6f,
                KillY = -6f,
                Terrain = new List<StaticSegment>
                {
                    // 비탈. 평지면 전부 앉아 있어 아무것도 안 보인다.
                    new StaticSegment(new Vector2(-13f, 8f), new Vector2(3f, 1f)),
                    new StaticSegment(new Vector2(3f, 1f), new Vector2(13f, 1f)),
                    new StaticSegment(new Vector2(-13f, 8f), new Vector2(-13f, 13f)),  // 왼쪽 담장
                    new StaticSegment(new Vector2(13f, 1f), new Vector2(13f, 9f)),     // 오른쪽 담장
                },
            };
        }

        /// <summary>
        /// 비탈 위에 늘어놓는다.
        /// 전부 띄우고 기울여 둔다 — 겹쳐 놓으면
        /// 밀어내는 방향이 불안정하다.
        /// </summary>
        public static Solution ShowcaseSolution()
        {
            var solution = new Solution();

            // 0) 바퀴 — 28각형이면 매끄럽게 구른다.
            solution.Strokes.Add(Closed(new Vector2(-11f, 8f), 0.55f, sides: 28));

            // 1) 상자 — 모서리로 섰다가 넘어간다.
            solution.Strokes.Add(Closed(new Vector2(-9f, 7.3f), 0.7f, sides: 4, rotation: 65f));

            // 2) 삼각형 — 무게중심이 가장 치우친 형태.
            solution.Strokes.Add(Closed(new Vector2(-7f, 6.4f), 0.7f, sides: 3, rotation: 90f));

            // 3) 기울어진 막대 — 미끄러지며 넘어간다.
            solution.Strokes.Add(Bar(new Vector2(-5f, 5.6f), length: 1.8f, degrees: 55f));

            // 4) ㄱ자 — 긴 팔 쪽으로 기운다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-3.8f, 4.6f),
                new Vector2(-2.2f, 4.6f),
                new Vector2(-2.2f, 5.8f),
            }));

            // 5) 지그재그 — 산술 평균이면 무게중심이 어긋난다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1.4f, 3.5f),
                new Vector2(-0.7f, 4.2f),
                new Vector2(0f, 3.5f),
                new Vector2(0.7f, 4.2f),
                new Vector2(1.4f, 3.5f),
            }));

            // 6) 그릇 — 오목한 쪽이 위로 와야 정상이다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody,
                Arc(new Vector2(4.5f, 2.5f), 1.1f, segments: 12, startDegrees: 200f, sweepDegrees: 140f)));

            // 7·8) 시소 — 회전축과 충돌이 함께 걸린다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(8.5f, 1f),
                new Vector2(8.5f, 1.7f),
            }));
            solution.Strokes.Add(Bar(new Vector2(8.5f, 1.8f), length: 3.2f, degrees: 8f));
            solution.Pivots.Add(new PivotJoint(8, PivotJoint.WorldIndex, new Vector2(8.5f, 1.8f)));

            return solution;
        }

        /// <summary>닫힌 정다각형. 변이 많으면 바퀴가 된다.</summary>
        static Stroke Closed(Vector2 center, float radius, int sides, float rotation = 0f)
            => new Stroke(ToolType.FreeBody, Arc(center, radius, sides, rotation, 360f));

        /// <summary>중심과 각도로 기울인 막대.</summary>
        static Stroke Bar(Vector2 center, float length, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 half = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (length * 0.5f);

            return new Stroke(ToolType.FreeBody, new List<Vector2> { center - half, center + half });
        }

        /// <summary>호를 폴리라인으로 전개한다. 360도면 고리.</summary>
        static List<Vector2> Arc(Vector2 center, float radius, int segments,
                                 float startDegrees, float sweepDegrees)
        {
            var points = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (startDegrees + sweepDegrees * i / segments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }
    }
}
