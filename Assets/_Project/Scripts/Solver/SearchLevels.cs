using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 두 패스를 가르는 최소 레벨들.
    /// 하나는 통로만으로 풀리고 하나는 그러지 못한다 —
    /// 탐색이 순서대로 도는지는 이 둘의 차이로만 확인된다.
    /// 테스트와 뷰어가 같은 것을 봐야 눈으로 본 것이 근거가 된다.
    /// </summary>
    public static class SearchLevels
    {
        /// <summary>
        /// 비탈을 굴러 목표에 닿는다. 통로만으로 풀려야 한다.
        /// 목표를 비탈에서 띄워 둔 것은 통로를 찾기 위해서다 —
        /// 표면에 붙여 놓으면 목표에서 나가는 간선이 전부 지형에 걸려
        /// 그래프에서 고립되고, 통로가 하나도 안 나온다.
        /// </summary>
        public static LevelData Slope()
        {
            var level = new LevelData
            {
                BallStart = new Vector2(-4f, 3.2f),
                GoalPosition = new Vector2(4f, 0.2f),
                KillY = -12f,
            };

            level.Terrain.Add(new StaticSegment(new Vector2(-5f, 3f), new Vector2(5f, -1f)));

            return level;
        }

        /// <summary>
        /// 목표가 선반 위에 있다. 공이 스스로 오를 길이 없어
        /// 통로를 아무리 세워도 통로 패스로는 안 풀린다.
        /// 공을 공중에 띄워 둔 것은 지렛대를 놓을 자리를 내기 위해서다 —
        /// 판은 공보다 한 반지름 아래에 깔리고 추 쪽은 더 내려가야 해서,
        /// 공이 바닥에 붙어 있으면 지렛대가 들어갈 틈이 없다.
        /// </summary>
        public static LevelData Uphill()
        {
            var level = new LevelData
            {
                BallStart = new Vector2(-3f, 2.5f),
                GoalPosition = new Vector2(2.5f, 4f),
                KillY = -12f,
            };

            level.Terrain.Add(new StaticSegment(new Vector2(-6f, 0f), new Vector2(6f, 0f)));
            level.Terrain.Add(new StaticSegment(new Vector2(1.5f, 3.4f), new Vector2(6f, 3.4f)));

            return level;
        }

        public static StageData Stage(string id, LevelData level)
            => new StageData { StageId = id, Seed = 0, Level = level };
    }
}
