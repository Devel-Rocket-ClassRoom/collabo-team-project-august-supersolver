using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 셀 → 대표 상태 되돌리기 검증.
    /// 왕복이 깨지면 간선의 출발 셀이 엉뚱한 곳에 찍혀
    /// 그래프가 조용히 어긋난다 — 어디서도 예외가 안 난다.
    /// </summary>
    public class BallDequantizeTests
    {
        /// 실제 폭(공 지름)에 가깝게 잡았다.
        const float PosStep = 0.5f;

        static BallQuantizer Quantizer() => new BallQuantizer(PosStep);

        /// <summary>
        /// 유효한 속도 라벨 전부. 방향 8 × 크기 구간 + 정지 1.
        /// 라벨은 속도 성분이 아니라 부호 × 구간이라
        /// 아무 정수쌍이나 셀이 되는 것이 아니다.
        /// </summary>
        static IEnumerable<Vector2Int> VelocityLabels()
        {
            yield return Vector2Int.zero;

            for (int band = 1; band <= SolverConfig.SpeedBands; band++)
            {
                yield return new Vector2Int(band, 0);
                yield return new Vector2Int(-band, 0);
                yield return new Vector2Int(0, band);
                yield return new Vector2Int(0, -band);
                yield return new Vector2Int(band, band);
                yield return new Vector2Int(band, -band);
                yield return new Vector2Int(-band, band);
                yield return new Vector2Int(-band, -band);
            }
        }

        [Test]
        public void 모든_셀에서_왕복이_성립한다()
        {
            var q = Quantizer();

            foreach (var label in VelocityLabels())
            {
                for (int x = -3; x <= 3; x++)
                {
                    for (int y = -3; y <= 3; y++)
                    {
                        var cell = new BallCell(x, y, label.x, label.y);
                        BallState state = q.Dequantize(cell);

                        Assert.AreEqual(cell, q.Quantize(state.Position, state.Velocity),
                            $"{cell} → {state} → 다른 셀");
                    }
                }
            }
        }

        [Test]
        public void 정지_셀의_대표_속도가_영벡터다()
        {
            BallState state = Quantizer().Dequantize(new BallCell(2, -1, 0, 0));

            Assert.AreEqual(Vector2.zero, state.Velocity);
        }

        /// <summary>
        /// 전부 0 으로 두면 속도가 0 이 아닌 셀에서
        /// 나가는 간선이 안 생겨 h 가 전부 ∞ 가 된다.
        /// </summary>
        [Test]
        public void 움직이는_셀의_대표_속도가_영벡터가_아니다()
        {
            var q = Quantizer();

            foreach (var label in VelocityLabels())
            {
                if (label == Vector2Int.zero) continue;

                BallState state = q.Dequantize(new BallCell(0, 0, label.x, label.y));

                Assert.Greater(state.Velocity.magnitude, SolverConfig.StopSpeed,
                    $"라벨 ({label.x},{label.y}) 의 대표 속도가 정지로 접힌다");
            }
        }

        [Test]
        public void 대표_속력이_그_구간_안에_든다()
        {
            var q = Quantizer();

            foreach (var label in VelocityLabels())
            {
                int band = Mathf.Max(Mathf.Abs(label.x), Mathf.Abs(label.y));
                if (band == 0) continue;

                float speed = q.Dequantize(new BallCell(0, 0, label.x, label.y)).Velocity.magnitude;

                Assert.GreaterOrEqual(speed, SolverConfig.BandFloor(band) - 1e-4f,
                    $"구간 {band} 의 하한 아래다");

                // 최상단은 위가 열려 있어 상한을 따지지 않는다.
                if (band < SolverConfig.SpeedBands)
                    Assert.Less(speed, SolverConfig.BandFloor(band + 1), $"구간 {band} 의 상한 위다");
            }
        }

        [Test]
        public void 위치가_셀의_중심이다()
        {
            BallState state = Quantizer().Dequantize(new BallCell(2, -3, 0, 0));

            // (2 + 0.5) × 0.5, (-3 + 0.5) × 0.5
            Assert.AreEqual(1.25f, state.Position.x, 1e-5f);
            Assert.AreEqual(-1.25f, state.Position.y, 1e-5f);
        }
    }
}
