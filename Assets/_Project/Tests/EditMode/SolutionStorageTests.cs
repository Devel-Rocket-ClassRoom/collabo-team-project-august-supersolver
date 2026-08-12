using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 저장이 그림을 바꾸지 않는가.
    /// 표기가 한 비트라도 깎이면 저장한 판과
    /// 솔버가 검증한 판이 다른 판이 된다.
    /// </summary>
    public class SolutionStorageTests
    {
        const string StageId = "T_SolutionStorage";

        [TearDown]
        public void 남긴_파일을_지운다()
        {
            string path = SolutionStorage.PathOf(StageId);
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void 임의_좌표_그림이_왕복에서_비트째_보존된다()
        {
            var original = ArbitrarySolution(20260812);

            Assert.IsTrue(SolutionStorage.Save(StageId, original), "저장에 실패했다.");
            Assert.IsTrue(SolutionStorage.TryLoad(StageId, out Solution restored),
                "저장한 파일을 다시 읽지 못했다.");

            Assert.AreEqual(original.Strokes.Count, restored.Strokes.Count, "획 개수가 다르다.");
            Assert.AreEqual(original.Pivots.Count, restored.Pivots.Count, "핀 개수가 다르다.");

            for (int s = 0; s < original.Strokes.Count; s++)
            {
                Stroke before = original.Strokes[s];
                Stroke after = restored.Strokes[s];

                Assert.AreEqual(before.Tool, after.Tool, $"획 {s} 의 도구가 다르다.");
                Assert.AreEqual(before.Points.Count, after.Points.Count, $"획 {s} 의 점 개수가 다르다.");

                for (int p = 0; p < before.Points.Count; p++)
                    AssertSameBits(before.Points[p], after.Points[p], $"획 {s} 점 {p}");
            }

            for (int i = 0; i < original.Pivots.Count; i++)
            {
                PivotJoint before = original.Pivots[i];
                PivotJoint after = restored.Pivots[i];

                Assert.AreEqual(before.StrokeA, after.StrokeA, $"핀 {i} 의 StrokeA 가 다르다.");
                Assert.AreEqual(before.StrokeB, after.StrokeB, $"핀 {i} 의 StrokeB 가 다르다.");
                AssertSameBits(before.Anchor, after.Anchor, $"핀 {i} 의 Anchor");
            }

            // 잉크는 길이 재계산이라 좌표가 살면 따라 산다.
            // 잔량이 이 값 하나로 성립하므로 따로 못 박는다.
            Assert.IsTrue(original.TotalInk().Equals(restored.TotalInk()),
                $"잉크 총량이 달라졌다: {original.TotalInk():R} → {restored.TotalInk():R}");
        }

        [Test]
        public void 없는_판을_읽으면_실패한다()
        {
            Assert.IsFalse(SolutionStorage.TryLoad("T_저장한적없는판", out Solution solution));
            Assert.IsNull(solution);
        }

        [Test]
        public void 저장_경로가_사양이_정한_자리다()
        {
            string path = SolutionStorage.PathOf("S042");

            StringAssert.StartsWith(Application.persistentDataPath, path);
            StringAssert.EndsWith(Path.Combine("solutions", "S042.json"), path);
        }

        /// <summary>
        /// 미결합 핀까지 섞어 지금 만들 수 있는
        /// 조합을 한 판에 담는다.
        /// </summary>
        static Solution ArbitrarySolution(int seed)
        {
            var rng = new System.Random(seed);
            var solution = new Solution();

            for (int s = 0; s < 4; s++)
            {
                var points = new List<Vector2>();
                for (int p = 0; p < 12; p++)
                    points.Add(new Vector2(NextCoord(rng), NextCoord(rng)));

                solution.Strokes.Add(new Stroke(
                    s % 2 == 0 ? ToolType.FixedLine : ToolType.FreeBody, points));
            }

            solution.Pivots.Add(new PivotJoint(0, 1, NextAnchor(rng)));
            solution.Pivots.Add(new PivotJoint(2, PivotJoint.WorldIndex, NextAnchor(rng)));
            solution.Pivots.Add(new PivotJoint(3, PivotJoint.Unbound, NextAnchor(rng)));

            return solution;
        }

        /// <summary>
        /// 가수부가 꽉 찬 값. 손으로 그은 획의 좌표가
        /// 이렇게 생겼다 — -4.5 같은 얌전한 값만 재면
        /// 표기가 깎여도 통과한다.
        /// </summary>
        static float NextCoord(System.Random rng) => (float)(rng.NextDouble() * 20.0 - 10.0);

        static Vector2 NextAnchor(System.Random rng) => new Vector2(NextCoord(rng), NextCoord(rng));

        /// <summary>
        /// float.Equals 는 비트 비교다. Vector2 의 == 는
        /// 오차를 허용해 여기 쓸 수 없다.
        /// </summary>
        static void AssertSameBits(Vector2 expected, Vector2 actual, string where)
        {
            Assert.IsTrue(expected.x.Equals(actual.x) && expected.y.Equals(actual.y),
                $"{where} 좌표가 왕복에서 바뀌었다: " +
                $"({expected.x:R}, {expected.y:R}) → ({actual.x:R}, {actual.y:R})");
        }
    }
}
