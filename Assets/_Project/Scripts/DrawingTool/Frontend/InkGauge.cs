using UnityEngine;
using UnityEngine.UI;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 잉크 잔량 바. 숫자는 쓰지 않는다.
    /// 그리는 중 값은 프리뷰 근사고, 진실은 획을
    /// 확정한 뒤의 재계산이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InkGauge : MonoBehaviour
    {
        [SerializeField] Image _fill;

        PointerReader _input;

        /// 배선 전 한 프레임이 뜰 수 있다. 그때 돌면
        /// 아직 없는 로직을 읽는다.
        void Awake() => enabled = false;

        public void Bind(PointerReader input)
        {
            _input = input;
            enabled = true;
        }

        void Update()
        {
            _fill.fillAmount = _input.InkRatio;
        }
    }
}
