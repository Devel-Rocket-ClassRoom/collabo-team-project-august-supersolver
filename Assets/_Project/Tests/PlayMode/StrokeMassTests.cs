using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 자유 물체의 질량 특성이 **폴리라인에서도 맞는가**.
    ///
    /// 이전 구현은 무게중심을 포인트의 산술 평균으로, 관성을 곧은 막대 공식으로 잡았다.
    /// 2점 직선에서만 맞는 값이라 프리미티브(경사면·그릇·바퀴)가 곡선을 전개하기 시작하면
    /// 그 오차가 그대로 물리 결과가 된다. 코어를 동결하기 전에 닫아야 할 구멍이었다.
    ///
    /// 기대값은 전부 손으로 계산한 해석값이다. 구현이 스스로를 증명하지 않도록
    /// 코드가 쓰는 것과 다른 경로로 구한 숫자를 박아 둔다.
    /// </summary>
    public class StrokeMassTests
    {
        const float Density = ColliderFactory.FreeBodyMassPerUnit;

        [Test]
        public void 직선_자유물체는_막대_공식_그대로다()
        {
            // 회귀 감시용. 이 값이 바뀌면 기존 직선 레벨의 해시 기준선이 통째로 어긋난다.
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
            // 오른쪽 끝에만 점이 촘촘한 직선. 형상은 (-1,3)~(1,3) 인 곧은 막대 그대로다.
            //   포인트 산술 평균 : x = (-1 + 0.5 + 0.75 + 1) / 4 = 0.3125   ← 이전 구현의 값
            //   길이 비중 무게중심 : x = 0                                   ← 곧은 막대의 실제 중심
            // 같은 형상이 점을 어디에 찍었느냐로 다르게 움직이면 안 된다.
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
                    "무게중심이 점이 촘촘한 쪽으로 끌렸다 — 길이 비중이 아니라 포인트 평균을 쓰고 있다.");
            }
        }

        [Test]
        public void 꺾인_폴리라인의_질량_특성은_해석값과_맞는다()
        {
            // ㄱ 자. 선분 A (0,3)~(2,3) 중점 (1,3), 선분 B (2,3)~(2,5) 중점 (2,4). 둘 다 길이 2.
            //   무게중심 = (1,3)·0.5 + (2,4)·0.5 = (1.5, 3.5)
            //   포인트 평균이라면 (4/3, 11/3) ≈ (1.333, 3.667) 로 어긋난다
            //   관성 = 2·(2²/12) + 2·0.5  를 두 선분에 대해 = 3.3333
            //   곧은 막대 공식이라면 m·L²/12 = 4·16/12 = 5.333 으로 60% 과대
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
            // 바퀴 프리미티브가 마주칠 경우. 반지름 1 의 얇은 고리는 I = m·r² 이지만
            // 곧은 막대 공식은 m·L²/12 = m·(2πr)²/12 ≈ 3.3·m·r² 을 준다.
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
            // **이것이 진짜 불변량이다.** 직선을 중간 점으로 쪼개는 것은 형상을 전혀 바꾸지 않는다.
            // 같은 철사를 어디서 끊어 세느냐의 문제일 뿐이므로 질량·무게중심·관성이 모두 같아야 한다.
            //
            // 이전 구현은 여기서 깨졌다. 포인트 산술 평균이라 쪼갠 위치가 무게중심을 끌고 다녔다.
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
            // 원을 몇 각형으로 전개하느냐는 **형상 자체가 달라지는 것**이다. 변이 원 안쪽을
            // 가로지르는 만큼 질량이 중심에 가깝게 분포하므로, 각형이 클수록 얇은 고리(I = m·r²)에
            // 가까워진다. 정 N 각형의 형상 관성은 정확히 다음과 같다.
            //
            //     I/m = 1 - (2/3)·sin²(π/N)
            //
            // 해석해를 그대로 기대값으로 쓴다 — 구현과 다른 경로로 구한 숫자여야 검증이 성립한다.
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

        // ── 도구 ──────────────────────────────────────────────────────────

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
