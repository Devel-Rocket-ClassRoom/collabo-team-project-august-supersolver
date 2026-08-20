using Cysharp.Threading.Tasks;

/// <summary>설정 팝업. 지금은 홈으로 나가는 길만 있다.</summary>
public class SettingsView : UIPopup
{
    /// 홈은 판을 버리는 길이라 한 번 더 묻는다.
    public void OnClickHome()
        => UIManager.Instance.ShowPopup<HomeConfirmView>().Forget();
}
