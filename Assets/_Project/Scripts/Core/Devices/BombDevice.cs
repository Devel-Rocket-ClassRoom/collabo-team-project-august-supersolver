using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// <summary>
        /// 폭탄 몸체의 반지름. <see cref="DeviceData.Radius"/> 는 **폭발 반경**이지 몸 크기가 아니다.
        /// 둘을 한 필드로 합치면 "작은 폭탄이 넓게 터진다"를 표현할 수 없어진다.
        /// 몸 크기를 레벨마다 조절하고 싶어지면 그때 필드로 올리면 된다.
        /// </summary>
        public const float BodyRadius = 0.28f;

        readonly DeviceData _data;

        /// <summary>
        /// 월드의 바디 목록. **소유하지 않고 살아 있는 참조로 들고만 있는다.**
        ///
        /// 스냅샷이 아니라 참조여야 하는 이유: 장치는 스트로크보다 먼저 등록되므로,
        /// 생성 시점에 복사해 두면 유저가 그린 물체를 영원히 보지 못한다.
        /// <see cref="Tick"/> 이 도는 시점에는 이 목록에 스트로크까지 전부 들어와 있다.
        ///
        /// **순회할 때 항상 null 검사를 할 것.** 시뮬 도중 파괴된 바디는 목록에서 빠지지 않고
        /// 그 자리에 null 로 남는다 (인덱스가 밀리면 해시 구조가 통째로 바뀌기 때문).
        /// 순서도 건드리면 안 된다 — 힘이 가해지는 순서가 곧 부동소수점 합산 순서다.
        /// </summary>
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        /// 폭탄 자신의 몸체. <see cref="_bodies"/> 안에도 같은 참조가 들어 있다. 터지면 파괴하고 null 로 만든다.
        Rigidbody2D _body;

        /// 아직 뽑지 않았으면 -1. 첫 Tick 에서 확정된다.
        int _fireStep = -1;
        bool _fired;

        /// <summary>
        /// 폭탄의 몸체를 만든다. **정적 바디다** — <see cref="DeviceData.Position"/> 이
        /// "여기 있다"는 뜻이어야 레벨 디자인이 성립한다. 동적이면 그 값이 스폰 지점일 뿐이 된다.
        /// 굴러다니는 폭탄이 필요하면 장치 담당이 바꾸면 된다.
        ///
        /// 컴포넌트 순서는 <see cref="ColliderFactory"/> 와 같다 — transform 을 먼저 놓고
        /// Rigidbody2D 를 나중에 붙여야 바디가 올바른 위치에서 등록된다.
        /// </summary>
        public static Rigidbody2D CreateBody(Scene scene, in DeviceData data, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = data.Position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = BodyRadius;

            return body;
        }

        /// <param name="body">
        /// <see cref="CreateBody"/> 가 만든 몸체. 월드의 바디 목록에 **이미 등록된 뒤에** 넘어온다.
        /// </param>
        /// <param name="bodies">
        /// 월드의 바디 목록을 **살아 있는 참조로** 받는다. 장치는 스트로크보다 먼저 등록되므로
        /// 생성 시점의 스냅샷을 받으면 유저가 그린 물체를 영원히 보지 못한다.
        /// </param>
        public BombDevice(in DeviceData data, Rigidbody2D body, IReadOnlyList<Rigidbody2D> bodies)
        {
            _data = data;
            _body = body;
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
                if (body == null) continue;

                // 자기 몸체는 효과 대상이 아니다.
                //
                // 지금은 정적이라 아래 검사에서도 걸러지지만, 몸체를 동적으로 바꾸는 순간
                // 자기 자신이 후보로 들어온다. 그때 거리가 정확히 0 이라 Vector2.up 폴백에
                // falloff 1.0 이 걸려 최대 세기로 자기를 밀어 올리고, 그 속도는 같은 Tick 안의
                // 파괴로 그냥 버려진다 — 물리에는 무해하지만 읽는 사람을 멈춰 세운다.
                if (ReferenceEquals(body, _body)) continue;

                if (body.bodyType != RigidbodyType2D.Dynamic) continue;

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

            // 효과를 다 준 뒤에 소모된다. 순서가 물리 결과를 바꾸지는 않는다 —
            // Simulate() 는 Tick() 이 전부 끝난 뒤에 돌기 때문이다.
            // 다만 위 루프가 **파괴 이전의 온전한 목록**을 훑게 되어 읽기가 분명해진다.
            DestroyBody();
        }

        /// <summary>
        /// 폭탄이 소모되어 월드에서 사라진다. 위에 얹혀 있던 것은 받침을 잃는다.
        ///
        /// **`Destroy` 가 아니라 `DestroyImmediate` 여야 한다.** `Destroy` 는 프레임 끝까지
        /// 지연되는데, 한 프레임에 도는 스텝 수는 바깥 사정이다 — 게임에서는 누적기가 0~8 번,
        /// 솔버에서는 1800 번을 한 프레임에 몰아 돌린다. 지연 파괴를 쓰면 **바디가 실제로 사라지는
        /// 스텝이 프레임 경계에 따라 달라져** 프레임 독립성이 그 자리에서 깨진다.
        /// 여기는 물리 콜백 밖이고 <c>Simulate()</c> 사이의 지점이라 즉시 파괴가 안전하다.
        ///
        /// **바디 목록에서 빼지 않는다.** 빼면 뒤 인덱스가 밀려 해시 구조가 통째로 바뀐다.
        /// <see cref="WorldHasher"/> 는 null 자리를 그대로 해시에 섞도록 되어 있다 —
        /// "여기 뭔가 있었다"는 사실 자체가 디싱크 신호이기 때문이다.
        /// </summary>
        void DestroyBody()
        {
            if (_body == null) return;

            UnityEngine.Object.DestroyImmediate(_body.gameObject);
            _body = null;
        }
    }
}
