using System.Collections.Generic;
using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PPS.MapEditor
{
    /// <summary>
    /// 시작·목표·별을 화면에 띄우고 끌어서 옮긴다.
    /// 편집 결과는 곧바로 레벨 데이터에 들어간다.
    /// </summary>
    public sealed class MapEditHandles : MonoBehaviour
    {
        /// 집을 수 있는 반지름(dp). 48dp 타겟의 절반이다.
        const float PickRadiusDp = 24f;

        /// 기획이 정한 별 개수. 데이터에는 제한이 없다.
        const int MaxStars = 3;

        [SerializeField] MapEditSession _session;
        [SerializeField] CanvasCameraFitter _fitter;
        [SerializeField] ToolPalette _palette;

        /// 별을 놓는 도구가 들어 있는 탭.
        [SerializeField] int _starTab = 3;

        [SerializeField] Color _startColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        [SerializeField] Color _starColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);
        [SerializeField] Color _selectedColor = Color.white;

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;
        readonly List<SpriteRenderer> _starHandles = new List<SpriteRenderer>();

        Sprite _sprite;

        Selection _selected = Selection.None;
        bool _dragging;

        void Awake()
        {
            _sprite = CircleSprite();
            _startHandle = CreateHandle("StartHandle");
            _goalHandle = CreateHandle("GoalHandle");
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
            if (_selected.Kind != HandleKind.Star) return;

            _session.Current.Level.Stars.RemoveAt(_selected.Index);
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

                // 빈 곳을 눌렀고 별 도구를 들었으면 새로 놓는다.
                if (_selected.Kind == HandleKind.None && IsStarTool())
                    _selected = AddStar(world);

                _dragging = _selected.Kind != HandleKind.None;
            }

            if (pointer.press.wasReleasedThisFrame) _dragging = false;

            if (_dragging && pointer.press.isPressed) MoveSelected(world);
        }

        bool IsStarTool() => _palette != null && _palette.SelectedTab == _starTab;

        /// <summary>
        /// 손가락에 제일 가까운 것 하나만 고른다.
        /// 겹쳐 있을 때 둘 다 잡히면 엉뚱한 게 끌린다.
        /// </summary>
        Selection Pick(Vector2 world)
        {
            var level = _session.Current.Level;

            var best = Selection.None;
            float bestDist = PickRadiusWorld();

            Closer(ref best, ref bestDist, world, level.BallStart,
                new Selection(HandleKind.Start, 0));
            Closer(ref best, ref bestDist, world, level.GoalPosition,
                new Selection(HandleKind.Goal, 0));

            for (int i = 0; i < level.Stars.Count; i++)
                Closer(ref best, ref bestDist, world, level.Stars[i],
                    new Selection(HandleKind.Star, i));

            return best;
        }

        static void Closer(
            ref Selection best, ref float bestDist,
            Vector2 world, Vector2 candidate, Selection selection)
        {
            float dist = Vector2.Distance(world, candidate);
            if (dist > bestDist) return;

            bestDist = dist;
            best = selection;
        }

        Selection AddStar(Vector2 world)
        {
            var stars = _session.Current.Level.Stars;
            if (stars.Count >= MaxStars) return Selection.None;

            stars.Add(Clamp(world));
            return new Selection(HandleKind.Star, stars.Count - 1);
        }

        void MoveSelected(Vector2 world)
        {
            Vector2 clamped = Clamp(world);
            var level = _session.Current.Level;

            switch (_selected.Kind)
            {
                case HandleKind.Start: level.BallStart = clamped; break;
                case HandleKind.Goal: level.GoalPosition = clamped; break;
                case HandleKind.Star: level.Stars[_selected.Index] = clamped; break;
            }
        }

        /// 끝없이 멀리 놓으면 플레이 영역이 같이 커져
        /// 화면이 줌아웃되고 편집을 못 하게 된다.
        Vector2 Clamp(Vector2 world) => _fitter.ClampToPlayArea(world);

        void Redraw()
        {
            var level = _session.Current.Level;

            Place(_startHandle, level.BallStart, LevelData.BallRadius,
                Tint(_startColor, HandleKind.Start, 0));
            Place(_goalHandle, level.GoalPosition, LevelData.GoalRadius,
                Tint(_goalColor, HandleKind.Goal, 0));

            // 별은 개수가 변한다. 남는 핸들은 끄고 다시 쓴다.
            while (_starHandles.Count < level.Stars.Count)
                _starHandles.Add(CreateHandle($"StarHandle_{_starHandles.Count}"));

            for (int i = 0; i < _starHandles.Count; i++)
            {
                bool used = i < level.Stars.Count;
                _starHandles[i].gameObject.SetActive(used);
                if (!used) continue;

                Place(_starHandles[i], level.Stars[i], LevelData.StarCaptureRadius,
                    Tint(_starColor, HandleKind.Star, i));
            }
        }

        Color Tint(Color normal, HandleKind kind, int index) =>
            _selected.Kind == kind && _selected.Index == index ? _selectedColor : normal;

        static void Place(SpriteRenderer handle, Vector2 world, float radius, Color color)
        {
            handle.transform.position = new Vector3(world.x, world.y, 0f);
            handle.transform.localScale = Vector3.one * (radius * 2f);
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

        SpriteRenderer CreateHandle(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
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
                bool inside = dist <= center;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        enum HandleKind
        {
            None,
            Start,
            Goal,
            Star,
        }

        /// <summary>
        /// 고른 대상. 별은 개수가 변해서
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
