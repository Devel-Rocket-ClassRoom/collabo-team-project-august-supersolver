using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;

/// <summary>
/// 드로잉툴 화면. UI 와 월드의 유일한 경계다 —
/// 버튼 onClick 이 여기로 들어오고, 코어는 클릭을 모른다.
/// </summary>
public class DrawingToolSceneUI : UIScene
{
    /// 밴드를 뺀 그리기 영역. 카메라 fit 의 기준이다.
    [SerializeField] RectTransform canvasArea;

    /// 월드 쪽 프리팹. UI 계층에 넣으면 캔버스 배율이
    /// 월드 오브젝트의 lossyScale 에 곱해져 따로 세운다.
    [SerializeField] DrawingToolComposite worldPrefab;

    [Header("모드별 UI")]
    [SerializeField] GameObject drawPanel;
    [SerializeField] GameObject simPanel;
    [SerializeField] GameObject play;
    [SerializeField] GameObject pauseResume;
    [SerializeField] GameObject speed;

    [Header("코어를 듣는 표시")]
    [SerializeField] ToolbarView toolbar;
    [SerializeField] SpeedToggle speedToggle;
    [SerializeField] InkGauge inkGauge;
    [SerializeField] ResultBanner resultBanner;
    [SerializeField] TutorialViewer tutorial;

    DrawingToolComposite _world;

    public override void Initialize()
    {
        // 부모를 주지 않는다. 캔버스 밑에 두면 화면 배율이
        // 월드 오브젝트 크기에 그대로 곱해진다.
        _world = Instantiate(worldPrefab);

        toolbar.Bind(_world.Tools);
        speedToggle.Bind(_world.Driver);
        inkGauge.Bind(_world.Input);
        resultBanner.Bind(_world.Driver);

        _world.Flow.ModeChanged += ApplyMode;
        ApplyMode(_world.Flow.Mode);

        _world.gameObject.SetActive(false);

        base.Initialize();
    }

    /// <summary>
    /// 카메라는 씬에 있고 영역은 이 프리팹 안에 있다.
    /// 씬 쪽에서 참조가 안 되니 켜질 때 넘겨준다.
    /// </summary>
    public override void OnBeforeShow()
    {
        CanvasCameraFitter.Instance.SetCanvasArea(canvasArea);
        _world.gameObject.SetActive(true);
    }

    /// 월드는 UI 계층 밖이라 패널을 따라 꺼지지 않는다.
    public override void OnAfterHide() => _world.gameObject.SetActive(false);

    /// 월드의 주인이 여기다. 패널이 사라지면 같이 버린다.
    void OnDestroy()
    {
        if (_world == null) return;

        _world.Flow.ModeChanged -= ApplyMode;
        Destroy(_world.gameObject);
    }

    /// <summary>판과 튜토리얼을 함께 물린다. 스테이지 버튼이 부른다.</summary>
    public void EnterStage(StageData stage, int stageIndex)
    {
        _world.Stages.SetStage(stage);
        tutorial.Play(stageIndex);
    }

    void ApplyMode(StageMode mode)
    {
        bool drawing = mode == StageMode.Draw;

        // 하단은 높이를 유지한 채 내용만 바뀐다.
        drawPanel.SetActive(drawing);
        simPanel.SetActive(!drawing);

        // 상단 슬롯의 두 버튼은 겹쳐 있다. 한쪽을 끄지
        // 않으면 위엣것이 클릭을 전부 먹는다.
        play.SetActive(drawing);
        pauseResume.SetActive(!drawing);

        speed.SetActive(!drawing);
    }

    public void OnClickFixedLine() => _world.Tools.SelectFixedLine();

    public void OnClickFreeBody() => _world.Tools.SelectFreeBody();

    public void OnClickPivotSingle() => _world.Tools.SelectPivotSingle();

    public void OnClickPivotWorld() => _world.Tools.SelectPivotWorld();

    public void OnClickErase() => _world.Tools.SelectErase();

    public void OnClickUndo() => _world.Session.Undo();

    public void OnClickRedo() => _world.Session.Redo();

    public void OnClickReset() => _world.Session.Clear();

    public void OnClickPlay() => _world.Flow.Play();

    public void OnClickPauseResume() => _world.Flow.PauseResume();

    public void OnClickRetry() => _world.Flow.Retry();
}
