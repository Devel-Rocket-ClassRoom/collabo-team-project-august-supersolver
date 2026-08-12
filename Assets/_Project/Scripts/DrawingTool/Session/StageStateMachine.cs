namespace PPS.DrawingTool
{
    /// <summary>판이 지금 무엇을 하고 있는가.</summary>
    public enum StageMode
    {
        /// 캔버스 입력과 도구 UI 가 사는 유일한 모드.
        Draw = 0,

        Simulate = 1,

        Paused = 2,
    }

    /// <summary>
    /// 전이표만 아는 순수 상태 머신. 부수 효과는 StageFlow 가
    /// 진다 — 재시도×일시정지 조합을 씬 없이 재려면 둘이
    /// 갈라져 있어야 한다.
    /// </summary>
    public sealed class StageStateMachine
    {
        public StageMode Mode { get; private set; } = StageMode.Draw;

        /// <returns>모드가 바뀌었으면 true. 부수 효과는 이때만 건다.</returns>
        public bool Play() => Move(StageMode.Draw, StageMode.Simulate);

        /// <summary>버튼 하나가 두 방향을 쥔다.</summary>
        public bool PauseResume() =>
            Mode == StageMode.Simulate
                ? Move(StageMode.Simulate, StageMode.Paused)
                : Move(StageMode.Paused, StageMode.Simulate);

        /// <summary>시뮬레이션·일시정지 어느 쪽에서도 그리기로 돌아온다.</summary>
        public bool Retry()
        {
            if (Mode == StageMode.Draw) return false;

            Mode = StageMode.Draw;
            return true;
        }

        bool Move(StageMode from, StageMode to)
        {
            if (Mode != from) return false;

            Mode = to;
            return true;
        }
    }
}
