using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 일정 스텝 뒤 한 번 터져 반경 안의 동적 바디를 바깥으로 밀어내는 장치.
    ///
    /// **장치 6종 중 첫 번째이자, 나머지가 참고할 본보기다 (placeholder).**
    /// 담당 팀원이 자유롭게 바꾸거나 갈아엎어도 된다. 지금 이것이 있는 이유는 콘텐츠가 아니라
    /// 두 가지다 — <see cref="SimWorld.Step"/> 의 로직 루프와 <see cref="Judge"/> 의
    /// <c>HasPendingWork</c> 분기가 실행되는 것, 그리고 주입된 rng 가 실제로 소비되어
    /// 시드가 결과에 닿는 것. 장치가 하나도 없으면 그 셋 다 검증할 수 없다.
    ///
    /// 새 장치를 만들 때 지켜야 할 결정론 규칙이 이 클래스에 다 들어 있다.
    /// </summary>
    public sealed class BombDevice : IStepLogic
    {
        readonly DeviceData _data;
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        /// 아직 뽑지 않았으면 -1. 첫 Tick 에서 확정된다.
        int _fireStep = -1;
        bool _fired;

        /// <param name="bodies">
        /// 월드의 바디 목록을 **살아 있는 참조로** 받는다. 장치는 스트로크보다 먼저 등록되므로
        /// 생성 시점의 스냅샷을 받으면 유저가 그린 물체를 영원히 보지 못한다.
        /// </param>
        public BombDevice(in DeviceData data, IReadOnlyList<Rigidbody2D> bodies)
        {
            _data = data;
            _bodies = bodies;
        }

        /// <summary>아직 안 터졌으면 true. 전 바디가 잠들어도 Stalled 를 미루게 한다.</summary>
        public bool HasPendingWork => !_fired;

        public void Tick(int step, System.Random rng)
        {
            if (_fired) return;

            // 발동 스텝은 생성 시점이 아니라 첫 Tick 에서 뽑는다.
            // rng 는 계약상 Tick 에서만 주어지고, 그래야 난수 소비 순서가
            // "스텝 순서 × 장치 등록 순서"로 고정된다. 월드 구축 중에 뽑으면
            // 구축 순서까지 난수 순서에 얽혀 보장해야 할 것이 한 겹 늘어난다.
            if (_fireStep < 0)
                _fireStep = _data.DelaySteps + (_data.JitterSteps > 0 ? rng.Next(_data.JitterSteps) : 0);

            if (step < _fireStep) return;

            Explode();
            _fired = true;
        }

        void Explode()
        {
            float radius = Mathf.Max(_data.Radius, 1e-4f);
            float sqrRadius = radius * radius;

            // 등록 순서 그대로 훑는다. 순서가 바뀌면 힘을 받는 순서가 바뀌고,
            // 부동소수점 합산 순서가 달라져 같은 입력이 다른 결과를 낸다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic) continue;

                Vector2 delta = body.position - _data.Position;
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > sqrRadius) continue;

                // 잠든 바디는 힘을 줘도 스스로 깨지 않는다 (Physics2D 는 3D 와 다르다).
                // 명시적으로 깨우지 않으면 "터졌는데 아무 일도 일어나지 않는" 장치가 된다.
                body.WakeUp();

                float distance = Mathf.Sqrt(sqrDistance);
                Vector2 direction = distance > 1e-6f ? delta / distance : Vector2.up;

                // 가장자리로 갈수록 선형으로 약해진다. 중심에 겹쳐 있으면 위로 밀어 올린다.
                float falloff = 1f - distance / radius;

                // 힘이 아니라 속도를 더한다. 힘은 질량으로 나뉘는데 자유 물체의 질량은
                // ColliderFactory.FreeBodyMassPerUnit 임시값에서 나오므로,
                // 그 값을 조정하면 폭탄의 세기가 함께 흔들린다.
                body.linearVelocity += direction * (_data.Power * falloff);
            }
        }
    }
}
