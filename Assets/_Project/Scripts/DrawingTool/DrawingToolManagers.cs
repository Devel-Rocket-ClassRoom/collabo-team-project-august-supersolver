using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 로직 쪽 유일한 창구. UI 프리팹은 런타임에 붙어
    /// 씬 오브젝트를 직렬화로 참조할 수 없다 — 배선이
    /// 여기 한 곳으로 모여야 경계가 하나로 유지된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawingToolManagers : MonoSingleton<DrawingToolManagers>
    {
        [SerializeField] ToolSelection _tools;
        [SerializeField] DrawingSession _session;
        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] GameSimDriver _driver;
        [SerializeField] StageFlow _flow;

        public ToolSelection Tools => _tools;

        public DrawingSession Session => _session;

        public DrawInputBehaviour Input => _input;

        public GameSimDriver Driver => _driver;

        public StageFlow Flow => _flow;
    }
}
