using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 포인터 한 샘플의 단계. 장치 enum 을 그대로 쓰지
    /// 않아야 합성 궤적으로 검증할 수 있다.
    /// </summary>
    public enum PointerPhase
    {
        Down = 0,
        Move = 1,
        Up = 2,

        /// 시스템이 터치를 회수했다 — 알림·전화·팜.
        Canceled = 3,
    }

    /// <summary>
    /// 장치가 보고한 포인터 한 점.
    /// 좌표는 이미 월드로 옮겨진 값이다.
    /// </summary>
    public readonly struct PointerSample
    {
        /// 손가락을 구분하는 값. 마우스도 하나를 쓴다.
        public readonly int PointerId;

        public readonly PointerPhase Phase;

        public readonly Vector2 World;

        /// Down 에서만 본다. 소유권은 그 뒤 안 바뀐다 —
        /// 매 프레임 재판정하면 툴바를 스칠 때 획이 끊긴다.
        public readonly bool IsOverUI;

        public PointerSample(int pointerId, PointerPhase phase, Vector2 world, bool isOverUI = false)
        {
            PointerId = pointerId;
            Phase = phase;
            World = world;
            IsOverUI = isOverUI;
        }
    }
}
