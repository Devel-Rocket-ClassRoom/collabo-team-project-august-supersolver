using PPS.DrawingTool;
using UnityEngine;
using UnityEngine.UI;

public class DrawingToolSceneUI : UIScene
{
    /// 밴드를 뺀 그리기 영역. 카메라 fit 의 기준이다.
    [SerializeField] RectTransform canvasArea;

    [Header("모드별 UI")]
    [SerializeField] GameObject drawPanel;
    [SerializeField] GameObject simPanel;
    [SerializeField] GameObject speedSlot;
    [SerializeField] Button play;
    [SerializeField] Button pauseResume;
    [SerializeField] Button retry;

    [Header("그림 편집")]
    [SerializeField] Button undo;
    [SerializeField] Button redo;
    [SerializeField] Button reset;

    [Header("하위 뷰")]
    [SerializeField] ToolbarView toolbar;
    [SerializeField] InkGauge inkGauge;
    [SerializeField] ResultBanner resultBanner;
    [SerializeField] SpeedToggle speedToggle;

    /// <summary>
    /// UI 와 로직이 만나는 유일한 지점. 프리팹은 씬
    /// 오브젝트를 직렬화로 참조할 수 없어 배선이
    /// 인스펙터가 아니라 여기로 온다.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        DrawingToolManagers managers = DrawingToolManagers.Instance;

        toolbar.Bind(managers.Tools);
        inkGauge.Bind(managers.Input);
        resultBanner.Bind(managers.Driver);
        speedToggle.Bind(managers.Driver);

        StageFlow flow = managers.Flow;
        play.onClick.AddListener(flow.OnClickPlay);
        pauseResume.onClick.AddListener(flow.OnClickPauseResume);
        retry.onClick.AddListener(flow.OnClickRetry);

        DrawingSession session = managers.Session;
        undo.onClick.AddListener(session.OnClickUndo);
        redo.onClick.AddListener(session.OnClickRedo);
        reset.onClick.AddListener(session.OnClickClear);

        // 구독만으로는 첫 화면이 비어 있다. 지금 모드를
        // 한 번 비추고 나서 변화를 따라간다.
        flow.ModeChanged += ApplyMode;
        ApplyMode(flow.Mode);
    }

    /// <summary>
    /// 카메라는 씬에 있고 영역은 이 프리팹 안에 있다.
    /// 씬 쪽에서 참조가 안 되니 켜질 때 넘겨준다.
    /// </summary>
    public override void OnBeforeShow() =>
        CanvasCameraFitter.Instance.SetCanvasArea(canvasArea);

    void ApplyMode(StageMode mode)
    {
        bool drawing = mode == StageMode.Draw;

        // 하단은 높이를 유지한 채 내용만 바뀐다.
        drawPanel.SetActive(drawing);
        simPanel.SetActive(!drawing);

        // 상단 슬롯의 두 버튼은 겹쳐 있다. 한쪽을 끄지
        // 않으면 위엣것이 클릭을 전부 먹는다.
        play.gameObject.SetActive(drawing);
        pauseResume.gameObject.SetActive(!drawing);

        speedSlot.SetActive(!drawing);
    }
}
