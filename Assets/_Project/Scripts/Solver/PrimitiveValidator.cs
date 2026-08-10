using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 디코드 전에 프리미티브를 놓을 수 있는지 본다.
    /// 시뮬 한 번이 후보 수만 개보다 훨씬 비싸므로,
    /// 못 놓는 것은 점을 펼치기 전에 걸러낸다.
    /// </summary>
    public static class PrimitiveValidator
    {
        /// <summary>
        /// 기각 사유. None 이면 놓을 수 있다.
        /// </summary>
        /// <param name="usedInk">이미 놓은 프리미티브가 쓴 잉크.</param>
        public static PlacementReject Validate(in Primitive primitive, LevelData level, float usedInk)
        {
            if (primitive.Size < LevelData.BallRadius)
                return PlacementReject.TooSmall;

            // 상한을 Size 가 아니라 잉크로 비교해야
            // MaxSize 왕복에서 생기는 오차가 끼지 않는다.
            float ink = Ink(primitive);
            if (ink > level.InkLimit)
                return PlacementReject.TooLarge;

            if (!Inside(LevelDataArea.Calculate(level), primitive))
                return PlacementReject.OutOfArea;

            if (usedInk + ink > level.InkLimit)
                return PlacementReject.InkExceeded;

            return PlacementReject.None;
        }

        /// 이 프리미티브가 쓸 잉크. 외접원이 아니라
        /// 실제 폴리라인 길이라 Shape 마다 다르다.
        public static float Ink(in Primitive primitive) =>
            PrimitiveShapeExtensions.Rule(primitive.Shape).InkPerRadius * primitive.Size;

        /// 잉크만 놓고 본 Shape 별 Size 상한.
        /// 후보 격자의 Size 축을 여기까지만 뻗는다.
        public static float MaxSize(PrimitiveShape shape, LevelData level) =>
            level.InkLimit / PrimitiveShapeExtensions.Rule(shape).InkPerRadius;

        /// 외접원 상자로 본다. 각도를 안 보므로
        /// 실제보다 조금 크게 잡히지만 삼각함수가 없다.
        static bool Inside(Rect area, in Primitive primitive) =>
            primitive.Center.x - primitive.Size >= area.xMin &&
            primitive.Center.x + primitive.Size <= area.xMax &&
            primitive.Center.y - primitive.Size >= area.yMin &&
            primitive.Center.y + primitive.Size <= area.yMax;
    }
}
