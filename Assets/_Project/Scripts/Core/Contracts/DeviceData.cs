using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 장치 종류 판별자.
    ///
    /// 레벨 데이터가 <c>List&lt;IStepLogic&gt;</c> 이 아닌 이유는 JsonUtility 가 다형 직렬화를
    /// 하지 못하기 때문이다. 인터페이스로 실으면 JSON 왕복에서 타입이 사라져
    /// "레벨 추가는 데이터만으로"가 성립하지 않는다.
    /// 그래서 판별자 + 파라미터로 싣고 <see cref="DeviceFactory"/> 가 실체를 만든다.
    ///
    /// **값을 재배열하지 말 것.** 정수로 직렬화되므로 순서가 바뀌면
    /// 이미 저장된 레벨 JSON 과 리플레이가 다른 장치를 가리키게 된다. 추가는 뒤에만 붙인다.
    /// </summary>
    public enum DeviceType
    {
        /// 일정 스텝 뒤 한 번 터져 반경 안의 동적 바디를 밀어낸다.
        Bomb = 0,
    }

    /// <summary>
    /// 레벨에 배치된 장치 하나.
    ///
    /// **이 스키마는 자리를 잡아 둔 것이지 확정이 아니다 (placeholder).**
    /// 장치 6종(목표·별·장애물·스위치와 문·바람 구역·폭탄)을 맡은 팀원이 자유롭게 바꿔도 된다 —
    /// 종류가 늘면 공용 필드 하나로는 감당이 안 될 것이고, 그때는 종류별 파라미터 블록으로
    /// 나누는 편이 낫다. 지금 형태는 <see cref="BombDevice"/> 하나를 태우기 위한 최소 골격이다.
    ///
    /// 바꿀 때 지켜야 할 것은 둘뿐이다.
    /// - JsonUtility 가 다룰 수 있는 형태일 것 (public 필드 + <see cref="SerializableAttribute"/>)
    /// - 리스트 순서가 곧 로직 등록 순서이자 **난수 소비 순서**라는 것
    ///
    /// **장치는 콜라이더를 가져도 된다.** 장애물·문처럼 물리적 실체가 있는 장치는 바디가 필요하며,
    /// <see cref="DeviceFactory"/> 가 씬과 바디 목록을 함께 받는 이유가 그것이다.
    /// 폭탄·바람 구역처럼 영역 효과만 있는 장치가 안 만들 뿐이다.
    /// </summary>
    [Serializable]
    public struct DeviceData
    {
        public DeviceType Type;

        public Vector2 Position;

        /// 영향 반경. 이 밖의 바디는 건드리지 않는다.
        public float Radius;

        /// 중심에서의 최대 속도 변화량(m/s). 가장자리로 갈수록 선형으로 준다.
        public float Power;

        /// 발동까지의 기본 스텝 수.
        public int DelaySteps;

        /// <summary>
        /// 주입된 rng 로 뽑는 추가 지연의 상한(미만). 0 이면 시드와 무관하게
        /// <see cref="DelaySteps"/> 에 정확히 발동한다.
        ///
        /// 코어에서 rng 가 흘러가는 곳은 <see cref="IStepLogic.Tick"/> 하나뿐이라,
        /// 이 값을 쓰는 장치가 없으면 **시드가 결과에 반영되는지 확인할 방법이 없다.**
        /// </summary>
        public int JitterSteps;
    }
}
