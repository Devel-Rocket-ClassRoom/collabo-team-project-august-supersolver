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

        /// 선택이 바뀔 때마다. ToolbarView 가 듣는다.
        public event Action Changed;

        public void OnClickSelectFixedLine() => Select(DrawTool.FixedLine);

        public void OnClickSelectFreeBody() => Select(DrawTool.FreeBody);

        public void OnClickSelectPivotSingle() => Select(DrawTool.PivotSingle);

        public void OnClickSelectPivotWorld() => Select(DrawTool.PivotWorld);

        public void OnClickSelectErase() => Select(DrawTool.Erase);

        void Select(DrawTool tool)
        {
            Current = tool;
            Changed?.Invoke();
        }
    }
}
