using System;
using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// "한 판의 그림 전체". **시간 축이 없는 정적 스트로크 집합**이다 (설계서 결정 1·2).
    /// 그리기는 시뮬 시작 전에만 가능하므로 솔루션에 "언제"라는 차원이 존재하지 않는다.
    ///
    /// 리스트의 순서가 곧 월드 구축 순서이며, 이것이 결정론의 전제다.
    /// 순서를 바꾸면 Box2D 바디 등록 순서가 바뀌어 결과가 달라진다.
    /// </summary>
    [Serializable]
    public class Solution
    {
        public List<Stroke> Strokes = new List<Stroke>();
        public List<PivotJoint> Pivots = new List<PivotJoint>();

        public static Solution Empty => new Solution();

        public float TotalInk()
        {
            float sum = 0f;
            for (int i = 0; i < Strokes.Count; i++)
                sum += Strokes[i].Length();
            return sum;
        }

        public Solution Clone()
        {
            var copy = new Solution();
            for (int i = 0; i < Strokes.Count; i++)
            {
                var s = Strokes[i];
                copy.Strokes.Add(new Stroke(s.Tool, s.Points == null
                    ? null
                    : new List<UnityEngine.Vector2>(s.Points)));
            }
            copy.Pivots.AddRange(Pivots);
            return copy;
        }
    }
}
