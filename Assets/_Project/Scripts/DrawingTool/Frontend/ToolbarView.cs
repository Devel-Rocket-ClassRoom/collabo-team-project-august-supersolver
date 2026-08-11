using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 툴바가 지금 고른 도구를 내보인다. 표시 품질과
    /// 모드별 숨김은 상태 머신 작업 몫이고, 여기는
    /// 어느 도구인지 손으로 확인할 최소한만 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolbarView : MonoBehaviour
    {
        [SerializeField] ToolSelection _tools;

        [Header("선택 표시")]
        [SerializeField] GameObject _fixedLineSelected;
        [SerializeField] GameObject _freeBodySelected;
        [SerializeField] GameObject _justPivotSelected;
        [SerializeField] GameObject _worldPivotSelected;

        [Header("회전축 슬롯 2모드")]
        [SerializeField] GameObject _justPivot;
        [SerializeField] GameObject _worldPivot;

        void OnEnable()
        {
            _tools.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            _tools.Changed -= Apply;
        }

        void Apply()
        {
            // 두 핀 버튼은 같은 rect 에 겹쳐 있다. 한쪽을
            // 끄지 않으면 위엣것이 클릭을 전부 먹는다.
            bool single = _tools.PivotMode == DrawTool.PivotSingle;
            _justPivot.SetActive(single);
            _worldPivot.SetActive(!single);

            DrawTool current = _tools.Current;
            _fixedLineSelected.SetActive(current == DrawTool.FixedLine);
            _freeBodySelected.SetActive(current == DrawTool.FreeBody);
            _justPivotSelected.SetActive(current == DrawTool.PivotSingle);
            _worldPivotSelected.SetActive(current == DrawTool.PivotWorld);
        }
    }
}
