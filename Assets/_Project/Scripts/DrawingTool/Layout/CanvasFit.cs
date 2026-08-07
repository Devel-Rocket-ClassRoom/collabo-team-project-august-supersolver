using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 카메라 한 대의 확정된 값.
    /// </summary>
    public readonly struct CameraFrame
    {
        public readonly float OrthographicSize;
        public readonly Vector2 Position;

        /// 화면 픽셀 ↔ 월드 유닛 환산 계수.
        /// dp 를 월드로 옮기려면 이걸 거쳐야 한다.
        public readonly float PixelsPerUnit;

        public CameraFrame(float orthographicSize, Vector2 position, float pixelsPerUnit)
        {
            OrthographicSize = orthographicSize;
            Position = position;
            PixelsPerUnit = pixelsPerUnit;
        }
    }

    /// <summary>
    /// 플레이 영역이 캔버스 rect 안에 다 들어오는
    /// 카메라 값을 낸다. 씬도 카메라도 모른다.
    /// </summary>
    public static class CanvasFit
    {
        /// <returns>레이아웃이 아직 안 잡혔으면 false.</returns>
        public static bool TrySolve(
            Rect canvasPixels, Vector2 screenPixels, Rect playArea, out CameraFrame frame)
        {
            frame = default;

            if (canvasPixels.width <= 0f || canvasPixels.height <= 0f) return false;
            if (playArea.width <= 0f || playArea.height <= 0f) return false;
            if (screenPixels.y <= 0f) return false;

            float pixelsPerUnit = Mathf.Min(
                canvasPixels.width / playArea.width,
                canvasPixels.height / playArea.height);

            // 뷰포트는 화면 전체다. 캔버스가 아니라 화면
            // 높이의 절반을 월드로 옮긴 값이 orthoSize 다.
            float orthographicSize = screenPixels.y * 0.5f / pixelsPerUnit;

            // 캔버스 중심은 화면 중심이 아니다 — 상/하단
            // 밴드가 비대칭이다. 크기만 맞추고 위치를 안
            // 옮기면 영역이 밴드 쪽으로 밀린다.
            Vector2 drift = canvasPixels.center - screenPixels * 0.5f;

            frame = new CameraFrame(
                orthographicSize,
                playArea.center - drift / pixelsPerUnit,
                pixelsPerUnit);
            return true;
        }
    }
}
