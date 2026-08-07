using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 한 위치에 놓아 볼 프리미티브 후보 목록.
    /// Shape × Body × 각도 × Size 를 그대로 곱한다.
    /// 유효성은 보지 않는다 — Validate 가 누적 잉크를
    /// 받으므로 여기서 걸면 집합이 탐색 도중 바뀐다.
    /// </summary>
    public sealed class PrimitiveCandidates
    {
        static readonly ToolType[] Bodies = (ToolType[])Enum.GetValues(typeof(ToolType));

        readonly LevelData _level;
        readonly int _sizeSteps;

        /// <param name="sizeSteps">Size 축을 몇 칸으로 쪼갤지.
        /// 각도 칸 수는 Shape 규칙표가 정한다.</param>
        public PrimitiveCandidates(LevelData level, int sizeSteps)
        {
            if (sizeSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(sizeSteps), sizeSteps, "1 이상이어야 한다.");

            _level = level;
            _sizeSteps = sizeSteps;
        }

        /// 위치와 무관하게 고정인 후보 수.
        public int Count
        {
            get
            {
                int angles = 0;
                foreach (PrimitiveShape shape in Enum.GetValues(typeof(PrimitiveShape)))
                    angles += PrimitiveShapeExtensions.Rule(shape).AngleDivisions;
                return angles * Bodies.Length * _sizeSteps;
            }
        }

        /// <summary>
        /// Pivot 은 아직 탐색 축이 아니라 전부 None 이다.
        /// </summary>
        public IEnumerable<Primitive> At(Vector2 center)
        {
            foreach (PrimitiveShape shape in Enum.GetValues(typeof(PrimitiveShape)))
            {
                var rule = PrimitiveShapeExtensions.Rule(shape);
                float maxSize = PrimitiveValidator.MaxSize(shape, _level);

                for (int a = 0; a < rule.AngleDivisions; a++)
                {
                    float angle = rule.AnglePeriod * a / rule.AngleDivisions;

                    for (int s = 0; s < _sizeSteps; s++)
                    {
                        float size = SizeAt(s, maxSize);

                        foreach (var body in Bodies)
                            yield return new Primitive(shape, body, center, angle, size);
                    }
                }
            }
        }

        /// <summary>
        /// 공 반지름에서 Shape 별 상한까지 등비로 나눈다.
        /// 작은 쪽이 조금만 달라져도 결과가 갈려서다.
        /// </summary>
        float SizeAt(int step, float maxSize)
        {
            // 등비식으로 짚으면 오차가 상한을 넘길 수 있고
            // 그러면 가장 큰 후보가 TooLarge 로 죽는다.
            if (step == _sizeSteps - 1)
                return maxSize;

            float span = maxSize / _level.BallRadius;
            return _level.BallRadius * Mathf.Pow(span, (float)step / (_sizeSteps - 1));
        }
    }
}
