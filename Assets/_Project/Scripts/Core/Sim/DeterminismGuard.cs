using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 전역 물리 설정 검사. 결정론은 코드만으로 지켜지지 않는다 —
    /// Physics2D 설정은 프로젝트 전역 가변 상태이고, 인스펙터에서 누가 한 번 누르면
    /// 코드는 그대로인데 결과가 달라진다. 그 사고를 테스트로 잡기 위한 장치다.
    /// </summary>
    public static class DeterminismGuard
    {
        /// <summary>
        /// 결정론을 직접 깨뜨리는 설정을 찾는다.
        /// </summary>
        /// <returns>위반이 없으면 빈 리스트.</returns>
        public static List<string> FindViolations()
        {
            var violations = new List<string>();

            if (Physics2D.jobOptions.useMultithreading)
            {
                violations.Add(
                    "Physics2D.jobOptions.useMultithreading 가 켜져 있다. " +
                    "스텝 내부 멀티스레딩은 부동소수점 합산 순서를 바꿔 결정론을 깨뜨린다. " +
                    "Project Settings > Physics 2D 에서 끌 것.");
            }

            if (Physics2D.autoSyncTransforms)
            {
                violations.Add(
                    "Physics2D.autoSyncTransforms 가 켜져 있다. Transform 을 건드리는 코드가 " +
                    "스텝 사이에 물리 상태를 밀어 넣을 수 있어 재현이 흔들린다. 끌 것.");
            }

            return violations;
        }

        /// <summary>
        /// 시뮬 결과에 영향을 주는 전역 설정들의 지문.
        ///
        /// 값 자체를 고정하지는 않는다 — 물리 튜닝은 정당한 작업이기 때문이다.
        /// 다만 리플레이가 재생되지 않을 때 "코드 버그"와 "설정이 바뀐 것"을
        /// 구분할 수 있어야 하며, 그 판단 재료로 리포트·리플레이에 함께 기록한다.
        /// </summary>
        public static string SettingsFingerprint()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;

            sb.Append("gravity=").Append(Physics2D.gravity.ToString("F4", c));
            sb.Append(" velIter=").Append(Physics2D.velocityIterations);
            sb.Append(" posIter=").Append(Physics2D.positionIterations);
            sb.Append(" contactOffset=").Append(Physics2D.defaultContactOffset.ToString("F4", c));
            sb.Append(" timeToSleep=").Append(Physics2D.timeToSleep.ToString("F4", c));
            sb.Append(" linSleepTol=").Append(Physics2D.linearSleepTolerance.ToString("F4", c));
            sb.Append(" angSleepTol=").Append(Physics2D.angularSleepTolerance.ToString("F4", c));
            sb.Append(" baumgarte=").Append(Physics2D.baumgarteScale.ToString("F4", c));
            sb.Append(" maxTransSpeed=").Append(Physics2D.maxTranslationSpeed.ToString("F4", c));
            sb.Append(" maxRotSpeed=").Append(Physics2D.maxRotationSpeed.ToString("F4", c));
            sb.Append(" subStepping=").Append(Physics2D.useSubStepping);
            sb.Append(" maxSubStep=").Append(Physics2D.maxSubStepCount);
            sb.Append(" minSubStepFPS=").Append(Physics2D.minSubStepFPS.ToString("F4", c));
            sb.Append(" multithread=").Append(Physics2D.jobOptions.useMultithreading);

            return sb.ToString();
        }
    }
}
