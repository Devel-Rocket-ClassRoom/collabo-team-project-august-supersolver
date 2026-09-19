using System.Collections.Generic;
using PPS.Core;
using PPS.Game;
using UnityEngine;

// UnityEngine 에도 같은 이름이 있다(SystemInfo.deviceType).
using DeviceType = PPS.Core.DeviceType;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 화면을 그린다. 레벨을 읽기만 하고 고치지 않는다 —
    /// 그리는 쪽이 데이터를 건드리면 무엇이 바꿨는지 알 수 없다.
    /// </summary>
    public sealed class MapEditView : MonoBehaviour
    {
        [SerializeField] MapEditStyle _style;
        [SerializeField] MapEditorVisuals _visuals;
        public MapEditorVisuals Visuals => _visuals;
        public MapEditStyle Style => _style;

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;
        SpriteRenderer _scaleHandle;

        /// 고른 장치가 미치는 범위.
        SpriteRenderer _reachHandle;
        SpriteRenderer _reachOutline;

        /// 지우개가 닿는 범위.
        SpriteRenderer _eraserHandle;

        /// 긋는 중인 선. 확정 전이라 도형 목록에 없다.
        readonly List<SpriteRenderer> _strokeHandles = new List<SpriteRenderer>();

        /// 크기 조절의 기준이 되는 테두리 네 변.
        readonly SpriteRenderer[] _boundsHandles = new SpriteRenderer[4];

        /// 테두리를 그리는 임시 버퍼.
        readonly Vector2[] _corners = new Vector2[4];

        readonly List<SpriteRenderer> _vertexHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _deviceHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrainHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _deviceToolHandles = new List<SpriteRenderer>();

        /// 도형을 선분으로 굽는 임시 버퍼.
        /// 매번 새로 만들면 프레임마다 할당이 생긴다.
        readonly List<StaticSegment> _scratch = new List<StaticSegment>();

        /// 이번에 그리는 것. 전달 인자를 메서드마다 나르지 않는다.
        MapDrawModel _model;
        bool _reportedMissingAssets;

        void OnEnable() => HideAll();

        /// <summary>
        /// 크기 핸들 자리. 테두리의 오른쪽 위 모서리다 —
        /// 허공에 뜬 점이 아니라 무엇을 잡는지 보인다.
        /// 집는 쪽과 그리는 쪽이 같은 자리를 봐야 한다.
        /// </summary>
        public static Vector2 ScaleHandleAt(ShapeData shape) => shape.Bounds().max;

        void Awake()
        {
            if (ServiceLocator.TryGet<IThemeRepository>(out var repo) && repo.Asset.MapStyle != null)
                _style = repo.Asset.MapStyle;

            EnsureHandles();
        }

        bool EnsureHandles()
        {
            if (_style == null || _style.Sim == null || _style.Sim.Sprites == null || _visuals == null)
            {
                if (!_reportedMissingAssets)
                    Debug.LogError("MapEditView needs a MapEditStyle with SimStyle and a MapEditorVisuals asset.", this);
                _reportedMissingAssets = true;
                return false;
            }
            _reportedMissingAssets = false;
            if (_startHandle == null) _startHandle = Create("StartHandle", _style.Sim.Sprites.Ball);
            if (_goalHandle == null) _goalHandle = Create("GoalHandle", _style.Sim.Sprites.Goal);
            if (_scaleHandle == null) _scaleHandle = Create("ScaleHandle", _visuals.ResizeHandle);
            if (_reachHandle == null) _reachHandle = Create("ReachHandle", null);
            if (_reachOutline == null) _reachOutline = Create("ReachOutline", null);
            _reachHandle.sortingOrder = -1;
            _reachOutline.sortingOrder = 19;
            if (_eraserHandle == null) _eraserHandle = Create("EraserHandle", _visuals.Eraser);

            for (int i = 0; i < _boundsHandles.Length; i++)
                if (_boundsHandles[i] == null)
                    _boundsHandles[i] = Create($"BoundsHandle_{i}", _visuals.Line);
            return true;
        }

        /// <summary>편집 쪽이 매 프레임 부른다.</summary>
        public void OnDraw(in MapDrawModel model)
        {
            if (model.Level == null || !EnsureHandles()) return;

            _model = model;

            // 이 둘은 늘 있어서 개수로 켜고 끄지 않는다.
            // HideAll 로 꺼진 뒤 스스로 돌아올 길이 여기뿐이다.
            _startHandle.gameObject.SetActive(true);
            _goalHandle.gameObject.SetActive(true);

            MapHandleGfx.PlaceDot(_startHandle, model.Level.BallStart, LevelData.BallRadius,
                Tint(SimStyle.Plain, MapHandleKind.Start, 0));
            MapHandleGfx.PlaceDot(_goalHandle, model.Level.GoalPosition, LevelData.GoalRadius,
                Tint(SimStyle.Plain, MapHandleKind.Goal, 0));

            DrawStars();
            DrawDevices();
            DrawShapes();
            DrawEditHandles();
            DrawDeviceTools();
            DrawStroke();
            DrawEraser();
        }

        /// <summary>
        /// 손이 닿아 있는 동안의 선.
        /// 확정되면 도형이 되어 지형 쪽에서 그려진다.
        /// </summary>
        void DrawStroke()
        {
            var points = _model.Stroke;
            int need = points != null && points.Count >= 2 ? points.Count - 1 : 0;

            Grow(_strokeHandles, need, "StrokeHandle", _visuals.Line);

            for (int i = 0; i < _strokeHandles.Count; i++)
            {
                bool used = i < need;
                _strokeHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceLine(_strokeHandles[i], _visuals.Line,
                    new StaticSegment(points[i], points[i + 1]), _style.Selected);
            }
        }

        /// <summary>
        /// 지우개가 닿는 범위. 어디까지 지워지는지
        /// 보이지 않으면 손가락이 가린 곳을 짐작해야 한다.
        /// </summary>
        void DrawEraser()
        {
            bool on = _model.EraserRadius > 0f;

            _eraserHandle.gameObject.SetActive(on);
            if (!on) return;

            MapHandleGfx.PlaceDot(
                _eraserHandle, _visuals.Eraser, _model.EraserAt, _model.EraserRadius, _style.Scale);
        }

        void DrawStars()
        {
            var stars = _model.Level.Stars;

            // 개수가 변한다. 남는 핸들은 끄고 다시 쓴다.
            Grow(_starHandles, stars.Count, "StarHandle", _style.Sim.Sprites.Star);

            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                MapHandleGfx.PlaceDot(_starHandles[i], stars[i], LevelData.StarCaptureRadius,
                    Tint(SimStyle.Plain, MapHandleKind.Star, i));
            }
        }

        /// <summary>
        /// 장치를 종류에 맞는 모양으로 그린다.
        /// 미치는 범위는 고른 것만 — 늘 그리면 원이 겹쳐 어지럽다.
        /// </summary>
        void DrawDevices()
        {
            var devices = _model.Level.Devices;

            Grow(_deviceHandles, devices.Count, "DeviceHandle", _style.Sim.SpriteOf(DeviceType.Bomb));

            for (int i = 0; i < _deviceHandles.Count; i++)
            {
                bool used = i < devices.Count;
                _deviceHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                _deviceHandles[i].sprite = _style.Sim.SpriteOf(devices[i].Type);

                MapHandleGfx.PlaceDot(_deviceHandles[i], devices[i].Position,
                    devices[i].DrawRadius,
                    Tint(SimStyle.Plain, MapHandleKind.Device, i),
                    SimStyle.AngleOf(devices[i]));
            }

            DrawReach(devices);
        }

        void DrawReach(List<IDeviceData> devices)
        {
            bool on = _model.Selection.Kind == MapHandleKind.Device
                && _model.Selection.Index >= 0
                && _model.Selection.Index < devices.Count
                && MapEditStyle.HasReach(devices[_model.Selection.Index]);

            _reachHandle.gameObject.SetActive(on);
            _reachOutline.gameObject.SetActive(on && _model.DeviceTool != DeviceEditKind.Radius);
            if (!on) return;

            var device = (IHasReach)devices[_model.Selection.Index];
            var art = _style.Sim.VisualOf(devices[_model.Selection.Index].Type);
            MapHandleGfx.PlaceDot(
                _reachHandle, art?.RangeFill, devices[_model.Selection.Index].Position, device.Reach, _style.Reach);
            MapHandleGfx.PlaceDot(_reachOutline, art?.RangeOutline, devices[_model.Selection.Index].Position,
                device.Reach, _style.Scale);
        }

        void DrawDeviceTools()
        {
            Hide(_deviceToolHandles);
            if (_model.Selection.Kind != MapHandleKind.Device
                || _model.Selection.Index < 0 || _model.Selection.Index >= _model.Level.Devices.Count) return;
            var device = _model.Level.Devices[_model.Selection.Index];
            var art = _style.Sim.VisualOf(device.Type);
            Grow(_deviceToolHandles, 6, "DeviceTool", _visuals.ResizeHandle);
            var parameter = DeviceParameterSchema.For(device).Find(_model.DeviceTool);
            bool rotation = _model.DeviceTool == DeviceEditKind.Angle;
            bool ringVisible = parameter != null && _model.DeviceTool != DeviceEditKind.Position;
            float radius = rotation ? DeviceTransformGeometry.RotationRadius(device, _model.HandleRadius)
                : ringVisible ? (float)parameter.Read(device) : 0f;
            var ring = _deviceToolHandles[0];
            ring.gameObject.SetActive(ringVisible);
            var ringArt = rotation ? _visuals.RotationRing : art?.RangeOutline;
            ring.sortingOrder = 20;
            if (ringVisible)
                MapHandleGfx.PlaceDot(ring, ringArt, device.Position, radius, rotation ? _style.Selected : _style.Scale);
            for (int i = 0; i < 4; i++)
            {
                var handle = _deviceToolHandles[i + 1];
                handle.gameObject.SetActive(ringVisible && (!rotation || i == 0));
                var handleArt = rotation ? _visuals.VertexHandle : _visuals.ResizeHandle;
                handle.sortingOrder = 21;
                float angle = rotation ? (float)parameter.Read(device) : i * 90f;
                MapHandleGfx.PlaceDot(handle, handleArt, OnCircle(device.Position, radius, angle),
                    _model.HandleRadius, rotation ? _style.Selected : _style.Scale, rotation ? 0 : angle - 45);
            }
            var arrow = _deviceToolHandles[5];
            arrow.gameObject.SetActive(device is IHasFacing);
            if (device is IHasFacing facing)
            {
                arrow.sortingOrder = 22;
                float distance = rotation ? radius
                    : Mathf.Max(device.DrawRadius + _model.HandleRadius * 3f, _model.HandleRadius * 4f);
                MapHandleGfx.PlaceDot(arrow, art?.DirectionArrow, OnCircle(device.Position, distance, facing.FacingDegrees),
                    _model.HandleRadius * 1.5f, _style.Selected, facing.FacingDegrees);
            }
        }

        static Vector2 OnCircle(Vector2 center, float radius, float degrees) =>
            center + new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * radius;

        /// <summary>
        /// 도형마다 색을 정하고 그 도형의 선분을 그린다.
        /// 구운 지형을 그대로 그리면 어느 선분이 어느
        /// 도형의 것인지 알 수 없어 선택 표시가 안 된다.
        /// </summary>
        void DrawShapes()
        {
            var shapes = _model.Shapes.Shapes;

            Grow(_terrainHandles, _model.Level.Terrain.Count,
                "TerrainHandle", _visuals.Line);

            int handle = 0;

            for (int i = 0; i < shapes.Count; i++)
            {
                Color color = Tint(_style.Sim.Terrain, MapHandleKind.Terrain, i);

                _scratch.Clear();
                ShapeBaker.Append(shapes[i], _scratch);

                for (int s = 0; s < _scratch.Count && handle < _terrainHandles.Count; s++, handle++)
                {
                    _terrainHandles[handle].gameObject.SetActive(true);
                    MapHandleGfx.PlaceLine(_terrainHandles[handle], _visuals.Line, _scratch[s], color);
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
            MapHandleGfx.PlaceDot(_scaleHandle, _visuals.ResizeHandle, ScaleHandleAt(shape), radius, _style.Scale);

            Grow(_vertexHandles, shape.Points.Count, "VertexHandle", _visuals.VertexHandle);

            for (int i = 0; i < _vertexHandles.Count; i++)
            {
                bool used = i < shape.Points.Count;
                _vertexHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                // 마지막으로 고른 점은 크고 밝게.
                bool active = _model.ActiveVertex == i;
                MapHandleGfx.PlaceDot(_vertexHandles[i], _visuals.VertexHandle, shape.Points[i],
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
                MapHandleGfx.PlaceLine(_boundsHandles[i], _visuals.Line,
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
            // 재컴파일로 필드 참조가 초기화돼도 숨긴다.
            foreach (Transform child in transform)
                if (child.GetComponent<SpriteRenderer>() != null)
                    child.gameObject.SetActive(false);
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

        SpriteRenderer Create(string name, Sprite sprite)
        {
            var existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent<SpriteRenderer>(out var renderer))
            {
                renderer.sprite = sprite;
                return renderer;
            }
            return MapHandleGfx.Create(transform, name, sprite);
        }
    }
}
