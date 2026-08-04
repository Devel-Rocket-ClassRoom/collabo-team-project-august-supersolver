using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 회전축. 두 스트로크를 한 점에서 잇는다.
    /// 연결 정보라 잉크를 쓰지 않는다.
    /// </summary>
    [Serializable]
    public struct PivotJoint
    {
        /// 스트로크 인덱스. -1 이면 월드 고정.
        public int StrokeA;

        public int StrokeB;

        /// 회전 중심. 월드 좌표.
        public Vector2 Anchor;

        public PivotJoint(int strokeA, int strokeB, Vector2 anchor)
        {
            StrokeA = strokeA;
            StrokeB = strokeB;
            Anchor = anchor;
        }

        /// 월드 고정 축 (바퀴의 중심축 등).
        public const int WorldIndex = -1;
    }
}
