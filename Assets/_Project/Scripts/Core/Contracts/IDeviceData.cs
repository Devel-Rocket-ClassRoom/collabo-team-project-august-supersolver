using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 배치된 장치 하나. 공통은 종류·자리·복제뿐이다 —
    /// 나머지 성질은 장치마다 다르다.
    /// </summary>
    public interface IDeviceData
    {
        DeviceType Type { get; }

        Vector2 Position { get; set; }

        /// <summary>
        /// 화면에 그릴 반지름. 콜라이더 크기 그대로다 —
        /// 보이는 것과 닿는 것이 어긋나면 레벨을 못 만든다.
        /// 크기가 데이터에 달린 장치가 있어 표로 뺄 수 없다.
        /// </summary>
        float DrawRadius { get; }

        /// <summary>
        /// 맵 에디터 붙여넣기가 쓴다. 참조를 공유하면
        /// 붙인 것을 옮길 때 원본이 따라 움직인다.
        /// </summary>
        IDeviceData Clone();
    }

    /// <summary>
    /// 카메라가 잡을 영역에 이만큼을 보탠다.
    /// 모든 장치가 구현한다 — 화면 밖의 장치는 대응할 수 없다.
    /// </summary>
    public interface IOccupiesCameraArea
    {
        float AreaRadius { get; }
    }

    /// <summary>
    /// 미치는 범위. 이걸 가진 장치만 범위 원을 그린다.
    /// 가시는 몸이 곧 범위라 구현하지 않는다.
    /// </summary>
    public interface IHasReach
    {
        float Reach { get; }
    }

    /// <summary>
    /// 미는 방향(도). 0 이 오른쪽이다.
    /// 회전·좌우반전 편집이 이걸 보고 열린다.
    /// </summary>
    public interface IHasFacing
    {
        float FacingDegrees { get; set; }
    }
}
