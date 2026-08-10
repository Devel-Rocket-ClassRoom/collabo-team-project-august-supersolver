using UnityEngine;

namespace PPS.Core
{
    public static class LevelDataArea
    {
        // 공, 목표, 지형 바깥쪽에 추가할 공통 여백.
        public const float AreaMargin = 2f;

        public static Rect Calculate(LevelData level)
        {
            //공의 왼쪽 아래 좌표를 최초 최솟값으로 설정한다.
            Vector2 min = level.BallStart - Vector2.one * LevelData.BallRadius;

            // 공의 오른쪽 위 좌표를 최초 최댓값으로 설정한다.
            Vector2 max = level.BallStart + Vector2.one * LevelData.BallRadius;

            // 목표 지점의 왼쪽 아래 좌표를 영역에 포함한다.
            Include(level.GoalPosition - Vector2.one * LevelData.GoalRadius, ref min, ref max);

            // 목표의 오른쪽 위 좌표를 영역에 포함한다.
            Include (level.GoalPosition + Vector2.one * LevelData.GoalRadius, ref min, ref max);

            // 레벨에 등록된 모든 지형 선분을 차례대로 확인한다.
            for (int i = 0; i < level.Terrain.Count; i++)
            {
                // 지형 선분의 시작점 A를 영역에 포함한다.
                Include (level.Terrain[i].A, ref min, ref max);

                //지형 선분의 끝점 B를 영역에 포함한다.
                Include(level.Terrain[i].B, ref min, ref max);
            }
            return Rect.MinMaxRect(
                min.x - AreaMargin,
                min.y - AreaMargin,
                max.x + AreaMargin,
                max.y + AreaMargin);
        }

        static void Include(Vector2 point, ref Vector2 min, ref Vector2 max)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
    }
}
