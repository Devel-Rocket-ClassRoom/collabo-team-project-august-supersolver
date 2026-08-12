using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 시뮬레이션이 알리는 사건.
    /// <para>
    /// 표시 전용이다. 구독자가 월드를 되돌려 건드리면
    /// 같은 입력에 다른 결과가 나와 솔버가 무너진다.
    /// </para>
    /// <para>
    /// 물리 콜백과 달리 발행 시점이 우리 손에 있다 —
    /// 로직·판정이 도는 정해진 자리에서만 부르므로
    /// 부르는 순서가 스텝마다 같다.
    /// </para>
    /// </summary>
    public sealed class SimEvents
    {
        /// <param name="index">레벨의 장치 번호.</param>
        public event Action<int, Vector2> DeviceFired;

        /// <param name="index">레벨의 별 번호.</param>
        public event Action<int, Vector2> StarCollected;

        internal void RaiseDeviceFired(int index, Vector2 at) => DeviceFired?.Invoke(index, at);

        internal void RaiseStarCollected(int index, Vector2 at) =>
            StarCollected?.Invoke(index, at);
    }
}
