using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 스테이지 내내 캔버스 한가운데 떠 있는 표시 한 장.
    /// 순서·앵커·조건이 없어 Tutorial 과 따로 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "FixedTutorial", menuName = "Scriptable Objects/FixedTutorial")]
    public class FixedTutorial : ScriptableObject
    {
        /// 이 표시가 붙는 스테이지. ThemeModel.Stages 의
        /// 인덱스다 — ToolUnlock 의 테마를 가로지르는
        /// 1-기반 번호와 다르다.
        public int StageIndex;

        /// 캔버스 한가운데 띄울 것. 안의 그래픽은 전부
        /// RaycastTarget 을 꺼야 한다. 하나라도 켜져 있으면
        /// 내내 떠 있어 그 자리에서 영영 획을 못 긋는다.
        public GameObject Prefab;
    }
}
