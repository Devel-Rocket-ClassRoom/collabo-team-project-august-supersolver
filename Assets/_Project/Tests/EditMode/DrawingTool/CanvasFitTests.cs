using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 플레이 영역이 캔버스 rect 안에 다 들어오고,
    /// 밴드 비대칭만큼 카메라가 따라 움직이는지 본다.
    /// </summary>
    public class CanvasFitTests
    {
        /// 1080×1920 · 인셋 없음. 씬의 밴드 값(상 270 ·
        /// 하 346)에서 나온 캔버스 rect 다.
        static readonly Rect Canvas1080x1920 = new Rect(0f, 346f, 1080f, 1304f);
        static readonly Vector2 Screen1080x1920 = new Vector2(1080f, 1920f);

        [Test]
        public void 플레이_영역이_캔버스_안에_다_들어온다()
        {
            AssertFitsInside(Canvas1080x1920, Screen1080x1920, Box(8f, 12f));
            AssertFitsInside(Canvas1080x1920, Screen1080x1920, Box(14.05f, 8.63f));
            AssertFitsInside(new Rect(0f, 396f, 1080f, 1634f),
                new Vector2(1080f, 2400f), Box(8f, 12f));
        }

        [Test]
        public void 플레이_영역_중심이_캔버스_중심에_온다()
        {
            Assert.IsTrue(CanvasFit.TrySolve(
                Canvas1080x1920, Screen1080x1920, Box(8f, 12f), out var frame));

            Vector2 center = WorldToScreen(frame, Screen1080x1920, Vector2.zero);

            Assert.AreEqual(Canvas1080x1920.center.x, center.x, 0.01f);
            Assert.AreEqual(Canvas1080x1920.center.y, center.y, 0.01f);
        }

        [Test]
        public void 캔버스가_위로_치우친_만큼_카메라가_아래로_내려간다()
        {
            // 씬의 캔버스는 하단 밴드가 더 두꺼워 화면
            // 중심보다 위에 있다. 크기만 맞추고 위치를
            // 안 옮기면 영역이 밴드 쪽으로 밀린다.
            Assert.Greater(Canvas1080x1920.center.y, Screen1080x1920.y * 0.5f);

            Assert.IsTrue(CanvasFit.TrySolve(
                Canvas1080x1920, Screen1080x1920, Box(8f, 12f), out var frame));

            Assert.Less(frame.Position.y, 0f);
        }

        [Test]
        public void 제약_축은_영역과_캔버스의_종횡비가_정한다()
        {
            // 세로로 긴 영역 → 높이가 먼저 찬다.
            AssertPixelsPerUnit(Canvas1080x1920, Screen1080x1920, Box(8f, 12f), 1304f / 12f);

            // 인셋이 커져 캔버스가 세로로 늘면 폭으로 넘어간다.
            AssertPixelsPerUnit(new Rect(0f, 396f, 1080f, 1634f),
                new Vector2(1080f, 2400f), Box(8f, 12f), 1080f / 8f);

            // 가로로 긴 영역은 어느 기기에서도 폭이 먼저 찬다.
            AssertPixelsPerUnit(Canvas1080x1920, Screen1080x1920,
                Box(14.05f, 8.63f), 1080f / 14.05f);
        }

        [Test]
        public void 레이아웃이_안_잡힌_프레임에서는_풀지_않는다()
        {
            Assert.IsFalse(CanvasFit.TrySolve(
                new Rect(0f, 0f, 0f, 0f), Screen1080x1920, Box(8f, 12f), out _));

            Assert.IsFalse(CanvasFit.TrySolve(
                Canvas1080x1920, Screen1080x1920, new Rect(0f, 0f, 0f, 0f), out _));

            Assert.IsFalse(CanvasFit.TrySolve(
                Canvas1080x1920, Vector2.zero, Box(8f, 12f), out _));
        }

        static Rect Box(float width, float height) =>
            new Rect(-width * 0.5f, -height * 0.5f, width, height);

        /// <summary>
        /// 카메라 매핑의 역. 뷰포트가 화면 전체라
        /// 화면 중심이 카메라 위치에 대응한다.
        /// </summary>
        static Vector2 WorldToScreen(CameraFrame frame, Vector2 screenPixels, Vector2 world) =>
            screenPixels * 0.5f + (world - frame.Position) * frame.PixelsPerUnit;

        static void AssertFitsInside(Rect canvas, Vector2 screen, Rect playArea)
        {
            Assert.IsTrue(CanvasFit.TrySolve(canvas, screen, playArea, out var frame));

            Vector2 min = WorldToScreen(frame, screen, playArea.min);
            Vector2 max = WorldToScreen(frame, screen, playArea.max);

            // fit-inside 라 제약 축은 경계에 정확히 닿는다.
            const float tolerance = 0.01f;
            Assert.GreaterOrEqual(min.x, canvas.xMin - tolerance, "왼쪽이 캔버스를 넘었다");
            Assert.GreaterOrEqual(min.y, canvas.yMin - tolerance, "아래가 캔버스를 넘었다");
            Assert.LessOrEqual(max.x, canvas.xMax + tolerance, "오른쪽이 캔버스를 넘었다");
            Assert.LessOrEqual(max.y, canvas.yMax + tolerance, "위가 캔버스를 넘었다");
        }

        static void AssertPixelsPerUnit(Rect canvas, Vector2 screen, Rect playArea, float expected)
        {
            Assert.IsTrue(CanvasFit.TrySolve(canvas, screen, playArea, out var frame));
            Assert.AreEqual(expected, frame.PixelsPerUnit, 0.01f);
        }
    }
}
