using System.Collections;
using NUnit.Framework;
using PPS.Core.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 도달 집합·간선 수집기의 계약.
    /// 이 값들은 측정으로 얻어 4번에 기록된 것이라, 어긋나면
    /// 그 위에서 정한 후보 설정과 깊이 판단이 같이 무효가 된다.
    /// </summary>
    public class CellGraphBuilderTests
    {
        /// <summary>
        /// 4번 정정 표의 GapPuzzle 깊이 1 이 775 였다.
        /// 간선을 굴린 자리에서 뻗도록 고친 뒤로는 이보다 늘어야 한다 —
        /// 출발 셀이 도달 집합에 들어오고, 첫 표본까지의 홉이 살아나
        /// 그 자리에서 넓힐 수 있게 됐다. 줄면 뭔가 잃은 것이다.
        /// 새 값이 나오면 정확히 못박는다.
        /// </summary>
        const int Depth1CellsBefore = 775;

        /// 깊이 1 은 5.3초였다. 넉넉히 준다.
        [UnityTest, Timeout(600000)]
        public IEnumerator 퍼즐_레벨_깊이_1_이_기록된_셀_수를_낸다()
        {
            var builder = new CellGraphBuilder(TestLevels.GapPuzzle(), seed: 0, maxDepth: 1);
            yield return builder.Build();

            Debug.Log(
                $"[맵 깊이 1] 셀 {builder.States.Count:N0} · 간선 {builder.Edges.Count:N0} · " +
                $"이기는 셀 {builder.Wins.Count:N0} · 시뮬 {builder.Sims:N0} " +
                $"(월드 {builder.Worlds:N0}) · 씬 최고 +{builder.PeakScenes} (절대 {builder.PeakTotalScenes}) · " +
                $"{builder.Elapsed.TotalSeconds:F1}초");

            Assert.IsFalse(builder.Stopped, "깊이 1 이 시뮬 예산에 걸릴 리 없다");
            Assert.AreEqual(1, builder.ReachedDepth);
            Assert.GreaterOrEqual(builder.States.Count, Depth1CellsBefore,
                "고치기 전보다 셀이 줄었다 — 후보 설정이나 굴림 방식이 바뀌었다");
            Assert.Greater(builder.Edges.Count, 0, "간선을 하나도 못 모았다");
        }

        /// <summary>
        /// 간선의 양끝이 전부 도달 집합 안에 있어야 한다.
        /// 집합 밖으로 나가는 경로가 없다는 뜻이라, 좁힌 안에서 계산한 h 가
        /// 전체에서 계산한 것과 같아진다 — 근사가 아니다 (4번 3장).
        /// 이게 깨지면 0-1 BFS 가 대표 상태 없는 셀을 만난다.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator 간선의_양끝이_전부_도달_집합_안이다()
        {
            var builder = new CellGraphBuilder(TestLevels.GapPuzzle(), seed: 0, maxDepth: 1);
            yield return builder.Build();

            foreach (CellEdge edge in builder.Edges.Keys)
            {
                Assert.IsTrue(builder.States.ContainsKey(edge.From), $"출발 셀이 밖이다: {edge}");
                Assert.IsTrue(builder.States.ContainsKey(edge.To), $"도착 셀이 밖이다: {edge}");
            }

            foreach (BallCell cell in builder.Wins.Keys)
                Assert.IsTrue(builder.States.ContainsKey(cell), $"이기는 셀이 도달 집합 밖이다: {cell}");
        }
    }
}
