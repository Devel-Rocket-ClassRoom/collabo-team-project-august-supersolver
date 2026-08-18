using UnityEngine;

namespace PPS.Core
{
    /// 튜토리얼이 가리킬 수 있는 UI 자리.
    /// 실제 RectTransform 은 TutorialViewer 가 물린다.
    public enum TutorialAnchor
    {
        Settings,
        Play,
        PauseResume,
        Speed,
        Inkband,
        FixedLine,
        Freebody,
        Erase,
        PivotGroup,
        Reset,
        Undo,
        Redo,
        Retry,
    }

    /// <summary>
    /// 스테이지 하나에 붙는 튜토리얼 한 컷.
    /// 소비자가 PPS.Core 안에 있어 여기 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "Tutorial", menuName = "Scriptable Objects/Tutorial")]
    public class Tutorial : ScriptableObject
    {
        /// 이 컷이 붙는 스테이지. ThemeModel.Stages 의 인덱스다.
        public int StageIndex;

        /// 이 시간이 지나면 스스로 사라진다.
        public float Duration = 2f;

        /// 이 컷이 붙을 UI 자리.
        public TutorialAnchor Target;

        /// 앵커 자리에 띄울 것. 사라질 때 같이 파괴된다.
        public GameObject Prefab;
    }
}
