using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PPS.MapEditor.EditorTools
{
    /// <summary>
    /// 맵 에디터 화면 골격을 씬에 세운다.
    /// 손으로 만든 계층은 씬 파일 충돌이 잦고
    /// 앵커 값이 사람마다 달라진다.
    /// </summary>
    public static class MapEditorLayoutBuilder
    {
        const string RootName = "MapEditorCanvas";

        /// 세로 기준 해상도. 앵커 비율의 기준이다.
        static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        // 세이프 에리어 안에서의 세로 분할 비율.
        // 픽셀이 아니라 비율이라 화면비가 바뀌어도
        // 위아래 관계가 그대로 유지된다.
        const float BottomBarTop = 0.18f;
        const float TopBarBottom = 0.88f;

        static readonly Color BackgroundColor = new Color32(0x1A, 0x1C, 0x22, 0xFF);
        static readonly Color EditAreaColor = new Color32(0xFF, 0xE6, 0xB3, 0xFF);
        static readonly Color SafeAreaColor = new Color32(0x00, 0xE5, 0xFF, 0x1E);
        static readonly Color TopBarColor = new Color32(0x1B, 0x4F, 0xA0, 0xB4);
        static readonly Color CenterColor = new Color32(0x0B, 0x6E, 0x6E, 0x50);
        static readonly Color BottomBarColor = new Color32(0x6B, 0x3F, 0xA0, 0xB4);

        [MenuItem("Tools/맵 에디터/레이아웃 생성", false, 100)]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();

            var existing = Object.FindFirstObjectByType<Canvas>();
            if (existing != null && existing.name == RootName)
            {
                if (!EditorUtility.DisplayDialog(
                        "맵 에디터 레이아웃",
                        $"'{RootName}' 가 이미 있습니다. 지우고 다시 만들까요?",
                        "다시 만들기", "취소"))
                    return;

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject canvasGo = CreateCanvas();
            var canvasRect = (RectTransform)canvasGo.transform;

            // 배경과 편집 영역은 세이프 에리어 밖이다.
            // 노치 뒤까지 채워야 검은 띠가 안 생긴다.
            CreateStretched("Background", canvasRect, BackgroundColor);
            CreateStretched("EditArea", canvasRect, EditAreaColor);

            var safeArea = CreateStretched("SafeArea", canvasRect, SafeAreaColor);
            safeArea.GetComponent<Image>().raycastTarget = false;
            Undo.AddComponent<SafeAreaPanel>(safeArea.gameObject);

            CreateRegion("TopBar", safeArea,
                new Vector2(0f, TopBarBottom), Vector2.one, TopBarColor);
            CreateRegion("Center", safeArea,
                new Vector2(0f, BottomBarTop), new Vector2(1f, TopBarBottom), CenterColor);
            CreateRegion("BottomBar", safeArea,
                Vector2.zero, new Vector2(1f, BottomBarTop), BottomBarColor);

            EnsureEventSystem();

            Selection.activeGameObject = canvasGo;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static GameObject CreateCanvas()
        {
            var go = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "맵 에디터 레이아웃 생성");

            var canvas = Undo.AddComponent<Canvas>(go);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = Undo.AddComponent<CanvasScaler>(go);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            // 폭·높이를 반반 본다. 한쪽만 보면
            // 화면비가 달라질 때 반대 축이 넘친다.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Undo.AddComponent<GraphicRaycaster>(go);
            return go;
        }

        /// <summary>부모를 꽉 채우는 자리표시 패널.</summary>
        static RectTransform CreateStretched(string name, RectTransform parent, Color color)
            => CreateRegion(name, parent, Vector2.zero, Vector2.one, color);

        /// <summary>
        /// 앵커만으로 자리를 잡는다. 오프셋을 0 으로
        /// 두어야 부모가 늘어날 때 같이 늘어난다.
        /// </summary>
        static RectTransform CreateRegion(
            string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "맵 에디터 레이아웃 생성");
            Undo.SetTransformParent(go.transform, parent, name);

            var rect = (RectTransform)go.transform;
            rect.localScale = Vector3.one;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = Undo.AddComponent<Image>(go);
            image.color = color;

            return rect;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "맵 에디터 레이아웃 생성");

            // 프로젝트 입력이 신형 하나뿐이라
            // 구형 모듈을 붙이면 입력이 죽는다.
            Undo.AddComponent<InputSystemUIInputModule>(go);
        }
    }
}
