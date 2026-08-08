using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// Stroke 를 물리 바디로 만든다.
    /// transform 을 먼저 놓고 Rigidbody2D 를
    /// 나중에 붙여야 위치가 맞는다.
    /// </summary>
    public static class ColliderFactory
    {
        /// FreeBody 의 단위 길이당 질량. 임시값.
        public const float FreeBodyMassPerUnit = 1f;

        /// <summary>
        /// 자유 물체 두께의 절반. 0 일 수 없다.
        /// Box2D 가 edge ↔ edge 충돌을 안 만들어
        /// edge 로 두면 지형을 통과한다.
        /// </summary>
        public const float FreeBodyHalfWidth = 0.06f;

        /// 0 이면 각속도 계산에서 NaN 이 난다.
        const float MinInertia = 1e-6f;

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

            Vector2[] local = ToLocalPoints(stroke.Points, centroid);

            if (body.bodyType == RigidbodyType2D.Dynamic)
            {
                AddThickCollider(go, local);

                // 면적이 생겨도 질량은 철사 모델로 준다.
                // 면적으로 잡으면 두께 상수를 건드릴 때마다
                // 모든 레벨의 물리가 바뀐다.
                float length = Mathf.Max(stroke.Length(), 0.01f);
                body.useAutoMass = false;
                body.mass = length * FreeBodyMassPerUnit;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                ComputeWireMassProperties(stroke.Points, centroid, body.mass,
                    out Vector2 localCenterOfMass, out float inertia);

                body.centerOfMass = localCenterOfMass;
                body.inertia = inertia;
            }
            else
            {
                var edge = go.AddComponent<EdgeCollider2D>();
                edge.points = local;
            }

            return body;
        }

        /// <summary>
        /// 선분마다 사각형으로 부풀려 경로로 넣는다.
        /// 자식을 안 만들어야 해시 순서가 그대로다.
        /// 양 끝을 늘여 꺾이는 지점의 틈을 막는다.
        /// </summary>
        static void AddThickCollider(GameObject go, Vector2[] points)
        {
            var paths = new List<Vector2[]>(Mathf.Max(points.Length - 1, 1));

            for (int i = 1; i < points.Length; i++)
            {
                Vector2 a = points[i - 1];
                Vector2 delta = points[i] - a;
                float segment = delta.magnitude;
                if (segment <= 0f) continue;   // 넓이 0 은 Box2D 가 거부한다.

                Vector2 direction = delta / segment;
                Vector2 normal = new Vector2(-direction.y, direction.x) * FreeBodyHalfWidth;
                Vector2 cap = direction * FreeBodyHalfWidth;

                Vector2 start = a - cap;
                Vector2 end = points[i] + cap;

                paths.Add(new[] { start - normal, start + normal, end + normal, end - normal });
            }

            var poly = go.AddComponent<PolygonCollider2D>();
            poly.pathCount = paths.Count;
            for (int i = 0; i < paths.Count; i++) poly.SetPath(i, paths[i]);
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

        /// <param name="position">출발 위치. 레벨의 시작점과 다를 수 있다.</param>
        public static Rigidbody2D CreateBall(Scene scene, LevelData level, Vector2 position, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.sleepMode = RigidbodySleepMode2D.StartAwake;

            // 얇은 선을 뚫으면 풀리는 레벨이 실패가 된다.
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = level.BallRadius;

            return body;
        }

        /// <summary>
        /// 폴리라인을 균일한 철사로 보고
        /// 무게중심과 관성을 구한다.
        /// 산술 평균·막대 공식은 직선에서만 맞다.
        /// </summary>
        /// <param name="origin">바디의 로컬 원점.</param>
        static void ComputeWireMassProperties(
            List<Vector2> points, Vector2 origin, float mass,
            out Vector2 localCenterOfMass, out float inertia)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
                total += Vector2.Distance(points[i - 1], points[i]);

            if (total <= 0f)
            {
                // 모든 점이 겹친 퇴화 스트로크.
                localCenterOfMass = Vector2.zero;
                inertia = MinInertia;
                return;
            }

            // 1) 무게중심 — 선분 중점을 길이 비중으로 평균.
            Vector2 centerOfMass = Vector2.zero;
            for (int i = 1; i < points.Count; i++)
            {
                float segment = Vector2.Distance(points[i - 1], points[i]);
                if (segment <= 0f) continue;

                Vector2 mid = (points[i - 1] + points[i]) * 0.5f;
                centerOfMass += mid * (segment / total);
            }

            // 2) 관성 — 자체 관성 + 평행축 이동.
            float sum = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                float segment = Vector2.Distance(points[i - 1], points[i]);
                if (segment <= 0f) continue;

                float segmentMass = mass * (segment / total);
                Vector2 mid = (points[i - 1] + points[i]) * 0.5f;

                sum += segmentMass * segment * segment / 12f
                     + segmentMass * (mid - centerOfMass).sqrMagnitude;
            }

            localCenterOfMass = centerOfMass - origin;
            inertia = Mathf.Max(sum, MinInertia);
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
