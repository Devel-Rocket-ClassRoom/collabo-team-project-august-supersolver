using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 공을 어디서 어떤 속도로 출발시킬지.
    /// 휴리스틱 맵이 셀의 대표 상태를 여기 담아 넘긴다 —
    /// 레벨의 시작점 하나로는 셀마다 굴려 볼 수 없다.
    /// </summary>
    public readonly struct BallState
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;

        public BallState(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        public override string ToString()
            => $"p({Position.x:F2},{Position.y:F2}) v({Velocity.x:F2},{Velocity.y:F2})";
    }
}
