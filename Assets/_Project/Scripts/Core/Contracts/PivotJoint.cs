using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 회전축 도구. 두 스트로크를 한 점에서 잇는다.
    /// 스트로크가 아니라 **연결 정보**이므로 잉크를 소모하지 않는다.
    ///
    /// 솔버에서는 단독 유전자가 아니라 복합 프리미티브(지렛대·바퀴) 내부에 캡슐화된다.
    /// 항상 유효한 구성만 생성되도록 하기 위함 (설계서 §5.2).
    /// </summary>
    [Serializable]
    public struct PivotJoint
    {
        /// 연결 대상 스트로크의 인덱스. -1 이면 월드(정적 앵커)에 고정한다.
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
