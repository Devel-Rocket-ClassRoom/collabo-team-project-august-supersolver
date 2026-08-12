using System.Collections.Generic;
using PPS.Core;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 전체 스냅샷(Memento) 되돌리기. 액션마다 직전 그림을
    /// 통째로 쌓는다 — 역연산이 없어 핀 인덱스 무결성
    /// 문제가 생기지 않는다. 순수 C# 이라 씬 없이 잰다.
    /// </summary>
    public sealed class SolutionHistory
    {
        readonly List<Solution> _undo = new List<Solution>();
        readonly List<Solution> _redo = new List<Solution>();

        /// 되돌리기가 인스턴스를 통째로 갈아치운다.
        /// 바깥이 참조를 들고 있으면 안 되는 이유다.
        Solution _current = new Solution();

        public Solution Current => _current;

        public bool CanUndo => _undo.Count > 0;

        public bool CanRedo => _redo.Count > 0;

        public void AddStroke(Stroke stroke)
        {
            Commit();
            _current.Strokes.Add(stroke);
        }

        /// <summary>핀 생성도 액션 하나다.</summary>
        public void AddPivot(PivotJoint pivot)
        {
            Commit();
            _current.Pivots.Add(pivot);
        }

        public void Clear()
        {
            Commit();
            _current.Strokes.Clear();
            _current.Pivots.Clear();
        }

        public bool Undo()
        {
            if (!CanUndo) return false;

            _redo.Add(_current.Clone());
            _current = Pop(_undo);
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            _undo.Add(_current.Clone());
            _current = Pop(_redo);
            return true;
        }

        /// <summary>
        /// 액션 직전 상태를 사본으로 쌓는다. 참조를 쌓으면
        /// 이어지는 변경이 스냅샷까지 뚫는다.
        /// </summary>
        void Commit()
        {
            _undo.Add(_current.Clone());
            _redo.Clear();
        }

        static Solution Pop(List<Solution> stack)
        {
            int last = stack.Count - 1;
            Solution top = stack[last];
            stack.RemoveAt(last);
            return top;
        }
    }
}
