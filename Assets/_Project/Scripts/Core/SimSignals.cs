using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 시뮬에서 벌어진 일 중 보여줄 거리를 알린다.
    /// 코어는 소리도 이펙트도 모르는 채로 남는다.
    /// </summary>
    public static class SimSignals
    {
        /// 장치가 발동했다. 종류와 터진 자리를 준다.
        /// static 이라 델리게이트가 씬을 넘어 살아남는다 —
        /// 구독자는 꺼질 때 반드시 해제한다.
        public static event Action<DeviceType, Vector2> DeviceTriggered;

        public static void Trigger(DeviceType type, Vector2 at)
            => DeviceTriggered?.Invoke(type, at);
    }
}
