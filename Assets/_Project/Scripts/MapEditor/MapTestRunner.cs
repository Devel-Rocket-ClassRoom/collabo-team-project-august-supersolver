using System.Collections.Generic;
using PPS.Core;
using PPS.Game;
using UnityEngine;

// UnityEngine 에도 같은 이름이 있다(SystemInfo.deviceType).
using DeviceType = PPS.Core.DeviceType;

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
        [SerializeField] Color _bombColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
        [SerializeField] Color _fragColor = new Color32(0x8A, 0x2B, 0x2B, 0xFF);
        [SerializeField] Color _spikeColor = new Color32(0x6B, 0x1F, 0x1F, 0xFF);
        [SerializeField] Color _windColor = new Color32(0x2E, 0x86, 0xA8, 0xFF);
        [SerializeField] Color _burstColor = new Color32(0xFF, 0x8C, 0x2A, 0xFF);

        /// 불꽃이 남는 길이. 60 스텝이 1 초다.
        const int BurstSteps = 18;

        SpriteRenderer _ball;
        SpriteRenderer _goal;
        readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrain = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _devices = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _fragments = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _bursts = new List<SpriteRenderer>();

        /// 직전 프레임에 바디가 있었는가. 사라지는 순간이 폭발이다.
        bool[] _deviceAlive;

        /// 터진 스텝. 아직 안 터졌으면 -1.
        int[] _burstStep;

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

            Grow(_stars, level.Stars.Count, "TestStar", MapHandleGfx.Star);
            for (int i = 0; i < _stars.Count; i++)
            {
                // 먹은 별은 지운다. 남아 있으면 먹었는지 알 수 없다.
                bool used = i < level.Stars.Count && !world.Judge.IsCollected(i);
                _stars[i].gameObject.SetActive(used);
                if (used)
                    MapHandleGfx.PlaceDot(_stars[i], level.Stars[i],
                        LevelData.StarCaptureRadius / MapHandleGfx.StarSpan, _starColor);
            }

            DrawDevices(world, level);
            DrawBursts(world, level);
            DrawFragments(world);
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

            EnsureBurstState(level.Devices.Count);
            Grow(_devices, level.Devices.Count, "TestDevice", MapHandleGfx.Bomb);

            for (int i = 0; i < _devices.Count; i++)
            {
                if (i >= level.Devices.Count)
                {
                    _devices[i].gameObject.SetActive(false);
                    continue;
                }

                DeviceData data = level.Devices[i];
                bool hasBody = DeviceFactory.MakesBody(data.Type);

                Rigidbody2D body = hasBody && at < world.Bodies.Count
                    ? world.Bodies[at]
                    : null;

                // 바디를 만든 장치만 자리를 하나 쓴다.
                if (hasBody) at++;

                bool used = !hasBody || body != null;

                // 있던 바디가 사라진 프레임이 터진 순간이다.
                // 코어에 발동 시점을 묻지 않아도 여기서 알 수 있다.
                if (hasBody && _deviceAlive[i] && body == null)
                    _burstStep[i] = world.CurrentStep;

                _deviceAlive[i] = used;
                _devices[i].gameObject.SetActive(used);
                if (!used) continue;

                _devices[i].sprite = DeviceSprite(data.Type);

                MapHandleGfx.PlaceDot(_devices[i],
                    body != null ? body.position : data.Position,
                    DeviceDrawRadius(data), DeviceColor(data.Type),
                    data.Type == DeviceType.Wind ? data.Angle : 0f);
            }
        }

        void EnsureBurstState(int count)
        {
            if (_deviceAlive != null && _deviceAlive.Length == count) return;

            _deviceAlive = new bool[count];
            _burstStep = new int[count];

            for (int i = 0; i < count; i++) _burstStep[i] = -1;
        }

        /// <summary>
        /// 터진 자리에 잠깐 남는 불꽃.
        /// 몸 크기에서 폭발 반경까지 커지며 옅어진다 —
        /// 어디까지 밀렸는지가 눈에 남아야 한다.
        /// </summary>
        void DrawBursts(SimWorld world, LevelData level)
        {
            Grow(_bursts, level.Devices.Count, "TestBurst", MapHandleGfx.Burst);

            for (int i = 0; i < _bursts.Count; i++)
            {
                int since = i < level.Devices.Count && _burstStep[i] >= 0
                    ? world.CurrentStep - _burstStep[i]
                    : int.MaxValue;

                bool used = since >= 0 && since < BurstSteps;
                _bursts[i].gameObject.SetActive(used);
                if (!used) continue;

                DeviceData data = level.Devices[i];
                float t = (float)since / BurstSteps;

                // 파편 폭탄은 반경이 0 이라 파편이 퍼지는 만큼만 잡는다.
                float reach = Mathf.Max(data.Radius, 0.9f);

                Color color = _burstColor;
                color.a *= 1f - t;

                MapHandleGfx.PlaceDot(_bursts[i], data.Position,
                    Mathf.Lerp(BombDevice.BodyRadius, reach, t), color);
            }
        }

        static Sprite DeviceSprite(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.FragBomb: return MapHandleGfx.FragBomb;
                case DeviceType.Spike: return MapHandleGfx.Spike;
                case DeviceType.Wind: return MapHandleGfx.Wind;
                default: return MapHandleGfx.Bomb;
            }
        }

        Color DeviceColor(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.FragBomb: return _fragColor;
                case DeviceType.Spike: return _spikeColor;
                case DeviceType.Wind: return _windColor;
                default: return _bombColor;
            }
        }

        static float DeviceDrawRadius(in DeviceData device)
        {
            switch (device.Type)
            {
                case DeviceType.FragBomb:
                    return BombDevice.BodyRadius / MapHandleGfx.FragBombBodySpan;

                case DeviceType.Spike:
                    return Mathf.Max(device.Radius, SpikeDevice.MinRadius)
                        / MapHandleGfx.SpikeBodySpan;

                case DeviceType.Wind:
                    return Mathf.Max(device.Radius * 0.5f, 0.3f);

                default:
                    return BombDevice.BodyRadius / MapHandleGfx.BombBodySpan;
            }
        }

        /// <summary>
        /// 파편은 닿으면 실패다. 안 보이면 왜 죽었는지 알 수 없다.
        /// </summary>
        void DrawFragments(SimWorld world)
        {
            var hazards = world.Hazards;

            Grow(_fragments, hazards.Count, "TestFragment", MapHandleGfx.Circle);

            for (int i = 0; i < _fragments.Count; i++)
            {
                bool used = i < hazards.Count && hazards[i] != null;

                _fragments[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceDot(_fragments[i], hazards[i].transform.position,
                    FragBombDevice.FragmentRadius, _fragColor);
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
            for (int i = 0; i < _devices.Count; i++) _devices[i].gameObject.SetActive(false);
            for (int i = 0; i < _fragments.Count; i++) _fragments[i].gameObject.SetActive(false);
            for (int i = 0; i < _bursts.Count; i++) _bursts[i].gameObject.SetActive(false);

            // 다음 테스트에서 예전 폭발이 되살아나면 안 된다.
            _deviceAlive = null;
        }

        void OnDestroy()
        {
            if (_driver != null) _driver.Stop();
        }
    }
}
