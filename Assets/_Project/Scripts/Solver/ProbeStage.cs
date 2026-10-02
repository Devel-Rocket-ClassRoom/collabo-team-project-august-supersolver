using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 도구 하나만 놓고 보는 빈 무대.
    /// 지형도 닿을 수 있는 목표도 없다 — 도구 자체의 성능만
    /// 남기려면 도중에 부딪히거나 판정이 끼어들 것이 없어야 한다.
    /// 실측과 뷰어가 같은 무대를 봐야 눈으로 본 것이 근거가 된다.
    /// </summary>
    public static class ProbeStage
    {
        /// 목표를 이만큼 밀어 둔다. 닿으면 거기서 시뮬이 끝난다.
        const float GoalAway = 10000f;

        /// 낙사 판정을 안 받을 만큼 낮은 바닥.
        const float FarBelow = -10000f;

        public static LevelData Empty(Vector2 ballStart) => new LevelData
        {
            BallStart = ballStart,
            GoalPosition = new Vector2(GoalAway, GoalAway),
            KillY = FarBelow,
            InkLimit = float.MaxValue,
        };
    }
}
