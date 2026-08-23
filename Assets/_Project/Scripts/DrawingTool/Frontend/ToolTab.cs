using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// ToolMenu 탭 하나. 도구 하나를 맡아 선택 여부를
    /// 배경색과 라벨로 내보인다. 색은 Layer Lab
    /// Tab_Nomal / Tab_Select 프리셋 그대로다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ToolTab : MonoBehaviour
    {
        static readonly Color NormalBg = new Color32(0x3B, 0x30, 0x4E, 0xFF);
        static readonly Color SelectedBg = new Color32(0x75, 0x50, 0xD2, 0xFF);
        static readonly Color NormalLight = new Color32(0x56, 0x50, 0x76, 0xFF);
        static readonly Color SelectedLight = new Color32(0x8F, 0x8F, 0xFF, 0xFF);
        static readonly Color NormalShadow = new Color32(0x33, 0x2B, 0x47, 0xFF);
        static readonly Color SelectedShadow = new Color32(0x59, 0x3D, 0xA1, 0xFF);

        [SerializeField] DrawTool _tool;

        [Header("색이 갈리는 배경 세 장")]
        [SerializeField] Graphic _bg;
        [SerializeField] Graphic _light;
        [SerializeField] Graphic _innerShadow;

        /// 선택 표시의 두 번째 채널. 배경 세 장은 회색조로
        /// 보면 1.9:1 밖에 안 갈려 색약 사용자에게는 이
        /// 라벨이 현재 도구를 아는 유일한 길이다(WCAG 1.4.1).
        [SerializeField] GameObject _label;

        /// 이 탭이 맡은 도구. ToolbarView 가 읽는다.
        public DrawTool Tool => _tool;

        /// <summary>
        /// 눌리면 제 도구를 고르게 잇는다. 인스펙터
        /// onClick 으로 도구를 또 고르면 _tool 과
        /// 어긋나도 아무도 모른다.
        /// </summary>
        public void Bind(ToolSelection tools) =>
            GetComponent<Button>().onClick.AddListener(() => tools.Select(_tool));

        public void SetSelected(bool selected)
        {
            _bg.color = selected ? SelectedBg : NormalBg;
            _light.color = selected ? SelectedLight : NormalLight;
            _innerShadow.color = selected ? SelectedShadow : NormalShadow;
            _label.SetActive(selected);
        }
    }
}
