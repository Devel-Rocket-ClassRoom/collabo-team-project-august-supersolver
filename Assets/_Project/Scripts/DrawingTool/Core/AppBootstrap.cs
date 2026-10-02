using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 앱 전역 프레임 설정. 씬과 무관하게
    /// 시작 시 한 번 돈다.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // 모바일 기본값이 30fps 라 명시하지 않으면
            // 120Hz 기기도 30 으로 돈다. 물리는 수동
            // 스텝이라 시뮬 결과에는 영향이 없다.
            Application.targetFrameRate =
                Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        }
    }
}
