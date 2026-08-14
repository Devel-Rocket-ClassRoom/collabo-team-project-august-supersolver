using System;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 지금 고른 도구. 툴바 버튼 onClick 이 여기로
    /// 들어온다. 화면 표시는 ToolbarView 가 하고
    /// 상태 전이는 상태 머신 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolSelection : MonoBehaviour
    {
        public DrawTool Current { get; private set; } = DrawTool.FixedLine;

        /// 회전축 슬롯이 지금 내보이는 모드. 다른 도구로
        /// 갔다 와도 마지막에 고른 쪽이 남는다 — 아니면
        /// 슬롯 아이콘이 제멋대로 되돌아간다.
        public DrawTool PivotMode { get; private set; } = DrawTool.PivotSingle;

        /// 선택이 바뀔 때마다. ToolbarView 가 듣는다.
        public event Action Changed;

        public void OnClickSelectFixedLine() => Select(DrawTool.FixedLine);

        public void OnClickSelectFreeBody() => Select(DrawTool.FreeBody);

        public void OnClickSelectPivotSingle() => SelectPivot(DrawTool.PivotSingle);

        public void OnClickSelectPivotWorld() => SelectPivot(DrawTool.PivotWorld);

        public void OnClickSelectErase() => Select(DrawTool.Erase);

        void Select(DrawTool tool)
        {
            Current = tool;
            Changed?.Invoke();
        }

        /// <summary>
        /// 슬롯 하나가 두 모드를 쥔다. 고르지 않은 상태면
        /// 내보이는 모드를 고르고, 이미 고른 모드를 또
        /// 누르면 다른 모드로 넘어간다.
        /// </summary>
        void SelectPivot(DrawTool mode)
        {
            PivotMode = Current == mode ? Other(mode) : mode;
            Select(PivotMode);
        }

        static DrawTool Other(DrawTool mode) =>
            mode == DrawTool.PivotSingle ? DrawTool.PivotWorld : DrawTool.PivotSingle;
    }
}
