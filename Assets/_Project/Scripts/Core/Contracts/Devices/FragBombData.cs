using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 터져서 파편을 뿌리는 장치. 파편에 닿으면 실패다.
    /// 파편이 어디까지 날지는 모르니 범위가 없다.
    /// </summary>
    [Serializable]
    public sealed class FragBombData : IDeviceData, IOccupiesCameraArea
    {
        public Vector2 Position;

        /// 파편이 튀어 나가는 속도(m/s).
        public float Power = 6f;

        /// 발동까지의 기본 스텝 수.
        public int DelaySteps = 30;

        /// rng 로 뽑는 추가 지연의 상한.
        /// 0 이면 시드와 무관하게 발동한다.
        public int JitterSteps;

        public DeviceType Type => DeviceType.FragBomb;

        Vector2 IDeviceData.Position
        {
            get => Position;
            set => Position = value;
        }

        public float AreaRadius => FragBombDevice.BodyRadius;

        public IDeviceData Clone() => (FragBombData)MemberwiseClone();
    }
}
