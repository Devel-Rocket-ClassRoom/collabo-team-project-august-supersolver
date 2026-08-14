using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Time = UnityEngine.Time;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 한 손가락이 닿아 있는 동안 소비한 샘플 수를
    /// 프레임 수로 나눈다. 1 이면 history 를 안 읽고
    /// 프레임당 한 번만 읽는 것이다. 개발 빌드 전용.
    /// </summary>
    public sealed class SampleRateProbe
    {
        /// 계측 중인 손가락. -1 이면 쉬는 중. 인식기와 같이
        /// first-touch-wins 라야 둘째 손가락이 샘플 수를
        /// 부풀려 거짓 통과를 만들지 않는다.
        int _pointerId = -1;

        int _samples;
        int _frames;
        int _lastFrame;

        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public void Record(int pointerId, PointerPhase phase)
        {
            if (phase == PointerPhase.Down && _pointerId < 0)
            {
                _pointerId = pointerId;
                _samples = 0;
                _frames = 0;
                _lastFrame = -1;
            }

            if (pointerId != _pointerId) return;

            _samples++;
            if (_lastFrame != Time.frameCount)
            {
                _lastFrame = Time.frameCount;
                _frames++;
            }

            if (phase != PointerPhase.Up && phase != PointerPhase.Canceled) return;

            Debug.Log($"[5-1] 샘플 {_samples} / 프레임 {_frames}"
                + $" = {(float)_samples / _frames:0.00} 개/프레임");
            _pointerId = -1;
        }
    }
}
