using System;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 그림 하나의 주인. 획·핀·초기화·되돌리기가 전부
    /// 여기를 거친다 — 다른 곳이 Solution 을 직접 고치면
    /// 되돌리기 스택이 실제 화면과 어긋난다.
    /// 툴바 버튼 onClick 이 여기로 들어온다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawingSession : MonoBehaviour
    {
        readonly SolutionHistory _history = new SolutionHistory();

        /// 지금 그림. 되돌리기가 인스턴스를 갈아치우므로
        /// 참조를 캐시하지 말고 그때그때 읽는다.
        public Solution Solution => _history.Current;

        /// 그림이 바뀔 때마다. 렌더러가 듣고 재구축한다.
        public event Action Changed;

        /// <summary>
        /// 획을 더하고, 그 획이 붙을 곳 없던 핀을 되살린다.
        /// 재결합은 같은 액션 안이라 되돌리기 한 번에 획과
        /// 핀이 함께 이전 상태로 간다.
        /// </summary>
        public void AddStroke(Stroke stroke)
        {
            _history.AddStroke(stroke);
            SolutionEditing.RebindPivots(Solution, Solution.Strokes.Count - 1);
            Changed?.Invoke();
        }

        public void AddPivot(PivotJoint pivot)
        {
            _history.AddPivot(pivot);
            Changed?.Invoke();
        }

        /// <summary>
        /// 탭 한 점을 핀으로 푼다. 어느 획에 걸리는지는
        /// Solution 을 아는 여기서 정한다. 도구는 인식기가
        /// Down 에서 잡아둔 값이라 도중에 바꿔도 안 흔들린다.
        /// </summary>
        public void PlacePivot(DrawTool tool, Vector2 anchor, float radius)
        {
            if (SolutionEditing.TryCreatePivot(
                    Solution, anchor, radius,
                    tool == DrawTool.PivotWorld, out PivotJoint pivot))
                AddPivot(pivot);
        }

        /// <summary>
        /// 핀을 획보다 먼저 본다 — 핀은 획 위에 놓이므로
        /// 획을 먼저 보면 핀을 영영 못 지운다. 반경 안에
        /// 아무것도 없으면 아무 일도 일어나지 않는다.
        /// </summary>
        public void EraseAt(Vector2 anchor, float radius)
        {
            int pivot = SolutionEditing.FindPivotNear(Solution.Pivots, anchor, radius);
            if (pivot >= 0)
            {
                ErasePivot(pivot);
                return;
            }

            int stroke = SolutionEditing.FindStrokeNear(
                Solution.Strokes, anchor, radius, Solution.Strokes.Count - 1);
            if (stroke >= 0) EraseStroke(stroke);
        }

        /// <summary>
        /// 획 하나를 빼고 남은 핀을 맞춘다. 재매핑이 같은
        /// 액션 안이라 되돌리기 한 번에 획과 핀이 함께
        /// 이전 상태로 간다. 잉크는 잔량 재계산이라
        /// 환급 로직이 필요 없다.
        /// </summary>
        public void EraseStroke(int index)
        {
            _history.EraseStroke(index);
            SolutionEditing.RemapPivots(Solution, index);
            Changed?.Invoke();
        }

        /// <summary>
        /// 핀 하나를 뺀다. 아무것도 핀을 인덱스로 참조하지
        /// 않아 재매핑할 것이 없다.
        /// </summary>
        public void ErasePivot(int index)
        {
            _history.ErasePivot(index);
            Changed?.Invoke();
        }

        /// <summary>
        /// 확인 다이얼로그를 두지 않는다. 되돌릴 수 있어
        /// 안전하고, 시행착오 루프에 탭이 하나 늘어난다.
        /// </summary>
        public void OnClickClear()
        {
            _history.Clear();
            Changed?.Invoke();
        }

        public void OnClickUndo()
        {
            if (_history.Undo()) Changed?.Invoke();
        }

        public void OnClickRedo()
        {
            if (_history.Redo()) Changed?.Invoke();
        }
    }
}
