using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 지형 선분에서 앵커를 뽑는다.
    /// 자리는 두 끝점이다 — 지형이 끊기는 곳이 곧 공이 떨어지는 곳이고,
    /// 유저가 다리를 놓을 곳도 거기다. 중간은 이미 발판이라 값이 없다.
    /// </summary>
    public class StaticSegmentAnchorSelector : SolverPrimitiveAnchorSelector
    {
        public virtual SolverAnchor[] Select(StaticSegment segment, LevelData level)
        {
            float angle = Angle(segment);

            return new[]
            {
                new SolverAnchor(segment.A, Primitives(angle, level)),
                new SolverAnchor(segment.B, Primitives(angle, level)),
            };
        }

        /// <summary>
        /// 끊긴 지형을 잇는 그림. 선분과 같은 각도로 이어 놓는다 —
        /// 끊긴 발판을 그대로 연장하는 것이 이 자리에서 가장 먼저
        /// 떠오르는 한 획이다.
        /// 크기는 공 지름의 두 배다. 레벨 데이터에 길이 기준이
        /// 공 반지름과 잉크 한도뿐이라 공 쪽을 잡았다.
        /// </summary>
        protected virtual Primitive[] Primitives(float angle, LevelData level)
            => new[]
            {
                new Primitive(PrimitiveShape.Line, level.BallRadius * 4f,
                    ToolType.FixedLine, angle),
            };

        /// 선분이 누운 각도(라디안).
        static float Angle(StaticSegment segment)
        {
            Vector2 along = segment.B - segment.A;
            return Mathf.Atan2(along.y, along.x);
        }
    }
}
