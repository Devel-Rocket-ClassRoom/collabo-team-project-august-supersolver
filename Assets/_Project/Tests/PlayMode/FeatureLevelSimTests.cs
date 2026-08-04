using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 파일의 모든 피처가 물리 월드에 닿는가.
    /// 파싱이 맞아도 WorldBuilder 가 흘릴 수 있다.
    /// Outcome 은 단정하지 않는다 — 목적이 다르다.
    /// </summary>
    public class FeatureLevelSimTests
    {
        [Test]
        public void 파일_구성이_그대로_월드가_된다()
        {
            var level = FeatureLevelFile.LoadLevel();
            var solution = FeatureLevelFile.LoadSolution();

            using (var world = WorldBuilder.Build(level, solution, 0))
            {
                // 공 1 + 지형 + 장치 + 스트로크.
            // 순서도 이 순서다.
                int expected = 1 + level.Terrain.Count + level.Devices.Count + solution.Strokes.Count;
                Assert.AreEqual(expected, world.Bodies.Count,
                    "바디 수가 파일 구성과 다르다 — 어딘가에서 스트로크나 지형을 흘렸다.");
            }
        }

        [Test]
        public void 고정선은_정적_자유물체는_동적으로_선다()
        {
            var solution = FeatureLevelFile.LoadSolution();

            using (var world = WorldBuilder.Build(FeatureLevelFile.LoadLevel(), solution, 0))
            {
                int statics = 0;
                int dynamics = 0;

                for (int i = 0; i < solution.Strokes.Count; i++)
                {
                    var body = FindStroke(world, i);
                    if (body.bodyType == RigidbodyType2D.Static) statics++;
                    else dynamics++;
                }

                Assert.AreEqual(FeatureLevelFile.ExpectedFixedLineCount, statics);
                Assert.AreEqual(FeatureLevelFile.ExpectedFreeBodyCount, dynamics);
            }
        }

        [Test]
        public void 회전축이_실제_조인트로_만들어진다()
        {
            var solution = FeatureLevelFile.LoadSolution();

            using (var world = WorldBuilder.Build(FeatureLevelFile.LoadLevel(), solution, 0))
            {
                int joints = 0;
                for (int i = 0; i < world.Bodies.Count; i++)
                {
                    var body = world.Bodies[i];
                    if (body != null) joints += body.GetComponents<HingeJoint2D>().Length;
                }

                Assert.AreEqual(FeatureLevelFile.ExpectedPivotCount, joints,
                    "회전축 개수만큼 조인트가 만들어지지 않았다.");
            }
        }

        [Test]
        public void 파일의_장치가_로직으로_등록된다()
        {
            using (var world = WorldBuilder.Build(FeatureLevelFile.LoadLevel(), FeatureLevelFile.LoadSolution(), 0))
            {
                Assert.IsTrue(world.AnyPendingWork(),
                    "파일에 장치가 있는데 대기 중인 로직이 없다 — DeviceFactory 까지 가지 못했다.");
            }
        }

        [Test]
        public void 전_피처_레벨도_결정론적이다()
        {
            // 고정선·자유 물체·닫힌 형태·조인트가 한 판에 모인 상태에서의 재현성.
            // 조인트와 접촉이 많을수록 부동소수점 합산 순서가 흔들릴 여지가 커진다.
            var a = new List<ulong>();
            var b = new List<ulong>();

            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), FeatureLevelFile.LoadSolution(), 11, a, 600);
            SimRunner.RunTraced(FeatureLevelFile.LoadLevel(), FeatureLevelFile.LoadSolution(), 11, b, 600);

            Assert.Greater(a.Count, 0, "시뮬이 한 스텝도 진행되지 않았다.");
            CollectionAssert.AreEqual(a, b);
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
