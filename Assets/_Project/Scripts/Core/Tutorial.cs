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
        PivotLink,
        Reset,
        Undo,
        Redo,
        Retry,

        // 새 자리는 뒤에만 붙인다. 값이 밀리면 씬에
        // 배선된 앵커와 저장된 컷이 통째로 어긋난다.
        PivotWorld,
        CanvasArea,
    }

    /// 컷이 다음으로 넘어가는 조건.
    public enum TutorialAdvance
    {
        /// Duration 이 지나면.
        Time,

        /// 대상 앵커의 버튼이 눌리면.
        Press,

        /// 캔버스의 그림이 달라지면.
        Stroke,

        /// 시뮬레이션 판정이 나면. 실패만 기다리면
        /// 공이 굴러가다 멈춘 판에서 영영 안 넘어간다.
        SimDecided,
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

        /// Time 조건일 때 컷이 버티는 시간.
        [Tooltip("Advance 가 Time 일 때만 쓰인다. 다른 조건에서는 " +
                 "대상 버튼을 못 찾았을 때 빠져나오는 값이다.")]
        public float Duration = 2f;

        /// 이 컷이 붙을 UI 자리.
        public TutorialAnchor Target;

        /// 앵커 자리에 띄울 것. 사라질 때 같이 파괴된다.
        public GameObject Prefab;

        /// 앵커 자리에서 이만큼 민 곳에 손가락을 얹는다.
        public Vector2 Offset;

        /// 손가락이 끄는 거리.
        [Tooltip("긋는 제스처 프리팹만 읽는다. 버튼을 짚는 " +
                 "고래 포인터에는 뜻이 없다.")]
        public Vector2 Drag;

        /// 다음 컷으로 넘어가는 조건. Time 일 때만
        /// Duration 을 쓴다.
        public TutorialAdvance Advance;
    }
}
