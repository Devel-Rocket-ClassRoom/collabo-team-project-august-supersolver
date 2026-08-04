using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 자유 물체의 질량 특성이 폴리라인에서도 맞는가.
    /// 기대값은 전부 손으로 계산한 해석값이다 —
    /// 구현이 스스로를 증명하지 않게 한다.
    /// </summary>
    public class StrokeMassTests
    {
        const float Density = ColliderFactory.FreeBodyMassPerUnit;

        [Test]
        public void 직선_자유물체는_막대_공식_그대로다()
        {
            // 회귀 감시용. 바뀌면 기존 레벨의 해시가 어긋난다.
            var points = new List<Vector2> { new Vector2(-1f, 3f), new Vector2(1f, 3f) };

            using (var world = Build(points, out var bar))
            {
                const float length = 2f;
                float mass = length * Density;

                Assert.AreEqual(mass, bar.mass, 1e-4f);
                AssertCenterOfMass(bar, new Vector2(0f, 3f));
                Assert.AreEqual(mass * length * length / 12f, bar.inertia, 1e-4f);
            }
        }

        [Test]
        public void 무게중심은_포인트_평균이_아니라_길이_비중이다()
        {
            // 오른쪽만 점이 촘촘한 직선. 형상은 곧은 막대다.
            //   포인트 평균 x = 0.3125, 길이 비중 x = 0.
            var points = new List<Vector2>
            {
                new Vector2(-1f, 3f),
                new Vector2(0.5f, 3f),
                new Vector2(0.75f, 3f),
                new Vector2(1f, 3f),
            };

            using (var world = Build(points, out var bar))
            {
                AssertCenterOfMass(bar, new Vector2(0f, 3f),
                    "무게중심이 점이 촘촘한 쪽으로 끌렸다 — 포인트 평균을 쓰고 있다.");
            }
        }

        [Test]
        public void 꺾인_폴리라인의_질량_특성은_해석값과_맞는다()
        {
            // ㄱ 자. 두 선분 다 길이 2.
            //   무게중심 (1.5, 3.5), 관성 3.3333.
            //   포인트 평균이면 (1.33, 3.67), 막대 공식이면 5.33.
            var points = new List<Vector2>
            {
                new Vector2(0f, 3f),
                new Vector2(2f, 3f),
                new Vector2(2f, 5f),
            };

            using (var world = Build(points, out var bar))
            {
                Assert.AreEqual(4f * Density, bar.mass, 1e-4f);
                AssertCenterOfMass(bar, new Vector2(1.5f, 3.5f));
                Assert.AreEqual(3.3333f, bar.inertia, 0.002f);
            }
        }

        [Test]
        public void 원형_폴리라인의_관성은_막대_공식보다_훨씬_작다()
        {
            // 얇은 고리는 I = m·r².
            // 막대 공식이면 m·(2πr)²/12 ≈ 3.3·m·r².
            const float radius = 1f;
            var points = Circle(new Vector2(0f, 4f), radius, segments: 32);

            using (var world = Build(points, out var bar))
            {
                float ring = bar.mass * radius * radius;
                float rod = bar.mass * TotalLength(points) * TotalLength(points) / 12f;

                Assert.AreEqual(ring, bar.inertia, ring * 0.03f,
                    "고리의 관성이 m·r² 에서 벗어났다.");
                Assert.Less(bar.inertia, rod * 0.5f,
                    $"관성이 여전히 막대 공식에 가깝다 (고리 {ring:F3} / 막대 {rod:F3} / 실제 {bar.inertia:F3}).");
            }
        }

        [Test]
        public void 같은_직선을_불규칙하게_쪼개도_질량_특성이_같다()
        {
            // 쪼개는 것은 형상을 바꾸지 않는다.
            // 이전 구현은 쪼갠 위치가 무게중심을 끌고 다녔다.
            var whole = new List<Vector2> { new Vector2(-1f, 3f), new Vector2(1f, 3f) };
            var split = new List<Vector2>
            {
                new Vector2(-1f, 3f),
                new Vector2(-0.4f, 3f),
                new Vector2(0.1f, 3f),
                new Vector2(0.55f, 3f),
                new Vector2(1f, 3f),
            };

            using (var a = Build(whole, out var wholeBar))
            using (var b = Build(split, out var splitBar))
            {
                Assert.AreEqual(wholeBar.mass, splitBar.mass, 1e-4f);
                AssertCenterOfMass(splitBar, wholeBar.worldCenterOfMass);
                Assert.AreEqual(wholeBar.inertia, splitBar.inertia, 1e-4f,
                    "같은 직선인데 중간 점 위치가 관성을 바꿨다.");
            }
        }

        [Test]
        public void 다각형은_변이_많아질수록_고리에_수렴한다()
        {
            // 각형이 클수록 얇은 고리에 가까워진다.
            // 정 N 각형의 형상 관성: I/m = 1 - (2/3)·sin²(π/N).
            foreach (int segments in new[] { 8, 16, 32, 64 })
            {
                float theta = Mathf.PI / segments;
                float expected = 1f - 2f / 3f * Mathf.Sin(theta) * Mathf.Sin(theta);

                using (var world = Build(Circle(new Vector2(0f, 4f), 1f, segments), out var ring))
                {
                    Assert.AreEqual(expected, ring.inertia / ring.mass, 1e-3f,
                        $"{segments}각형의 형상 관성이 해석해와 다르다.");
                }
            }
        }

        // ── 도구 ──

        static SimWorld Build(List<Vector2> points, out Rigidbody2D stroke)
        {
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, points));

            var world = WorldBuilder.Build(TestLevels.FlatRest(), solution, 0);

            stroke = null;
            for (int i = 0; i < world.Bodies.Count; i++)
            {
                if (world.Bodies[i] != null && world.Bodies[i].name == "Stroke_0")
                {
                    stroke = world.Bodies[i];
                    break;
                }
            }

            Assert.IsNotNull(stroke, "Stroke_0 바디를 찾지 못했다.");
            return world;
        }

        static void AssertCenterOfMass(Rigidbody2D body, Vector2 expected,
                                       string message = null, float tolerance = 1e-3f)
        {
            float distance = Vector2.Distance(body.worldCenterOfMass, expected);
            Assert.Less(distance, tolerance,
                message ?? $"무게중심이 {expected} 여야 하는데 {body.worldCenterOfMass} 다.");
        }

        static List<Vector2> Circle(Vector2 center, float radius, int segments)
        {
            var points = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }

        static float TotalLength(List<Vector2> points)
        {
            float sum = 0f;
            for (int i = 1; i < points.Count; i++) sum += Vector2.Distance(points[i - 1], points[i]);
            return sum;
        }
    }
}
