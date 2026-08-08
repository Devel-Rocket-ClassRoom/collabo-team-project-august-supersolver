using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 셀 대표 위치에 공을 놓을 수 있는지 본다.
    /// 지형에 박힌 채 출발한 공은 밀려나며 튀어,
    /// 그 궤적의 간선이 물리적으로 불가능한 이동이 된다.
    /// 맵에 섞이면 h 가 낙관 쪽으로 무너진다.
    /// </summary>
    public static class BallSpawn
    {
        /// <summary>
        /// 공이 지형과 겹치는 자리인가.
        /// 놓인 프리미티브는 보지 않는다 — 후보마다 달라
        /// 셀 단위로 한 번 거를 수 있는 값이 아니다.
        /// </summary>
        public static bool Blocked(LevelData level, Vector2 position)
        {
            if (level.Terrain == null) return false;

            for (int i = 0; i < level.Terrain.Count; i++)
            {
                var segment = level.Terrain[i];
                if (DistanceToSegment(position, segment.A, segment.B) < level.BallRadius)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 점과 선분 사이 최단 거리.
        /// 수선의 발이 선분 밖으로 나가면 가까운 끝점이 답이다.
        /// </summary>
        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;

            // 길이 0 인 지형은 점 하나다.
            if (lengthSq <= 0f) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return Vector2.Distance(point, a + ab * t);
        }
    }
}
