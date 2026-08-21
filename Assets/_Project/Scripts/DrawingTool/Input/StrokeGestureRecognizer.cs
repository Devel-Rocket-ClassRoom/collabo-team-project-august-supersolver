using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 포인터 샘플을 획으로 바꾸는 상태 머신.
    /// 소유권·탭·경계·offset·멀티터치를 여기서 정하고
    /// 점 필터와 단순화는 파이프라인에 맡긴다.
    /// </summary>
    public sealed class StrokeGestureRecognizer
    {
        /// 어떤 장치도 이 값을 포인터 id 로 쓰지 않는다.
        const int NoPointer = int.MinValue;

        readonly StrokeBuilder _builder = new StrokeBuilder();
        readonly IStrokeProcessor _processor;

        int _pointerId = NoPointer;
        bool _ownedByCanvas;

        /// 획을 쌓는 중인가. 탭 도구는 캔버스를 쥐고도
        /// 빌더를 안 열어 이 값이 false 다.
        bool _buildingStroke;

        /// 지우개로 끄는 중인가. 이 동안은 탭 판정도
        /// 프리뷰도 없어 그리기 경로를 통째로 비켜 간다.
        bool _erasing;

        DrawContext _context;
        Vector2 _downWorld;
        float _maxTravel;

        public StrokeGestureRecognizer(IStrokeProcessor processor)
        {
            _processor = processor;
        }

        /// 확정된 획. 탭·롤백·Canceled 에서는 오지 않는다.
        public event Action<Stroke> StrokeConfirmed;

        /// <summary>
        /// 회전축 탭. 도구·앵커·히트 반경(월드)을 준다.
        /// 어느 획에 걸리는지는 Solution 을 아는 쪽이 정한다.
        /// </summary>
        public event Action<DrawTool, Vector2, float> PivotRequested;

        /// <summary>
        /// 지우개가 닿은 자리. 앵커·반경(월드)을 준다.
        /// 끄는 동안 표본마다 오므로 한 제스처에 여러 번
        /// 온다. 무엇이 걸리는지는 Solution 을 아는 쪽이 정한다.
        /// </summary>
        public event Action<Vector2, float> EraseRequested;

        public bool IsDrawing => _buildingStroke;

        /// 그리는 중인 점. IsDrawing 이 false 면 직전 획의
        /// 잔재라 읽으면 안 된다.
        public IReadOnlyList<Vector2> PreviewPoints => _builder.Points;

        /// 프리뷰 근사 잔량. 게이지가 읽는다.
        public float PreviewRemainingInk => _builder.RemainingInk;

        public void Feed(in PointerSample sample, in DrawContext context)
        {
            switch (sample.Phase)
            {
                case PointerPhase.Down: Down(sample, context); break;
                case PointerPhase.Move: Move(sample); break;
                case PointerPhase.Up: Up(sample); break;
                case PointerPhase.Canceled: Cancel(sample); break;
            }
        }

        void Down(in PointerSample sample, in DrawContext context)
        {
            // first-touch-wins. 두 번째 손가락은 첫 손가락을
            // 뗄 때까지 완전히 무시한다.
            if (_pointerId != NoPointer) return;

            _pointerId = sample.PointerId;
            _ownedByCanvas = !sample.IsOverUI;
            if (!_ownedByCanvas) return;

            _context = context;
            _downWorld = sample.World;
            _maxTravel = 0f;

            // 누른 자리부터 바로 지운다. 뗄 때 지우면
            // 끄는 동안 아무 반응이 없다.
            if (context.Tool == DrawTool.Erase)
            {
                _erasing = true;
                EraseAt(sample.World);
                return;
            }

            // 핀은 잉크도 프리뷰도 쓰지 않는다.
            if (context.Tool.IsTap()) return;

            _buildingStroke = true;
            _builder.Begin(context.RemainingInk);
            AddPoint(sample.World);
        }

        void Move(in PointerSample sample)
        {
            if (sample.PointerId != _pointerId || !_ownedByCanvas) return;

            if (_erasing) EraseAt(sample.World);
            else Track(sample.World);
        }

        void Up(in PointerSample sample)
        {
            if (sample.PointerId != _pointerId) return;

            // 지우개는 뗀 자리를 안 본다. 누른 자리에서 이미
            // 한 번 지웠으므로 여기서 또 지우면 탭 한 번이
            // 두 개를 지운다.
            if (_ownedByCanvas && !_erasing)
            {
                // 뗀 자리도 실제 샘플이다. 버리면 Down·Up 만
                // 오는 빠른 획이 이동 0 으로 잡혀 탭이 된다.
                Track(sample.World);

                if (_buildingStroke) Confirm();
                else ConfirmTap();
            }

            Release();
        }

        /// <summary>
        /// 시스템이 회수한 터치는 사용자가 끝낸 제스처가
        /// 아니다. 확정하면 팜·알림 셰이드가 콜라이더를
        /// 남긴다. 잔량은 재계산이라 저절로 복구된다.
        /// </summary>
        void Cancel(in PointerSample sample)
        {
            if (sample.PointerId != _pointerId) return;
            Release();
        }

        void Confirm()
        {
            // 드로잉 도구는 탭을 무시한다 — 미세 콜라이더가
            // 물리에서 터널링·지터를 일으킨다.
            if (_maxTravel < _context.ToWorld(ScreenConstants.TapThresholdDp)) return;

            Stroke stroke = _processor.Process(_context.Tool.ToToolType(), _builder.Points);
            if (stroke.IsValid) StrokeConfirmed?.Invoke(stroke);
        }

        /// <summary>
        /// 핀은 탭만 받는다. 끌었으면 아무것도 하지
        /// 않는다 — 획을 그으려다 도구를 잘못 고른 손이다.
        /// 히트 반경은 탭 임계값과 같은 값을 쓴다.
        /// </summary>
        void ConfirmTap()
        {
            float radius = _context.ToWorld(ScreenConstants.TapThresholdDp);
            if (_maxTravel >= radius) return;

            PivotRequested?.Invoke(_context.Tool, Adjust(_downWorld), radius);
        }

        /// <summary>
        /// 지우개 반경은 탭 임계값이 아니라 제 상수를 쓴다 —
        /// 끌면서 지우려면 손가락 폭만 한 붓이어야 한다.
        /// </summary>
        void EraseAt(Vector2 world) => EraseRequested?.Invoke(
            Adjust(world), _context.ToWorld(ScreenConstants.EraseRadiusDp));

        /// <summary>
        /// 그리던 것을 버린다. 획 진행 중에 두 번째 손가락으로
        /// 플레이를 누르면 버튼은 눌리고 인식기만 남는다 —
        /// 확정하지 않는 이유는 터치 Canceled 와 같다.
        /// </summary>
        public void Abort() => Release();

        void Release()
        {
            _pointerId = NoPointer;
            _ownedByCanvas = false;
            _buildingStroke = false;
            _erasing = false;
        }

        /// <summary>
        /// 시작점에서 가장 멀어진 거리로 탭을 가른다.
        /// 끝점까지 재면 되돌아온 획이 탭이 된다.
        /// </summary>
        void Track(Vector2 world)
        {
            _maxTravel = Mathf.Max(_maxTravel, Vector2.Distance(_downWorld, world));
            if (_buildingStroke) AddPoint(world);
        }

        void AddPoint(Vector2 world) => _builder.AddPoint(Adjust(world));

        /// <summary>
        /// offset 을 얹고 영역 안에 가둔다. 손가락이 영역을
        /// 벗어나도 점은 경계에 붙는다 — 버리면 복귀할 때
        /// 그린 적 없는 선분이 튀어나온다.
        /// </summary>
        Vector2 Adjust(Vector2 world)
        {
            Rect area = _context.PlayArea;

            // offset 은 화면 위쪽 고정이라 Y 에만 더한다.
            float y = world.y + _context.ToWorld(_context.OffsetDp);

            return new Vector2(
                Mathf.Clamp(world.x, area.xMin, area.xMax),
                Mathf.Clamp(y, area.yMin, area.yMax));
        }
    }
}
