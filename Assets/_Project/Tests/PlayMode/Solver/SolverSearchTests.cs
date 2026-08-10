using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 탐색의 계약.
    /// </summary>
    public class SolverSearchTests
    {
        /// <summary>
        /// 기저가 이미 Clear 인 판은 첫 판에서 끝나야 한다 (D10).
        /// 한 판이라도 더 굴렸다면 "이미 이긴 판" 을 못 알아본 것이다.
        /// </summary>
        [UnityTest]
        public IEnumerator L001_은_빈_풀이로_즉시_끝난다()
            => 기저가_이긴다(SampleLevelFile.Load(), "L001");

        [UnityTest]
        public IEnumerator L002_는_빈_풀이로_즉시_끝난다()
            => 기저가_이긴다(FeatureLevelFile.LoadLevel(), "L002");

        static IEnumerator 기저가_이긴다(LevelData level, string name)
        {
            var search = new SolverSearch(level);
            yield return search.Run();

            SolverReport report = search.Report;
            Debug.Log($"[{name}] {report}");

            Assert.AreEqual(SolverStop.Solved, report.Stop);
            Assert.IsNotNull(report.Solution);
            Assert.IsEmpty(report.Solution.Strokes, "기저가 이기는 판의 답은 빈 Solution 이다");
            Assert.AreEqual(1, report.Sims, "무배치 한 판으로 끝났어야 한다");
        }

        /// <summary>
        /// 깊이 1 을 끝까지 훑으면 Exhausted 로 끝나야 한다.
        /// 전수 순회의 계약이 이것뿐이다 — 예산에 걸려 끝나면
        /// 그 실행은 레벨에 대해 아무것도 말하지 못한다.
        /// </summary>
        [UnityTest, Timeout(1800000)]
        public IEnumerator 깊이_1_은_예산_안에_전수_순회를_끝낸다()
        {
            var search = new SolverSearch(TestLevels.GapPuzzle(), maxDepth: 1);
            yield return search.Run();

            SolverReport report = search.Report;
            Debug.Log($"[깊이 1 전수] {report}");

            Assert.AreEqual(SolverStop.Exhausted, report.Stop,
                "예산이 먼저 걸리면 실패를 판정으로 쓸 수 없다");
        }

        /// <summary>
        /// 내보내는 Solution 이 실제로 굴린 배치와 같은 결과를 내야 한다 (D8).
        /// 탐색은 벡터를 굴리는데 답은 Solution 으로 나가므로, 그 사이에
        /// 코덱 왕복이 낀다. 여기가 어긋나면 "풀었다" 는 판정이 거짓이 된다.
        /// 퍼즐이 아직 안 풀려 탐색으로는 이 경로를 못 밟아, 배치를 직접 넣는다.
        /// </summary>
        [Test]
        public void 내보내는_풀이가_굴린_배치와_같다()
        {
            var level = TestLevels.GapPuzzle();
            var codec = new PrimitiveCodec(level);
            Primitive[] primitives = 시작점_주위_후보(level, count: 2);

            SimResult rolled = new PrimitiveTrial(level, 0).Run(codec.Encode(primitives)).Sim;
            SimResult replayed = SimRunner.Run(
                level, SolverSearch.Rebuild(codec, primitives), 0);

            Assert.AreEqual(rolled.Outcome, replayed.Outcome, "판정이 다르다");
            Assert.AreEqual(rolled.EndStep, replayed.EndStep, "끝난 스텝이 다르다");
            Assert.AreEqual(rolled.MinGoalDist, replayed.MinGoalDist, 1e-4f,
                "같은 배치인데 궤적이 갈렸다");
        }

        /// 탐색이 첫 노드에서 실제로 내는 것과 같은 후보들.
        static Primitive[] 시작점_주위_후보(LevelData level, int count)
        {
            var candidates = new PrimitiveCandidates(level, SolverConfig.CandidateSizeSteps);
            var area = LevelDataArea.Calculate(level);
            var picked = new List<Primitive>();

            float ink = 0f;
            foreach (var candidate in candidates.At(new BallState(level.BallStart, Vector2.zero)))
            {
                if (PrimitiveValidator.Validate(candidate, level, ink, area)
                    != PlacementReject.None)
                    continue;

                picked.Add(candidate);
                ink += PrimitiveValidator.Ink(candidate);

                if (picked.Count == count) break;
            }

            Assert.AreEqual(count, picked.Count, "놓을 수 있는 후보가 모자라 검증이 성립하지 않는다");
            return picked.ToArray();
        }

        /// <summary>
        /// 예산 소진은 "못 푸는 판" 과 다른 값으로 나가야 한다 (D9).
        /// 같은 값이면 풀리는 레벨을 못 푼다고 판정하게 되고,
        /// 그것이 이 프로젝트의 실패 조건이다.
        /// </summary>
        [UnityTest]
        public IEnumerator 예산_소진은_풀_수_없음과_다른_값이다()
        {
            var search = new SolverSearch(TestLevels.GapPuzzle(), seed: 0, simBudget: 40);
            yield return search.Run();

            SolverReport report = search.Report;
            Debug.Log($"[예산 40] {report}");

            Assert.AreEqual(SolverStop.SimBudget, report.Stop);
            Assert.AreNotEqual(SolverStop.Exhausted, report.Stop,
                "예산이 모자란 것과 후보 격자에 답이 없는 것은 다른 사실이다");
            Assert.IsNull(report.Solution, "못 찾았으면 풀이를 내놓으면 안 된다");
        }

        /// <summary>
        /// 실패 응답이 들고 나와야 할 것들 (D9).
        /// 이게 없으면 레벨을 고칠 수도, 예산을 정할 수도 없다.
        /// </summary>
        [UnityTest]
        public IEnumerator 실패_보고서가_시도_횟수와_목표_거리를_싣는다()
        {
            var search = new SolverSearch(TestLevels.GapPuzzle(), seed: 0, simBudget: 40);
            yield return search.Run();

            SolverReport report = search.Report;

            Assert.AreEqual(40, report.Sims, "예산만큼 시도했어야 한다");
            Assert.IsTrue(float.IsFinite(report.MinGoalDist),
                "한 판이라도 굴렸으면 목표까지의 최소 거리가 있다");
            Assert.Greater(report.Elapsed.TotalSeconds, 0d);
        }
    }
}
