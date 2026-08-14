using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 기획 요구인 최소 48dp 를 씬에서 직접 확인한다.
    /// 치수를 테스트에 베껴 적으면 씬이 바뀔 때
    /// 감사가 헛돈다.
    /// </summary>
    public class TouchTargetAuditTests
    {
        const string ScenePath = "Assets/_Project/Scenes/DrawingTool.unity";

        /// CanvasScaler 레퍼런스 해상도. 씬 치수의 단위다.
        static readonly Vector2 Reference = new Vector2(1080f, 1920f);

        /// 지원 하한 기기의 화면 물리 폭. 16:9 5.0인치가
        /// 2.45인치다 — minSdk 26 이면 사정권에 든다.
        const float MinScreenWidthInches = 2.45f;

        static readonly string[] TouchTargets =
        {
            "Btn_Settings", "Btn_Play", "Btn_PauseResume", "Btn_Speed",
            "Btn_FixedLine", "Btn_FreeBody", "Btn_PivotGroup", "Btn_Erase",
            "Btn_Reset", "Btn_Undo", "Btn_Redo", "Btn_Retry",
        };

        Scene _scene;
        bool _openedHere;
        Dictionary<string, RectTransform> _rects;

        [SetUp]
        public void 씬을_읽는다()
        {
            // 편집 중인 씬을 뺏으면 테스트가 작업을 망친다.
            _scene = SceneManager.GetSceneByPath(ScenePath);
            _openedHere = !_scene.isLoaded;

            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            _rects = new Dictionary<string, RectTransform>();

            foreach (GameObject root in _scene.GetRootGameObjects())
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                    _rects[rect.name] = rect;
        }

        [TearDown]
        public void 씬을_닫는다()
        {
            if (_openedHere && _scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void 모든_터치_타겟이_지원_하한_기기에서_48dp_이상이다()
        {
            var offenses = new List<string>();

            foreach (string name in TouchTargets)
            {
                Assert.IsTrue(_rects.ContainsKey(name), $"씬에서 {name} 을 찾지 못했다");

                Vector2 size = ReferenceSize(_rects[name]);
                float shortest = Mathf.Min(size.x, size.y);
                float needed = RequiredInches(shortest);

                if (needed > MinScreenWidthInches)
                    offenses.Add($"{name}: {shortest:F0}ref — 화면 폭 {needed:F2}\" 이상에서만 48dp");
            }

            Assert.IsEmpty(offenses,
                $"\n지원 하한 {MinScreenWidthInches}\" 에서 미달:\n" + string.Join("\n", offenses));
        }

        /// <summary>
        /// 이 치수가 48dp 를 채우는 최소 화면 폭.
        /// CanvasScaler 가 폭 기준이라 픽셀 크기가
        /// 화면 폭에 비례하고, dpi 가 그대로 약분된다.
        /// </summary>
        static float RequiredInches(float referenceSize) =>
            TouchMetrics.MinTouchTargetDp * Reference.x
            / (referenceSize * DeviceUnits.BaselineDpi);

        /// <summary>
        /// 앵커를 타고 올라가며 레퍼런스 단위 크기를 푼다.
        /// RectTransform.rect 는 게임 뷰 크기에 좌우돼
        /// 테스트에서 값이 흔들린다.
        /// </summary>
        static Vector2 ReferenceSize(RectTransform rect)
        {
            if (rect.GetComponent<Canvas>() != null) return Reference;

            var parent = rect.parent as RectTransform;
            if (parent == null) return Reference;

            Vector2 parentSize = ReferenceSize(parent);
            Vector2 span = rect.anchorMax - rect.anchorMin;

            return new Vector2(
                span.x * parentSize.x + rect.sizeDelta.x,
                span.y * parentSize.y + rect.sizeDelta.y);
        }
    }
}
