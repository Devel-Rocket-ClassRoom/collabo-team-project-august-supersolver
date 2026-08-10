using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 프리미티브가 공을 얼마나 띄우고 보내는지 재는 자.
    /// 무대는 ProbeStage 가 낸다.
    /// </summary>
    public static class MeasureStage
    {
        /// <summary>
        /// 한 번 굴려 보고 잰 값.
        /// 높이와 거리를 따로 두는 것은 지렛대가 높이를,
        /// 망치가 거리를 버는 도구라서다.
        /// </summary>
        public readonly struct Flight
        {
            public readonly SimOutcome Outcome;

            public readonly int EndStep;

            /// 출발점에서 최고점까지의 높이.
            public readonly float Rise;

            /// 최고점에 왔을 때까지의 수평 이동. 부호가 방향이다.
            public readonly float RunAtApex;

            /// 마지막으로 본 자리.
            public readonly Vector2 End;

            public Flight(SimOutcome outcome, int endStep, float rise, float runAtApex, Vector2 end)
            {
                Outcome = outcome;
                EndStep = endStep;
                Rise = rise;
                RunAtApex = runAtApex;
                End = end;
            }

            public override string ToString()
                => $"{Outcome} @step {EndStep} (rise {Rise:F4}, run {RunAtApex:F4}, end {End})";
        }

        /// <summary>
        /// 한 판 굴리고 최고점을 집어낸다.
        /// 매 스텝을 담는다 — 실측이라 최고점을 건너뛰면 안 된다.
        /// </summary>
        public static Flight Fly(
            LevelData level,
            Solution solution,
            BallState start,
            int maxSteps = SimWorld.DefaultMaxSteps)
        {
            var buffer = new TrajectoryBuffer(1, maxSteps);
            SimResult result = SimRunner.RunSampled(
                level, solution, 0, buffer, maxSteps, start);

            if (buffer.Count == 0)
                return new Flight(result.Outcome, result.EndStep, 0f, 0f, start.Position);

            int apex = 0;
            for (int i = 1; i < buffer.Count; i++)
                if (buffer[i].Position.y > buffer[apex].Position.y) apex = i;

            return new Flight(
                result.Outcome,
                result.EndStep,
                buffer[apex].Position.y - start.Position.y,
                buffer[apex].Position.x - start.Position.x,
                buffer[buffer.Count - 1].Position);
        }
    }
}
