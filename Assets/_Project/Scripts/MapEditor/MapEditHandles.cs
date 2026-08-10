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

        /// 지형을 그리는 굵기. 표시용일 뿐 물리는 선이다.
        const float LineWidth = 0.12f;

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

        Sprite _circle;
        Sprite _square;

        Selection _selected = Selection.None;
        bool _dragging;

        /// 드래그 중 직전 손가락 위치. 이동량을 여기서 낸다.
        Vector2 _dragFrom;

        void Awake()
        {
            _circle = CircleSprite();
            _square = SquareSprite();

            _startHandle = CreateHandle("StartHandle", _circle);
            _goalHandle = CreateHandle("GoalHandle", _circle);
        }

        void Update()
        {
            if (_session == null || _fitter == null || !_fitter.IsReady) return;

            HandleInput();
            Redraw();
        }

        /// <summary>
        /// 상단바 삭제 버튼이 부른다.
        /// 시작·목표는 레벨의 필수 요소라 지우지 않는다.
        /// </summary>
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

            PlaceDot(_startHandle, level.BallStart, LevelData.BallRadius,
                Tint(_startColor, HandleKind.Start, 0));
            PlaceDot(_goalHandle, level.GoalPosition, LevelData.GoalRadius,
                Tint(_goalColor, HandleKind.Goal, 0));

            // 개수가 변한다. 남는 핸들은 끄고 다시 쓴다.
            Grow(_starHandles, level.Stars.Count, "StarHandle", _circle);
            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < level.Stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (used)
                    PlaceDot(_starHandles[i], level.Stars[i], LevelData.StarCaptureRadius,
                        Tint(_starColor, HandleKind.Star, i));
            }

            Grow(_terrainHandles, level.Terrain.Count, "TerrainHandle", _square);
            for (int i = 0; i < _terrainHandles.Count; i++)
            {
                bool used = i < level.Terrain.Count;
                _terrainHandles[i].gameObject.SetActive(used);
                if (used)
                    PlaceLine(_terrainHandles[i], level.Terrain[i],
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

        static void PlaceDot(SpriteRenderer handle, Vector2 world, float radius, Color color)
        {
            handle.transform.position = new Vector3(world.x, world.y, 0f);
            handle.transform.rotation = Quaternion.identity;
            handle.transform.localScale = Vector3.one * (radius * 2f);
            handle.color = color;
        }

        static void PlaceLine(SpriteRenderer handle, in StaticSegment segment, Color color)
        {
            Vector2 center = (segment.A + segment.B) * 0.5f;
            Vector2 ab = segment.B - segment.A;

            handle.transform.position = new Vector3(center.x, center.y, 0f);
            handle.transform.rotation =
                Quaternion.Euler(0f, 0f, Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg);
            handle.transform.localScale = new Vector3(ab.magnitude, LineWidth, 1f);
            handle.color = color;
        }

        float PickRadiusWorld()
        {
            float pixels = new DeviceUnits(Screen.dpi).ToPixels(PickRadiusDp);
            return pixels / _fitter.PixelsPerUnit;
        }

        /// 도구 버튼을 눌렀는데 맵이 반응하면 안 된다.
        static bool OverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        SpriteRenderer CreateHandle(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        /// <summary>
        /// 임시 표시라 스프라이트를 코드로 만든다.
        /// 실제 아트가 들어오면 통째로 갈아낀다.
        /// </summary>
        static Sprite CircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, dist <= center ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        /// 지형 선분을 늘려 그리는 데 쓴다.
        static Sprite SquareSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        }

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
