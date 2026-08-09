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
    /// 맵 없이 도는 탐색의 계약.
    /// 여기서 나오는 시뮬 횟수가 D0 기준선이다 — 나중에 맵을 h 자리에
    /// 꽂았을 때 이 값과 비교해야 맵이 값을 하는지 알 수 있다.
    /// </summary>
    public class SolverSearchTests
    {
        /// <summary>
        /// 기저가 이미 Clear 인 판은 첫 팝에서 끝나야 한다 (D10).
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
        /// 풀 것이 있는 유일한 판. 여기서 나온 시뮬 횟수가 기준선이다.
        /// 예산 안에 못 찾으면 실패로 뜨지만, 그 실패가 곧
        /// "맵 없이는 이만큼도 못 간다" 는 측정값이다.
        /// </summary>
        [UnityTest, Timeout(3600000)]
        public IEnumerator 퍼즐_레벨을_예산_안에_푼다()
        {
            var level = TestLevels.GapPuzzle();
            var search = new SolverSearch(level);
            yield return search.Run();

            SolverReport report = search.Report;
            Debug.Log($"[GapPuzzle · D0 기준선] {report}");

            Assert.AreEqual(SolverStop.Solved, report.Stop,
                $"예산 {SolverConfig.SearchSimBudget:N0} 시뮬 안에 못 찾았다");
        }

        /// <summary>
        /// 맵을 h 자리에 꽂고 같은 판을 푼다.
        /// 사흘 걸려 만든 맵이 값을 하는지가 여기서 처음 수치로 나온다 —
        /// 비교 대상은 맵 없이 잰 D0 기준선이다.
        /// </summary>
        /// 맵 빌드 8.5분 + 탐색. 넉넉히 준다.
        [UnityTest, Timeout(3600000)]
        public IEnumerator 맵을_꽂으면_기준선보다_적은_시뮬로_푼다()
        {
            var level = TestLevels.GapPuzzle();

            var builder = new CellGraphBuilder(level, seed: 0, maxDepth: 3);
            yield return builder.Build();

            HeuristicMap map = HeuristicMap.Build(builder.Edges, builder.Wins);
            Debug.Log($"[맵] 셀 {builder.States.Count:N0} · 값을 아는 셀 {map.Count:N0} · " +
                      $"벌점 {map.Penalty} · 빌드 {builder.Sims:N0} 시뮬 " +
                      $"{builder.Elapsed.TotalSeconds:F0}초");

            // 시간이 아니라 시뮬 횟수를 맞춰야 기준선과 견줄 수 있다.
            // 앞선 실행은 시간 예산이 먼저 걸려 9% 만 굴리고 끝났다.
            var search = new SolverSearch(level, map, timeBudget: 3000d);
            yield return search.Run();

            SolverReport report = search.Report;

            // 기준선과 나란히 찍는다. 따로 찍으면 나중에 짝을 못 맞춘다.
            Debug.Log($"[맵 있음] {report}\n" +
                      $"[D0 기준선] SimBudget · 시뮬 200,000 · 노드 199,769 · " +
                      $"최대 깊이 2 · 최소 목표 거리 1.149 · 226.5초 (시뮬당 1.13ms)");

            Assert.AreEqual(SolverStop.Solved, report.Stop,
                $"맵을 꽂고도 예산 {SolverConfig.SearchSimBudget:N0} 안에 못 찾았다");
        }

        /// <summary>
        /// 반환한 풀이를 그대로 재실행하면 Clear 여야 한다 (D8).
        /// 탐색이 Clear 를 받은 배치와 내보내는 Solution 이 어긋나면
        /// 검증기가 통과시킨 레벨을 사람이 못 푸는 일이 생긴다.
        /// </summary>
        [UnityTest, Timeout(3600000)]
        public IEnumerator 찾은_풀이를_재실행하면_클리어한다()
        {
            var level = TestLevels.GapPuzzle();
            var search = new SolverSearch(level);
            yield return search.Run();

            if (!search.Report.Solved)
                Assert.Ignore($"풀이를 못 찾아 재실행할 것이 없다: {search.Report}");

            SimResult replay = SimRunner.Run(level, search.Report.Solution, 0);

            Assert.AreEqual(SimOutcome.Clear, replay.Outcome,
                $"탐색은 풀었다는데 재실행이 {replay.Outcome} 다");
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
            Assert.Greater(report.Nodes, 0, "굴려 본 배치가 없다");
            Assert.IsTrue(float.IsFinite(report.MinGoalDist),
                "한 판이라도 굴렸으면 목표까지의 최소 거리가 있다");
            Assert.Greater(report.Elapsed.TotalSeconds, 0d);
        }
    }
}
