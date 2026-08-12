using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 범위 안의 동적 바디를 한 방향으로 민다.
    /// 바디를 만들지 않는다 — 통과하는 구역이라
    /// 부딪힐 것이 있으면 안 된다.
    /// </summary>
    public sealed class WindDevice : IStepLogic
    {
        readonly DeviceData _data;

        /// <summary>
        /// 밀 후보. 월드의 전 바디다.
        /// 여기서 반경과 동적 여부로 걸러낸다.
        /// </summary>
        readonly IReadOnlyList<Rigidbody2D> _bodies;

        /// 매 스텝 더할 속도. 미리 접어 둔다.
        readonly Vector2 _push;

        public WindDevice(in DeviceData data, IReadOnlyList<Rigidbody2D> bodies)
        {
            _data = data;
            _bodies = bodies;

            float radians = data.Angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Power 는 가속도다. 스텝마다 같은 속도를 더하면
            // 물리 간격을 바꿀 때 세기가 함께 변한다.
            _push = direction * (data.Power * SimWorld.FixedDt);
        }

        /// <summary>
        /// 늘 false 다. 바람은 끝나지 않아서, true 로 두면
        /// 공이 멈춰도 Stalled 가 안 나 상한까지 태운다.
        /// </summary>
        public bool HasPendingWork => false;

        public void Tick(int step, System.Random rng)
        {
            float radius = Mathf.Max(_data.Radius, 1e-4f);
            float sqrRadius = radius * radius;

            // 등록 순서 그대로 훑는다.
            // 힘을 받는 순서가 곧 합산 순서다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null) continue;
                if (body.bodyType != RigidbodyType2D.Dynamic) continue;

                if ((body.position - _data.Position).sqrMagnitude > sqrRadius) continue;

                // Physics2D 는 힘을 줘도 스스로 깨지 않는다.
                body.WakeUp();

                // 거리에 따라 약해지지 않는다. 구역 안이면 같은 세기다 —
                // 경계가 뚜렷해야 레벨을 읽을 수 있다.
                body.linearVelocity += _push;
            }
        }
    }
}
