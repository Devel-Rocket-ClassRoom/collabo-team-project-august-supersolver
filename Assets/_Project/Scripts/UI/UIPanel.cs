using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    public virtual void Initialize()
    {
        gameObject.SetActive(false);
    }

    public async UniTask Show()
    {
        gameObject.SetActive(true);
        await OnShowAnimation();
        await OnShow();
    }

    public async UniTask Hide(bool instant = false)
    {
        if (!instant) await OnHideAnimation();
        await OnHide();
        gameObject.SetActive(false);
    }

    protected virtual UniTask OnShowAnimation() => UniTask.CompletedTask;
    protected virtual UniTask OnShow() => UniTask.CompletedTask;
    protected virtual UniTask OnHideAnimation() => UniTask.CompletedTask;
    protected virtual UniTask OnHide() => UniTask.CompletedTask;

    public virtual void OnBeforeShow() { }
    public virtual void OnAfterHide() { }
}