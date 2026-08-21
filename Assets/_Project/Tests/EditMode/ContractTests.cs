using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 공유 계약 타입의 최소 보장.
    /// 월드를 만들지 않는 검사만 남긴다 —
    /// 나머지는 ContractSimTests 로 갔다.
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
        public void 총_잉크는_스트로크_길이의_합에_회전축_개수만큼_더한_값이다()
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
            Assert.AreEqual(5f, solution.TotalInk(), 0.0001f, "획만 있을 때는 길이의 합이다");

            solution.Pivots.Add(new PivotJoint(0, 1, Vector2.zero));

            Assert.AreEqual(5f + PivotJoint.InkCost, solution.TotalInk(), 0.0001f,
                "핀이 잉크를 안 쓰면 개수에 상한이 없어진다");
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
            // 진입점은 잃었지만 스테이지에 실려
            // 직렬화되므로 보존은 그대로 필요하다.
            var original = TestLevels.Gap();
            var restored = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(original));

            Assert.AreEqual(original.InkLimit, restored.InkLimit);
            Assert.AreEqual(original.BallStart, restored.BallStart);
            Assert.AreEqual(original.GoalPosition, restored.GoalPosition);
            Assert.AreEqual(original.KillY, restored.KillY);
            Assert.AreEqual(original.Terrain.Count, restored.Terrain.Count);
            Assert.AreEqual(original.Terrain[1].B, restored.Terrain[1].B);
        }

        [Test]
        public void 고정_시간_간격은_60분의_1이다()
        {
            Assert.AreEqual(1f / 60f, SimWorld.FixedDt);
            Assert.AreEqual(1800, SimWorld.DefaultMaxSteps, "시뮬 시간 상한 30초");
        }
    }
}
