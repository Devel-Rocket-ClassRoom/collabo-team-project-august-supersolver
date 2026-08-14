using System;
using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 그리기 ↔ 시뮬레이션 ↔ 일시정지 전이의 유일한 주인.
    /// 버튼 onClick 이 여기로 들어온다 — 핸들러마다 전이를
    /// 흩뿌리면 재시도×일시정지 조합에서 추적이 끊긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageFlow : MonoBehaviour
    {
        [SerializeField] StageLoader _stage;
        [SerializeField] DrawingSession _session;
        [SerializeField] GameSimDriver _driver;
        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] SimStageView _simView;

        readonly StageStateMachine _flow = new StageStateMachine();

        public StageMode Mode => _flow.Mode;

        /// 모드가 바뀔 때마다. StageModeView 가 듣는다.
        public event Action<StageMode> ModeChanged;

        void Start() => Apply();

        public void OnClickPlay()
        {
            if (!_flow.Play()) return;

            // 획을 그리는 중에도 두 번째 손가락이 버튼을 누른다.
            // 남겨두면 프리뷰 선이 화면에 박힌다.
            _input.CancelStroke();

            // 여기서 아무것도 저장하지 않는다. 그림을 파일로
            // 뽑는 일은 에디터 도구 몫이다(StageFlowInspector) —
            // 게임에는 그 파일을 읽는 코드가 없다.
            _driver.StartSimulation(_stage.Stage, _session.Solution);

            // 스텝이 돌기 전에 잡아야 획이 제자리에서 출발한다.
            _simView.Begin();

            Apply();
        }

        public void OnClickPauseResume()
        {
            if (!_flow.PauseResume()) return;

            _driver.Paused = _flow.Mode == StageMode.Paused;
            Apply();
        }

        /// <summary>
        /// 되감기가 아니라 전파괴다. 되돌리기 스택은 건드리지
        /// 않는다 — 재시도 뒤에도 되돌릴 수 있어야 한다.
        /// 도구도 그대로다. ToolSelection 이 씬에 살아 있어
        /// 패널을 껐다 켜면 마지막에 고른 것이 돌아온다.
        /// </summary>
        public void OnClickRetry()
        {
            if (!_flow.Retry()) return;

            _driver.Stop();
            _simView.Reset();

            Apply();
        }

        void Apply()
        {
            // 캔버스 입력은 그리기에서만 산다.
            _input.enabled = _flow.Mode == StageMode.Draw;

            ModeChanged?.Invoke(_flow.Mode);
        }
    }
}
