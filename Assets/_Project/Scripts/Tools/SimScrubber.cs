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

        /// 뷰어 기본 레벨에 얹는 폭탄. 흔들림이 있어 시드를 바꾸면 발동 스텝이 달라진다.
        static readonly DeviceData SampleBomb = new DeviceData
        {
            Type = Core.DeviceType.Bomb,
            Position = new Vector2(-2.5f, 1.6f),
            Radius = 3f,
            Power = 5f,
            DelaySteps = 30,
            JitterSteps = 60,
        };

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

            // 레벨을 매번 새로 만든다. 장치는 발동 여부를 들고 있는 상태 객체이고,
            // WorldBuilder 가 레벨 데이터로부터 매번 새로 찍어내야 재생이 처음부터 같아진다.
            _world = WorldBuilder.Build(entry.MakeLevel(), _solution, _seed);
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

            // ToResult() 를 쓰지 않는다. 그쪽은 "아직 판정 안 남"과 "상한까지 갔는데 판정 안 남"을
            // 둘 다 Timeout 으로 접어버리는데, 그건 끝까지 돌린 뒤에 부르는 것을 전제한 계약이다.
            // 뷰어는 시뮬 도중에 물어보므로 Judge 를 직접 읽는다.
            GUILayout.Label($"{StatusText()}   MinGoalDist {_world.Judge.MinGoalDist:F3}   " +
                            $"Ball ({_world.Ball.position.x:F2}, {_world.Ball.position.y:F2})");

            GUILayout.Label($"잉크 {_solution.TotalInk():F2} / {_world.Level.InkLimit:F0}");

            // 같은 스텝으로 되돌아왔을 때 이 값이 같으면 재구축이 상태를 정확히 재현한 것이다.
            GUILayout.Label($"Hash 0x{WorldHasher.Hash(_world):X16}");

            int devices = _world.Level.Devices == null ? 0 : _world.Level.Devices.Count;

            if (devices == 0)
            {
                // 장치가 없으면 rng 가 아무에게도 전달되지 않는다. 시드를 바꿔도 결과가 그대로다.
                GUILayout.Label("장치 없음 — 이 레벨에서는 시드가 결과에 영향을 주지 않는다");
            }
            else
            {
                GUILayout.Label($"장치 {devices}개 — " +
                                (_world.AnyPendingWork() ? "대기 중" : "전부 발동 완료"));
            }

            // 시드를 바꾸면 폭탄 발동 스텝이 달라진다. 바꾸는 즉시 재구축하고 처음으로 돌린다.
            GUILayout.BeginHorizontal();
            GUILayout.Label($"시드 {_seed}", GUILayout.Width(60f));
            if (GUILayout.Button("◀", GUILayout.Width(30f))) ApplySeed(_seed - 1);
            if (GUILayout.Button("▶", GUILayout.Width(30f))) ApplySeed(_seed + 1);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 현재 판정. <c>SimOutcome</c> 에는 "진행 중"이 없다 — 시뮬을 끝까지 돌린 뒤의 결과만
        /// 표현하는 계약이라 그렇고, 그 자체는 옳다. 다만 뷰어는 도중 상태를 보여줘야 하므로
        /// 여기서만 진행 중을 따로 구분한다.
        /// </summary>
        string StatusText()
        {
            var judge = _world.Judge;

            if (judge.Cleared) return $"Clear (스텝 {judge.DecidedStep})";
            if (judge.Failed) return $"Fail (스텝 {judge.DecidedStep})";
            if (judge.Stalled) return $"Stalled (스텝 {judge.DecidedStep})";

            // 상한까지 갔는데 아무 판정도 안 났으면 그때가 진짜 Timeout 이다.
            return _world.CurrentStep >= MaxSteps ? "Timeout" : "진행 중";
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

            // 자유 물체 — 선분마다 부풀린 사각형이 경로 하나씩 들어 있다.
            //
            // 콜라이더 외곽이 아니라 **원래 그은 선**을 복원해 그린다. 두께는 edge ↔ edge 충돌을
            // 우회하려고 붙인 것이지 유저가 그린 것이 아니고, 렌더링은 물리와 같은 스트로크를
            // 쓴다는 원칙(설계서 결정 7)에도 중심선 쪽이 맞다.
            //
            // 사각형은 { 시작-법선, 시작+법선, 끝+법선, 끝-법선 } 순서라
            // 마주보는 두 변의 중점을 이으면 선분이 그대로 나온다. 양 끝은 반 두께만큼
            // 늘여 두었으므로 그만큼 되돌리면 원래 점이 된다.
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

                    // 늘여 둔 만큼 되돌린다. 선분이 두께보다 짧으면 되돌리다 뒤집히므로 그냥 둔다.
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

            // 회전축 표시. 바디가 아니라 조인트라 Bodies 를 훑는 것만으로는 보이지 않는다.
            GL.Color(PivotColor);
            for (int i = 0; i < _solution.Pivots.Count; i++)
                Circle(_solution.Pivots[i].Anchor, 0.12f);

            // 장치 표시. 대기 중이면 보라, 전부 터지면 마젠타. 바깥 원은 영향 반경이다.
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
            public Entry(string name, Func<LevelData> makeLevel, Func<Solution> makeSolution)
            {
                Name = name;
                MakeLevel = makeLevel;
                MakeSolution = makeSolution;
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
                new Entry("뷰어 기본 (폭탄)", SampleLevel, SampleSolution),
                new Entry("자유 물체 전시장", ShowcaseLevel, ShowcaseSolution),
            };

#if UNITY_EDITOR
            entries.AddRange(new[]
            {
                new Entry("L001 (JSON 파일)", SampleLevelFile.Load, () => null),
                new Entry("L002 전 피처 (JSON)", FeatureLevelFile.LoadLevel, FeatureLevelFile.LoadSolution),
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

        // ── 자유 물체 전시장 ───────────────────────────────────────────────

        /// <summary>
        /// 서로 다른 형태의 자유 물체를 한 판에 떨어뜨린다. 담장 안이라 굴러 나가지 않는다.
        ///
        /// <c>ColliderFactory</c> 의 질량 특성 계산을 눈으로 검증하는 용도다. 무게중심이나 관성이
        /// 틀리면 숫자가 아니라 **움직임**으로 드러난다 — 고리가 안 구르거나, ㄱ자가 엉뚱한 점을
        /// 중심으로 돌거나, 그릇이 뒤집힌 채 안정되거나 한다. 테스트는 값을 재고 여기서는 거동을 본다.
        /// </summary>
        static LevelData ShowcaseLevel()
        {
            return new LevelData
            {
                Id = "DBG_Showcase",
                InkLimit = 100f,
                BallStart = new Vector2(-12f, 10f),
                BallRadius = 0.3f,
                GoalPosition = new Vector2(11.5f, 1.6f),
                GoalRadius = 0.6f,
                KillY = -6f,
                Terrain = new List<StaticSegment>
                {
                    // 비스듬한 비탈. 평지에 세워 두면 전부 그 자리에 가만히 앉아 있어
                    // 질량 특성이 맞는지 틀린지가 드러나지 않는다. 굴리고 넘어뜨려야 보인다.
                    new StaticSegment(new Vector2(-13f, 8f), new Vector2(3f, 1f)),
                    new StaticSegment(new Vector2(3f, 1f), new Vector2(13f, 1f)),
                    new StaticSegment(new Vector2(-13f, 8f), new Vector2(-13f, 13f)),  // 왼쪽 담장
                    new StaticSegment(new Vector2(13f, 1f), new Vector2(13f, 9f)),     // 오른쪽 담장
                },
            };
        }

        /// <summary>
        /// 비탈 위에 형태가 다른 자유 물체를 늘어놓는다. 공이 굴러 내려오며 연쇄로 건드린다.
        ///
        /// 배치 원칙 둘:
        /// - **비탈 위에** 둔다. 평지면 전부 가만히 앉아 있어 질량 특성이 맞는지 알 수 없다
        /// - **기울여** 둔다. 반듯하게 놓으면 대칭이라 무게중심이 틀려도 티가 안 난다
        /// </summary>
        static Solution ShowcaseSolution()
        {
            var solution = new Solution();

            // 비탈 y = 8 - 0.4375·(x + 13).
            //
            // **모든 물체는 비탈 위에 여유를 두고 띄운다.** 정적 edge 에 다각형이 겹친 채로
            // 시작하면 Box2D 가 밀어내는 방향이 불안정해서 튕기거나 끼거나 아래로 빠진다.
            // 살짝 떨어뜨려 스스로 자리를 잡게 하는 편이 안전하다.

            // 0) 바퀴 — 관성이 m·r² 라야 비탈을 굴러 내려간다.
            //    변이 적으면 꼭짓점마다 부딪히며 에너지를 잃어 잘 안 구른다. 28각형이면 충분히 매끄럽다.
            solution.Strokes.Add(Closed(new Vector2(-11f, 8f), 0.55f, sides: 28));

            // 1) 상자 — 닫힌 사각형을 기울여 둔다. 모서리로 서 있다가 넘어간다.
            solution.Strokes.Add(Closed(new Vector2(-9f, 7.3f), 0.7f, sides: 4, rotation: 65f));

            // 2) 삼각형 — 닫힌 형태 중 무게중심이 가장 치우친 것. 한쪽으로 굴러야 정상이다.
            solution.Strokes.Add(Closed(new Vector2(-7f, 6.4f), 0.7f, sides: 3, rotation: 90f));

            // 3) 기울어진 막대 — 비스듬히 떨어져 미끄러지며 넘어간다.
            solution.Strokes.Add(Bar(new Vector2(-5f, 5.6f), length: 1.8f, degrees: 55f));

            // 4) ㄱ자 — 두 팔의 길이 비중으로 무게중심이 잡히는지. 긴 팔 쪽으로 기운다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-3.8f, 4.6f),
                new Vector2(-2.2f, 4.6f),
                new Vector2(-2.2f, 5.8f),
            }));

            // 5) 지그재그 — 점이 많은 폴리라인. 산술 평균이었다면 무게중심이 어긋난다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody, new List<Vector2>
            {
                new Vector2(-1.4f, 3.5f),
                new Vector2(-0.7f, 4.2f),
                new Vector2(0f, 3.5f),
                new Vector2(0.7f, 4.2f),
                new Vector2(1.4f, 3.5f),
            }));

            // 6) 그릇 — 열린 곡선. 오목한 쪽이 위로 오게 안정되어야 정상이다.
            //    평지에 두어 비탈에서 내려온 것들을 받는다.
            solution.Strokes.Add(new Stroke(ToolType.FreeBody,
                Arc(new Vector2(4.5f, 2.5f), 1.1f, segments: 12, startDegrees: 200f, sweepDegrees: 140f)));

            // 7·8) 시소 — 정적 받침 + 회전축에 매달린 판자.
            //       회전축과 충돌이 함께 걸리는 유일한 자리다.
            solution.Strokes.Add(new Stroke(ToolType.FixedLine, new List<Vector2>
            {
                new Vector2(8.5f, 1f),
                new Vector2(8.5f, 1.7f),
            }));
            solution.Strokes.Add(Bar(new Vector2(8.5f, 1.8f), length: 3.2f, degrees: 8f));
            solution.Pivots.Add(new PivotJoint(8, PivotJoint.WorldIndex, new Vector2(8.5f, 1.8f)));

            return solution;
        }

        /// <summary>닫힌 정다각형. 변을 늘리면 고리(바퀴)가 된다.</summary>
        static Stroke Closed(Vector2 center, float radius, int sides, float rotation = 0f)
            => new Stroke(ToolType.FreeBody, Arc(center, radius, sides, rotation, 360f));

        /// <summary>중심과 각도로 기울인 막대.</summary>
        static Stroke Bar(Vector2 center, float length, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 half = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (length * 0.5f);

            return new Stroke(ToolType.FreeBody, new List<Vector2> { center - half, center + half });
        }

        /// <summary>호를 폴리라인으로 전개한다. 360도를 주면 닫힌 고리가 된다.</summary>
        static List<Vector2> Arc(Vector2 center, float radius, int segments,
                                 float startDegrees, float sweepDegrees)
        {
            var points = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (startDegrees + sweepDegrees * i / segments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }

        // ── 뷰어 기본 레벨 ─────────────────────────────────────────────────
        // 뷰어 전용 임의 데이터다. 테스트와 무관하며 폭탄이 붙는 유일한 레벨이다.

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
                Devices = new List<DeviceData> { SampleBomb },
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

    }
}
