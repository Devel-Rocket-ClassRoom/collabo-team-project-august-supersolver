using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 그릇이 실제로 공을 담는가.
    /// 오목 폴리라인이라 콜라이더가 선분마다
    /// 쪼개지고, 그 이음매로 새면 여기서 걸린다.
    /// </summary>
    public class BowlContainmentTests
    {
        const int Steps = 400;   // 낙하 + 튕김이 잦아든다.

        /// 그릇 외접원 반지름. 지형 홈보다 넓다.
        const float Size = 2f;

        [Test]
        public void 공이_그릇에_담긴다()
        {
            var solution = new Solution();
            solution.Strokes.Add(BowlStroke(new Vector2(0f, Size), Size));

            using (var world = WorldBuilder.Build(BowlPit(), solution, 0))
            {
                for (int i = 0; i < Steps; i++) world.Step();

                Vector2 ball = world.Ball.position;

                // 새면 홈 아래로 빠져 KillY(-20)를 지난다.
                Assert.Greater(ball.y, -1.5f,
                    $"공이 그릇을 새어 {ball.y:F2} 까지 내려갔다.");
                Assert.Less(Mathf.Abs(ball.x), Size,
                    $"공이 그릇 밖({ball.x:F2})으로 나갔다.");
            }
        }

        /// <summary>
        /// 지형에 홈을 파고 그릇을 걸쳐 둔다.
        /// 바닥을 비워야 샌 공이 멈추지 않고 떨어져,
        /// 담김과 샘이 높이 하나로 갈린다.
        /// </summary>
        static LevelData BowlPit()
        {
            return new LevelData
            {
                InkLimit = 20f,
                BallStart = new Vector2(0f, 3.2f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-6f, 0f), new Vector2(-1.5f, 0f)),
                    new StaticSegment(new Vector2(1.5f, 0f), new Vector2(6f, 0f)),
                },
            };
        }

        static Stroke BowlStroke(Vector2 center, float size)
        {
            var points = PrimitivePolyline.LocalPoints(PrimitiveShape.Bowl, size);
            for (int i = 0; i < points.Count; i++) points[i] += center;
            return new Stroke(ToolType.FreeBody, points);
        }
    }
}
