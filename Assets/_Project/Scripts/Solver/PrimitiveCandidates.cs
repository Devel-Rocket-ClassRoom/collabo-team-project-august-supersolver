using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공 주위에 놓아 볼 프리미티브 후보 목록.
    /// Shape × Body × 각도 × Size × 방향 을 그대로 곱한다.
    /// 유효성은 보지 않는다 — Validate 가 누적 잉크를
    /// 받으므로 여기서 걸면 집합이 탐색 도중 바뀐다.
    /// </summary>
    public sealed class PrimitiveCandidates
    {
        static readonly ToolType[] Bodies = (ToolType[])Enum.GetValues(typeof(ToolType));

        readonly LevelData _level;
        readonly int _sizeSteps;
        readonly int _directions;
        readonly PrimitiveShape[] _shapes;

        /// <param name="sizeSteps">Size 축을 몇 칸으로 쪼갤지.
        /// 각도 칸 수는 Shape 규칙표가 정한다.</param>
        public PrimitiveCandidates(LevelData level, int sizeSteps)
            : this(level, sizeSteps, SolverConfig.CandidateDirections)
        {
        }

        public PrimitiveCandidates(LevelData level, int sizeSteps, int directions)
            : this(level, sizeSteps, directions, SolverConfig.CandidateShapes)
        {
        }

        /// <param name="shapes">후보로 낼 Shape. 테스트가 축을 고정할 때 쓴다.</param>
        public PrimitiveCandidates(
            LevelData level, int sizeSteps, int directions, PrimitiveShape[] shapes)
        {
            if (shapes == null || shapes.Length == 0)
                throw new ArgumentException("후보로 낼 Shape 이 없다.", nameof(shapes));

            _shapes = shapes;
            if (sizeSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(sizeSteps), sizeSteps, "1 이상이어야 한다.");
            if (directions < 1)
                throw new ArgumentOutOfRangeException(nameof(directions), directions, "1 이상이어야 한다.");

            _level = level;
            _sizeSteps = sizeSteps;
            _directions = directions;
        }

        /// 위치와 무관하게 고정인 후보 수.
        public int Count
        {
            get
            {
                int angles = 0;
                foreach (var shape in _shapes)
                    angles += PrimitiveShapeExtensions.Rule(shape).AngleDivisions;
                return angles * Bodies.Length * _sizeSteps * _directions;
            }
        }

        /// <summary>
        /// 공이 가는 쪽으로 편 부챗살 위에 놓는다.
        /// 공 자리에 그대로 놓으면 시작부터 박혀 튕겨 나가고,
        /// 그 궤적의 간선은 물리적으로 불가능한 이동이 된다.
        /// 반대로 공이 안 가는 쪽은 닿지를 않아 값을 못 한다.
        /// Pivot 은 아직 탐색 축이 아니라 전부 None 이다.
        /// </summary>
        /// <param name="ball">공의 위치와 속도. 속도가 부채의 기준이다.</param>
        public IEnumerable<Primitive> At(BallState ball)
        {
            float facing = Facing(ball.Velocity);

            foreach (PrimitiveShape shape in _shapes)
            {
                var rule = PrimitiveShapeExtensions.Rule(shape);
                float maxSize = PrimitiveValidator.MaxSize(shape, _level);

                for (int a = 0; a < rule.AngleDivisions; a++)
                {
                    // 각도는 절대각이다. 위로 열린 그릇과 아래로
                    // 열린 그릇은 중력 때문에 물리가 다르다.
                    float angle = rule.AnglePeriod * a / rule.AngleDivisions;

                    for (int s = 0; s < _sizeSteps; s++)
                    {
                        float size = SizeAt(s, maxSize);

                        // 외접원끼리 딱 맞닿는 거리. 더 가까우면 박히고
                        // 더 멀면 공에 닿지 못해 궤적을 못 바꾼다.
                        float reach = size + _level.BallRadius;

                        for (int d = 0; d < _directions; d++)
                        {
                            float theta = facing + Offset(d);
                            Vector2 center = ball.Position + new Vector2(
                                Mathf.Cos(theta), Mathf.Sin(theta)) * reach;

                            foreach (var body in Bodies)
                                yield return new Primitive(shape, body, center, angle, size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 부채의 기준 방향.
        /// 멈춘 공은 갈 곳이 없어 방향을 물을 수 없다 —
        /// 그때는 중력이 데려갈 아래쪽을 기준으로 삼는다.
        /// </summary>
        static float Facing(Vector2 velocity)
        {
            if (velocity.sqrMagnitude <= SolverConfig.StopSpeed * SolverConfig.StopSpeed)
                return -Mathf.PI * 0.5f;

            return Mathf.Atan2(velocity.y, velocity.x);
        }

        /// <summary>
        /// 기준에서 몇 라디안 벌릴지. 부채는 ±90° 다.
        /// 갈래가 하나면 정면만 본다.
        /// </summary>
        float Offset(int direction)
        {
            if (_directions == 1) return 0f;

            return -Mathf.PI * 0.5f + Mathf.PI * direction / (_directions - 1);
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
