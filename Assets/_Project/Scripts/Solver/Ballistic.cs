using UnityEngine;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 손을 떠난 공의 길. 중력만 받으므로 시뮬 없이 풀린다.
    /// 이산으로 푸는 것은 Box2D 가 속도를 먼저 더하고 자리를 옮기기 때문이다 —
    /// 연속 포물선과 반 스텝 어긋나서, 좁은 판정에서는 그 차이가 답을 바꾼다.
    /// </summary>
    public static class Ballistic
    {
        /// <summary>
        /// from 에서 velocity 로 떠난 공의 step 스텝 뒤 자리.
        /// 매 스텝 속도를 먼저 더하므로 중력 항이 n(n+1)/2 로 쌓인다.
        /// </summary>
        public static Vector2 At(Vector2 from, Vector2 velocity, int step)
        {
            float dt = SimWorld.FixedDt;

            return from
                   + velocity * (dt * step)
                   + (Vector2)Physics2D.gravity * (dt * dt * step * (step + 1) * 0.5f);
        }

        /// <summary>
        /// 이 길이 target 을 tolerance 안쪽으로 지나가는 첫 스텝. 안 지나면 -1.
        /// 스텝 사이를 선분으로 이어 재는 것이 중요하다 — 빠른 공은
        /// 좁은 판정 구간을 두 샘플 사이로 통째로 건너뛴다.
        /// </summary>
        public static int StepNear(
            Vector2 from, Vector2 velocity, Vector2 target, float tolerance, int maxSteps)
        {
            Vector2 was = from;

            for (int step = 1; step <= maxSteps; step++)
            {
                Vector2 now = At(from, velocity, step);

                if (PointToSegment(target, was, now) <= tolerance) return step;

                // 목표보다 낮아지며 멀어지기만 하면 더 볼 것이 없다.
                if (now.y < target.y - tolerance && now.y < was.y) return -1;

                was = now;
            }

            return -1;
        }

        static float PointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 along = b - a;
            float lengthSq = along.sqrMagnitude;

            if (lengthSq <= Mathf.Epsilon) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / lengthSq);
            return Vector2.Distance(point, a + along * t);
        }
    }
}
