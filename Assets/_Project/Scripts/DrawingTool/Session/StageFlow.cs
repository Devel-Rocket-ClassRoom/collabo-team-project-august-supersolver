using System;
using PPS.Core;
using PPS.Game;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 그리기 ↔ 시뮬레이션 ↔ 일시정지 전이의 유일한 주인.
    /// 핸들러마다 전이를 흩뿌리면 재시도×일시정지 조합에서
    /// 추적이 끊긴다.
    /// 모드별로 무엇을 보일지는 모른다 — 모드만 알리고
    /// 화면 구성은 듣는 쪽이 정한다.
    /// </summary>
    public sealed class StageFlow
    {
        readonly StageStateMachine _flow = new StageStateMachine();

        readonly DrawingSession _session;
        readonly GameSimDriver _driver;
        readonly DrawInputBehaviour _input;
        readonly SimStageView _simView;

        /// 지금 판. StageLoader 가 물려 준다.
        StageData _stage;

        public StageMode Mode => _flow.Mode;

        /// 모드가 바뀔 때마다. UI 가 듣고 패널을 고른다.
        public event Action<StageMode> ModeChanged;

        public StageFlow(
            DrawingSession session, GameSimDriver driver,
            DrawInputBehaviour input, SimStageView simView)
        {
            _session = session;
            _driver = driver;
            _input = input;
            _simView = simView;

            Apply();
        }

        /// <summary>
        /// 새 판을 물린다. 전이가 아니라 배선이라 모드를
        /// 건드리지 않는다 — 판을 갈아 끼우는 일은
        /// EnterStage 가 한다.
        /// </summary>
        public void SetStage(StageData stage) => _stage = stage;

        /// <summary>
        /// 판이 갈렸다. 그림·시뮬·모드가 전부 이전 판의
        /// 것이라 통째로 버린다 — 남겨 두면 스테이지 2 에서
        /// 스테이지 1 의 획과 공이 그대로 보인다.
        /// </summary>
        public void EnterStage()
        {
            // 월드를 먼저 버린다. 살아 있으면 SimStageView 가
            // 새 판의 공을 이전 판 자리로 끌고 간다.
            _driver.Stop();

            _session.ResetForStage();
            _simView.Reset();
            _flow.Retry();

            Apply();
        }

        public void Play()
        {
            if (!_flow.Play()) return;

            // 획을 그리는 중에도 두 번째 손가락이 버튼을 누른다.
            // 남겨두면 프리뷰 선이 화면에 박힌다.
            _input.CancelStroke();

            // 여기서 아무것도 저장하지 않는다. 그림을 파일로
            // 뽑는 일은 에디터 도구 몫이다(StageFlowInspector) —
            // 게임에는 그 파일을 읽는 코드가 없다.
            _driver.StartSimulation(_stage, _session.Solution);

            // 스텝이 돌기 전에 잡아야 획이 제자리에서 출발한다.
            _simView.Begin();

            Apply();
        }

        public void PauseResume()
        {
            if (!_flow.PauseResume()) return;

            _driver.Paused = _flow.Mode == StageMode.Paused;
            Apply();
        }

        /// <summary>
        /// 되감기가 아니라 전파괴다. 되돌리기 스택은 건드리지
        /// 않는다 — 재시도 뒤에도 되돌릴 수 있어야 한다.
        /// 도구도 그대로다. ToolSelection 이 코어에 살아 있어
        /// 패널을 껐다 켜면 마지막에 고른 것이 돌아온다.
        /// </summary>
        public void Retry()
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
