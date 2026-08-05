using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 자기 RectTransform 을 단말의 세이프
    /// 에리어에 맞춘다. 조작 UI 루트에만 건다.
    /// 배경·편집 영역은 화면 전체를 써야 한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        RectTransform _rect;
        Canvas _canvas;

        /// 마지막으로 반영한 값. 같으면 건너뛴다.
        Rect _appliedSafeArea;
        Rect _appliedCanvasRect;

        void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _appliedSafeArea = Rect.zero;
            Apply();
        }

        /// <summary>
        /// 매 프레임 확인한다. 회전·폴더블 접힘·
        /// 에디터 게임 뷰 크기 변경에는 콜백이 없다.
        /// 값이 그대로면 아무것도 하지 않는다.
        /// </summary>
        void Update()
        {
            Apply();
        }

        void Apply()
        {
            if (_rect == null) return;

            Rect safe = Screen.safeArea;

            // 캔버스의 픽셀 크기를 기준으로 나눈다.
            // Screen 해상도는 렌더 스케일이 끼면 어긋난다.
            Rect canvasRect = CanvasPixelRect();
            if (canvasRect.width <= 0f || canvasRect.height <= 0f) return;

            if (safe == _appliedSafeArea && canvasRect == _appliedCanvasRect) return;
            _appliedSafeArea = safe;
            _appliedCanvasRect = canvasRect;

            Vector2 min = new Vector2(
                (safe.xMin - canvasRect.xMin) / canvasRect.width,
                (safe.yMin - canvasRect.yMin) / canvasRect.height);
            Vector2 max = new Vector2(
                (safe.xMax - canvasRect.xMin) / canvasRect.width,
                (safe.yMax - canvasRect.yMin) / canvasRect.height);

            // 값이 튀면 UI 가 화면 밖으로 나간다.
            min.x = Mathf.Clamp01(min.x);
            min.y = Mathf.Clamp01(min.y);
            max.x = Mathf.Clamp01(max.x);
            max.y = Mathf.Clamp01(max.y);
            if (max.x <= min.x || max.y <= min.y) return;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }

        Rect CanvasPixelRect()
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            var root = _canvas != null ? _canvas.rootCanvas : null;
            if (root != null)
            {
                Rect r = root.pixelRect;
                if (r.width > 0f && r.height > 0f) return r;
            }

            return new Rect(0f, 0f, Screen.width, Screen.height);
        }
    }
}
