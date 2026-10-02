using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 튜토리얼 SO 가 스테이지를 지칭하는 방식.
    /// 컷과 고정 표시가 같은 기준으로 걸리게 한다.
    /// </summary>
    public abstract class TutorialBase : ScriptableObject
    {
        /// 도구 해금으로 걸리는 튜토리얼인지. 켜면
        /// Entry 대신 Tool 이 걸리는 자리를 정한다.
        public bool IsUnlockTutorial;

        /// 이 튜토리얼이 붙는 스테이지 자리.
        public StageEntry Entry;

        /// IsUnlockTutorial 일 때 걸리는 도구.
        public DrawTool Tool;
    }
}
