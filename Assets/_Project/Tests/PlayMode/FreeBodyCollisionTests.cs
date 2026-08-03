using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 자유 물체가 다른 것들과 실제로 부딪히는가.
    /// edge ↔ edge 는 충돌하지 않아, 기존 검사가
    /// 공(원)만 봐서 오래 놓쳤던 구멍이다.
    /// </summary>
    public class FreeBodyCollisionTests
    {
        const int Steps = 240;   // 4초. 자리를 잡고도 남는다.

        [Test]
        public void 자유_물체가_지형을_통과하지_않는다()
        {
            // 통과하면 4초 만에 78 유닛 아래로 내려간다.
            var solution = SolutionWith(Bar(new Vector2(2f, 3f), new Vector2(4f, 3f)));

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), solution, 0))
            {
                var bar = FindStroke(world, 0);
                for (int i = 0; i < Steps; i++) world.Step();

                Assert.Greater(bar.position.y, -0.5f,
                    $"자유 물체가 지형을 통과해 {bar.position.y:F2} 까지 내려갔다.");
                Assert.Less(bar.position.y, 0.5f, "지형 위에 얹혔다기엔 너무 높다.");
            }
        }

        [Test]
        public void 자유_물체가_고정선_위에_멈춘다()
        {
            // 게임에서 가장 흔할 조합이다.
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(1.5f, 1f),
                new Vector2(4.5f, 1f),
            }));
            solution.Strokes.Add(Bar(new Vector2(2.4f, 3f), new Vector2(3.6f, 3f)));

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), solution, 0))
            {
                var bar = FindStroke(world, 1);
                for (int i = 0; i < Steps; i++) world.Step();

                Assert.Greater(bar.position.y, 0.5f,
                    $"고정선(y=1)을 통과해 {bar.position.y:F2} 까지 내려갔다.");
            }
        }

        [Test]
        public void 자유_물체끼리_충돌한다()
        {
            // 통과했다면 둘 다 바닥에 겹쳐 높이 차가 사라진다.
            var solution = SolutionWith(
                Bar(new Vector2(2f, 3f), new Vector2(4f, 3f)),
                Bar(new Vector2(2f, 5f), new Vector2(4f, 5f)));

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), solution, 0))
            {
                var lower = FindStroke(world, 0);
                var upper = FindStroke(world, 1);

                for (int i = 0; i < Steps; i++) world.Step();

                Assert.Greater(upper.position.y, lower.position.y + 0.05f,
                    $"위 막대({upper.position.y:F2})가 아래 막대({lower.position.y:F2})를 통과했다.");
            }
        }

        [Test]
        public void 공이_닫힌_자유_물체_위에_얹힌다()
        {
            // 원 ↔ 다각형. 얹히면 약 1.15, 통과하면 0.25.
            var box = new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1f, 0.1f),
                new Vector2(1f, 0.1f),
                new Vector2(1f, 0.9f),
                new Vector2(-1f, 0.9f),
                new Vector2(-1f, 0.1f),
            });

            using (var world = WorldBuilder.Build(TestLevels.FlatRest(), SolutionWith(box), 0))
            {
                for (int i = 0; i < Steps; i++) world.Step();

                Assert.Greater(world.Ball.position.y, 0.7f,
                    $"공이 상자를 통과해 {world.Ball.position.y:F2} 까지 내려갔다.");
            }
        }

        // ── 도구 ──

        static Stroke Bar(Vector2 a, Vector2 b)
            => new Stroke(ToolType.FreeBody, new List<Vector2> { a, b });

        static Solution SolutionWith(params Stroke[] strokes)
        {
            var solution = new Solution();
            for (int i = 0; i < strokes.Length; i++) solution.Strokes.Add(strokes[i]);
            return solution;
        }

        static Rigidbody2D FindStroke(SimWorld world, int strokeIndex)
        {
            string name = $"Stroke_{strokeIndex}";

            for (int i = 0; i < world.Bodies.Count; i++)
            {
                var body = world.Bodies[i];
                if (body != null && body.name == name) return body;
            }

            Assert.Fail($"{name} 바디를 찾지 못했다.");
            return null;
        }
    }
}
