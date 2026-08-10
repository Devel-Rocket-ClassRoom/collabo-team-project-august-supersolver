using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 지금 고른 그리기 도구. 툴바 버튼 onClick 이
    /// 여기로 들어온다. 선택 표시와 상태 전이는
    /// 상태 머신 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolSelection : MonoBehaviour
    {
        public ToolType Current { get; private set; } = ToolType.FixedLine;

        public void OnClickSelectFixedLine() => Current = ToolType.FixedLine;

        public void OnClickSelectFreeBody() => Current = ToolType.FreeBody;
    }
}
