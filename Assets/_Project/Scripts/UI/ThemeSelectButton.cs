using System;
using UnityEngine;
using UnityEngine.UI;

public class ThemeSelectButton : MonoBehaviour
{
    [SerializeField] Image flag;
    [SerializeField] Image locked;
    [SerializeField] Button button;

    /// 이 테마가 해금되었는가. 로드 중 일괄 비활성을
    /// 되돌릴 때 잠긴 버튼까지 켜지 않으려고 남긴다.
    bool unlocked;

    public void Init(Sprite icon, bool unlocked, Action onClick)
    {
        this.unlocked = unlocked;
        flag.sprite = icon;
        locked.enabled = !unlocked;
        button.interactable = unlocked;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick());
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value && unlocked;
    }
}
