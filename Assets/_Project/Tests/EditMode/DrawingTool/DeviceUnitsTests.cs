using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// dp 환산과, 사양서가 요구한 "최소 획 길이 > 탭
    /// 임계값 환산치" 확인. 후자는 플레이 영역 크기에
    /// 걸리므로 대표 기기 전체에서 본다.
    /// </summary>
    public class DeviceUnitsTests
    {
        /// 씬의 밴드 합(상 270 + 하 346)과 캔버스 중심
        /// 보정. 1080 폭 기준값이라 기기 폭에 비례한다.
        const float BandTotalAtReference = 616f;
        const float CanvasDriftAtReference = 38f;
        const float ReferenceWidth = 1080f;

        struct Device
        {
            public string Name;
            public float Width;
            public float Height;
            public float Dpi;
            public float InsetTop;
            public float InsetBottom;
        }

        static readonly Device[] Devices =
        {
            new Device { Name = "16:9 전형",   Width = 1080f, Height = 1920f, Dpi = 420f },
            new Device { Name = "20:9 전형",   Width = 1080f, Height = 2400f, Dpi = 395f, InsetTop = 100f, InsetBottom = 50f },
            new Device { Name = "저가형",      Width = 720f,  Height = 1280f, Dpi = 294f },
            new Device { Name = "고밀도",      Width = 1440f, Height = 3200f, Dpi = 560f, InsetTop = 120f, InsetBottom = 60f },
            new Device { Name = "소형 고밀도", Width = 1080f, Height = 2340f, Dpi = 470f, InsetTop = 90f,  InsetBottom = 50f },
        };

        [Test]
        public void 기준_밀도에서는_dp_와_픽셀이_같다()
        {
            Assert.AreEqual(8f, new DeviceUnits(DeviceUnits.BaselineDpi).ToPixels(8f), 1e-4f);
        }

        [Test]
        public void 쓸_수_없는_dpi_는_폴백으로_대체된다()
        {
            float[] broken = { 0f, -1f, float.NaN, float.PositiveInfinity };

            foreach (float dpi in broken)
            {
                Assert.IsFalse(DeviceUnits.IsUsable(dpi), $"{dpi} 를 쓸 수 있다고 판정했다");
                Assert.AreEqual(DeviceUnits.FallbackDpi, new DeviceUnits(dpi).Dpi, 1e-4f);
            }
        }

        [Test]
        public void 픽셀_환산은_밀도에_비례한다()
        {
            // 픽셀 수는 달라져도 물리 길이는 같아야 한다.
            // 8dp 는 어느 기기에서나 0.05인치다.
            Assert.AreEqual(20f, new DeviceUnits(400f).ToPixels(8f), 1e-3f);
            Assert.AreEqual(28f, new DeviceUnits(560f).ToPixels(8f), 1e-3f);
        }

        [Test]
        public void 설계_비율_영역에서는_최소_획_길이가_탭_임계값보다_크다()
        {
            // 사양서가 요구한 확인. 겹치면 "탭이냐 획이냐"
            // 판단이 두 단위에서 따로 갈린다.
            foreach (var device in Devices)
            {
                float tapWorld = TapThresholdInWorld(device, Box(8f, 12f));

                Assert.Less(tapWorld, DrawConstants.MinStrokeLength,
                    $"{device.Name}: 탭 임계값 {tapWorld:F3}wu 가 최소 획 길이를 넘었다");
            }
        }

        [Test]
        public void 플레이_영역이_가로로_길어지면_탭_임계값이_최소_획_길이를_추월한다()
        {
            // 폭이 찰수록 픽셀당 월드 거리가 커져 같은 8dp
            // 가 더 긴 월드 길이가 된다. 레벨 데이터가
            // 지켜야 할 상한이라 경계를 못박아 둔다.
            foreach (var device in Devices)
            {
                float tapWorld = TapThresholdInWorld(device, Landscape(11f));

                Assert.Less(tapWorld, DrawConstants.MinStrokeLength,
                    $"{device.Name}: 폭 11wu 에서 이미 겹쳤다");
            }

            Device worst = Devices[4];
            Assert.Greater(TapThresholdInWorld(worst, Landscape(12f)),
                DrawConstants.MinStrokeLength,
                "폭 12wu 에서도 안 겹치면 상한이 바뀐 것이다");
        }

        static Rect Box(float width, float height) =>
            new Rect(-width * 0.5f, -height * 0.5f, width, height);

        /// 커밋된 레벨과 같은 가로 지향 비율.
        static Rect Landscape(float width) => Box(width, width * 0.6f);

        /// <summary>
        /// 8dp 를 월드 유닛으로 옮긴 값.
        /// dp → 픽셀 → 월드 두 단계를 거친다.
        /// </summary>
        static float TapThresholdInWorld(Device device, Rect playArea)
        {
            var screen = new Vector2(device.Width, device.Height);

            Assert.IsTrue(CanvasFit.TrySolve(CanvasRect(device), screen, playArea, out var frame),
                $"{device.Name}: 카메라를 풀지 못했다");

            float tapPixels = new DeviceUnits(device.Dpi).ToPixels(ScreenConstants.TapThresholdDp);
            return tapPixels / frame.PixelsPerUnit;
        }

        /// <summary>
        /// 씬 레이아웃을 그대로 옮긴 캔버스 rect.
        /// CanvasScaler 가 폭 기준이라 밴드가 폭에 비례한다.
        /// </summary>
        static Rect CanvasRect(Device device)
        {
            float scale = device.Width / ReferenceWidth;

            float safeBottom = device.InsetBottom;
            float safeTop = device.Height - device.InsetTop;

            float height = (safeTop - safeBottom) - BandTotalAtReference * scale;
            float centerY = (safeBottom + safeTop) * 0.5f + CanvasDriftAtReference * scale;

            return new Rect(0f, centerY - height * 0.5f, device.Width, height);
        }
    }
}
