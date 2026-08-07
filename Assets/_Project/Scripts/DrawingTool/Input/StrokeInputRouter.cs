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
    public sealed class StrokeInputRouter
    {
        /// 어떤 장치도 이 값을 포인터 id 로 쓰지 않는다.
        const int NoPointer = int.MinValue;

        readonly StrokeBuilder _builder = new StrokeBuilder();
        readonly IStrokeProcessor _processor;

        int _pointerId = NoPointer;
        bool _ownedByCanvas;
        DrawContext _context;
        Vector2 _downWorld;
        float _maxTravel;

        public StrokeInputRouter(IStrokeProcessor processor)
        {
            _processor = processor;
        }

        /// 확정된 획. 탭·롤백·Canceled 에서는 오지 않는다.
        public event Action<Stroke> StrokeConfirmed;

        public bool IsDrawing => _pointerId != NoPointer && _ownedByCanvas;

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

            _builder.Begin(context.RemainingInk);
            AddPoint(sample.World);
        }

        void Move(in PointerSample sample)
        {
            if (sample.PointerId != _pointerId || !_ownedByCanvas) return;
            Track(sample.World);
        }

        void Up(in PointerSample sample)
        {
            if (sample.PointerId != _pointerId) return;

            if (_ownedByCanvas)
            {
                // 뗀 자리도 실제 샘플이다. 버리면 Down·Up 만
                // 오는 빠른 획이 이동 0 으로 잡혀 탭이 된다.
                Track(sample.World);
                Confirm();
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

            Stroke stroke = _processor.Process(_context.Tool, _builder.Points);
            if (stroke.IsValid) StrokeConfirmed?.Invoke(stroke);
        }

        void Release()
        {
            _pointerId = NoPointer;
            _ownedByCanvas = false;
        }

        /// <summary>
        /// 시작점에서 가장 멀어진 거리로 탭을 가른다.
        /// 끝점까지 재면 되돌아온 획이 탭이 된다.
        /// </summary>
        void Track(Vector2 world)
        {
            _maxTravel = Mathf.Max(_maxTravel, Vector2.Distance(_downWorld, world));
            AddPoint(world);
        }

        /// <summary>
        /// 손가락이 영역 밖이면 클램프가 아니라 미추가다 —
        /// 클램프하면 경계를 따라 직선이 그려진다.
        /// </summary>
        void AddPoint(Vector2 world)
        {
            if (!_context.PlayArea.Contains(world)) return;

            // offset 은 화면 위쪽 고정이라 Y 만 민다.
            // X 는 손가락이 안에 있는 이상 넘칠 수 없다.
            float y = world.y + _context.ToWorld(_context.OffsetDp);
            _builder.AddPoint(new Vector2(world.x, Mathf.Min(y, _context.PlayArea.yMax)));
        }
    }
}
