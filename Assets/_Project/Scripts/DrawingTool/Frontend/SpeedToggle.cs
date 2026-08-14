using PPS.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 배속 두 단. 전이가 아니라서 StageFlow 밖에 있다 —
    /// 모드도 UI 구성도 바뀌지 않는다. 코어가 누적 시간만
    /// 늘리고 FixedDt 는 그대로라 결과는 배속과 무관하다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class SpeedToggle : MonoBehaviour
    {
        const float Fast = 2f;

        [SerializeField] TMP_Text _label;

        GameSimDriver _driver;

        /// 배선 전에는 읽을 드라이버가 없다. 꺼 두면
        /// 그 프레임의 OnEnable 도 오지 않는다.
        void Awake() => enabled = false;

        public void Bind(GameSimDriver driver)
        {
            _driver = driver;
            GetComponent<Button>().onClick.AddListener(Toggle);
            enabled = true;
        }

        /// 판이 바뀌어도 배속은 유지된다. 다시 보일 때
        /// 라벨을 맞춰 두지 않으면 실제 값과 어긋난다.
        void OnEnable() => Show();

        void Toggle()
        {
            _driver.SpeedMultiplier = IsFast ? 1f : Fast;
            Show();
        }

        bool IsFast => _driver.SpeedMultiplier > 1f;

        void Show() => _label.text = IsFast ? "2x" : "1x";
    }
}
