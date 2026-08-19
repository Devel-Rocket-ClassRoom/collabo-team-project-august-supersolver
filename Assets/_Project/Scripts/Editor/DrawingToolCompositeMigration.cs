using System;
using PPS.Core;
using PPS.DrawingTool;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 월드를 뗀 뒤 남은 배선을 채운다. 한 번 돌리고 지우는
/// 이관 도구다 — 손으로 고치면 fileID 가 어긋나 조용히
/// 끊긴다. 무엇 하나라도 못 찾으면 저장하지 않고 던진다.
/// </summary>
public static class DrawingToolCompositeMigration
{
    const string UiPrefabPath = "Assets/_Project/Prefabs/DrawingTool/DrawingToolSceneUI.prefab";
    const string WorldPrefabPath = "Assets/_Project/Prefabs/DrawingTool/DrawingToolComposite.prefab";
    const string LegacyScenePath = "Assets/_Project/Scenes/DrawingTool.unity";
    const string MapSimPrefabPath = "Assets/_Project/Prefabs/MapEditor/MapSim.prefab";

    const string BackgroundMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

    /// 앵커와 UI 자리의 짝. 열거형 순서가 아니라 이름으로
    /// 짝지어 항목이 늘어도 조용히 밀리지 않는다.
    static readonly (TutorialAnchor Anchor, string Path)[] TutorialAnchors =
    {
        (TutorialAnchor.Settings, "TopBar/Btn_Settings"),
        (TutorialAnchor.Play, "TopBar/TopRightSlot/Btn_Play"),
        (TutorialAnchor.PauseResume, "TopBar/TopRightSlot/Btn_PauseResume"),
        (TutorialAnchor.Speed, "TopBar/Btn_Speed"),
        (TutorialAnchor.Inkband, "InkBand"),
        (TutorialAnchor.FixedLine, "BottomBand/DrawPanel/ToolGroup/Btn_FixedLine"),
        (TutorialAnchor.Freebody, "BottomBand/DrawPanel/ToolGroup/Btn_FreeBody"),
        (TutorialAnchor.Erase, "BottomBand/DrawPanel/ToolGroup/Btn_Erase"),
        (TutorialAnchor.PivotGroup, "BottomBand/DrawPanel/ToolGroup/Btn_PivotGroup"),
        (TutorialAnchor.Reset, "BottomBand/DrawPanel/ActionGroup/Btn_Reset"),
        (TutorialAnchor.Undo, "BottomBand/DrawPanel/ActionGroup/Btn_Undo"),
        (TutorialAnchor.Redo, "BottomBand/DrawPanel/ActionGroup/Btn_Redo"),
        (TutorialAnchor.Retry, "BottomBand/SimPanel/Btn_Retry"),
    };

    /// 버튼과 UI 경계 메서드의 짝. 코어는 클릭을 모른다.
    static readonly (string Path, string Method)[] ButtonCalls =
    {
        ("BottomBand/DrawPanel/ToolGroup/Btn_FixedLine", "OnClickFixedLine"),
        ("BottomBand/DrawPanel/ToolGroup/Btn_FreeBody", "OnClickFreeBody"),
        ("BottomBand/DrawPanel/ToolGroup/Btn_Erase", "OnClickErase"),
        ("BottomBand/DrawPanel/ToolGroup/Btn_PivotGroup/JustPivot", "OnClickPivotSingle"),
        ("BottomBand/DrawPanel/ToolGroup/Btn_PivotGroup/WorldPivot", "OnClickPivotWorld"),
        ("BottomBand/DrawPanel/ActionGroup/Btn_Undo", "OnClickUndo"),
        ("BottomBand/DrawPanel/ActionGroup/Btn_Redo", "OnClickRedo"),
        ("BottomBand/DrawPanel/ActionGroup/Btn_Reset", "OnClickReset"),
        ("TopBar/TopRightSlot/Btn_Play", "OnClickPlay"),
        ("TopBar/TopRightSlot/Btn_PauseResume", "OnClickPauseResume"),
        ("BottomBand/SimPanel/Btn_Retry", "OnClickRetry"),
    };

    [MenuItem("Tools/드로잉툴/구조 이관 실행")]
    public static void Run()
    {
        try
        {
            DrawingToolComposite world = CompleteWorldPrefab();
            RewireUiPrefab(world);
            RewireLegacyScene(world);
            StripMissingScripts(MapSimPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[이관] 완료.");
        }
        catch (Exception e)
        {
            // batchmode 종료 코드로 드러나야 한다. 삼키면
            // 깨진 프리팹이 통과한 것처럼 보인다.
            Debug.LogError($"[이관] 중단: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    // ── 월드 프리팹 ──────────────────────────────

    /// <summary>
    /// 손으로 만든 프리팹에 빠진 것만 채운다. 배경과 테마가
    /// 없으면 판은 돌지만 화면이 회색으로 남는다.
    /// </summary>
    static DrawingToolComposite CompleteWorldPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WorldPrefabPath);

        try
        {
            var composite = root.GetComponent<DrawingToolComposite>();
            if (composite == null) throw new Exception("루트에 DrawingToolComposite 가 없다");

            RequireWired(composite, "_levelView", "_pivots", "_strokes", "_simView", "_input");

            var levelView = root.GetComponentInChildren<LevelView>(true);
            var simView = root.GetComponentInChildren<SimStageView>(true);
            if (levelView == null || simView == null)
                throw new Exception("LevelView 또는 SimStageView 가 없다");

            var backgroundView = root.GetComponentInChildren<BackgroundView>(true);
            if (backgroundView == null)
            {
                GameObject go = Child(root, "BackgroundView");
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = LoadMaterial(BackgroundMaterialGuid);

                backgroundView = go.AddComponent<BackgroundView>();
                Set(backgroundView, "_renderer", renderer);
                Debug.Log("[이관] 월드에 BackgroundView 를 넣었다.");
            }

            var theme = root.GetComponentInChildren<DrawingToolAssetApplier>(true);
            if (theme == null)
            {
                theme = Child(root, "DrawingToolAssetApplier")
                    .AddComponent<DrawingToolAssetApplier>();
                Debug.Log("[이관] 월드에 DrawingToolAssetApplier 를 넣었다.");
            }

            Set(theme, "_background", backgroundView);
            Set(theme, "_levelView", levelView);
            Set(theme, "_simView", simView);

            PrefabUtility.SaveAsPrefabAsset(root, WorldPrefabPath, out bool saved);
            if (!saved) throw new Exception($"월드 프리팹을 저장하지 못했다: {WorldPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
        return asset.GetComponent<DrawingToolComposite>();
    }

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static Material LoadMaterial(string guid)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
        if (material == null) throw new Exception($"머티리얼을 못 찾았다: {guid}");
        return material;
    }

    // ── UI 프리팹 ────────────────────────────────

    static void RewireUiPrefab(DrawingToolComposite worldPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);

        try
        {
            if (root.transform.Find("DrawingToolManagers") != null)
                throw new Exception("DrawingToolManagers 가 아직 남아 있다");

            var ui = root.GetComponent<DrawingToolSceneUI>();
            if (ui == null) throw new Exception("루트에 DrawingToolSceneUI 가 없다");

            // 세 컴포넌트는 월드 오브젝트에 얹혀 있다가
            // 같이 지워졌다. 그리는 대상이 UI 라 UI 로 온다.
            InkGauge inkGauge = Ensure<InkGauge>(root, "InkGauge");
            Set(inkGauge, "_fill", Find<Image>(root, "InkBand/InkFrame/InkFill"));

            ResultBanner resultBanner = Ensure<ResultBanner>(root, "ResultBanner");
            Set(resultBanner, "_banner", Find(root, "CanvasArea/Banner").gameObject);
            Set(resultBanner, "_label", Find<TMP_Text>(root, "CanvasArea/Banner/Text (TMP)"));

            TutorialViewer tutorial = Ensure<TutorialViewer>(root, "TutorialViewer");
            WireTutorialAnchors(root, tutorial);

            Set(ui, "canvasArea", Find(root, "CanvasArea"));
            Set(ui, "worldPrefab", worldPrefab);

            Set(ui, "drawPanel", Find(root, "BottomBand/DrawPanel").gameObject);
            Set(ui, "simPanel", Find(root, "BottomBand/SimPanel").gameObject);
            Set(ui, "play", Find(root, "TopBar/TopRightSlot/Btn_Play").gameObject);
            Set(ui, "pauseResume", Find(root, "TopBar/TopRightSlot/Btn_PauseResume").gameObject);
            Set(ui, "speed", Find(root, "TopBar/Btn_Speed").gameObject);

            Set(ui, "toolbar", Find<ToolbarView>(root, "BottomBand/DrawPanel/ToolGroup"));
            Set(ui, "speedToggle", Find<SpeedToggle>(root, "TopBar/Btn_Speed"));
            Set(ui, "inkGauge", inkGauge);
            Set(ui, "resultBanner", resultBanner);
            Set(ui, "tutorial", tutorial);

            foreach ((string path, string method) in ButtonCalls) Wire(root, ui, path, method);

            PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath, out bool saved);
            if (!saved) throw new Exception($"UI 프리팹을 저장하지 못했다: {UiPrefabPath}");

            Debug.Log($"[이관] UI 배선을 채우고 버튼 {ButtonCalls.Length} 개를 다시 걸었다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>이미 있으면 그것을 쓴다. 다시 돌려도 안 겹친다.</summary>
    static T Ensure<T>(GameObject root, string name) where T : Component
    {
        var existing = root.GetComponentInChildren<T>(true);
        if (existing != null) return existing;

        Transform found = root.transform.Find(name);
        GameObject host = found != null
            ? found.gameObject
            : new GameObject(name, typeof(RectTransform));

        if (found == null) host.transform.SetParent(root.transform, false);
        return host.AddComponent<T>();
    }

    static void WireTutorialAnchors(GameObject root, TutorialViewer tutorial)
    {
        var serialized = new SerializedObject(tutorial);
        SerializedProperty list = serialized.FindProperty("targets");
        if (list == null) throw new Exception("TutorialViewer.targets 를 못 찾았다");

        list.arraySize = TutorialAnchors.Length;

        for (int i = 0; i < TutorialAnchors.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Anchor").enumValueIndex = (int)TutorialAnchors[i].Anchor;
            element.FindPropertyRelative("Rect").objectReferenceValue =
                Find(root, TutorialAnchors[i].Path);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 인스펙터 배선을 통째로 갈아 끼운다. 남겨 두면 지운
    /// 오브젝트를 가리키는 죽은 호출이 남는다.
    /// </summary>
    static void Wire(GameObject root, DrawingToolSceneUI ui, string path, string method)
    {
        var button = Find<Button>(root, path);

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        var call = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), ui, method);
        UnityEventTools.AddPersistentListener(button.onClick, call);
    }

    // ── 레거시 씬 ────────────────────────────────

    /// <summary>
    /// PlayMode 테스트가 이 씬을 띄워 코어를 찾는다.
    /// UI 프리팹이 더는 월드를 품지 않으니 월드를 직접 둔다.
    /// </summary>
    static void RewireLegacyScene(DrawingToolComposite worldPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Single);

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.GetComponentInChildren<DrawingToolComposite>(true) == null) continue;

            Debug.Log("[이관] 레거시 씬에 이미 조립자가 있다. 건너뛴다.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(worldPrefab.gameObject, scene);
        instance.name = "DrawingToolComposite";

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new Exception($"레거시 씬을 저장하지 못했다: {LegacyScenePath}");

        Debug.Log("[이관] 레거시 씬에 조립자를 놓았다.");
    }

    // ── 뒷정리 ──────────────────────────────────

    /// <summary>
    /// 드라이버가 MonoBehaviour 를 벗어 이 오브젝트는 담을
    /// 것이 없다. 이제 러너가 직접 들고 돌린다.
    /// </summary>
    [MenuItem("Tools/드로잉툴/맵에디터 드라이버 정리")]
    public static void CleanMapEditorDriver()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MapSimPrefabPath);

        try
        {
            Transform host = root.transform.Find("SimDriver ");
            if (host == null)
            {
                Debug.Log("[이관] 맵에디터에 뗄 드라이버 오브젝트가 없다.");
                return;
            }

            UnityEngine.Object.DestroyImmediate(host.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, MapSimPrefabPath, out bool saved);
            if (!saved) throw new Exception($"저장하지 못했다: {MapSimPrefabPath}");

            Debug.Log("[이관] 맵에디터에서 빈 드라이버 오브젝트를 뗐다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// MonoBehaviour 를 벗은 스크립트가 남긴 빈 컴포넌트를
    /// 턴다. 두면 프리팹을 열 때마다 오류가 뜬다.
    /// </summary>
    static void StripMissingScripts(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            int removed = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

            if (removed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[이관] {path}: 끊어진 컴포넌트 {removed} 개를 뗐다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── 공통 ────────────────────────────────────

    static Transform Find(GameObject root, string path)
    {
        Transform found = root.transform.Find(path);
        if (found == null) throw new Exception($"경로를 못 찾았다: {path}");
        return found;
    }

    static T Find<T>(GameObject root, string path) where T : Component
    {
        var component = Find(root, path).GetComponent<T>();
        if (component == null) throw new Exception($"{path} 에 {typeof(T).Name} 이 없다");
        return component;
    }

    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
            throw new Exception($"{target.GetType().Name}.{field} 필드를 못 찾았다");

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void RequireWired(UnityEngine.Object target, params string[] fields)
    {
        var serialized = new SerializedObject(target);

        foreach (string field in fields)
        {
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null || property.objectReferenceValue == null)
                throw new Exception($"{target.GetType().Name}.{field} 이 안 물려 있다");
        }
    }
}
