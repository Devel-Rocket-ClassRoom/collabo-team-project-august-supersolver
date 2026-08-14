using System.Collections.Generic;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 획이 그려지는 동안의 실시간 단계.
    /// 최소 거리 필터 → 잉크 누적 → 상한 절단.
    /// 프레임 개념이 없어야 렌더 주사율이
    /// 점 밀도를 바꾸지 못한다.
    /// </summary>
    public sealed class StrokeBuilder
    {
        readonly List<Vector2> _points = new List<Vector2>();

        float _remainingInk;
        bool _exhausted;

        /// 확정된 점. 프리뷰가 이걸 그리고
        /// 확정 시 그대로 파이프라인에 넘어간다.
        public List<Vector2> Points => _points;

        /// 프리뷰 근사값. 진실은 확정 시의 Length() 스냅이다.
        public float RemainingInk => _remainingInk;

        /// <summary>획 시작. 이전 버퍼는 버린다.</summary>
        public void Begin(float remainingInk)
        {
            _points.Clear();
            _remainingInk = remainingInk;

            // 최소 획 길이도 못 채우는 잔량은 0 과 같다.
            // 그려봤자 롤백될 획이라 아예 시작하지 않는다.
            _exhausted = remainingInk < WorldMetrics.MinStrokeLength;
        }

        /// <returns>점이 실제로 추가됐으면 true.</returns>
        public bool AddPoint(Vector2 point)
        {
            if (_exhausted) return false;

            if (_points.Count == 0)
            {
                _points.Add(point);
                return true;
            }

            Vector2 last = _points[_points.Count - 1];
            float step = Vector2.Distance(last, point);

            if (step < WorldMetrics.MinPointDistance) return false;

            if (step <= _remainingInk)
            {
                _points.Add(point);
                _remainingInk -= step;
                return true;
            }

            // 잉크가 끊기는 지점을 보간해 찍는다. 손을 뗀
            // 뒤에 뒷부분이 사라지는 방식은 안 된다.
            float cut = _remainingInk;
            _remainingInk = 0f;
            _exhausted = true;

            // 절단점만 유일하게 필터 밖이라 직전 점과 붙을
            // 수 있다. 붙은 채로 두면 콜라이더가 퇴화한다.
            if (cut < WorldMetrics.MinPointDistance) return false;

            _points.Add(last + (point - last) * (cut / step));
            return true;
        }
    }
}
