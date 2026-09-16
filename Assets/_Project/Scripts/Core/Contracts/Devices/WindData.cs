using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 범위 안의 동적 바디를 한 방향으로 미는 구역.
    /// 바디를 만들지 않는다.
    /// </summary>
    [Serializable]
    public sealed class WindData : IDeviceData, IOccupiesCameraArea, IHasReach, IHasFacing
    {
        public Vector2 Position;

        /// 미는 구역의 반경. 밖의 바디는 안 건드린다.
        public float Radius = 2f;

        /// 매 스텝 더할 가속도(m/s²).
        public float Power = 5f;

        /// 미는 방향(도). 0 이 오른쪽이다.
        public float Angle;

        public DeviceType Type => DeviceType.Wind;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        /// 화살표라 몸 크기가 없다. 구역 안에 들어갈 만큼 그린다.
        public float DrawRadius => Mathf.Max(Radius * 0.5f, 0.3f);

        public float AreaRadius => Radius;

        public float Reach => Radius;

        public float FacingDegrees
        {
            get => Angle;
            set => Angle = value;
        }

        public IDeviceData Clone() => (WindData)MemberwiseClone();
    }
}
