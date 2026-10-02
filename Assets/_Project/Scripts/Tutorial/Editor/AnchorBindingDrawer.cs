using UnityEditor;
using UnityEngine;

/// <summary>
/// 앵커와 RectTransform 을 한 줄에 나란히 그린다.
/// 기본 폴드아웃은 항목마다 펼쳐야 해서 갈아끼운다.
/// </summary>
[CustomPropertyDrawer(typeof(TutorialViewer.AnchorBinding))]
public class AnchorBindingDrawer : PropertyDrawer
{
    const float Gap = 4f;
    const float AnchorRatio = 0.4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 요소 라벨(Element 0)은 자리만 먹어서 지운다.
        position = EditorGUI.PrefixLabel(position, GUIContent.none);

        var anchorWidth = (position.width - Gap) * AnchorRatio;
        var anchorRect = new Rect(position.x, position.y, anchorWidth, position.height);
        var rectRect = new Rect(
            position.x + anchorWidth + Gap, position.y,
            position.width - anchorWidth - Gap, position.height);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        EditorGUI.PropertyField(anchorRect, property.FindPropertyRelative("Anchor"), GUIContent.none);
        EditorGUI.PropertyField(rectRect, property.FindPropertyRelative("Rect"), GUIContent.none);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}
