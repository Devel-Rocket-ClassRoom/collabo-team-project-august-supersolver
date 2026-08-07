using System;

namespace PPS.Core
{
    /// <summary>
    /// 공의 시계열을 담는 고정 크기 버퍼.
    /// 전 스텝을 담으면 1800개 × 시행 수라 감당이 안 되므로
    /// Interval 스텝마다 하나만 남긴다.
    /// 시행마다 Clear 로 재사용한다 — 수만 시행에서
    /// 배열을 새로 잡으면 GC가 처리량을 먹는다.
    /// </summary>
    public sealed class TrajectoryBuffer
    {
        readonly BallSample[] _samples;

        /// 샘플 간격(스텝). 고정이다.
        public readonly int Interval;

        public TrajectoryBuffer(int interval, int maxSteps = SimWorld.DefaultMaxSteps)
        {
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval));

            Interval = interval;
            _samples = new BallSample[maxSteps / interval];
        }

        public int Count { get; private set; }

        public int Capacity => _samples.Length;

        /// <summary>
        /// 스텝 오름차순이다 — 넣은 순서가 곧 시간 순서다.
        /// </summary>
        public BallSample this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _samples[index];
            }
        }

        /// <summary>이 스텝을 담아야 하는가.</summary>
        public bool IsSampleStep(int step) => step % Interval == 0;

        public void Add(in BallSample sample) => _samples[Count++] = sample;

        /// <summary>
        /// 개수만 되돌린다. 배열은 그대로 두고 덮어쓴다 —
        /// Count 밖은 읽을 수 없으므로 이전 값은 보이지 않는다.
        /// </summary>
        public void Clear() => Count = 0;
    }
}
