using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// 도구가 열리는 스테이지. 해금 여부만 답하고 표시는
    /// ToolTab, 적용 시점은 ToolbarView 몫이다.
    /// </summary>
    public static class ToolUnlock
    {
        /// 도구를 여는 자리. 지우개가 (0,0) 인 것은 첫
        /// 스테이지 튜토리얼이 지우개 탭을 누르게 시키기
        /// 때문이다 — 잠그면 그 컷에서 멈췄다. 연결핀은
        /// 기획 확정값이 아니라 확인이 필요하다.
        static readonly Dictionary<DrawTool, StageEntry> UnlockAt = new()
        {
            { DrawTool.FixedLine,   new StageEntry(0, 0) },
            { DrawTool.Erase,       new StageEntry(0, 0) },
            { DrawTool.FreeBody,    new StageEntry(0, 10) },
            { DrawTool.PivotSingle, new StageEntry(0, 19) },
            { DrawTool.PivotWorld,  new StageEntry(1, 6) },
        };

        public static StageEntry EntryOf(DrawTool tool) => UnlockAt[tool];

        public static bool IsUnlocked(DrawTool tool, StageEntry at) =>
            at >= UnlockAt[tool];
    }
}
