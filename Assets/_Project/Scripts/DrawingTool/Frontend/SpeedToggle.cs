using PPS.Game;
using TMPro;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 배속 두 단. 전이가 아니라서 StageFlow 밖에 있다 —
    /// 모드도 UI 구성도 바뀌지 않는다. 코어가 누적 시간만
    /// 늘리고 FixedDt 는 그대로라 결과는 배속과 무관하다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeedToggle : MonoBehaviour
    {
        const float Fast = 2f;

        [SerializeField] GameSimDriver _driver;
        [SerializeField] TMP_Text _label;

        /// 판이 바뀌어도 배속은 유지된다. 다시 보일 때
        /// 라벨을 맞춰 두지 않으면 실제 값과 어긋난다.
        void OnEnable() => Show();

        public void OnClick()
        {
            _driver.SpeedMultiplier = IsFast ? 1f : Fast;
            Show();
        }

        bool IsFast => _driver.SpeedMultiplier > 1f;

        void Show() => _label.text = IsFast ? "2x" : "1x";
    }
}
