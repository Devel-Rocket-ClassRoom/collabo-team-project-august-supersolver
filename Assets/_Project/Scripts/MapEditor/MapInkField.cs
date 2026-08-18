using PPS.Core;
using TMPro;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 판의 잉크 상한을 숫자로 고친다.
    /// 상한은 판마다 다르고 레벨 데이터에 들어 있어,
    /// 저장·불러오기는 판을 따라간다.
    /// </summary>
    public sealed class MapInkField : MonoBehaviour
    {
        /// 0 이면 한 획도 못 긋는 판이 되어 막는다.
        const float MinInk = 0.1f;

        [SerializeField] MapEditSession _session;
        [SerializeField] MapEditHistory _history;
        [SerializeField] TMP_InputField _field;

        /// 화면에 띄운 값. 판이 바뀌었는지 여기서 본다.
        float _shown = float.NaN;

        void Awake()
        {
            _field.contentType = TMP_InputField.ContentType.DecimalNumber;
            _field.onEndEdit.AddListener(Commit);
        }

        /// <summary>
        /// 불러오기·되돌리기·새 맵은 판을 통째로 갈아끼운다.
        /// 그쪽에서 알려 주는 길이 없어 매 프레임 맞춘다.
        /// </summary>
        void Update()
        {
            if (_session == null || _field.isFocused) return;

            float ink = _session.Current.Level.InkLimit;
            if (Mathf.Approximately(ink, _shown)) return;

            Show(ink);
        }

        void Commit(string text)
        {
            LevelData level = _session.Current.Level;

            if (!float.TryParse(text, out float ink) || ink < MinInk)
            {
                // 못 읽은 값은 버리고 지금 상한을 되돌려 보인다.
                Show(level.InkLimit);
                return;
            }

            if (Mathf.Approximately(ink, level.InkLimit)) return;

            if (_history != null) _history.BeginEdit();

            level.InkLimit = ink;
            Show(ink);

            Debug.Log($"[맵 에디터] 잉크 상한: {ink}");
        }

        void Show(float ink)
        {
            _shown = ink;
            _field.SetTextWithoutNotify(ink.ToString("0.##"));
        }
    }
}
