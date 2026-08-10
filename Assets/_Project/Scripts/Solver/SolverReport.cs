using System;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 탐색 1회의 결과.
    /// 실패도 결과다 — 왜 멈췄는지와 얼마나 가까웠는지가 없으면
    /// 레벨을 고칠 수도, 예산을 정할 수도 없다.
    /// </summary>
    public sealed class SolverReport
    {
        public SolverStop Stop;

        /// <summary>
        /// 찾은 풀이. Solved 가 아니면 null 이다.
        /// 레벨 시작점에서 전체 배치로 굴려 Clear 를 받은 것이라
        /// 그대로 재실행해도 Clear 다.
        /// </summary>
        public Solution Solution;

        /// 실제로 돌린 시뮬 횟수. 곧 굴려 본 배치의 수다.
        public int Sims;

        /// <summary>
        /// 모든 시뮬의 스텝 합. 시뮬 단가가 왜 변했는지는 이걸 봐야 안다 —
        /// 순서가 바뀌면 굴리는 판의 길이가 통째로 달라진다.
        /// </summary>
        public long Steps;

        /// 우리가 얹은 시뮬 씬의 최고점과, 그때의 절대 개수.
        /// 절대 개수가 생성·해제 비용을 결정한다.
        public int PeakScenes;

        public int PeakTotalScenes;

        /// 시뮬 전에 걸러진 후보 수. 잉크·영역에 걸린 것들이다.
        public int Rejected;

        /// 굴려 본 것 중 가장 깊은 배치의 선 개수.
        public int Deepest;

        /// <summary>
        /// 모든 시뮬을 통틀어 공이 목표에 가장 가까웠던 거리.
        /// 실패한 탐색의 유일한 신호다 — 아깝게 놓친 것과
        /// 근처도 못 간 것을 여기서 가른다.
        /// </summary>
        public float MinGoalDist = float.PositiveInfinity;

        public TimeSpan Elapsed;

        public bool Solved => Stop == SolverStop.Solved;

        public override string ToString()
            => $"{Stop}{(Solution == null ? "" : $" · 선 {Solution.Strokes.Count}")} · " +
               $"시뮬 {Sims:N0} (거부 {Rejected:N0}) · 최대 깊이 {Deepest} · " +
               $"최소 목표 거리 {MinGoalDist:F3} · {Elapsed.TotalSeconds:F1}초\n" +
               $"  시뮬당 {(Sims == 0 ? 0 : Elapsed.TotalMilliseconds / Sims):F2}ms · " +
               $"평균 {(Sims == 0 ? 0 : (double)Steps / Sims):F0} 스텝 · " +
               $"씬 최고 +{PeakScenes} (절대 {PeakTotalScenes})";
    }
}
