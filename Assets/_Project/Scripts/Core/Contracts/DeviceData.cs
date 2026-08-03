using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 장치 종류 판별자.
    /// 정수로 직렬화되므로 재배열 금지.
    /// 추가는 뒤에만.
    /// </summary>
    public enum DeviceType
    {
        /// 터져서 반경 안의 바디를 밀어낸다.
        Bomb = 0,

        /// 터져서 파편을 뿌린다. 닿으면 실패.
        FragBomb = 1,
    }

    /// <summary>
    /// 배치된 장치 하나. placeholder 라
    /// 장치 담당이 바꿔도 된다.
    /// 콜라이더를 가져도 된다.
    /// </summary>
    [Serializable]
    public struct DeviceData
    {
        public DeviceType Type;

        public Vector2 Position;

        /// 영향 반경. 밖의 바디는 안 건드린다.
        public float Radius;

        /// 중심에서의 최대 속도 변화량(m/s).
        public float Power;

        /// 발동까지의 기본 스텝 수.
        public int DelaySteps;

        /// rng 로 뽑는 추가 지연의 상한.
        /// 0 이면 시드와 무관하게 발동한다.
        public int JitterSteps;

        /// 파편 수명(스텝). FragBomb 전용.
        /// 없으면 Stalled 가 나지 않는다.
        public int FragmentLifeSteps;
    }
}
