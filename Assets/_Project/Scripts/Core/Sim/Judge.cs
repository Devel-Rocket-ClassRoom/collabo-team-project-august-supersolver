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

        /// 지금까지 모은 별 개수.
        public int Stars { get; private set; }

        /// 초기 상태부터 계측한다.
        public float MinGoalDist { get; private set; } = float.PositiveInfinity;

        /// 별마다 수집 여부. 인덱스는 레벨 순서.
        bool[] _collected;

        /// <summary>
        /// 그 별을 먹었는가. 화면이 먹은 별을 지울 때 쓴다.
        /// 판정에는 개수만 쓰이지만 표시에는 어느 것인지가 필요하다.
        /// </summary>
        public bool IsCollected(int index) =>
            _collected != null && index >= 0 && index < _collected.Length && _collected[index];

        internal void Initialize(SimWorld world)
        {
            var stars = world.Level.Stars;
            _collected = new bool[stars == null ? 0 : stars.Count];

            UpdateGoalDistance(world);

            // 시작 지점에 별이 겹쳐 있으면 0 스텝에 먹는다.
            CollectStars(world);
        }

        /// <summary>
        /// 물리 전진 직후에 호출된다.
        /// 한 번 확정된 판정은 뒤집지 않는다.
        /// </summary>
        public void Evaluate(int step, SimWorld world)
        {
            if (Cleared || Failed || Stalled) return;

            // 판정보다 먼저 센다. 같은 스텝에 골에
            // 닿아도 그때 지나친 별은 인정한다.
            CollectStars(world);

            float dist = UpdateGoalDistance(world);
            var level = world.Level;

            if (dist <= LevelData.GoalRadius + LevelData.BallRadius)
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

        /// <summary>
        /// 별은 콜라이더가 없다. 물리에 끼면
        /// 공의 궤적이 바뀌어 솔버가 푼 답이 달라진다.
        /// </summary>
        void CollectStars(SimWorld world)
        {
            var stars = world.Level.Stars;
            float reach = LevelData.StarCaptureRadius+ LevelData.BallRadius;
            Vector2 ball = world.Ball.position;

            for (int i = 0; i < _collected.Length; i++)
            {
                if (_collected[i]) continue;
                if (Vector2.Distance(ball, stars[i]) > reach) continue;

                _collected[i] = true;
                Stars++;

                // 판정을 끝낸 뒤 알린다. 세는 것이 먼저다.
                world.Events?.RaiseStarCollected(i, stars[i]);
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
