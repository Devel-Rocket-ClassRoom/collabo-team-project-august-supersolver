#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using System;

namespace PPS.Tools
{
    public class ReplayDebugPanel : MonoBehaviour
    {
        RectTransform _panel;

        Font _font;
        Text _status;
        Text _errorText;
        Button _saveButton;
        Button _exportButton;
        Button _replayButton;
        Button _returnButton;
        bool _isReplayMode;

        Button _toggleButton;
        Text _toggleLabel;
        bool _isExpanded;

        public event Action SaveRequested;
        public event Action ExportRequested;
        public event Action ReplayRequested;
        public event Action ReturnRequested;

        void Awake()
        {
            // 별도 폰트 에셋 없이 Windows 한글 폰트를 사용한다.
            _font = Font.CreateDynamicFontFromOSFont(
                "Malgun Gothic", 24);

            CreateCanvas();
            CreatePanel();
            CreateContents();

            CreateToggleButton();
            CreateErrorNotice();
            SetExpanded(false);
        }

        // 게임 화면 위에 디버그 UI를 표시한다.
        void CreateCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler =
                gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        // 버튼과 상태 문구가 들어갈 영역을 만든다.
        void CreatePanel()
        {
            GameObject panelObject = new GameObject(
                "ReplayDebugPanelBody",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            _panel = panelObject.GetComponent<RectTransform>();
            _panel.SetParent(transform, false);

            // 화면 왼쪽 위를 기준으로 배치한다.
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.anchoredPosition = new Vector2(20f, -380f);
            _panel.sizeDelta = new Vector2(680f, 460f);

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            background.raycastTarget = true;
        }
        // 이미지 에셋 없이 단색 버튼을 만든다.
        Button CreateButton(string label, float top)
        {
            GameObject buttonObject = new GameObject(
                label,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.SetParent(_panel, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(20f, -top - 90f);
            rect.offsetMax = new Vector2(-20f, -top);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.25f, 0.25f, 0.25f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            Text text = CreateText(rect, label, 44);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        // 문구가 버튼의 클릭 판정을 가리지 않게 한다.
        Text CreateText(Transform parent, string value, int size)
        {
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return text;
        }
        // 버튼 세 개와 상태 문구를 생성한다.
        void CreateContents()
        {
            _saveButton = CreateButton("저장", 18f);
            _exportButton = CreateButton("에셋으로 저장", 124f);
            _replayButton = CreateButton("리플레이로 이동", 230f);

            _saveButton.onClick.AddListener(
                () => SaveRequested?.Invoke());

            _exportButton.onClick.AddListener(
                () => ExportRequested?.Invoke());

            _replayButton.onClick.AddListener(
                () => ReplayRequested?.Invoke());

            _returnButton = CreateButton("돌아가기", 18f);

            _returnButton.onClick.AddListener(
                () => ReturnRequested?.Invoke());

            _returnButton.gameObject.SetActive(false);

            _status = CreateText(_panel, "캐시 없음", 32);

            RectTransform rect = _status.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(20f, 18f);
            rect.offsetMax = new Vector2(-20f, 114f);

            SetButtonsEnabled(false, false);
        }
        // 본문을 접어도 남아 있는 작은 버튼이다.
        void CreateToggleButton()
        {
            _toggleButton = CreateButton("디버그 열기", 0f);

            RectTransform rect =
                _toggleButton.GetComponent<RectTransform>();

            // 본문 밖으로 옮겨 본문과 따로 표시한다.
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -280f);
            rect.sizeDelta = new Vector2(320f, 90f);

            _toggleLabel =
                _toggleButton.GetComponentInChildren<Text>();

            _toggleLabel.fontSize = 36;

            _toggleButton.onClick.AddListener(() =>
            {
                SetExpanded(!_isExpanded);
            });

            // 돌아가기도 본문 밖의 작은 버튼으로 배치한다.
            RectTransform returnRect =
                _returnButton.GetComponent<RectTransform>();

            returnRect.SetParent(transform, false);
            returnRect.anchorMin = new Vector2(0f, 1f);
            returnRect.anchorMax = new Vector2(0f, 1f);
            returnRect.pivot = new Vector2(0f, 1f);
            returnRect.anchoredPosition = new Vector2(20f, -280f);
            returnRect.sizeDelta = new Vector2(320f, 90f);

            _returnButton.GetComponentInChildren<Text>(true).fontSize = 36;
        }
        // 본문과 별개로 오류를 표시한다.
        void CreateErrorNotice()
        {
            GameObject notice = new GameObject(
                "ErrorNotice",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform rect = notice.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            // 작은 버튼 아래, 펼친 본문과 같은 위치다.
            rect.anchoredPosition = new Vector2(20f, -380f);
            rect.sizeDelta = new Vector2(680f, 120f);

            Image background = notice.GetComponent<Image>();
            background.color = new Color(0.25f, 0.05f, 0.05f, 0.95f);
            background.raycastTarget = false;

            _errorText = CreateText(rect, string.Empty, 32);

            RectTransform textRect = _errorText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            notice.SetActive(false);
        }

        //패널 본문의 표시 여부를 바꾼다.
        public void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;

            _panel.gameObject.SetActive(
                !_isReplayMode && _isExpanded);

            _toggleLabel.text =
                _isExpanded ? "디버그 닫기" : "디버그 열기";

            if (expanded)
                _errorText.transform.parent.gameObject.SetActive(false);
        }

        // 저장 성공을 알리고 그리기 공간을 돌려준다.
        public void ShowCacheSaved()
        {
            SetExpanded(false);
            _toggleLabel.text = "저장 완료";
        }

        // 저장 결과 등을 패널에 표시한다.
        public void SetStatus(string message)
        {
            _status.text = message;
            _errorText.transform.parent.gameObject.SetActive(false);
        }
        // 오류는 본문이 접혀 있어도 보이게 한다.
        public void ShowError(string message)
        {
            _status.text = message;
            _errorText.text = message;

            SetExpanded(false);
            _errorText.transform.parent.gameObject.SetActive(true);
        }

        // 이동 기능까지 준비됐을 때만 이동 버튼을 허용한다.
        public void SetButtonsEnabled(
            bool canSave,
            bool hasReplay,
            bool canReplay = false)
        {
            _saveButton.interactable =
                !_isReplayMode && canSave;

            _exportButton.interactable =
                !_isReplayMode && hasReplay;

            _replayButton.interactable =
                !_isReplayMode && hasReplay && canReplay;
        }
        // 리플레이에서는 돌아가기 버튼만 표시한다.
        public void SetReplayMode(bool replayMode)
        {
            _isReplayMode = replayMode;

            _toggleButton.gameObject.SetActive(!replayMode);
            _returnButton.gameObject.SetActive(replayMode);

            SetExpanded(false);
        }

        // 씬을 불러오거나 내리는 동안 중복 입력을 막는다.
        public void SetBusy(bool busy)
        {
            CanvasGroup group = GetComponent<CanvasGroup>();

            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            group.interactable = !busy;
        }

        // 코드로 생성한 폰트를 정리한다.
        void OnDestroy()
        {
            if (_font != null)
                Destroy(_font);
        }
    }
}
#endif
