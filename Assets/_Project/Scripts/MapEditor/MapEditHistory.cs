using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 상태를 통째로 저장해 되돌린다.
    /// 편집 종류마다 되돌리는 방법을 따로 짜면
    /// 종류가 늘 때마다 빠뜨리는 것이 생긴다.
    /// </summary>
    public sealed class MapEditHistory : MonoBehaviour
    {
        /// 되돌릴 수 있는 횟수. 한 판이 몇 KB 라 넉넉히 잡는다.
        const int MaxDepth = 50;

        [SerializeField] MapEditSession _session;

        readonly List<Snapshot> _undo = new List<Snapshot>();
        readonly List<Snapshot> _redo = new List<Snapshot>();

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        /// <summary>
        /// 무언가를 바꾸기 직전에 부른다.
        /// 바꾼 뒤에 부르면 되돌릴 상태를 이미 잃은 뒤다.
        /// </summary>
        public void BeginEdit()
        {
            if (_session == null) return;

            Push(_undo, Capture());

            // 되돌린 뒤 새로 편집하면 앞선 갈래는 버린다.
            _redo.Clear();
        }

        /// <summary>상단바 실행 취소 버튼이 부른다.</summary>
        public void Undo() => Move(_undo, _redo);

        /// <summary>상단바 다시 실행 버튼이 부른다.</summary>
        public void Redo() => Move(_redo, _undo);

        void Move(List<Snapshot> from, List<Snapshot> to)
        {
            if (_session == null || from.Count == 0) return;

            Push(to, Capture());

            Snapshot snapshot = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);

            _session.Replace(
                StageData.FromJson(snapshot.Stage),
                MapShapes.FromJson(snapshot.Shapes));
        }

        /// <summary>
        /// 도형이 원본이라 판만 되돌리면 다음 굽기에
        /// 되살아난다. 둘을 함께 뜬다.
        /// </summary>
        Snapshot Capture() => new Snapshot(
            _session.Current.ToJson(false),
            _session.Shapes.ToJson(false));

        static void Push(List<Snapshot> stack, Snapshot snapshot)
        {
            stack.Add(snapshot);

            // 오래된 것부터 버린다.
            if (stack.Count > MaxDepth) stack.RemoveAt(0);
        }

        readonly struct Snapshot
        {
            public readonly string Stage;
            public readonly string Shapes;

            public Snapshot(string stage, string shapes)
            {
                Stage = stage;
                Shapes = shapes;
            }
        }
    }
}
