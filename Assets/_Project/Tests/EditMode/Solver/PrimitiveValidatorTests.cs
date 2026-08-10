using System;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 놓을 수 있는지 판정의 최소 보장.
    /// 하한·상한 경계값과 사유 구분을 못박는다.
    /// </summary>
    public class PrimitiveValidatorTests
    {
        const float Eps = 0.0001f;

        static readonly PrimitiveShape[] Shapes =
        {
            PrimitiveShape.Line, PrimitiveShape.Bowl, PrimitiveShape.Triangle,
        };

        /// 영역이 x[-7,7.5] y[-3,2.5] 가 되는 레벨.
        static LevelData Level()
        {
            var level = new LevelData
            {
                InkLimit = 20f,
                BallStart = Vector2.zero,
                GoalPosition = new Vector2(5f, 0f),
            };
            level.Terrain.Add(new StaticSegment(new Vector2(-5f, -1f), new Vector2(5f, -1f)));
            return level;
        }

        static Primitive Line(float size, Vector2 center) =>
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine, center, 0f, size);

        [Test]
        public void 공_반지름이_Size_하한이다()
        {
            var level = Level();

            Assert.AreEqual(PlacementReject.None,
                PrimitiveValidator.Validate(Line(0.25f, Vector2.zero), level, 0f));
            Assert.AreEqual(PlacementReject.TooSmall,
                PrimitiveValidator.Validate(Line(0.24f, Vector2.zero), level, 0f));
        }

        [Test]
        public void 상한으로_만든_프리미티브가_잉크_총량을_넘지_않는다()
        {
            var level = Level();

            foreach (var shape in Shapes)
            {
                float max = PrimitiveValidator.MaxSize(shape, level);
                var primitive = new Primitive(shape, ToolType.FixedLine, Vector2.zero, 0.7f, max);

                Assert.LessOrEqual(PrimitiveDecoder.ToStroke(primitive).Length(),
                    level.InkLimit + Eps, shape.ToString());
            }
        }

        [Test]
        public void 상한은_모양마다_다르다()
        {
            var level = Level();

            float line = PrimitiveValidator.MaxSize(PrimitiveShape.Line, level);
            float triangle = PrimitiveValidator.MaxSize(PrimitiveShape.Triangle, level);
            float bowl = PrimitiveValidator.MaxSize(PrimitiveShape.Bowl, level);

            Assert.AreEqual(10f, line, Eps, "길이 2R 이라 R 은 InkLimit/2");
            Assert.Less(triangle, bowl, "둘레가 길수록 상한이 낮다");
            Assert.Less(bowl, line);
        }

        [Test]
        public void 상한을_넘으면_혼자서도_기각된다()
        {
            var level = Level();

            foreach (var shape in Shapes)
            {
                float max = PrimitiveValidator.MaxSize(shape, level);
                var over = new Primitive(shape, ToolType.FixedLine, Vector2.zero, 0f, max * 1.01f);

                Assert.AreEqual(PlacementReject.TooLarge,
                    PrimitiveValidator.Validate(over, level, 0f), shape.ToString());

                var at = new Primitive(shape, ToolType.FixedLine, Vector2.zero, 0f, max);

                Assert.AreNotEqual(PlacementReject.TooLarge,
                    PrimitiveValidator.Validate(at, level, 0f), shape.ToString());
            }
        }

        [Test]
        public void 영역은_지형_공_목표를_담고_마진만큼_넓다()
        {
            var area = LevelDataArea.Calculate(Level());
            float margin = LevelDataArea.AreaMargin;

            Assert.AreEqual(-5f - margin, area.xMin, Eps, "지형 왼쪽 끝");
            Assert.AreEqual(5.5f + margin, area.xMax, Eps, "목표 오른쪽 끝");
            Assert.AreEqual(-1f - margin, area.yMin, Eps, "지형 아래");
            Assert.AreEqual(0.5f + margin, area.yMax, Eps, "목표 위쪽");
        }

        [Test]
        public void 영역_밖으로_삐져나가면_기각된다()
        {
            var level = Level();
            var area = LevelDataArea.Calculate(level);

            // 오른쪽 끝에 딱 붙인 선은 통과한다.
            var inside = Line(1f, new Vector2(area.xMax - 1f, 0f));
            var outside = Line(1f, new Vector2(area.xMax - 0.9f, 0f));

            Assert.AreEqual(PlacementReject.None,
                PrimitiveValidator.Validate(inside, level, 0f));
            Assert.AreEqual(PlacementReject.OutOfArea,
                PrimitiveValidator.Validate(outside, level, 0f));
        }

        [Test]
        public void 누적_잉크가_총량을_넘으면_기각된다()
        {
            var level = Level();
            var primitive = Line(1f, Vector2.zero);

            Assert.AreEqual(2f, PrimitiveValidator.Ink(primitive), Eps);

            Assert.AreEqual(PlacementReject.None,
                PrimitiveValidator.Validate(primitive, level, 18f), "딱 맞으면 통과");
            Assert.AreEqual(PlacementReject.InkExceeded,
                PrimitiveValidator.Validate(primitive, level, 18.1f));
        }

        [Test]
        public void 잉크는_실제_스트로크_길이와_같다()
        {
            foreach (var shape in Shapes)
            {
                var primitive = new Primitive(shape, ToolType.FreeBody,
                    new Vector2(1f, -0.5f), 0.9f, 1.7f);

                Assert.AreEqual(PrimitiveDecoder.ToStroke(primitive).Length(),
                    PrimitiveValidator.Ink(primitive), 0.001f, shape.ToString());
            }
        }

        [Test]
        public void 모든_사유가_구분된다()
        {
            Assert.AreEqual(6, Enum.GetValues(typeof(PlacementReject)).Length);
        }
    }
}
