using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 캔버스 rect 를 읽어 카메라를 맞추고, 화면↔월드
    /// 변환을 그리기 계층에 내준다. 카메라 값의 유일한
    /// 주인이라 씬에 박힌 값은 진입 즉시 덮인다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CanvasCameraFitter : MonoSingleton<CanvasCameraFitter>
    {
        [SerializeField] Camera _camera;

        /// 밴드를 뺀 그리기 영역. 카메라 fit 의 기준이다.
        [SerializeField] RectTransform _canvasArea;

        /// 레벨이 붙기 전까지 쓰는 임시 영역.
        [SerializeField] Rect _fallbackPlayArea = new Rect(-4f, -6f, 8f, 12f);

        readonly Vector3[] _corners = new Vector3[4];

        Rect _levelPlayArea;
        bool _hasLevel;
        bool _warnedFallback;
        CameraFrame _frame;
        bool _solved;

        Rect _appliedCanvas;
        Vector2 _appliedScreen;
        Rect _appliedPlayArea;

        /// 화면 픽셀 ↔ 월드 환산 계수. dp 판정이 쓴다.
        public float PixelsPerUnit => _frame.PixelsPerUnit;

        public Rect PlayArea => _hasLevel ? _levelPlayArea : _fallbackPlayArea;

        public bool IsReady => _solved;

        protected override void Awake()
        {
            base.Awake();

            if (_camera == null) _camera = GetComponent<Camera>();

            // 폴백을 조용히 쓰면 탭 임계값이 기기와
            // 어긋난 채로 굴러간다.
            if (!DeviceUnits.IsUsable(Screen.dpi))
                Debug.LogWarning(
                    $"Screen.dpi 가 {Screen.dpi} 다. {DeviceUnits.FallbackDpi} 로 대체한다.");
        }

        /// <summary>
        /// 그리기 영역을 쥐여 준다. 영역을 가진 UI 는
        /// 런타임에 붙어 씬에서 미리 꽂을 수 없다.
        /// </summary>
        public void SetCanvasArea(RectTransform area) => _canvasArea = area;

        /// <summary>
        /// 레벨이 정해지면 그리기 영역도 정해진다.
        /// 영역은 레벨 내용물에서 나오는 값이라
        /// 여기서 계산하지 않는다.
        /// </summary>
        public void SetLevel(LevelData level)
        {
            _hasLevel = level != null;
            if (_hasLevel) _levelPlayArea = LevelDataArea.Calculate(level);
        }

        /// <summary>
        /// 세이프 에어리어 적용과 게임 뷰 크기 변경에는
        /// 콜백이 없다. 값이 그대로면 아무것도 안 한다.
        /// </summary>
        void LateUpdate()
        {
            if (_canvasArea == null || _camera == null) return;

            Rect canvas = CanvasPixelRect();
            var screen = new Vector2(Screen.width, Screen.height);
            Rect playArea = PlayArea;

            if (canvas == _appliedCanvas
                && screen == _appliedScreen
                && playArea == _appliedPlayArea) return;

            if (!CanvasFit.TrySolve(canvas, screen, playArea, out _frame)) return;

            _appliedCanvas = canvas;
            _appliedScreen = screen;
            _appliedPlayArea = playArea;
            _solved = true;

            // 임시 영역은 그럴듯해 보여서 더 위험하다.
            // 레벨 주입이 빠진 걸 눈치 못 챈다.
            if (!_hasLevel && !_warnedFallback)
            {
                _warnedFallback = true;
                Debug.LogWarning($"레벨이 없어 임시 영역 {playArea} 을 쓴다.", this);
            }

            _camera.orthographic = true;
            _camera.orthographicSize = _frame.OrthographicSize;

            Vector3 position = _camera.transform.position;
            _camera.transform.position =
                new Vector3(_frame.Position.x, _frame.Position.y, position.z);
        }

        /// <summary>
        /// 화면이 바뀐 프레임에 아직 다시 풀지 않았을 수
        /// 있다. 프레임을 푼 그때의 화면 크기로 재야
        /// 계수와 짝이 맞는다.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPixels) =>
            _frame.Position + (screenPixels - _appliedScreen * 0.5f) / _frame.PixelsPerUnit;

        /// <summary>
        /// dp 를 월드 길이로 옮긴다. 환산 계수의 주인이
        /// 여기라 화면 크기를 따라가는 표시는 이 길을
        /// 쓴다 — 그리기 판정과 같은 값이 나와야
        /// 보이는 크기와 누를 수 있는 크기가 같다.
        /// </summary>
        public float DpToWorld(float dp) =>
            new DeviceUnits(Screen.dpi).ToPixels(dp) / _frame.PixelsPerUnit;

        public bool IsInsidePlayArea(Vector2 world) => PlayArea.Contains(world);

        public Vector2 ClampToPlayArea(Vector2 world)
        {
            Rect area = PlayArea;
            return new Vector2(
                Mathf.Clamp(world.x, area.xMin, area.xMax),
                Mathf.Clamp(world.y, area.yMin, area.yMax));
        }

        /// <summary>
        /// 배선 확인용. 남는 빨간 테두리가 곧 여백이다 —
        /// 초록이 캔버스를 다 덮으면 여백이 0 이다.
        /// 빨강이 화면 전체로 번지면 연결이 틀렸다.
        /// </summary>
        void OnDrawGizmos()
        {
            if (!_solved) return;

            Vector2 canvasSize = _appliedCanvas.size / _frame.PixelsPerUnit;
            DrawPanel(ScreenToWorld(_appliedCanvas.center), canvasSize,
                new Color(1f, 0.2f, 0.2f), 0f);

            Rect area = PlayArea;
            DrawPanel(area.center, area.size, new Color(0.3f, 1f, 0.4f), -0.1f);
        }

        /// 와이어만으로는 배경에 묻힌다. 반투명 면을
        /// 깔고 그 위에 테두리를 얹는다.
        static void DrawPanel(Vector2 center, Vector2 size, Color color, float depth)
        {
            var position = new Vector3(center.x, center.y, depth);
            var extents = new Vector3(size.x, size.y, 0.01f);

            Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
            Gizmos.DrawCube(position, extents);

            Gizmos.color = color;
            Gizmos.DrawWireCube(position, extents);
        }

        /// <summary>
        /// Overlay 캔버스라 코너의 월드 좌표가 곧 화면
        /// 픽셀이지만, 렌더 스케일이 끼면 어긋난다.
        /// </summary>
        Rect CanvasPixelRect()
        {
            _canvasArea.GetWorldCorners(_corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, _corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, _corners[2]);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
