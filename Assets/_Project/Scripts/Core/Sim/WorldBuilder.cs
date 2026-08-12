using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 격리 물리 씬에 월드를 구축한다.
    /// 등록 순서가 이 클래스의 존재 이유다 —
    /// Box2D 는 바디 순서에 결과가 달라진다.
    /// </summary>
    public static class WorldBuilder
    {
        static int _sceneCounter;

        /// <summary>
        /// 스테이지 진입점.
        /// 시드는 스테이지가 들고 있다.
        /// </summary>
        public static SimWorld Build(StageData stage, Solution solution)
        {
            if (stage == null) throw new System.ArgumentNullException(nameof(stage));
            return Build(stage.Level, solution, stage.Seed);
        }

        /// <summary>
        /// 시드를 직접 주는 저수준 진입점.
        /// 시드를 축으로 훑는 곳만 쓴다.
        /// </summary>
        /// <param name="start">공의 출발 상태. null 이면 레벨의 시작점에 정지로 둔다.</param>
        public static SimWorld Build(
            LevelData level, Solution solution, int seed, BallState? start = null)
        {
            if (level == null) throw new System.ArgumentNullException(nameof(level));
            solution = solution ?? Solution.Empty;

            var scene = SceneManager.CreateScene(
                $"PPS_Sim_{_sceneCounter++}",
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));

            var physics = scene.GetPhysicsScene2D();

            var bodies = new List<Rigidbody2D>();
            var logics = new List<IStepLogic>();

            // 장치가 도중에 파편을 덧붙이므로 살아 있는 참조로 넘긴다.
            var hazards = new List<Collider2D>();

            // 1. 공 — 항상 인덱스 0. 해시를 읽을 기준점.
            //    속도는 바디가 생긴 뒤에만 줄 수 있고,
            //    Judge 가 초기 상태를 재기 전이어야 한다.
            var ball = ColliderFactory.CreateBall(
                scene, level, start?.Position ?? level.BallStart, "Ball");
            if (start.HasValue) ball.linearVelocity = start.Value.Velocity;
            bodies.Add(ball);

            // 2. 지형 — 레벨 데이터 순서
            if (level.Terrain != null)
            {
                for (int i = 0; i < level.Terrain.Count; i++)
                    bodies.Add(ColliderFactory.CreateSegment(scene, level.Terrain[i], $"Terrain_{i}"));
            }

            // 3. 장치 — 레벨 데이터 순서.
            //    지형 뒤·스트로크 앞이라는 것이 중요하다.
            //    레벨이 만드는 것과 유저가 그리는 것의 경계다.
            if (level.Devices != null)
            {
                for (int i = 0; i < level.Devices.Count; i++)
                    logics.Add(DeviceFactory.Create(level.Devices[i], i, scene, bodies, hazards));
            }

            // 4. 스트로크 — 솔루션 리스트 순서.
            //    유저가 그렸든 솔버가 전개했든 구분하지 않는다.
            var strokeBodies = new List<Rigidbody2D>();
            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                var body = ColliderFactory.CreateStroke(scene, solution.Strokes[i], $"Stroke_{i}");
                strokeBodies.Add(body);
                if (body != null) bodies.Add(body);
            }

            // 5. 회전축 — 스트로크가 전부 선 뒤에 연결한다.
            //    상대를 기다리는 핀은 물리에 없다.
            for (int i = 0; i < solution.Pivots.Count; i++)
            {
                if (!solution.Pivots[i].IsComplete) continue;
                CreatePivot(solution.Pivots[i], strokeBodies);
            }

            var judge = new Judge();
            var world = new SimWorld(
                scene, physics, level, seed,
                ball, ball.GetComponent<Collider2D>(),
                bodies, strokeBodies, hazards, logics, judge);
            return world;
        }

        static void CreatePivot(in PivotJoint pivot, List<Rigidbody2D> strokeBodies)
        {
            Rigidbody2D a = Resolve(pivot.StrokeA, strokeBodies);
            Rigidbody2D b = Resolve(pivot.StrokeB, strokeBodies);

            // 조인트를 붙일 쪽은 동적 바디여야 한다.
            Rigidbody2D host = PickHost(a, b);
            if (host == null) return;

            Rigidbody2D other = ReferenceEquals(host, a) ? b : a;

            var joint = host.gameObject.AddComponent<HingeJoint2D>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = other;
            joint.anchor = host.transform.InverseTransformPoint(pivot.Anchor);
            joint.connectedAnchor = other == null
                ? pivot.Anchor
                : (Vector2)other.transform.InverseTransformPoint(pivot.Anchor);
        }

        static Rigidbody2D Resolve(int index, List<Rigidbody2D> strokeBodies)
        {
            if (index == PivotJoint.WorldIndex) return null;
            if (index < 0 || index >= strokeBodies.Count) return null;
            return strokeBodies[index];
        }

        static Rigidbody2D PickHost(Rigidbody2D a, Rigidbody2D b)
        {
            if (a != null && a.bodyType == RigidbodyType2D.Dynamic) return a;
            if (b != null && b.bodyType == RigidbodyType2D.Dynamic) return b;
            return null;
        }
    }
}
