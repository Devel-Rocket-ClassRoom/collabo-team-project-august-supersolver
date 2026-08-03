using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 전역 물리 설정 검사.
    /// 인스펙터에서 누가 한 번 누르면
    /// 코드는 그대로인데 결과가 달라진다.
    /// </summary>
    public static class DeterminismGuard
    {
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
        /// 전역 설정의 지문.
        /// 리플레이가 안 맞을 때 코드 버그와
        /// 설정 변경을 구분하는 재료다.
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
