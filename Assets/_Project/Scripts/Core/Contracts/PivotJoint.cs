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

        /// <summary>
        /// 붙을 획을 아직 못 정했다. 핀을 먼저 놓고 나중에
        /// 그은 획을 받는 순서를 위해 있다.
        /// **WorldBuilder 는 아직 이 값을 안 본다** — Resolve 가
        /// 음수를 전부 null 로 보므로 (자유물체, Unbound) 는
        /// 월드 고정 조인트가 되어 버린다. 미결합 핀은 물리에
        /// 없어야 한다.
        /// </summary>
        public const int Unbound = -2;
    }
}
