using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 추의 무게를 상자 밖에서 읽을 수 있는가.
    /// 이 값이 실제 스트로크와 어긋나면 실측 표의 무게 축이 거짓말이 된다.
    /// </summary>
    public class WeightBlockTests
    {
        static readonly Vector2 Size = new Vector2(1f, 0.8f);

        [Test]
        public void 무게식이_실제_선_길이와_같다([Values(1, 2, 3, 5, 7)] int rows)
        {
            var block = new WeightBlock(Vector2.zero, Size, rows);

            Assert.AreEqual(block.ToStroke().Length(), block.Length, 1e-4f,
                $"{rows}줄에서 무게식과 실제 길이가 다르다.");
        }

        [Test]
        public void 줄을_늘리면_자리는_그대로고_무게만_는다()
        {
            var light = new WeightBlock(Vector2.zero, Size, 1);
            var heavy = new WeightBlock(Vector2.zero, Size, 7);

            Assert.Greater(heavy.Mass, light.Mass * 3f,
                "줄을 일곱 배로 늘렸는데 무게가 따라오지 않는다.");

            Bounds(light, out Vector2 lightMin, out Vector2 lightMax);
            Bounds(heavy, out Vector2 heavyMin, out Vector2 heavyMax);

            // 가벼운 쪽은 가운데 한 줄뿐이라 세로로만 상자보다 작다.
            Assert.AreEqual(lightMin.x, heavyMin.x, 1e-4f, "무거운 추가 옆으로 삐져나온다.");
            Assert.AreEqual(lightMax.x, heavyMax.x, 1e-4f, "무거운 추가 옆으로 삐져나온다.");
            Assert.AreEqual(Size.y * 0.5f, heavyMax.y, 1e-4f, "무거운 추가 상자 위로 넘친다.");
            Assert.AreEqual(-Size.y * 0.5f, heavyMin.y, 1e-4f, "무거운 추가 상자 아래로 넘친다.");
        }

        [Test]
        public void 줄_간격이_콜라이더_두께보다_넓다()
        {
            // 겹치면 도형만 늘고 시뮬이 느려진다.
            var block = new WeightBlock(Vector2.zero, Size, 7);

            Assert.LessOrEqual(block.Rows, block.MaxRows,
                $"{block.Rows}줄은 이 상자에 너무 촘촘하다. 한계는 {block.MaxRows}줄이다.");
        }

        static void Bounds(in WeightBlock block, out Vector2 min, out Vector2 max)
        {
            var points = block.ToStroke().Points;

            min = points[0];
            max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }
        }
    }
}
