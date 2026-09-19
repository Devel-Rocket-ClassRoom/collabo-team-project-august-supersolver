using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 붙박이 장애물. 닿으면 실패다.
    /// 몸이 곧 범위라 영향 반경이 따로 없다.
    /// </summary>
    [Serializable]
    public sealed class SpikeData : IDeviceData, IOccupiesCameraArea
    {
        [DeviceParameter("Position", Kind = DeviceEditKind.Position)]
        public Vector2 Position;

        /// 몸 크기.
        [DeviceParameter("Radius", Kind = DeviceEditKind.Radius, Unit = "m")]
        [Min(SpikeDevice.MinRadius)]
        public float Radius = 0.3f;

        public DeviceType Type => DeviceType.Spike;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        public float DrawRadius => Mathf.Max(Radius, SpikeDevice.MinRadius);

        /// 몸이 곧 범위다.
        public float AreaRadius => DrawRadius;

        public IDeviceData Clone() => (SpikeData)MemberwiseClone();
    }
}
