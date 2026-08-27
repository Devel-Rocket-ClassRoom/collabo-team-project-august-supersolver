using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캔버스를 긋는 손가락을 글자 없이 보여 준다.
/// 버튼을 누르는 탭은 이것을 안 쓴다 — 그쪽은
/// 프리팹이 통째로 다르다.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialGesture : MonoBehaviour
{
    /// 손가락이 닿은 자리.
    [SerializeField] RectTransform _dot;

    /// 닿는 순간 퍼지는 파문. 탭을 읽히게 하는 유일한 단서다.
    [SerializeField] Graphic _ripple;

    const float RippleScale = 2.4f;
    const float RipplePeriod = 0.45f;
    const float DragTravel = 1f;
    const float DragFade = 0.15f;
    const float DragRest = 0.25f;

    Tween _tween;

    void OnDisable() => _tween?.Kill();

    /// <summary>
    /// 끝날 때까지 도는 연출을 건다. 컷이 파괴되면
    /// OnDisable 이 트윈을 걷어 간다.
    /// </summary>
    public void Play(Vector2 drag)
    {
        _tween?.Kill();
        _tween = Drag(drag);
    }

    Sequence Drag(Vector2 drag)
    {
        Graphic dot = _dot.GetComponent<Graphic>();

        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                _dot.anchoredPosition = Vector2.zero;
                dot.color = Alpha(dot.color, 0f);
            })
            .Append(dot.DOFade(1f, DragFade))
            .Join(Touchdown())
            .Append(_dot.DOAnchorPos(drag, DragTravel).SetEase(Ease.InOutSine))
            .Append(dot.DOFade(0f, DragFade))
            .AppendInterval(DragRest)
            .SetLoops(-1);
    }

    /// 손가락을 내려놓는 순간을 한 번 튕긴다. 획이
    /// 어디서 시작하는지 짚어 준다 — 점만 미끄러지면
    /// 시작점이 눈에 안 남는다.
    Sequence Touchdown()
    {
        var rect = (RectTransform)_ripple.transform;

        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                rect.localScale = Vector3.one;
                _ripple.color = Alpha(_ripple.color, 0.55f);
            })
            .Append(rect.DOScale(RippleScale, RipplePeriod).SetEase(Ease.OutQuad))
            .Join(_ripple.DOFade(0f, RipplePeriod));
    }

    static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
}
