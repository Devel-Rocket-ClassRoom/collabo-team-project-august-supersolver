namespace PPS.Core
{
    /// <summary>
    /// 플레이어 도구. 회전축은 스트로크가
    /// 아니라 PivotJoint 로 따로 표현한다.
    /// </summary>
    public enum ToolType
    {
        /// 그 자리에 남는 발판 (정적 바디)
        FixedLine = 0,

        /// 떨어지는 물체 (동적 바디)
        FreeBody = 1,
    }
}
