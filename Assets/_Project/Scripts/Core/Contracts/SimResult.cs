namespace PPS.Core
{
    /// <summary>
    /// 시뮬 1회의 결과. 코어가 소유하는 계약이며 솔버·게임·리포트가 공유한다.
    /// </summary>
    public readonly struct SimResult
    {
        public readonly SimOutcome Outcome;

        /// 시뮬이 끝난 스텝. 클리어 시 곧 클리어 시간(스텝) 이다.
        public readonly int EndStep;

        public readonly int Stars;

        public readonly float InkUsed;

        /// <summary>
        /// 시뮬 전체에서 공과 목표 사이의 최소 거리.
        ///
        /// **모든 Outcome 에 대해 채워진다.** 탐색 초반처럼 전부 실패인 구간에서도
        /// "어느 후보가 그나마 나았는지"를 알려주는 유일한 학습 신호이기 때문이다 (설계서 §3.2).
        /// CEM 적합도 함수의 주 재료.
        /// </summary>
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
