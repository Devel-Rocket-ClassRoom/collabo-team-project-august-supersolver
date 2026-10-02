using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 재는 자리 자체가 맞는가.
    /// 여기가 틀리면 지렛대·망치 실측이 통째로 틀어지므로
    /// 손으로 풀리는 탄도로 먼저 맞춰 둔다.
    /// </summary>
    public class MeasureStageTests
    {
        const int Steps = 60;

        /// 이산 적분끼리 비교하므로 오차는 부동소수점 수준만 남는다.
        const float Tolerance = 1e-3f;

        [Test]
        public void 가만히_둔_공은_이산_자유낙하와_같다()
        {
            MeasureStage.Flight flight = Fly(Vector2.zero);
            Ballistic(Vector2.zero, out Vector2 expected, out _);

            Assert.AreNotEqual(SimOutcome.Clear, flight.Outcome,
                "빈 무대에서 목표에 닿았다 — 목표를 덜 밀어 놓았다.");
            Assert.AreEqual(expected.y, flight.End.y, Tolerance,
                $"떨어진 높이가 다르다. 잰 값 {flight.End.y:F4}, 이산 해 {expected.y:F4}. "
                + "자리를 먼저 옮기고 속도를 더하는 적분이면 여기서 갈린다.");
        }

        [Test]
        public void 수평_속도는_줄지_않는다()
        {
            // 감쇠가 켜져 있으면 여기서 잡힌다.
            var start = new Vector2(3f, 0f);

            MeasureStage.Flight flight = Fly(start);
            Ballistic(start, out Vector2 expected, out _);

            Assert.AreEqual(expected.x, flight.End.x, Tolerance,
                $"수평 이동이 다르다. 잰 값 {flight.End.x:F4}, 이산 해 {expected.x:F4}.");
        }

        [Test]
        public void 위로_쏜_공의_최고점을_집어낸다()
        {
            // 최고점이 구간 안에 오도록 중력 크기만큼 쏜다.
            var start = new Vector2(0f, -Physics2D.gravity.y);

            MeasureStage.Flight flight = Fly(start);
            Ballistic(start, out _, out float expected);

            Assert.AreEqual(expected, flight.Rise, Tolerance,
                $"최고점이 다르다. 잰 값 {flight.Rise:F4}, 이산 해 {expected:F4}.");
        }

        static MeasureStage.Flight Fly(Vector2 velocity)
            => MeasureStage.Fly(
                ProbeStage.Empty(Vector2.zero),
                Solution.Empty,
                new BallState(Vector2.zero, velocity),
                Steps);

        /// <summary>
        /// 원점에서 velocity 로 출발한 공의 이산 궤적.
        /// Box2D 는 속도를 먼저 더하고 자리를 옮긴다 —
        /// 연속 해와는 한 스텝만큼 어긋나서, 실측이 맞춰야 할 쪽은 이 값이다.
        /// </summary>
        static void Ballistic(Vector2 velocity, out Vector2 position, out float rise)
        {
            Vector2 gravity = Physics2D.gravity;
            position = Vector2.zero;

            // 출발점을 후보에 넣지 않는다. 재는 쪽도 첫 스텝부터 본다.
            rise = float.NegativeInfinity;

            for (int i = 0; i < Steps; i++)
            {
                velocity += gravity * SimWorld.FixedDt;
                position += velocity * SimWorld.FixedDt;

                if (position.y > rise) rise = position.y;
            }
        }
    }
}
