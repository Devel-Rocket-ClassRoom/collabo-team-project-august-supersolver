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
    /// 전이의 부수 효과를 실제 씬에서 잰다. 전이표 자체는
    /// StageStateMachineTests 몫이고, 여기서 보는 것은 저장·
    /// 물리·카메라가 전이를 따라오는가다.
    /// </summary>
    public class StageFlowTests
    {
        const string ScenePath = "Assets/_Project/Scenes/DrawingTool.unity";

        StageFlow _flow;
        StageLoader _stage;
        DrawingSession _session;
        DrawInputBehaviour _input;
        GameSimDriver _driver;
        Camera _camera;

        [UnitySetUp]
        public IEnumerator 씬을_띄운다()
        {
            SceneManager.LoadScene(ScenePath, LoadSceneMode.Single);

            yield return null;   // Awake·Start
            yield return null;   // 카메라 fit 은 LateUpdate 에서 풀린다

            _flow = Object.FindFirstObjectByType<StageFlow>();
            _stage = Object.FindFirstObjectByType<StageLoader>();
            _session = Object.FindFirstObjectByType<DrawingSession>();
            _input = Object.FindFirstObjectByType<DrawInputBehaviour>();
            _driver = Object.FindFirstObjectByType<GameSimDriver>();
            _camera = Object.FindFirstObjectByType<CanvasCameraFitter>().GetComponent<Camera>();

            Assert.IsNotNull(_flow, "씬에 StageFlow 가 없다 — 배선이 빠졌다");
            Assert.IsNotNull(_stage.Stage, "스테이지 파일이 안 물렸다");
        }

        [UnityTest]
        public IEnumerator 플레이가_월드를_짓는다()
        {
            _session.AddStroke(Bar());

            _flow.OnClickPlay();
            yield return null;

            Assert.AreEqual(StageMode.Simulate, _flow.Mode);
            Assert.IsTrue(_driver.HasWorld, "플레이를 눌렀는데 월드가 없다");
        }

        [UnityTest]
        public IEnumerator 재시도는_획을_남기고_물리만_버린다()
        {
            _session.AddStroke(Bar());
            float ink = _session.Solution.TotalInk();

            _flow.OnClickPlay();

            // 공이 실제로 움직인 뒤에 되돌린다. 첫 프레임에
            // 재시도하면 아무것도 안 움직여도 통과한다.
            for (int i = 0; i < 30; i++) yield return null;

            _flow.OnClickRetry();
            yield return null;

            Assert.AreEqual(StageMode.Draw, _flow.Mode);
            Assert.IsFalse(_driver.HasWorld, "재시도인데 월드가 남았다");
            Assert.AreEqual(1, _session.Solution.Strokes.Count, "그린 획이 사라졌다");
            Assert.AreEqual(ink, _session.Solution.TotalInk(), "잉크가 달라졌다");
        }

        [UnityTest]
        public IEnumerator 시뮬레이션_중에는_캔버스_입력이_죽는다()
        {
            Assert.IsTrue(_input.enabled, "그리기인데 입력이 죽어 있다");

            _flow.OnClickPlay();
            yield return null;
            Assert.IsFalse(_input.enabled, "시뮬레이션 중에 획이 그려진다");

            _flow.OnClickPauseResume();
            yield return null;
            Assert.IsFalse(_input.enabled, "일시정지 중에 획이 그려진다");

            _flow.OnClickRetry();
            yield return null;
            Assert.IsTrue(_input.enabled, "그리기로 돌아왔는데 입력이 안 살아났다");
        }

        /// <summary>완료조건 6-1.</summary>
        [UnityTest]
        public IEnumerator 모드를_오가도_카메라가_그대로다()
        {
            float size = _camera.orthographicSize;
            Vector3 position = _camera.transform.position;

            _flow.OnClickPlay();
            yield return null;
            AssertCamera(size, position, "플레이");

            _flow.OnClickRetry();
            yield return null;
            AssertCamera(size, position, "재시도");
        }

        void AssertCamera(float size, Vector3 position, string moment)
        {
            Assert.AreEqual(size, _camera.orthographicSize, $"{moment} 에서 카메라 크기가 변했다");
            Assert.AreEqual(position, _camera.transform.position, $"{moment} 에서 카메라 위치가 변했다");
        }

        /// 최소 획 길이를 넉넉히 넘는 막대.
        static Stroke Bar() =>
            new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1f, 1f),
                new Vector2(1f, 1f),
            });
    }
}
