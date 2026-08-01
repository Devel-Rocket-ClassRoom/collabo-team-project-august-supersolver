using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// <see cref="LevelData"/> + <see cref="Solution"/> → 격리 물리 씬에 월드를 구축한다.
    ///
    /// **등록 순서가 이 클래스의 존재 이유다.** Box2D 는 바디 등록 순서에 따라
    /// 아일랜드 구성과 제약 해결 순서가 달라지고, 그 결과 부동소수점 합산 순서가 바뀐다.
    /// 같은 입력이면 항상 같은 순서로 만들어야 같은 결과가 나온다.
    ///
    /// 고정된 순서: 공 → 지형(레벨 데이터 순서) → 장치(레벨 데이터 순서) → 스트로크(리스트 순서) → 회전축(리스트 순서)
    ///
    /// 오브젝트 풀링을 쓰지 않는 것도 같은 이유다. 풀 반납 순서가 다음 시도의
    /// 생성 순서를 오염시켜 "같은 입력, 다른 결과"를 만든다. 시도마다 새로 만든다.
    /// </summary>
    public static class WorldBuilder
    {
        static int _sceneCounter;

        public static SimWorld Build(LevelData level, Solution solution, int seed)
        {
            if (level == null) throw new System.ArgumentNullException(nameof(level));
            solution = solution ?? Solution.Empty;

            var scene = SceneManager.CreateScene(
                $"PPS_Sim_{_sceneCounter++}",
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));

            var physics = scene.GetPhysicsScene2D();

            var bodies = new List<Rigidbody2D>();
            var logics = new List<IStepLogic>();

            // 1. 공 — 항상 인덱스 0. 해시 덤프를 읽을 때 기준점이 고정되어 있으면 디버깅이 쉽다.
            var ball = ColliderFactory.CreateBall(scene, level, "Ball");
            bodies.Add(ball);

            // 2. 지형 — 레벨 데이터 순서
            if (level.Terrain != null)
            {
                for (int i = 0; i < level.Terrain.Count; i++)
                    bodies.Add(ColliderFactory.CreateSegment(scene, level.Terrain[i], $"Terrain_{i}"));
            }

            // 3. 장치 — 레벨 데이터 순서. 장치는 바디를 만들지 않으므로
            //    바디 인덱스(= 해시 순서)에는 영향이 없다.
            //    bodies 를 살아 있는 참조로 넘긴다 — 장치는 스트로크보다 먼저 등록되지만
            //    Tick 이 도는 시점에는 스트로크까지 전부 들어와 있어야 한다.
            if (level.Devices != null)
            {
                for (int i = 0; i < level.Devices.Count; i++)
                    logics.Add(DeviceFactory.Create(level.Devices[i], bodies));
            }

            // 4. 스트로크 — 솔루션 리스트 순서.
            //    유저가 그린 것이든 솔버가 전개한 것이든 여기서부터는 구분되지 않는다.
            int strokeBodyOffset = bodies.Count;
            var strokeBodies = new List<Rigidbody2D>();
            for (int i = 0; i < solution.Strokes.Count; i++)
            {
                var body = ColliderFactory.CreateStroke(scene, solution.Strokes[i], $"Stroke_{i}");
                strokeBodies.Add(body);
                if (body != null) bodies.Add(body);
            }

            // 5. 회전축 — 리스트 순서. 스트로크가 전부 만들어진 뒤에 연결한다.
            for (int i = 0; i < solution.Pivots.Count; i++)
                CreatePivot(solution.Pivots[i], strokeBodies);

            var judge = new Judge();
            var world = new SimWorld(scene, physics, level, seed, ball, bodies, logics, judge);
            return world;
        }

        static void CreatePivot(in PivotJoint pivot, List<Rigidbody2D> strokeBodies)
        {
            Rigidbody2D a = Resolve(pivot.StrokeA, strokeBodies);
            Rigidbody2D b = Resolve(pivot.StrokeB, strokeBodies);

            // 조인트를 붙일 쪽은 동적 바디여야 한다. 둘 다 없거나 둘 다 정적이면 회전축이 의미가 없다.
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
