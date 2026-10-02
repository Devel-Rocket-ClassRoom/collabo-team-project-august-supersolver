using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 회전축. 두 스트로크를 한 점에서 잇는다.
    /// 길이가 없지만 잉크는 쓴다 — InkCost 를 본다.
    /// </summary>
    [Serializable]
    public struct PivotJoint
    {
        /// <summary>
        /// 핀 하나가 쓰는 잉크. 길이에서 나오는 값이 아니라
        /// 개수를 묶으려고 매긴 값이다 — 공짜면 월드 고정이
        /// 빈 곳에도 항상 찍혀 개수에 상한이 없어진다.
        /// 최소 획(0.25wu)의 두 배다. 기획 근거가 없는 우리
        /// 값이라 흔한 2~3개 풀이가 예산의 2~7% 만 쓰게 잡았다.
        /// </summary>
        public const float InkCost = 0.5f;

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
        /// </summary>
        public const int Unbound = -2;

        /// <summary>
        /// 이을 것이 양쪽 다 정해졌는가. 한 칸이라도 비어 있으면
        /// 물리에 조인트를 만들지 않는다 — 상대를 기다리는 핀이
        /// 자유물체를 붙잡으면 회전축이 없는데도 안 떨어진다.
        /// </summary>
        public bool IsComplete => StrokeA != Unbound && StrokeB != Unbound;
    }
}
