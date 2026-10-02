using Cysharp.Threading.Tasks;
using PPS.DrawingTool;
using UnityEngine;

/// <summary>
/// 상단 설정 버튼. 팝업이 화면을 덮는 동안 공이 계속
/// 굴러가면 플레이어가 손해라 여는 김에 판을 세운다.
/// </summary>
public class SettingsButton : MonoBehaviour
{
    [SerializeField] private StageFlow flow;

    public void OnClick()
    {
        flow.PauseForPopup();
        UIManager.Instance.ShowPopup<SettingsView>().Forget();
    }
}
