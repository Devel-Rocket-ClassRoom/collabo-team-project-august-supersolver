using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 팀 4인이 공유하는 계약 타입의 최소 보장. 여기가 깨지면 잉크 표시·리플레이·
    /// 레벨 데이터가 동시에 틀어진다.
    /// </summary>
    public class ContractTests
    {
        [Test]
        public void 스트로크_길이는_폴리라인_길이의_합이다()
        {
            var stroke = new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(3f, 4f),   // 5
                new Vector2(3f, 5f),   // 1
            });

            Assert.AreEqual(6f, stroke.Length(), 0.0001f);
        }

        [Test]
        public void 포인트가_모자란_스트로크는_무효다()
        {
            Assert.IsFalse(new Stroke(ToolType.FixedLine, null).IsValid);
            Assert.IsFalse(new Stroke(ToolType.FixedLine, new List<Vector2> { Vector2.zero }).IsValid);
            Assert.AreEqual(0f, new Stroke(ToolType.FixedLine, null).Length());
        }

        [Test]
        public void 총_잉크는_스트로크_길이의_합이고_회전축은_잉크를_쓰지_않는다()
        {
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                Vector2.zero, new Vector2(2f, 0f),
            }));
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                Vector2.zero, new Vector2(0f, 3f),
            }));
            solution.Pivots.Add(new PivotJoint(0, 1, Vector2.zero));

            Assert.AreEqual(5f, solution.TotalInk(), 0.0001f);
        }

        [Test]
        public void 솔루션_복사본은_원본과_포인트_리스트를_공유하지_않는다()
        {
            var original = TestLevels.BridgeSolution();
            var copy = original.Clone();

            copy.Strokes[0].Points[0] = new Vector2(99f, 99f);

            Assert.AreNotEqual(99f, original.Strokes[0].Points[0].x,
                "복사본을 수정했더니 원본이 바뀐다 — 솔버가 후보를 변형할 때 원본 솔루션이 오염된다.");
        }

        [Test]
        public void 레벨데이터는_JSON_왕복에서_보존된다()
        {
            var original = TestLevels.Gap();
            var restored = LevelData.FromJson(original.ToJson());

            Assert.AreEqual(original.Id, restored.Id);
            Assert.AreEqual(original.InkLimit, restored.InkLimit);
            Assert.AreEqual(original.BallStart, restored.BallStart);
            Assert.AreEqual(original.GoalPosition, restored.GoalPosition);
            Assert.AreEqual(original.KillY, restored.KillY);
            Assert.AreEqual(original.Terrain.Count, restored.Terrain.Count);
            Assert.AreEqual(original.Terrain[1].B, restored.Terrain[1].B);
        }

        [Test]
        public void 복원한_레벨데이터로_돌린_결과가_원본과_같다()
        {
            // "레벨 추가는 데이터만으로" 가 성립하려면 직렬화 왕복이 시뮬 결과를 바꾸면 안 된다.
            var original = TestLevels.RampToGoal();
            var restored = LevelData.FromJson(original.ToJson());

            var a = new List<ulong>();
            var b = new List<ulong>();
            SimRunner.RunTraced(original, null, 3, a, 300);
            SimRunner.RunTraced(restored, null, 3, b, 300);

            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void 고정_시간_간격은_60분의_1이다()
        {
            Assert.AreEqual(1f / 60f, SimWorld.FixedDt);
            Assert.AreEqual(1800, SimWorld.DefaultMaxSteps, "시뮬 시간 상한 30초 (설계서 결정 8)");
        }
    }
}
