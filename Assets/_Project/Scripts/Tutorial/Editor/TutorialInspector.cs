using PPS.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 컷의 손가락 자리를 씬 뷰에서 끌어 잡는다. Offset·Drag
/// 를 숫자로 넣으면 캔버스 어디에 얹히는지 플레이 전에는
/// 알 수 없어서, 앵커 위에 그대로 띄워 보여 준다.
/// </summary>
[CustomEditor(typeof(Tutorial))]
public sealed class TutorialInspector : Editor
{
    const float HandleRatio = 0.08f;

    void OnEnable() => SceneView.duringSceneGui += Draw;
    void OnDisable() => SceneView.duringSceneGui -= Draw;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Anchor() == null)
            EditorGUILayout.HelpBox(
                "앵커를 못 찾았다. DrawingToolSceneUI 프리팹을 열면 " +
                "씬 뷰에서 손가락 자리를 끌어 잡을 수 있다.",
                MessageType.Info);
    }

    /// 이 컷이 붙을 자리. 뷰어가 없으면 잡을 기준도 없다.
    RectTransform Anchor()
    {
        TutorialViewer viewer = Viewer();
        return viewer == null ? null : viewer.Find(((Tutorial)target).Target);
    }

    /// 프리팹 스테이지가 열려 있으면 그 안을 먼저 본다.
    /// 씬에 같은 UI 가 또 있으면 엉뚱한 쪽을 잡는다.
    static TutorialViewer Viewer()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
            return stage.prefabContentsRoot.GetComponentInChildren<TutorialViewer>(true);

        return FindFirstObjectByType<TutorialViewer>(FindObjectsInactive.Include);
    }

    void Draw(SceneView view)
    {
        if (target == null) return;

        var cut = (Tutorial)target;
        RectTransform anchor = Anchor();
        if (anchor == null) return;

        // 프리팹은 앵커 한가운데를 기준으로 붙는다
        // (AnchorMin/Max 0.5). 핸들도 같은 데서 재야 한다.
        Vector2 center = anchor.rect.center;
        Vector3 start = anchor.TransformPoint(center + cut.Offset);
        Vector3 end = anchor.TransformPoint(center + cut.Offset + cut.Drag);

        // 끄는 연출이 없는 컷은 끝점이 뜻이 없다.
        bool drags = cut.Prefab != null
            && cut.Prefab.GetComponent<TutorialGesture>() != null;

        if (drags)
        {
            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, start, end);
        }

        EditorGUI.BeginChangeCheck();

        Handles.color = Color.green;
        Vector3 movedStart = Handles.FreeMoveHandle(
            start, HandleUtility.GetHandleSize(start) * HandleRatio,
            Vector3.zero, Handles.SphereHandleCap);

        Vector3 movedEnd = end;
        if (drags)
        {
            Handles.color = Color.cyan;
            movedEnd = Handles.FreeMoveHandle(
                end, HandleUtility.GetHandleSize(end) * HandleRatio,
                Vector3.zero, Handles.SphereHandleCap);
        }

        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(cut, "튜토리얼 제스처 자리");

        Vector2 offset = Local(anchor, movedStart) - center;

        // 두 점은 따로 논다. 시작점을 옮겨도 끝점은
        // 제자리에 남아야 획을 한쪽씩 다듬을 수 있다.
        if (drags) cut.Drag = Round(Local(anchor, movedEnd) - center - offset);
        cut.Offset = Round(offset);

        EditorUtility.SetDirty(cut);
    }

    static Vector2 Local(RectTransform rect, Vector3 world)
        => rect.InverseTransformPoint(world);

    /// UI 자리는 픽셀 단위다. 소수점이 붙으면 에셋
    /// 디프만 지저분해지고 화면에서는 안 갈린다.
    static Vector2 Round(Vector2 v)
        => new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
}
