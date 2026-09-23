#if UNITY_EDITOR
using PPS.Core;
using UnityEngine;

namespace PPS.Tools
{
    // 씬을 이동해도 저장한 리플레이를 보관한다.
    public static class ReplayDebugCache
    {
        public static ReplayData Current { get; private set; }

        public static bool HasReplay => Current != null;

        // 복사에 성공했을 때만 기존 캐시를 교체한다.
        public static bool Save(StageData stage, Solution solution)
        {
            ReplayData snapshot =
                ReplayData.CreateSnapshot(stage, solution);

            if (snapshot == null)
                return false;

            Current = snapshot;
            return true;
        }

        // 보관 중인 리플레이를 비운다.
        public static void Clear()
        {
            Current = null;
        }

        // 도메인 리로드를 꺼도 재생 시작 시 캐시를 비운다.
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Clear();
        }
    }
}
#endif