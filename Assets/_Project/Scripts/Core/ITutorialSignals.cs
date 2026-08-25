using System;

namespace PPS.Core
{
    /// <summary>
    /// 튜토리얼이 판을 지켜보는 창구. 전이는 StageFlow 가
    /// 쥐고 안내 층은 보기만 한다 — 그 경계가 여기다.
    /// 안내가 세션과 드라이버를 직접 물면 드로잉 툴
    /// 안쪽까지 알게 된다.
    /// </summary>
    public interface ITutorialSignals
    {
        /// 캔버스의 그림이 달라졌다. 
        /// WaitForStroke 가 씀
        event Action DrawingChanged;

        /// 시뮬레이션 판정이 났다. 이벤트가 아니라 상태다 —
        /// 판정은 한 번 나면 안 뒤집힌다.
        /// WaitForSimDecision 이 씀
        bool SimDecided { get; }
    }
}
