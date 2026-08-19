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
        DrawInputBehaviour _input;
        [SerializeField] Image _fill;

        /// <summary>
        /// 입력을 물린다. UI 프리팹은 월드보다 먼저 깨어나
        /// 첫 프레임에는 비어 있다.
        /// </summary>
        public void Bind(DrawInputBehaviour input) => _input = input;

        void Update()
        {
            if (_input == null) return;

            _fill.fillAmount = _input.InkRatio;
        }
    }
}
