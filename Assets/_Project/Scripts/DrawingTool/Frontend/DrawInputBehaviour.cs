using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 장치를 읽어 라우터에 밀어 넣는 얇은 층.
    /// 판정은 전부 StrokeInputRouter 가 한다 — 여기 로직이
    /// 늘면 기기 없이 검증 못 하는 코드가 늘어난다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawInputBehaviour : MonoBehaviour
    {
        /// 터치 id 는 1부터라 마우스와 겹치지 않는다.
        const int MouseId = 0;

        [SerializeField] CanvasCameraFitter _fitter;
        [SerializeField] ToolSelection _tools;

        /// 레벨이 붙기 전까지 쓰는 임시 상한.
        /// 레벨이 생기면 LevelData.InkLimit 이 주인이다.
        [SerializeField] float _inkLimit = 20f;

        readonly StrokeInputRouter _router = new StrokeInputRouter(new StrokeProcessor());
        readonly Solution _solution = new Solution();
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        /// 손가락별로 마지막까지 읽은 샘플 시각.
        /// history 가 프레임 너머까지 남아 중복으로 들어온다.
        readonly Dictionary<int, double> _consumed = new Dictionary<int, double>();

        PointerEventData _probe;

        /// 지금까지 확정된 그림. 스냅샷 Undo 가 이 위에 얹힌다.
        public Solution Solution => _solution;

        /// 확정된 획. 렌더러가 듣는다.
        public event Action<Stroke> StrokeConfirmed;

        public bool IsDrawing => _router.IsDrawing;

        public IReadOnlyList<Vector2> PreviewPoints => _router.PreviewPoints;

        /// 확정된 획만 센 잔량. 획을 시작할 때 쓰는 값이다.
        public float RemainingInk => _inkLimit - _solution.TotalInk();

        /// 게이지용. 그리는 중에는 프리뷰 근사를 보여준다.
        public float InkRatio => Mathf.Clamp01(
            (_router.IsDrawing ? _router.PreviewRemainingInk : RemainingInk) / _inkLimit);

        void OnEnable()
        {
            // 안 부르면 activeTouches 가 항상 비어 있다.
            EnhancedTouchSupport.Enable();
            _router.StrokeConfirmed += Append;
        }

        void OnDisable()
        {
            _router.StrokeConfirmed -= Append;
            EnhancedTouchSupport.Disable();
        }

        void Append(Stroke stroke)
        {
            _solution.Strokes.Add(stroke);
            StrokeConfirmed?.Invoke(stroke);
        }

        void Update()
        {
            // fit 이 아직 안 풀린 프레임의 좌표는 쓰레기다.
            if (_fitter == null || !_fitter.IsReady) return;

            // 터치를 먼저 읽는다. 시뮬레이터는 마우스와 터치를
            // 같이 내보내는데, 먼저 잡은 쪽이 획을 가져간다.
            ReadTouches();
            ReadMouse();
        }

        /// <summary>
        /// 터치 보고율이 렌더 프레임레이트보다 빠르다.
        /// 프레임당 한 번만 읽으면 중간 샘플이 버려진다.
        /// </summary>
        void ReadTouches()
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                TouchHistory history = touch.history;

                // history 는 최신이 먼저다.
                // 뒤에서부터 넣어야 시간 순서가 된다.
                for (int i = history.Count - 1; i >= 0; i--)
                    Consume(history[i]);

                // Began 이 덮어씌워진 사본은 좌표가 시작점으로
                // 되돌아가 있다. 진짜 값은 다음 프레임 history
                // 로 오니 여기서 시각만 먹으면 그걸 잃는다.
                if (touch.phase == TouchPhase.Began && history.Count > 0) continue;

                Consume(touch);
            }
        }

        void Consume(Touch record)
        {
            int id = record.touchId;
            if (_consumed.TryGetValue(id, out double last) && record.time <= last) return;
            _consumed[id] = record.time;

            PointerPhase phase = ToPhase(record.phase);
            Feed(id, phase, record.screenPosition, ScreenConstants.DrawOffsetDp);

            // 끝난 손가락을 남겨두면 id 가 계속 쌓인다.
            if (phase == PointerPhase.Up || phase == PointerPhase.Canceled) _consumed.Remove(id);
        }

        static PointerPhase ToPhase(TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began: return PointerPhase.Down;
                case TouchPhase.Ended: return PointerPhase.Up;
                case TouchPhase.Canceled: return PointerPhase.Canceled;
                default: return PointerPhase.Move;
            }
        }

        /// <summary>마우스는 offset 0. 그 외에는 같은 경로다.</summary>
        void ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
                Feed(MouseId, PointerPhase.Down, position, 0f);
            else if (mouse.leftButton.wasReleasedThisFrame)
                Feed(MouseId, PointerPhase.Up, position, 0f);
            else if (mouse.leftButton.isPressed)
                Feed(MouseId, PointerPhase.Move, position, 0f);
        }

        void Feed(int pointerId, PointerPhase phase, Vector2 screenPixels, float offsetDp)
        {
            var sample = new PointerSample(
                pointerId,
                phase,
                _fitter.ScreenToWorld(screenPixels),
                phase == PointerPhase.Down && IsOverUI(screenPixels));

            _router.Feed(sample, new DrawContext(
                _tools.Current, RemainingInk, _fitter.PixelsPerUnit,
                Screen.dpi, offsetDp, _fitter.PlayArea));
        }

        /// <summary>
        /// pointerId 를 넘기는 대신 직접 레이캐스트한다.
        /// 신형 Input System 의 touchId 와 UI 모듈이 매기는
        /// pointerId 는 값 체계가 달라 어긋날 수 있다.
        /// </summary>
        bool IsOverUI(Vector2 screenPixels)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;

            if (_probe == null) _probe = new PointerEventData(events);
            _probe.position = screenPixels;

            events.RaycastAll(_probe, _hits);
            return _hits.Count > 0;
        }
    }
}
