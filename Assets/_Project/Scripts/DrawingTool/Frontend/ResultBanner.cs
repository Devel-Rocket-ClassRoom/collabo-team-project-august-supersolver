using PPS.Core;
using PPS.Game;
using TMPro;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 판정이 확정되면 배너를 띄운다. 상태 전이는 하지
    /// 않는다 — 재시도는 수동이다. 월드에서 파생되므로
    /// 전이 쪽에서 꺼 줄 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultBanner : MonoBehaviour
    {
        [SerializeField] GameObject _banner;
        [SerializeField] TMP_Text _label;

        GameSimDriver _driver;

        bool _shown;

        /// 첫 프레임의 상태는 파생이 아니라 강제한다.
        /// 씬에 켜진 채 저장돼 있으면 폴링은 못 고친다.
        /// 배선 전에 도는 프레임도 여기서 막는다.
        void Awake()
        {
            _banner.SetActive(false);
            enabled = false;
        }

        public void Bind(GameSimDriver driver)
        {
            _driver = driver;
            enabled = true;
        }

        // 드라이버가 Update 에서 스텝을 돌린다. 여기가
        // Update 면 실행 순서에 따라 한 프레임 늦는다.
        void LateUpdate()
        {
            SimWorld world = _driver.World;
            bool decided = world != null && world.IsTerminal;

            // 판정은 한 번 확정되면 안 뒤집힌다. 상태가
            // 바뀔 때만 손대 매 프레임 문자열을 만들지 않는다.
            if (decided == _shown) return;

            _shown = decided;
            _banner.SetActive(decided);

            if (decided) _label.text = TextOf(world.Judge);
        }

        /// <summary>
        /// 클리어 문구가 별을 함께 알린다 — 보상 화면이
        /// 붙기 전까지 이게 결과 표시의 전부다.
        /// </summary>
        static string TextOf(Judge judge)
        {
            if (judge.Cleared) return $"Clear!   stars {judge.Stars}";
            if (judge.Failed) return "Failed";

            return "Stopped";
        }
    }
}
