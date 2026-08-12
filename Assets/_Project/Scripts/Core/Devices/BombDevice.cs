using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 터져서 반경 안의 동적 바디를 밀어낸다.
    /// 새 장치가 참고할 본보기다 (placeholder).
    /// </summary>
    public sealed class BombDevice : IStepLogic
    {
        /// 몸 크기. DeviceData.Radius 는 폭발 반경이다.
        public const float BodyRadius = 0.28f;

        readonly DeviceData _data;

        /// <summary>
        /// 폭발이 밀어낼 후보. 월드의 전 바디다.
        /// 여기서 반경과 동적 여부로 걸러낸다.
        /// </summary>
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        Rigidbody2D _body;

        /// 아직 안 뽑았으면 -1. 첫 Tick 에서 정한다.
        int _fireStep = -1;
        bool _fired;

        /// <summary>
        /// 정적 바디다. Position 이 "여기 있다"는
        /// 뜻이어야 레벨 디자인이 성립한다.
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

        readonly SimEvents _events;

        /// 레벨의 장치 번호. 알릴 때 누구인지 밝힌다.
        readonly int _index;

        public BombDevice(
            in DeviceData data, Rigidbody2D body, IReadOnlyList<Rigidbody2D> bodies,
            SimEvents events, int index)
        {
            _data = data;
            _body = body;
            _bodies = bodies;
            _events = events;
            _index = index;
        }

        /// 아직 안 터졌으면 Stalled 를 미루게 한다.
        public bool HasPendingWork => !_fired;

        public void Tick(int step, System.Random rng)
        {
            if (_fired) return;

            // 발동 스텝은 첫 Tick 에서 뽑는다.
            // 그래야 난수 소비 순서가
            // 스텝 × 장치 등록 순서로 고정된다.
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

            // 등록 순서 그대로 훑는다.
            // 힘을 받는 순서가 곧 합산 순서다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;

                // 자기 자신은 밀어내지 않는다. 거리가 0 이라
                // 최대 세기가 걸리는데 어차피 곧 파괴된다.
                if (ReferenceEquals(body, _body)) continue;

                if (body.bodyType != RigidbodyType2D.Dynamic) continue;

                Vector2 delta = body.position - _data.Position;
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > sqrRadius) continue;

                // Physics2D 는 힘을 줘도 스스로 깨지 않는다.
                body.WakeUp();

                float distance = Mathf.Sqrt(sqrDistance);
                Vector2 direction = distance > 1e-6f ? delta / distance : Vector2.up;

                // 가장자리로 갈수록 선형으로 약해진다.
                float falloff = 1f - distance / radius;

                // 힘이 아니라 속도를 더한다. 힘은 질량으로
                // 나뉘는데 자유 물체 질량이 임시값이라
                // 그걸 고치면 세기가 함께 흔들린다.
                body.linearVelocity += direction * (_data.Power * falloff);
            }

            DestroyBody();

            // 미는 것을 다 끝낸 뒤 알린다.
            // 구독자가 무엇을 보든 결과는 이미 확정돼 있다.
            _events?.RaiseDeviceFired(_index, _data.Position);
        }

        /// <summary>
        /// Destroy 가 아니라 DestroyImmediate 여야 한다.
        /// 지연 파괴는 사라지는 스텝이 프레임 경계를 타서
        /// 프레임 독립성이 깨진다.
        /// </summary>
        void DestroyBody()
        {
            if (_body == null) return;

            // 목록에서는 빼지 않는다. 빼면 뒤 인덱스가 밀린다.
            UnityEngine.Object.DestroyImmediate(_body.gameObject);
            _body = null;
        }
    }
}
