using PPS.Core;
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

        [Header("모드별 UI")]
        [SerializeField] GameObject _drawPanel;
        [SerializeField] GameObject _simPanel;
        [SerializeField] GameObject _play;
        [SerializeField] GameObject _pauseResume;
        [SerializeField] GameObject _pauseIcon;
        [SerializeField] GameObject _replayIcon;
        [SerializeField] GameObject _speed;
        [SerializeField] GameObject _retry;
        [SerializeField] GameObject _reset;
        [SerializeField] GameObject _undo;
        [SerializeField] GameObject _redo;

        readonly StageStateMachine _flow = new StageStateMachine();

        public StageMode Mode => _flow.Mode;

        void Start()
        {
            Apply();
            ServiceLocator.Get<IRewardView>().BindButtonListener(
                retry: () => { 
                    OnClickRetry(); 
                    ServiceLocator.Get<IRewardView>().Hide(); 
                },
                home: null,  // DI 때문에 StageSceneLoaderOnClick.cs 에서 주입
                next: null); // DI 때문에 StageSceneLoaderOnClick.cs 에서 주입
        }
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

        public void OnClickPlay()
        {
            if (!_flow.Play()) return;

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
        /// 팝업이 화면을 덮는 동안 공이 굴러가면 손해다.
        /// 닫을 때 되돌리지 않는다 — 재개는 플레이어가 정한다.
        /// </summary>
        public void PauseForPopup()
        {
            if (_flow.Mode != StageMode.Simulate) return;

            OnClickPauseResume();
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
            bool drawing = _flow.Mode == StageMode.Draw;

            // 캔버스 입력은 그리기에서만 산다.
            _input.enabled = drawing;

            // 하단은 높이를 유지한 채 내용만 바뀐다.
            _drawPanel.SetActive(drawing);
            _simPanel.SetActive(!drawing);

            // 상단 슬롯의 두 버튼은 겹쳐 있다. 한쪽을 끄지
            // 않으면 위엣것이 클릭을 전부 먹는다.
            _play.SetActive(drawing);
            _pauseResume.SetActive(!drawing);

            // 한 버튼에 아이콘 둘이 겹쳐 있다. 지금 누르면
            // 무엇이 되는지를 보인다 — 굴러가는 중이면 멈춤,
            // 멈춰 있으면 재개.
            bool paused = _flow.Mode == StageMode.Paused;
            _pauseIcon.SetActive(!paused);
            _replayIcon.SetActive(paused);

            _speed.SetActive(!drawing);
            _retry.SetActive(!drawing);

            // 편집 버튼은 DrawPanel 밖에 있어 패널을
            // 따라 꺼지지 않는다. 시뮬 중에 초기화가
            // 살아 있으면 그림이 지워진다.
            _reset.SetActive(drawing);
            _undo.SetActive(drawing);
            _redo.SetActive(drawing);
        }
    }
}
