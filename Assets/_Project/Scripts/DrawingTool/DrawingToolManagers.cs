using PPS.Core;
using PPS.Game;
using UnityEngine;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("_input")]
        [SerializeField] PointerReader _pointer;

        [SerializeField] InkBudget _ink;
        [SerializeField] GameSimDriver _driver;
        [SerializeField] StageFlow _flow;
        [SerializeField] LevelView _levelView;

        public ToolSelection Tools => _tools;

        public DrawingSession Session => _session;

        public PointerReader Pointer => _pointer;

        public GameSimDriver Driver => _driver;

        public StageFlow Flow => _flow;

        /// <summary>
        /// 고른 판을 필요한 곳에 물린다. 판을 아는 곳이
        /// 넷이라 배포 지점도 참조를 든 여기 하나로 둔다.
        /// </summary>
        public void SetStage(StageData stage)
        {
            _flow.SetStage(stage);
            _ink.SetLevel(stage.Level);
            _levelView.SetLevel(stage.Level);
            CanvasCameraFitter.Instance.SetLevel(stage.Level);
        }
    }
}
