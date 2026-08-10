using System.Collections.Generic;
using PPS.Core;
using PPS.Game;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 중인 맵을 그대로 돌려본다.
    /// 편집 데이터는 읽기만 한다 — 테스트 전후로
    /// 맵이 달라지면 만든 것을 믿을 수 없다.
    /// </summary>
    public sealed class MapTestRunner : MonoBehaviour
    {
        [SerializeField] MapEditSession _session;
        [SerializeField] MapEditHandles _handles;
        [SerializeField] GameSimDriver _driver;

        [SerializeField] Color _ballColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        [SerializeField] Color _starColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);
        [SerializeField] Color _terrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);

        SpriteRenderer _ball;
        SpriteRenderer _goal;
        readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrain = new List<SpriteRenderer>();

        bool _running;

        /// 결과를 한 번만 남기려고 둔다.
        bool _reported;

        /// 직전에 알린 별 개수. 늘 때만 알린다.
        int _reportedStars;

        public bool Running => _running;

        /// <summary>상단바 테스트 버튼이 부른다.</summary>
        public void Toggle()
        {
            if (_running) Stop();
            else Begin();
        }

        void Begin()
        {
            if (_session == null || _driver == null) return;

            // 그린 것 없이 레벨만 돌린다. 도구는 아직 없다.
            _driver.StartSimulation(_session.Current, Solution.Empty);

            if (_handles != null) _handles.gameObject.SetActive(false);

            _running = true;
            _reported = false;
            _reportedStars = 0;
            Debug.Log($"[맵 에디터] 테스트 시작: {_session.Current.StageId}");
        }

        void Stop()
        {
            _driver.Stop();
            HideAll();

            if (_handles != null) _handles.gameObject.SetActive(true);

            _running = false;
            Debug.Log("[맵 에디터] 테스트 종료. 편집으로 돌아간다.");
        }

        void Update()
        {
            if (!_running || !_driver.HasWorld) return;

            Redraw();
            Report();
        }

        void Report()
        {
            var world = _driver.World;

            // 어느 별을 먹었는지는 규칙상 뜻이 없다.
            // 몇 개인지만 센다.
            if (world.Judge.Stars > _reportedStars)
            {
                _reportedStars = world.Judge.Stars;
                Debug.Log($"[맵 에디터] 별 {_reportedStars} / {world.Level.Stars.Count}");
            }

            if (_reported || !world.IsTerminal) return;

            _reported = true;
            SimResult result = world.ToResult(0f);
            Debug.Log($"[맵 에디터] 테스트 결과: {result}");
        }

        void Redraw()
        {
            var world = _driver.World;
            var level = world.Level;

            if (_ball == null) _ball = Make("TestBall", MapHandleGfx.Circle);
            if (_goal == null) _goal = Make("TestGoal", MapHandleGfx.Circle);

            _ball.gameObject.SetActive(true);
            _goal.gameObject.SetActive(true);

            MapHandleGfx.PlaceDot(_ball, world.Ball.position, LevelData.BallRadius, _ballColor);
            MapHandleGfx.PlaceDot(_goal, level.GoalPosition, LevelData.GoalRadius, _goalColor);

            Grow(_terrain, level.Terrain.Count, "TestTerrain", MapHandleGfx.Square);
            for (int i = 0; i < _terrain.Count; i++)
            {
                bool used = i < level.Terrain.Count;
                _terrain[i].gameObject.SetActive(used);
                if (used) MapHandleGfx.PlaceLine(_terrain[i], level.Terrain[i], _terrainColor);
            }

            Grow(_stars, level.Stars.Count, "TestStar", MapHandleGfx.Circle);
            for (int i = 0; i < _stars.Count; i++)
            {
                bool used = i < level.Stars.Count;
                _stars[i].gameObject.SetActive(used);
                if (used)
                    MapHandleGfx.PlaceDot(
                        _stars[i], level.Stars[i], LevelData.StarCaptureRadius, _starColor);
            }
        }

        void Grow(List<SpriteRenderer> handles, int need, string name, Sprite sprite)
        {
            while (handles.Count < need)
                handles.Add(Make($"{name}_{handles.Count}", sprite));
        }

        SpriteRenderer Make(string name, Sprite sprite) =>
            MapHandleGfx.Create(transform, name, sprite);

        void HideAll()
        {
            if (_ball != null) _ball.gameObject.SetActive(false);
            if (_goal != null) _goal.gameObject.SetActive(false);

            for (int i = 0; i < _stars.Count; i++) _stars[i].gameObject.SetActive(false);
            for (int i = 0; i < _terrain.Count; i++) _terrain[i].gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_driver != null) _driver.Stop();
        }
    }
}
