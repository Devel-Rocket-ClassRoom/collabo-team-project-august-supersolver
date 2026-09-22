using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    public class BarricadeStageTests
    {
        static readonly string[] Names =
        {
            "BarricadeDrop_BR1_FB1",
            "BarricadeBounce_SL1BR1_FB1",
            "BarricadeBomb_BB1BR1_FB1",
            "BarricadeWind_WD1BR1_FB1",
            "BarricadeRelay_BB1WD1SL1BR1_FB1",
        };

        [TestCaseSource(nameof(Names))]
        public void 한_획으로_바리게이트를_부수고_클리어한다(string name)
        {
            var stage = Load(name);
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-0.9f, 3.1f),
                new Vector2(0.1f, 3.1f),
            }));

            Assert.LessOrEqual(solution.TotalInk(), stage.Level.InkLimit);

            bool broken = false;
            using (var world = WorldBuilder.Build(stage, solution))
            {
                while (world.CurrentStep < SimWorld.DefaultMaxSteps && !world.IsTerminal)
                {
                    world.Step();
                    if (world.GetDevice(0).body == null) broken = true;
                }

                Assert.IsTrue(broken, $"{name}: 바리게이트가 부서지지 않았다.");
                Assert.AreEqual(SimOutcome.Clear, world.ToResult(solution.TotalInk()).Outcome,
                    $"{name}: 한 획 풀이로 클리어되지 않았다.");
            }
        }

        [TestCaseSource(nameof(Names))]
        public void 그리지_않으면_클리어되지_않는다(string name)
        {
            Assert.AreNotEqual(SimOutcome.Clear, SimRunner.Run(Load(name), Solution.Empty).Outcome);
        }

        [TestCaseSource(nameof(Names))]
        public void 바리게이트가_없으면_같은_한_획으로_클리어되지_않는다(string name)
        {
            var stage = Load(name);
            stage.Level.Devices.RemoveAt(0);
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-0.9f, 3.1f),
                new Vector2(0.1f, 3.1f),
            }));

            Assert.AreNotEqual(SimOutcome.Clear, SimRunner.Run(stage, solution).Outcome,
                $"{name}: 바리게이트 없이도 같은 풀이로 클리어된다.");
        }

        [TestCaseSource(nameof(Names))]
        public void 한_획의_작은_위치_오차를_허용한다(string name)
        {
            var stage = Load(name);
            foreach (float x in new[] { -0.25f, 0.25f })
            foreach (float y in new[] { -0.2f, 0.2f })
            {
                var solution = new Solution();
                solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
                {
                    new Vector2(-0.9f + x, 3.1f + y),
                    new Vector2(0.1f + x, 3.1f + y),
                }));

                Assert.AreEqual(SimOutcome.Clear, SimRunner.Run(stage, solution).Outcome,
                    $"{name}: 획 오차 ({x}, {y})에서 클리어되지 않았다.");
            }
        }

        [TestCase("BarricadeBounce_SL1BR1_FB1", 1)]
        [TestCase("BarricadeBomb_BB1BR1_FB1", 1)]
        [TestCase("BarricadeWind_WD1BR1_FB1", 1)]
        [TestCase("BarricadeRelay_BB1WD1SL1BR1_FB1", 1)]
        [TestCase("BarricadeRelay_BB1WD1SL1BR1_FB1", 2)]
        [TestCase("BarricadeRelay_BB1WD1SL1BR1_FB1", 3)]
        public void 보조_장치가_공의_경로를_바꾼다(string name, int deviceIndex)
        {
            var stage = Load(name);
            var solution = new Solution();
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-0.9f, 3.1f),
                new Vector2(0.1f, 3.1f),
            }));

            int clearStep = SimRunner.Run(stage, solution).EndStep;
            Vector2 withDevice = BallAtStep(stage, solution, clearStep);
            stage.Level.Devices.RemoveAt(deviceIndex);
            Vector2 withoutDevice = BallAtStep(stage, solution, clearStep);

            Assert.Greater(Vector2.Distance(withDevice, withoutDevice), 0.1f,
                $"{name}: 장치 {deviceIndex}가 공의 경로에 관여하지 않았다.");
        }

        static Vector2 BallAtStep(StageData stage, Solution solution, int step)
        {
            using (var world = WorldBuilder.Build(stage, solution))
            {
                for (int i = 0; i < step; i++) world.Step();
                return world.Ball.position;
            }
        }

        static StageData Load(string name)
        {
            string path = Path.Combine(Application.dataPath, "_Project/Levels", name + ".json");
            return StageData.FromJson(File.ReadAllText(path));
        }
    }
}
