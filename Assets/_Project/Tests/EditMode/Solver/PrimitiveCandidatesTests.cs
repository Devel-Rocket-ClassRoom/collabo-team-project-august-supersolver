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

        /// 개수 검산이 눈에 보이도록 고정한다.
        const int Directions = 8;

        /// <summary>
        /// 설정이 Shape 을 빼고 넣어도 이 검사는 안 흔들려야 한다.
        /// 여기서 보는 건 축의 곱셈이지 어떤 Shape 을 쓰느냐가 아니다.
        /// </summary>
        static readonly PrimitiveShape[] Shapes =
            (PrimitiveShape[])Enum.GetValues(typeof(PrimitiveShape));

        /// 멈춘 공. 갈 방향이 없어 부채 기준이 중력 아래쪽이 된다.
        static readonly BallState Still = new BallState(Vector2.zero, Vector2.zero);

        /// 오른쪽으로 굴러가는 공. 부채가 +x 를 정면으로 편다.
        static BallState Moving(Vector2 position)
            => new BallState(position, Vector2.right * 5f);

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

        /// 같은 배치인지 가르는 열쇠.
        /// 위치도 축이라 함께 본다 — 방향마다 자리가 다르다.
        static (PrimitiveShape, ToolType, float, float, Vector2) Key(in Primitive p) =>
            (p.Shape, p.Body, p.Angle, p.Size, p.Center);

        [Test]
        public void 개수는_각도합_바디_Size단계_방향의_곱이다()
        {
            int bodies = Enum.GetValues(typeof(ToolType)).Length;

            foreach (int steps in new[] { 1, 3, 7 })
            {
                var candidates = new PrimitiveCandidates(Level(), steps, Directions, Shapes);

                Assert.AreEqual(AngleTotal() * bodies * steps * Directions, candidates.Count,
                    $"{steps} 단계");
                Assert.AreEqual(candidates.Count, candidates.At(Still).Count(), $"{steps} 단계");
            }
        }

        [Test]
        public void 방향을_늘리면_개수도_그만큼_늘어난다()
        {
            var level = Level();

            int four = new PrimitiveCandidates(level, 2, 4, Shapes).At(Still).Count();
            int eight = new PrimitiveCandidates(level, 2, 8, Shapes).At(Still).Count();

            Assert.AreEqual(four * 2, eight);
        }

        /// <summary>
        /// 공 자리에 그대로 놓으면 시작부터 박혀 튕겨 나가고,
        /// 그 궤적의 간선은 없는 이동이 된다.
        /// </summary>
        [Test]
        public void 어떤_후보도_공과_겹치지_않는다()
        {
            var level = Level();
            var ball = new Vector2(1f, -0.5f);

            foreach (var p in new PrimitiveCandidates(level, 5, Directions, Shapes).At(Moving(ball)))
                Assert.GreaterOrEqual(Vector2.Distance(p.Center, ball), p.Size + level.BallRadius - Eps,
                    $"{p.Shape} 크기 {p.Size:F3} 이 공에 박힌다");
        }

        /// <summary>
        /// 너무 멀면 공에 못 닿아 궤적을 못 바꾼다.
        /// 시뮬 값은 다 치르고 간선은 하나도 안 만든다.
        /// </summary>
        [Test]
        public void 어떤_후보도_공에서_닿을_거리_밖에_있지_않다()
        {
            var level = Level();
            var ball = new Vector2(1f, -0.5f);

            foreach (var p in new PrimitiveCandidates(level, 5, Directions, Shapes).At(Moving(ball)))
                Assert.LessOrEqual(Vector2.Distance(p.Center, ball), p.Size + level.BallRadius + Eps,
                    $"{p.Shape} 크기 {p.Size:F3} 이 공에 못 닿는다");
        }

        /// <summary>
        /// 공이 안 가는 쪽에 놓아 봐야 닿지 않는다.
        /// 실측에서 진행 방향 ±90° 밖은 새 셀을 거의 못 열었다.
        /// </summary>
        [Test]
        public void 후보가_진행_방향_앞쪽_반원에만_놓인다()
        {
            foreach (var p in new PrimitiveCandidates(Level(), 2, 5, Shapes).At(Moving(Vector2.zero)))
                Assert.GreaterOrEqual(p.Center.x, -Eps, $"진행 방향 뒤에 놓였다: {p.Center}");
        }

        /// <summary>
        /// 멈춘 공은 갈 곳이 없어 방향을 물을 수 없다.
        /// 그때는 중력이 데려갈 아래쪽이 정면이다.
        /// </summary>
        [Test]
        public void 멈춘_공은_아래쪽을_정면으로_삼는다()
        {
            foreach (var p in new PrimitiveCandidates(Level(), 2, 5, Shapes).At(Still))
                Assert.LessOrEqual(p.Center.y, Eps, $"멈춘 공 위에 놓였다: {p.Center}");
        }

        /// 갈래가 홀수라야 정면이 한 갈래로 잡힌다. 정면이 압도적이다.
        [Test]
        public void 부채의_한가운데가_속도_방향이다()
        {
            var ball = new BallState(Vector2.zero, new Vector2(3f, 4f));
            Vector2 forward = ball.Velocity.normalized;

            var all = new PrimitiveCandidates(Level(), 1, 5, Shapes).At(ball).ToList();

            Assert.IsTrue(all.Any(p => Vector2.Dot(p.Center.normalized, forward) > 0.999f),
                "정면 갈래가 없다");
        }

        [Test]
        public void Size_단계를_늘리면_개수도_그만큼_늘어난다()
        {
            var level = Level();

            int one = new PrimitiveCandidates(level, 1, Directions, Shapes).At(Still).Count();
            int four = new PrimitiveCandidates(level, 4, Directions, Shapes).At(Still).Count();

            Assert.AreEqual(one * 4, four);
        }

        [Test]
        public void 같은_배치가_두_번_나오지_않는다()
        {
            var seen = new HashSet<(PrimitiveShape, ToolType, float, float, Vector2)>();

            foreach (var candidate in new PrimitiveCandidates(Level(), 5, Directions, Shapes).At(Moving(new Vector2(1f, -0.5f))))
                Assert.IsTrue(seen.Add(Key(candidate)),
                    $"{candidate.Shape} {candidate.Body} 각도 {candidate.Angle} " +
                    $"크기 {candidate.Size} 자리 {candidate.Center}");
        }

        [Test]
        public void 각도는_주기를_균등하게_나눈다()
        {
            var byShape = new PrimitiveCandidates(Level(), 1, Directions, Shapes).At(Still)
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
            new PrimitiveCandidates(level, steps, Directions, Shapes).At(Still)
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

            foreach (var candidate in new PrimitiveCandidates(level, 5, Directions, Shapes).At(Still))
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
            var candidates = new PrimitiveCandidates(level, 3, Directions, Shapes);

            int rejected = candidates.At(Moving(area.max))
                .Count(p => PrimitiveValidator.Validate(p, level, 0f) != PlacementReject.None);

            Assert.AreEqual(candidates.Count, candidates.At(Moving(area.max)).Count(), "개수는 위치와 무관");
            Assert.Greater(rejected, 0, "모서리에 놓으면 기각될 후보가 섞여 나온다");
        }
    }
}
