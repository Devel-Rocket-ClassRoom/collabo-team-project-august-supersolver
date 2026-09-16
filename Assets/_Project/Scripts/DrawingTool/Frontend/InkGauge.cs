using PPS.Core;
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

        /// 잉크 등급을 알리는 별. 그림 하나를 등급 색으로 물들인다.
        [SerializeField] Image _gradeStar;

        /// 등급이 갈리는 지점을 짚는 눈금. 각각 금↔은, 은↔동 경계다.
        [SerializeField] RectTransform _goldTick;
        [SerializeField] RectTransform _silverTick;

        /// 보상·스테이지 선택 화면이 쓰는 별 그림에서 뽑은 색.
        /// 같은 등급이 화면마다 다른 색으로 보이면 안 된다.
        static readonly Color GoldTint = new Color32(0xF8, 0xB4, 0x3B, 0xFF);
        static readonly Color SilverTint = new Color32(0xA4, 0xAB, 0xB5, 0xFF);
        static readonly Color BronzeTint = new Color32(0xC3, 0x72, 0x3F, 0xFF);

        /// 마지막으로 글자에 써 넣은 값.
        /// TMP 는 같은 문자열을 넣어도 메시를 다시 만든다.
        int _shownPercent = -1;
        float _shownTotal = -1f;
        int _shownGrade = -1;

        /// 눈금 자리는 비율이라 판이 바뀌어도 움직이지 않는다.
        void Awake()
        {
            PlaceTick(_goldTick, InkGrade.GoldPercent);
            PlaceTick(_silverTick, InkGrade.SilverPercent);
        }

        /// <summary>등급이 갈리는 잔량 지점에 눈금을 세운다.</summary>
        /// 바가 잔량이라 사용량 축을 뒤집어야 제자리에 선다.
        /// 아트에 맞춘 픽셀 보정은 프리팹이 쥐고 있다.
        static void PlaceTick(RectTransform tick, float usedPercent)
        {
            float x = 1f - usedPercent / 100f;

            tick.anchorMin = new Vector2(x, tick.anchorMin.y);
            tick.anchorMax = new Vector2(x, tick.anchorMax.y);
        }

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

            // 바와 같은 값을 봐야 눈금과 별이 따로 놀지
            // 않는다. 그리는 중에는 둘 다 프리뷰 근사다.
            int grade = InkGrade.Of(total * (1f - ratio), total);
            if (grade != _shownGrade)
            {
                _shownGrade = grade;
                _gradeStar.color = ColorOf(grade);
            }
        }

        static Color ColorOf(int grade)
        {
            switch (grade)
            {
                case InkGrade.Gold: return GoldTint;
                case InkGrade.Silver: return SilverTint;
                default: return BronzeTint;
            }
        }
    }
}
