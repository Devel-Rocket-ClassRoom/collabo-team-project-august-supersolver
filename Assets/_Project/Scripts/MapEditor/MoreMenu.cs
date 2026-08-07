using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    /// <summary>
    /// 더보기 메뉴 열고 닫기.
    /// 항목이 하는 일은 아직 없다.
    /// </summary>
    public sealed class MoreMenu : MonoBehaviour
    {
        [SerializeField] Button _toggleButton;
        [SerializeField] GameObject _panel;

        /// 바깥을 눌렀을 때를 받는 투명 판.
        [SerializeField] Button _blocker;

        [SerializeField] Button[] _items;

        void Awake()
        {
            _toggleButton.onClick.AddListener(Toggle);
            _blocker.onClick.AddListener(Close);

            for (int i = 0; i < _items.Length; i++)
                _items[i].onClick.AddListener(Close);

            SetOpen(false);
        }

        void Toggle() => SetOpen(!_panel.activeSelf);

        void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            _panel.SetActive(open);
            _blocker.gameObject.SetActive(open);
        }
    }
}
