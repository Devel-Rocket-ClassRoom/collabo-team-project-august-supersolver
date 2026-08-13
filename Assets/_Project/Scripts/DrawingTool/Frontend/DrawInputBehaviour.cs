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
    /// 장치를 읽어 인식기에 밀어 넣는 얇은 층.
    /// 판정은 전부 StrokeGestureRecognizer 가 한다 — 여기 로직이
    /// 늘면 기기 없이 검증 못 하는 코드가 늘어난다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawInputBehaviour : MonoBehaviour
    {
        /// 터치 id 는 1부터라 마우스와 겹치지 않는다.
        const int MouseId = 0;

        [SerializeField] CanvasCameraFitter _fitter;
        [SerializeField] ToolSelection _tools;
        [SerializeField] DrawingSession _session;

        /// 잉크 상한이 나오는 판. 상한을 float 로 복사해
        /// 두면 원본과 갈라질 자리가 생겨 판을 통째로 든다.
        /// 레벨이 붙기 전까지는 기본값 판이다.
        LevelData _level = new LevelData();

        readonly StrokeGestureRecognizer _recognizer = new StrokeGestureRecognizer(new StrokeProcessor());
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        /// 손가락별로 마지막까지 읽은 샘플 시각.
        /// history 가 프레임 너머까지 남아 중복으로 들어온다.
        readonly Dictionary<int, double> _consumed = new Dictionary<int, double>();

        PointerEventData _probe;

        public bool IsDrawing => _recognizer.IsDrawing;

        public IReadOnlyList<Vector2> PreviewPoints => _recognizer.PreviewPoints;

        /// 확정된 획만 센 잔량. 획을 시작할 때 쓰는 값이다.
        public float RemainingInk => _level.InkLimit - _session.Solution.TotalInk();

        /// <summary>그리던 획을 버린다. 시뮬레이션 진입이 부른다.</summary>
        public void CancelStroke() => _recognizer.Abort();

        /// <summary>
        /// 잉크 상한이 나오는 판을 물린다. 획을 그린 뒤에
        /// 바뀌면 잔량이 음수가 되므로 레벨을 붙일 때
        /// 한 번만 부른다.
        /// </summary>
        public void SetLevel(LevelData level) => _level = level;

        /// 게이지용. 그리는 중에는 프리뷰 근사를 보여준다.
        public float InkRatio => Mathf.Clamp01(
            (_recognizer.IsDrawing ? _recognizer.PreviewRemainingInk : RemainingInk)
            / _level.InkLimit);

        void OnEnable()
        {
            // 안 부르면 activeTouches 가 항상 비어 있다.
            EnhancedTouchSupport.Enable();
            _recognizer.StrokeConfirmed += _session.AddStroke;
            _recognizer.PivotRequested += PlacePivot;
        }

        void OnDisable()
        {
            _recognizer.StrokeConfirmed -= _session.AddStroke;
            _recognizer.PivotRequested -= PlacePivot;
            EnhancedTouchSupport.Disable();
        }

        /// <summary>
        /// 어느 획에 걸리는지는 Solution 을 아는 여기서 푼다.
        /// 도구는 인식기가 Down 에서 잡아둔 값이다 — 그리는
        /// 도중에 툴바를 눌러 바꿔도 시작할 때 고른 게 이긴다.
        /// </summary>
        void PlacePivot(DrawTool tool, Vector2 anchor, float radius)
        {
            if (PivotPlacement.TryResolve(
                    _session.Solution, anchor, radius,
                    tool == DrawTool.PivotWorld, out PivotJoint pivot))
                _session.AddPivot(pivot);
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
            ProbeSampleRate(id, phase);
            Feed(id, phase, record.screenPosition, ScreenConstants.DrawOffsetDp);

            // 끝난 손가락을 남겨두면 id 가 계속 쌓인다.
            if (phase == PointerPhase.Up || phase == PointerPhase.Canceled) _consumed.Remove(id);
        }

        /// 완료조건 5-1 계측 중인 손가락. -1 이면 쉬는 중.
        /// 인식기와 같이 first-touch-wins 라야 둘째 손가락이
        /// 샘플 수를 부풀려 거짓 통과를 만들지 않는다.
        int _probeId = -1;
        int _probeSamples;
        int _probeFrames;
        int _probeLastFrame;

        /// <summary>
        /// 한 손가락이 닿아 있는 동안 소비한 샘플 수를
        /// 프레임 수로 나눈다. 1 이면 history 를 안 읽고
        /// 프레임당 한 번만 읽는 것이다. 개발 빌드 전용.
        /// </summary>
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void ProbeSampleRate(int id, PointerPhase phase)
        {
            if (phase == PointerPhase.Down && _probeId < 0)
            {
                _probeId = id;
                _probeSamples = 0;
                _probeFrames = 0;
                _probeLastFrame = -1;
            }

            if (id != _probeId) return;

            _probeSamples++;
            if (_probeLastFrame != Time.frameCount)
            {
                _probeLastFrame = Time.frameCount;
                _probeFrames++;
            }

            if (phase != PointerPhase.Up && phase != PointerPhase.Canceled) return;

            Debug.Log($"[5-1] 샘플 {_probeSamples} / 프레임 {_probeFrames}"
                + $" = {(float)_probeSamples / _probeFrames:0.00} 개/프레임");
            _probeId = -1;
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

            _recognizer.Feed(sample, new DrawContext(
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
