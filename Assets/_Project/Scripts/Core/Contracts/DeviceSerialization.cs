using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 런타임 장치 목록과 디스크 형태 사이를 오간다.
    /// LevelData 를 데이터 클래스로 두려고 밖에 뒀다.
    /// </summary>
    public static class DeviceSerialization
    {
        /// <summary>런타임 형태 → 디스크 형태.</summary>
        public static void Pack(List<IDeviceData> devices, List<DeviceEntry> entries)
        {
            entries.Clear();
            if (devices == null) return;

            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                if (device == null) continue;

                entries.Add(new DeviceEntry
                {
                    Type = device.Type,
                    Json = JsonUtility.ToJson(device),
                });
            }
        }

        /// <summary>
        /// 디스크 형태 → 런타임 형태.
        /// 리스트 순서를 그대로 지킨다 — 등록 순서가 곧 난수 소비 순서다.
        /// </summary>
        public static void Unpack(List<DeviceEntry> entries, List<IDeviceData> devices)
        {
            devices.Clear();
            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var data = (IDeviceData)JsonUtility.FromJson(
                    entry.Json, DeviceRegistry.DataTypeOf(entry.Type));

                if (data == null)
                    throw new System.InvalidOperationException(
                        $"장치 {i}({entry.Type}) 의 json 을 읽지 못했다: {entry.Json}");

                devices.Add(data);
            }
        }
    }
}
