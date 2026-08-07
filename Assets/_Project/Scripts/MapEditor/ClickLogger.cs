using UnityEngine;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    /// <summary>
    /// 눌렸는지만 확인하는 임시 스크립트.
    /// 실제 동작이 붙으면 지운다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ClickLogger : MonoBehaviour
    {
        /// 비워두면 오브젝트 이름을 쓴다.
        [SerializeField] string _label;

        void Awake()
        {
            string label = string.IsNullOrEmpty(_label) ? name : _label;
            GetComponent<Button>().onClick.AddListener(
                () => Debug.Log($"[맵 에디터] {label}"));
        }
    }
}
