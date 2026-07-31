namespace PPS.Core
{
    /// <summary>
    /// 한 번의 시뮬 결과 분류.
    ///
    /// <see cref="Stalled"/> 를 별도 상태로 두는 것이 솔버 처리량의 핵심이다.
    /// 실패 시도의 대부분은 수 초 안에 "아무 일도 일어나지 않는 상태"로 결판나는데,
    /// 이를 <see cref="Timeout"/> 과 구분하지 못하면 매번 1,800스텝을 끝까지 돌게 된다 (설계서 §3.2).
    /// </summary>
    public enum SimOutcome
    {
        /// 공이 목표에 도달
        Clear = 0,

        /// 장애물 접촉 또는 낙사
        Fail = 1,

        /// 모든 바디가 잠들고 대기 중인 장치도 없음 — 더 볼 것이 없으므로 조기 종료
        Stalled = 2,

        /// 스텝 상한(기본 1,800 = 30초) 도달
        Timeout = 3,
    }
}
