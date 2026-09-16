using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 닿은 동적 바디를 되튕기는 공.
    /// 몸이 곧 판정 범위라 영향 반경이 따로 없다.
    /// 되튕기는 세기는 장치가 상수로 든다.
    /// </summary>
    [Serializable]
    public sealed class BouncerData : IDeviceData, IOccupiesCameraArea
    {
        public Vector2 Position;

        /// 몸 크기. 이 표면에 닿는 것이 되튕긴다.
        public float Radius = 0.5f;

        public DeviceType Type => DeviceType.Bouncer;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        public float DrawRadius => Mathf.Max(Radius, BouncerDevice.MinRadius);

        /// 몸이 곧 범위다.
        public float AreaRadius => DrawRadius;

        public IDeviceData Clone() => (BouncerData)MemberwiseClone();
    }
}
