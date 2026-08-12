using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 화면을 그린다. 레벨을 읽기만 하고 고치지 않는다 —
    /// 그리는 쪽이 데이터를 건드리면 무엇이 바꿨는지 알 수 없다.
    /// </summary>
    public sealed class MapEditView : MonoBehaviour
    {
        [SerializeField] MapEditStyle _style;

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;
        SpriteRenderer _scaleHandle;

        /// 고른 장치가 미치는 범위.
        SpriteRenderer _reachHandle;

        /// 크기 조절의 기준이 되는 테두리 네 변.
        readonly SpriteRenderer[] _boundsHandles = new SpriteRenderer[4];

        /// 테두리를 그리는 임시 버퍼.
        readonly Vector2[] _corners = new Vector2[4];

        readonly List<SpriteRenderer> _vertexHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _deviceHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrainHandles = new List<SpriteRenderer>();

        /// 도형을 선분으로 굽는 임시 버퍼.
        /// 매번 새로 만들면 프레임마다 할당이 생긴다.
        readonly List<StaticSegment> _scratch = new List<StaticSegment>();

        /// 이번에 그리는 것. 전달 인자를 메서드마다 나르지 않는다.
        MapDrawModel _model;

        /// <summary>
        /// 크기 핸들 자리. 테두리의 오른쪽 위 모서리다 —
        /// 허공에 뜬 점이 아니라 무엇을 잡는지 보인다.
        /// 집는 쪽과 그리는 쪽이 같은 자리를 봐야 한다.
        /// </summary>
        public static Vector2 ScaleHandleAt(ShapeData shape) => shape.Bounds().max;

        void Awake()
        {
            _startHandle = Create("StartHandle", _style.Sprites.Ball);
            _goalHandle = Create("GoalHandle", _style.Sprites.Goal);
            _scaleHandle = Create("ScaleHandle", MapHandleGfx.Square);
            _reachHandle = Create("ReachHandle", MapHandleGfx.Circle);

            for (int i = 0; i < _boundsHandles.Length; i++)
                _boundsHandles[i] = Create($"BoundsHandle_{i}", MapHandleGfx.Square);
        }

        /// <summary>편집 쪽이 매 프레임 부른다.</summary>
        public void OnDraw(in MapDrawModel model)
        {
            if (_style == null || model.Level == null) return;

            _model = model;

            // 이 둘은 늘 있어서 개수로 켜고 끄지 않는다.
            // HideAll 로 꺼진 뒤 스스로 돌아올 길이 여기뿐이다.
            _startHandle.gameObject.SetActive(true);
            _goalHandle.gameObject.SetActive(true);

            MapHandleGfx.PlaceDot(_startHandle, model.Level.BallStart, LevelData.BallRadius,
                Tint(MapEditStyle.Plain, MapHandleKind.Start, 0));
            MapHandleGfx.PlaceDot(_goalHandle, model.Level.GoalPosition, LevelData.GoalRadius,
                Tint(MapEditStyle.Plain, MapHandleKind.Goal, 0));

            DrawStars();
            DrawDevices();
            DrawShapes();
            DrawEditHandles();
        }

        void DrawStars()
        {
            var stars = _model.Level.Stars;

            // 개수가 변한다. 남는 핸들은 끄고 다시 쓴다.
            Grow(_starHandles, stars.Count, "StarHandle", _style.Sprites.Star);

            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceDot(_starHandles[i], stars[i], MapEditStyle.StarRadius,
                    Tint(MapEditStyle.Plain, MapHandleKind.Star, i));
            }
        }

        /// <summary>
        /// 장치를 종류에 맞는 모양으로 그린다.
        /// 미치는 범위는 고른 것만 — 늘 그리면 원이 겹쳐 어지럽다.
        /// </summary>
        void DrawDevices()
        {
            var devices = _model.Level.Devices;

            Grow(_deviceHandles, devices.Count, "DeviceHandle", _style.Sprites.Bomb);

            for (int i = 0; i < _deviceHandles.Count; i++)
            {
                bool used = i < devices.Count;
                _deviceHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                _deviceHandles[i].sprite = _style.SpriteOf(devices[i].Type);

                MapHandleGfx.PlaceDot(_deviceHandles[i], devices[i].Position,
                    MapEditStyle.RadiusOf(devices[i]),
                    Tint(MapEditStyle.Plain, MapHandleKind.Device, i),
                    MapEditStyle.AngleOf(devices[i]));
            }

            DrawReach(devices);
        }

        void DrawReach(List<DeviceData> devices)
        {
            bool on = _model.Selection.Kind == MapHandleKind.Device
                && _model.Selection.Index < devices.Count
                && MapEditStyle.HasReach(devices[_model.Selection.Index]);

            _reachHandle.gameObject.SetActive(on);
            if (!on) return;

            DeviceData device = devices[_model.Selection.Index];
            MapHandleGfx.PlaceDot(_reachHandle, device.Position, device.Radius, _style.Reach);
        }

        /// <summary>
        /// 도형마다 색을 정하고 그 도형의 선분을 그린다.
        /// 구운 지형을 그대로 그리면 어느 선분이 어느
        /// 도형의 것인지 알 수 없어 선택 표시가 안 된다.
        /// </summary>
        void DrawShapes()
        {
            var shapes = _model.Shapes.Shapes;

            Grow(_terrainHandles, _model.Level.Terrain.Count,
                "TerrainHandle", MapHandleGfx.Square);

            int handle = 0;

            for (int i = 0; i < shapes.Count; i++)
            {
                Color color = Tint(_style.Terrain, MapHandleKind.Terrain, i);

                _scratch.Clear();
                ShapeBaker.Append(shapes[i], _scratch);

                for (int s = 0; s < _scratch.Count && handle < _terrainHandles.Count; s++, handle++)
                {
                    _terrainHandles[handle].gameObject.SetActive(true);
                    MapHandleGfx.PlaceLine(_terrainHandles[handle], _scratch[s], color);
                }
            }

            for (int i = handle; i < _terrainHandles.Count; i++)
                _terrainHandles[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// 편집 모드에서만 버텍스·테두리·크기 핸들을 띄운다.
        /// 표시일 뿐이라 물리 데이터에 닿지 않는다.
        /// </summary>
        void DrawEditHandles()
        {
            var shapes = _model.Shapes.Shapes;

            bool on = _model.EditMode
                && _model.Selection.Kind == MapHandleKind.Terrain
                && _model.Selection.Index < shapes.Count;

            _scaleHandle.gameObject.SetActive(on);
            for (int i = 0; i < _boundsHandles.Length; i++)
                _boundsHandles[i].gameObject.SetActive(on);

            if (!on)
            {
                for (int i = 0; i < _vertexHandles.Count; i++)
                    _vertexHandles[i].gameObject.SetActive(false);
                return;
            }

            ShapeData shape = shapes[_model.Selection.Index];
            float radius = _model.HandleRadius;

            DrawBounds(shape.Bounds());

            // 크기 핸들은 사각형이라 버텍스와 한눈에 구분된다.
            MapHandleGfx.PlaceDot(_scaleHandle, ScaleHandleAt(shape), radius, _style.Scale);

            Grow(_vertexHandles, shape.Points.Count, "VertexHandle", MapHandleGfx.Circle);

            for (int i = 0; i < _vertexHandles.Count; i++)
            {
                bool used = i < shape.Points.Count;
                _vertexHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                // 마지막으로 고른 점은 크고 밝게.
                bool active = _model.ActiveVertex == i;
                MapHandleGfx.PlaceDot(_vertexHandles[i], shape.Points[i],
                    active ? radius : radius * 0.7f,
                    active ? _style.Selected : _style.Vertex);
            }
        }

        /// <summary>
        /// 크기 조절의 기준 테두리.
        /// 도형보다 얇게 그려 지형과 헷갈리지 않는다.
        /// </summary>
        void DrawBounds(Rect bounds)
        {
            _corners[0] = new Vector2(bounds.xMin, bounds.yMin);
            _corners[1] = new Vector2(bounds.xMax, bounds.yMin);
            _corners[2] = new Vector2(bounds.xMax, bounds.yMax);
            _corners[3] = new Vector2(bounds.xMin, bounds.yMax);

            for (int i = 0; i < _boundsHandles.Length; i++)
                MapHandleGfx.PlaceLine(_boundsHandles[i],
                    new StaticSegment(_corners[i], _corners[(i + 1) % _corners.Length]),
                    _style.Bounds, MapHandleGfx.LineWidth * 0.25f);
        }

        /// <summary>
        /// 고른 것만 색을 입힌다. 안 고른 것은 넘겨받은 색
        /// 그대로다 — 그림이 있는 것은 제 색, 코드로 그리는
        /// 지형은 스타일 색이 온다.
        /// 삽입 모드일 때는 색을 한 번 더 바꿔,
        /// 누르기 전에 무엇이 일어날지 알린다.
        /// </summary>
        Color Tint(Color normal, MapHandleKind kind, int index)
        {
            if (!_model.Selection.Is(kind, index)) return normal;

            return kind == MapHandleKind.Terrain && _model.InsertReady
                ? _style.Insert
                : _style.Selected;
        }

        /// <summary>
        /// 편집이 멈추면 표시도 멈춰야 한다.
        /// 계층 구조에 기대면 오브젝트를 옮기는 순간 깨진다.
        /// </summary>
        public void HideAll()
        {
            _startHandle.gameObject.SetActive(false);
            _goalHandle.gameObject.SetActive(false);
            _scaleHandle.gameObject.SetActive(false);
            _reachHandle.gameObject.SetActive(false);

            for (int i = 0; i < _boundsHandles.Length; i++)
                _boundsHandles[i].gameObject.SetActive(false);

            Hide(_vertexHandles);
            Hide(_starHandles);
            Hide(_deviceHandles);
            Hide(_terrainHandles);
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
