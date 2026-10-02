using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 레벨과 풀이의 구성이 그대로 물리 월드가 되는가.
    /// 파싱이 맞아도 WorldBuilder 가 흘릴 수 있다.
    /// Outcome 은 단정하지 않는다 — 목적이 다르다.
    /// </summary>
    public class WorldBuilderTests
    {
        [Test]
        public void 판_구성이_그대로_월드가_된다()
        {
            var level = TestLevels.PivotSwingWithBomb();
            var solution = TestLevels.PivotSolution();

            using (var world = WorldBuilder.Build(level, solution, 0))
            {
                // 공 1 + 지형 + 장치 + 스트로크.
                // 순서도 이 순서다.
                int expected = 1 + level.Terrain.Count + level.Devices.Count + solution.Strokes.Count;

                Assert.AreEqual(expected, world.Bodies.Count,
                    "바디 수가 판 구성과 다르다 — 어딘가에서 스트로크나 지형을 흘렸다.");
            }
        }

        [Test]
        public void 고정선은_정적_자유물체는_동적으로_선다()
        {
            var solution = TestLevels.PivotSolution();

            int expectedStatics = 0;
            int expectedDynamics = 0;

            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                if (solution.Strokes[i].Tool == ToolType.FixedLine) expectedStatics++;
                else expectedDynamics++;
            }

            // 도구 두 종류가 다 있어야 검사가 성립한다.
            Assert.Greater(expectedStatics, 0);
            Assert.Greater(expectedDynamics, 0);

            using (var world = WorldBuilder.Build(TestLevels.PivotSwingWithBomb(), solution, 0))
            {
                int statics = 0;
                int dynamics = 0;

                for (int i = 0; i < solution.Strokes.Count; i++)
                {
                    var body = FindStroke(world, i);
                    if (body.bodyType == RigidbodyType2D.Static) statics++;
                    else dynamics++;
                }

                Assert.AreEqual(expectedStatics, statics);
                Assert.AreEqual(expectedDynamics, dynamics);
            }
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
