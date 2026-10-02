using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 향한 쪽으로 계속 날아가는 장치.
    /// 막히면 사라지고, 밀 수 있는 것은 밀고 간다.
    /// </summary>
    [Serializable]
    public sealed class BatData : IDeviceData, IOccupiesCameraArea, IHasFacing
    {
        [DeviceParameter("Position", Kind = DeviceEditKind.Position)]
        public Vector2 Position;

        /// 몸 크기. 이 원이 부딪히는 범위다.
        [DeviceParameter("Radius", Kind = DeviceEditKind.Radius, Unit = "m")]
        [Min(BatDevice.MinRadius)]
        public float Radius = 0.3f;

        /// 날아가는 속도(m/s). 부딪혀도 느려지지 않는다.
        [DeviceParameter("Speed", Unit = "m/s")]
        [Min(0f)]
        public float Speed = 6f;

        /// 날아가는 방향(도). 0 이 오른쪽이다.
        [DeviceParameter("Direction", Kind = DeviceEditKind.Angle, Unit = "°")]
        [Tooltip("0° = right. 90° = up.")]
        public float Angle;

        public DeviceType Type => DeviceType.Bat;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        public float DrawRadius => Mathf.Max(Radius, BatDevice.MinRadius);

        /// 몸이 곧 범위다. 날아간 뒤의 자리는 미리 알 수 없다.
        public float AreaRadius => DrawRadius;

        public float FacingDegrees
        {
            get => Angle;
            set => Angle = value;
        }

        public IDeviceData Clone() => (BatData)MemberwiseClone();
    }
}
