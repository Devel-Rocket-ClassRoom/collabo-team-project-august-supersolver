using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Solver
{
    /// <summary>
    /// 동시에 로드해 둘 시뮬 씬 수를 상수로 묶는다.
    /// SimWorld 의 언로드는 프레임 끝에 처리되는데, 씬이 쌓이면
    /// 생성·해제 비용이 로드된 씬 수를 타서 전체가 제곱이 된다.
    /// 성능 손잡이가 아니라 구조 제약이고, 맵 빌드와 탐색이
    /// 같은 규칙을 써야 해서 한곳에 둔다.
    /// </summary>
    public sealed class SimScenes
    {
        /// 우리가 만들기 전부터 있던 씬 수.
        public int Baseline { get; private set; }

        /// 우리가 더 얹은 씬의 최고점. 상한이 먹히는지 본다.
        public int Peak { get; private set; }

        /// <summary>
        /// 같은 시점의 절대 개수.
        /// 증가분만 보면 실제로 몇 개가 떠 있는지 안 보인다 —
        /// 생성·해제 비용을 결정하는 것은 이쪽이다.
        /// </summary>
        public int PeakTotal { get; private set; }

        /// <summary>
        /// 앞 단계가 남긴 언로드 대기분이 빠질 때까지 기다렸다 기준선을 잡는다.
        /// 바로 재면 그 꼬리가 기준선에 들어가 상한이 그만큼 헐거워진다 —
        /// 맵을 짓고 바로 탐색을 돌리면 빌드가 남긴 씬을 통째로 물려받는다.
        /// </summary>
        public IEnumerator Settle()
        {
            int previous = int.MaxValue;
            int waited = 0;

            while (SceneManager.sceneCount < previous && waited++ < SolverConfig.MaxDrainFrames)
            {
                previous = SceneManager.sceneCount;
                yield return null;
            }

            Baseline = SceneManager.sceneCount;
        }

        /// <summary>
        /// 로드된 씬이 상한 아래로 내려올 때까지 프레임을 넘긴다.
        /// 만든 개수가 아니라 남아 있는 개수를 눌러야 한다 —
        /// 프레임당 몇 개나 빠지는지는 부르는 쪽이 알 수 없다.
        /// </summary>
        public IEnumerator Drain()
        {
            int waited = 0;

            while (SceneManager.sceneCount > Baseline + SolverConfig.MaxLoadedScenes)
            {
                if (++waited > SolverConfig.MaxDrainFrames)
                {
                    Debug.LogWarning(
                        $"씬이 {SolverConfig.MaxDrainFrames} 프레임 동안 안 줄었다 " +
                        $"({SceneManager.sceneCount} 개). 그대로 진행한다.");
                    break;
                }

                yield return null;
            }

            Peak = Mathf.Max(Peak, SceneManager.sceneCount - Baseline);
            PeakTotal = Mathf.Max(PeakTotal, SceneManager.sceneCount);
        }
    }
}
