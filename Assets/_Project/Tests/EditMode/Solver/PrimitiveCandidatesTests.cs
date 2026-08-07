using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 후보 생성기의 최소 보장.
    /// 개수가 손잡이에 비례하는지, 같은 배치가
    /// 두 번 나오지 않는지를 못박는다.
    /// </summary>
    public class PrimitiveCandidatesTests
    {
        const float Eps = 0.0001f;

        static LevelData Level()
        {
            var level = new LevelData
            {
                InkLimit = 20f,
                BallStart = Vector2.zero,
                BallRadius = 0.25f,
                GoalPosition = new Vector2(5f, 0f),
                GoalRadius = 0.5f,
            };
            level.Terrain.Add(new StaticSegment(new Vector2(-5f, -1f), new Vector2(5f, -1f)));
            return level;
        }

        static int AngleTotal()
        {
            int sum = 0;
            foreach (PrimitiveShape shape in Enum.GetValues(typeof(PrimitiveShape)))
                sum += PrimitiveShapeExtensions.Rule(shape).AngleDivisions;
            return sum;
        }

        /// 같은 배치인지 가르는 열쇠. 위치는 인자로
        /// 받아 모든 후보가 같으므로 빼둔다.
        static (PrimitiveShape, ToolType, float, float) Key(in Primitive p) =>
            (p.Shape, p.Body, p.Angle, p.Size);

        [Test]
        public void 개수는_각도합_바디_Size단계의_곱이다()
        {
            int bodies = Enum.GetValues(typeof(ToolType)).Length;

            foreach (int steps in new[] { 1, 3, 7 })
            {
                var candidates = new PrimitiveCandidates(Level(), steps);

                Assert.AreEqual(AngleTotal() * bodies * steps, candidates.Count, $"{steps} 단계");
                Assert.AreEqual(candidates.Count, candidates.At(Vector2.zero).Count(), $"{steps} 단계");
            }
        }

        [Test]
        public void Size_단계를_늘리면_개수도_그만큼_늘어난다()
        {
            var level = Level();

            int one = new PrimitiveCandidates(level, 1).At(Vector2.zero).Count();
            int four = new PrimitiveCandidates(level, 4).At(Vector2.zero).Count();

            Assert.AreEqual(one * 4, four);
        }

        [Test]
        public void 같은_배치가_두_번_나오지_않는다()
        {
            var seen = new HashSet<(PrimitiveShape, ToolType, float, float)>();

            foreach (var candidate in new PrimitiveCandidates(Level(), 5).At(new Vector2(1f, -0.5f)))
                Assert.IsTrue(seen.Add(Key(candidate)),
                    $"{candidate.Shape} {candidate.Body} {candidate.Angle} {candidate.Size}");
        }

        [Test]
        public void 각도는_주기를_균등하게_나눈다()
        {
            var byShape = new PrimitiveCandidates(Level(), 1).At(Vector2.zero)
                .GroupBy(p => p.Shape);

            foreach (var group in byShape)
            {
                var rule = PrimitiveShapeExtensions.Rule(group.Key);
                var angles = group.Select(p => p.Angle).Distinct().OrderBy(a => a).ToArray();

                Assert.AreEqual(rule.AngleDivisions, angles.Length, group.Key.ToString());

                for (int i = 0; i < angles.Length; i++)
                    Assert.AreEqual(rule.AnglePeriod * i / rule.AngleDivisions, angles[i], Eps,
                        group.Key.ToString());
            }
        }

        /// Shape 별 Size 칸을 작은 것부터.
        static Dictionary<PrimitiveShape, float[]> SizesByShape(LevelData level, int steps) =>
            new PrimitiveCandidates(level, steps).At(Vector2.zero)
                .GroupBy(p => p.Shape)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Size).Distinct().OrderBy(s => s).ToArray());

        [Test]
        public void Size는_공_반지름에서_시작해_상한에서_끝난다()
        {
            var level = Level();

            foreach (var pair in SizesByShape(level, 5))
            {
                Assert.AreEqual(5, pair.Value.Length, pair.Key.ToString());
                Assert.AreEqual(level.BallRadius, pair.Value[0], Eps, pair.Key.ToString());

                // 상한은 근사가 아니라 그 값이어야 한다.
                Assert.AreEqual(PrimitiveValidator.MaxSize(pair.Key, level), pair.Value[4],
                    pair.Key.ToString());
            }
        }

        [Test]
        public void Size는_간격이_아니라_비율이_일정하다()
        {
            foreach (var pair in SizesByShape(Level(), 5))
            {
                var sizes = pair.Value;
                float ratio = sizes[1] / sizes[0];

                for (int i = 2; i < sizes.Length; i++)
                    Assert.AreEqual(ratio, sizes[i] / sizes[i - 1], 0.001f, pair.Key.ToString());

                Assert.Greater(ratio, 1f, pair.Key.ToString());
            }
        }

        [Test]
        public void 단계가_하나면_상한만_나온다()
        {
            var level = Level();

            foreach (var pair in SizesByShape(level, 1))
                Assert.AreEqual(PrimitiveValidator.MaxSize(pair.Key, level), pair.Value.Single(),
                    pair.Key.ToString());
        }

        [Test]
        public void 가장_큰_후보도_혼자서는_잉크를_넘지_않는다()
        {
            var level = Level();

            foreach (var candidate in new PrimitiveCandidates(level, 5).At(Vector2.zero))
                Assert.AreNotEqual(PlacementReject.TooLarge,
                    PrimitiveValidator.Validate(candidate, level, 0f),
                    $"{candidate.Shape} {candidate.Size}");
        }

        [Test]
        public void 상한이_Shape마다_달라_같은_단계도_크기가_다르다()
        {
            var sizes = SizesByShape(Level(), 5);

            Assert.Greater(sizes[PrimitiveShape.Line][3], sizes[PrimitiveShape.Bowl][3]);
            Assert.Greater(sizes[PrimitiveShape.Bowl][3], sizes[PrimitiveShape.Triangle][3]);
        }

        [Test]
        public void 유효성은_보지_않아_영역_밖_후보도_그대로_나온다()
        {
            var level = Level();
            var area = LevelDataArea.Calculate(level);
            var candidates = new PrimitiveCandidates(level, 3);

            int rejected = candidates.At(area.max)
                .Count(p => PrimitiveValidator.Validate(p, level, 0f) != PlacementReject.None);

            Assert.AreEqual(candidates.Count, candidates.At(area.max).Count(), "개수는 위치와 무관");
            Assert.Greater(rejected, 0, "모서리에 놓으면 기각될 후보가 섞여 나온다");
        }
    }
}
