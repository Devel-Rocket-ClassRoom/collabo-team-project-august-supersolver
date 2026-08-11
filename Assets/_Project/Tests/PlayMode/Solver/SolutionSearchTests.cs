using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Tests
{
    /// <summary>
    /// 두 패스가 순서대로 도는가.
    /// 통로로 되는 레벨에서 지렛대까지 가면 값을 낭비하는 것이고,
    /// 통로로 안 되는 레벨에서 지렛대를 안 보면 풀 수 있는 것을 놓친다.
    /// </summary>
    public class SolutionSearchTests
    {
        static SolutionSearch Search()
        {
            Assert.IsTrue(LeverPresetFile.Exists,
                $"프리셋 파일이 없다. LeverPresetTests 를 먼저 돌린다 — {LeverPresetFile.RelativePath}");

            return new SolutionSearch(LeverPresets.Load());
        }

        static StageData Slope() => SearchLevels.Stage("비탈", SearchLevels.Slope());

        static StageData Uphill() => SearchLevels.Stage("오르막", SearchLevels.Uphill());

        [Test]
        public void 통로로_풀리는_레벨은_첫_패스에서_끝난다()
        {
            SolveReport report = Search().Solve(Slope());

            Debug.Log($"비탈 — {report}");

            // 0회면 통로가 안 나온 것이다. 푸는 데 실패한 것과 전혀 다른 문제라
            // 먼저 갈라 둔다 — 안 그러면 레벨 탓을 솔버 탓으로 읽게 된다.
            Assert.Greater(report.Tries, 0,
                "한 번도 안 굴렸다 — BallPath 가 통로를 못 냈다. 레벨 쪽 문제다.");

            Assert.AreEqual(SolvePass.Corridor, report.Pass,
                "통로로 풀릴 레벨인데 통로 패스가 못 풀었다.");
            Assert.IsEmpty(report.Solution.Pivots,
                "통로만으로 풀렸다는데 회전축이 들어 있다 — 지렛대가 섞였다.");
        }

        [Test]
        public void 오르막에서는_지렛대_패스까지_간다()
        {
            SolveReport report = Search().Solve(Uphill());

            Debug.Log($"오르막 — {report}");

            // 풀렸는지는 물리가 정한다. 여기서 확인하는 것은
            // 통로에서 멈추지 않고 지렛대까지 실제로 굴려 봤다는 것이다.
            Assert.Greater(report.Tries, 1,
                "통로만 굴려 보고 끝났다 — 지렛대 후보가 하나도 안 걸렸다.");

            Assert.AreNotEqual(SolvePass.Corridor, report.Pass,
                "통로만으로 풀렸다 — 지렛대가 필요한 레벨이 아니다.");
        }

        [Test]
        public void 조회는_잉크가_적은_것부터_낸다()
        {
            var presets = LeverPresets.Load();
            var reaching = new List<LeverPreset>();

            presets.Reaching(new Vector2(3f, 2f), reaching);

            Assert.IsNotEmpty(reaching, "(3, 2) 로 보낼 프리셋이 하나도 없다.");

            for (int i = 1; i < reaching.Count; i++)
                Assert.LessOrEqual(reaching[i - 1].Ink, reaching[i].Ink,
                    $"{i}번째에서 잉크 순서가 뒤집혔다.");
        }

        [Test]
        public void 왼쪽_목표는_뒤집어_찾는다()
        {
            var presets = LeverPresets.Load();
            var right = new List<LeverPreset>();
            var left = new List<LeverPreset>();

            presets.Reaching(new Vector2(3f, 2f), right);
            presets.Reaching(new Vector2(-3f, 2f), left);

            Assert.AreEqual(right.Count, left.Count,
                "좌우 대칭인 목표인데 걸리는 프리셋 수가 다르다.");
        }
    }
}
