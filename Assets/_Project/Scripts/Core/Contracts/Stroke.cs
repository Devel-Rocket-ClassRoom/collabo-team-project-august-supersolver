using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 한 번의 그리기 결과. **단순화가 끝난 최종 포인트**를 담으며
    /// 물리·렌더링·리플레이 저장이 모두 이 하나를 공유한다 (설계서 결정 6·7).
    ///
    /// 유저 경로에서는 손가락 궤적을 <see cref="IStrokeProcessor"/> 가 단순화한 결과이고,
    /// 솔버 경로에서는 프리미티브 Generate() 가 전개한 결과다.
    /// 시뮬 호스트는 두 출처를 구분하지 않는다 — 이것이 "솔버 통과 = 유저도 클리어 가능"의 근거.
    /// </summary>
    [Serializable]
    public struct Stroke
    {
        public ToolType Tool;

        /// 단순화 후 포인트. 월드 좌표. 최소 2개.
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
