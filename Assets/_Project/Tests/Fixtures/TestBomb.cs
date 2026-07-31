using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 테스트용 가짜 폭탄. 일정 스텝 뒤 한 번 <see cref="Target"/> 을 밀어 올리고 끝난다.
    ///
    /// **콘텐츠가 아니라 검증 도구다.** 장치가 하나도 없으면
    /// <see cref="SimWorld.Step"/> 의 로직 루프가 0 회 반복하고,
    /// <see cref="Judge"/> 의 <c>HasPendingWork</c> 분기가 실행되지 않으며,
    /// 주입된 rng 가 아무에게도 전달되지 않아 시드가 결과에 반영되는지 확인할 수 없다.
    /// 코어가 아니라 테스트 픽스처에 두는 이유가 이것이다 — 출하 코드에 남을 물건이 아니다.
    ///
    /// 장치가 레벨 데이터에서 나오게 되는 것은 팀원 B·C 의 작업이다.
    /// 그 스키마를 지금 정하지 않으려고 <see cref="WorldBuilder.Build"/> 의 주입 인자만 쓴다.
    /// </summary>
    public sealed class TestBomb : IStepLogic
    {
        readonly int _delaySteps;
        readonly int _jitterSteps;
        readonly float _power;

        /// 아직 뽑지 않았으면 -1. 첫 Tick 에서 확정된다.
        int _fireStep = -1;
        bool _fired;

        /// <summary>
        /// 밀어 올릴 대상. 월드가 만들어진 뒤 <c>world.Ball</c> 로 채운다.
        /// 첫 <see cref="Step"/> 이전에만 넣으면 되므로 결정론에는 영향이 없다.
        /// </summary>
        public Rigidbody2D Target;

        /// <param name="jitterSteps">
        /// rng 로 뽑는 추가 지연의 상한(미만). 0 이면 시드와 무관하게 정확히
        /// <paramref name="delaySteps"/> 에 터진다. 이 값이 시드 축을 살린다.
        /// </param>
        public TestBomb(int delaySteps, int jitterSteps = 0, float power = 4f)
        {
            _delaySteps = delaySteps;
            _jitterSteps = jitterSteps;
            _power = power;
        }

        public bool HasPendingWork => !_fired;

        public void Tick(int step, System.Random rng)
        {
            if (_fired) return;

            // 발동 스텝은 생성 시점이 아니라 첫 Tick 에서 뽑는다. rng 는 계약상 Tick 에서만
            // 주어지고, 그래야 난수 소비 순서가 "스텝 순서 × 장치 등록 순서"로 고정된다.
            if (_fireStep < 0)
                _fireStep = _delaySteps + (_jitterSteps > 0 ? rng.Next(_jitterSteps) : 0);

            if (step < _fireStep) return;

            if (Target != null)
            {
                // 잠든 바디는 힘을 줘도 스스로 깨지 않는다 (Physics2D 는 3D 와 다르다).
                Target.WakeUp();

                // 힘이 아니라 속도를 더한다. 힘은 질량으로 나뉘는데 그 질량은
                // ColliderFactory 의 임시값에서 나오므로, 값이 조정되면 폭탄 세기가 함께 흔들린다.
                Target.linearVelocity += Vector2.up * _power;
            }

            _fired = true;
        }
    }
}
