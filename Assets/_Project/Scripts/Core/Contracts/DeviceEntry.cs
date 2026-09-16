using System;

namespace PPS.Core
{
    /// <summary>
    /// 디스크에 남는 장치 한 칸. 속은 장치마다 다르다.
    /// JsonUtility 는 인터페이스를 직렬화하지 못해
    /// 타입 태그와 중첩 문자열로 나눠 담는다.
    /// </summary>
    [Serializable]
    public struct DeviceEntry
    {
        public DeviceType Type;

        /// 장치별 데이터를 JsonUtility 로 찍은 것.
        public string Json;
    }
}
