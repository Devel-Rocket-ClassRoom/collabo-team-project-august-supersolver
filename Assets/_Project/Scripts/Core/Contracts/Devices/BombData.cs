using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 터져서 반경 안의 동적 바디를 밀어내는 장치.
    /// </summary>
    [Serializable]
    public sealed class BombData : IDeviceData, IOccupiesCameraArea, IHasReach
    {
        public Vector2 Position;

        /// 폭발 반경. 밖의 바디는 안 건드린다.
        public float Radius = 2f;

        /// 중심에서의 최대 속도 변화량(m/s).
        public float Power = 11f;

        /// 발동까지의 기본 스텝 수.
        public int DelaySteps = 30;

        /// rng 로 뽑는 추가 지연의 상한.
        /// 0 이면 시드와 무관하게 발동한다.
        public int JitterSteps;

        public DeviceType Type => DeviceType.Bomb;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        /// 폭발 반경이 아니라 몸 크기다.
        public float DrawRadius => BombDevice.BodyRadius;

        public float AreaRadius => Radius;

        public float Reach => Radius;

        public IDeviceData Clone() => (BombData)MemberwiseClone();
    }
}
