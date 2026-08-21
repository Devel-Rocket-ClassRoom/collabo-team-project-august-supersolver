using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// ToolMenu 탭 하나. 도구 하나를 맡아 선택 여부를
    /// 자기 그래픽 색으로 내보인다. 색값은 Layer Lab
    /// Tab_Nomal / Tab_Select 프리셋 그대로다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolTab : MonoBehaviour
    {
        static readonly Color NormalBg = new Color32(0x3B, 0x30, 0x4E, 0xFF);
        static readonly Color SelectedBg = new Color32(0x75, 0x50, 0xD2, 0xFF);
        static readonly Color NormalLight = new Color32(0x56, 0x50, 0x76, 0xFF);
        static readonly Color SelectedLight = new Color32(0x8F, 0x8F, 0xFF, 0xFF);
        static readonly Color NormalShadow = new Color32(0x33, 0x2B, 0x47, 0xFF);
        static readonly Color SelectedShadow = new Color32(0x59, 0x3D, 0xA1, 0xFF);

        /// 선택 표시의 두 번째 채널. 탭 배경 세 장은 회색조로
        /// 보면 1.9:1 밖에 안 갈려 색약 사용자에게 현재 도구가
        /// 안 보인다(WCAG 1.4.1). 아이콘 두 상태는 명도비가
        /// 14:1 이라 그쪽으로 읽힌다.
        static readonly Color NormalIcon = new Color32(0xE8, 0xEA, 0xEE, 0xFF);
        static readonly Color SelectedIcon = new Color32(0x1B, 0x1E, 0x24, 0xFF);

        [SerializeField] DrawTool _tool;

        [Header("Layer Lab 탭 그래픽")]
        [SerializeField] Graphic _bg;
        [SerializeField] Graphic _light;
        [SerializeField] Graphic _innerShadow;
        [SerializeField] Graphic _icon;

        /// 이 탭이 맡은 도구. ToolbarView 가 읽는다.
        public DrawTool Tool => _tool;

        public void SetSelected(bool selected)
        {
            _bg.color = selected ? SelectedBg : NormalBg;
            _light.color = selected ? SelectedLight : NormalLight;
            _innerShadow.color = selected ? SelectedShadow : NormalShadow;
            _icon.color = selected ? SelectedIcon : NormalIcon;
        }
    }
}
