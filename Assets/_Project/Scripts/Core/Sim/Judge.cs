using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 클리어/실패/정지 판정과 <see cref="MinGoalDist"/> 계측.
    ///
    /// 판정은 물리 콜백(OnTriggerEnter2D 등)이 아니라 **매 스텝 직접 계산**으로 한다.
    /// 콜백은 호출 순서와 시점을 물리 엔진이 정하므로 결정론 검증의 사각지대가 되고,
    /// 무엇보다 헤드리스 솔버에서 콜백 수신을 위해 MonoBehaviour 를 붙여야 해서
    /// "코어는 프레임을 모른다"가 깨진다.
    /// </summary>
    public sealed class Judge
    {
        public bool Cleared { get; private set; }
        public bool Failed { get; private set; }
        public bool Stalled { get; private set; }

        /// 판정이 확정된 스텝. 아직이면 -1.
        public int DecidedStep { get; private set; } = -1;

        /// <summary>M01 범위에서는 항상 0. 별 장치가 들어오는 시점에 수집 개수로 채운다.</summary>
        public int Stars { get; private set; }

        /// <summary>
        /// 시뮬 전체에서 공과 목표 사이의 최소 거리. 초기 상태부터 계측한다.
        /// 실패한 시도에서도 CEM 이 쓸 수 있는 유일한 학습 신호이므로 항상 채워져야 한다.
        /// </summary>
        public float MinGoalDist { get; private set; } = float.PositiveInfinity;

        internal void Initialize(SimWorld world)
        {
            UpdateGoalDistance(world);
        }

        /// <summary>물리 전진 직후에 호출된다. 한 번 확정된 판정은 뒤집지 않는다.</summary>
        public void Evaluate(int step, SimWorld world)
        {
            if (Cleared || Failed || Stalled) return;

            float dist = UpdateGoalDistance(world);
            var level = world.Level;

            if (dist <= level.GoalRadius + level.BallRadius)
            {
                Cleared = true;
                DecidedStep = step;
                return;
            }

            if (world.Ball.position.y < level.KillY)
            {
                Failed = true;
                DecidedStep = step;
                return;
            }

            // 더 이상 아무 일도 일어나지 않는 상태. 대기 중인 장치가 있으면 아직 아니다.
            if (world.AllBodiesSleeping() && !world.AnyPendingWork())
            {
                Stalled = true;
                DecidedStep = step;
            }
        }

        float UpdateGoalDistance(SimWorld world)
        {
            float dist = Vector2.Distance(world.Ball.position, world.Level.GoalPosition);
            if (dist < MinGoalDist) MinGoalDist = dist;
            return dist;
        }
    }
}
