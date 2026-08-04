namespace PPS.Core
{
    /// <summary>
    /// 시뮬 결과 분류.
    /// Stalled 를 Timeout 과 구분하는 것이
    /// 솔버 처리량의 핵심이다.
    /// </summary>
    public enum SimOutcome
    {
        /// 공이 목표에 도달
        Clear = 0,

        /// 위험 바디 접촉 또는 낙사
        Fail = 1,

        /// 전 바디 sleep + 대기 장치 없음
        Stalled = 2,

        /// 스텝 상한 도달
        Timeout = 3,
    }
}
