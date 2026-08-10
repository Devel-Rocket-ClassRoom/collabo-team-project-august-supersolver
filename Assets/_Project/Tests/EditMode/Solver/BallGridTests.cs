using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 격자 범위·셀 개수의 최소 보장.
    /// 공용 영역을 그대로 쓰는지, 레벨이 커지면
    /// 셀도 따라 느는지를 못박는다.
    /// </summary>
    public class BallGridTests
    {
        const float Eps = 0.0001f;

        /// 산수가 눈에 보이라고 고른 값이다.
        const float PosStep = 0.25f;
        const float VelStep = 1f;

        static BallQuantizer Quantizer() => new BallQuantizer(PosStep, VelStep);

        /// <param name="terrainRight">지형 오른쪽 끝. 레벨 크기를 이걸로 바꾼다.</param>
        static LevelData Level(float terrainRight = 5f)
        {
            var level = new LevelData
            {
                BallStart = new Vector2(0f, 0f),
                GoalPosition = new Vector2(5f, 0f),
            };
            level.Terrain.Add(new StaticSegment(
                new Vector2(-5f, -1f), new Vector2(terrainRight, -1f)));

            return level;
        }

        [Test]
        public void 격자_범위는_공용_플레이_영역_그대로다()
        {
            var level = Level();
            var grid = new BallGrid(level, Quantizer());
            Rect area = LevelDataArea.Calculate(level);

            Assert.AreEqual(area.xMin, grid.Area.xMin, Eps);
            Assert.AreEqual(area.xMax, grid.Area.xMax, Eps);
            Assert.AreEqual(area.yMin, grid.Area.yMin, Eps);
            Assert.AreEqual(area.yMax, grid.Area.yMax, Eps);
        }

        /// 실측값. 맵 생성 비용이 여기 달려 있어
        /// 폭이나 마진이 바뀌면 이 수가 먼저 깨진다.
        [Test]
        public void 샘플_레벨의_셀_수는_실측값과_같다()
        {
            var grid = new BallGrid(Level(), Quantizer());

            // x: [-7, 7.5] → 인덱스 -28..30, y: [-3, 2.5] → -12..10
            Assert.AreEqual(59, grid.Columns, "열");
            Assert.AreEqual(23, grid.Rows, "행");
            Assert.AreEqual(59 * 23, grid.CellCount, "셀 수");
        }

        [Test]
        public void 레벨이_커지면_셀_수도_늘어난다()
        {
            var small = new BallGrid(Level(), Quantizer());
            var large = new BallGrid(Level(15f), Quantizer());

            Assert.Greater(large.Columns, small.Columns, "넓어진 축");
            Assert.AreEqual(small.Rows, large.Rows, "안 건드린 축");
            Assert.Greater(large.CellCount, small.CellCount, "셀 수");
        }

        /// 폭을 절반으로 줄이면 축마다 셀이 대략 두 배다.
        [Test]
        public void 폭이_좁아지면_셀_수가_는다()
        {
            var coarse = new BallGrid(Level(), Quantizer());
            var fine = new BallGrid(Level(), new BallQuantizer(PosStep * 0.5f, VelStep));

            Assert.Greater(fine.CellCount, coarse.CellCount);
        }

        /// 영역 안 좌표는 전부 격자 인덱스 범위에 들어온다.
        [Test]
        public void 영역_안_좌표는_모두_셀_범위_안이다()
        {
            var q = Quantizer();
            var grid = new BallGrid(Level(), q);

            foreach (var point in new[]
            {
                grid.Area.min, grid.Area.max, grid.Area.center,
                new Vector2(grid.Area.xMin, grid.Area.yMax),
                new Vector2(grid.Area.xMax, grid.Area.yMin),
            })
            {
                BallCell cell = q.Quantize(point, Vector2.zero);

                Assert.GreaterOrEqual(cell.X - grid.MinX, 0, $"{point} 열 하한");
                Assert.Less(cell.X - grid.MinX, grid.Columns, $"{point} 열 상한");
                Assert.GreaterOrEqual(cell.Y - grid.MinY, 0, $"{point} 행 하한");
                Assert.Less(cell.Y - grid.MinY, grid.Rows, $"{point} 행 상한");
            }
        }
    }
}
