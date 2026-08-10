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
        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] Image _fill;

        void Update()
        {
            _fill.fillAmount = _input.InkRatio;
        }
    }
}
