using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PPS.Input
{
    /// <summary>
    /// 모바일 제스처를 탭·더블탭·드래그·핀치로 갈라
    /// 이벤트로 내보낸다. 좌표는 전부 스크린 픽셀이다 —
    /// 월드 변환은 씬마다 카메라가 다르니 듣는 쪽이 한다.
    /// 그리기 입력은 DrawInputBehaviour 가 따로 처리한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileInput : MonoBehaviour
    {
        /// 이 시간을 넘겨 누르고 있으면 탭이 아니다.
        const float TapMaxDuration = 0.3f;

        /// 첫 탭 뒤 이만큼 안에 두 번째가 오면 더블탭.
        const float DoubleTapWindow = 0.25f;

        /// 손가락이 이보다 움직이면 드래그로 넘어간다.
        const float MoveSlopDp = 8f;

        /// 두 탭이 이보다 떨어져 있으면 각각 다른 탭이다.
        const float DoubleTapSlopDp = 32f;

        /// 휠 한 칸(120)을 배율로 바꾸는 계수.
        const float WheelZoomPerNotch = 0.1f;

        /// 터치 id 는 1부터라 마우스와 겹치지 않는다.
        const int MouseId = 0;

        /// 잡고 있는 포인터가 없음.
        const int NoPointer = -1;

        /// 짧게 눌렀다 뗐다. 더블탭이 아님이 확정된 뒤 온다.
        public event Action<Vector2> Tapped;

        public event Action<Vector2> DoubleTapped;

        public event Action<Vector2> DragBegan;

        /// (현재 위치, 직전 프레임 대비 이동량).
        public event Action<Vector2, Vector2> DragMoved;

        public event Action<Vector2> DragEnded;

        public event Action<Vector2> PinchBegan;

        /// (두 손가락 중점, 직전 프레임 대비 배율).
        /// 1보다 크면 벌어진 것이다. 마우스 휠도 여기로 온다.
        public event Action<Vector2, float> PinchMoved;

        public event Action PinchEnded;

        /// 현재 제스처를 잡고 있는 포인터. 없으면 NoPointer.
        int _pointerId = NoPointer;
        Vector2 _startPosition;
        Vector2 _lastPosition;
        float _startTime;
        bool _dragging;

        bool _pinching;
        float _lastPinchDistance;

        /// 핀치가 끝나도 손가락이 남는다. 전부 뗄 때까지
        /// 막지 않으면 남은 손가락이 드래그를 시작한다.
        bool _pinchLocked;

        /// 두 번째 탭을 기다리는 중인 첫 탭. 시각이 0보다
        /// 크면 대기 중이다.
        Vector2 _pendingTapPosition;
        float _pendingTapTime;

        PointerEventData _probe;
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        static float Dp(float dp)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return dp * dpi / 160f;
        }

        void OnEnable()
        {
            // 안 부르면 activeTouches 가 항상 비어 있다.
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            CancelGesture();
        }

        void Update()
        {
            int touchCount = Touch.activeTouches.Count;

            if (touchCount >= 2) ReadPinch();
            else if (touchCount == 1) ReadTouch(Touch.activeTouches[0]);
            else ReadIdle();

            FlushPendingTap();
        }

        /// <summary>손가락 두 개 이상 — 나머지는 무시한다.</summary>
        void ReadPinch()
        {
            Vector2 a = Touch.activeTouches[0].screenPosition;
            Vector2 b = Touch.activeTouches[1].screenPosition;
            Vector2 center = (a + b) * 0.5f;
            float distance = Vector2.Distance(a, b);

            if (!_pinching)
            {
                // 드래그 도중 손가락이 하나 더 오면 드래그를 끝낸다.
                if (_dragging) DragEnded?.Invoke(_lastPosition);
                _pointerId = NoPointer;
                _dragging = false;

                _pinching = true;
                _pinchLocked = true;
                _lastPinchDistance = distance;
                PinchBegan?.Invoke(center);
                return;
            }

            // 두 손가락이 겹치면 배율이 발산한다.
            if (_lastPinchDistance <= Mathf.Epsilon || distance <= Mathf.Epsilon) return;

            PinchMoved?.Invoke(center, distance / _lastPinchDistance);
            _lastPinchDistance = distance;
        }

        void ReadTouch(Touch touch)
        {
            EndPinch();
            if (_pinchLocked) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    Begin(touch.touchId, touch.screenPosition);
                    break;
                case TouchPhase.Ended:
                    End(touch.touchId, touch.screenPosition);
                    break;
                case TouchPhase.Canceled:
                    if (touch.touchId == _pointerId) CancelGesture();
                    break;
                default:
                    Move(touch.touchId, touch.screenPosition);
                    break;
            }
        }

        /// <summary>손가락이 없는 프레임. 마우스로 대신 본다.</summary>
        void ReadIdle()
        {
            EndPinch();
            _pinchLocked = false;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame) Begin(MouseId, position);
            else if (mouse.leftButton.wasReleasedThisFrame) End(MouseId, position);
            else if (mouse.leftButton.isPressed) Move(MouseId, position);

            // 휠은 핀치와 같은 이벤트로 낸다 — 듣는 쪽이
            // 확대 경로를 하나만 두게 하려는 것이다.
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
                PinchMoved?.Invoke(position, 1f + wheel / 120f * WheelZoomPerNotch);
        }

        void Begin(int pointerId, Vector2 position)
        {
            if (_pointerId != NoPointer) return;

            // UI 위에서 시작한 제스처는 버튼 몫이다. 시작할 때
            // 한 번만 본다 — 매 프레임 재판정하면 툴바를 스칠
            // 때 드래그가 끊긴다.
            if (IsOverUI(position)) return;

            _pointerId = pointerId;
            _startPosition = position;
            _lastPosition = position;
            _startTime = Time.unscaledTime;
            _dragging = false;
        }

        void Move(int pointerId, Vector2 position)
        {
            if (pointerId != _pointerId) return;

            if (!_dragging)
            {
                if (Vector2.Distance(position, _startPosition) < Dp(MoveSlopDp)) return;

                _dragging = true;
                DragBegan?.Invoke(_startPosition);
            }

            DragMoved?.Invoke(position, position - _lastPosition);
            _lastPosition = position;
        }

        void End(int pointerId, Vector2 position)
        {
            if (pointerId != _pointerId) return;
            _pointerId = NoPointer;

            if (_dragging)
            {
                _dragging = false;
                DragEnded?.Invoke(position);
                return;
            }

            if (Time.unscaledTime - _startTime <= TapMaxDuration) RegisterTap(position);
        }

        /// <summary>
        /// 탭을 바로 내보내지 않고 더블탭 창만큼 쥐고 있는다.
        /// 탭 반응이 그만큼 늦지만, 안 그러면 더블탭 때
        /// 탭이 먼저 나가 두 이벤트가 겹친다.
        /// </summary>
        void RegisterTap(Vector2 position)
        {
            bool isSecond = _pendingTapTime > 0f
                && Time.unscaledTime - _pendingTapTime <= DoubleTapWindow
                && Vector2.Distance(position, _pendingTapPosition) <= Dp(DoubleTapSlopDp);

            if (isSecond)
            {
                _pendingTapTime = 0f;
                DoubleTapped?.Invoke(position);
                return;
            }

            // 창을 넘긴 첫 탭이 아직 남아 있으면 먼저 내보낸다.
            FlushPendingTap();

            _pendingTapPosition = position;
            _pendingTapTime = Time.unscaledTime;
        }

        void FlushPendingTap()
        {
            if (_pendingTapTime <= 0f) return;
            if (Time.unscaledTime - _pendingTapTime <= DoubleTapWindow) return;

            _pendingTapTime = 0f;
            Tapped?.Invoke(_pendingTapPosition);
        }

        void EndPinch()
        {
            if (!_pinching) return;
            _pinching = false;
            PinchEnded?.Invoke();
        }

        /// <summary>진행 중인 제스처를 결과 없이 접는다.</summary>
        void CancelGesture()
        {
            if (_dragging) DragEnded?.Invoke(_lastPosition);
            EndPinch();

            _pointerId = NoPointer;
            _dragging = false;
            _pinchLocked = false;
            _pendingTapTime = 0f;
        }

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
