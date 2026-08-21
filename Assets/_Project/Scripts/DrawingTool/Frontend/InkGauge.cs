using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 잉크 잔량 바. 그리는 중 값은 프리뷰 근사고,
    /// 진실은 획을 확정한 뒤의 재계산이다 — 숫자도
    /// 그리는 동안에는 근사를 보여준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InkGauge : MonoBehaviour
    {
        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] Slider _bar;
        [SerializeField] TextMeshProUGUI _percent;
        [SerializeField] TextMeshProUGUI _totalInk;

        /// 마지막으로 글자에 써 넣은 값.
        /// TMP 는 같은 문자열을 넣어도 메시를 다시 만든다.
        int _shownPercent = -1;
        float _shownTotal = -1f;

        void Update()
        {
            float ratio = _input.InkRatio;
            _bar.value = ratio;

            // 내림이라 0% 는 진짜로 못 긋는 상태다.
            int percent = Mathf.FloorToInt(ratio * 100f);
            if (percent != _shownPercent)
            {
                _shownPercent = percent;
                _percent.text = percent + "%";
            }

            // 상한은 판이 바뀔 때만 움직인다. 갈아 끼우는
            // 시점을 알리는 자리가 없어 여기서 훑는다.
            float total = _input.InkLimit;
            if (total != _shownTotal)
            {
                _shownTotal = total;
                _totalInk.text = total.ToString("0.#");
            }
        }
    }
}
