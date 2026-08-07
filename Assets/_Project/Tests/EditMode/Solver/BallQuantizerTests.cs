using NUnit.Framework;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 셀 키의 최소 보장.
    /// 결정성·경계 안정성·위치와 속도의 분리를 못박는다.
    /// </summary>
    public class BallQuantizerTests
    {
        /// 산수가 눈에 보이라고 고른 값이다.
        /// 실제 폭은 실측 후 결정하므로 기준이 아니다.
        const float PosStep = 0.25f;

        /// 스냅 폭(격자 1e-4)보다 작은 흔들림.
        const float Eps = 0.000001f;

        static BallQuantizer Quantizer() => new BallQuantizer(PosStep);

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

        /// 크기 구간의 경계 속력. 정지 경계도 포함한다.
        [TestCase(0.5f)]
        [TestCase(1.5f)]
        [TestCase(4.5f)]
        [TestCase(13.5f)]
        public void SpeedBoundaryStaysInOneCell(float line)
        {
            var q = Quantizer();
            var position = Vector2.zero;

            BallCell exact = q.Quantize(position, new Vector2(line, 0f));
            BallCell below = q.Quantize(position, new Vector2(line - Eps, 0f));
            BallCell above = q.Quantize(position, new Vector2(line + Eps, 0f));

            Assert.AreEqual(exact, below, "경계 아래");
            Assert.AreEqual(exact, above, "경계 위");
        }

        /// <summary>
        /// 멈춘 공은 방향이 의미가 없다.
        /// 8칸으로 갈리면 같은 상태가 여러 키를 갖는다.
        /// </summary>
        [TestCase(0f, 0f)]
        [TestCase(0.1f, 0.2f)]
        [TestCase(-0.3f, 0.1f)]
        public void StoppedBallHasNoDirection(float vx, float vy)
        {
            BallCell cell = Quantizer().Quantize(Vector2.zero, new Vector2(vx, vy));

            Assert.AreEqual(0, cell.VX);
            Assert.AreEqual(0, cell.VY);
        }

        /// 속력 5 는 전부 크기 3 구간이라 방향만 갈린다.
        [TestCase(5f, 0f, 3, 0)]
        [TestCase(-5f, 0f, -3, 0)]
        [TestCase(0f, 5f, 0, 3)]
        [TestCase(0f, -5f, 0, -3)]
        [TestCase(5f, 5f, 3, 3)]
        [TestCase(-5f, 5f, -3, 3)]
        public void HeadingFoldsIntoEightDirections(float vx, float vy, int ex, int ey)
        {
            BallCell cell = Quantizer().Quantize(Vector2.zero, new Vector2(vx, vy));

            Assert.AreEqual(ex, cell.VX);
            Assert.AreEqual(ey, cell.VY);
        }

        /// <summary>
        /// 수평·수직은 구간 한가운데라 조금 기울어도 안 갈린다.
        /// 평지를 구르는 공과 곧장 떨어지는 공이 가장 흔하다.
        /// </summary>
        [Test]
        public void FlatAndVerticalHeadingsSitAtSectorCenter()
        {
            var q = Quantizer();

            Assert.AreEqual(
                q.Quantize(Vector2.zero, new Vector2(5f, 0f)),
                q.Quantize(Vector2.zero, new Vector2(5f, 0.5f)));

            Assert.AreEqual(
                q.Quantize(Vector2.zero, new Vector2(0f, -5f)),
                q.Quantize(Vector2.zero, new Vector2(0.5f, -5f)));
        }

        /// 상한 위는 전부 최상단으로 접는다.
        [Test]
        public void SpeedClampsAtTopBand()
        {
            var q = Quantizer();

            Assert.AreEqual(4, q.Quantize(Vector2.zero, new Vector2(100f, 0f)).VX);
            Assert.AreEqual(4, q.Quantize(Vector2.zero, new Vector2(1000f, 0f)).VX);
        }

        [Test]
        public void SameCellForNearbyStates()
        {
            var q = Quantizer();
            Assert.AreEqual(
                q.Quantize(new Vector2(0.76f, 0.76f), new Vector2(3.1f, 3.1f)),
                q.Quantize(new Vector2(0.99f, 0.99f), new Vector2(2.5f, 2.5f)));
        }

        [Test]
        public void DifferentCellAcrossGridLine()
        {
            var q = Quantizer();
            Assert.AreNotEqual(
                q.Quantize(new Vector2(0.74f, 0f), Vector2.zero),
                q.Quantize(new Vector2(0.76f, 0f), Vector2.zero));
        }

        /// 위치는 갈라지고 속도는 안 갈라지는 해상도 차이.
        [Test]
        public void PositionAndVelocityUseSeparateResolutions()
        {
            var q = Quantizer();
            BallCell a = q.Quantize(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            BallCell b = q.Quantize(new Vector2(0.9f, 0f), new Vector2(0.9f, 0f));

            Assert.AreNotEqual(a.X, b.X, "위치는 폭 0.25 라 갈라진다");
            Assert.AreEqual(a.VX, b.VX, "속도는 같은 크기 구간이라 안 갈라진다");
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
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BallQuantizer(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BallQuantizer(-1f));
        }
    }
}
