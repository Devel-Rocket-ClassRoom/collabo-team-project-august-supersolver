using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 붙박이 장애물. 닿으면 실패다.
    /// 스스로 하는 일이 없어 Tick 이 비어 있다 —
    /// 판정은 Judge 가 Hazards 를 훑어 한다.
    /// </summary>
    public sealed class SpikeDevice : IStepLogic
    {
        /// 크기를 안 준 레벨도 보이기는 해야 한다.
        public const float MinRadius = 0.1f;

        /// <summary>
        /// 정적 바디다. 공이 위에 올라타도 밀리지 않는다.
        /// </summary>
        public static Rigidbody2D CreateBody(Scene scene, in DeviceData data, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = data.Position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = Mathf.Max(data.Radius, MinRadius);

            return body;
        }

        public void Tick(int step, System.Random rng)
        {
        }

        /// <summary>
        /// 늘 false 다. true 면 공이 멈춰도 Stalled 가 안 나서
        /// 실패 시도마다 상한까지 시뮬레이션을 태운다.
        /// </summary>
        public bool HasPendingWork => false;
    }
}
