using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 추를 얼마나 높이 들 수 있는지가 이 값에서 나온다.
    /// 없는 여유를 있다고 읽으면 추가 지형에 걸려 판까지 오지 못한다.
    /// </summary>
    public class HeadroomTests
    {
        const float Fallback = 99f;

        [Test]
        public void 천장까지의_거리를_낸다()
        {
            var terrain = new List<StaticSegment> { Ceiling(-5f, 5f, 4f) };

            Assert.AreEqual(4f, Headroom.Above(terrain, Vector2.zero, 1f, Fallback), 1e-4f);
        }

        [Test]
        public void 막는_것이_없으면_기본값을_낸다()
        {
            var terrain = new List<StaticSegment> { Ceiling(-5f, 5f, 4f) };

            // 천장이 옆으로 비켜 있다.
            Assert.AreEqual(Fallback, Headroom.Above(terrain, new Vector2(20f, 0f), 1f, Fallback));
            Assert.AreEqual(Fallback, Headroom.Above(null, Vector2.zero, 1f, Fallback));
        }

        [Test]
        public void 아래에_있는_지형은_세지_않는다()
        {
            var terrain = new List<StaticSegment> { Ceiling(-5f, 5f, -3f) };

            Assert.AreEqual(Fallback, Headroom.Above(terrain, Vector2.zero, 1f, Fallback));
        }

        [Test]
        public void 폭_안에_걸리는_것을_놓치지_않는다()
        {
            // 세로선 하나만 쏘면 지나쳐 버리는 자리에 천장을 둔다.
            var terrain = new List<StaticSegment> { Ceiling(1.2f, 3f, 2f) };

            Assert.AreEqual(Fallback, Headroom.Above(terrain, Vector2.zero, 1f, Fallback),
                "폭 밖인데 걸렸다.");
            Assert.AreEqual(2f, Headroom.Above(terrain, Vector2.zero, 3f, Fallback), 1e-4f,
                "폭 안에 있는 천장을 놓쳤다.");
        }

        [Test]
        public void 기울어진_천장은_가장_낮은_쪽으로_잰다()
        {
            var terrain = new List<StaticSegment>
            {
                new StaticSegment(new Vector2(-1f, 2f), new Vector2(1f, 6f)),
            };

            // 폭 2 를 걸치면 왼쪽 끝의 2 가 가장 낮다.
            Assert.AreEqual(2f, Headroom.Above(terrain, Vector2.zero, 2f, Fallback), 1e-4f);
        }

        static StaticSegment Ceiling(float fromX, float toX, float y)
            => new StaticSegment(new Vector2(fromX, y), new Vector2(toX, y));
    }
}
