using DG.Tweening;
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

        const float RiseDuration = 0.12f;

        [SerializeField] DrawTool _tool;

        [Header("색이 갈리는 배경 세 장")]
        [SerializeField] Graphic _bg;
        [SerializeField] Graphic _light;
        [SerializeField] Graphic _innerShadow;

        /// 선택 표시의 두 번째 채널. 배경 세 장은 회색조로
        /// 보면 1.9:1 밖에 안 갈려 색약 사용자에게는 이
        /// 라벨이 현재 도구를 아는 유일한 길이다(WCAG 1.4.1).
        [SerializeField] GameObject _label;

        /// 선택 표시의 세 번째 채널. 탭 내용을 통째로 담은
        /// 자식이라 이걸 키워야 배경까지 따라 자란다. 루트는
        /// ToolMenu 레이아웃이 리빌드마다 덮어써서 못 쓴다.
        [SerializeField] RectTransform _visual;

        /// 선택된 탭이 위로 자라는 높이. ToolMenu 위로
        /// 156 이 비어 있어 넉넉하다.
        [SerializeField] float _riseY = 14f;

        /// 아직 한 번도 표시하지 않았으면 null. 첫 표시는
        /// 연출 없이 자리만 잡는다.
        bool? _shown;

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
            Rise(selected);
        }

        // 패널을 다시 열 때마다 처음부터 다시 표시한다.
        void OnDisable() => _shown = null;

        /// <summary>
        /// ToolbarView 는 안 바뀐 탭에도 SetSelected 를 부른다.
        /// 그대로 태우면 패널을 열 때마다 이미 선택된 탭이
        /// 다시 올라와 방금 고른 것처럼 보인다.
        /// </summary>
        void Rise(bool selected)
        {
            if (_shown == selected)
                return;

            bool instant = _shown == null;
            _shown = selected;

            float y = selected ? _riseY : 0f;
            _visual.DOKill();

            if (instant)
            {
                Grow(y);
                return;
            }

            DOTween.To(() => _visual.sizeDelta.y, Grow, y, RiseDuration)
                .SetTarget(_visual)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 위로 자란 높이를 반영한다. 밑변은 화면 맨 아래에
        /// 붙어 있어 올려 버리면 그 자리가 빈다. 스트레치에
        /// 피벗이 가운데라 늘어난 절반만큼 되밀어야 제자리다.
        /// </summary>
        void Grow(float grow)
        {
            _visual.sizeDelta = new Vector2(0f, grow);
            _visual.anchoredPosition = new Vector2(0f, grow * 0.5f);
        }
    }
}
