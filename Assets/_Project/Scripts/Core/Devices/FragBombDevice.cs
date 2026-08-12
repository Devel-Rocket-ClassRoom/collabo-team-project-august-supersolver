using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 터져서 파편을 뿌린다. 닿으면 실패다.
    /// 파편을 Hazards 에 등록만 하고
    /// 판정은 Judge 가 매 스텝 질의한다.
    /// </summary>
    public sealed class FragBombDevice : IStepLogic
    {
        /// 두 장치가 서로의 상수에 얽히지 않게 따로 둔다.
        public const float BodyRadius = 0.28f;

        public const float FragmentRadius = 0.09f;

        /// 레벨마다 바꿀 이유가 생기면 데이터로 올린다.
        public const int FragmentCount = 5;

        /// <summary>
        /// 파편 수명. 60 스텝이 1 초다.
        /// 상한(DefaultMaxSteps 1800)에 비하면 여전히 짧아야 한다 —
        /// 파편이 굴러다니는 동안은 Stalled 가 나지 않아
        /// 실패 시도마다 그만큼 시뮬레이션을 더 태운다.
        /// </summary>
        public const int LifeSteps = 300;

        /// 완전 균등이면 기계적으로 보인다.
        const float SpreadJitter = 0.35f;

        readonly DeviceData _data;
        readonly Scene _scene;
        readonly string _name;
        readonly List<Rigidbody2D> _bodies;
        readonly List<Collider2D> _hazards;
        readonly List<Rigidbody2D> _fragments = new List<Rigidbody2D>(FragmentCount);

        readonly SimEvents _events;

        /// 레벨의 장치 번호. 알릴 때 누구인지 밝힌다.
        readonly int _index;

        Rigidbody2D _body;

        /// 아직 안 뽑았으면 -1. 첫 Tick 에서 정한다.
        int _fireStep = -1;
        bool _fired;

        /// 파편을 걷어낼 스텝. 폭발 때 정해진다.
        int _expireStep;

        /// <summary>정적 바디. BombDevice 와 같은 이유다.</summary>
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

        public FragBombDevice(
            in DeviceData data,
            Rigidbody2D body,
            Scene scene,
            string name,
            List<Rigidbody2D> bodies,
            List<Collider2D> hazards,
            SimEvents events,
            int index)
        {
            _data = data;
            _body = body;
            _scene = scene;
            _name = name;
            _bodies = bodies;
            _hazards = hazards;
            _events = events;
            _index = index;
        }

        /// <summary>
        /// 파편이 살아 있는 동안은 세지 않는다.
        /// 움직이면 어차피 sleep 이 안 되고,
        /// 다 잠들었으면 Stalled 가 맞다.
        /// </summary>
        public bool HasPendingWork => !_fired;

        public void Tick(int step, System.Random rng)
        {
            if (_fired)
            {
                ExpireFragments(step);
                return;
            }

            // 발동 스텝은 첫 Tick 에서 뽑는다.
            // 그래야 난수 소비 순서가
            // 스텝 × 장치 등록 순서로 고정된다.
            if (_fireStep < 0)
                _fireStep = _data.DelaySteps + (_data.JitterSteps > 0 ? rng.Next(_data.JitterSteps) : 0);

            if (step < _fireStep) return;

            Explode(step, rng);
            _fired = true;
        }

        void Explode(int step, System.Random rng)
        {
            _expireStep = step + LifeSteps;

            float speed = Mathf.Max(_data.Power, 0f);

            // 기준 각도 하나를 뽑아 균등하게 벌린다.
            // 파편마다 무작위면 한쪽으로 몰리는 판이 나온다.
            float baseAngle = (float)rng.NextDouble() * Mathf.PI * 2f;

            // 몸체 밖에서 시작한다. 겹쳐 놓으면
            // 서로 파고든 채로 첫 스텝을 맞는다.
            float spawnRadius = BodyRadius + FragmentRadius + 0.02f;

            for (int i = 0; i < FragmentCount; i++)
            {
                // 뽑는 순서가 곧 난수 소비 순서다.
                float jitter = ((float)rng.NextDouble() - 0.5f) * SpreadJitter;
                float angle = baseAngle + i * (Mathf.PI * 2f / FragmentCount) + jitter;

                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                var fragment = CreateFragment(
                    _scene, _data.Position + direction * spawnRadius, $"{_name}_Frag{i}");
                fragment.linearVelocity = direction * speed;

                // 뒤에만 붙인다. 끼우면 뒤 인덱스가 밀린다.
                _bodies.Add(fragment);
                _hazards.Add(fragment.GetComponent<Collider2D>());
                _fragments.Add(fragment);
            }

            // 파편을 다 뿌린 뒤, 몸을 지우기 전에 알린다.
            _events?.RaiseDeviceFired(_index);

            DestroyBody();
        }

        static Rigidbody2D CreateFragment(Scene scene, Vector2 position, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;

            // 작고 빨라서 이산 검출로는 공을 통과한다.
            // 통과하면 Judge 의 접촉 질의도 놓친다.
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = FragmentRadius;

            return body;
        }

        /// <summary>수명이 다한 파편을 걷어낸다.</summary>
        void ExpireFragments(int step)
        {
            if (_fragments.Count == 0 || step < _expireStep) return;

            for (int i = 0; i < _fragments.Count; i++)
            {
                var fragment = _fragments[i];
                if (fragment != null) Object.DestroyImmediate(fragment.gameObject);
            }

            // _bodies·_hazards 는 그대로 둔다. 인덱스가 밀린다.
            _fragments.Clear();
        }

        /// <summary>
        /// DestroyImmediate 여야 한다. 지연 파괴는
        /// 사라지는 스텝이 프레임 경계를 타서
        /// 프레임 독립성이 깨진다.
        /// </summary>
        void DestroyBody()
        {
            if (_body == null) return;

            Object.DestroyImmediate(_body.gameObject);
            _body = null;
        }
    }
}
