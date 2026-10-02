using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 한 시점의 공 상태. 궤적의 한 점이다.
    /// 휴리스틱 맵이 간선을 뽑는 원재료라
    /// 속도까지 있어야 한다 — 같은 자리라도
    /// 어디로 가던 중이었는지가 다르다.
    /// </summary>
    public readonly struct BallSample
    {
        /// 이 값을 찍은 스텝.
        public readonly int Step;

        public readonly Vector2 Position;

        public readonly Vector2 Velocity;

        public BallSample(int step, Vector2 position, Vector2 velocity)
        {
            Step = step;
            Position = position;
            Velocity = velocity;
        }

        public override string ToString()
            => $"#{Step} p({Position.x:F2},{Position.y:F2}) v({Velocity.x:F2},{Velocity.y:F2})";
    }
}
