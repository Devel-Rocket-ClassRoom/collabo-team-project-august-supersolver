using System.Collections.Generic;
using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

        [SerializeField] int _starTab = 3;
        [SerializeField] int _terrainTab = 1;

        [SerializeField] Color _startColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        [SerializeField] Color _starColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);
        [SerializeField] Color _terrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);
        [SerializeField] Color _selectedColor = Color.white;

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;
        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _terrainHandles = new List<SpriteRenderer>();

        Selection _selected = Selection.None;
        bool _dragging;

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
        StaticSegment _clipSegment;

        void Awake()
        {
            _startHandle = CreateHandle("StartHandle", MapHandleGfx.Circle);
            _goalHandle = CreateHandle("GoalHandle", MapHandleGfx.Circle);
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
        /// </summary>
        void DropStaleSelection()
        {
            if (ReferenceEquals(_lastStage, _session.Current)) return;

            _lastStage = _session.Current;
            _selected = Selection.None;
            _dragging = false;
        }

        /// <summary>
        /// 상단바 삭제 버튼이 부른다.
        /// 시작·목표는 레벨의 필수 요소라 지우지 않는다.
        /// </summary>
        /// <summary>
        /// 상단바 복사 버튼이 부른다.
        /// 시작·목표는 하나씩만 존재해 복사가 성립하지 않는다.
        /// </summary>
        public void CopySelected()
        {
            var level = _session.Current.Level;

            if (_selected.Kind == HandleKind.Star)
            {
                _clipStar = level.Stars[_selected.Index];
                _clipKind = HandleKind.Star;
            }
            else if (_selected.Kind == HandleKind.Terrain)
            {
                _clipSegment = level.Terrain[_selected.Index];
                _clipKind = HandleKind.Terrain;
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

            if (_clipKind == HandleKind.Star)
            {
                if (level.Stars.Count >= MaxStars)
                {
                    Debug.Log($"[맵 에디터] 별은 {MaxStars} 개까지다.");
                    return;
                }

                Vector2 shift = ClampDelta(PasteOffset, _clipStar, _clipStar);
                level.Stars.Add(_clipStar + shift);
                _selected = new Selection(HandleKind.Star, level.Stars.Count - 1);
            }
            else if (_clipKind == HandleKind.Terrain)
            {
                Vector2 shift = ClampDelta(PasteOffset, _clipSegment.A, _clipSegment.B);
                level.Terrain.Add(
                    new StaticSegment(_clipSegment.A + shift, _clipSegment.B + shift));
                _selected = new Selection(HandleKind.Terrain, level.Terrain.Count - 1);
            }
        }

        /// <summary>
        /// 상단바 회전 버튼이 부른다.
        /// 고른 지형이 있으면 그것을, 없으면 다음에 놓을
        /// 각도를 돌린다. 시작·목표·별은 원이라 안 돈다.
        /// </summary>
        public void RotateSelected()
        {
            if (_selected.Kind != HandleKind.Terrain)
            {
                _placeAngle += RotateStep;
                Debug.Log($"[맵 에디터] 놓을 각도: {_placeAngle % 360f:F0}도");
                return;
            }

            var terrain = _session.Current.Level.Terrain;
            var segment = terrain[_selected.Index];

            Vector2 center = (segment.A + segment.B) * 0.5f;
            Vector2 a = Rotate(segment.A - center, RotateStep) + center;
            Vector2 b = Rotate(segment.B - center, RotateStep) + center;

            // 돌다가 밖으로 나가면 안으로 밀어 넣는다.
            Vector2 shift = ClampDelta(Vector2.zero, a, b);
            terrain[_selected.Index] = new StaticSegment(a + shift, b + shift);
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        public void DeleteSelected()
        {
            var level = _session.Current.Level;

            if (_selected.Kind == HandleKind.Star) level.Stars.RemoveAt(_selected.Index);
            else if (_selected.Kind == HandleKind.Terrain) level.Terrain.RemoveAt(_selected.Index);
            else return;

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
                _selected = Pick(world);
                if (_selected.Kind == HandleKind.None) _selected = Place(world);

                _dragging = _selected.Kind != HandleKind.None;
                _dragFrom = world;
            }

            if (pointer.press.wasReleasedThisFrame) _dragging = false;

            if (_dragging && pointer.press.isPressed) Drag(world);
        }

        /// <summary>빈 곳을 눌렀을 때 도구에 맞춰 새로 놓는다.</summary>
        Selection Place(Vector2 world)
        {
            if (_palette == null) return Selection.None;

            if (_palette.SelectedTab == _starTab) return AddStar(world);
            if (_palette.SelectedTab == _terrainTab) return AddTerrain(world, _palette.SelectedItem);

            return Selection.None;
        }

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

            // 지형은 점이 아니라 선이라 선까지의 거리로 잰다.
            for (int i = 0; i < level.Terrain.Count; i++)
            {
                var segment = level.Terrain[i];
                Closer(ref best, ref bestDist, DistanceToSegment(world, segment.A, segment.B),
                    new Selection(HandleKind.Terrain, i));
            }

            return best;
        }

        static void Closer(ref Selection best, ref float bestDist, float dist, Selection selection)
        {
            if (dist > bestDist) return;

            bestDist = dist;
            best = selection;
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
        /// 도형을 변으로 쪼개 넣는다. 지형은 선분 목록이라
        /// 사각형도 삼각형도 변이 여러 개인 지형이 된다.
        /// </summary>
        Selection AddTerrain(Vector2 center, int shape)
        {
            Vector2[] points = ShapePoints(center, shape);
            if (points == null) return Selection.None;

            // 놓을 때 각도를 먹인다. 놓고 나면 도형이 변으로
            // 쪼개져 통째로 돌릴 수 없다.
            for (int i = 0; i < points.Length; i++)
                points[i] = Rotate(points[i] - center, _placeAngle) + center;

            var terrain = _session.Current.Level.Terrain;
            int first = terrain.Count;

            for (int i = 0; i + 1 < points.Length; i++)
                terrain.Add(new StaticSegment(points[i], points[i + 1]));

            return new Selection(HandleKind.Terrain, first);
        }

        /// <returns>닫힌 도형은 첫 점을 끝에 한 번 더 넣는다.</returns>
        static Vector2[] ShapePoints(Vector2 c, int shape)
        {
            float r = ShapeSize;

            switch (shape)
            {
                case 0: // 직선
                    return new[] { c + Vector2.left * r, c + Vector2.right * r };

                case 1: // 사각형
                    return new[]
                    {
                        c + new Vector2(-r, -r), c + new Vector2(r, -r),
                        c + new Vector2(r, r), c + new Vector2(-r, r),
                        c + new Vector2(-r, -r),
                    };

                case 2: // 정삼각형
                    var points = new Vector2[4];
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Mathf.PI * 0.5f + 2f * Mathf.PI * i / 3f;
                        points[i] = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    }
                    points[3] = points[0];
                    return points;

                default:
                    return null;
            }
        }

        void Drag(Vector2 world)
        {
            Vector2 delta = world - _dragFrom;
            if (delta == Vector2.zero) return;

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
                case HandleKind.Terrain:
                    var segment = level.Terrain[_selected.Index];
                    delta = ClampDelta(delta, segment.A, segment.B);
                    level.Terrain[_selected.Index] =
                        new StaticSegment(segment.A + delta, segment.B + delta);
                    break;
            }

            // 잘린 만큼만 따라간다. 안 그러면 한계에 닿은 뒤
            // 손가락을 되돌릴 때 바로 안 붙는다.
            _dragFrom += delta;
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
            Grow(_starHandles, level.Stars.Count, "StarHandle", MapHandleGfx.Circle);
            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < level.Stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (used)
                    MapHandleGfx.PlaceDot(_starHandles[i], level.Stars[i],
                        LevelData.StarCaptureRadius, Tint(_starColor, HandleKind.Star, i));
            }

            Grow(_terrainHandles, level.Terrain.Count, "TerrainHandle", MapHandleGfx.Square);
            for (int i = 0; i < _terrainHandles.Count; i++)
            {
                bool used = i < level.Terrain.Count;
                _terrainHandles[i].gameObject.SetActive(used);
                if (used)
                    MapHandleGfx.PlaceLine(_terrainHandles[i], level.Terrain[i],
                        Tint(_terrainColor, HandleKind.Terrain, i));
            }
        }

        void Grow(List<SpriteRenderer> handles, int need, string name, Sprite sprite)
        {
            while (handles.Count < need)
                handles.Add(CreateHandle($"{name}_{handles.Count}", sprite));
        }

        Color Tint(Color normal, HandleKind kind, int index) =>
            _selected.Kind == kind && _selected.Index == index ? _selectedColor : normal;

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

        enum HandleKind
        {
            None,
            Start,
            Goal,
            Star,
            Terrain,
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
