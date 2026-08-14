using PPS.DrawingTool;
using UnityEngine;

public class DrawingToolSceneUI : UIScene
{
    /// 밴드를 뺀 그리기 영역. 카메라 fit 의 기준이다.
    [SerializeField] RectTransform canvasArea;

    /// 월드 좌표로 그리는 것들. UI 와 같이 저작하려고
    /// 프리팹에 넣었을 뿐 화면 좌표계에 속하지 않는다.
    [SerializeField] Transform managers;

    /// <summary>
    /// Canvas 스케일이 자식의 크기에 곱해져 월드
    /// 오브젝트가 화면 배율만큼 작아진다. 위치는
    /// 월드로 잡혀 있어 크기만 어긋난다.
    /// </summary>
    public override void Initialize()
    {
        managers.SetParent(null, false);
        base.Initialize();
        managers.gameObject.SetActive(false);
    }

    /// <summary>
    /// 카메라는 씬에 있고 영역은 이 프리팹 안에 있다.
    /// 씬 쪽에서 참조가 안 되니 켜질 때 넘겨준다.
    /// </summary>
    public override void OnBeforeShow()
    {
        CanvasCameraFitter.Instance.SetCanvasArea(canvasArea);
        managers.gameObject.SetActive(true);
    }

    /// 부모가 갈라져 패널을 따라 꺼지지 않는다.
    public override void OnAfterHide() => managers.gameObject.SetActive(false);
}
