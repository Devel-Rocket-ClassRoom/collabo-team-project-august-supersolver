using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 툴바가 지금 고른 도구를 내보인다. 표시는 탭이
    /// 저마다 하고 여기는 어느 탭인지만 정한다. 모드별
    /// 숨김은 패널을 통째로 끄는 StageFlow 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolbarView : MonoBehaviour
    {
        [SerializeField] ToolSelection _tools;

        /// 도구 하나에 탭 하나. 순서는 표시에 쓰이지 않는다.
        [SerializeField] ToolTab[] _tabs;

        // 패널이 껐다 켜질 때마다 이으면 리스너가 쌓인다.
        // Awake 는 살아 있는 동안 한 번만 돈다.
        void Awake()
        {
            foreach (ToolTab tab in _tabs)
                tab.Bind(_tools);
        }

        void OnEnable()
        {
            _tools.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            _tools.Changed -= Apply;
        }

        /// <summary>
        /// 판이 갈릴 때마다 잠금을 다시 계산한다. StageFlow 가
        /// 부른다 — OnEnable 은 그리기↔시뮬 전이마다 돌아
        /// 스테이지가 갈리는 시점과 맞지 않는다.
        /// </summary>
        public void ApplyUnlock()
        {
            int stage = CurrentStageIndex.CurrentGlobalIndex + 1;

            foreach (ToolTab tab in _tabs)
                tab.SetUnlocked(ToolUnlock.IsUnlocked(tab.Tool, stage));

            // 잠긴 도구가 선택된 채 남으면 탭은 잠겨 보이는데
            // 캔버스에서는 그대로 그려진다. 고정선은 1스테이지
            // 부터 열려 있어 언제 돌아가도 안전하다.
            if (!ToolUnlock.IsUnlocked(_tools.Current, stage))
                _tools.Select(DrawTool.FixedLine);
        }

        void Apply()
        {
            DrawTool current = _tools.Current;
            foreach (ToolTab tab in _tabs)
                tab.SetSelected(tab.Tool == current);
        }
    }
}
