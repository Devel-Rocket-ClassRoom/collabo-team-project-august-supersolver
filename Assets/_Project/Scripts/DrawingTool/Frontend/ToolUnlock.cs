using System.Collections.Generic;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 도구가 열리는 스테이지. 해금 여부만 답하고 표시는
    /// ToolTab, 적용 시점은 ToolbarView 몫이다. 표가 Core 가
    /// 아니라 여기 있는 것은 DrawTool 이 프론트엔드 전용이라
    /// Core 에서 보이지 않기 때문이다.
    /// </summary>
    public static class ToolUnlock
    {
        /// 도구를 여는 스테이지. 테마를 가로지르는 1-기반
        /// 번호다 — 2챕터 7스테이지가 27 이다. 고정선·자유물체·
        /// 월드핀만 기획 확정값이고 연결핀은 우리가 채운
        /// 값이라 확인이 필요하다.
        /// 지우개가 1 인 것은 1스테이지 튜토리얼이 지우개 탭을
        /// 누르게 시키기 때문이다 — 잠그면 그 컷에서 멈췄다.
        static readonly Dictionary<DrawTool, int> UnlockStage = new()
        {
            { DrawTool.FixedLine, 1 },
            { DrawTool.Erase, 1 },
            { DrawTool.FreeBody, 11 },
            { DrawTool.PivotSingle, 20 },
            { DrawTool.PivotWorld, 27 },
        };

        /// <summary>stage 는 1-기반 전역 스테이지 번호다.</summary>
        public static bool IsUnlocked(DrawTool tool, int stage) =>
            stage >= UnlockStage[tool];
    }
}
