using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 빠르게 부딪힌 것에 부서지는 벽.
    /// 부서지면서 주변의 동적 바디를 밀어낸다.
    /// 느리게 닿는 것은 그냥 막는다.
    /// </summary>
    public sealed class BarricadeDevice : IStepLogic
    {
        /// 크기를 안 준 레벨도 보이기는 해야 한다.
        public const float MinHalfSize = 0.1f;

        /// 밀어내는 범위는 몸 크기의 이 배다.
        public const float BlastScale = 3f;

        readonly BarricadeData _data;

        /// <summary>
        /// 부딪힐 후보이자 밀어낼 후보. 월드의 전 바디다.
        /// 여기서 거리와 동적 여부로 걸러낸다.
        /// </summary>
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        /// <summary>
        /// 직전 스텝의 속도. 인덱스는 _bodies 와 같다.
        /// 접촉이 보이는 스텝에는 충돌이 이미 속도를 먹은
        /// 뒤라, 그때 재면 무엇이 와도 느린 것으로 나온다.
        /// </summary>
        readonly List<float> _speeds = new List<float>();

        readonly SimEvents _events;

        /// 레벨의 장치 번호. 알릴 때 누구인지 밝힌다.
        readonly int _index;

        Rigidbody2D _body;
        Collider2D _collider;

        /// <summary>
        /// 정적 바디다. 부서지기 전까지는 벽이라
        /// 얹힌 것에 밀려나면 안 된다.
        /// </summary>
        public static Rigidbody2D CreateBody(Scene scene, BarricadeData data, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(
                data.Position, Quaternion.Euler(0f, 0f, data.Angle));

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one * (data.DrawRadius * 2f);

            return body;
        }

        /// <summary>
        /// 몸은 위험하지 않다. 닿아도 실패가 아니라 막힌다.
        /// </summary>
        public static IStepLogic Build(IDeviceData data, in DeviceBuildContext ctx)
        {
            var barricade = (BarricadeData)data;

            var body = CreateBody(ctx.Scene, barricade, ctx.Name);
            ctx.Bodies.Add(body);

            return new BarricadeDevice(barricade, body, ctx.Bodies, ctx.Events, ctx.Index);
        }

        BarricadeDevice(
            BarricadeData data, Rigidbody2D body, IReadOnlyList<Rigidbody2D> bodies,
            SimEvents events, int index)
        {
            _data = data;
            _body = body;
            _collider = body.GetComponent<Collider2D>();
            _bodies = bodies;
            _events = events;
            _index = index;
        }

        /// <summary>
        /// 늘 false 다. 부딪히는 것이 없으면 영영 그대로라,
        /// true 로 두면 공이 멈춰도 Stalled 가 안 난다.
        /// </summary>
        public bool HasPendingWork => false;

        public void Tick(int step, System.Random rng)
        {
            if (_body == null) return;

            // 등록 순서 그대로 훑는다.
            // 첫 스텝에는 잰 속도가 없어 아무것도 안 부순다.
            for (int i = 0; i < _bodies.Count && i < _speeds.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;

                // 동적 바디만 본다. 박쥐처럼 스스로 속도를
                // 되돌리는 것까지 세면 장치 등록 순서에 따라
                // 부서지는 스텝이 갈린다.
                if (body.bodyType != RigidbodyType2D.Dynamic) continue;

                if (_speeds[i] < _data.ThresholdSpeed) continue;
                if (!body.IsTouching(_collider)) continue;

                Break();
                return;
            }

            RecordSpeeds();
        }

        /// <summary>
        /// 이번 스텝의 속도를 다음 스텝을 위해 남긴다.
        /// 목록은 뒤로만 자라므로 인덱스가 밀리지 않는다.
        /// </summary>
        void RecordSpeeds()
        {
            while (_speeds.Count < _bodies.Count) _speeds.Add(0f);

            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                _speeds[i] = body == null ? 0f : body.linearVelocity.magnitude;
            }
        }

        void Break()
        {
            Blast();

            // 미는 것을 다 끝낸 뒤, 몸을 지우기 전에 알린다.
            _events?.RaiseDeviceFired(_index);

            DestroyBody();
            SimSignals.Trigger(DeviceType.Barricade, _data.Position);
        }

        /// <summary>
        /// 폭탄과 같은 방식으로 민다 — 힘이 아니라 속도를
        /// 더한다. 힘은 질량으로 나뉘는데 자유 물체 질량이
        /// 임시값이라 그걸 고치면 세기가 함께 흔들린다.
        /// </summary>
        void Blast()
        {
            float radius = Mathf.Max(_data.Reach, 1e-4f);
            float sqrRadius = radius * radius;

            // 등록 순서 그대로 훑는다.
            // 힘을 받는 순서가 곧 합산 순서다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;
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

                body.linearVelocity += direction * (_data.Power * falloff);
            }
        }

        /// <summary>
        /// DestroyImmediate 여야 한다. 지연 파괴는
        /// 사라지는 스텝이 프레임 경계를 타서
        /// 프레임 독립성이 깨진다.
        /// </summary>
        void DestroyBody()
        {
            // 목록에서는 빼지 않는다. 빼면 뒤 인덱스가 밀린다.
            Object.DestroyImmediate(_body.gameObject);
            _body = null;
            _collider = null;
        }
    }
}
