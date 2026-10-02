using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 재시도 버튼을 반짝여 공이 멈췄다고 알린다.
    /// 정지 판정에는 팝업도 자동 재시작도 없어 이 연출이
    /// 판이 끝났음을 말하는 유일한 신호다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class RetryBlink : MonoBehaviour
    {
        /// 어두워졌다 돌아오기까지 한쪽 방향에 드는 시간.
        const float Period = 0.5f;

        /// 가장 어두울 때의 밝기 배율. 버튼 바탕이 거의
        /// 흰색이라 밝히는 쪽으로는 갈 자리가 없다.
        const float DimScale = 0.72f;

        Graphic _bg;

        /// 연출 전의 바탕색. 되돌릴 곳이 여기뿐이다.
        Color _base;

        Tween _tween;

        void Awake()
        {
            _bg = GetComponent<Graphic>();
            _base = _bg.color;
        }

        /// <summary>
        /// 누를 때까지 돈다. 그리기로 돌아가면 StageFlow 가
        /// 버튼을 꺼 OnDisable 이 걷어 간다.
        /// </summary>
        public void Play()
        {
            _tween?.Kill();
            _bg.color = _base;

            Color dim = new Color(_base.r * DimScale,
                                  _base.g * DimScale,
                                  _base.b * DimScale,
                                  _base.a);

            _tween = DOTween.To(() => _bg.color, c => _bg.color = c, dim, Period)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
            _bg.color = _base;
        }
    }
}
