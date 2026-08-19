using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 월드 쪽 조립자. 순수 C# 코어를 세워 뷰에 물리고
    /// 프레임 틱을 넘긴다. 코어를 UI 계층 아래 두면 캔버스
    /// 배율이 월드 오브젝트 크기에 곱해지므로, 이 프리팹은
    /// UI 와 별개로 놓인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawingToolComposite : MonoBehaviour
    {
        [SerializeField] LevelView _levelView;
        [SerializeField] PivotMarkerView _pivots;
        [SerializeField] StrokePreviewRenderer _strokes;
        [SerializeField] SimStageView _simView;
        [SerializeField] DrawInputBehaviour _input;

        DrawingSession _session;
        ToolSelection _tools;
        GameSimDriver _driver;
        StageFlow _flow;
        StageLoader _stages;

        public DrawingSession Session => _session;
        public ToolSelection Tools => _tools;
        public GameSimDriver Driver => _driver;
        public StageFlow Flow => _flow;
        public StageLoader Stages => _stages;

        public LevelView LevelView => _levelView;
        public DrawInputBehaviour Input => _input;

        void Awake()
        {
            _session = new DrawingSession();
            _tools = new ToolSelection();
            _driver = new GameSimDriver();
            _flow = new StageFlow(_session, _driver, _input, _simView);
            _stages = new StageLoader(_input, _levelView, _flow);

            _input.Bind(_tools, _session);
            _strokes.Bind(_input, _session, _tools);
            _pivots.Bind(_session);
            _simView.Bind(_driver, _session, _levelView, _strokes, _pivots);
        }

        // 드라이버가 스스로 Time 을 읽지 않는다. 여기서
        // 돌려야 뷰의 LateUpdate 보다 앞선다는 순서가 선다.
        void Update() => _driver.Tick(Time.deltaTime);

        void OnDestroy() => _driver?.Dispose();
    }
}
