using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 실제로 모은 간선 위에서 h 가 성립하는지 본다.
    /// 손으로 짠 그래프와 달리 여기서는 값이 맞는지 눈으로 못 보므로,
    /// 반드시 참이어야 하는 것만 걸고 분포는 로그로 남긴다.
    /// h 가 쓸 만한 깊이가 어디부터인지도 이 로그로 정한다.
    /// </summary>
    public class HeuristicMapSimTests
    {
        /// <summary>
        /// GapPuzzle 은 선 셋으로 풀린다. Wins 는 확장 중인 프론티어 셀에
        /// 기록되므로, 마지막 확장이 깊이 2 셀에서 "한 개 더로 이기는가" 를
        /// 물어야 3개짜리 풀이가 잡힌다. 그래서 깊이 3 이다.
        /// 더 얕게 돌리면 이기는 셀이 0 이고 h 가 통째로 빈다.
        /// </summary>
        /// 4번 표 기준 깊이 3 이 135만 시뮬 · 9분이다.
        [UnityTest, Timeout(3600000)]
        public IEnumerator 퍼즐_레벨_깊이_3_의_h_분포()
        {
            var builder = new CellGraphBuilder(TestLevels.GapPuzzle(), seed: 0, maxDepth: 3);
            yield return builder.Build();

            HeuristicMap map = HeuristicMap.Build(builder.Edges, builder.Wins);

            Debug.Log(
                $"[h 깊이 {builder.ReachedDepth}{(builder.Stopped ? " · 예산 소진, 미완" : "")}] " +
                $"셀 {builder.States.Count:N0} · 간선 {builder.Edges.Count:N0} · " +
                $"이기는 셀 {builder.Wins.Count:N0}\n" +
                $"  시뮬 {builder.Sims:N0} (월드 {builder.Worlds:N0}) · " +
                $"씬 최고 +{builder.PeakScenes} (절대 {builder.PeakTotalScenes}) · {builder.Elapsed.TotalSeconds:F0}초\n" +
                $"  값을 아는 셀 {map.Count:N0} " +
                $"({(builder.States.Count == 0 ? 0 : (double)map.Count / builder.States.Count):P1}) · " +
                $"가장 먼 값 {map.Furthest} · 벌점 {map.Penalty}\n" +
                $"  {Histogram(builder, map)}");

            // 이기는 셀이 없으면 h 가 전부 같은 값이라 맵이 아무 일도 못 한다.
            // 여기서 걸리면 후보 격자로는 3홉 안에 이기는 배치를 못 만든다는
            // 뜻이고, 그건 깊이가 아니라 후보 설계로 돌아가야 할 일이다.
            Assert.Greater(builder.Wins.Count, 0,
                "깊이 3 까지 갔는데 이기는 셀이 하나도 없다");

            // 이기는 셀의 h 는 그때 든 선 개수를 넘을 수 없다.
            // 더 짧은 길이 따로 있으면 작아질 수는 있다.
            foreach (var win in builder.Wins)
                Assert.LessOrEqual(map.Of(win.Key), win.Value,
                    $"{win.Value} 개로 이기는 셀인데 h 가 더 크다: {win.Key}");

            Assert.AreEqual(map.Furthest + 1, map.Penalty,
                "벌점은 아는 것 중 가장 먼 값 바로 다음이어야 한다 — 컷이 아니라 벌점이다");
        }

        /// h 값별 셀 수. ∞ 셀 비율이 문제가 되는지 여기서 본다 (D9).
        static string Histogram(CellGraphBuilder builder, HeuristicMap map)
        {
            var counts = new Dictionary<int, int>();
            int unknown = 0;

            foreach (BallCell cell in builder.States.Keys)
            {
                if (!map.Knows(cell)) { unknown++; continue; }

                counts.TryGetValue(map.Of(cell), out int seen);
                counts[map.Of(cell)] = seen + 1;
            }

            var text = new System.Text.StringBuilder();
            for (int h = 0; h <= map.Furthest; h++)
            {
                counts.TryGetValue(h, out int count);
                text.Append($"h={h}: {count:N0}  ");
            }

            text.Append($"모름: {unknown:N0}");
            return text.ToString();
        }
    }
}
