using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 디코드한 회전축이 실제로 도는가.
    /// 축이 무게중심에 있으면 중력만으로는 안 돌아
    /// 공을 떨어뜨려 토크를 준다.
    /// </summary>
    public class PrimitivePivotSimTests
    {
        const int Steps = 180;   // 3초. 낙하 + 충돌 + 회전이 끝난다.

        /// 프리미티브를 놓는 높이. 지면 위 공중.
        static readonly Vector2 Spawn = new Vector2(0f, 3f);

        /// 축에 붙어 있으면 이만큼도 못 벗어난다.
        const float AnchorDistanceTolerance = 0.1f;

        /// 회전했다고 볼 최소 각(도).
        const float TiltThreshold = 10f;

        [Test]
        public void 직선_중점축은_공이_얹힌_쪽으로_기운다()
        {
            // 오른팔에 떨어뜨리면 시계 방향(음의 각).
            Assert.Less(TiltAfterBallDrop(PrimitiveShape.Line, ballX: 1.5f), -TiltThreshold,
                "오른쪽으로 기울지 않았다.");

            // 왼팔이면 반대. 한쪽으로만 도는 게 아님을 본다.
            Assert.Greater(TiltAfterBallDrop(PrimitiveShape.Line, ballX: -1.5f), TiltThreshold,
                "왼쪽으로 기울지 않았다.");
        }

        [Test]
        public void 삼각형_중점축은_공에_맞으면_중심축으로_돈다()
        {
            Assert.Greater(Mathf.Abs(TiltAfterBallDrop(PrimitiveShape.Triangle, ballX: 0.7f)),
                TiltThreshold, "풍차가 돌지 않았다.");
        }

        [Test]
        public void 축이_같은_좌표에_겹쳐도_월드가_선다()
        {
            var center = new Vector2(0f, 3f);
            var primitives = new List<Primitive>
            {
                Pivoted(PrimitiveShape.Line, center, 0f),
                Pivoted(PrimitiveShape.Triangle, center, 0.5f),
                Pivoted(PrimitiveShape.Line, center, 1f),
            };

            var solution = PrimitiveDecoder.Decode(primitives);
            Assert.AreEqual(3, solution.Pivots.Count);

            using (var world = WorldBuilder.Build(Ground(new Vector2(-12f, 0.5f)), solution, 0))
            {
                for (int i = 0; i < Steps; i++) world.Step();
            }
        }

        /// <summary>
        /// 중점축을 월드에 고정한 프리미티브 위로
        /// 공을 떨어뜨리고, 축에 붙어 있는지 확인한 뒤
        /// 최종 회전각(도)을 돌려준다.
        /// </summary>
        static float TiltAfterBallDrop(PrimitiveShape shape, float ballX)
        {
            var primitive = Pivoted(shape, Spawn, 0f);
            var solution = PrimitiveDecoder.Decode(new List<Primitive> { primitive });
            Vector2 anchor = solution.Pivots[0].Anchor;

            using (var world = WorldBuilder.Build(
                Ground(new Vector2(ballX, Spawn.y + 3f)), solution, 0))
            {
                // 공(0) → 지면(1) → 스트로크(2) 순서다.
                var body = world.Bodies[2];
                float initial = Vector2.Distance(body.position, anchor);

                for (int i = 0; i < Steps; i++)
                {
                    world.Step();
                    float drift = Mathf.Abs(Vector2.Distance(body.position, anchor) - initial);
                    Assert.Less(drift, AnchorDistanceTolerance,
                        $"{shape} 가 축에서 {drift:F3} 만큼 벗어났다.");
                }

                return body.rotation;
            }
        }

        static Primitive Pivoted(PrimitiveShape shape, Vector2 center, float angle) =>
            new Primitive(shape, ToolType.FreeBody, center, angle, 2f, PivotSpot.Middle);

        /// 평평한 바닥 하나. 공의 시작점만 바뀐다.
        static LevelData Ground(Vector2 ballStart)
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = ballStart,
                BallRadius = 0.25f,
                GoalPosition = new Vector2(50f, 50f),
                GoalRadius = 0.5f,
                KillY = -20f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-14f, 0f), new Vector2(14f, 0f)),
                },
            };
        }
    }
}
