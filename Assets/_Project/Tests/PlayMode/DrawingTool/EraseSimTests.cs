using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using PPS.Core.Tests;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 완료조건 1-2. 재매핑이 인덱스만 맞춘 게 아니라 실제로
    /// 맞는 바디에 조인트를 붙였는가 — 화면으로는 안 보이고
    /// 시뮬을 돌려야 드러나므로 해시로만 닫힌다.
    /// </summary>
    public class EraseSimTests
    {
        /// 막대가 흔들리는 구간을 넉넉히 덮는다.
        const int Steps = 300;

        const int Seed = 7;

        /// <summary>
        /// 기둥(획 0)을 지운 그림. 뒤 인덱스가 둘 다 당겨지고
        /// 기둥에 매달렸던 축은 상대를 잃는다.
        /// </summary>
        static Solution ErasedFirstStroke()
        {
            Solution solution = TestLevels.PivotSolution();

            solution.Strokes.RemoveAt(0);
            SolutionEditing.RemapPivots(solution, 0);
            return solution;
        }

        /// <summary>
        /// 「처음부터 이 두 획만 그린 그림」. 재매핑 규칙에서
        /// 손으로 뽑은 기대값이라, 구현이 규칙과 어긋나면
        /// 두 궤적이 갈린다.
        /// </summary>
        static Solution DrawnWithoutPillar()
        {
            Solution full = TestLevels.PivotSolution();

            var expected = new Solution();
            expected.Strokes.Add(full.Strokes[1]);
            expected.Strokes.Add(full.Strokes[2]);

            // 기둥이 없어 매달린 축은 한 칸이 빈 채 남는다.
            expected.Pivots.Add(new PivotJoint(
                PivotJoint.Unbound, 0, TestLevels.PivotOnFixedLine));

            // 월드 고정은 한 칸 당겨진 막대를 그대로 문다.
            expected.Pivots.Add(new PivotJoint(
                1, PivotJoint.WorldIndex, TestLevels.PivotOnWorld));

            return expected;
        }

        static List<ulong> Trace(Solution solution)
        {
            var trace = new List<ulong>();
            SimRunner.RunTraced(TestLevels.PivotSwing(), solution, Seed, trace, Steps);
            return trace;
        }

        [Test]
        public void 지운_뒤의_궤적이_처음부터_그렇게_그린_그림과_같다()
        {
            List<ulong> erased = Trace(ErasedFirstStroke());

            Assert.Greater(erased.Count, 30, "궤적이 너무 짧다 — 비교가 헛돈다");
            CollectionAssert.AreEqual(Trace(DrawnWithoutPillar()), erased,
                "재매핑이 물리까지 못 따라갔다 — 엉뚱한 획에 조인트가 붙었다");
        }

        [Test]
        public void 재매핑을_빼먹으면_궤적이_갈린다()
        {
            // 대조군. 안 갈리면 위 검사가 재매핑 없이도 통과한다.
            Solution unmapped = TestLevels.PivotSolution();
            unmapped.Strokes.RemoveAt(0);

            CollectionAssert.AreNotEqual(Trace(ErasedFirstStroke()), Trace(unmapped),
                "핀을 그대로 둬도 같은 물리가 나온다 — 검사가 헛돈다");
        }
    }
}
