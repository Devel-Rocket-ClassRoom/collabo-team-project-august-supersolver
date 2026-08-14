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

        /// <summary>
        /// 획 하나를 뺀다. 남은 핀의 인덱스는 안 맞춘다 —
        /// 재매핑은 세션이 같은 액션 안에서 잇는다.
        /// </summary>
        public void EraseStroke(int index)
        {
            Commit();
            _current.Strokes.RemoveAt(index);
        }

        public void ErasePivot(int index)
        {
            Commit();
            _current.Pivots.RemoveAt(index);
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
        /// 되돌리기 깊이 상한. 액션마다 그림을 통째로 복사하는데
        /// 초기화와 핀 탭은 잉크를 안 써서 무한히 반복된다 —
        /// 안 막으면 스택이 그만큼 자란다.
        /// </summary>
        public const int MaxDepth = 100;

        /// <summary>
        /// 액션 직전 상태를 사본으로 쌓는다. 참조를 쌓으면
        /// 이어지는 변경이 스냅샷까지 뚫는다.
        /// </summary>
        void Commit()
        {
            _undo.Add(_current.Clone());

            // 넘치면 가장 오래된 것을 버린다. 최근 쪽을 버리면
            // 방금 한 일을 못 되돌린다.
            if (_undo.Count > MaxDepth) _undo.RemoveAt(0);

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
