using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PPS.MapEditor
{
    /// <summary>
    /// 시작·목표를 화면에 띄우고 끌어서 옮긴다.
    /// 편집 결과는 곧바로 레벨 데이터에 들어간다.
    /// </summary>
    public sealed class MapEditHandles : MonoBehaviour
    {
        /// 집을 수 있는 반지름(dp). 48dp 타겟의 절반이다.
        const float PickRadiusDp = 24f;

        [SerializeField] MapEditSession _session;
        [SerializeField] CanvasCameraFitter _fitter;

        [SerializeField] Color _startColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        [SerializeField] Color _goalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        [SerializeField] Color _selectedColor = new Color32(0xFF, 0xD8, 0x66, 0xFF);

        SpriteRenderer _startHandle;
        SpriteRenderer _goalHandle;

        /// 0 = 시작, 1 = 목표, -1 = 없음.
        int _selected = -1;
        bool _dragging;

        void Awake()
        {
            var sprite = CircleSprite();
            _startHandle = CreateHandle("StartHandle", sprite);
            _goalHandle = CreateHandle("GoalHandle", sprite);
        }

        void Update()
        {
            if (_session == null || _fitter == null || !_fitter.IsReady) return;

            HandleInput();
            Redraw();
        }

        void HandleInput()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 world = _fitter.ScreenToWorld(pointer.position.ReadValue());

            if (pointer.press.wasPressedThisFrame && !OverUI())
            {
                _selected = Pick(world);
                _dragging = _selected >= 0;
            }

            if (pointer.press.wasReleasedThisFrame) _dragging = false;

            if (_dragging && pointer.press.isPressed) MoveSelected(world);
        }

        /// <summary>
        /// 손가락에 제일 가까운 것 하나만 고른다.
        /// 겹쳐 있을 때 둘 다 잡히면 엉뚱한 게 끌린다.
        /// </summary>
        int Pick(Vector2 world)
        {
            float radius = PickRadiusWorld();
            var level = _session.Current.Level;

            float toStart = Vector2.Distance(world, level.BallStart);
            float toGoal = Vector2.Distance(world, level.GoalPosition);

            if (toStart > radius && toGoal > radius) return -1;
            return toStart <= toGoal ? 0 : 1;
        }

        void MoveSelected(Vector2 world)
        {
            // 끝없이 멀리 놓으면 플레이 영역이 같이 커져
            // 화면이 줌아웃되고 편집을 못 하게 된다.
            Vector2 clamped = _fitter.ClampToPlayArea(world);
            var level = _session.Current.Level;

            if (_selected == 0) level.BallStart = clamped;
            else if (_selected == 1) level.GoalPosition = clamped;
        }

        void Redraw()
        {
            var level = _session.Current.Level;

            Place(_startHandle, level.BallStart, LevelData.BallRadius,
                _selected == 0 ? _selectedColor : _startColor);
            Place(_goalHandle, level.GoalPosition, LevelData.GoalRadius,
                _selected == 1 ? _selectedColor : _goalColor);
        }

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
                bool inside = dist <= center;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
