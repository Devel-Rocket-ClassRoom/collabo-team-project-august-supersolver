using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 한 번의 그리기 결과. 단순화가 끝난
    /// 최종 포인트를 담는다. 물리·렌더링·
    /// 리플레이가 이 하나를 공유한다.
    /// </summary>
    [Serializable]
    public struct Stroke
    {
        public ToolType Tool;

        /// 월드 좌표. 최소 2개.
        public List<Vector2> Points;

        public Stroke(ToolType tool, List<Vector2> points)
        {
            Tool = tool;
            Points = points;
        }

        /// 잉크 소모량 = 폴리라인 길이의 합.
        public float Length()
        {
            if (Points == null || Points.Count < 2) return 0f;

            float sum = 0f;
            for (int i = 1; i < Points.Count; i++)
                sum += Vector2.Distance(Points[i - 1], Points[i]);
            return sum;
        }

        public bool IsValid => Points != null && Points.Count >= 2;
    }
}
