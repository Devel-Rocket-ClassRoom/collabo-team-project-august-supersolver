using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 옛 형식으로 저장된 판을 현재 형식으로 올린다.
    /// 저장소의 파일을 다시 쓰지 않으므로 이 코드는 영구히 남는다.
    /// </summary>
    public static class StageDataMigration
    {
        /// <summary>
        /// 공통 DeviceData 하나로 저장된 장치들을 장치별 데이터로 가른다.
        /// 원본 문자열을 받는 이유는 JsonUtility 가 새 형식에 없는 필드를
        /// 이미 버려서, 옛 값은 여기서 다시 읽어야만 되찾을 수 있기 때문이다.
        /// </summary>
        /// <returns>옛 장치가 없으면 빈 목록.</returns>
        public static List<IDeviceData> ToV1Devices(string json)
        {
            var devices = new List<IDeviceData>();

            var legacy = JsonUtility.FromJson<LegacyStageV0>(json);
            var old = legacy?.Level?.Devices;
            if (old == null) return devices;

            for (int i = 0; i < old.Count; i++) devices.Add(Convert(old[i]));

            return devices;
        }

        /// <summary>
        /// 지금 코드가 읽지 않는 옛 값은 버린다.
        /// 버린 값이 시뮬에 닿지 않으므로 결과는 같다.
        /// </summary>
        static IDeviceData Convert(in LegacyDeviceV0 old)
        {
            switch (old.Type)
            {
                case DeviceType.Bomb:
                    return new BombData
                    {
                        Position = old.Position,
                        Radius = old.Radius,
                        Power = old.Power,
                        DelaySteps = old.DelaySteps,
                        JitterSteps = old.JitterSteps,
                    };

                case DeviceType.FragBomb:
                    return new FragBombData
                    {
                        Position = old.Position,
                        Power = old.Power,
                        DelaySteps = old.DelaySteps,
                        JitterSteps = old.JitterSteps,
                    };

                case DeviceType.Spike:
                    return new SpikeData
                    {
                        Position = old.Position,
                        Radius = old.Radius,
                    };

                case DeviceType.Wind:
                    return new WindData
                    {
                        Position = old.Position,
                        Radius = old.Radius,
                        Power = old.Power,
                        Angle = old.Angle,
                    };

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(old),
                        $"알 수 없는 장치 종류: {(int)old.Type}. 마이그레이션이 다룰 수 없다.");
            }
        }

        // 옛 형식은 더 이상 바뀌지 않는다. 이 복제는 얼어붙는다.

        [Serializable]
        class LegacyStageV0
        {
            public LegacyLevelV0 Level;
        }

        [Serializable]
        class LegacyLevelV0
        {
            public List<LegacyDeviceV0> Devices;
        }

        [Serializable]
        struct LegacyDeviceV0
        {
            public DeviceType Type;
            public Vector2 Position;
            public float Radius;
            public float Power;
            public int DelaySteps;
            public int JitterSteps;
            public float Angle;
        }
    }
}
