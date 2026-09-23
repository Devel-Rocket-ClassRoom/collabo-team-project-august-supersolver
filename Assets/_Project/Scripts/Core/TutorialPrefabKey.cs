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

        /// 장치가 무엇을 하는지 보여 주는 안내 그림.
        GuideBouncer,
        GuideBomb,
        GuideWind,
        GuideBarricade,
        GuideBat,

        /// 자유물체를 어디에 그려야 하는지 짚는 그림.
        GuideFreeBody,
    }
}
