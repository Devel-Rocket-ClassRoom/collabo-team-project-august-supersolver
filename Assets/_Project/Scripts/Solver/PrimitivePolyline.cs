using System.Collections.Generic;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// Shape 를 Stroke 용 폴리라인으로 펼친다.
    /// 여기서는 크기만 입힌 로컬 좌표까지 만든다.
    /// Center·Angle 은 월드 변환에서 한 번에 건다.
    /// </summary>
    public static class PrimitivePolyline
    {
        /// <summary>
        /// 외접원 반지름이 size 인 로컬 폴리라인.
        /// 닫힌 모양은 첫 점을 끝에 값 복사해 닫는다.
        /// Stroke.Points 에 닫힘 플래그가 없어서
        /// 첫 점과 끝 점의 일치가 닫힘의 유일한 표현이고,
        /// ColliderFactory 는 이걸로 폴리곤/엣지를 가른다.
        /// </summary>
        public static List<Vector2> LocalPoints(this PrimitiveShape shape, float size)
        {
            var unit = shape.Rule().UnitPoints;

            var points = new List<Vector2>(unit.Length);
            for (int i = 0; i < unit.Length; i++)
                points.Add(unit[i] * size);

            // 끝 점을 다시 계산하면 부동소수 오차로
            // 닫힘 판정이 깨진다. 반드시 값 복사다.
            if (IsClosed(unit))
                points[unit.Length - 1] = points[0];

            return points;
        }

        static bool IsClosed(Vector2[] points) =>
            points.Length >= 3 && points[0] == points[points.Length - 1];
    }
}
