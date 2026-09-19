using System;
using System.Collections.Generic;
using System.Globalization;
using PPS.Core;
using PPS.DrawingTool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    /// 선택한 장치의 필드로 속성 패널을 구성한다.
    /// 맵과 패널이 겹치지 않도록 카메라 영역도 나눈다.
    public sealed class MapDeviceInspector : MonoBehaviour
    {
        MapEditorVisuals _visuals;
        readonly List<Action> _refresh = new List<Action>();
        readonly Dictionary<DeviceEditKind, Button> _tools = new Dictionary<DeviceEditKind, Button>();
        MapEditHandles _handles;
        CanvasCameraFitter _fitter;
        RectTransform _area;
        RectTransform _viewport;
        RectTransform _panel;
        RectTransform _body;
        RectTransform _content;
        TMP_FontAsset _font;
        TMP_Text _title;
        TMP_Text _collapseText;
        TMP_Text _transformText;
        Button _decrease;
        Button _increase;
        bool _selectionReady;
        IDeviceData _shown;
        bool _collapsed;

        public bool ContainsScreenPoint(Vector2 point) => _panel != null && _panel.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(_panel, point,
                _panel.GetComponentInParent<Canvas>().worldCamera);

        public void Initialize(MapEditHandles handles, CanvasCameraFitter fitter, ToolPalette palette, RectTransform area, MapEditorVisuals visuals)
        {
            _handles = handles;
            _visuals = visuals;
            _fitter = fitter;
            _area = area;
            _font = palette.GetComponentInParent<Canvas>().GetComponentInChildren<TMP_Text>(true)?.font;
            _viewport = Rect("DeviceEditViewport", _area);
            _panel = Rect("DeviceInspector", _area);
            Skin(_panel.gameObject.AddComponent<Image>(), _visuals.Panel);
            var layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 4;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var header = Row(_panel, 48);
            _title = Text(header, "Device properties", 36);
            Flexible(_title.gameObject);
            var collapse = Button(header, "Hide", () => _collapsed = !_collapsed, 104);
            _collapseText = collapse.GetComponentInChildren<TMP_Text>();

            _body = Rect("Body", _panel);
            Flexible(_body.gameObject);
            var bodyLayout = _body.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 4;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandHeight = false;
            var toolbar = Row(_body, 60);
            AddTool(toolbar, DeviceEditKind.Position, "Move");
            AddTool(toolbar, DeviceEditKind.Angle, "Rotate");
            AddTool(toolbar, DeviceEditKind.Radius, "Resize");
            var transformRow = Row(_body, 48);
            _transformText = Text(transformRow, "", 30);
            Flexible(_transformText.gameObject);
            _decrease = Button(transformRow, "−", () => NudgeTransform(-1), 88);
            _increase = Button(transformRow, "+", () => NudgeTransform(1), 88);

            var scrollRect = Rect("Properties", _body);
            Flexible(scrollRect.gameObject);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;
            var clip = Rect("Viewport", scrollRect);
            Skin(clip.gameObject.AddComponent<Image>(), _visuals.Panel);
            clip.gameObject.AddComponent<RectMask2D>();
            _content = Rect("Content", clip);
            _content.anchorMin = new Vector2(0, 1);
            _content.pivot = new Vector2(0.5f, 1);
            var contentLayout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.padding = new RectOffset(0, 12, 0, 0);
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = clip;
            scroll.content = _content;
            var bar = Rect("Scrollbar", scrollRect);
            bar.anchorMin = new Vector2(1, 0);
            bar.offsetMin = new Vector2(-8, 0);
            var thumb = Rect("Thumb", bar);
            var thumbImage = thumb.gameObject.AddComponent<Image>();
            Skin(thumbImage, _visuals.Scrollbar);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = thumb;
            scrollbar.targetGraphic = thumbImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            _fitter.SetCanvasArea(_viewport);
            Rebuild(null);
            Layout();
        }

        void OnEnable()
        {
            if (_panel == null) return;
            _panel.gameObject.SetActive(true);
            _fitter.SetCanvasArea(_viewport);
        }

        void OnDisable()
        {
            if (_panel == null) return;
            _panel.gameObject.SetActive(false);
            if (_fitter != null) _fitter.SetCanvasArea(_area);
        }

        void OnDestroy()
        {
            if (_panel != null) Destroy(_panel.gameObject);
            if (_viewport != null) Destroy(_viewport.gameObject);
        }

        void LateUpdate()
        {
            if (_panel == null) return;
            var device = _handles.SelectedDevice;
            if (device == null) _selectionReady = false;
            else if (!_handles.IsDragging) _selectionReady = true;
            if (!ReferenceEquals(device, _shown)) Rebuild(device);
            foreach (var refresh in _refresh) refresh();
            foreach (var tool in _tools)
            {
                tool.Value.interactable = device != null
                    && DeviceParameterSchema.For(device).Find(tool.Key) != null;
                ((Image)tool.Value.targetGraphic).sprite = _handles.DeviceTool == tool.Key
                    ? _visuals.SelectedButton : _visuals.Button;
                tool.Value.GetComponentInChildren<TMP_Text>().color =
                    _handles.DeviceTool == tool.Key ? _visuals.SelectedText : _visuals.Text;
                tool.Value.transform.Find("Icon").GetComponent<Image>().color =
                    _handles.DeviceTool == tool.Key ? _visuals.SelectedText : _visuals.Text;
            }
            var spatial = device == null ? null : DeviceParameterSchema.For(device).Find(_handles.DeviceTool);
            _transformText.text = device == null ? "Select a device" : spatial == null ? ""
                : spatial.Kind == DeviceEditKind.Position ? $"X {device.Position.x:0.##}    Y {device.Position.y:0.##}   ·   Drag to move"
                : $"{spatial.Label}: {Convert.ToSingle(spatial.Read(device)):0.##} {spatial.Unit}   ·   Drag ring";
            bool canNudge = spatial != null && spatial.Kind != DeviceEditKind.Position;
            _decrease.gameObject.SetActive(canNudge);
            _increase.gameObject.SetActive(canNudge);
            Layout();
        }

        void NudgeTransform(int direction)
        {
            var device = _handles.SelectedDevice;
            if (device == null) return;
            var parameter = DeviceParameterSchema.For(device).Find(_handles.DeviceTool);
            if (parameter == null || parameter.Kind == DeviceEditKind.Position) return;
            float value = (float)parameter.Read(device);
            value = parameter.Kind == DeviceEditKind.Angle ? Mathf.Repeat(value + direction * 15, 360)
                : parameter.Constrain(value * (direction < 0 ? 0.9f : 1.1f));
            _handles.SetDeviceParameter(device, parameter, value.ToString(CultureInfo.InvariantCulture), out _);
        }

        void Layout()
        {
            bool collapsed = _collapsed || !_selectionReady;
            _body.gameObject.SetActive(!collapsed);
            _collapseText.text = collapsed ? "Show" : "Hide";
            _collapseText.transform.parent.gameObject.SetActive(_shown != null);
            float height = 184 + LayoutUtility.GetPreferredHeight(_content);
            float size = collapsed ? 64 : Mathf.Min(height, Mathf.Min(560, _area.rect.height * 0.46f));
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = new Vector2(1, 0);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = new Vector2(0, size);
            _viewport.offsetMin = new Vector2(0, size);
            _viewport.offsetMax = Vector2.zero;
        }

        void Rebuild(IDeviceData device)
        {
            _shown = device;
            _refresh.Clear();
            foreach (Transform child in _content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _content.anchoredPosition = Vector2.zero;
            _title.text = device == null ? "Device properties" : $"{device.Type}";
            if (device == null) return;
            _collapsed = false;
            var defaults = (IDeviceData)Activator.CreateInstance(device.GetType());
            foreach (var parameter in DeviceParameterSchema.For(device).Parameters)
                if (parameter.Kind == DeviceEditKind.Value) AddParameter(device, defaults, parameter);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        void AddParameter(IDeviceData device, IDeviceData defaults, DeviceParameter parameter)
        {
            var group = Rect(parameter.Name, _content);
            var layout = group.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var row = Row(group, 88);
            var label = Text(row, parameter.Label, 34);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 290;
            var help = Text(group, "", 28);
            help.color = _visuals.MutedText;
            Height(help.gameObject, 34);
            help.gameObject.SetActive(false);
            if (!parameter.CanEdit)
            {
                Text(row, "Custom tool needed", 24);
                return;
            }
            if (parameter.ValueType == typeof(bool) || parameter.ValueType.IsEnum)
            {
                var choice = Button(row, "", () =>
                {
                    object current = parameter.Read(device);
                    string next;
                    if (current is bool flag) next = (!flag).ToString();
                    else
                    {
                        var values = Enum.GetValues(parameter.ValueType);
                        next = values.GetValue((Array.IndexOf(values, current) + 1) % values.Length).ToString();
                    }
                    _handles.SetDeviceParameter(device, parameter, next, out _);
                });
                _refresh.Add(() => choice.GetComponentInChildren<TMP_Text>().text = parameter.Format(device));
                return;
            }
            TMP_Text valueText = Value(row);
            valueText.text = parameter.Format(device);
            string error = "";
            void Commit(string value)
            {
                if (!ReferenceEquals(device, _handles.SelectedDevice)) return;
                if (_handles.SetDeviceParameter(device, parameter, value, out error))
                    valueText.text = parameter.Format(device);
            }
            if (parameter.IsNumber)
            {
                var numeric = valueText.transform.parent.gameObject.AddComponent<MapNumericDrag>();
                double original = 0;
                bool recorded = false;
                numeric.BeginScrub = () =>
                {
                    original = Convert.ToDouble(parameter.Read(device));
                    recorded = false;
                };
                numeric.Scrub = distance =>
                {
                    double step = parameter.ValueType == typeof(int) ? 1 : 0.1;
                    float next = parameter.Constrain((float)(original + Math.Round(distance / 8) * step));
                    string value = next.ToString(parameter.ValueType == typeof(int) ? "0" : "0.########", CultureInfo.InvariantCulture);
                    if (value == parameter.Format(device)) return;
                    if (_handles.SetDeviceParameter(device, parameter, value, out error, !recorded))
                    {
                        recorded = true;
                        valueText.text = parameter.Format(device);
                    }
                };
                void Step(int direction)
                {
                    double step = parameter.ValueType == typeof(int) ? 1 : 0.1;
                    double next = Convert.ToDouble(parameter.Read(device)) + direction * step;
                    Commit(next.ToString(parameter.ValueType == typeof(int) ? "0" : "0.########", CultureInfo.InvariantCulture));
                }
                Button(row, "−", () => Step(-1), 88);
                Button(row, "+", () => Step(1), 88);
            }
            Button(row, "Reset", () => Commit(parameter.Format(defaults)), 112);
            _refresh.Add(() =>
            {
                valueText.text = parameter.Format(device);
                string unit = parameter.Unit == "steps"
                    ? $"steps · {Convert.ToDouble(parameter.Read(device)) * SimWorld.FixedDt:0.###} s" : parameter.Unit;
                label.text = parameter.Label + (unit.Length > 0 ? $"\n<size=27><color=#{ColorUtility.ToHtmlStringRGB(_visuals.MutedText)}>{unit}</color></size>" : "");
                help.text = error;
                help.color = error.Length > 0 ? _visuals.ErrorText : _visuals.MutedText;
                help.gameObject.SetActive(help.text.Length > 0);
            });
        }

        void AddTool(RectTransform parent, DeviceEditKind kind, string label)
        {
            var button = Button(parent, label, () => _handles.SelectDeviceTool(kind));
            _tools.Add(kind, button);
            var icon = Rect("Icon", button.transform);
            icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
            icon.sizeDelta = new Vector2(36, 36);
            icon.anchoredPosition = new Vector2(28, 0);
            var image = icon.gameObject.AddComponent<Image>();
            image.sprite = kind == DeviceEditKind.Position ? _visuals.MoveIcon
                : kind == DeviceEditKind.Angle ? _visuals.RotateIcon : _visuals.ResizeIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            button.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new Vector2(48, 0);
        }

        static void Skin(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        TMP_Text Value(Transform parent)
        {
            var root = Rect("Value", parent);
            Flexible(root.gameObject);
            Skin(root.gameObject.AddComponent<Image>(), _visuals.ValueBackground);
            var text = Text(root, "", 36);
            text.rectTransform.offsetMin = new Vector2(10, 3);
            text.rectTransform.offsetMax = new Vector2(-10, -3);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        Button Button(Transform parent, string label, UnityEngine.Events.UnityAction action, float width = 0)
        {
            var rect = Rect(label, parent);
            var image = rect.gameObject.AddComponent<Image>();
            Skin(image, _visuals.Button);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var text = Text(rect, label, 34);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 28;
            text.fontSizeMax = 34;
            if (width > 0) rect.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            else Flexible(rect.gameObject);
            return button;
        }

        TMP_Text Text(Transform parent, string value, float size)
        {
            var rect = Rect("Label", parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = _visuals.Text;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        static RectTransform Row(Transform parent, float height)
        {
            var rect = Rect("Row", parent);
            Height(rect.gameObject, height);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return rect;
        }

        static void Height(GameObject go, float height)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }
        static void Flexible(GameObject go)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1;
            layout.flexibleHeight = 1;
        }
    }
}
