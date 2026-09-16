namespace PPS.Core
{
    /// <summary>
    /// 튜토리얼 컷이 띄울 것의 이름. SO 가 프리팹을 직접
    /// 들면 직렬화가 씬 밖으로 못 나가서 이름만 든다.
    /// </summary>
    public enum TutorialPrefabKey
    {
        /// 띄울 것 없이 조건만 기다리는 컷.
        None = 0,

        DrawGesture,
        Pointer,
    }
}
