namespace PPS.Core
{
    /// <summary>
    /// 플레이어 도구 종류. 미션 가이드의 도구 3종 중 회전축은 스트로크가 아니라
    /// 연결 정보이므로 <see cref="PivotJoint"/> 로 따로 표현한다.
    /// </summary>
    public enum ToolType
    {
        /// 그린 자리에 붙박이로 남는 발판 (정적 바디)
        FixedLine = 0,

        /// 중력의 영향을 받아 떨어지는 물체 (동적 바디)
        FreeBody = 1,
    }
}
