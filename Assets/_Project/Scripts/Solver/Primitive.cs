using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 솔버가 놓아 보는 그림 하나.
    /// 위치는 갖지 않는다 — 어디에 놓을지는 앵커가 정하고,
    /// 이쪽은 "무엇을 어떻게 그리는가" 만 담는다.
    /// </summary>
    [Serializable]
    public struct Primitive
    {
        public PrimitiveShape Shape;

        /// 외접원 반지름.
        public float Size;

        /// 정적(FixedLine) / 동적(FreeBody).
        public ToolType Tool;

        /// 회전각(라디안). 중력 때문에 절대각이다 —
        /// 같은 모양도 뒤집히면 물리가 다르다.
        public float Angle;

        public Primitive(PrimitiveShape shape, float size, ToolType tool, float angle)
        {
            Shape = shape;
            Size = size;
            Tool = tool;
            Angle = angle;
        }

        /// <summary>
        /// 이 그림을 position 에 놓았을 때의 획.
        /// 외접원 중심이 position 에 온다.
        /// </summary>
        public Stroke ToStroke(Vector2 position)
            => new Stroke(Tool, Points(position));

        /// <summary>
        /// 종류별 꼭짓점. 외접원 위의 점을 Angle 만큼 돌려 찍는다.
        /// 삼각형은 첫 점을 끝에 한 번 더 넣어 고리를 닫는다 —
        /// 열어 두면 한 변이 없는 ㄱ 자가 된다.
        /// </summary>
        List<Vector2> Points(Vector2 position)
        {
            switch (Shape)
            {
                case PrimitiveShape.Line:
                    return new List<Vector2>
                    {
                        position + Radial(Angle),
                        position + Radial(Angle + Mathf.PI),
                    };

                case PrimitiveShape.Triangle:
                    const float third = 2f * Mathf.PI / 3f;
                    return new List<Vector2>
                    {
                        position + Radial(Angle),
                        position + Radial(Angle + third),
                        position + Radial(Angle + third * 2f),
                        position + Radial(Angle),
                    };

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(Shape), Shape, "획으로 옮기는 규칙이 없는 종류다.");
            }
        }

        /// 중심에서 각 theta 로 Size 만큼 뻗은 벡터.
        Vector2 Radial(float theta)
            => new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * Size;
    }
}
