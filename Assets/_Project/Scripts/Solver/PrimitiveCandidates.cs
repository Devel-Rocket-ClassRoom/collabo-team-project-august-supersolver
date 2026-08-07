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
        /// 칸 가운데를 쓴다. 끝을 그대로 쓰면 하한·상한이
        /// 곧 기각 경계라 오차 한 번에 통째로 죽는다.
        /// 상한이 Shape 마다 달라 같은 s 도 크기가 다르다.
        /// </summary>
        float SizeAt(int step, float maxSize) =>
            Mathf.Lerp(_level.BallRadius, maxSize, (step + 0.5f) / _sizeSteps);
    }
}
