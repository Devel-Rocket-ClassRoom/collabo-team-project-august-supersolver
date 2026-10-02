using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 다가오는 동적 바디를 반지름 방향으로 되튕긴다.
    /// 느리게 닿는 것은 되튕기지 않아 그냥 벽이 된다.
    /// </summary>
    public sealed class BouncerDevice : IStepLogic
    {
        /// 크기를 안 준 레벨도 보이기는 해야 한다.
        public const float MinRadius = 0.1f;

        /// 되튕긴 뒤 남는 접근 속도의 비율.
        const float PowerAmplifier = 0.6f;

        /// <summary>
        /// 이보다 느리게 다가오면 되튕기지 않는다.
        /// 없으면 위에 얹힌 바디가 매 스텝 되튕겨 잠들지 못하고,
        /// 실패하는 시도마다 상한까지 시뮬레이션을 태운다.
        /// </summary>
        const float MinApproachSpeed = 0.1f;

        readonly BouncerData _data;

        /// <summary>
        /// 되튕길 후보. 월드의 전 바디다.
        /// 여기서 거리와 동적 여부로 걸러낸다.
        /// </summary>
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        /// <summary>
        /// 되튕김이 걸리는 중심 거리. 몸 크기에 공 반지름을 더했다 —
        /// 바디를 점으로 보는 폭탄·바람과 같은 어림이라
        /// 공보다 작은 바디에는 조금 이르게 걸린다.
        /// </summary>
        readonly float _triggerRadius;

        /// <summary>
        /// 정적 바디다. 되튕기지 못한 바디는 여기 얹힌다.
        /// </summary>
        public static Rigidbody2D CreateBody(Scene scene, BouncerData data, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = data.Position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = data.DrawRadius;

            return body;
        }

        /// <summary>
        /// 몸은 위험하지 않다. 닿아도 실패가 아니라 튕긴다.
        /// </summary>
        public static IStepLogic Build(IDeviceData data, in DeviceBuildContext ctx)
        {
            var bouncer = (BouncerData)data;

            var body = CreateBody(ctx.Scene, bouncer, ctx.Name);
            ctx.Bodies.Add(body);

            return new BouncerDevice(bouncer, ctx.Bodies);
        }

        BouncerDevice(BouncerData data, IReadOnlyList<Rigidbody2D> bodies)
        {
            _data = data;
            _bodies = bodies;
            _triggerRadius = data.DrawRadius + LevelData.BallRadius;
        }

        /// <summary>
        /// 늘 false 다. 바운서는 끝나지 않아서, true 로 두면
        /// 공이 멈춰도 Stalled 가 안 나 상한까지 태운다.
        /// </summary>
        public bool HasPendingWork => false;

        public void Tick(int step, System.Random rng)
        {
            // 등록 순서 그대로 훑는다.
            // 속도를 고치는 순서가 곧 합산 순서다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;
                if (body.bodyType != RigidbodyType2D.Dynamic) continue;

                Vector2 delta = body.position - _data.Position;
                float distance = delta.magnitude;

                // 중심이 겹치면 되튕길 방향이 없다.
                if (distance < 1e-6f) continue;

                Vector2 normal = delta / distance;

                // 장치 쪽으로 다가오는 속도. 멀어지는 중이면 음수다.
                float approach = -Vector2.Dot(body.linearVelocity, normal);
                if (approach < MinApproachSpeed) continue;

                // 이번 스텝에 표면까지 닿는가. 닿고 나서 보면
                // 솔버가 이미 속도를 먹어치운 뒤라 되튕길 것이 없다.
                if (distance - approach * SimWorld.FixedDt > _triggerRadius) continue;

                // Physics2D 는 속도를 줘도 스스로 깨지 않는다.
                body.WakeUp();

                // 법선 성분만 뒤집어 비율만큼 남긴다.
                // 접선 성분은 그대로 지나간다 — 스쳐 가는 것은 스쳐 간다.
                body.linearVelocity += normal * (approach * (1f + PowerAmplifier));

                SimSignals.Trigger(DeviceType.Bouncer, _data.Position);
            }
        }
    }
}
