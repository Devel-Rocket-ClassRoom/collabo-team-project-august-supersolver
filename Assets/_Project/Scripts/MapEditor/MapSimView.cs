using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 돌아가는 시뮬레이션을 그린다. 월드를 읽기만 한다.
    /// 먹은 별은 스태커에게 묻는다 — 판정 상태를
    /// 여기서 또 들면 둘이 어긋난다.
    /// </summary>
    public sealed class MapSimView : MonoBehaviour
    {
        [SerializeField] MapEditStyle _style;
        [SerializeField] StarStacker _stars;

        SpriteRenderer _ball;
        SpriteRenderer _goal;

        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrain = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _devices = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _fragments = new List<SpriteRenderer>();

        void Awake()
        {
            _ball = Create("SimBall", _style.Sprites.Ball);
            _goal = Create("SimGoal", _style.Sprites.Goal);
        }

        public void OnDraw(SimWorld world)
        {
            if (_style == null) return;

            LevelData level = world.Level;

            _ball.gameObject.SetActive(true);
            _goal.gameObject.SetActive(true);

            MapHandleGfx.PlaceDot(_ball, world.Ball.position, LevelData.BallRadius, MapEditStyle.Plain);
            MapHandleGfx.PlaceDot(_goal, level.GoalPosition, LevelData.GoalRadius, MapEditStyle.Plain);

            DrawTerrain(level);
            DrawStars(level);
            DrawDevices(world, level);
            DrawFragments(world);
        }

        void DrawTerrain(LevelData level)
        {
            Grow(_terrain, level.Terrain.Count, "SimTerrain", MapHandleGfx.Square);

            for (int i = 0; i < _terrain.Count; i++)
            {
                bool used = i < level.Terrain.Count;
                _terrain[i].gameObject.SetActive(used);
                if (used) MapHandleGfx.PlaceLine(_terrain[i], level.Terrain[i], _style.Terrain);
            }
        }

        void DrawStars(LevelData level)
        {
            Grow(_starHandles, level.Stars.Count, "SimStar", _style.Sprites.Star);

            for (int i = 0; i < _starHandles.Count; i++)
            {
                // 먹은 별은 지운다. 남아 있으면 먹었는지 알 수 없다.
                bool used = i < level.Stars.Count && !_stars.IsCollected(i);

                _starHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceDot(_starHandles[i], level.Stars[i],
                    MapEditStyle.StarRadius, MapEditStyle.Plain);
            }
        }

        /// <summary>
        /// 살아 있는 바디를 보고 그린다. 터진 폭탄은 바디가
        /// 사라지므로 표시도 그 순간 함께 사라진다.
        /// 바디가 없는 장치는 레벨 데이터로 그린다 —
        /// 사라지는 일이 없어 자리만 알면 된다.
        /// </summary>
        void DrawDevices(SimWorld world, LevelData level)
        {
            // 바디 순서는 공 하나 → 지형 → 장치다.
            int at = 1 + level.Terrain.Count;

            Grow(_devices, level.Devices.Count, "SimDevice", _style.Sprites.Bomb);

            for (int i = 0; i < _devices.Count; i++)
            {
                if (i >= level.Devices.Count)
                {
                    _devices[i].gameObject.SetActive(false);
                    continue;
                }

                DeviceData data = level.Devices[i];
                bool hasBody = DeviceFactory.MakesBody(data.Type);

                Rigidbody2D body = hasBody && at < world.Bodies.Count ? world.Bodies[at] : null;

                // 바디를 만든 장치만 자리를 하나 쓴다.
                if (hasBody) at++;

                bool used = !hasBody || body != null;
                _devices[i].gameObject.SetActive(used);
                if (!used) continue;

                _devices[i].sprite = _style.SpriteOf(data.Type);

                MapHandleGfx.PlaceDot(_devices[i],
                    body != null ? body.position : data.Position,
                    MapEditStyle.RadiusOf(data), MapEditStyle.Plain,
                    MapEditStyle.AngleOf(data));
            }
        }

        /// <summary>
        /// 파편은 닿으면 실패다. 안 보이면 왜 죽었는지 알 수 없다.
        /// </summary>
        void DrawFragments(SimWorld world)
        {
            var hazards = world.Hazards;

            Grow(_fragments, hazards.Count, "SimFragment", _style.Sprites.Dot);

            for (int i = 0; i < _fragments.Count; i++)
            {
                bool used = i < hazards.Count && hazards[i] != null;

                _fragments[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceDot(_fragments[i], hazards[i].transform.position,
                    FragBombDevice.FragmentRadius, MapEditStyle.Plain);
            }
        }

        public void HideAll()
        {
            if (_ball != null) _ball.gameObject.SetActive(false);
            if (_goal != null) _goal.gameObject.SetActive(false);

            Hide(_starHandles);
            Hide(_terrain);
            Hide(_devices);
            Hide(_fragments);
        }

        static void Hide(List<SpriteRenderer> handles)
        {
            for (int i = 0; i < handles.Count; i++) handles[i].gameObject.SetActive(false);
        }

        void Grow(List<SpriteRenderer> handles, int need, string name, Sprite sprite)
        {
            while (handles.Count < need)
                handles.Add(Create($"{name}_{handles.Count}", sprite));
        }

        SpriteRenderer Create(string name, Sprite sprite) =>
            MapHandleGfx.Create(transform, name, sprite);
    }
}
