using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 벡터 경계면의 최소 보장.
    /// 왕복 무손실·각도 연속성·k 배 길이를 못박는다.
    /// </summary>
    public class PrimitiveCodecTests
    {
        const float Eps = 0.0001f;

        static readonly PrimitiveShape[] Shapes =
        {
            PrimitiveShape.Line, PrimitiveShape.Bowl, PrimitiveShape.Triangle,
        };

        static readonly ToolType[] Bodies = { ToolType.FixedLine, ToolType.FreeBody };

        static readonly PivotSpot[] Pivots =
        {
            PivotSpot.None, PivotSpot.Start, PivotSpot.Middle,
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

        static Primitive[] Sample() => new[]
        {
            new Primitive(PrimitiveShape.Line, ToolType.FixedLine,
                new Vector2(-3.5f, 1.25f), 0.4f, 1.5f),
            new Primitive(PrimitiveShape.Bowl, ToolType.FreeBody,
                new Vector2(2f, -2f), 4.9f, 0.8f, PivotSpot.Middle),
            new Primitive(PrimitiveShape.Triangle, ToolType.FreeBody,
                new Vector2(6.5f, 0f), 1.9f, 2.75f, PivotSpot.Start),
        };

        static void AssertSame(in Primitive expected, in Primitive actual, string label)
        {
            Assert.AreEqual(expected.Shape, actual.Shape, label);
            Assert.AreEqual(expected.Body, actual.Body, label);
            Assert.AreEqual(expected.Pivot, actual.Pivot, label);
            Assert.AreEqual(expected.Center.x, actual.Center.x, Eps, label);
            Assert.AreEqual(expected.Center.y, actual.Center.y, Eps, label);
            Assert.AreEqual(expected.Angle, actual.Angle, Eps, label);
            Assert.AreEqual(expected.Size, actual.Size, Eps, label);
        }

        [Test]
        public void 왕복해도_그대로다()
        {
            var codec = new PrimitiveCodec(Level());
            var original = Sample();

            var back = codec.Decode(codec.Encode(original));

            Assert.AreEqual(original.Length, back.Length);
            for (int i = 0; i < original.Length; i++)
                AssertSame(original[i], back[i], i.ToString());
        }

        [Test]
        public void 카테고리_조합이_전부_왕복한다()
        {
            var codec = new PrimitiveCodec(Level());

            foreach (var shape in Shapes)
            foreach (var body in Bodies)
            foreach (var pivot in Pivots)
            {
                var primitive = new Primitive(shape, body, new Vector2(1f, 0.5f), 0.3f, 1.2f, pivot);
                var back = codec.Decode(codec.Encode(new[] { primitive }))[0];

                AssertSame(primitive, back, $"{shape}/{body}/{pivot}");
            }
        }

        [Test]
        public void 각도_경계에서_벡터가_튀지_않는다()
        {
            var codec = new PrimitiveCodec(Level());

            foreach (var shape in Shapes)
            {
                float period = PrimitiveShapeExtensions.AnglePeriod(shape);
                float step = period * 0.001f;

                var before = codec.Encode(new[] { At(shape, period - step) });
                var after = codec.Encode(new[] { At(shape, step) });

                // 주기를 넘어간 두 각은 같은 모양이다.
                // 한 축 정규화라면 0.998 만큼 벌어질 자리다.
                Assert.Less(Distance(before, after), 0.02f, shape.ToString());
            }
        }

        [Test]
        public void 주기를_넘은_각은_같은_벡터가_된다()
        {
            var codec = new PrimitiveCodec(Level());

            foreach (var shape in Shapes)
            {
                float period = PrimitiveShapeExtensions.AnglePeriod(shape);

                var once = codec.Encode(new[] { At(shape, 0.3f) });
                var twice = codec.Encode(new[] { At(shape, 0.3f + period) });

                Assert.Less(Distance(once, twice), Eps, shape.ToString());
            }
        }

        [Test]
        public void k를_바꾸면_길이가_k배가_된다()
        {
            var codec = new PrimitiveCodec(Level());
            int unit = codec.Encode(new[] { Sample()[0] }).Length;

            Assert.AreEqual(PrimitiveCodec.Dimensions, unit);
            Assert.AreEqual(unit * 3, codec.Encode(Sample()).Length);
            Assert.AreEqual(unit * 7, PrimitiveCodec.Length(7));
        }

        [Test]
        public void 정상_배치는_모든_축이_0과_1_사이다()
        {
            var codec = new PrimitiveCodec(Level());

            foreach (float axis in codec.Encode(Sample()))
            {
                Assert.GreaterOrEqual(axis, 0f);
                Assert.LessOrEqual(axis, 1f);
            }
        }

        [Test]
        public void 아무_벡터나_디코드해도_범위_안이다()
        {
            var level = Level();
            var codec = new PrimitiveCodec(level);
            var area = LevelDataArea.Calculate(level);
            var random = new System.Random(1234);

            var vector = new float[PrimitiveCodec.Length(64)];
            for (int i = 0; i < vector.Length; i++)
                vector[i] = (float)random.NextDouble();

            foreach (var primitive in codec.Decode(vector))
            {
                Assert.IsTrue(area.Contains(primitive.Center), "중심이 영역 안");
                Assert.GreaterOrEqual(primitive.Size, LevelData.BallRadius);
                Assert.LessOrEqual(primitive.Size, PrimitiveValidator.MaxSize(primitive.Shape, level));
                Assert.GreaterOrEqual(primitive.Angle, 0f);
                Assert.Less(primitive.Angle, primitive.AnglePeriod);
            }
        }

        static Primitive At(PrimitiveShape shape, float angle) =>
            new Primitive(shape, ToolType.FixedLine, new Vector2(1f, 0.5f), angle, 1.2f);

        static float Distance(float[] a, float[] b)
        {
            float sum = 0f;
            for (int i = 0; i < a.Length; i++)
                sum += (a[i] - b[i]) * (a[i] - b[i]);
            return Mathf.Sqrt(sum);
        }
    }
}
