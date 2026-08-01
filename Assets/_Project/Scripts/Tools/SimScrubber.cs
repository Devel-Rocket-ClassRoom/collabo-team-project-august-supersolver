using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

#if UNITY_EDITOR
// 테스트 레벨은 PPS.Core.TestFixtures 어셈블리에 있고 UNITY_INCLUDE_TESTS 제약이 걸려 있어
// 빌드에는 들어가지 않는다. 에디터에서만 참조한다.
using PPS.Core.Tests;
#endif

namespace PPS.Tools
{
    /// <summary>
    /// 임시 디버그 뷰어. 슬라이더로 스텝을 지정하면 그 스텝의 시뮬 상태를 그린다.
    ///
    /// **뒤로 가는 것은 되감기가 아니라 재구축이다.** 목표 스텝이 현재보다 앞이면
    /// 월드를 통째로 버리고 0 부터 다시 돌린다. 물리 엔진 내부 상태(접촉·슬립)는
    /// 되돌릴 수 없으므로 되감기로는 같은 상태를 재현할 수 없다.
    /// 재구축이 같은 결과를 낸다는 것은 결정론 테스트가 증명한 사실이라 이대로 성립한다.
    ///
    /// 에디터에서는 **테스트가 쓰는 레벨을 그대로 골라 볼 수 있다.** 테스트가 빨간불일 때
    /// 그 레벨에서 실제로 무슨 일이 벌어지는지 눈으로 확인하는 것이 이 도구의 주 용도다.
    /// 테스트 레벨에는 폭탄을 얹지 않는다 — 테스트가 도는 것과 다른 시뮬을 보여주면 의미가 없다.
    ///
    /// 뷰어 기본 레벨에만 폭탄이 붙어 있다. 장치가 있어야 rng 가 소비되고, 그래야 시드가
    /// 결과에 닿는 것을 눈으로 볼 수 있다 — 시드 버튼으로 발동 스텝이 바뀌는 것을 확인할 수 있다.
    ///
    /// 씬의 아무 GameObject 에 붙이고 플레이하면 된다. 별도 세팅은 없다.
    /// 정식 기능이 아니라 눈으로 확인하기 위한 도구다.
    /// </summary>
    public sealed class SimScrubber : MonoBehaviour
    {
        const int MaxSteps = SimWorld.DefaultMaxSteps;
        const int CircleSegments = 28;

        // 밝은 배경(FFE6B3) 위에서 읽히도록 전부 어둡고 진한 색으로 잡았다.
        static readonly Color TerrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);   // 거의 검정
        static readonly Color FreeBodyColor = new Color32(0x1B, 0x4F, 0xA0, 0xFF);  // 진한 파랑
        static readonly Color BallColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);      // 크림슨
        static readonly Color GoalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);      // 진한 초록
        static readonly Color KillLineColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);  // 어두운 벽돌
        static readonly Color PivotColor = new Color32(0x0B, 0x6E, 0x6E, 0xFF);     // 진한 청록
        static readonly Color BombIdleColor = new Color32(0x6B, 0x3F, 0xA0, 0xFF);  // 진한 보라
        static readonly Color BombFiredColor = new Color32(0xC4, 0x00, 0x6B, 0xFF); // 진한 마젠타

        // 폭탄. 흔들림이 있어 시드를 바꾸면 발동 스텝이 달라진다 — 시드가 결과에 닿는 유일한 경로다.
        const int BombDelaySteps = 30;
        const int BombJitterSteps = 60;
        const float BombPower = 5f;
        static readonly Vector2 BombPosition = new Vector2(-2.5f, 1.6f);

        [SerializeField] int _seed;
        [SerializeField] bool _autoFitCamera = true;

        /// <summary>
        /// 선 두께를 화면 배율(직교 크기) 대비 비율로 잡는다. 월드 단위 고정값으로 두면
        /// LongRoll 처럼 넓은 레벨에서 실처럼 얇아지고, 좁은 레벨에서는 뭉개진다.
        /// </summary>
        [SerializeField] float _lineWidthRatio = 0.013f;

        [SerializeField] Color _background = new Color32(0xFF, 0xE6, 0xB3, 0xFF);

        SimWorld _world;
        Solution _solution;
        DebugBomb _bomb;
        int _targetStep;
        string _stepInput = "0";

        Entry[] _catalog;
        string[] _catalogNames;
        int _levelIndex;

        /// 실제로 그릴 때 쓰는 두께(월드 단위). <see cref="FitCamera"/> 가 배율에서 계산한다.
        float _lineWidth = 0.09f;

        Material _lineMaterial;

        void Start()
        {
            _catalog = BuildCatalog();
            _catalogNames = Array.ConvertAll(_catalog, e => e.Name);

            Rebuild();
            FitCamera();
        }

        void Update()
        {
            // 슬라이더 값 반영은 프레임당 한 번만 한다. OnGUI 에서 바로 전진시키면
            // IMGUI 가 한 프레임에 여러 번 도는 동안 월드가 여러 번 재구축된다.
            if (_targetStep < _world.CurrentStep) Rebuild();

            while (_world.CurrentStep < _targetStep && !_world.IsTerminal)
                _world.Step();
        }

        void OnDestroy()
        {
            _world?.Dispose();
            _world = null;

            if (_lineMaterial != null) Destroy(_lineMaterial);
        }

        void Rebuild()
        {
            _world?.Dispose();

            var entry = _catalog[_levelIndex];
            _solution = entry.MakeSolution() ?? new Solution();

            IStepLogic[] logics = null;
            if (entry.WithBomb)
            {
                // 로직도 매 재구축마다 새로 만든다. 발동 여부가 남아 있는 인스턴스를 재사용하면
                // 두 번째 재생부터 폭탄이 터지지 않아 같은 스텝이 다른 상태를 낸다.
                // 오브젝트 풀링을 금지하는 이유와 정확히 같다.
                _bomb = new DebugBomb(BombDelaySteps, BombJitterSteps, BombPower);
                logics = new IStepLogic[] { _bomb };
            }
            else
            {
                _bomb = null;
            }

            _world = WorldBuilder.Build(entry.MakeLevel(), _solution, _seed, logics);

            if (_bomb != null) _bomb.Target = _world.Ball;
        }

        void ApplySeed(int seed)
        {
            _seed = seed;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
        }

        void ApplyLevel(int index)
        {
            _levelIndex = index;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
            FitCamera();
        }

        // ── 표시 ──────────────────────────────────────────────────────────

        void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10f, 10f, 460f, 350f), GUI.skin.box);

            int picked = GUILayout.SelectionGrid(_levelIndex, _catalogNames, 2);
            if (picked != _levelIndex) ApplyLevel(picked);

            GUILayout.Space(6f);

            GUILayout.Label($"Step {_world.CurrentStep} / {MaxSteps}" +
                            (_world.IsTerminal ? "   (종료됨 — 더 진행하지 않는다)" : ""));

            _targetStep = Mathf.RoundToInt(GUILayout.HorizontalSlider(_targetStep, 0f, MaxSteps));

            GUILayout.BeginHorizontal();
            GUILayout.Label("직접 입력", GUILayout.Width(60f));
            _stepInput = GUILayout.TextField(_stepInput, GUILayout.Width(70f));
            if (GUILayout.Button("이동", GUILayout.Width(50f)) && int.TryParse(_stepInput, out int typed))
                _targetStep = Mathf.Clamp(typed, 0, MaxSteps);
            if (GUILayout.Button("처음으로", GUILayout.Width(70f)))
                _targetStep = 0;
            GUILayout.EndHorizontal();

            var result = _world.ToResult(_solution.TotalInk());
            GUILayout.Label($"{result.Outcome}   MinGoalDist {result.MinGoalDist:F3}   " +
                            $"Ball ({_world.Ball.position.x:F2}, {_world.Ball.position.y:F2})");

            // 같은 스텝으로 되돌아왔을 때 이 값이 같으면 재구축이 상태를 정확히 재현한 것이다.
            GUILayout.Label($"Hash 0x{WorldHasher.Hash(_world):X16}");

            if (_bomb == null)
            {
                // 장치가 없으면 rng 가 아무에게도 전달되지 않는다. 시드를 바꿔도 결과가 그대로다.
                GUILayout.Label("장치 없음 — 이 레벨에서는 시드가 결과에 영향을 주지 않는다");
            }
            else
            {
                GUILayout.Label(_bomb.FireStep < 0
                    ? "폭탄 — 아직 첫 Tick 전이라 발동 스텝 미정"
                    : $"폭탄 — 발동 스텝 {_bomb.FireStep} ({(_bomb.Fired ? "터짐" : "대기 중")})");
            }

            // 시드를 바꾸면 폭탄 발동 스텝이 달라진다. 바꾸는 즉시 재구축하고 처음으로 돌린다.
            GUILayout.BeginHorizontal();
            GUILayout.Label($"시드 {_seed}", GUILayout.Width(60f));
            if (GUILayout.Button("◀", GUILayout.Width(30f))) ApplySeed(_seed - 1);
            if (GUILayout.Button("▶", GUILayout.Width(30f))) ApplySeed(_seed + 1);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void OnRenderObject()
        {
            if (_world == null) return;

            EnsureMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();

            // GL.LINES 는 두께를 줄 수 없다 (드라이버가 항상 1px 로 그린다).
            // 두꺼운 선이 필요하면 선분마다 사각형을 하나씩 그리는 수밖에 없다.
            GL.Begin(GL.QUADS);

            DrawLevelMarkers();

            var bodies = _world.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null) continue;

                bool isBall = ReferenceEquals(body, _world.Ball);
                GL.Color(isBall ? BallColor
                       : body.bodyType == RigidbodyType2D.Static ? TerrainColor
                       : FreeBodyColor);

                DrawBody(body);
            }

            GL.End();
            GL.PopMatrix();
        }

        void DrawBody(Rigidbody2D body)
        {
            var edge = body.GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                var points = edge.points;
                for (int i = 0; i + 1 < points.Length; i++)
                {
                    Line(body.transform.TransformPoint(points[i]),
                         body.transform.TransformPoint(points[i + 1]));
                }
                return;
            }

            var circle = body.GetComponent<CircleCollider2D>();
            if (circle != null) Circle(body.position, circle.radius);
        }

        void DrawLevelMarkers()
        {
            var level = _world.Level;

            GL.Color(GoalColor);
            Circle(level.GoalPosition, level.GoalRadius);

            GL.Color(KillLineColor);
            Line(new Vector2(-30f, level.KillY), new Vector2(30f, level.KillY));

            // 회전축 표시. 바디가 아니라 조인트라 Bodies 를 훑는 것만으로는 보이지 않는다.
            GL.Color(PivotColor);
            for (int i = 0; i < _solution.Pivots.Count; i++)
                Circle(_solution.Pivots[i].Anchor, 0.12f);

            // 폭탄 표시. 대기 중이면 보라, 터지면 마젠타로 바뀐다.
            if (_bomb != null)
            {
                GL.Color(_bomb.Fired ? BombFiredColor : BombIdleColor);
                Circle(BombPosition, 0.35f);
                Line(BombPosition + new Vector2(-0.5f, 0f), BombPosition + new Vector2(0.5f, 0f));
                Line(BombPosition + new Vector2(0f, -0.5f), BombPosition + new Vector2(0f, 0.5f));
            }
        }

        /// <summary>선분 하나를 두께 <see cref="_lineWidth"/> 의 사각형으로 그린다.</summary>
        void Line(Vector2 a, Vector2 b)
        {
            Vector2 dir = b - a;
            float length = dir.magnitude;
            if (length < 1e-6f) return;

            // 선분에 수직인 방향으로 반 두께만큼 벌린다.
            Vector2 offset = new Vector2(-dir.y, dir.x) * (_lineWidth * 0.5f / length);

            GL.Vertex3(a.x - offset.x, a.y - offset.y, 0f);
            GL.Vertex3(a.x + offset.x, a.y + offset.y, 0f);
            GL.Vertex3(b.x + offset.x, b.y + offset.y, 0f);
            GL.Vertex3(b.x - offset.x, b.y - offset.y, 0f);
        }

        void Circle(Vector2 center, float radius)
        {
            Vector2 prev = center + new Vector2(radius, 0f);
            for (int i = 1; i <= CircleSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / CircleSegments;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Line(prev, next);
                prev = next;
            }
        }

        void EnsureMaterial()
        {
            if (_lineMaterial != null) return;

            // 프로젝트에 에셋을 추가하지 않으려고 내장 셰이더를 쓴다.
            _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _lineMaterial.SetFloat("_ZWrite", 0f);
            _lineMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        }

        /// <summary>
        /// 지형·공·스트로크가 다 들어오도록 카메라를 맞춘다. 레벨마다 크기가 크게 달라서
        /// (LongRoll 은 가로 40, 뷰어 기본은 12) 고정 배율로는 어느 한쪽이 화면 밖으로 나간다.
        ///
        /// 목표 위치는 계산에 넣지 않는다. 테스트 레벨은 "닿을 수 없는 목표"를 (50, 50) 같은
        /// 먼 곳에 두는 관례라, 그것까지 담으려 하면 화면이 쓸모없이 축소된다.
        /// </summary>
        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (_autoFitCamera) Frame(cam);

            // 두께는 카메라를 맞추든 말든 현재 배율을 따라간다.
            _lineWidth = Mathf.Max(cam.orthographicSize, 0.1f) * _lineWidthRatio;
        }

        void Frame(Camera cam)
        {
            var level = _world.Level;

            Vector2 min = level.BallStart;
            Vector2 max = level.BallStart;

            if (level.Terrain != null)
            {
                for (int i = 0; i < level.Terrain.Count; i++)
                {
                    Encapsulate(ref min, ref max, level.Terrain[i].A);
                    Encapsulate(ref min, ref max, level.Terrain[i].B);
                }
            }

            for (int i = 0; i < _solution.Strokes.Count; i++)
            {
                var points = _solution.Strokes[i].Points;
                if (points == null) continue;
                for (int p = 0; p < points.Count; p++) Encapsulate(ref min, ref max, points[p]);
            }

            Vector2 center = (min + max) * 0.5f;
            Vector2 extent = (max - min) * 0.5f;

            float halfHeight = Mathf.Max(extent.y, extent.x / Mathf.Max(cam.aspect, 0.1f));

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(halfHeight * 1.25f, 2f);   // 1.25 = 여백
            cam.transform.position = new Vector3(center.x, center.y, -10f);

            // 선 색이 이 배경을 기준으로 잡혀 있으므로 배경도 함께 고정한다.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _background;
        }

        static void Encapsulate(ref Vector2 min, ref Vector2 max, Vector2 point)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        // ── 시뮬 대상 ─────────────────────────────────────────────────────

        /// <summary>고를 수 있는 시뮬 하나. 레벨과 솔루션을 **매번 새로 만들도록** 팩토리로 들고 있다.</summary>
        readonly struct Entry
        {
            public readonly string Name;
            public readonly Func<LevelData> MakeLevel;
            public readonly Func<Solution> MakeSolution;
            public readonly bool WithBomb;

            public Entry(string name, Func<LevelData> makeLevel, Func<Solution> makeSolution, bool withBomb = false)
            {
                Name = name;
                MakeLevel = makeLevel;
                MakeSolution = makeSolution;
                WithBomb = withBomb;
            }
        }

        /// <summary>
        /// 에디터에서는 테스트가 쓰는 레벨을 그대로 목록에 넣는다. 테스트가 빨간불일 때
        /// 그 레벨에서 실제로 무슨 일이 벌어지는지 보는 것이 이 도구의 주 용도다.
        ///
        /// **테스트 레벨에는 폭탄을 얹지 않는다.** 테스트가 돌리는 것과 다른 시뮬을 보여주면
        /// 눈으로 본 것이 실패 원인을 짚는 근거가 되지 못한다.
        /// </summary>
        static Entry[] BuildCatalog()
        {
            var entries = new List<Entry>
            {
                new Entry("뷰어 기본 (폭탄)", SampleLevel, SampleSolution, withBomb: true),
            };

#if UNITY_EDITOR
            entries.AddRange(new[]
            {
                new Entry("Ramp → Clear", TestLevels.RampToGoal, () => null),
                new Entry("Gap 다리 없음 → Fail", TestLevels.Gap, () => null),
                new Entry("Gap + 다리 → Stalled", TestLevels.Gap, TestLevels.BridgeSolution),
                new Entry("FlatRest → Stalled", TestLevels.FlatRest, () => null),
                new Entry("FreeFall → Fail", TestLevels.FreeFall, () => null),
                new Entry("LongRoll (계속 굴러감)", TestLevels.LongRoll, () => null),
                new Entry("PivotSwing (회전축)", TestLevels.PivotSwing, TestLevels.PivotSolution),
                new Entry("자유 물체 낙하", TestLevels.FlatRest, TestLevels.FreeBodySolution),
            });
#endif

            return entries.ToArray();
        }

        // 아래 둘은 뷰어 전용 임의 데이터다. 테스트와 무관하며 폭탄이 붙는 유일한 레벨이다.

        static LevelData SampleLevel()
        {
            return new LevelData
            {
                Id = "DBG_Ramp",
                InkLimit = 20f,
                BallStart = new Vector2(-4.5f, 3.3f),
                BallRadius = 0.25f,
                GoalPosition = new Vector2(4.5f, -0.5f),
                GoalRadius = 0.5f,
                KillY = -8f,
                Terrain = new List<StaticSegment>
                {
                    new StaticSegment(new Vector2(-5f, 3f), new Vector2(0f, 1f)),
                    new StaticSegment(new Vector2(2f, -1f), new Vector2(6f, -1f)),
                },
            };
        }

        static Solution SampleSolution()
        {
            var solution = new Solution();

            // 끊긴 지형을 잇는 고정선. 이게 없으면 공이 틈으로 떨어진다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(0f, 1f),
                new Vector2(2.2f, -0.9f),
            }));

            // 공이 굴러오는 길목을 가로지르는 회전 막대.
            //
            // 자유 물체를 공중에 그냥 두면 떨어져 바닥에 눕고, 공은 그 위를 스치고 지나가
            // 부딪히는 장면이 안 나온다. 가운데를 월드에 고정해 두면 제자리에 떠 있다가
            // 공이 끝을 때리는 순간 팽이처럼 돈다.
            //
            // 막대 높이 -0.55 는 지형(-1) 위를 구르는 공의 몸통(-1.0 ~ -0.5) 안에 들어가는 값이다.
            // 이보다 높으면 공이 밑으로 지나가고, 낮으면 지형에 묻힌다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(2.8f, -0.55f),
                new Vector2(4.2f, -0.55f),
            }));

            // 막대(스트로크 1)의 한가운데를 월드에 고정. StrokeB 가 WorldIndex(-1) 면 정적 앵커다.
            solution.Pivots.Add(new PivotJoint(1, PivotJoint.WorldIndex, new Vector2(3.5f, -0.55f)));

            return solution;
        }

        // ── 폭탄 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 뷰어 전용 가짜 폭탄. 일정 스텝 뒤 공을 한 번 밀어 올리고 끝난다.
        ///
        /// 테스트 쪽 <c>TestBomb</c> 과 같은 물건이지만 그쪽은 PPS.Core.TestFixtures 어셈블리에
        /// UNITY_INCLUDE_TESTS 제약과 함께 격리되어 있어 여기서 참조할 수 없다. 임시 도구를 위해
        /// 어셈블리 구성을 바꾸느니 스무 줄을 다시 쓰는 편이 싸다.
        ///
        /// 위치 개념이 없다는 점에 주의 — 반경 판정 없이 무조건 공을 민다. 화면의 폭탄 표시는
        /// 순전히 눈으로 보기 위한 것이고 물리에 관여하지 않는다. 발동 시점에 공이 어디에 있든
        /// 확실히 눈에 보이는 변화가 나오는 편이 확인용 도구로서 낫다.
        /// </summary>
        sealed class DebugBomb : IStepLogic
        {
            readonly int _delaySteps;
            readonly int _jitterSteps;
            readonly float _power;

            int _fireStep = -1;
            bool _fired;

            /// 밀어 올릴 대상. 월드 구축 직후 공을 넣는다.
            public Rigidbody2D Target;

            /// 뽑힌 발동 스텝. 첫 Tick 전에는 -1.
            public int FireStep => _fireStep;

            public bool Fired => _fired;

            public DebugBomb(int delaySteps, int jitterSteps, float power)
            {
                _delaySteps = delaySteps;
                _jitterSteps = jitterSteps;
                _power = power;
            }

            public bool HasPendingWork => !_fired;

            public void Tick(int step, System.Random rng)
            {
                if (_fired) return;

                // 발동 스텝은 생성 시점이 아니라 첫 Tick 에서 뽑는다. rng 는 계약상 Tick 에서만
                // 주어지고, 그래야 난수 소비 순서가 "스텝 순서 × 장치 등록 순서"로 고정된다.
                if (_fireStep < 0)
                    _fireStep = _delaySteps + (_jitterSteps > 0 ? rng.Next(_jitterSteps) : 0);

                if (step < _fireStep) return;

                if (Target != null)
                {
                    // 잠든 바디는 힘을 줘도 스스로 깨지 않는다 (Physics2D 는 3D 와 다르다).
                    Target.WakeUp();
                    Target.linearVelocity += Vector2.up * _power;
                }

                _fired = true;
            }
        }
    }
}
