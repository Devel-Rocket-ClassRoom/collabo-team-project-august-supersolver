using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 툴바가 지금 고른 도구를 내보인다. 표시는 두 채널을
    /// 겹친다 — 선택 배경의 유무와 아이콘 색. 모드별 숨김은
    /// 패널을 통째로 끄는 StageModeView 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolbarView : MonoBehaviour
    {
        /// 선택 표시의 두 번째 채널. 배경 유무 하나로는
        /// 색약 사용자에게 현재 도구가 안 보인다(WCAG 1.4.1).
        /// 두 상태의 명도비가 약 14:1 이라 회색조로도 갈린다 —
        /// 선택 배경이 어떤 색으로 바뀌어도 어두운 아이콘은
        /// 그 위에서 읽힌다.
        static readonly Color SelectedIcon = new Color32(0x1B, 0x1E, 0x24, 0xFF);
        static readonly Color NormalIcon = new Color32(0xE8, 0xEA, 0xEE, 0xFF);

        ToolSelection _tools;

        [Header("도구 버튼")]
        [SerializeField] Button _fixedLine;
        [SerializeField] Button _freeBody;
        [SerializeField] Button _erase;

        [Header("선택 표시")]
        [SerializeField] GameObject _fixedLineSelected;
        [SerializeField] GameObject _freeBodySelected;
        [SerializeField] GameObject _justPivotSelected;
        [SerializeField] GameObject _worldPivotSelected;
        [SerializeField] GameObject _eraseSelected;

        [Header("아이콘 색 채널")]
        [SerializeField] Graphic _fixedLineIcon;
        [SerializeField] Graphic _freeBodyIcon;
        [SerializeField] Graphic _justPivotIcon;
        [SerializeField] Graphic _worldPivotIcon;
        [SerializeField] Graphic _eraseIcon;

        [Header("회전축 슬롯 2모드")]
        [SerializeField] Button _justPivot;
        [SerializeField] Button _worldPivot;

        /// <summary>
        /// 구독만으로는 첫 화면이 비어 있다. 지금 도구를
        /// 한 번 비추고 나서 변화를 따라간다.
        /// </summary>
        public void Bind(ToolSelection tools)
        {
            _tools = tools;

            _fixedLine.onClick.AddListener(tools.OnClickSelectFixedLine);
            _freeBody.onClick.AddListener(tools.OnClickSelectFreeBody);
            _erase.onClick.AddListener(tools.OnClickSelectErase);
            _justPivot.onClick.AddListener(tools.OnClickSelectPivotSingle);
            _worldPivot.onClick.AddListener(tools.OnClickSelectPivotWorld);

            tools.Changed += Apply;
            Apply();
        }

        void Apply()
        {
            // 두 핀 버튼은 같은 rect 에 겹쳐 있다. 한쪽을
            // 끄지 않으면 위엣것이 클릭을 전부 먹는다.
            bool single = _tools.PivotMode == DrawTool.PivotSingle;
            _justPivot.gameObject.SetActive(single);
            _worldPivot.gameObject.SetActive(!single);

            DrawTool current = _tools.Current;
            _fixedLineSelected.SetActive(current == DrawTool.FixedLine);
            _freeBodySelected.SetActive(current == DrawTool.FreeBody);
            _justPivotSelected.SetActive(current == DrawTool.PivotSingle);
            _worldPivotSelected.SetActive(current == DrawTool.PivotWorld);
            _eraseSelected.SetActive(current == DrawTool.Erase);

            // 배경 유무는 색이 아닌 채널이다. 색약이어도 그쪽으로
            // 알 수 있고, 배경을 놓쳐도 아이콘 색으로 안다.
            Tint(_fixedLineIcon, current == DrawTool.FixedLine);
            Tint(_freeBodyIcon, current == DrawTool.FreeBody);
            Tint(_justPivotIcon, current == DrawTool.PivotSingle);
            Tint(_worldPivotIcon, current == DrawTool.PivotWorld);
            Tint(_eraseIcon, current == DrawTool.Erase);
        }

        static void Tint(Graphic icon, bool selected) =>
            icon.color = selected ? SelectedIcon : NormalIcon;
    }
}
