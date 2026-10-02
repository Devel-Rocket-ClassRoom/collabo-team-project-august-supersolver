using System;

namespace PPS.Core
{
    /// <summary>
    /// 튜토리얼이 판을 지켜보는 창구. 전이는 StageFlow 가
    /// 쥐고 안내 층은 보기만 한다 — 그 경계가 여기다.
    /// 안내가 세션과 드라이버를 직접 참조하면 드로잉 툴
    /// 안쪽까지 알게 된다.
    /// </summary>
    public interface ITutorialSignals
    {
        /// 플레이어가 고른 도구로 무언가 했다. 되돌리기·
        /// 초기화는 판을 되돌리는 것이지 도구를 쓴 것이
        /// 아니라 여기 안 온다 — 안 거르면 되돌리기 버튼에
        /// "선을 그어라" 컷이 넘어간다.
        /// WaitForDrawingChange 가 씀
        event Action ToolActed;

        /// 시뮬레이션 판정이 났다. 이벤트가 아니라 상태다 —
        /// 판정은 한 번 나면 안 뒤집힌다.
        /// WaitForSimDecision 이 씀
        bool SimDecided { get; }
    }
}
