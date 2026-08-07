using NUnit.Framework;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 셀 키의 최소 보장.
    /// 결정성·경계 안정성·폭 분리를 못박는다.
    /// </summary>
    public class BallQuantizerTests
    {
        /// 산수가 눈에 보이라고 고른 값이다.
        /// 실제 폭은 실측 후 결정하므로 기준이 아니다.
        const float PosStep = 0.25f;
        const float VelStep = 1f;

        /// 스냅 폭(격자 1e-4)보다 작은 흔들림.
        const float Eps = 0.000001f;

        static BallQuantizer Quantizer() => new BallQuantizer(PosStep, VelStep);

        [Test]
        public void SameStateGivesSameCellEveryTime()
        {
            var q = Quantizer();
            var position = new Vector2(1.37f, -2.61f);
            var velocity = new Vector2(3.4f, -0.7f);

            BallCell first = q.Quantize(position, velocity);
            for (int i = 0; i < 100; i++)
            {
                BallCell again = q.Quantize(position, velocity);
                Assert.AreEqual(first, again, $"{i} 회차");
                Assert.AreEqual(first.GetHashCode(), again.GetHashCode(), $"{i} 회차 해시");
            }
        }

        /// 격자선 위 좌표는 ±ε 로 흔들려도 한 셀에 머문다.
        [TestCase(0f)]
        [TestCase(0.75f)]
        [TestCase(-0.75f)]
        [TestCase(2.5f)]
        public void PositionBoundaryStaysInOneCell(float line)
        {
            var q = Quantizer();
            var velocity = Vector2.zero;

            BallCell exact = q.Quantize(new Vector2(line, line), velocity);
            BallCell below = q.Quantize(new Vector2(line - Eps, line - Eps), velocity);
            BallCell above = q.Quantize(new Vector2(line + Eps, line + Eps), velocity);

            Assert.AreEqual(exact, below, "경계 아래");
            Assert.AreEqual(exact, above, "경계 위");
        }

        [TestCase(0f)]
        [TestCase(3f)]
        [TestCase(-3f)]
        public void VelocityBoundaryStaysInOneCell(float line)
        {
            var q = Quantizer();
            var position = Vector2.zero;

            BallCell exact = q.Quantize(position, new Vector2(line, line));
            BallCell below = q.Quantize(position, new Vector2(line - Eps, line - Eps));
            BallCell above = q.Quantize(position, new Vector2(line + Eps, line + Eps));

            Assert.AreEqual(exact, below, "경계 아래");
            Assert.AreEqual(exact, above, "경계 위");
        }

        [Test]
        public void SameCellForNearbyStates()
        {
            var q = Quantizer();
            Assert.AreEqual(
                q.Quantize(new Vector2(0.76f, 0.76f), new Vector2(3.1f, 3.1f)),
                q.Quantize(new Vector2(0.99f, 0.99f), new Vector2(3.9f, 3.9f)));
        }

        [Test]
        public void DifferentCellAcrossGridLine()
        {
            var q = Quantizer();
            Assert.AreNotEqual(
                q.Quantize(new Vector2(0.74f, 0f), Vector2.zero),
                q.Quantize(new Vector2(0.76f, 0f), Vector2.zero));
        }

        /// 위치는 갈라지고 속도는 안 갈라지는 폭 차이.
        [Test]
        public void PositionAndVelocityUseSeparateSteps()
        {
            var q = Quantizer();
            BallCell a = q.Quantize(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            BallCell b = q.Quantize(new Vector2(0.9f, 0f), new Vector2(0.9f, 0f));

            Assert.AreNotEqual(a.X, b.X, "위치는 폭 0.25 라 갈라진다");
            Assert.AreEqual(a.VX, b.VX, "속도는 폭 1 이라 같다");
        }

        [Test]
        public void NegativeValuesFloorDownward()
        {
            var q = Quantizer();
            BallCell cell = q.Quantize(new Vector2(-0.1f, -0.6f), Vector2.zero);

            Assert.AreEqual(-1, cell.X);
            Assert.AreEqual(-3, cell.Y);
        }

        [Test]
        public void RejectsNonPositiveStep()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BallQuantizer(0f, VelStep));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BallQuantizer(PosStep, -1f));
        }
    }
}
