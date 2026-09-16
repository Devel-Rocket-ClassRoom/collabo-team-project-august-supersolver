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
        /// 읽어 온 판의 장치 목록을 현재 형식으로 세운다.
        /// 단계는 순서대로 적용되고 버전은 끝에서 한 번만 올린다.
        /// </summary>
        /// <param name="json">읽어 온 원본 문자열. 옛 값이 여기 남아 있다.</param>
        public static void Migrate(StageData stage, string json)
        {
            RefuseNewer(stage.Version);

            if (stage.Level == null) return;

            // 현재 형식의 자리를 먼저 편다. 옛 판은 이 자리가 비어 있다.
            stage.Level.UnpackDevices();

            if (stage.Version < 1) ToV1(stage.Level, ToV1Devices(json));

            stage.Version = StageData.CurrentVersion;
        }

        /// <summary>
        /// 리플레이 안에 든 판도 같은 길을 탄다.
        /// 중첩이 한 겹 더 있어 옛 값을 꺼내는 자리가 다르다.
        /// </summary>
        public static void MigrateReplay(ReplayData replay, string json)
        {
            if (replay?.Stage?.Level == null) return;

            RefuseNewer(replay.Stage.Version);

            replay.Stage.Level.UnpackDevices();

            if (replay.Stage.Version < 1)
            {
                var legacy = JsonUtility.FromJson<LegacyReplayV0>(json);
                ToV1(replay.Stage.Level, Convert(legacy?.Stage));
            }

            replay.Stage.Version = StageData.CurrentVersion;
        }

        /// <summary>
        /// 모르는 형식은 읽지 않는다. 읽어 두면 모르는 값을
        /// 지운 채 되저장해서, 새 형식으로 만든 판이 조용히 깎인다.
        /// </summary>
        static void RefuseNewer(int version)
        {
            if (version <= StageData.CurrentVersion) return;

            throw new InvalidOperationException(
                $"판의 저장 형식이 {version} 이다. " +
                $"이 빌드는 {StageData.CurrentVersion} 까지만 안다.");
        }

        /// 공통 DeviceData 하나가 장치별 데이터로 갈라졌다.
        static void ToV1(LevelData level, List<IDeviceData> devices)
        {
            level.Devices.Clear();
            level.Devices.AddRange(devices);
        }

        /// <summary>
        /// 공통 DeviceData 하나로 저장된 장치들을 장치별 데이터로 가른다.
        /// 원본 문자열을 받는 이유는 JsonUtility 가 새 형식에 없는 필드를
        /// 이미 버려서, 옛 값은 여기서 다시 읽어야만 되찾을 수 있기 때문이다.
        /// </summary>
        /// <returns>옛 장치가 없으면 빈 목록.</returns>
        public static List<IDeviceData> ToV1Devices(string json)
        {
            var devices = new List<IDeviceData>();

            return Convert(JsonUtility.FromJson<LegacyStageV0>(json));
        }

        static List<IDeviceData> Convert(LegacyStageV0 legacy)
        {
            var devices = new List<IDeviceData>();

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
        class LegacyReplayV0
        {
            public LegacyStageV0 Stage;
        }

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
