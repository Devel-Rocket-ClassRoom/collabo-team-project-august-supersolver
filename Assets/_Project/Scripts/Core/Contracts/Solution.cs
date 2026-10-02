using System;
using System.Collections.Generic;

namespace PPS.Core
{
    /// <summary>
    /// 한 판의 그림 전체.
    /// 시간 축 없는 정적 스트로크 집합.
    /// 리스트 순서 = 월드 구축 순서.
    /// </summary>
    [Serializable]
    public class Solution
    {
        public List<Stroke> Strokes = new List<Stroke>();
        public List<PivotJoint> Pivots = new List<PivotJoint>();

        public static Solution Empty => new Solution();

        /// <summary>
        /// 획은 길이만큼, 핀은 개수만큼 쓴다. 핀은 길이가
        /// 없어 공짜로 두면 개수에 상한이 없다.
        /// </summary>
        public float TotalInk()
        {
            float sum = 0f;
            for (int i = 0; i < Strokes.Count; i++)
                sum += Strokes[i].Length();
            return sum + Pivots.Count * PivotJoint.InkCost;
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
