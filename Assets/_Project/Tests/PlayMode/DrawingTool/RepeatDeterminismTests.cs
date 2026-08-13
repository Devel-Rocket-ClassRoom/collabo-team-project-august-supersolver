using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PPS.Core;
using PPS.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 완료조건 4-1. 되돌리기·초기화·재시도를 아무리 반복해도
    /// 같은 그림에서 같은 물리가 나와야 솔버가 검증한 판과
    /// 플레이어가 푼 판이 같은 판이다.
    /// </summary>
    public class RepeatDeterminismTests
    {
        const string ScenePath = "Assets/_Project/Scenes/DrawingTool.unity";

        /// 비교 구간. 이 판은 그 전에 판정이 난다.
        const int Steps = 600;

        const int Attempts = 5;

        /// 기준 그림을 만드는 액션 수. 되돌리기 깊이가 된다.
        const int ActionCount = 4;

        StageFlow _flow;
        StageLoader _stage;
        DrawingSession _session;
        GameSimDriver _driver;

        [UnitySetUp]
        public IEnumerator 씬을_띄운다()
        {
            SceneManager.LoadScene(ScenePath, LoadSceneMode.Single);

            yield return null;   // Awake·Start
            yield return null;   // 카메라 fit 은 LateUpdate 에서 풀린다

            _flow = Object.FindFirstObjectByType<StageFlow>();
            _stage = Object.FindFirstObjectByType<StageLoader>();
            _session = Object.FindFirstObjectByType<DrawingSession>();
            _driver = Object.FindFirstObjectByType<GameSimDriver>();

            Assert.IsNotNull(_flow, "씬에 StageFlow 가 없다 — 배선이 빠졌다");
            Assert.IsNotNull(_stage.Stage, "스테이지 파일이 안 물렸다");
        }

        [UnityTest]
        public IEnumerator 재시도를_반복해도_해시_궤적이_같다()
        {
            Draw();

            List<ulong> first = null;

            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                _flow.OnClickPlay();

                // 누산기를 얼리고 스텝을 직접 돌린다. Time.deltaTime
                // 이 섞이면 회차마다 스텝 수가 달라져 비교가 성립하지
                // 않는다 — 재는 것은 프레임이 아니라 재구축이다.
                _driver.Paused = true;

                List<ulong> trace = StepAndHash(_driver.World);

                _flow.OnClickRetry();
                yield return null;

                if (first == null)
                {
                    first = trace;
                    AssertWorthComparing(first);
                    continue;
                }

                AssertTracesMatch(first, trace, $"{attempt + 1} 회차");
            }
        }

        [UnityTest]
        public IEnumerator 되돌리기와_초기화를_반복해도_같은_그림이_나온다()
        {
            Draw();

            Solution reference = _session.Solution.Clone();
            List<ulong> expected = TraceOf(reference);

            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < ActionCount; i++) _session.OnClickUndo();
                for (int i = 0; i < ActionCount; i++) _session.OnClickRedo();
            }

            AssertSame(reference, expected, "되돌리기 왕복");

            // 초기화도 액션 하나라 되돌리기 한 번에 돌아온다.
            _session.OnClickClear();
            _session.OnClickUndo();

            AssertSame(reference, expected, "초기화 후 되돌리기");

            // 「다시 그려도」 — 지운 자리에 같은 그림을 다시 만든다.
            _session.OnClickClear();
            Draw();

            AssertSame(reference, expected, "초기화 후 재작화");

            yield break;
        }

        /// <summary>
        /// 기준 그림. 핀을 획 사이에 끼워 재결합이 도는
        /// 자리를 지난다 — 인덱스가 밀리면 여기서 갈린다.
        /// </summary>
        void Draw()
        {
            _session.AddStroke(Bar(ToolType.FreeBody, 1f));
            _session.AddPivot(new PivotJoint(0, PivotJoint.WorldIndex, new Vector2(0f, 1f)));
            _session.AddStroke(Bar(ToolType.FreeBody, 2f));
            _session.AddStroke(Bar(ToolType.FixedLine, 0f));
        }

        /// 최소 획 길이를 넉넉히 넘는 막대.
        static Stroke Bar(ToolType tool, float y) =>
            new Stroke(tool, new List<Vector2>
            {
                new Vector2(-1f, y),
                new Vector2(1f, y),
            });

        List<ulong> TraceOf(Solution solution)
        {
            using (SimWorld world = WorldBuilder.Build(_stage.Stage, solution))
                return StepAndHash(world);
        }

        static List<ulong> StepAndHash(SimWorld world)
        {
            var trace = new List<ulong>();

            for (int i = 0; i < Steps; i++)
            {
                world.Step();
                trace.Add(WorldHasher.Hash(world));

                if (world.IsTerminal) break;
            }

            return trace;
        }

        void AssertSame(Solution reference, List<ulong> expected, string moment)
        {
            AssertSolutionsMatch(reference, _session.Solution, moment);
            AssertTracesMatch(expected, TraceOf(_session.Solution), moment);
        }

        static void AssertSolutionsMatch(Solution expected, Solution actual, string moment)
        {
            Assert.AreEqual(expected.Strokes.Count, actual.Strokes.Count, $"{moment}: 획 개수가 다르다");
            Assert.AreEqual(expected.Pivots.Count, actual.Pivots.Count, $"{moment}: 핀 개수가 다르다");

            for (int s = 0; s < expected.Strokes.Count; s++)
            {
                Stroke before = expected.Strokes[s];
                Stroke after = actual.Strokes[s];

                Assert.AreEqual(before.Tool, after.Tool, $"{moment}: 획 {s} 의 도구가 다르다");
                Assert.AreEqual(before.Points.Count, after.Points.Count,
                    $"{moment}: 획 {s} 의 점 개수가 다르다");

                for (int p = 0; p < before.Points.Count; p++)
                    AssertSameBits(before.Points[p], after.Points[p], $"{moment}: 획 {s} 점 {p}");
            }

            for (int i = 0; i < expected.Pivots.Count; i++)
            {
                PivotJoint before = expected.Pivots[i];
                PivotJoint after = actual.Pivots[i];

                Assert.AreEqual(before.StrokeA, after.StrokeA, $"{moment}: 핀 {i} 의 StrokeA 가 다르다");
                Assert.AreEqual(before.StrokeB, after.StrokeB, $"{moment}: 핀 {i} 의 StrokeB 가 다르다");
                AssertSameBits(before.Anchor, after.Anchor, $"{moment}: 핀 {i} 의 Anchor");
            }

            Assert.IsTrue(expected.TotalInk().Equals(actual.TotalInk()),
                $"{moment}: 잉크 총량이 달라졌다 — {expected.TotalInk():R} → {actual.TotalInk():R}");
        }

        /// <summary>
        /// Vector2 의 == 는 오차를 허용해 비트 비교에 못 쓴다.
        /// float.Equals 가 비트 비교다.
        /// </summary>
        static void AssertSameBits(Vector2 expected, Vector2 actual, string where)
        {
            Assert.IsTrue(expected.x.Equals(actual.x) && expected.y.Equals(actual.y),
                $"{where} 좌표가 달라졌다: ({expected.x:R}, {expected.y:R}) → " +
                $"({actual.x:R}, {actual.y:R})");
        }

        /// <summary>
        /// 대조군. 한 스텝 만에 끝나거나 값이 안 변하는
        /// 궤적끼리는 무엇을 비교해도 같아서, 회귀가 나도
        /// 초록불이 된다.
        /// </summary>
        static void AssertWorthComparing(List<ulong> trace)
        {
            Assert.Greater(trace.Count, 30, "궤적이 너무 짧다 — 비교가 헛돈다");

            for (int i = 1; i < trace.Count; i++)
                if (trace[i] != trace[0]) return;

            Assert.Fail("궤적의 해시가 전부 같다 — 월드가 움직이지 않았다");
        }

        static void AssertTracesMatch(List<ulong> expected, List<ulong> actual, string moment)
        {
            int common = Mathf.Min(expected.Count, actual.Count);

            for (int i = 0; i < common; i++)
            {
                if (expected[i] != actual[i])
                    Assert.Fail($"{moment}: 스텝 {i} 에서 갈라졌다. " +
                                $"기준=0x{expected[i]:X16} 이번=0x{actual[i]:X16} " +
                                $"(그 앞 {i} 스텝은 완전히 일치)");
            }

            Assert.AreEqual(expected.Count, actual.Count,
                $"{moment}: 스텝 수가 다르다 — 판정 시점이 갈렸다");
        }
    }
}
