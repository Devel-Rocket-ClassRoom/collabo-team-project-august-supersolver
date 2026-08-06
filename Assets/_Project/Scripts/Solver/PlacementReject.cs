namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브를 놓을 수 없는 이유.
    /// 사유를 구분해야 후보 격자를 어느 축으로
    /// 좁힐지 판단할 수 있다.
    /// </summary>
    public enum PlacementReject
    {
        /// 놓을 수 있다.
        None = 0,

        /// 공이 못 올라타거나 끼일 만큼 작다.
        TooSmall = 1,

        /// 혼자서 잉크 총량을 넘긴다.
        TooLarge = 2,

        /// 플레이 영역 밖이다.
        OutOfArea = 3,

        /// 이미 쓴 잉크와 합치면 총량을 넘긴다.
        InkExceeded = 4,

        /// 벡터 길이가 Dimensions 의 배수가 아니다.
        /// 프리미티브로 펴기 전에 걸린다.
        BadVector = 5,
    }
}
