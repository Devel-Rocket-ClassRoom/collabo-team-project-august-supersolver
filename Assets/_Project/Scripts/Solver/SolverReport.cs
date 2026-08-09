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

        /// 실제로 돌린 시뮬 횟수. 맵의 값어치를 재는 기준선이다.
        public int Sims;

        /// 시뮬 전에 걸러진 후보 수. 잉크·영역에 걸린 것들이다.
        public int Rejected;

        /// 팝해서 굴린 노드 수. 캐시가 먹으면 Sims 와 같아진다.
        public int Nodes;

        /// 열린 목록의 최고점. 노드 메모리가 여기에 비례한다.
        public int OpenPeak;

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
               $"시뮬 {Sims:N0} (거부 {Rejected:N0}) · 노드 {Nodes:N0} · " +
               $"열린목록 최고 {OpenPeak:N0} · 최대 깊이 {Deepest} · " +
               $"최소 목표 거리 {MinGoalDist:F3} · {Elapsed.TotalSeconds:F1}초";
    }
}
