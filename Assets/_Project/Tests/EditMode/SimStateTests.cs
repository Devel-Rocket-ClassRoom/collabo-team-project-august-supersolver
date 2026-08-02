using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 상태 머신은 2상태뿐이다 — **편집 중(월드 없음) ↔ 시뮬 중(월드 있음)**.
    ///
    /// 편집 중에 물리가 진행되는 경로가 없다는 것을 지키는 실제 장치는
    /// <see cref="SimAccumulator.Advance"/> 의 첫 줄 하나뿐이다. 드라이버는 매 프레임
    /// 누적기를 부르고, 편집 중에는 월드가 null 로 들어간다.
    ///
    /// 월드를 만들지 않으므로 EditMode 다.
    /// </summary>
    public class SimStateTests
    {
        [Test]
        public void 월드가_없으면_누적기는_아무것도_하지_않는다()
        {
            var accumulator = new SimAccumulator();

            // 드라이버는 편집 중에도 매 프레임 이걸 부른다. 예외가 나면 게임이 멈춘다.
            Assert.AreEqual(0, accumulator.Advance(null, 1f / 60f));
            Assert.AreEqual(0, accumulator.Advance(null, 1f));
        }

        [Test]
        public void 배속이나_일시정지_상태에서도_월드가_없으면_안전하다()
        {
            var accumulator = new SimAccumulator { SpeedMultiplier = 8f, Paused = false };
            Assert.AreEqual(0, accumulator.Advance(null, 1f / 60f));

            accumulator.Paused = true;
            Assert.AreEqual(0, accumulator.Advance(null, 1f / 60f));
        }
    }
}
