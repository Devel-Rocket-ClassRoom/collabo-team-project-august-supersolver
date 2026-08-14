using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 잉크 예산. 상한은 판이 정하고 소비는 확정된 획이
    /// 정한다 — 그리는 중 값은 인식기의 근사라 여기
    /// 진실이 아니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InkBudget : MonoBehaviour
    {
        [SerializeField] DrawingSession _session;

        /// 상한이 나오는 판. 상한을 float 로 복사해 두면
        /// 원본과 갈라질 자리가 생겨 판을 통째로 든다.
        /// 판이 붙기 전까지는 기본값 판이다.
        LevelData _level = new LevelData();

        /// 확정된 획만 센 잔량. 획을 시작할 때 쓰는 값이다.
        public float Remaining => _level.InkLimit - _session.Solution.TotalInk();

        /// <summary>
        /// 상한이 나오는 판을 물린다. 획을 그린 뒤에
        /// 바뀌면 잔량이 음수가 되므로 판을 붙일 때
        /// 한 번만 부른다.
        /// </summary>
        public void SetLevel(LevelData level) => _level = level;

        /// <summary>게이지용. 잔량이 어디서 왔든 상한으로 나눈다.</summary>
        public float RatioOf(float remaining) => Mathf.Clamp01(remaining / _level.InkLimit);
    }
}
