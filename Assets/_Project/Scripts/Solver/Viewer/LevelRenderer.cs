using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 레벨 자체를 그린다 — 지형·목표·킬라인·장치와 공.
    /// LevelData 만 보므로 월드를 짓지도 스텝을 밟지도
    /// 않는다. 공 자리는 밖에서 넣어 준다.
    /// </summary>
    public sealed class LevelRenderer : MonoBehaviour
    {
        /// 킬라인은 끝이 없어 화면 밖까지 길게 긋는다.
        const float KillLineHalfWidth = 30f;

        /// 거의 검정.
        [SerializeField] Color _terrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);

        /// <summary>
        /// 진한 청록. 살아 있는 공(크림슨)과 달리
        /// 이건 궤적 스냅샷이 말하는 자리 표시다.
        /// 샘플 스텝에서 둘이 정확히 겹쳐야 정상이다.
        /// </summary>
        [SerializeField] Color _ballColor = new Color32(0x0B, 0x6E, 0x6E, 0xFF);

        /// 진한 초록.
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);

        /// 어두운 벽돌.
        [SerializeField] Color _killLineColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);

        /// 진한 보라.
        [SerializeField] Color _deviceColor = new Color32(0x6B, 0x3F, 0xA0, 0xFF);

        LevelData _level;

        /// 공을 그릴 자리. 기본은 출발점이다.
        Vector2 _ballAt;

        public void Show(LevelData level)
        {
            _level = level;
            _ballAt = level.BallStart;
        }

        /// <summary>
        /// 공을 옮긴다. 궤적의 한 스냅샷을 집어
        /// 그 시점의 공을 보는 용도다.
        /// </summary>
        public void MoveBall(Vector2 at) => _ballAt = at;

        public void Clear() => _level = null;

        void OnRenderObject()
        {
            if (_level == null) return;

            GLDraw.SetPass();

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            GL.Color(_terrainColor);
            var terrain = _level.Terrain;
            if (terrain != null)
            {
                for (int i = 0; i < terrain.Count; i++)
                    GLDraw.Line(terrain[i].A, terrain[i].B);
            }

            GL.Color(_goalColor);
            GLDraw.Circle(_level.GoalPosition, _level.GoalRadius);

            GL.Color(_killLineColor);
            GLDraw.Line(new Vector2(-KillLineHalfWidth, _level.KillY),
                        new Vector2(KillLineHalfWidth, _level.KillY));

            // 반경이 영향 범위라 판정 원과 함께 그린다.
            GL.Color(_deviceColor);
            var devices = _level.Devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    Vector2 at = devices[i].Position;
                    GLDraw.Circle(at, 0.3f);
                    GLDraw.Circle(at, devices[i].Radius);
                }
            }

            GL.Color(_ballColor);
            GLDraw.Circle(_ballAt, _level.BallRadius);

            GL.End();
            GL.PopMatrix();
        }
    }
}
