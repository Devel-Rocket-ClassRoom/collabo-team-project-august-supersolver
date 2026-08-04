namespace PPS.Core
{
    /// <summary>시뮬 1회의 결과.</summary>
    public readonly struct SimResult
    {
        public readonly SimOutcome Outcome;

        /// 시뮬이 끝난 스텝.
        public readonly int EndStep;

        public readonly int Stars;

        public readonly float InkUsed;

        /// 공과 목표의 최소 거리.
        /// 모든 Outcome 에서 채워진다.
        /// 실패한 시도의 유일한 학습 신호.
        public readonly float MinGoalDist;

        public SimResult(SimOutcome outcome, int endStep, int stars, float inkUsed, float minGoalDist)
        {
            Outcome = outcome;
            EndStep = endStep;
            Stars = stars;
            InkUsed = inkUsed;
            MinGoalDist = minGoalDist;
        }

        public bool Cleared => Outcome == SimOutcome.Clear;

        public override string ToString()
            => $"{Outcome} @step {EndStep} (stars {Stars}, ink {InkUsed:F2}, minGoalDist {MinGoalDist:F3})";
    }
}
