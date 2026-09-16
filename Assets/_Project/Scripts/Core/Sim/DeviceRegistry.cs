using System;
using System.Collections.Generic;

namespace PPS.Core
{
    public delegate IStepLogic DeviceBuilder(IDeviceData data, in DeviceBuildContext ctx);

    /// <summary>
    /// 장치 종류마다 필요한 것을 한 줄로 모은다.
    /// 새 장치를 등록하는 곳은 여기 하나다.
    /// </summary>
    public static class DeviceRegistry
    {
        public readonly struct Entry
        {
            /// 역직렬화가 쓴다. JsonUtility.FromJson(json, Type).
            public readonly Type DataType;

            /// 바디 인덱스 계산의 근거. 만든 장치만 한 자리를 쓴다.
            public readonly bool MakesBody;

            public readonly DeviceBuilder Build;

            public Entry(Type dataType, bool makesBody, DeviceBuilder build)
            {
                DataType = dataType;
                MakesBody = makesBody;
                Build = build;
            }
        }

        static readonly Dictionary<DeviceType, Entry> Table = new Dictionary<DeviceType, Entry>
        {
            { DeviceType.Bomb,     new Entry(typeof(BombData),     true,  BombDevice.Build) },
            { DeviceType.FragBomb, new Entry(typeof(FragBombData), true,  FragBombDevice.Build) },
            { DeviceType.Spike,    new Entry(typeof(SpikeData),    true,  SpikeDevice.Build) },
            { DeviceType.Wind,     new Entry(typeof(WindData),     false, WindDevice.Build) },
            { DeviceType.Bouncer, new Entry(typeof(BouncerData), true, BouncerDevice.Build) },
        };

        /// <summary>
        /// 이 종류의 데이터를 담는 클래스.
        /// 역직렬화가 JsonUtility.FromJson(json, Type) 에 쓴다.
        /// </summary>
        public static Type DataTypeOf(DeviceType type) => Of(type).DataType;

        /// <summary>
        /// 이 종류가 바디를 만드는가.
        /// 장치 번호로 바디를 찾으려면 앞의 장치 중
        /// 바디를 만든 것만 세어야 한다.
        /// </summary>
        public static bool MakesBody(DeviceType type) => Of(type).MakesBody;

        /// <summary>장치 데이터를 살아 있는 로직으로 세운다.</summary>
        public static IStepLogic Create(IDeviceData data, in DeviceBuildContext ctx)
            => Of(data.Type).Build(data, ctx);

        static Entry Of(DeviceType type)
        {
            if (Table.TryGetValue(type, out Entry entry)) return entry;

            // 조용히 건너뛰면 레벨과 다른 월드가 서고,
            // 솔버가 그걸 근거로 판정한다. 시끄럽게 죽는다.
            throw new ArgumentOutOfRangeException(
                nameof(type),
                $"알 수 없는 장치 종류: {(int)type}. DeviceRegistry 에 등록되지 않았다.");
        }
    }
}
