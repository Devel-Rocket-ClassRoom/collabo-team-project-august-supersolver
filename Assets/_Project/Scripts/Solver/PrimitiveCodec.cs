using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브 배치 ↔ 고정 길이 실수 벡터.
    /// 탐색·최적화는 고정 차원 벡터만 다루므로
    /// 이 변환이 그 경계면이다.
    /// 개수 k 는 고정이다 — 가변 개수는 k 별
    /// 분리 실행으로 다루고 게이트는 쓰지 않는다.
    /// </summary>
    public sealed class PrimitiveCodec
    {
        /// 프리미티브 하나가 차지하는 축 수.
        public const int Dimensions = 8;

        const int CenterXAxis = 0;
        const int CenterYAxis = 1;

        /// 각도는 순환값이라 한 축으로 못 편다.
        /// 0 과 주기 끝이 벡터에서 이웃이어야 해서
        /// 원 위의 두 축(cos·sin)으로 펴 둔다.
        const int AngleCosAxis = 2;
        const int AngleSinAxis = 3;

        const int SizeAxis = 4;
        const int ShapeAxis = 5;
        const int BodyAxis = 6;
        const int PivotAxis = 7;

        const int BodyCount = 2;
        const int PivotCount = 3;

        readonly LevelData _level;
        readonly Rect _area;

        public PrimitiveCodec(LevelData level)
        {
            _level = level;
            _area = LevelDataArea.Calculate(level);
        }

        /// 프리미티브 k 개를 담는 벡터 길이.
        public static int Length(int count) => count * Dimensions;

        public float[] Encode(IReadOnlyList<Primitive> primitives)
        {
            var vector = new float[Length(primitives.Count)];
            for (int i = 0; i < primitives.Count; i++)
                EncodeOne(primitives[i], vector, i * Dimensions);
            return vector;
        }

        public Primitive[] Decode(float[] vector)
        {
            if (vector.Length % Dimensions != 0)
                throw new ArgumentException(
                    $"벡터 길이 {vector.Length} 가 {Dimensions} 의 배수가 아니다.", nameof(vector));

            var primitives = new Primitive[vector.Length / Dimensions];
            for (int i = 0; i < primitives.Length; i++)
                primitives[i] = DecodeOne(vector, i * Dimensions);
            return primitives;
        }

        /// <summary>
        /// 축 스케일이 다르면(위치는 미터, 각도는 라디안)
        /// 이후 최적화가 한쪽으로 기울어지므로
        /// 연속 축은 전부 [0,1] 로 눕힌다.
        /// 영역 밖 프리미티브는 자르지 않고 그대로 내보낸다 —
        /// 왕복이 정확해야 하고 유효성은 Validator 가 본다.
        /// </summary>
        void EncodeOne(in Primitive primitive, float[] vector, int at)
        {
            // PivotLink 는 아직 탐색 축이 아니다.
            // 조용히 버리면 왕복이 깨지므로 막는다.
            if (primitive.PivotLink != Primitive.WorldLink)
                throw new ArgumentException("PivotLink 는 벡터에 담기지 않는다.", nameof(primitive));

            vector[CenterXAxis + at] = Normalize(primitive.Center.x, _area.xMin, _area.xMax);
            vector[CenterYAxis + at] = Normalize(primitive.Center.y, _area.yMin, _area.yMax);

            float turn = Mathf.Repeat(primitive.Angle, primitive.AnglePeriod) / primitive.AnglePeriod;
            float radians = turn * 2f * Mathf.PI;
            vector[AngleCosAxis + at] = (Mathf.Cos(radians) + 1f) * 0.5f;
            vector[AngleSinAxis + at] = (Mathf.Sin(radians) + 1f) * 0.5f;

            vector[SizeAxis + at] = Normalize(primitive.Size,
                _level.BallRadius, PrimitiveValidator.MaxSize(primitive.Shape, _level));

            vector[ShapeAxis + at] = CategoryToAxis((int)primitive.Shape, PrimitiveShapeExtensions.RuleCount);
            vector[BodyAxis + at] = CategoryToAxis((int)primitive.Body, BodyCount);
            vector[PivotAxis + at] = CategoryToAxis((int)primitive.Pivot, PivotCount);
        }

        /// <summary>
        /// Size 범위가 Shape 에 딸려 있어 Shape 를 먼저 읽는다.
        /// 축을 Shape 별 상한으로 재면 어떤 모양이든
        /// 같은 값이 같은 "상한 대비 비율"을 뜻한다.
        /// </summary>
        Primitive DecodeOne(float[] vector, int at)
        {
            var shape = (PrimitiveShape)AxisToCategory(
                vector[ShapeAxis + at], PrimitiveShapeExtensions.RuleCount);

            var center = new Vector2(
                Denormalize(vector[CenterXAxis + at], _area.xMin, _area.xMax),
                Denormalize(vector[CenterYAxis + at], _area.yMin, _area.yMax));

            float cos = vector[AngleCosAxis + at] * 2f - 1f;
            float sin = vector[AngleSinAxis + at] * 2f - 1f;
            float turn = Mathf.Repeat(Mathf.Atan2(sin, cos) / (2f * Mathf.PI), 1f);
            float angle = turn * PrimitiveShapeExtensions.AnglePeriod(shape);

            float size = Denormalize(vector[SizeAxis + at],
                _level.BallRadius, PrimitiveValidator.MaxSize(shape, _level));

            var body = (ToolType)AxisToCategory(vector[BodyAxis + at], BodyCount);
            var pivot = (PivotSpot)AxisToCategory(vector[PivotAxis + at], PivotCount);

            return new Primitive(shape, body, center, angle, size, pivot);
        }

        static float Normalize(float value, float min, float max) => (value - min) / (max - min);

        static float Denormalize(float axis, float min, float max) =>
            Mathf.Lerp(min, max, Mathf.Clamp01(axis));

        /// 칸 가운데를 쓴다. 경계에 놓으면 부동소수
        /// 오차 한 번에 옆 칸으로 넘어가 왕복이 깨진다.
        static float CategoryToAxis(int index, int count) => (index + 0.5f) / count;

        static int AxisToCategory(float axis, int count) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(axis) * count), 0, count - 1);
    }
}
