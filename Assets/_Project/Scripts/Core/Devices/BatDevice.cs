using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 향한 쪽으로 계속 날아간다. 막히면 사라진다.
    /// 막혔는지는 접촉이 아니라 어긋남으로 본다 —
    /// 밀어낼 수 있는 것은 밀고 지나가야 하기 때문이다.
    /// </summary>
    public sealed class BatDevice : IStepLogic
    {
        /// 크기를 안 준 레벨도 보이기는 해야 한다.
        public const float MinRadius = 0.1f;

        /// <summary>
        /// 그리는 반지름에서 콜라이더로 줄이는 비율.
        /// 그림 둘레에 투명한 여백이 있어, 그대로 쓰면
        /// 보이는 박쥐보다 넓은 자리에서 부딪힌다.
        /// </summary>
        const float ColliderScale = 5f / 8f;

        /// <summary>
        /// 이만큼의 시간만큼 뒤처지면 막힌 것으로 본다.
        /// 넉넉하게 잡는다 — 무거운 것을 끌거나 잠깐 걸린
        /// 박쥐까지 죽으면 밀고 가는 쓰임새가 사라진다.
        /// 거리로 고정하지 않는 것은 느린 박쥐와 빠른 박쥐가
        /// 같은 시간만큼 버티게 하기 위해서다.
        /// </summary>
        const float StuckSeconds = 5f;

        readonly BatData _data;

        /// 매 스텝 되돌려 놓는 속도. 부딪혀도 느려지지 않는다.
        readonly Vector2 _velocity;

        /// 아무것도 막지 않았을 때 있어야 할 자리의 기준점.
        readonly Vector2 _start;

        /// <summary>
        /// 판이 품는 자리. 여기를 벗어난 박쥐는 곧게 날아
        /// 돌아올 수 없다. 남겨 두면 잠들지 않아 판정이
        /// 영영 안 난다.
        /// </summary>
        readonly Rect _area;

        Rigidbody2D _body;

        /// <summary>
        /// 동적 바디다. 막히는 것이 곧 사라지는 조건이라
        /// 충돌에 영향받지 않는 운동학 바디로는 판정이 안 선다.
        /// </summary>
        public static Rigidbody2D CreateBody(Scene scene, BatData data, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(
                data.Position, Quaternion.Euler(0f, 0f, data.Angle));

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;

            // 제 힘으로 나는 것이라 떨어지지 않는다.
            body.gravityScale = 0f;

            // 돌아가면 향한 쪽과 그림이 어긋난다.
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 작고 빨라서 이산 검출로는 얇은 지형을 통과한다.
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = data.DrawRadius * ColliderScale;

            return body;
        }

        /// <summary>
        /// 몸은 위험하지 않다. 닿아도 실패가 아니라 밀린다.
        /// </summary>
        public static IStepLogic Build(IDeviceData data, in DeviceBuildContext ctx)
        {
            var bat = (BatData)data;

            var body = CreateBody(ctx.Scene, bat, ctx.Name);
            ctx.Bodies.Add(body);

            return new BatDevice(bat, body, LevelDataArea.Calculate(ctx.Level));
        }

        BatDevice(BatData data, Rigidbody2D body, Rect area)
        {
            _data = data;
            _body = body;
            _area = area;
            _start = data.Position;

            float radians = data.Angle * Mathf.Deg2Rad;
            _velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * data.Speed;
        }

        /// <summary>
        /// 늘 false 다. 박쥐는 끝나지 않아서, true 로 두면
        /// 공이 멈춰도 Stalled 가 안 나 상한까지 태운다.
        /// </summary>
        public bool HasPendingWork => false;

        public void Tick(int step, System.Random rng)
        {
            if (_body == null) return;

            if (Vector2.Distance(_body.position, PredictedAt(step)) > StuckDistance
                || !_area.Contains(_body.position))
            {
                DestroyBody();
                return;
            }

            // Physics2D 는 속도를 줘도 스스로 깨지 않는다.
            _body.WakeUp();

            // 더하지 않고 덮어쓴다. 부딪혀 먹힌 속도를
            // 되돌려 놓아야 같은 빠르기로 계속 난다.
            _body.linearVelocity = _velocity;
        }

        /// 아무것도 막지 않았다면 이 스텝에 있어야 할 자리.
        Vector2 PredictedAt(int step) => _start + _velocity * (step * SimWorld.FixedDt);

        /// <summary>
        /// 막혔다고 볼 어긋남. 속도가 0 이면 비례값도 0 이라
        /// 몸 크기를 바닥으로 깐다.
        /// </summary>
        float StuckDistance => Mathf.Max(_data.Speed * StuckSeconds, _data.DrawRadius);

        /// <summary>
        /// DestroyImmediate 여야 한다. 지연 파괴는
        /// 사라지는 스텝이 프레임 경계를 타서
        /// 프레임 독립성이 깨진다.
        /// </summary>
        void DestroyBody()
        {
            // 자리는 지우기 전에 뜬다. 출발점을 알리면
            // 날아간 거리만큼 엉뚱한 곳에서 터진다.
            Vector2 at = _body.position;

            // 목록에서는 빼지 않는다. 빼면 뒤 인덱스가 밀린다.
            Object.DestroyImmediate(_body.gameObject);
            _body = null;

            SimSignals.Trigger(DeviceType.Bat, at);
        }
    }
}
