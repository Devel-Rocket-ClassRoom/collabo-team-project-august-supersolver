using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    public sealed class MapEditToolbar : MonoBehaviour
    {
        MapEditorVisuals _visuals;
        readonly List<(Button button, ColorBlock colors, Func<bool> selected)> _modes = new();

        public void Initialize(MapEditHandles handles, MapEditorVisuals visuals)
        {
            _visuals = visuals;
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.onClick.GetPersistentEventCount() > 0 && button.targetGraphic is Image background)
                {
                    background.sprite = visuals.Button;
                    background.type = Image.Type.Sliced;
                    background.color = Color.white;
                }
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    var target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);
                    Func<bool> selected = null;
                    if (target == handles && method == nameof(MapEditHandles.ToggleEditMode))
                        selected = () => handles.EditingShape && !handles.EditingVertices;
                    else if (target == handles && method == nameof(MapEditHandles.ToggleInsertMode))
                        selected = () => handles.EditingVertices;
                    else if (target is MapEditorSimRunner runner && method == nameof(MapEditorSimRunner.Toggle))
                        selected = () => runner.Running;
                    if (selected == null) continue;
                    _modes.Add((button, button.colors, selected));
                    break;
                }
            }
        }

        void LateUpdate()
        {
            foreach (var mode in _modes)
            {
                if (mode.button == null) continue;
                ColorBlock colors = mode.colors;
                bool selected = mode.selected();
                if (mode.button.targetGraphic is Image background)
                    background.sprite = selected ? _visuals.SelectedButton : _visuals.Button;
                var label = mode.button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = selected ? _visuals.SelectedText : _visuals.Text;
                if (selected)
                {
                    colors.normalColor = Color.white;
                    colors.highlightedColor = Color.white;
                    colors.selectedColor = Color.white;
                    colors.pressedColor = Color.white * 0.85f;
                }
                else
                {
                    // 키보드 포커스가 편집 모드처럼 남지 않게 한다.
                    colors.selectedColor = colors.normalColor;
                }
                mode.button.colors = colors;
            }
        }

        void OnDisable()
        {
            foreach (var mode in _modes)
                if (mode.button != null) mode.button.colors = mode.colors;
        }
    }
}
