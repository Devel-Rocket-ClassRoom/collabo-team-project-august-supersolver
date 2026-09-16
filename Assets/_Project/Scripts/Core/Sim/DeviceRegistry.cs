using System;
using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// 장치 종류마다 필요한 것을 한 줄로 모은다.
    /// 새 장치를 등록하는 곳은 여기 하나다.
    /// </summary>
    public static class DeviceRegistry
    {
        static readonly Dictionary<DeviceType, Type> DataTypes = new Dictionary<DeviceType, Type>
        {
            { DeviceType.Bomb,     typeof(BombData) },
            { DeviceType.FragBomb, typeof(FragBombData) },
            { DeviceType.Spike,    typeof(SpikeData) },
            { DeviceType.Wind,     typeof(WindData) },
        };

        /// <summary>
        /// 이 종류의 데이터를 담는 클래스.
        /// 역직렬화가 JsonUtility.FromJson(Type, json) 에 쓴다.
        /// </summary>
        public static Type DataTypeOf(DeviceType type)
        {
            if (DataTypes.TryGetValue(type, out Type dataType)) return dataType;

            // 조용히 건너뛰면 레벨과 다른 월드가 서고,
            // 솔버가 그걸 근거로 판정한다. 시끄럽게 죽는다.
            throw new ArgumentOutOfRangeException(
                nameof(type),
                $"알 수 없는 장치 종류: {(int)type}. DeviceRegistry 에 등록되지 않았다.");
        }
    }
}
