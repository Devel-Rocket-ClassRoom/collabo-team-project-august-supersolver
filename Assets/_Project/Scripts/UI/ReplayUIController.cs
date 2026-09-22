using System.Collections.Generic;
using System.IO;
using PPS.Core;
using PPS.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PPS.UI
{
    // Replay 씬의 조작부를 IMGUI로 표시한다.
    public class ReplayUIController : MonoBehaviour
    {
        const float ReferenceWidth = 750f;
        const float ReferenceHeight = 1334f;
        const float ListItemHeight = 62f;

        [SerializeField] SimWorldRenderer _renderer;

        readonly List<ReplayEntry> _entries = new List<ReplayEntry>();

        bool _isListOpen;
        bool _timelineDragging;
        bool _listDragging;
        float _listDragDistance;
        Vector2 _listDragOrigin;
        Vector2 _listScrollOrigin;
        Vector2 _listScroll;
        string _selectedReplayName = "Current Replay";

        GUIStyle _panelStyle;
        GUIStyle _buttonStyle;
        GUIStyle _selectedButtonStyle;
        GUIStyle _labelStyle;
        GUIStyle _statusStyle;
        GUIStyle _listButtonStyle;

        Texture2D _panelTexture;
        Texture2D _buttonTexture;
        Texture2D _buttonHoverTexture;
        Texture2D _selectedTexture;

        sealed class ReplayEntry
        {
            public ReplayData Data;
            public string DisplayName;
        }

        void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;

            ReplayListUI[] legacyLists =
                GetComponentsInChildren<ReplayListUI>(true);

            for (int i = 0; i < legacyLists.Length; i++)
                legacyLists[i].enabled = false;

            // 저장되지 않은 이전 씬으로 실행해도
            // 기존 uGUI가 IMGUI 위에 남지 않게 한다.
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            RefreshReplayFiles();
        }

        void Update()
        {
            if (!TryReadPointer(
                    out Vector2 screenPosition,
                    out bool pressed,
                    out bool held,
                    out bool released))
            {
                return;
            }

            float scale = GetScale();
            if (scale <= 0f)
                return;

            Vector2 guiPosition = new Vector2(
                screenPosition.x / scale,
                (Screen.height - screenPosition.y) / scale);

            float width = Screen.width / scale;
            float height = Screen.height / scale;
            ProcessPointer(
                guiPosition, pressed, held, released,
                width, height);
        }

        void ProcessPointer(
            Vector2 position,
            bool pressed,
            bool held,
            bool released,
            float width,
            float height)
        {
            Rect timeline = GetTimelineTrack(width, height);

            if (pressed && timeline.Contains(position) &&
                _renderer != null && _renderer.HasReplay)
            {
                _timelineDragging = true;
                SetTimelineFromPointer(position.x, timeline);
            }

            if (_timelineDragging)
            {
                if (held)
                    SetTimelineFromPointer(position.x, timeline);

                if (released)
                    _timelineDragging = false;

                return;
            }

            if (_isListOpen)
            {
                Rect listView = GetListView(width, height);

                if (pressed && listView.Contains(position))
                {
                    _listDragging = true;
                    _listDragDistance = 0f;
                    _listDragOrigin = position;
                    _listScrollOrigin = _listScroll;
                }

                if (_listDragging)
                {
                    if (held)
                    {
                        float movement = position.y - _listDragOrigin.y;
                        _listDragDistance = Mathf.Max(
                            _listDragDistance, Mathf.Abs(movement));

                        float maximumScroll = Mathf.Max(
                            0f,
                            _entries.Count * ListItemHeight -
                            listView.height);

                        _listScroll.y = Mathf.Clamp(
                            _listScrollOrigin.y - movement,
                            0f, maximumScroll);
                    }

                    if (released)
                    {
                        if (_listDragDistance < 12f &&
                            listView.Contains(position))
                        {
                            int index = Mathf.FloorToInt(
                                (position.y - listView.y +
                                 _listScroll.y) / ListItemHeight);

                            if (index >= 0 && index < _entries.Count)
                            {
                                SelectReplay(_entries[index]);
                                _isListOpen = false;
                            }
                        }

                        _listDragging = false;
                    }

                    return;
                }
            }

            if (!released)
                return;

            GetToolbarButtons(
                width,
                out Rect listButton,
                out Rect playButton,
                out Rect restartButton);

            if (listButton.Contains(position))
            {
                ToggleReplayList();
                return;
            }

            if (_renderer != null && _renderer.HasReplay)
            {
                if (playButton.Contains(position))
                {
                    TogglePlayback();
                    return;
                }

                if (restartButton.Contains(position))
                {
                    _renderer.Restart();
                    return;
                }
            }

            if (_isListOpen &&
                GetRefreshButton(width, height).Contains(position))
            {
                RefreshReplayFiles();
            }
        }

        static bool TryReadPointer(
            out Vector2 position,
            out bool pressed,
            out bool held,
            out bool released)
        {
            position = default;
            pressed = false;
            held = false;
            released = false;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                bool touchPressed = touch.press.wasPressedThisFrame;
                bool touchHeld = touch.press.isPressed;
                bool touchReleased = touch.press.wasReleasedThisFrame;

                if (touchPressed || touchHeld || touchReleased)
                {
                    position = touch.position.ReadValue();
                    pressed = touchPressed;
                    held = touchHeld;
                    released = touchReleased;
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;

            pressed = mouse.leftButton.wasPressedThisFrame;
            held = mouse.leftButton.isPressed;
            released = mouse.leftButton.wasReleasedThisFrame;

            if (!pressed && !held && !released)
                return false;

            position = mouse.position.ReadValue();
            return true;
        }

        public void ToggleReplayList()
        {
            _isListOpen = !_isListOpen;

            if (_isListOpen)
                RefreshReplayFiles();
        }

        public void TogglePlayback()
        {
            if (_renderer == null || !_renderer.HasReplay)
                return;

            if (_renderer.IsPlaying)
                _renderer.Pause();
            else
                _renderer.Play();
        }

        public void SetTimelineValue(float value)
        {
            _renderer?.SetTargetStep(Mathf.RoundToInt(value));
        }

        void SetTimelineFromPointer(float pointerX, Rect timeline)
        {
            float normalized = Mathf.InverseLerp(
                timeline.xMin, timeline.xMax, pointerX);

            _renderer.SetTargetStep(Mathf.RoundToInt(
                normalized * _renderer.MaximumStep));
        }

        void OnGUI()
        {
            EnsureStyles();

            int previousDepth = GUI.depth;
            GUI.depth = -1000;

            float scale = GetScale();
            if (scale <= 0f)
            {
                GUI.depth = previousDepth;
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(
                new Vector3(scale, scale, 1f));

            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawToolbar(width);
            DrawReplayStatus(width);
            DrawTimeline(width, height);

            if (_isListOpen)
                DrawReplayList(width, height);

            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
        }

        void DrawToolbar(float width)
        {
            Rect toolbar = GetToolbar(width);
            GUI.Box(toolbar, GUIContent.none, _panelStyle);

            GetToolbarButtons(
                width,
                out Rect listButton,
                out Rect playButton,
                out Rect restartButton);

            string listLabel = _isListOpen
                ? $"Replay List ({_entries.Count})  Close"
                : $"Replay List ({_entries.Count})";
            GUI.Box(listButton, listLabel, _buttonStyle);

            bool hasReplay = _renderer != null && _renderer.HasReplay;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = hasReplay;

            string playLabel = _renderer != null && _renderer.IsPlaying
                ? "Pause"
                : "Start";

            GUI.Box(playButton, playLabel, _buttonStyle);
            GUI.Box(restartButton, "Restart", _buttonStyle);
            GUI.enabled = previousEnabled;
        }

        void DrawReplayStatus(float width)
        {
            Rect statusPanel = new Rect(20f, 122f, width - 40f, 92f);
            GUI.Box(statusPanel, GUIContent.none, _panelStyle);

            if (_renderer == null || !_renderer.HasReplay)
            {
                GUI.Label(
                    new Rect(statusPanel.x + 18f, statusPanel.y + 24f,
                        statusPanel.width - 36f, 44f),
                    "Select a replay from Replay List.",
                    _statusStyle);
                return;
            }

            GUI.Label(
                new Rect(statusPanel.x + 18f, statusPanel.y + 10f,
                    statusPanel.width - 36f, 34f),
                $"{_selectedReplayName}    " +
                $"Step {_renderer.CurrentStep} / {_renderer.MaximumStep}",
                _labelStyle);

            GUI.Label(
                new Rect(statusPanel.x + 18f, statusPanel.y + 48f,
                    statusPanel.width - 36f, 30f),
                $"World Hash  0x{_renderer.CurrentHash:X16}",
                _statusStyle);
        }

        void DrawTimeline(float width, float height)
        {
            Rect timelinePanel = new Rect(
                20f, height - 116f, width - 40f, 96f);
            GUI.Box(timelinePanel, GUIContent.none, _panelStyle);

            bool hasReplay = _renderer != null && _renderer.HasReplay;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = hasReplay;

            int currentTarget = hasReplay ? _renderer.TargetStep : 0;
            int maximumStep = hasReplay ? _renderer.MaximumStep : 1;

            GUI.Label(
                new Rect(timelinePanel.x + 18f, timelinePanel.y + 8f,
                    timelinePanel.width - 36f, 30f),
                hasReplay
                    ? $"Timeline    {currentTarget} / {maximumStep}"
                    : "Timeline",
                _labelStyle);

            GUI.HorizontalSlider(
                GetTimelineTrack(width, height),
                currentTarget, 0f, maximumStep);

            GUI.enabled = previousEnabled;
        }

        void DrawReplayList(float width, float height)
        {
            Rect panel = GetListPanel(width, height);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 12f,
                    panel.width - 120f, 38f),
                $"Saved Replays  ({_entries.Count})",
                _labelStyle);

            GUI.Box(
                GetRefreshButton(width, height),
                "Refresh", _buttonStyle);

            Rect viewRect = GetListView(width, height);
            Rect contentRect = new Rect(
                0f, 0f, viewRect.width - 20f,
                Mathf.Max(viewRect.height,
                    _entries.Count * ListItemHeight));

            _listScroll = GUI.BeginScrollView(
                viewRect, _listScroll, contentRect);

            if (_entries.Count == 0)
            {
                GUI.Label(
                    new Rect(10f, 12f, contentRect.width - 20f, 40f),
                    "No replay files found.", _statusStyle);
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                ReplayEntry entry = _entries[i];
                Rect itemRect = new Rect(
                    4f, i * ListItemHeight + 4f,
                    contentRect.width - 8f, 54f);

                GUIStyle style = entry.DisplayName == _selectedReplayName
                    ? _selectedButtonStyle
                    : _listButtonStyle;

                GUI.Box(itemRect, entry.DisplayName, style);
            }

            GUI.EndScrollView();
        }

        static Rect GetToolbar(float width)
        {
            return new Rect(20f, 20f, width - 40f, 92f);
        }

        static void GetToolbarButtons(
            float width,
            out Rect listButton,
            out Rect playButton,
            out Rect restartButton)
        {
            Rect toolbar = GetToolbar(width);
            const float gap = 8f;
            float buttonWidth =
                (toolbar.width - 32f - gap * 2f) / 3f;

            listButton = new Rect(
                toolbar.x + 16f, toolbar.y + 14f,
                buttonWidth, 64f);
            playButton = new Rect(
                listButton.xMax + gap, listButton.y,
                buttonWidth, listButton.height);
            restartButton = new Rect(
                playButton.xMax + gap, listButton.y,
                buttonWidth, listButton.height);
        }

        Rect GetListPanel(float width, float height)
        {
            float availableHeight = Mathf.Max(180f, height - 360f);
            float panelHeight = Mathf.Min(
                Mathf.Max(220f,
                    82f + _entries.Count * ListItemHeight),
                Mathf.Min(480f, availableHeight));

            return new Rect(20f, 118f, width - 40f, panelHeight);
        }

        Rect GetListView(float width, float height)
        {
            Rect panel = GetListPanel(width, height);
            return new Rect(
                panel.x + 12f, panel.y + 58f,
                panel.width - 24f, panel.height - 70f);
        }

        Rect GetRefreshButton(float width, float height)
        {
            Rect panel = GetListPanel(width, height);
            return new Rect(
                panel.xMax - 104f, panel.y + 9f,
                86f, 42f);
        }

        static Rect GetTimelineTrack(float width, float height)
        {
            Rect timelinePanel = new Rect(
                20f, height - 116f, width - 40f, 96f);
            return new Rect(
                timelinePanel.x + 22f,
                timelinePanel.y + 48f,
                timelinePanel.width - 44f, 38f);
        }

        static float GetScale()
        {
            return Mathf.Min(
                Screen.width / ReferenceWidth,
                Screen.height / ReferenceHeight);
        }

        void SelectReplay(ReplayEntry entry)
        {
            if (_renderer == null || entry?.Data == null)
                return;

            ReplayData replay = entry.Data;
            if (replay.Stage == null || replay.Solution == null)
                return;

            _renderer.SetReplay(replay.Stage, replay.Solution);
            _selectedReplayName = entry.DisplayName;
        }

        void RefreshReplayFiles()
        {
            _entries.Clear();
            string[] filePaths = ReplayStorage.GetReplayFiles();

            for (int i = 0; i < filePaths.Length; i++)
            {
                string filePath = filePaths[i];
                if (!ReplayStorage.TryLoad(filePath, out ReplayData replay))
                    continue;

                string stageId = replay.Stage?.StageId ?? "Unknown Stage";
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                _entries.Add(new ReplayEntry
                {
                    Data = replay,
                    DisplayName = $"{stageId}  |  {fileName}",
                });
            }
        }

        void EnsureStyles()
        {
            if (_panelStyle != null)
                return;

            _panelTexture = CreateTexture(
                new Color(0.055f, 0.063f, 0.086f, 0.9f));
            _buttonTexture = CreateTexture(
                new Color(0.18f, 0.2f, 0.28f, 0.98f));
            _buttonHoverTexture = CreateTexture(
                new Color(0.28f, 0.31f, 0.43f, 1f));
            _selectedTexture = CreateTexture(
                new Color(0.31f, 0.24f, 0.62f, 1f));

            _panelStyle = new GUIStyle(GUI.skin.box);
            SetBackground(_panelStyle, _panelTexture);
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white },
            };
            _buttonStyle.normal.background = _buttonTexture;
            _buttonStyle.hover.background = _buttonHoverTexture;
            _buttonStyle.active.background = _selectedTexture;

            _listButtonStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(18, 12, 4, 4),
            };

            _selectedButtonStyle = new GUIStyle(_listButtonStyle);
            SetBackground(_selectedButtonStyle, _selectedTexture);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
            };

            _statusStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 19,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.75f, 0.8f, 0.9f) },
            };
        }

        static void SetBackground(GUIStyle style, Texture2D texture)
        {
            style.normal.background = texture;
            style.hover.background = texture;
            style.active.background = texture;
            style.focused.background = texture;
        }

        static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            Destroy(_panelTexture);
            Destroy(_buttonTexture);
            Destroy(_buttonHoverTexture);
            Destroy(_selectedTexture);
        }
    }
}
