using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브의 모양. Shape 별로 다른 규칙
    /// (각도 주기·점 생성)은 전부 이 축에 붙인다.
    /// </summary>
    public enum PrimitiveShape
    {
        /// 직선
        Line = 0,

        /// 그릇(U자)
        Bowl = 1,

        /// 정삼각형
        Triangle = 2,
    }

    public static class PrimitiveShapeExtensions
    {
        /// <summary>
        /// 각도가 한 바퀴 도는 주기(라디안).
        /// 이 값을 넘는 각은 같은 모양이 되므로
        /// 후보 격자를 그만큼만 만든다.
        /// </summary>
        public static float AnglePeriod(this PrimitiveShape shape)
        {
            switch (shape)
            {
                // 뒤집어도 같은 선이다.
                case PrimitiveShape.Line: return Mathf.PI;

                // 세 변이 같아 120° 마다 겹친다.
                case PrimitiveShape.Triangle: return 2f * Mathf.PI / 3f;

                default: return 2f * Mathf.PI;
            }
        }
    }
}
