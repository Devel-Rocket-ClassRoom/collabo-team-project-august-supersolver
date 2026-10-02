using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ThemeSelectButton : MonoBehaviour
{
    [SerializeField] Image flag;
    [SerializeField] Image locked;
    [SerializeField] Button button;

    [Header("Unlock Animation")]
    /// 자물쇠가 쪼그라들며 사라지는 시간.
    [SerializeField] float lockFadeDuration = .35f;

    /// 자물쇠가 풀린 뒤 버튼이 튀는 세기와 시간.
    [SerializeField] float punchStrength = .3f;
    [SerializeField] float punchDuration = .5f;

    /// 이 테마가 해금되었는가. 로드 중 일괄 비활성을
    /// 되돌릴 때 잠긴 버튼까지 켜지 않으려고 남긴다.
    bool unlocked;

    public void Init(Sprite icon, bool unlocked, Action onClick)
    {
        this.unlocked = unlocked;
        flag.sprite = icon;
        locked.enabled = !unlocked;

        // 해금 연출이 자물쇠를 줄이고 지운 채 끝난다.
        // 버튼은 돌려 쓰므로 띄울 때마다 되돌린다.
        locked.transform.localScale = Vector3.one;
        locked.color = new Color(locked.color.r, locked.color.g, locked.color.b, 1f);

        button.interactable = unlocked;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick());
    }

    /// <summary>
    /// 자물쇠가 사라지고 버튼이 튄다. 잠긴 채로 띄운
    /// 버튼에만 쓴다 — 끝나면 열린 상태로 남는다.
    /// </summary>
    public async UniTask PlayUnlock()
    {
        unlocked = true;

        Sequence sequence = DOTween.Sequence().SetLink(gameObject);

        sequence.Append(locked.transform.DOScale(0f, lockFadeDuration).SetEase(Ease.InBack));
        sequence.Join(locked.DOFade(0f, lockFadeDuration));
        sequence.AppendCallback(() => locked.enabled = false);
        sequence.Append(transform.DOPunchScale(Vector3.one * punchStrength, punchDuration));

        await sequence.AsyncWaitForCompletion();

        button.interactable = true;
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value && unlocked;
    }
}
