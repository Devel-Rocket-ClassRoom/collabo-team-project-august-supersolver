using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    /// <summary>
    /// 도구 카테고리를 고르면 그 항목만 띄운다.
    /// 항목이 하는 일은 아직 없다.
    /// </summary>
    public sealed class ToolPalette : MonoBehaviour
    {
        [SerializeField] Button[] _tabs;

        /// 탭과 순서를 맞춘다.
        [SerializeField] GameObject[] _pages;

        [SerializeField] ScrollRect _itemScroll;

        [SerializeField] Color _selectedColor = new Color32(0xFF, 0xD8, 0x66, 0xFF);
        [SerializeField] Color _normalColor = Color.white;

        void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i].onClick.AddListener(() => Select(index));
            }

            if (_tabs.Length > 0) Select(0);
        }

        void Select(int index)
        {
            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].targetGraphic.color = i == index ? _selectedColor : _normalColor;

            for (int i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == index);

            // 카테고리를 바꾸면 앞에서부터 본다.
            _itemScroll.horizontalNormalizedPosition = 0f;

            Debug.Log($"[맵 에디터] 도구 선택: {_tabs[index].name}");
        }
    }
}
