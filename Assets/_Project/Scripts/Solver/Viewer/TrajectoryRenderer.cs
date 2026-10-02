using System;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 궤적 버퍼를 선 하나로 그린다.
    /// 버퍼는 시행마다 Clear 로 재사용되므로 참조를
    /// 들지 않고 받는 즉시 복사한다 — 참조만 들면
    /// 다음 시행이 덮어써 남의 궤적을 그리게 된다.
    /// </summary>
    public sealed class TrajectoryRenderer : MonoBehaviour
    {
        /// 진한 남색. SimScrubber 배경·선 색과 겹치지 않는다.
        [SerializeField] Color _color = new Color32(0x2E, 0x2E, 0x8F, 0xFF);

        /// 진한 주황. 궤적선과 겹쳐 그려도 구분된다.
        [SerializeField] Color _velocityColor = new Color32(0xD4, 0x4A, 0x00, 0xFF);

        /// <summary>
        /// 속도 틱의 길이 배율(월드 단위 / (m/s)).
        /// 속력을 길이로 읽는 것이라 고정값이어야 한다 —
        /// 자동 정규화하면 구간끼리 비교가 안 된다.
        /// </summary>
        [SerializeField] float _velocityScale = 0.1f;

        /// 위치만 두면 속도를 그릴 수 없어 샘플째로 든다.
        BallSample[] _samples = Array.Empty<BallSample>();

        public int Count { get; private set; }

        /// <summary>스텝 오름차순. 버퍼의 순서 그대로다.</summary>
        public BallSample this[int index] => _samples[index];

        /// <summary>
        /// 버퍼를 옮겨 담는다.
        /// 배열은 모자랄 때만 다시 잡는다.
        /// </summary>
        public void Show(TrajectoryBuffer buffer)
        {
            if (_samples.Length < buffer.Count) _samples = new BallSample[buffer.Count];

            for (int i = 0; i < buffer.Count; i++)
                _samples[i] = buffer[i];

            Count = buffer.Count;
        }

        public void Clear() => Count = 0;

        void OnRenderObject()
        {
            if (Count < 2) return;

            GLDraw.SetPass();

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            GL.Color(_color);
            for (int i = 0; i + 1 < Count; i++)
                GLDraw.Line(_samples[i].Position, _samples[i + 1].Position);

            // 스냅샷마다 속도 방향으로 뻗은 틱.
            // 궤적의 접선과 어긋나 있으면 그 샘플 사이에
            // 충돌이 묻혀 있다는 뜻이다.
            GL.Color(_velocityColor);
            for (int i = 0; i < Count; i++)
            {
                Vector2 at = _samples[i].Position;
                GLDraw.Line(at, at + _samples[i].Velocity * _velocityScale);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
