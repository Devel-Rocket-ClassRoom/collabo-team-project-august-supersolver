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
        public Vector2 Position;

        /// 몸 크기.
        public float Radius = 0.3f;

        public DeviceType Type => DeviceType.Spike;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        /// 바디가 서는 크기와 같아야 한다.
        public float AreaRadius => Mathf.Max(Radius, SpikeDevice.MinRadius);

        public IDeviceData Clone() => (SpikeData)MemberwiseClone();
    }
}
