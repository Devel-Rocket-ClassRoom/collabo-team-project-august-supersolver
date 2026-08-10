using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// LeverSweepTests 가 1800 조합에서 뽑은, 공을 앞으로 가장 멀리
    /// 보낸 열 개. 상승순이 아니라 수평 이동순이다 —
    /// 통로를 따라 보내려면 뒤로 넘기지 않는 쪽이 쓸 조합이다.
    /// 값은 축에서 나온 원값이다. 로그는 소수 둘째 자리에서 잘려
    /// 그대로 옮기면 뷰어가 실측과 다른 것을 보여준다.
    /// </summary>
    public static class ViewerLevers
    {
        static readonly Vector2 WeightSize = new Vector2(1f, 0.8f);

        public readonly struct Sample
        {
            public readonly string Name;
            public readonly Lever Lever;

            public Sample(string name, in Lever lever)
            {
                Name = name;
                Lever = lever;
            }
        }

        /// 수평 이동 큰 순. 이름의 두 숫자가 실측한 상승과 수평 이동이다.
        public static readonly Sample[] Top =
        {
            Make("1  →2.48 ↑1.24", 2.375f, 3.125f, 0f, 7, 10f),
            Make("2  →2.17 ↑1.02", 2.375f, 3.125f, 0f, 5, 10f),
            Make("3  →1.99 ↑2.75", 2.375f, 3.125f, 0f, 7, 8.1f),
            Make("4  →1.88 ↑2.63", 2.375f, 2.25f, 0.4f, 7, 6.2f),
            Make("5  →1.76 ↑0.68", 2.375f, 3.125f, 0f, 3, 10f),
            Make("6  →1.75 ↑2.33", 2.375f, 3.125f, 0f, 5, 8.1f),
            Make("7  →1.67 ↑2.26", 1.125f, 1.375f, 0.4f, 7, 4.3f),
            Make("8  →1.63 ↑1.90", 2.375f, 2.25f, 0.4f, 5, 6.2f),
            Make("9  →1.47 ↑1.75", 1.125f, 1.375f, 0.4f, 5, 4.3f),
            Make("10 →1.36 ↑0.61", 1.75f, 2.25f, 0f, 7, 4.3f),
        };

        static Sample Make(
            string name, float ballArm, float weightArm, float angle, int rows, float drop)
            => new Sample(
                name,
                new Lever(Vector2.zero, ballArm, weightArm, angle, WeightSize, rows, drop));

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
