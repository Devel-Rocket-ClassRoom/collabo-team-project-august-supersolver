using System;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 솔버가 탐색하는 선 하나.
    /// 점 목록인 Stroke 와 달리 모양이 결정된 파라미터다. 
    /// </summary>
    [Serializable]
    public struct Primitive
    {
        public PrimitiveShape Shape;

        /// 정적/동적
        public ToolType Body;

        /// 외접원 중심. 월드 좌표.
        public Vector2 Center;

        /// 회전각(라디안). 주기는 Shape 가 정한다.
        public float Angle;

        /// 외접원 반지름
        public float Size;

        /// 회전축이 걸리는 지점. (none / 0 / 0.5)
        public PivotSpot Pivot;

        /// 축이 걸릴 상대 프리미티브 인덱스.
        /// -1 이면 월드 고정. 
        public int PivotLink;

        public const int WorldLink = PivotJoint.WorldIndex;

        /// <summary>
        /// default(Primitive) 는 PivotLink 가 0 이라
        /// 0번을 가리킨다. 항상 이 생성자로 만든다.
        /// </summary>
        public Primitive(PrimitiveShape shape, ToolType body, Vector2 center, float angle, float size,
            PivotSpot pivot = PivotSpot.None, int pivotLink = WorldLink)
        {
            Shape = shape;
            Body = body;
            Center = center;
            Angle = angle;
            Size = size;
            Pivot = pivot;
            PivotLink = pivotLink;
        }

        public bool HasPivot => Pivot != PivotSpot.None;

        /// 축 지점의 정규화 위치.
        public float PivotT => Pivot == PivotSpot.Middle ? 0.5f : 0f;

        /// 각도가 겹치기 시작하는 지점.
        public float AnglePeriod => Shape.AnglePeriod();
    }
}
