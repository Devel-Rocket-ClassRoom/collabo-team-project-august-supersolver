using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 도구가 열리는 자리를 담은 표. 해금 여부만 답하고
    /// 표시는 ToolTab, 적용 시점은 ToolbarView 몫이다.
    /// </summary>
    [CreateAssetMenu(fileName = "ToolUnlockTable", menuName = "Scriptable Objects/ToolUnlockTable")]
    public class ToolUnlockTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public DrawTool Tool;
            public StageEntry At;
        }

        /// 도구를 여는 자리. 지우개가 (0,0) 인 것은 첫
        /// 스테이지 튜토리얼이 지우개 탭을 누르게 시키기
        /// 때문이다 — 잠그면 그 컷에서 멈췄다. 연결핀은
        /// 기획 확정값이 아니라 확인이 필요하다.
        public Entry[] Entries;

        /// <summary>못 찾으면 (0,0) 을 돌려준다.</summary>
        public StageEntry EntryOf(DrawTool tool)
        {
            if (Entries != null)
                foreach (var entry in Entries)
                    if (entry.Tool == tool) return entry.At;

            Debug.LogWarning($"ToolUnlockTable: 도구 {tool} 가 표에 없다");
            return default;
        }

        public bool IsUnlocked(DrawTool tool, StageEntry at) => at >= EntryOf(tool);
    }
}
