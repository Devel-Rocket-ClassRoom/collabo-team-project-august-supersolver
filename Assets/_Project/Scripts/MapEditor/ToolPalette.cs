using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    /// <summary>
    /// 어느 도구가 골라졌는지 알린다.
    /// 도구가 하는 일은 맵 편집 쪽에 있다.
    /// </summary>
    public sealed class ToolPalette : MonoBehaviour
    {
        [SerializeField] Button[] _tabs;

        /// 탭과 순서를 맞춘다.
        [SerializeField] GameObject[] _pages;

        [SerializeField] ScrollRect _itemScroll;

        [SerializeField] Color _selectedColor = new Color32(0xFF, 0xD8, 0x66, 0xFF);
        [SerializeField] Color _normalColor = Color.white;

        /// 페이지별 항목 버튼. 계층에서 그대로 읽는다.
        Button[][] _items;

        /// 지금 고른 탭. 맵 편집이 무엇을 놓을지 판단한다.
        public int SelectedTab { get; private set; } = -1;

        /// 그 탭 안에서 고른 항목. 없으면 -1.
        public int SelectedItem { get; private set; } = -1;

        void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                _tabs[i].onClick.AddListener(() => SelectTab(tab));
            }

            CacheItems();

            if (_tabs.Length > 0) SelectTab(0);
        }

        /// <summary>
        /// 페이지가 꺼져 있어도 자식을 읽어야 해서
        /// 포함 인자를 켠다.
        /// </summary>
        void CacheItems()
        {
            _items = new Button[_pages.Length][];

            for (int page = 0; page < _pages.Length; page++)
            {
                _items[page] = _pages[page].GetComponentsInChildren<Button>(true);

                for (int i = 0; i < _items[page].Length; i++)
                {
                    int item = i;
                    _items[page][i].onClick.AddListener(() => SelectItem(item));
                }
            }
        }

        void SelectTab(int index)
        {
            SelectedTab = index;

            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].targetGraphic.color = i == index ? _selectedColor : _normalColor;

            for (int i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == index);

            // 카테고리를 바꾸면 앞에서부터 본다.
            _itemScroll.horizontalNormalizedPosition = 0f;

            SelectItem(_items[index].Length > 0 ? 0 : -1);
        }

        void SelectItem(int index)
        {
            SelectedItem = index;

            var page = _items[SelectedTab];
            for (int i = 0; i < page.Length; i++)
                page[i].targetGraphic.color = i == index ? _selectedColor : _normalColor;

            if (index >= 0)
                Debug.Log($"[맵 에디터] 도구: {_tabs[SelectedTab].name} / {page[index].name}");
        }
    }
}
