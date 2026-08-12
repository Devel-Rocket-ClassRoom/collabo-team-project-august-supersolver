using System.Collections.Generic;
using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// UnityEngine 에도 같은 이름이 있다(SystemInfo.deviceType).
using DeviceType = PPS.Core.DeviceType;

namespace PPS.MapEditor
{
    /// <summary>
    /// 시작·목표·별·지형을 화면에 띄우고 끌어서 옮긴다.
    /// 편집 결과는 곧바로 레벨 데이터에 들어간다.
    /// </summary>
    public sealed class MapEditHandles : MonoBehaviour
    {
        /// 집을 수 있는 반지름(dp). 48dp 타겟의 절반이다.
        const float PickRadiusDp = 24f;

        /// 기획이 정한 별 개수. 데이터에는 제한이 없다.
        const int MaxStars = 3;

        /// 새로 놓는 지형 도형의 크기.
        const float ShapeSize = 1f;

        /// 도형이 점으로 뭉개지지 않는 최소 크기.
        const float MinShapeSize = 0.1f;

        /// <summary>
        /// 새 버텍스가 기존 점과 이만큼은 떨어져야 한다(dp).
        /// 겹쳐 꽂히면 길이 0 짜리 선분이 생긴다.
        /// </summary>
        const float MinVertexGapDp = 12f;

        /// <summary>
        /// 한 번에 도는 각도(도).
        /// 자유 회전은 손가락으로 맞추기 어렵고,
        /// 45도는 완만한 경사를 못 만든다.
        /// </summary>
        const float RotateStep = 15f;

        /// <summary>
        /// 붙여넣은 것을 원본에서 밀어 놓는 거리.
        /// 겹쳐 놓으면 어느 쪽이 새것인지 알 수 없다.
        /// </summary>
        static readonly Vector2 PasteOffset = new Vector2(0.6f, -0.6f);

        [SerializeField] MapEditSession _session;
        [SerializeField] CanvasCameraFitter _fitter;
        [SerializeField] ToolPalette _palette;
        [SerializeField] MapEditHistory _history;

        /// 탭 번호. 씬의 탭 순서와 맞춰야 한다.
        [SerializeField] int _terrainTab = 1;
        [SerializeField] int _deviceTab = 2;

        [SerializeField] Color _startColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        [SerializeField] Color _starColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);
        [SerializeField] Color _terrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);
        [SerializeField] Color _bombColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
        [SerializeField] Color _fragColor = new Color32(0x8A, 0x2B, 0x2B, 0xFF);
        [SerializeField] Color _spikeColor = new Color32(0x6B, 0x1F, 0x1F, 0xFF);
        [SerializeField] Color _windColor = new Color32(0x2E, 0x86, 0xA8, 0xFF);
        [SerializeField] Color _blastColor = new Color32(0xFF, 0x6B, 0x2C, 0x30);
        [SerializeField] Color _selectedColor = Color.white;
        [SerializeField] Color _vertexColor = new Color32(0x3D, 0x8B, 0xFF, 0xFF);
        [SerializeField] Color _boundsColor = new Color32(0x9A, 0x9A, 0x9A, 0x99);
        [SerializeField] Color _scaleColor = new Color32(0xFF, 0x6B, 0x2C, 0xFF);
        [SerializeField] Color _insertColor = new Color32(0x3D, 0x8B, 0xFF, 0xFF);

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;
        SpriteRenderer _scaleHandle;
        SpriteRenderer _blastHandle;

        /// 크기 조절의 기준이 되는 테두리 네 변.
        readonly SpriteRenderer[] _boundsHandles = new SpriteRenderer[4];

        /// 테두리를 그리는 임시 버퍼.
        readonly Vector2[] _corners = new Vector2[4];
        readonly List<SpriteRenderer> _vertexHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _deviceHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrainHandles = new List<SpriteRenderer>();

        Selection _selected = Selection.None;
        bool _dragging;

        /// <summary>
        /// 도형 안을 고치는 상태. 버텍스와 크기 핸들이
        /// 이때만 나온다 — 늘 떠 있으면 도형을 집을 수 없다.
        /// </summary>
        bool _editMode;

        /// <summary>
        /// 누르면 점을 옮기지 않고 새로 꽂는 상태.
        /// 우클릭·시프트는 시뮬레이터와 실기기에 없어
        /// 모드로 가른다.
        /// </summary>
        bool _insertMode;

        /// 이번 드래그가 무엇을 움직이는가.
        GrabKind _grab = GrabKind.None;

        /// 버텍스를 잡았을 때의 점 번호.
        int _grabVertex = -1;

        /// 강조해 보여줄 점. 방금 꽂았거나 잡고 있는 것.
        int _activeVertex = -1;

        /// 직전 프레임의 스테이지. 갈아끼워졌는지 본다.
        StageData _lastStage;

        /// 드래그 중 직전 손가락 위치. 이동량을 여기서 낸다.
        Vector2 _dragFrom;

        /// <summary>
        /// 다음에 놓을 도형의 각도(도).
        /// 놓을 때마다 다시 맞추지 않도록 남겨둔다.
        /// </summary>
        float _placeAngle;

        /// 복사해 둔 것의 종류. None 이면 비어 있다.
        HandleKind _clipKind = HandleKind.None;

        Vector2 _clipStar;
        ShapeData _clipShape;
        DeviceData _clipDevice;

        /// 도형을 선분으로 굽는 임시 버퍼.
        /// 매번 새로 만들면 프레임마다 할당이 생긴다.
        readonly List<StaticSegment> _scratch = new List<StaticSegment>();

        /// 이번 드래그의 스냅샷을 이미 남겼는가.
        /// 끄는 내내 남기면 되돌리기가 한 픽셀씩 간다.
        bool _dragRecorded;

        void Awake()
        {
            _startHandle = CreateHandle("StartHandle", MapHandleGfx.Circle);
            _goalHandle = CreateHandle("GoalHandle", MapHandleGfx.Circle);
            _scaleHandle = CreateHandle("ScaleHandle", MapHandleGfx.Square);
            _blastHandle = CreateHandle("BlastHandle", MapHandleGfx.Circle);

            for (int i = 0; i < _boundsHandles.Length; i++)
                _boundsHandles[i] = CreateHandle($"BoundsHandle_{i}", MapHandleGfx.Square);
        }

        void Update()
        {
            if (_session == null || _fitter == null || !_fitter.IsReady) return;

            DropStaleSelection();
            HandleInput();
            Redraw();
        }

        /// <summary>
        /// 새 맵·불러오기·초기화는 스테이지를 통째로
        /// 갈아끼운다. 고른 번호는 예전 맵 기준이라
        /// 그대로 두면 없는 것을 지우려 든다.
        /// 목록만 짧아지는 경우도 있어 번호까지 함께 본다.
        /// </summary>
        void DropStaleSelection()
        {
            bool swapped = !ReferenceEquals(_lastStage, _session.Current);
            if (!swapped && InRange(_selected)) return;

            _lastStage = _session.Current;
            _selected = Selection.None;
            _dragging = false;
            _grab = GrabKind.None;
            _editMode = false;
            _insertMode = false;
            _activeVertex = -1;
        }

        /// <summary>
        /// 고른 번호가 아직 있는 것을 가리키는가.
        /// 상단바 버튼은 아무 때나 눌리므로 여기서 한 번에 막는다.
        /// </summary>
        bool InRange(Selection selection)
        {
            var level = _session.Current.Level;

            switch (selection.Kind)
            {
                case HandleKind.Star:
                    return selection.Index < level.Stars.Count;
                case HandleKind.Device:
                    return selection.Index < level.Devices.Count;
                case HandleKind.Terrain:
                    return selection.Index < _session.Shapes.Shapes.Count;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 상단바 편집 버튼이 부른다.
        /// 고른 도형이 없으면 들어갈 곳이 없다.
        /// </summary>
        public void ToggleEditMode()
        {
            if (_selected.Kind != HandleKind.Terrain)
            {
                _editMode = false;
                _insertMode = false;
                _activeVertex = -1;
                return;
            }

            _editMode = !_editMode;
            if (!_editMode)
            {
                _insertMode = false;
                _activeVertex = -1;
            }
            Debug.Log(_editMode ? "[맵 에디터] 도형 편집 중" : "[맵 에디터] 도형 편집 끝");
        }

        /// <summary>
        /// 상단바 버텍스 추가 버튼이 부른다.
        /// 편집 중이 아니면 넣을 도형이 없다.
        /// </summary>
        public void ToggleInsertMode()
        {
            if (!_editMode || _selected.Kind != HandleKind.Terrain)
            {
                _insertMode = false;
                return;
            }

            _insertMode = !_insertMode;
            Debug.Log(_insertMode ? "[맵 에디터] 버텍스 추가 중" : "[맵 에디터] 버텍스 추가 끝");
        }

        /// <summary>
        /// 상단바 복사 버튼이 부른다.
        /// 시작·목표는 하나씩만 존재해 복사가 성립하지 않는다.
        /// </summary>
        public void CopySelected()
        {
            var level = _session.Current.Level;

            if (!InRange(_selected)) return;

            if (_selected.Kind == HandleKind.Star)
            {
                _clipStar = level.Stars[_selected.Index];
                _clipKind = HandleKind.Star;
            }
            else if (_selected.Kind == HandleKind.Terrain)
            {
                _clipShape = _session.Shapes.Shapes[_selected.Index].Clone();
                _clipKind = HandleKind.Terrain;
            }
            else if (_selected.Kind == HandleKind.Device)
            {
                _clipDevice = level.Devices[_selected.Index];
                _clipKind = HandleKind.Device;
            }
            else
            {
                return;
            }

            Debug.Log($"[맵 에디터] 복사: {_clipKind}");
        }

        /// <summary>
        /// 상단바 붙여넣기 버튼이 부른다.
        /// 붙인 것을 고른 채로 둬서 바로 옮길 수 있게 한다.
        /// </summary>
        public void PasteClipboard()
        {
            var level = _session.Current.Level;

            if (_clipKind == HandleKind.None) return;

            if (_clipKind == HandleKind.Star)
            {
                if (level.Stars.Count >= MaxStars)
                {
                    Debug.Log($"[맵 에디터] 별은 {MaxStars} 개까지다.");
                    return;
                }

                Record();

                Vector2 shift = ClampDelta(PasteOffset, _clipStar, _clipStar);
                level.Stars.Add(_clipStar + shift);
                _selected = new Selection(HandleKind.Star, level.Stars.Count - 1);
            }
            else if (_clipKind == HandleKind.Device)
            {
                Record();

                DeviceData copy = _clipDevice;
                copy.Position += ClampDelta(PasteOffset, copy.Position, copy.Position);

                level.Devices.Add(copy);
                _selected = new Selection(HandleKind.Device, level.Devices.Count - 1);
            }
            else if (_clipKind == HandleKind.Terrain)
            {
                Record();

                ShapeData copy = _clipShape.Clone();
                Rect bounds = copy.Bounds();
                copy.Move(ClampDelta(PasteOffset, bounds.min, bounds.max));

                var shapes = _session.Shapes.Shapes;
                shapes.Add(copy);
                _session.Bake();

                _selected = new Selection(HandleKind.Terrain, shapes.Count - 1);
            }
        }

        /// <summary>
        /// 상단바 회전 버튼이 부른다.
        /// 고른 지형이 있으면 그것을, 없으면 다음에 놓을
        /// 각도를 돌린다. 시작·목표·별은 원이라 안 돈다.
        /// </summary>
        public void RotateSelected()
        {
            if (InRange(_selected) && HasAngle(_selected))
            {
                Record();

                var devices = _session.Current.Level.Devices;
                DeviceData device = devices[_selected.Index];
                device.Angle += RotateStep;
                devices[_selected.Index] = device;
                return;
            }

            if (_selected.Kind != HandleKind.Terrain || !InRange(_selected))
            {
                _placeAngle += RotateStep;
                Debug.Log($"[맵 에디터] 놓을 각도: {_placeAngle % 360f:F0}도");
                return;
            }

            Record();

            ShapeData shape = _session.Shapes.Shapes[_selected.Index];

            // 원은 돌려도 같은 모양이다.
            if (shape.Kind != ShapeKind.Circle)
            {
                Vector2 center = shape.Center();
                for (int i = 0; i < shape.Points.Count; i++)
                    shape.Points[i] = Rotate(shape.Points[i] - center, RotateStep) + center;

                // 돌다가 밖으로 나가면 안으로 밀어 넣는다.
                Rect bounds = shape.Bounds();
                shape.Move(ClampDelta(Vector2.zero, bounds.min, bounds.max));
            }

            _session.Bake();
        }

        /// <summary>
        /// 고른 것이 방향을 가진 장치인가.
        /// 폭탄·가시는 돌려도 달라지는 것이 없다.
        /// </summary>
        bool HasAngle(Selection selection)
        {
            if (selection.Kind != HandleKind.Device) return false;

            return _session.Current.Level.Devices[selection.Index].Type == DeviceType.Wind;
        }

        /// <summary>
        /// 상단바 좌우대칭 버튼이 부른다.
        /// 고른 것의 중심을 축으로 뒤집는다 — 맵 전체가 아니라
        /// 하나만 뒤집어야 다른 배치가 흐트러지지 않는다.
        /// </summary>
        public void MirrorSelected()
        {
            if (InRange(_selected) && HasAngle(_selected))
            {
                Record();

                var devices = _session.Current.Level.Devices;
                DeviceData device = devices[_selected.Index];

                // 좌우를 뒤집으면 오른쪽 성분만 부호가 바뀐다.
                device.Angle = 180f - device.Angle;
                devices[_selected.Index] = device;
                return;
            }

            if (_selected.Kind != HandleKind.Terrain || !InRange(_selected))
            {
                _placeAngle = 180f - _placeAngle;
                Debug.Log($"[맵 에디터] 놓을 각도: {_placeAngle % 360f:F0}도");
                return;
            }

            ShapeData shape = _session.Shapes.Shapes[_selected.Index];

            // 원은 뒤집어도 같은 모양이다.
            if (shape.Kind == ShapeKind.Circle) return;

            Record();

            float axis = shape.Center().x;
            for (int i = 0; i < shape.Points.Count; i++)
            {
                Vector2 point = shape.Points[i];
                shape.Points[i] = new Vector2(axis * 2f - point.x, point.y);
            }

            _session.Bake();
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// 상단바 삭제 버튼이 부른다.
        /// 시작·목표는 레벨의 필수 요소라 지우지 않는다.
        /// </summary>
        public void DeleteSelected()
        {
            var level = _session.Current.Level;

            if (_selected.Kind != HandleKind.Star
                && _selected.Kind != HandleKind.Terrain
                && _selected.Kind != HandleKind.Device) return;

            if (!InRange(_selected)) return;

            Record();

            if (_selected.Kind == HandleKind.Star)
            {
                level.Stars.RemoveAt(_selected.Index);
            }
            else if (_selected.Kind == HandleKind.Device)
            {
                level.Devices.RemoveAt(_selected.Index);
            }
            else
            {
                // 도형에 속한 선분이 전부 함께 사라진다.
                _session.Shapes.Shapes.RemoveAt(_selected.Index);
                _session.Bake();
            }

            _selected = Selection.None;
            _dragging = false;
        }

        void HandleInput()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 world = _fitter.ScreenToWorld(pointer.position.ReadValue());

            if (pointer.press.wasPressedThisFrame && !OverUI())
            {
                _grab = GrabKind.None;

                // 삽입 모드에서는 삽입이 핸들보다 먼저다.
                // 핸들을 먼저 보면 점 주변이 전부 옮기기로
                // 잡혀 새 점을 넣을 자리가 남지 않는다.
                bool insertMode = InsertReady();
                bool inserted = insertMode && InsertVertexAt(world);

                if (!insertMode)
                {
                    // 편집 모드에서는 도형 안의 핸들이 먼저다.
                    // 도형보다 작아서 뒤로 밀리면 절대 못 잡는다.
                    _grab = PickEditHandle(world);
                }

                if (!insertMode && _grab == GrabKind.None)
                {
                    Selection hit = Pick(world);

                    // 다른 도형을 고르면 편집 모드에서 빠진다.
                    if (hit.Kind != _selected.Kind || hit.Index != _selected.Index)
                    {
                        _editMode = false;
                        _insertMode = false;
                        _activeVertex = -1;
                    }

                    _selected = hit;
                    if (_selected.Kind == HandleKind.None) _selected = Place(world);

                    _grab = _selected.Kind == HandleKind.None ? GrabKind.None : GrabKind.Object;
                }

                _dragging = _grab != GrabKind.None;
                _dragFrom = world;

                // 삽입은 이미 스냅샷을 남겼다.
                // 여기서 풀면 이어지는 드래그가 하나 더 남긴다.
                _dragRecorded = inserted;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _dragging = false;
                _grab = GrabKind.None;
            }

            if (_dragging && pointer.press.isPressed) Drag(world);
        }

        /// 지금 누르면 버텍스가 꽂히는가.
        bool InsertReady() =>
            _insertMode && _editMode && _selected.Kind == HandleKind.Terrain;

        /// <summary>
        /// 편집 중인 선 위에 새 버텍스를 넣는다.
        /// 원은 첫 삽입 때 현재 외형 그대로 폐곡선이 된다.
        /// </summary>
        /// <returns>넣었으면 true. 이어서 그 점이 끌린다.</returns>
        bool InsertVertexAt(Vector2 world)
        {
            if (!_editMode || _selected.Kind != HandleKind.Terrain) return false;

            var shapes = _session.Shapes.Shapes;
            if (_selected.Index < 0 || _selected.Index >= shapes.Count) return false;

            ShapeData shape = shapes[_selected.Index];
            bool convertCircle = shape.Kind == ShapeKind.Circle;

            // 원본은 클릭이 유효한지 확인할 때까지 건드리지 않는다.
            // 빈 곳을 우클릭했는데 원이 폴리곤으로 바뀌면 안 된다.
            ShapeData editable = convertCircle ? shape.Clone() : shape;
            if (convertCircle) editable.ConvertCircleToPolygon(ShapeBaker.CircleSegments);

            float minGap = new DeviceUnits(Screen.dpi).ToPixels(MinVertexGapDp)
                / _fitter.PixelsPerUnit;

            if (!editable.TryFindVertexInsertion(
                    world, PickRadiusWorld(), minGap, out int insertAt, out Vector2 position))
                return false;

            Record();
            if (convertCircle)
            {
                shapes[_selected.Index] = editable;
                shape = editable;
            }

            shape.Points.Insert(insertAt, position);
            _activeVertex = insertAt;
            _grabVertex = insertAt;
            _grab = GrabKind.Vertex;
            _session.Bake();

            return true;
        }

        /// <summary>빈 곳을 눌렀을 때 도구에 맞춰 새로 놓는다.</summary>
        Selection Place(Vector2 world)
        {
            if (_palette == null) return Selection.None;

            if (_palette.SelectedTab == _terrainTab)
            {
                Record();
                return AddTerrain(world, _palette.SelectedItem);
            }

            if (_palette.SelectedTab == _deviceTab)
            {
                Record();
                return AddDeviceItem(world, _palette.SelectedItem);
            }

            return Selection.None;
        }

        /// <summary>
        /// 장치 탭의 항목 하나를 놓는다.
        /// 별은 같은 탭에 있지만 레벨의 별도 목록에 들어간다 —
        /// 코어가 장치가 아니라 수집 목표로 다룬다.
        /// 세기·지연은 기본값으로 둔다. 값을 고치는 UI 는 아직 없다.
        /// </summary>
        Selection AddDeviceItem(Vector2 world, int kind)
        {
            var devices = _session.Current.Level.Devices;

            switch (kind)
            {
                case 0: // 별
                    return AddStar(world);

                case 1: // 폭탄
                    devices.Add(new DeviceData
                    {
                        Type = DeviceType.Bomb,
                        Position = world,

                        // 좁고 세게. 넓고 약하면 어디에 놓아도
                        // 비슷하게 밀려 배치가 퍼즐이 되지 않는다.
                        Radius = 2f,
                        Power = 11f,
                        DelaySteps = 30,

                        // 흔들림은 0 이다. 값이 있으면 발동 시점이
                        // 시드에 따라 달라져 레벨을 읽기 어렵다.
                        JitterSteps = 0,
                    });
                    break;

                case 3: // 가시
                    devices.Add(new DeviceData
                    {
                        Type = DeviceType.Spike,
                        Position = world,
                        Radius = 0.3f,
                    });
                    break;

                case 4: // 바람 구역
                    devices.Add(new DeviceData
                    {
                        Type = DeviceType.Wind,
                        Position = world,
                        Radius = 2f,

                        // 가속도(m/s²). 중력의 절반쯤이라
                        // 공을 띄우지는 못하고 궤도만 휜다.
                        Power = 5f,
                        Angle = _placeAngle,
                    });
                    break;

                case 2: // 파편 폭탄
                    devices.Add(new DeviceData
                    {
                        Type = DeviceType.FragBomb,

                        // 반경 0 이다. 파편만 뿌리고 밀어내지 않는다.
                        Position = world,
                        Radius = 0f,
                        Power = 6f,
                        DelaySteps = 30,
                        JitterSteps = 0,
                    });
                    break;

                default:
                    return Selection.None;
            }

            return new Selection(HandleKind.Device, devices.Count - 1);
        }

        void Record() => _history?.BeginEdit();

        /// <summary>
        /// 크기 핸들을 버텍스보다 먼저 본다.
        /// 겹쳤을 때 도형이 찌그러지는 것보다
        /// 크기가 변하는 편이 되돌리기 쉽다.
        /// </summary>
        GrabKind PickEditHandle(Vector2 world)
        {
            if (!_editMode || _selected.Kind != HandleKind.Terrain) return GrabKind.None;

            ShapeData shape = _session.Shapes.Shapes[_selected.Index];
            float reach = HandleRadius();

            if (Vector2.Distance(world, ScaleHandleAt(shape)) <= reach) return GrabKind.Scale;

            for (int i = 0; i < shape.Points.Count; i++)
            {
                if (Vector2.Distance(world, shape.Points[i]) > reach) continue;

                _grabVertex = i;
                _activeVertex = i;
                return GrabKind.Vertex;
            }

            return GrabKind.None;
        }

        /// <summary>
        /// 크기 핸들 자리. 테두리의 오른쪽 위 모서리다 —
        /// 허공에 뜬 점이 아니라 무엇을 잡는지 보인다.
        /// </summary>
        static Vector2 ScaleHandleAt(ShapeData shape) => shape.Bounds().max;

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
                    _boundsColor, MapHandleGfx.LineWidth * 0.25f);
        }

        /// 핸들의 화면상 크기는 기기와 무관해야 한다.
        float HandleRadius() => PickRadiusWorld() * 0.5f;

        /// <summary>
        /// 손가락에 제일 가까운 것 하나만 고른다.
        /// 겹쳐 있을 때 둘 다 잡히면 엉뚱한 게 끌린다.
        /// </summary>
        Selection Pick(Vector2 world)
        {
            var level = _session.Current.Level;

            var best = Selection.None;
            float bestDist = PickRadiusWorld();

            Closer(ref best, ref bestDist, Vector2.Distance(world, level.BallStart),
                new Selection(HandleKind.Start, 0));
            Closer(ref best, ref bestDist, Vector2.Distance(world, level.GoalPosition),
                new Selection(HandleKind.Goal, 0));

            for (int i = 0; i < level.Stars.Count; i++)
                Closer(ref best, ref bestDist, Vector2.Distance(world, level.Stars[i]),
                    new Selection(HandleKind.Star, i));

            for (int i = 0; i < level.Devices.Count; i++)
                Closer(ref best, ref bestDist, Vector2.Distance(world, level.Devices[i].Position),
                    new Selection(HandleKind.Device, i));

            // 도형은 점이 아니라 선이라 선까지의 거리로 잰다.
            // 변 하나만 눌러도 도형 전체가 잡혀야 한다.
            var shapes = _session.Shapes.Shapes;
            for (int i = 0; i < shapes.Count; i++)
                Closer(ref best, ref bestDist, DistanceToShape(world, shapes[i]),
                    new Selection(HandleKind.Terrain, i));

            return best;
        }

        static void Closer(ref Selection best, ref float bestDist, float dist, Selection selection)
        {
            if (dist > bestDist) return;

            bestDist = dist;
            best = selection;
        }

        /// <summary>도형을 이루는 선분 중 가장 가까운 거리.</summary>
        float DistanceToShape(Vector2 point, ShapeData shape)
        {
            _scratch.Clear();
            ShapeBaker.Append(shape, _scratch);

            float best = float.PositiveInfinity;
            for (int i = 0; i < _scratch.Count; i++)
            {
                float dist = DistanceToSegment(point, _scratch[i].A, _scratch[i].B);
                if (dist < best) best = dist;
            }

            return best;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-6f) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return Vector2.Distance(point, a + ab * t);
        }

        Selection AddStar(Vector2 world)
        {
            var stars = _session.Current.Level.Stars;
            if (stars.Count >= MaxStars) return Selection.None;

            stars.Add(world);
            return new Selection(HandleKind.Star, stars.Count - 1);
        }

        /// <summary>
        /// 도형 하나를 통째로 넣는다.
        /// 연결 순서를 들고 있어야 나중에 변 하나를 눌러도
        /// 도형 전체를 집을 수 있다.
        /// </summary>
        Selection AddTerrain(Vector2 center, int kind)
        {
            ShapeData shape = MakeShape(center, kind);
            if (shape == null) return Selection.None;

            // 놓을 때 각도를 먹인다.
            for (int i = 0; i < shape.Points.Count; i++)
                shape.Points[i] = Rotate(shape.Points[i] - center, _placeAngle) + center;

            var shapes = _session.Shapes.Shapes;
            shapes.Add(shape);
            _session.Bake();

            return new Selection(HandleKind.Terrain, shapes.Count - 1);
        }

        static ShapeData MakeShape(Vector2 c, int kind)
        {
            float r = ShapeSize;

            switch (kind)
            {
                case 0: // 직선
                    return new ShapeData
                    {
                        Kind = ShapeKind.Polyline,
                        Points = new List<Vector2> { c + Vector2.left * r, c + Vector2.right * r },
                    };

                case 1: // 사각형
                    return new ShapeData
                    {
                        Kind = ShapeKind.Polygon,
                        Points = new List<Vector2>
                        {
                            c + new Vector2(-r, -r), c + new Vector2(r, -r),
                            c + new Vector2(r, r), c + new Vector2(-r, r),
                        },
                    };

                case 2: // 정삼각형
                    var points = new List<Vector2>();
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Mathf.PI * 0.5f + 2f * Mathf.PI * i / 3f;
                        points.Add(c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
                    }
                    return new ShapeData { Kind = ShapeKind.Polygon, Points = points };

                case 3: // 원
                    return new ShapeData
                    {
                        Kind = ShapeKind.Circle,
                        Points = new List<Vector2> { c },
                        Radius = r,
                    };

                default:
                    return null;
            }
        }

        void Drag(Vector2 world)
        {
            Vector2 delta = world - _dragFrom;
            if (delta == Vector2.zero) return;

            if (!_dragRecorded)
            {
                _dragRecorded = true;
                Record();
            }

            if (_grab == GrabKind.Vertex || _grab == GrabKind.Scale)
            {
                DragEditHandle(world);
                _dragFrom = world;
                return;
            }

            var level = _session.Current.Level;

            switch (_selected.Kind)
            {
                case HandleKind.Start:
                    level.BallStart = MovePoint(level.BallStart, ref delta);
                    break;
                case HandleKind.Goal:
                    level.GoalPosition = MovePoint(level.GoalPosition, ref delta);
                    break;
                case HandleKind.Star:
                    level.Stars[_selected.Index] = MovePoint(level.Stars[_selected.Index], ref delta);
                    break;
                case HandleKind.Device:
                    // 구조체라 꺼내 고치고 도로 넣는다.
                    DeviceData device = level.Devices[_selected.Index];
                    device.Position = MovePoint(device.Position, ref delta);
                    level.Devices[_selected.Index] = device;
                    break;
                case HandleKind.Terrain:
                    ShapeData shape = _session.Shapes.Shapes[_selected.Index];
                    Rect bounds = shape.Bounds();
                    delta = ClampDelta(delta, bounds.min, bounds.max);

                    shape.Move(delta);
                    _session.Bake();
                    break;
            }

            // 잘린 만큼만 따라간다. 안 그러면 한계에 닿은 뒤
            // 손가락을 되돌릴 때 바로 안 붙는다.
            _dragFrom += delta;
        }

        /// <summary>
        /// 버텍스 하나만 옮기거나 도형 전체를 늘린다.
        /// 어느 쪽이든 연결 순서는 그대로라 선분이
        /// 곧바로 다시 구워진다.
        /// </summary>
        void DragEditHandle(Vector2 world)
        {
            ShapeData shape = _session.Shapes.Shapes[_selected.Index];
            Vector2 center = shape.Center();

            if (_grab == GrabKind.Scale)
            {
                // 중심에서 멀어진 비율만큼 늘린다.
                // 절대 크기로 잡으면 잡은 지점이 튄다.
                float before = Vector2.Distance(_dragFrom, center);
                float after = Vector2.Distance(world, center);

                if (before > MinShapeSize) shape.Scale(after / before);
            }
            else if (_grabVertex >= 0 && _grabVertex < shape.Points.Count)
            {
                shape.Points[_grabVertex] = ClampPoint(world);
            }

            _session.Bake();
        }

        Vector2 ClampPoint(Vector2 world)
        {
            Rect area = _fitter.PlayArea;
            return new Vector2(
                Mathf.Clamp(world.x, area.xMin, area.xMax),
                Mathf.Clamp(world.y, area.yMin, area.yMax));
        }

        Vector2 MovePoint(Vector2 point, ref Vector2 delta)
        {
            delta = ClampDelta(delta, point, point);
            return point + delta;
        }

        /// <summary>
        /// 두 끝이 모두 한계 안에 남도록 이동량을 자른다.
        /// 좌표를 자르면 선분 길이가 바뀐다.
        /// </summary>
        Vector2 ClampDelta(Vector2 delta, Vector2 a, Vector2 b)
        {
            Rect area = _fitter.PlayArea;

            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);
            float minY = Mathf.Min(a.y, b.y);
            float maxY = Mathf.Max(a.y, b.y);

            return new Vector2(
                Mathf.Clamp(delta.x, area.xMin - minX, area.xMax - maxX),
                Mathf.Clamp(delta.y, area.yMin - minY, area.yMax - maxY));
        }

        void Redraw()
        {
            var level = _session.Current.Level;

            MapHandleGfx.PlaceDot(_startHandle, level.BallStart, LevelData.BallRadius,
                Tint(_startColor, HandleKind.Start, 0));
            MapHandleGfx.PlaceDot(_goalHandle, level.GoalPosition, LevelData.GoalRadius,
                Tint(_goalColor, HandleKind.Goal, 0));

            // 개수가 변한다. 남는 핸들은 끄고 다시 쓴다.
            Grow(_starHandles, level.Stars.Count, "StarHandle", MapHandleGfx.Star);
            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < level.Stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (used)
                    MapHandleGfx.PlaceDot(_starHandles[i], level.Stars[i],
                        LevelData.StarCaptureRadius / MapHandleGfx.StarSpan,
                        Tint(_starColor, HandleKind.Star, i));
            }

            DrawDevices();
            DrawShapes();
            DrawEditHandles();
        }

        /// <summary>
        /// 장치를 종류에 맞는 모양으로 그린다.
        /// 폭발 반경은 고른 것만 — 늘 그리면 원이 겹쳐 어지럽다.
        /// </summary>
        void DrawDevices()
        {
            var devices = _session.Current.Level.Devices;

            Grow(_deviceHandles, devices.Count, "DeviceHandle", MapHandleGfx.Bomb);

            for (int i = 0; i < _deviceHandles.Count; i++)
            {
                bool used = i < devices.Count;
                _deviceHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                _deviceHandles[i].sprite = DeviceSprite(devices[i].Type);

                MapHandleGfx.PlaceDot(_deviceHandles[i], devices[i].Position,
                    DeviceDrawRadius(devices[i]),
                    Tint(DeviceColor(devices[i].Type), HandleKind.Device, i),
                    devices[i].Type == DeviceType.Wind ? devices[i].Angle : 0f);
            }

            DrawBlastRadius(devices);
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

        /// <summary>
        /// 화면에 그릴 반지름. 몸통이 스프라이트를 꽉 채우지
        /// 않으므로 그 비율만큼 되돌려 키운다.
        /// </summary>
        static float DeviceDrawRadius(in DeviceData device)
        {
            switch (device.Type)
            {
                case DeviceType.FragBomb:
                    return BombDevice.BodyRadius / MapHandleGfx.FragBombBodySpan;

                case DeviceType.Spike:
                    return Mathf.Max(device.Radius, SpikeDevice.MinRadius)
                        / MapHandleGfx.SpikeBodySpan;

                // 바람은 화살표라 몸 크기가 없다. 구역 안에
                // 들어갈 만한 크기로 그린다.
                case DeviceType.Wind:
                    return Mathf.Max(device.Radius * 0.5f, 0.3f);

                default:
                    return BombDevice.BodyRadius / MapHandleGfx.BombBodySpan;
            }
        }

        /// <summary>
        /// 고른 장치가 미치는 범위. 가시는 몸이 곧 범위라
        /// 겹쳐 그리면 뜻이 없다.
        /// </summary>
        void DrawBlastRadius(List<DeviceData> devices)
        {
            bool on = _selected.Kind == HandleKind.Device
                && _selected.Index < devices.Count
                && devices[_selected.Index].Type != DeviceType.Spike
                && devices[_selected.Index].Radius > 0f;

            _blastHandle.gameObject.SetActive(on);
            if (!on) return;

            DeviceData device = devices[_selected.Index];
            MapHandleGfx.PlaceDot(_blastHandle, device.Position, device.Radius, _blastColor);
        }

        /// <summary>
        /// 편집 모드에서만 버텍스·중심·크기 핸들을 띄운다.
        /// 표시일 뿐이라 물리 데이터에 닿지 않는다.
        /// </summary>
        void DrawEditHandles()
        {
            bool on = _editMode && _selected.Kind == HandleKind.Terrain;

            _scaleHandle.gameObject.SetActive(on);
            for (int i = 0; i < _boundsHandles.Length; i++)
                _boundsHandles[i].gameObject.SetActive(on);

            if (!on)
            {
                for (int i = 0; i < _vertexHandles.Count; i++)
                    _vertexHandles[i].gameObject.SetActive(false);
                return;
            }

            ShapeData shape = _session.Shapes.Shapes[_selected.Index];
            float radius = HandleRadius();

            DrawBounds(shape.Bounds());

            // 크기 핸들은 사각형이라 버텍스와 한눈에 구분된다.
            MapHandleGfx.PlaceDot(_scaleHandle, ScaleHandleAt(shape), radius, _scaleColor);

            Grow(_vertexHandles, shape.Points.Count, "VertexHandle", MapHandleGfx.Circle);

            for (int i = 0; i < _vertexHandles.Count; i++)
            {
                bool used = i < shape.Points.Count;
                _vertexHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                // 마지막으로 고른 점은 크고 밝게.
                bool active = _activeVertex == i;
                MapHandleGfx.PlaceDot(_vertexHandles[i], shape.Points[i],
                    active ? radius : radius * 0.7f,
                    active ? _selectedColor : _vertexColor);
            }
        }

        /// <summary>
        /// 도형마다 색을 정하고 그 도형의 선분을 그린다.
        /// 구운 지형을 그대로 그리면 어느 선분이 어느
        /// 도형의 것인지 알 수 없어 선택 표시가 안 된다.
        /// </summary>
        void DrawShapes()
        {
            var shapes = _session.Shapes.Shapes;

            Grow(_terrainHandles, _session.Current.Level.Terrain.Count,
                "TerrainHandle", MapHandleGfx.Square);

            int handle = 0;

            for (int i = 0; i < shapes.Count; i++)
            {
                Color color = Tint(_terrainColor, HandleKind.Terrain, i);

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

        void Grow(List<SpriteRenderer> handles, int need, string name, Sprite sprite)
        {
            while (handles.Count < need)
                handles.Add(CreateHandle($"{name}_{handles.Count}", sprite));
        }

        /// <summary>
        /// 고른 것을 밝힌다. 삽입 모드일 때는 색을 한 번 더
        /// 바꿔, 누르기 전에 무엇이 일어날지 알린다.
        /// </summary>
        Color Tint(Color normal, HandleKind kind, int index)
        {
            if (_selected.Kind != kind || _selected.Index != index) return normal;

            return kind == HandleKind.Terrain && InsertReady() ? _insertColor : _selectedColor;
        }

        float PickRadiusWorld()
        {
            float pixels = new DeviceUnits(Screen.dpi).ToPixels(PickRadiusDp);
            return pixels / _fitter.PixelsPerUnit;
        }

        /// 도구 버튼을 눌렀는데 맵이 반응하면 안 된다.
        static bool OverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        SpriteRenderer CreateHandle(string name, Sprite sprite) =>
            MapHandleGfx.Create(transform, name, sprite);

        /// <summary>
        /// 이번 드래그가 움직이는 대상.
        /// 도형 전체와 도형 안의 점을 구분한다.
        /// </summary>
        enum GrabKind
        {
            None,
            Object,
            Vertex,
            Scale,
        }

        enum HandleKind
        {
            None,
            Start,
            Goal,
            Star,
            Terrain,
            Device,
        }

        /// <summary>
        /// 고른 대상. 별과 지형은 개수가 변해서
        /// 종류만으로는 특정할 수 없다.
        /// </summary>
        readonly struct Selection
        {
            public readonly HandleKind Kind;
            public readonly int Index;

            public Selection(HandleKind kind, int index)
            {
                Kind = kind;
                Index = index;
            }

            public static Selection None => new Selection(HandleKind.None, -1);
        }
    }
}
