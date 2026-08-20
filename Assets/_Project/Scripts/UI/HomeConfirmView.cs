using System;
using UnityEngine;

/// <summary>홈으로 나갈지 묻는 팝업.</summary>
public class HomeConfirmView : UIPopup
{
    private Action _confirmed;

    /// 홈 이동은 씬 전환이라 이 프리팹 밖에 산다.
    /// 애드레서블로 따로 로드돼 인스펙터로 못 묶는다.
    public void BindConfirm(Action confirmed) => _confirmed = confirmed;

    public void OnClickYes()
    {
        // 배선을 빠뜨리면 예를 눌러도 아무 일이 없다.
        if (_confirmed == null)
        {
            Debug.LogError("[HomeConfirmView] 홈 동작이 안 물려 있다.", this);
            return;
        }

        _confirmed();
    }

    /// 예와 짝을 맞춰 둔다. 닫는 일 자체는 UIPopup 몫이다.
    public void OnClickNo() => OnClickBack();
}
