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

        /// <summary>
        /// 자유 물체 스트로크의 두께 절반. **0 일 수 없다.**
        ///
        /// Box2D 는 edge ↔ edge 충돌을 만들지 않는다. 지형도 고정선도 EdgeCollider2D 이므로,
        /// 자유 물체까지 edge 로 두면 공(원) 말고는 아무것과도 부딪히지 않는다 —
        /// 지형을 그대로 통과해 떨어진다. 그래서 동적 스트로크만 면적을 가진 형태로 만든다.
        ///
        /// 정적 스트로크는 edge 로 둔다. 상대가 원이거나 다각형이면 충돌이 성립하고,
        /// 바꾸면 기존 레벨의 공-지형 해시 기준선이 통째로 흔들린다.
        /// </summary>
        public const float FreeBodyHalfWidth = 0.06f;

        /// <summary>
        /// 관성의 하한. 0 이면 Box2D 가 각속도 계산에서 0 으로 나누어 NaN 을 만들고,
        /// 그 NaN 은 해시를 타고 번져 원인 추적이 불가능해진다.
        /// 정상 스트로크에서는 이 값에 걸리지 않으므로 결과에 영향을 주지 않는다.
        /// </summary>
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

                // 콜라이더에 면적이 생겼지만 질량은 계속 **철사 모델**로 준다.
                // 두께는 충돌을 성립시키기 위한 최소값이지 물리적 굵기가 아니다.
                // 면적으로 질량을 잡으면 두께 상수를 건드릴 때마다 모든 레벨의 물리가 바뀐다.
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
        /// 폴리라인을 선분마다 사각형으로 부풀려 <see cref="PolygonCollider2D"/> 의 경로로 넣는다.
        ///
        /// 자식 오브젝트를 만들지 않는 것이 요점이다. 바디 하나에 경로 여러 개면
        /// 등록 순서가 리스트 순서 하나로 정해지고, 오브젝트가 늘지 않아 해시 순서도 그대로다.
        ///
        /// 각 선분의 양 끝을 반 두께만큼 늘여 이웃 사각형과 겹치게 한다. 겹치지 않으면
        /// 꺾이는 지점에 틈이 생겨 다른 물체의 모서리가 끼어든다.
        /// </summary>
        static void AddThickCollider(GameObject go, Vector2[] points)
        {
            var paths = new List<Vector2[]>(Mathf.Max(points.Length - 1, 1));

            for (int i = 1; i < points.Length; i++)
            {
                Vector2 a = points[i - 1];
                Vector2 delta = points[i] - a;
                float segment = delta.magnitude;
                if (segment <= 0f) continue;   // 겹친 점. 넓이 0 인 경로는 Box2D 가 거부한다.

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

        /// <summary>
        /// 폴리라인을 **균일한 굵기의 철사**로 보고 무게중심과 관성을 구한다.
        ///
        /// 이전 구현은 무게중심을 포인트의 산술 평균으로, 관성을 곧은 막대 공식 <c>m·L²/12</c> 로
        /// 잡았다. 둘 다 **2점 직선에서만 맞는 값**이다. 폴리라인에서는
        /// - 산술 평균이 점이 촘촘한 구간 쪽으로 끌려간다 → 같은 곡선도 샘플링 밀도에 따라 무게중심이 달라진다
        /// - 막대 공식은 구부러진 형상에 맞지 않는다 → 둘레 L 인 원이면 실제값(<c>m·L²/4π²</c>)의 약 3.3배
        ///
        /// 프리미티브(경사면·그릇·바퀴)가 곡선을 전개하기 시작하면 이 오차가 그대로 물리 결과가 된다.
        ///
        /// **길이 비중을 먼저 정규화(<c>li/total</c>)하는 것이 중요하다.** 선분이 하나뿐일 때
        /// 비중이 정확히 1.0 이 되어 <c>mid × 1.0 = mid</c>, <c>d = 0</c> 이 되고, 결과가
        /// 이전 구현과 **비트 단위로 같아진다**. 기존 직선 레벨의 해시 기준선이 흔들리지 않는다.
        /// </summary>
        /// <param name="origin">바디의 로컬 원점(= 포인트 산술 평균). 무게중심을 로컬로 돌려줄 기준.</param>
        static void ComputeWireMassProperties(
            List<Vector2> points, Vector2 origin, float mass,
            out Vector2 localCenterOfMass, out float inertia)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
                total += Vector2.Distance(points[i - 1], points[i]);

            if (total <= 0f)
            {
                // 모든 점이 겹친 퇴화 스트로크. 물리적으로 의미가 없지만 관성 0 은 NaN 을 부른다.
                localCenterOfMass = Vector2.zero;
                inertia = MinInertia;
                return;
            }

            // 1) 무게중심 — 각 선분의 중점을 길이 비중으로 평균낸다.
            Vector2 centerOfMass = Vector2.zero;
            for (int i = 1; i < points.Count; i++)
            {
                float segment = Vector2.Distance(points[i - 1], points[i]);
                if (segment <= 0f) continue;

                Vector2 mid = (points[i - 1] + points[i]) * 0.5f;
                centerOfMass += mid * (segment / total);
            }

            // 2) 관성 — 선분마다 "자체 관성 + 평행축 이동"을 더한다.
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
