using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 지형에 박힌 셀을 걸러내는 판정 검증.
    /// 놓치면 벽에서 튕겨 나간 궤적이 간선이 되어
    /// 불가능한 이동을 맵에 심는다.
    /// </summary>
    public class BallSpawnTests
    {
        const float Radius = 0.25f;

        /// 원점을 지나는 길이 10 의 수평 지형.
        static LevelData Flat()
        {
            return new LevelData
            {
                BallRadius = Radius,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 0f), new Vector2(5f, 0f)),
                },
            };
        }

        [Test]
        public void 지형에서_충분히_떨어진_자리는_안_막힌다()
        {
            Assert.IsFalse(BallSpawn.Blocked(Flat(), new Vector2(0f, 1f)));
        }

        [Test]
        public void 지형에_박힌_자리는_막힌다()
        {
            Assert.IsTrue(BallSpawn.Blocked(Flat(), new Vector2(0f, 0.1f)), "선분 위쪽");
            Assert.IsTrue(BallSpawn.Blocked(Flat(), Vector2.zero), "선분 위");
            Assert.IsTrue(BallSpawn.Blocked(Flat(), new Vector2(-3f, -0.2f)), "선분 아래쪽");
        }

        /// <summary>
        /// 반지름만큼 떨어진 자리는 지면에 얹힌 상태다.
        /// 가장 흔한 자세라 여기서 막으면
        /// 쓸 수 있는 셀을 대량으로 버린다.
        /// </summary>
        [Test]
        public void 지면에_얹힌_자리는_안_막힌다()
        {
            Assert.IsFalse(BallSpawn.Blocked(Flat(), new Vector2(0f, Radius)));
        }

        /// <summary>
        /// 수선의 발이 선분 밖이면 끝점까지의 거리로 쳐야 한다.
        /// 직선까지의 거리로 재면 선분을 무한히 늘린 셈이 되어
        /// 지형이 없는 자리를 막는다.
        /// </summary>
        [Test]
        public void 선분_바깥_연장선_위는_안_막힌다()
        {
            Assert.IsFalse(BallSpawn.Blocked(Flat(), new Vector2(6f, 0f)));
        }

        [Test]
        public void 끝점_근처는_막힌다()
        {
            Assert.IsTrue(BallSpawn.Blocked(Flat(), new Vector2(5.1f, 0.1f)));
        }

        [Test]
        public void 지형이_없으면_안_막힌다()
        {
            var empty = new LevelData { BallRadius = Radius, Terrain = new List<StaticSegment>() };

            Assert.IsFalse(BallSpawn.Blocked(empty, Vector2.zero));
        }
    }
}
