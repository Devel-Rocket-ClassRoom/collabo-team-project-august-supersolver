using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원을 그리는 손가락을 글자 없이 보여 준다. 컷과 달리
/// 고정 표시는 Drag 를 못 받아서 도는 크기를 프리팹이
/// 쥐고 스스로 돈다.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialRingGesture : MonoBehaviour
{
    /// 손가락이 닿은 자리. 원을 따라 돈다.
    [SerializeField] RectTransform _dot;

    /// 닿는 순간 퍼지는 파문. 어디서 시작하는지 짚어 준다.
    [SerializeField] Graphic _ripple;

    /// 도는 원의 반지름. 옆에 깔린 점선과 같아야 한다.
    [SerializeField] float _radius = 70f;

    /// 손가락이 원 꼭대기에서 출발해 시계 방향으로 돈다.
    const float StartDegrees = 90f;

    const float RippleScale = 2.4f;
    const float RipplePeriod = 0.45f;
    const float Lap = 1.7f;
    const float Fade = 0.15f;
    const float Rest = 0.3f;

    Tween _tween;

    // 컷이 불러 주는 Play 가 없다. 떠 있는 동안 계속 돈다.
    void OnEnable() => _tween = Trace();

    void OnDisable() => _tween?.Kill();

    Sequence Trace()
    {
        _tween?.Kill();
        Graphic dot = _dot.GetComponent<Graphic>();

        float turn = 0f;
        Tween lap = DOTween.To(() => turn, value =>
        {
            turn = value;
            _dot.anchoredPosition = At(value);
        }, 1f, Lap).SetEase(Ease.InOutSine);

        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                _dot.anchoredPosition = At(0f);
                dot.color = Alpha(dot.color, 0f);
            })
            .Append(dot.DOFade(1f, Fade))
            .Join(Touchdown())
            .Append(lap)
            .Append(dot.DOFade(0f, Fade))
            .AppendInterval(Rest)
            .SetLoops(-1);
    }

    /// <param name="turn">한 바퀴를 0~1 로 센 값.</param>
    Vector2 At(float turn)
    {
        float radians = (StartDegrees - turn * 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _radius;
    }

    /// 손가락을 내려놓는 순간을 한 번 튕긴다. 파문은 돌지
    /// 않아서 한 바퀴가 어디서 닫히는지 남는다.
    Sequence Touchdown()
    {
        var rect = (RectTransform)_ripple.transform;
        rect.anchoredPosition = At(0f);

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
