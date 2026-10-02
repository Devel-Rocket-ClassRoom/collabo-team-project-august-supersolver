using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 스테이지 내내 캔버스 한가운데 떠 있는 표시 한 장.
    /// 순서·앵커·조건이 없어 Tutorial 과 따로 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "FixedTutorial", menuName = "Scriptable Objects/FixedTutorial")]
    public class FixedTutorial : TutorialBase
    {
        /// 캔버스 한가운데 띄울 것. 안의 그래픽은 전부
        /// RaycastTarget 을 꺼야 한다. 하나라도 켜져 있으면
        /// 내내 떠 있어 그 자리에서 영영 획을 못 긋는다.
        public TutorialPrefabKey PrefabKey;

        /// 캔버스 한가운데에서 이만큼 민 자리에 띄운다.
        /// 판을 안 가리는 곳이 맵마다 달라 컷과 같은
        /// 캔버스 픽셀 값을 쓴다.
        public Vector2 Offset;
    }
}
