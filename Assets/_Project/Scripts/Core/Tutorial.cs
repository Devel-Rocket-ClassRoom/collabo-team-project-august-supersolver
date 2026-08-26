using UnityEngine;

namespace PPS.Core
{
    /// 튜토리얼이 가리킬 수 있는 UI 자리.
    /// 실제 RectTransform 은 TutorialViewer 가 물린다.
    public enum TutorialAnchor
    {
        [InspectorName("설정 버튼")] Settings,
        [InspectorName("재생 버튼")] Play,
        [InspectorName("일시정지·재개 버튼")] PauseResume,
        [InspectorName("배속 버튼")] Speed,
        [InspectorName("잉크 게이지")] Inkband,
        [InspectorName("고정선 탭")] FixedLine,
        [InspectorName("자유물체 탭")] Freebody,
        [InspectorName("지우개 탭")] Erase,
        [InspectorName("회전축 탭 — 연결")] PivotLink,
        [InspectorName("회전축 탭 — 월드 고정")] PivotWorld,
        [InspectorName("초기화 버튼")] Reset,
        [InspectorName("되돌리기 버튼")] Undo,
        [InspectorName("다시실행 버튼")] Redo,
        [InspectorName("재시도 버튼")] Retry,
        [InspectorName("캔버스 영역")] CanvasArea,
    }

    /// 컷이 다음으로 넘어가는 조건.
    public enum TutorialAdvanceCondition
    {
        /// Duration 이 지나면.
        [InspectorName("시간 — Duration 초")]
        Time,

        /// 대상 앵커의 버튼이 눌리면.
        [InspectorName("버튼 누름 — 대상 앵커")]
        Press,

        /// 그림이 달라지면. 획·핀·지우기가 다 여기로 온다 —
        /// 무엇을 한 것인지는 앞 컷이 고른 도구가 정한다.
        [InspectorName("그림이 달라짐 — 획·핀·지우기")]
        DrawingChanged,

        /// 시뮬레이션 판정이 나면. 실패만 기다리면
        /// 공이 굴러가다 멈춘 판에서 영영 안 넘어간다.
        [InspectorName("시뮬 판정 남 — 성공·실패 무관")]
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
        [Tooltip("Condition 이 Time 일 때만 쓰인다. 다른 조건에서는 " +
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
        public TutorialAdvanceCondition Condition;
    }
}
