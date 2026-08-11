using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 후보 선택기가 내는 것이 여전히 벽뿐인가.
    /// IPrimitive 로 바꾸면서 스트로크를 붙이는 주체가 옮겨갔다 —
    /// 여기가 흔들리면 기존 레벨의 풀이가 통째로 달라진다.
    /// </summary>
    public class PrimitiveCandidatesTests
    {
        /// 꺾임이 하나 있는 통로. 바깥쪽 호와 안쪽 끊김이 둘 다 나온다.
        static readonly Vector2[] Bent =
        {
            new Vector2(0f, 0f),
            new Vector2(2f, 0f),
            new Vector2(3f, 1.5f),
        };

        [Test]
        public void 벽만_낸다()
        {
            Solution solution = Build(Bent);

            Assert.IsNotEmpty(solution.Strokes, "통로를 줬는데 벽이 하나도 안 나왔다.");
            Assert.IsEmpty(solution.Pivots, "벽만 있는 통로에 회전축이 붙었다.");

            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                Stroke stroke = solution.Strokes[i];

                Assert.AreEqual(ToolType.FixedLine, stroke.Tool, $"{i}번이 정적 발판이 아니다.");
                Assert.AreEqual(2, stroke.Points.Count, $"{i}번이 선분 하나가 아니다.");
            }
        }

        [Test]
        public void 프리미티브_하나가_스트로크_하나다()
        {
            // Line 은 1:1 이다. 여기가 어긋나면 AppendTo 가 덧붙이고 있다는 뜻이다.
            IPrimitive[] picked = PrimitiveCandidates.Select(Stage(), Bent);

            Assert.AreEqual(picked.Length, Build(Bent).Strokes.Count);
        }

        [Test]
        public void 모든_벽이_통로에서_떨어져_있다()
        {
            // 벽이 통로를 파고들면 공이 낀다. 잘라 내기가 사는지 본다.
            Solution solution = Build(Bent);
            float half = LevelData.BallRadius * 1.6f;

            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                var points = solution.Strokes[i].Points;

                for (int p = 0; p < points.Count; p++)
                    Assert.GreaterOrEqual(ToPath(points[p]), half * 0.9f - 1e-3f,
                        $"{i}번 벽의 점이 통로에 너무 붙었다.");
            }
        }

        static Solution Build(Vector2[] path)
        {
            var solution = new Solution();
            IPrimitive[] picked = PrimitiveCandidates.Select(Stage(), path);

            for (int i = 0; i < picked.Length; i++) picked[i].AppendTo(solution);

            return solution;
        }

        /// 벽 생성은 아직 지형을 보지 않는다. 통로만으로 결정된다.
        static StageData Stage() => new StageData { Level = new LevelData() };

        static float ToPath(Vector2 at)
        {
            float least = float.PositiveInfinity;

            for (int i = 0; i + 1 < Bent.Length; i++)
            {
                Vector2 a = Bent[i];
                Vector2 along = Bent[i + 1] - a;

                float t = Mathf.Clamp01(Vector2.Dot(at - a, along) / along.sqrMagnitude);
                least = Mathf.Min(least, Vector2.Distance(at, a + along * t));
            }

            return least;
        }
    }
}
