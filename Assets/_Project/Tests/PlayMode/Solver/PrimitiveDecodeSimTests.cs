using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 디코드한 스트로크가 실제로 서는가.
    /// 6종(모양 3 × 바디 2)을 각각 세워 보고,
    /// 같은 입력이 같은 해시를 내는지 본다.
    /// </summary>
    public class PrimitiveDecodeSimTests
    {
        const int Steps = 300;   // 5초. 낙하 + 안착이 끝난다.

        /// 프리미티브를 놓는 높이. 지면 위 공중.
        static readonly Vector2 Spawn = new Vector2(0f, 3f);

        const float Size = 1f;

        static readonly PrimitiveShape[] Shapes =
        {
            PrimitiveShape.Line,
            PrimitiveShape.Bowl,
            PrimitiveShape.Triangle,
        };

        [Test]
        public void 같은_프리미티브를_두_번_디코드하면_해시가_같다()
        {
            var primitives = SixCombinations();

            var traceA = new List<ulong>();
            var traceB = new List<ulong>();

            SimRunner.RunTraced(Ground(), ToSolution(primitives), 0, traceA, Steps);
            SimRunner.RunTraced(Ground(), ToSolution(primitives), 0, traceB, Steps);

            Assert.AreEqual(traceA.Count, traceB.Count, "스텝 수가 다르다.");
            for (int i = 0; i < traceA.Count; i++)
                Assert.AreEqual(traceA[i], traceB[i], $"{i}번 스텝에서 갈라졌다.");
        }

        [Test]
        public void Fixed는_제자리에_남는다([ValueSource(nameof(Shapes))] PrimitiveShape shape)
        {
            var primitive = new Primitive(shape, ToolType.FixedLine, Spawn, 0.3f, Size);

            using (var world = Build(primitive))
            {
                var body = StrokeBody(world);
                Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);

                Vector2 before = body.position;
                for (int i = 0; i < Steps; i++) world.Step();

                Assert.AreEqual(before, body.position, $"{shape} 발판이 움직였다.");
            }
        }

        [Test]
        public void Free는_배치_즉시_떨어져_지면에_얹힌다([ValueSource(nameof(Shapes))] PrimitiveShape shape)
        {
            var primitive = new Primitive(shape, ToolType.FreeBody, Spawn, 0.3f, Size);

            using (var world = Build(primitive))
            {
                var body = StrokeBody(world);
                Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);

                float before = body.position.y;
                for (int i = 0; i < Steps; i++) world.Step();

                float after = body.position.y;
                Assert.Less(after, before, $"{shape} 가 낙하하지 않았다.");

                // 통과해 빠지면 지면 아래로 계속 내려간다.
                Assert.Greater(after, -0.5f, $"{shape} 가 지면을 통과했다.");
            }
        }

        static SimWorld Build(in Primitive primitive)
        {
            var solution = new Solution();
            solution.Strokes.Add(PrimitiveDecoder.ToStroke(primitive));
            return WorldBuilder.Build(Ground(), solution, 0);
        }

        /// 공(0) → 지면(1) → 스트로크(2) 순서다.
        static Rigidbody2D StrokeBody(SimWorld world) => world.Bodies[2];

        /// 6종을 겹치지 않게 가로로 늘어놓는다.
        static List<Primitive> SixCombinations()
        {
            var list = new List<Primitive>();
            for (int i = 0; i < Shapes.Length; i++)
            {
                var center = new Vector2(-6f + i * 4f, Spawn.y);
                list.Add(new Primitive(Shapes[i], ToolType.FixedLine, center, 0.3f, Size));
                list.Add(new Primitive(Shapes[i], ToolType.FreeBody,
                    center + new Vector2(0f, 3f), 0.3f, Size));
            }
            return list;
        }

        static Solution ToSolution(List<Primitive> primitives)
        {
            var solution = new Solution();
            solution.Strokes.AddRange(PrimitiveDecoder.Decode(primitives));
            return solution;
        }

        /// 평평한 바닥 하나. 공은 옆으로 치워 둔다.
        static LevelData Ground()
        {
            return new LevelData
            {
                InkLimit = 100f,
                BallStart = new Vector2(-12f, 0.5f),
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
