using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// <see cref="Stroke"/> 를 물리 바디로 만든다. <see cref="WorldBuilder"/> 내부에서만 호출된다.
    ///
    /// 설계서의 시그니처는 <c>Create(PhysicsScene2D, Stroke)</c> 였으나 실제로는 Scene 핸들이 필요하다.
    /// GameObject 를 격리 씬으로 옮기는 것은 Scene 단위 API 이고, PhysicsScene2D 로는 할 수 없다.
    ///
    /// 컴포넌트를 붙이는 순서가 고정되어 있는 것이 중요하다.
    /// transform 을 먼저 놓고 Rigidbody2D 를 나중에 붙여야 바디가 올바른 위치에서 등록된다
    /// (autoSyncTransforms 가 꺼져 있으므로 나중에 transform 을 옮겨도 바디는 따라오지 않는다).
    /// </summary>
    public static class ColliderFactory
    {
        /// FreeBody 의 단위 길이당 질량. M01 임시값.
        public const float FreeBodyMassPerUnit = 1f;

        public static Rigidbody2D CreateStroke(Scene scene, in Stroke stroke, string name)
        {
            if (!stroke.IsValid) return null;

            Vector2 centroid = Centroid(stroke.Points);

            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = centroid;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = stroke.Tool == ToolType.FixedLine
                ? RigidbodyType2D.Static
                : RigidbodyType2D.Dynamic;
            body.sleepMode = RigidbodySleepMode2D.StartAwake;

            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = ToLocalPoints(stroke.Points, centroid);

            if (body.bodyType == RigidbodyType2D.Dynamic)
            {
                // EdgeCollider2D 는 면적이 없어 자동 질량이 0 이 된다. 명시적으로 넣는다.
                // M03 에서 프리미티브가 닫힌 형태를 요구할 때 다각형 분할로 교체될 자리.
                float length = Mathf.Max(stroke.Length(), 0.01f);
                body.useAutoMass = false;
                body.mass = length * FreeBodyMassPerUnit;
                body.centerOfMass = Vector2.zero;
                body.inertia = body.mass * length * length / 12f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            return body;
        }

        public static Rigidbody2D CreateSegment(Scene scene, in StaticSegment segment, string name)
        {
            Vector2 centroid = (segment.A + segment.B) * 0.5f;

            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = centroid;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[] { segment.A - centroid, segment.B - centroid };

            return body;
        }

        public static Rigidbody2D CreateBall(Scene scene, LevelData level, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = level.BallStart;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.sleepMode = RigidbodySleepMode2D.StartAwake;
            // 빠르게 떨어지는 공이 얇은 선을 뚫고 지나가면 "풀리는 레벨"이 실패로 판정된다.
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = level.BallRadius;

            return body;
        }

        static Vector2 Centroid(List<Vector2> points)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Count; i++) sum += points[i];
            return sum / points.Count;
        }

        static Vector2[] ToLocalPoints(List<Vector2> points, Vector2 origin)
        {
            var result = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++) result[i] = points[i] - origin;
            return result;
        }
    }
}
