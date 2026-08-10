using NUnit.Framework;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 전 피처 JSON 이 그대로 읽히는가.
    /// 물리 반영은 FeatureLevelSimTests 가 본다.
    /// </summary>
    public class FeatureLevelJsonTests
    {
        [Test]
        public void 레벨과_솔루션_파일이_저장소에_있다()
        {
            Assert.IsTrue(FeatureLevelFile.Exists,
                $"파일을 찾을 수 없다:\n{FeatureLevelFile.LevelPath}\n{FeatureLevelFile.SolutionPath}");
        }

        [Test]
        public void 레벨의_값이_그대로_들어온다()
        {
            var level = FeatureLevelFile.LoadLevel();

            Assert.AreEqual(FeatureLevelFile.ExpectedInkLimit, level.InkLimit, 1e-4f);
            Assert.AreEqual(FeatureLevelFile.ExpectedKillY, level.KillY, 1e-4f);
            Assert.AreEqual(FeatureLevelFile.ExpectedTerrainCount, level.Terrain.Count,
                "지형 세그먼트 개수가 파일과 다르다.");
        }

        [Test]
        public void 레벨에_장치가_실려_있다()
        {
            var level = FeatureLevelFile.LoadLevel();

            Assert.AreEqual(FeatureLevelFile.ExpectedDeviceCount, level.Devices.Count);

            var bomb = level.Devices[0];
            Assert.AreEqual(DeviceType.Bomb, bomb.Type);
            Assert.Greater(bomb.Radius, 0f, "영향 반경이 비어 있다.");
            Assert.Greater(bomb.Power, 0f, "세기가 비어 있다.");
            Assert.Greater(bomb.JitterSteps, 0,
                "흔들림이 0 이면 시드가 결과에 반영되는지 이 레벨로 확인할 수 없다.");
        }

        [Test]
        public void 솔루션의_스트로크_구성이_그대로_들어온다()
        {
            var solution = FeatureLevelFile.LoadSolution();

            Assert.AreEqual(FeatureLevelFile.ExpectedStrokeCount, solution.Strokes.Count);

            int fixedLines = 0;
            int freeBodies = 0;

            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                var stroke = solution.Strokes[i];

                Assert.IsTrue(stroke.IsValid, $"스트로크 {i} 의 포인트가 모자란다 — 파싱이 흘렸다.");

                if (stroke.Tool == ToolType.FixedLine) fixedLines++;
                else freeBodies++;
            }

            Assert.AreEqual(FeatureLevelFile.ExpectedFixedLineCount, fixedLines);
            Assert.AreEqual(FeatureLevelFile.ExpectedFreeBodyCount, freeBodies);
        }

        [Test]
        public void 솔루션에_회전축_두_형태가_모두_있다()
        {
            var solution = FeatureLevelFile.LoadSolution();

            Assert.AreEqual(FeatureLevelFile.ExpectedPivotCount, solution.Pivots.Count);

            int toWorld = 0;
            int betweenStrokes = 0;

            for (int i = 0; i < solution.Pivots.Count; i++)
            {
                var pivot = solution.Pivots[i];

                if (pivot.StrokeA == PivotJoint.WorldIndex || pivot.StrokeB == PivotJoint.WorldIndex)
                    toWorld++;
                else
                    betweenStrokes++;
            }

            Assert.AreEqual(1, toWorld, "월드 고정 회전축이 없다.");
            Assert.AreEqual(1, betweenStrokes, "스트로크 간 회전축이 없다.");
        }

        [Test]
        public void 솔루션이_잉크_제한을_넘지_않는다()
        {
            var level = FeatureLevelFile.LoadLevel();
            var solution = FeatureLevelFile.LoadSolution();

            Assert.LessOrEqual(solution.TotalInk(), level.InkLimit,
                $"샘플 솔루션이 잉크 제한을 넘는다 ({solution.TotalInk():F2} / {level.InkLimit:F2}).");
            Assert.Greater(solution.TotalInk(), 0f, "잉크 소모가 0 이다 — 스트로크가 비었다.");
        }
    }
}
