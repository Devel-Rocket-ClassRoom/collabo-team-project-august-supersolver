using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 시행 1회의 결과. 시뮬 결과 + 거부 사유.
    /// 거부도 결과라 예외를 던지지 않는다 —
    /// 탐색은 수만 번 실패를 던지므로 루프가 끊기면 안 된다.
    /// 코어는 솔버를 모르므로 사유는 여기서 얹는다.
    /// </summary>
    public readonly struct TrialResult
    {
        /// 거부된 시행에서는 시뮬이 돌지 않은 자리표시다.
        public readonly SimResult Sim;

        /// None 이면 시뮬이 실제로 돌았다.
        public readonly PlacementReject Reject;

        /// <summary>시뮬이 돈 시행.</summary>
        public TrialResult(in SimResult sim)
        {
            Sim = sim;
            Reject = PlacementReject.None;
        }

        /// <summary>
        /// 시뮬 없이 거부된 시행.
        /// MinGoalDist 는 무한대다 — 실제로 잰 거리가
        /// 아니므로 학습 신호로 섞이면 안 된다.
        /// </summary>
        public TrialResult(PlacementReject reject)
        {
            Sim = new SimResult(SimOutcome.Fail, 0, 0, 0f, float.PositiveInfinity);
            Reject = reject;
        }

        public bool Rejected => Reject != PlacementReject.None;

        public bool Cleared => !Rejected && Sim.Cleared;

        public override string ToString()
            => Rejected ? $"Rejected({Reject})" : Sim.ToString();
    }
}
