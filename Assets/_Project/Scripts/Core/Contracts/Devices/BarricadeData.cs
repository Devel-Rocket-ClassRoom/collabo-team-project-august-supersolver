using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 빠르게 부딪힌 것에 부서지는 벽.
    /// 부서질 때 주변의 동적 바디를 밀어낸다.
    /// 느리게 닿는 것은 그냥 막는다.
    /// </summary>
    [Serializable]
    public sealed class BarricadeData : IDeviceData, IOccupiesCameraArea, IHasReach, IHasFacing
    {
        [DeviceParameter("Position", Kind = DeviceEditKind.Position)]
        public Vector2 Position;

        /// 정사각형 몸의 한 변의 절반.
        [DeviceParameter("Half size", Kind = DeviceEditKind.Radius, Unit = "m")]
        [Min(BarricadeDevice.MinHalfSize)]
        public float HalfSize = 0.5f;

        /// 이보다 빠르게 부딪힌 바디가 부순다(m/s).
        [DeviceParameter("Break speed", Unit = "m/s")]
        [Min(0f)]
        public float ThresholdSpeed = 4f;

        /// 부서질 때 중심에서의 최대 속도 변화량(m/s).
        [DeviceParameter("Power", Unit = "m/s")]
        [Min(0f)]
        public float Power = 8f;

        /// 몸이 기운 각도(도). 0 이 정사각형 그대로다.
        [DeviceParameter("Rotation", Kind = DeviceEditKind.Angle, Unit = "°")]
        public float Angle;

        public DeviceType Type => DeviceType.Barricade;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        /// 네모라서 반지름이 아니라 반변이다.
        /// 그리는 쪽이 한 변 2r 짜리 사각형을 놓는다.
        public float DrawRadius => Mathf.Max(HalfSize, BarricadeDevice.MinHalfSize);

        public float AreaRadius => Reach;

        /// <summary>
        /// 부서질 때 밀어내는 범위. 따로 편집하지 않는다 —
        /// 큰 바리게이트가 크게 터지는 편이 읽기 쉽다.
        /// </summary>
        public float Reach => DrawRadius * BarricadeDevice.BlastScale;

        public float FacingDegrees
        {
            get => Angle;
            set => Angle = value;
        }

        public IDeviceData Clone() => (BarricadeData)MemberwiseClone();
    }
}
