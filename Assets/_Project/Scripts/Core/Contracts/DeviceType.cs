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

        /// 붙박이 장애물. 닿으면 실패.
        Spike = 2,

        /// 범위 안의 동적 바디를 한 방향으로 민다.
        Wind = 3,

        /// 닿은 동적 바디를 반지름 방향으로 되튕긴다.
        Bouncer = 4,
    }
}
