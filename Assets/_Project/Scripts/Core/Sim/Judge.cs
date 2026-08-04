using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 클리어/실패/정지 판정.
    /// 물리 콜백이 아니라 매 스텝 직접 계산한다.
    /// 콜백은 순서를 엔진이 정해 재현이 흔들린다.
    /// </summary>
    public sealed class Judge
    {
        public bool Cleared { get; private set; }
        public bool Failed { get; private set; }
        public bool Stalled { get; private set; }

        /// 판정이 확정된 스텝. 아직이면 -1.
        public int DecidedStep { get; private set; } = -1;

        /// 별 장치가 들어올 때까지 항상 0.
        public int Stars { get; private set; }

        /// 초기 상태부터 계측한다.
        public float MinGoalDist { get; private set; } = float.PositiveInfinity;

        internal void Initialize(SimWorld world)
        {
            UpdateGoalDistance(world);
        }

        /// <summary>
        /// 물리 전진 직후에 호출된다.
        /// 한 번 확정된 판정은 뒤집지 않는다.
        /// </summary>
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

            // 클리어 검사 뒤에 둔다.
            // 같은 스텝에 겹치면 클리어를 준다.
            if (TouchedHazard(world))
            {
                Failed = true;
                DecidedStep = step;
                return;
            }

            // 대기 중인 장치가 있으면 아직 아니다.
            if (world.AllBodiesSleeping() && !world.AnyPendingWork())
            {
                Stalled = true;
                DecidedStep = step;
            }
        }

        /// <summary>
        /// IsTouching 은 콜백이 아니라 질의라
        /// 부르는 시점을 우리가 정한다.
        /// </summary>
        static bool TouchedHazard(SimWorld world)
        {
            var ball = world.BallCollider;
            if (ball == null) return false;

            var hazards = world.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                var hazard = hazards[i];
                if (hazard == null) continue;   // 파괴된 파편
                if (ball.IsTouching(hazard)) return true;
            }
            return false;
        }

        float UpdateGoalDistance(SimWorld world)
        {
            float dist = Vector2.Distance(world.Ball.position, world.Level.GoalPosition);
            if (dist < MinGoalDist) MinGoalDist = dist;
            return dist;
        }
    }
}
