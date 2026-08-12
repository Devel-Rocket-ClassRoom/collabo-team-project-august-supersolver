using PPS.Core;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 툴바의 4상태. 프론트엔드 전용이다 —
    /// 핀은 Stroke 가 아니라 PivotJoint 라서
    /// ToolType 에 넣으면 ColliderFactory·WorldBuilder 가
    /// 처리 못 하는 값이 Core 계약에 생긴다.
    /// </summary>
    public enum DrawTool
    {
        FixedLine = 0,
        FreeBody = 1,

        /// 반경 안의 최근 두 획을 잇는 핀.
        PivotSingle = 2,

        /// 반경 안의 최근 한 획을 월드에 고정하는 핀.
        PivotWorld = 3,
    }

    public static class DrawToolExtensions
    {
        public static bool IsPivot(this DrawTool tool) =>
            tool == DrawTool.PivotSingle || tool == DrawTool.PivotWorld;

        /// <summary>획을 만드는 두 도구만 계약 타입을 갖는다.</summary>
        public static ToolType ToToolType(this DrawTool tool) =>
            tool == DrawTool.FreeBody ? ToolType.FreeBody : ToolType.FixedLine;
    }
}
