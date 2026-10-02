using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 레벨 정의. 코드 수정 없이
    /// 데이터만으로 추가할 수 있어야 한다.
    /// JSON 진입점은 StageData 에 있다.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public const float BallRadius = 0.25f;
        public const float GoalRadius = 0.5f;
        public const float StarCaptureRadius = 0.35f;

        public float InkLimit = 20f;

        public Vector2 BallStart;

        public Vector2 GoalPosition;

        /// 리스트 순서 = 월드 등록 순서.
        public List<StaticSegment> Terrain = new List<StaticSegment>();

        /// <summary>
        /// 디스크 형태. 런타임에 읽지 않는다 —
        /// JsonUtility 가 인터페이스를 직렬화하지 못해
        /// 타입 태그와 중첩 json 으로 나눠 담는다.
        /// </summary>
        public List<DeviceEntry> DeviceEntries = new List<DeviceEntry>();

        /// <summary>
        /// 런타임 형태. 리스트 순서 = 로직 등록 순서 = 난수 소비 순서.
        /// 시뮬과 솔버는 이 안의 값을 변형하지 않는다 —
        /// 참조 타입이라 고치면 원본이 함께 바뀐다.
        /// </summary>
        [NonSerialized] public List<IDeviceData> Devices = new List<IDeviceData>();
        public List<Vector2> Stars = new List<Vector2>();

        public float KillY = -20f;

        /// <summary>런타임 형태 → 디스크 형태. 저장 직전에 부른다.</summary>
        public void PackDevices() => DeviceSerialization.Pack(Devices, DeviceEntries);

        /// <summary>디스크 형태 → 런타임 형태. 읽은 직후에 부른다.</summary>
        public void UnpackDevices() => DeviceSerialization.Unpack(DeviceEntries, Devices);
    }

    /// <summary>붙박이 지형 한 조각.</summary>
    [Serializable]
    public struct StaticSegment
    {
        public Vector2 A;
        public Vector2 B;

        public StaticSegment(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }
    }
}
