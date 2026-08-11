using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 지렛대 프리셋을 찾아 파일로 남긴다.
    /// 다섯 축을 훑되 재는 것은 목표 칸이 아니라 발사 상태다 —
    /// 공이 판을 떠난 뒤로는 중력밖에 안 남아서, 그 네 숫자만 있으면
    /// 어디에 닿는지는 시뮬 없이 풀린다.
    /// </summary>
    public class LeverPresetTests
    {
        /// 6초. 판이 휘둘러 공을 놓기까지는 이보다 훨씬 짧다.
        const int Steps = 360;

        /// <summary>
        /// 발사로 볼 최소 속도.
        /// 판에 얹혀 흔들리기만 한 것을 프리셋으로 남기면
        /// 조회 결과가 쓸모없는 것들로 채워진다.
        /// </summary>
        const float LaunchSpeed = 1f;

        static readonly Vector2 Seat = Vector2.zero;

        [Test]
        public void 발사하는_지렛대를_찾아_남긴다()
        {
            List<Lever> levers = Combinations();
            var table = new LeverPresetTable();

            for (int i = 0; i < levers.Count; i++)
            {
                if (!Launch(levers[i], out Vector2 offset, out Vector2 velocity, out int step))
                    continue;

                table.Presets.Add(LeverPreset.From(levers[i], offset, velocity, step));
            }

            Debug.Log($"지렛대 프리셋 — 조합 {levers.Count} 개 중 발사 {table.Presets.Count} 개");
            Debug.Log(Summary(table.Presets));

            LeverPresetFile.Save(table);
            Debug.Log($"프리셋을 저장했다 — {LeverPresetFile.FullPath}");

            Assert.IsNotEmpty(table.Presets,
                $"{levers.Count} 개 조합 중 공을 발사한 것이 없다.");
        }

        [Test]
        public void 예측한_포물선이_실제_궤적과_맞는다()
        {
            // 프리셋 전체가 이 예측 위에 서 있다. 여기가 틀리면
            // 조회는 통과하는데 시뮬은 실패하는 답만 나온다.
            var lever = new Lever(Seat, 3f, 0.3f, 0.9f, 20, 2f);

            Assert.IsTrue(Launch(lever, out Vector2 offset, out Vector2 velocity, out int step),
                "이 지렛대가 공을 발사하지 못했다. 검사할 궤적이 없다.");

            TrajectoryBuffer trace = Run(lever);

            // 발사 뒤 60 스텝을 예측과 견준다. 그 사이 판에 다시 닿으면
            // 갈라지는데, 그때는 예측이 아니라 그 접촉이 문제다.
            for (int ahead = 1; ahead <= 60; ahead++)
            {
                int at = step + ahead - 1;
                if (at >= trace.Count) break;

                Vector2 predicted = Seat + Ballistic.At(offset, velocity, ahead);

                Assert.AreEqual(predicted.x, trace[at].Position.x, 0.02f,
                    $"발사 {ahead} 스텝 뒤 가로가 어긋난다.");
                Assert.AreEqual(predicted.y, trace[at].Position.y, 0.02f,
                    $"발사 {ahead} 스텝 뒤 세로가 어긋난다.");
            }
        }

        [Test]
        public void 좌우를_뒤집으면_발사도_뒤집힌다()
        {
            var lever = new Lever(Seat, 3f, 0.3f, 0.9f, 20, 2f);

            Assert.IsTrue(Launch(lever, out Vector2 offset, out Vector2 velocity, out _));
            Assert.IsTrue(Launch(lever.Mirrored, out Vector2 flipped, out Vector2 back, out _));

            Assert.AreEqual(offset.x, -flipped.x, 1e-2f, "발사 자리가 거울이 아니다.");
            Assert.AreEqual(offset.y, flipped.y, 1e-2f, "발사 높이가 다르다.");
            Assert.AreEqual(velocity.x, -back.x, 1e-2f, "발사 속도가 뒤집히지 않는다.");
        }

        /// <summary>
        /// 다섯 축의 격자. 말이 안 되는 배치는 빼고 담는다 —
        /// 추와 공이 축을 사이에 두고 갈라져야 지렛대가 된다.
        /// </summary>
        static List<Lever> Combinations()
        {
            float[] lengths = { 1f, 2f, 3f, 4f };
            float[] fulcrums = { 0.2f, 0.3f, 0.5f, 0.7f };
            float[] ballAts = { 0.5f, 0.7f, 0.9f, 1f };
            int[] rows = { 4, 10, 20, 34 };
            float[] drops = { 1f, 2f, 4f, 8f };

            var levers = new List<Lever>();

            for (int l = 0; l < lengths.Length; l++)
            for (int f = 0; f < fulcrums.Length; f++)
            for (int b = 0; b < ballAts.Length; b++)
            for (int w = 0; w < rows.Length; w++)
            for (int d = 0; d < drops.Length; d++)
            {
                var lever = new Lever(
                    Seat, lengths[l], fulcrums[f], ballAts[b], rows[w], drops[d]);

                if (lever.IsValid) levers.Add(lever);
            }

            return levers;
        }

        /// <summary>
        /// 공이 판을 떠나는 순간. 세로 속도가 가장 큰 자리다 —
        /// 판에 닿아 있는 동안은 위로 밀려 오르고, 떠난 뒤로는
        /// 중력에 깎이기만 해서 그 꼭대기가 마지막 접촉이다.
        /// </summary>
        static bool Launch(in Lever lever, out Vector2 offset, out Vector2 velocity, out int step)
        {
            TrajectoryBuffer trace = Run(lever);

            offset = Vector2.zero;
            velocity = Vector2.zero;
            step = 0;

            int at = -1;
            float best = LaunchSpeed;

            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Velocity.y <= best) continue;

                best = trace[i].Velocity.y;
                at = i;
            }

            if (at < 0) return false;

            offset = trace[at].Position - lever.BallSeat;
            velocity = trace[at].Velocity;
            step = trace[at].Step;

            return true;
        }

        static TrajectoryBuffer Run(in Lever lever)
        {
            var solution = new Solution();
            lever.AppendTo(solution);

            return MeasureStage.Trace(
                ProbeStage.Empty(lever.BallSeat),
                solution,
                new BallState(lever.BallSeat, Vector2.zero),
                Steps,
                out SimResult _);
        }

        /// <summary>발사 속도가 어떤 범위에 퍼져 있는지.</summary>
        static string Summary(List<LeverPreset> presets)
        {
            var text = new StringBuilder();
            text.AppendLine("발사 속도 범위");

            float leastX = float.PositiveInfinity, mostX = float.NegativeInfinity;
            float leastY = float.PositiveInfinity, mostY = float.NegativeInfinity;
            float leastInk = float.PositiveInfinity, mostInk = float.NegativeInfinity;

            for (int i = 0; i < presets.Count; i++)
            {
                Vector2 v = presets[i].LaunchVelocity;

                leastX = Mathf.Min(leastX, v.x);
                mostX = Mathf.Max(mostX, v.x);
                leastY = Mathf.Min(leastY, v.y);
                mostY = Mathf.Max(mostY, v.y);
                leastInk = Mathf.Min(leastInk, presets[i].Ink);
                mostInk = Mathf.Max(mostInk, presets[i].Ink);
            }

            text.AppendLine($"가로 {leastX:F2} ~ {mostX:F2}");
            text.AppendLine($"세로 {leastY:F2} ~ {mostY:F2}");
            text.AppendLine($"잉크 {leastInk:F2} ~ {mostInk:F2}");

            return text.ToString();
        }
    }
}
