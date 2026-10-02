using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 유일하게 "풀 것이 있는" 픽스처의 계약.
    /// 다른 레벨은 그냥 굴려도 Clear 이거나 목표가 닿을 수 없는 곳에 있어
    /// 솔버를 재는 데 못 쓴다. 이 판이 그 자격을 잃으면
    /// 그 위에서 잰 모든 수치가 무의미해진다.
    /// </summary>
    public class GapPuzzleTests
    {
        [Test]
        public void 아무것도_안_그리면_실패한다()
        {
            var result = SimRunner.Run(TestLevels.GapPuzzle(), null, 0);

            Assert.AreEqual(SimOutcome.Fail, result.Outcome,
                "그냥 굴러서 풀리면 퍼즐이 아니다");
        }

        [Test]
        public void 선_셋을_그으면_클리어한다()
        {
            var result = SimRunner.Run(
                TestLevels.GapPuzzle(), TestLevels.GapPuzzleSolution(), 0);

            Assert.AreEqual(SimOutcome.Clear, result.Outcome,
                "풀 수 없는 판이면 솔버를 재는 기준이 못 된다");
        }

        [Test]
        public void 풀이가_잉크_제한_안에_든다()
        {
            var level = TestLevels.GapPuzzle();
            float ink = TestLevels.GapPuzzleSolution().TotalInk();

            Assert.LessOrEqual(ink, level.InkLimit, $"{ink:F2} / {level.InkLimit:F2}");
            Assert.Greater(ink, 0f, "선이 하나도 없다");
        }
    }
}
