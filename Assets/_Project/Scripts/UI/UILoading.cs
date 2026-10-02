using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로딩 화면. 스피너와 점 개수를 돌린다.</summary>
public class UILoading : UIInitialLoading
{
    /// 점이 늘어나는 최대 개수.
    private const int MaxDots = 3;

    [Header("Loading")]
    [SerializeField] private Image spinner;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Animation")]
    [SerializeField] private string labelText = "Loading";
    [SerializeField] private float spinDuration = 1f;
    [SerializeField] private float dotInterval = 0.35f;

    private Tweener _spin;
    private Sequence _dots;

    protected override UniTask OnShow()
    {
        PlaySpin();
        PlayDots();

        return UniTask.CompletedTask;
    }

    protected override UniTask OnHide()
    {
        _spin?.Kill();
        _dots?.Kill();

        _spin = null;
        _dots = null;

        return UniTask.CompletedTask;
    }

    /// 로딩 중에는 timeScale 이 0 일 수 있다.
    /// 그래서 두 연출 모두 실시간으로 돈다.
    private void PlaySpin()
    {
        _spin = spinner.rectTransform
            .DOLocalRotate(new Vector3(0f, 0f, -360f), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void PlayDots()
    {
        _dots = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        for (int i = 0; i <= MaxDots; i++)
        {
            int count = i;
            _dots.AppendCallback(() => label.text = labelText + new string('.', count));
            _dots.AppendInterval(dotInterval);
        }

        _dots.SetLoops(-1);
    }
}
