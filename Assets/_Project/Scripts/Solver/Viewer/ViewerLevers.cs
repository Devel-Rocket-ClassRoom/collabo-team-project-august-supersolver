using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 축을 하나씩만 바꿔 늘어놓은 지렛대들.
    /// 축이 셋뿐이라 상위 몇 개를 고르는 대신 격자를 그대로 걸어 둔다 —
    /// 나란히 보면 어느 축이 무엇을 바꾸는지가 표보다 빨리 보인다.
    /// </summary>
    public static class ViewerLevers
    {
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

        public static readonly Sample[] Top = Grid();

        /// <summary>
        /// 판 길이 × 축 자리 × 여유 공간. 이름에 유도된 값도 같이 적는다 —
        /// 상자 높이와 낙차는 고른 값이 아니라 여유에서 풀려 나온 값이다.
        /// </summary>
        static Sample[] Grid()
        {
            float[] fulcrums = { 0.3f, 0.7f };
            int[] rows = { 10, 34 };
            float[] drops = { 2f, 8f };

            var samples = new List<Sample>();

            for (int f = 0; f < fulcrums.Length; f++)
            for (int w = 0; w < rows.Length; w++)
            for (int d = 0; d < drops.Length; d++)
            {
                var lever = new Lever(Vector2.zero, 3f, fulcrums[f], 0.9f, rows[w], drops[d]);
                if (!lever.IsValid) continue;

                samples.Add(new Sample(
                    $"축{fulcrums[f]:F1} {rows[w]}줄 낙차{drops[d]:F0}", lever));
            }

            return samples.ToArray();
        }

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
