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

            // 장치도 화면에 보이고 플레이어가 대응해야 하는
            // 대상이라 영역이 품는다. 폭탄·바람은 반경이
            // 곧 영향 범위라 점이 아니라 반경으로 넣는다.
            for (int i = 0; i < level.Devices.Count; i++)
            {
                Vector2 center = level.Devices[i].Position;
                Vector2 reach = Vector2.one * level.Devices[i].Radius;

                Include(center - reach, ref min, ref max);
                Include(center + reach, ref min, ref max);
            }

            // 별은 먹는 판정 반경만큼 자리를 차지한다.
            for (int i = 0; i < level.Stars.Count; i++)
            {
                Vector2 reach = Vector2.one * LevelData.StarCaptureRadius;

                Include(level.Stars[i] - reach, ref min, ref max);
                Include(level.Stars[i] + reach, ref min, ref max);
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
