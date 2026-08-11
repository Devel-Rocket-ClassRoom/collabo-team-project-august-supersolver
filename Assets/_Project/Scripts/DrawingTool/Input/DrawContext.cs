using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 획 하나를 만드는 데 필요한 바깥 값.
    /// 인식기가 Down 에서 캡처해 끝까지 쓴다 — 도중에
    /// fit 이 바뀌면 한 획 안에서 좌표계가 갈라진다.
    /// </summary>
    public readonly struct DrawContext
    {
        public readonly DrawTool Tool;

        /// 획 시작 시점의 잔량.
        /// 진실은 확정 후의 재계산이다.
        public readonly float RemainingInk;

        /// 화면 픽셀 ↔ 월드 환산 계수.
        public readonly float PixelsPerUnit;

        public readonly float Dpi;

        /// 손가락 가림 대책. 마우스는 0 이라 분기가 없다.
        public readonly float OffsetDp;

        /// 점이 갇히는 영역. 캔버스가 아니다 —
        /// 손가락이 나가도 점은 이 경계에 붙는다.
        public readonly Rect PlayArea;

        public DrawContext(
            DrawTool tool,
            float remainingInk,
            float pixelsPerUnit,
            float dpi,
            float offsetDp,
            Rect playArea)
        {
            Tool = tool;
            RemainingInk = remainingInk;
            PixelsPerUnit = pixelsPerUnit;
            Dpi = dpi;
            OffsetDp = offsetDp;
            PlayArea = playArea;
        }

        /// <summary>
        /// dp 를 월드 길이로. 뷰포트가 회전 없는 직교라
        /// 픽셀 거리와 월드 거리가 상수배다.
        /// </summary>
        public float ToWorld(float dp) => new DeviceUnits(Dpi).ToPixels(dp) / PixelsPerUnit;
    }
}
