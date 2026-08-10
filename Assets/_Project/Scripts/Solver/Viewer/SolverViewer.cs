using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

#if UNITY_INCLUDE_TESTS
// 픽스처 어셈블리의 제약과 같은 심볼이어야 한다.
// 테스트 없는 빌드에서는 아예 컴파일되지 않는다.
using PPS.Core.Tests;
#endif

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 솔버용 디버그 뷰어. SimScrubber 와 같은 재생 기능에
    /// 솔버가 보는 것 — 공이 지날 통로 — 를 겹쳐 띄운다.
    /// 시뮬 없이 레벨 데이터만으로 나오는 값이라
    /// 스텝을 옮겨도 변하지 않는다.
    /// </summary>
    public sealed class SolverViewer : MonoBehaviour
    {
        const int MaxSteps = SimWorld.DefaultMaxSteps;
        const int CircleSegments = 28;

        // 밝은 배경(FFE6B3) 위에서 읽히도록 전부 어둡고 진한 색으로 잡았다.
        static readonly Color TerrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);   // 거의 검정
        static readonly Color FreeBodyColor = new Color32(0x1B, 0x4F, 0xA0, 0xFF);  // 진한 파랑
        static readonly Color BallColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);      // 크림슨
        static readonly Color GoalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);      // 진한 초록
        static readonly Color KillLineColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);  // 어두운 벽돌
        static readonly Color BombIdleColor = new Color32(0x6B, 0x3F, 0xA0, 0xFF);  // 진한 보라
        static readonly Color BombFiredColor = new Color32(0xC4, 0x00, 0x6B, 0xFF); // 진한 마젠타
        static readonly Color HazardColor = new Color32(0xD4, 0x4A, 0x00, 0xFF);    // 진한 주황 — 닿으면 실패
        static readonly Color PathColor = new Color32(0x0B, 0x3F, 0x7A, 0xFF);      // 진한 감청

        [SerializeField] bool _autoFitCamera = true;

        /// <summary>
        /// 두께를 화면 배율 대비 비율로 잡는다.
        /// 고정값이면 넓은 레벨에서 실처럼 얇아진다.
        /// </summary>
        [SerializeField] float _lineWidthRatio = 0.013f;

        [SerializeField] Color _background = new Color32(0xFF, 0xE6, 0xB3, 0xFF);

        SimWorld _world;
        StageData _stage;
        int _targetStep;
        string _stepInput = "0";

        /// 공이 지날 통로들. 레벨을 바꿀 때만 다시 짓는다. 시뮬과 무관하다.
        List<Vector2[]> _paths;

        bool _showPath = true;

        Entry[] _catalog;
        string[] _catalogNames;
        int _levelIndex;

        /// 실제 두께(월드 단위). FitCamera 가 계산한다.
        float _lineWidth = 0.09f;

        Material _lineMaterial;

        void Start()
        {
            _catalog = BuildCatalog();

            // 픽스처가 없는 빌드. 보여줄 것이 없다.
            if (_catalog.Length == 0)
            {
                enabled = false;
                return;
            }

            _catalogNames = Array.ConvertAll(_catalog, e => e.Name);

            Rebuild();
            FitCamera();
        }

        void Update()
        {
            if (_world == null) return;

            // 프레임당 한 번만. OnGUI 는 한 프레임에
            // 여러 번 돌아 월드가 여러 번 재구축된다.
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
            _stage = entry.MakeStage();

            // 스테이지를 매번 새로 만든다.
            // 장치가 발동 여부를 들고 있는 상태 객체다.
            // 풀이는 넣지 않는다 — 솔버가 보는 것은 빈 레벨이다.
            _world = WorldBuilder.Build(_stage, Solution.Empty);

            _paths = BallPath.Find(_stage.Level);
        }

        void ApplyLevel(int index)
        {
            _levelIndex = index;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
            FitCamera();
        }

        // ── 표시 ──

        void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10f, 10f, 460f, 410f), GUI.skin.box);

            int picked = GUILayout.SelectionGrid(_levelIndex, _catalogNames, 2);
            if (picked != _levelIndex) ApplyLevel(picked);

            GUILayout.Space(6f);

            GUILayout.Label($"Step {_world.CurrentStep} / {MaxSteps}" +
                            (_world.IsTerminal ? "   (판정 확정 — 더 진행하지 않는다)" : ""));

            _targetStep = Mathf.RoundToInt(GUILayout.HorizontalSlider(_targetStep, 0f, MaxSteps));

            GUILayout.BeginHorizontal();
            GUILayout.Label("직접 입력", GUILayout.Width(60f));
            _stepInput = GUILayout.TextField(_stepInput, GUILayout.Width(70f));
            if (GUILayout.Button("이동", GUILayout.Width(50f)) && int.TryParse(_stepInput, out int typed))
                _targetStep = Mathf.Clamp(typed, 0, MaxSteps);
            if (GUILayout.Button("처음으로", GUILayout.Width(70f)))
                _targetStep = 0;
            GUILayout.EndHorizontal();

            // ToResult 는 진행 중을 Timeout 으로 접는다.
            // 뷰어는 도중에 묻는다. Judge 를 직접 읽는다.
            GUILayout.Label($"{StatusText()}   MinGoalDist {_world.Judge.MinGoalDist:F3}   " +
                            $"Ball ({_world.Ball.position.x:F2}, {_world.Ball.position.y:F2})");

            // 같은 스텝에서 같으면 재구축이 정확한 것이다.
            GUILayout.Label($"Hash 0x{WorldHasher.Hash(_world):X16}");

            int devices = _world.Level.Devices == null ? 0 : _world.Level.Devices.Count;

            // 시드는 읽기 전용이다.
            // 장치가 없으면 rng 가 소비되지 않아
            // 시드가 결과에 닿지 못한다.
            GUILayout.Label(devices == 0
                ? $"{_stage.StageId}   시드 {_stage.Seed} (장치가 없어 결과에 닿지 않는다)"
                : $"{_stage.StageId}   시드 {_stage.Seed}   장치 {devices}개 — " +
                  (_world.AnyPendingWork() ? "대기 중" : "전부 발동 완료"));

            int hazards = CountLiveHazards();
            if (hazards > 0) GUILayout.Label($"위험 바디 {hazards}개 — 공에 닿으면 Fail");

            DrawTopologyPanel();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 솔버가 보는 구조. 스텝과 무관하므로 재생 표시와 나눠 둔다.
        /// </summary>
        void DrawTopologyPanel()
        {
            GUILayout.Space(6f);

            _showPath = GUILayout.Toggle(_showPath, " 통로", GUILayout.Width(100f));

            GUILayout.Label(_paths.Count == 0
                ? "통로 없음 — 공이 목표까지 갈 길이 지형에 막혀 있다"
                : $"통로 {_paths.Count}개");
        }

        /// <summary>
        /// SimOutcome 에는 진행 중이 없다.
        /// 뷰어만 그걸 따로 구분한다.
        /// </summary>
        string StatusText()
        {
            var judge = _world.Judge;

            if (judge.Cleared) return $"Clear (스텝 {judge.DecidedStep})";
            if (judge.Failed) return $"Fail (스텝 {judge.DecidedStep})";
            if (judge.Stalled) return $"Stalled (스텝 {judge.DecidedStep})";

            // 상한까지 갔는데 판정이 없으면 Timeout.
            return _world.CurrentStep >= MaxSteps ? "Timeout" : "진행 중";
        }

        void OnRenderObject()
        {
            if (_world == null) return;

            EnsureMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();

            // GL.LINES 는 두께를 줄 수 없어
            // 선분마다 사각형을 그린다.
            GL.Begin(GL.QUADS);

            DrawLevelMarkers();
            DrawTopology();

            var bodies = _world.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null) continue;

                GL.Color(ReferenceEquals(body, _world.Ball) ? BallColor
                       : IsHazard(body) ? HazardColor
                       : body.bodyType == RigidbodyType2D.Static ? TerrainColor
                       : FreeBodyColor);

                DrawBody(body);
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// 공이 지날 통로. 지형보다 먼저 그려 바디에 덮이게 둔다 —
        /// 가상의 선이 실제 물체를 가리면 오해를 부른다.
        /// </summary>
        void DrawTopology()
        {
            if (!_showPath || _paths == null) return;

            GL.Color(PathColor);
            for (int i = 0; i < _paths.Count; i++)
            {
                Vector2[] path = _paths[i];
                for (int p = 0; p + 1 < path.Length; p++) Line(path[p], path[p + 1]);
            }
        }

        /// <summary>목록이 짧아 선형 검색으로 충분하다.</summary>
        bool IsHazard(Rigidbody2D body)
        {
            var hazards = _world.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                var hazard = hazards[i];
                if (hazard != null && ReferenceEquals(hazard.attachedRigidbody, body)) return true;
            }
            return false;
        }

        /// <summary>파괴된 파편은 null 로 남아 있다.</summary>
        int CountLiveHazards()
        {
            int count = 0;
            var hazards = _world.Hazards;
            for (int i = 0; i < hazards.Count; i++)
                if (hazards[i] != null) count++;
            return count;
        }

        void DrawBody(Rigidbody2D body)
        {
            // 정적 스트로크와 지형 — 두께 0 의 선.
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

            // 자유 물체 — 외곽이 아니라 원래 그은 선을 그린다.
            // 두께는 충돌을 성립시키려고 붙인 것이지
            // 유저가 그린 것이 아니다.
            var polygon = body.GetComponent<PolygonCollider2D>();
            if (polygon != null)
            {
                var transform = body.transform;

                for (int p = 0; p < polygon.pathCount; p++)
                {
                    var path = polygon.GetPath(p);
                    if (path.Length != 4) continue;

                    Vector2 start = (path[0] + path[1]) * 0.5f;
                    Vector2 end = (path[2] + path[3]) * 0.5f;

                    Vector2 delta = end - start;
                    float length = delta.magnitude;

                    // 늘여 둔 만큼 되돌린다. 짧으면 뒤집히니 둔다.
                    if (length > 2f * ColliderFactory.FreeBodyHalfWidth)
                    {
                        Vector2 cap = delta / length * ColliderFactory.FreeBodyHalfWidth;
                        start += cap;
                        end -= cap;
                    }

                    Line(transform.TransformPoint(start), transform.TransformPoint(end));
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

            // 대기 중이면 보라, 터지면 마젠타.
            var devices = level.Devices;
            if (devices != null && devices.Count > 0)
            {
                GL.Color(_world.AnyPendingWork() ? BombIdleColor : BombFiredColor);

                for (int i = 0; i < devices.Count; i++)
                {
                    Vector2 at = devices[i].Position;

                    Circle(at, 0.3f);
                    Circle(at, devices[i].Radius);
                    Line(at + new Vector2(-0.45f, 0f), at + new Vector2(0.45f, 0f));
                    Line(at + new Vector2(0f, -0.45f), at + new Vector2(0f, 0.45f));
                }
            }
        }

        /// <summary>선분 하나를 사각형으로 그린다.</summary>
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

            // 에셋을 늘리지 않으려고 내장 셰이더를 쓴다.
            _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _lineMaterial.SetFloat("_ZWrite", 0f);
            _lineMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        }

        /// <summary>
        /// 레벨이 다 들어오도록 카메라를 맞춘다.
        /// 컷은 담지 않는다 — 천장까지 뻗어 있어 넣으면
        /// 화면이 쓸모없이 축소된다.
        /// </summary>
        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (_autoFitCamera) Frame(cam);

            // 두께는 항상 현재 배율을 따라간다.
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

            Vector2 center = (min + max) * 0.5f;
            Vector2 extent = (max - min) * 0.5f;

            float halfHeight = Mathf.Max(extent.y, extent.x / Mathf.Max(cam.aspect, 0.1f));

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(halfHeight * 1.25f, 2f);   // 1.25 = 여백
            cam.transform.position = new Vector3(center.x, center.y, -10f);

            // 선 색이 이 배경 기준이라 함께 고정한다.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _background;
        }

        static void Encapsulate(ref Vector2 min, ref Vector2 max, Vector2 point)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        // ── 시뮬 대상 ──

        /// <summary>매번 새로 만들도록 팩토리로 든다.</summary>
        readonly struct Entry
        {
            public readonly string Name;
            public readonly Func<StageData> MakeStage;
            public Entry(string name, Func<StageData> makeStage)
            {
                Name = name;
                MakeStage = makeStage;
            }
        }

        /// <summary>
        /// 레벨 팩토리를 스테이지로 감싼다.
        /// 장치가 없으면 시드는 아무 값이나 같다.
        /// </summary>
        static Entry Stage(string name, Func<LevelData> makeLevel, int seed = 0)
            => new Entry(
                name,
                () => new StageData { StageId = name, Seed = seed, Level = makeLevel() });

        /// <summary>
        /// 레벨은 전부 픽스처 어셈블리에서 온다.
        /// 목록의 레벨과 시드는 테스트가 돌리는 것과
        /// 같아야 눈으로 본 것이 근거가 된다.
        /// 풀이를 안 그리므로 레벨마다 한 줄이다.
        /// </summary>
        static Entry[] BuildCatalog()
        {
#if UNITY_INCLUDE_TESTS
            return new[]
            {
                Stage("뷰어 기본 (폭탄)", ViewerLevels.BombRamp, seed: 3),
                Stage("자유 물체 전시장", ViewerLevels.Showcase),
                Stage("퍼즐", TestLevels.GapPuzzle),
                Stage("L001 (JSON 파일)", SampleLevelFile.Load),
                Stage("L002 전 피처 (JSON)", FeatureLevelFile.LoadLevel),
                Stage("Ramp", TestLevels.RampToGoal),
                Stage("Gap", TestLevels.Gap),
                Stage("FlatRest", TestLevels.FlatRest),
                Stage("FreeFall", TestLevels.FreeFall),
                Stage("LongRoll", TestLevels.LongRoll),
                Stage("PivotSwing", TestLevels.PivotSwing),

                // 시드 7 = FragBombPitStage 의 기본값.
                Stage("파편 구덩이", TestLevels.FragBombPit, seed: 7),
                Stage("파편 멀리", TestLevels.FragBombFarAway, seed: 7),
            };
#else
            // 픽스처가 없는 빌드. 가드를 빼면
            // 없는 타입을 참조해 빌드가 깨진다.
            return Array.Empty<Entry>();
#endif
        }
    }
}
