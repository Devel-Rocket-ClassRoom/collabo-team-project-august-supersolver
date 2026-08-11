using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 지렛대 하나만 놓고 보는 무대.
    /// 프리셋 표에서 고른 것을 그대로 굴려 보는 자리라
    /// 지형도 닿을 수 있는 목표도 없다.
    /// </summary>
    public static class ViewerLevers
    {
        public static StageData Stage(in Lever lever)
            => new StageData
            {
                StageId = "LeverProbe",
                Seed = 0,
                Level = ProbeStage.Empty(lever.BallSeat),
            };

        public static Solution Build(in Lever lever)
        {
            var solution = new Solution();
            lever.AppendTo(solution);
            return solution;
        }
    }
}
