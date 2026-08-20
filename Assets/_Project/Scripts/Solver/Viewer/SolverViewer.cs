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
        static readonly Color SolutionColor = new Color32(0x0B, 0x6E, 0x6E, 0xFF);  // 진한 청록 — 솔버가 그린 것

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

        /// 통로마다 하나씩. 지금 굴리고 있는 것은 _index 번이다.
        List<Solution> _solutions;
        Solution _solution;
        int _index;

        bool _showPath = true;

        Entry[] _catalog;
        string[] _catalogNames;
        int _levelIndex;

        /// <summary>
        /// 파일에서 온 항목이 시작하는 자리.
        /// 목록은 하나로 두고 보여줄 때만 나눈다 — 둘로 쪼개면
        /// Rebuild 가 어느 쪽 항목인지부터 가려야 한다.
        /// </summary>
        int _fileFrom;

        /// 읽히지 않은 파일들. 이름과 이유.
        readonly List<string> _fileProblems = new List<string>();

        Vector2 _fileScroll;

        /// 실측이 남긴 표. 파일이 없으면 null 이고 프리셋 항목도 안 생긴다.
        List<LeverPreset> _presets;

        /// 프리셋마다 소속 유형. _presets 와 같은 순서다.
        int[] _clusters;
        int _clusterCount = 4;
        int _presetAt;

        /// 지금 펼친 탭. 0 = 카탈로그, 1 = 프리셋.
        int _tab;

        /// <summary>
        /// 레벨을 세 패스로 풀어 볼지. 끄면 통로만 그려 준다 —
        /// 통로를 하나씩 넘겨 보려면 탐색이 낸 답 하나로는 안 된다.
        /// </summary>
        bool _useSearch = true;

        /// 시도 목록에서 고른 판. -1 이면 답(없으면 가장 가까웠던 것)을 본다.
        int _attemptAt = -1;

        /// 패스로 거르기. 0 이면 전부.
        int _attemptPass;

        /// 결과로 거르기. 0 이면 전부, 나머지는 SimOutcome + 1.
        int _attemptOutcome;

        /// 0 순서 · 1 잉크 · 2 클리어 타임 · 3 영역 크기.
        int _attemptSort;

        /// <summary>
        /// 걸러 내고 줄 세운 결과. 목록이 수백 줄이라
        /// OnGUI 가 돌 때마다 다시 만들면 화면이 느려진다.
        /// </summary>
        readonly List<int> _attemptView = new List<int>();

        Vector2 _attemptScroll;

        /// 산점도의 점. 색만 바꿔 가며 쓴다.
        Texture2D _dot;

        /// <summary>
        /// 스테이지별 탐색 결과. 스텝을 되감을 때마다 Rebuild 가 도는데,
        /// 탐색은 수백 번 굴리는 일이라 그때마다 다시 할 수 없다.
        /// </summary>
        readonly Dictionary<string, SolveReport> _searched =
            new Dictionary<string, SolveReport>();

        /// 실제 두께(월드 단위). FitCamera 가 계산한다.
        float _lineWidth = 0.09f;

        Material _lineMaterial;

        void Start()
        {
            LoadPresets();
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
            if (_dot != null) Destroy(_dot);
        }

        void Rebuild()
        {
            _world?.Dispose();

            var entry = _catalog[_levelIndex];
            _stage = entry.MakeStage();

            _paths = BallPath.Find(_stage.Level);

            // 풀이를 직접 주는 항목이라도 지금은 안 낼 수 있다 — 탐색을 끈 경우다.
            Solution given = entry.MakeSolution?.Invoke();

            _solutions = given == null
                ? new SolutionBuilder().Build(_stage)
                : new List<Solution> { given };

            _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _solutions.Count - 1));
            _solution = _solutions.Count == 0 ? Solution.Empty : _solutions[_index];

            // 스테이지를 매번 새로 만든다.
            // 장치가 발동 여부를 들고 있는 상태 객체다.
            _world = WorldBuilder.Build(_stage, _solution);
        }

        /// 굴릴 통로를 바꾼다. 끝에서 넘어가면 반대쪽으로 돈다.
        void Show(int index)
        {
            _index = (index + _solutions.Count) % _solutions.Count;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
        }

        void ApplyLevel(int index)
        {
            _levelIndex = index;
            _targetStep = 0;
            _stepInput = "0";

            // 레벨이 바뀌면 고른 시도는 뜻을 잃는다.
            _attemptAt = -1;

            Rebuild();
            RefreshAttemptView();
            FitCamera();
        }

        // ── 프리셋 ──

        /// <summary>
        /// 실측이 남긴 표를 읽고 유형을 나눈다.
        /// 파일이 없으면 조용히 넘어간다 — 스윕을 아직 안 돌린 것뿐이다.
        /// </summary>
        void LoadPresets()
        {
            if (!LeverPresetFile.Exists) return;

            _presets = LeverPresetFile.Load().Presets;
            Recluster();
        }

        void Recluster()
            => _clusters = PresetClusters.Assign(_presets, _clusterCount);

        /// 고른 칸의 지렛대를 굴린다.
        void ApplyPreset(int index)
        {
            _presetAt = index;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
            FitCamera();
        }

        /// 지금 고른 프리셋. 공은 원점에 둔다 — 표가 어긋남만 담고 있다.
        Lever CurrentLever() => _presets[_presetAt].ToLever(Vector2.zero);

        // ── 탐색 ──

        /// <summary>
        /// 두 패스를 돌려 나온 그림. 스테이지마다 한 번만 돈다.
        /// 프리셋이 없으면 통로 패스만으로도 볼 것이 있으므로 그대로 진행한다.
        /// </summary>
        Solution Searched(StageData stage)
        {
            if (!_searched.TryGetValue(stage.StageId, out SolveReport report))
            {
                var search = new SolutionSearch(
                    LeverPresetFile.Exists
                        ? LeverPresets.Load()
                        : new LeverPresets(new List<LeverPreset>()));

                // 첫 답에서 멈추지 않는다. 어떤 답들이 있었는지를 보는 자리다.
                report = search.Solve(stage, stopAtClear: false);
                _searched[stage.StageId] = report;
            }

            // 시도 목록에서 고른 것이 있으면 그것이 우선이다.
            if (_attemptAt >= 0 && report.Log != null && _attemptAt < report.Log.Count)
                return report.Log[_attemptAt].Solution;

            // 못 풀었으면 가장 가까이 갔던 것을 대신 보여준다.
            // 빈 화면보다는 어디서 어긋났는지가 보이는 편이 낫다.
            return report.Solution ?? report.Closest ?? Solution.Empty;
        }

        /// 고른 시도를 굴린다.
        void ApplyAttempt(int index)
        {
            _attemptAt = index;
            _targetStep = 0;
            _stepInput = "0";
            Rebuild();
        }

        // ── 표시 ──

        void OnGUI()
        {
            if (_world == null) return;

            DrawMainPanel();
            DrawPlaybackPanel();
        }

        /// <summary>
        /// 하나로 합친 패널. 탭으로 나눈다 —
        /// 창이 여럿이면 화면을 가려 정작 시뮬이 안 보인다.
        /// </summary>
        void DrawMainPanel()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 470f, 600f), GUI.skin.box);

            int tab = GUILayout.Toolbar(_tab, TabNames);
            if (tab != _tab) _tab = tab;

            GUILayout.Space(6f);

            if (_tab == 0) DrawCatalogTab();
            else if (_tab == 1) DrawFileTab();
            else if (_tab == 2) DrawAttemptTab();
            else DrawPresetTab();

            GUILayout.EndArea();
        }

        static readonly string[] TabNames = { "카탈로그", "레벨 파일", "시도", "프리셋" };

        /// <summary>
        /// Levels 폴더에서 읽어 온 레벨들.
        /// 못 읽은 파일도 이유와 함께 보여준다 — 목록에서 조용히
        /// 빠지면 파일을 잘못 뒀는지 형식이 틀렸는지 알 길이 없다.
        /// </summary>
        void DrawFileTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{LevelFiles.RelativeFolder}", GUILayout.Width(200f));
            if (GUILayout.Button("다시 읽기", GUILayout.Width(80f))) ReloadCatalog();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            int count = _catalog.Length - _fileFrom;

            if (count <= 0)
            {
                GUILayout.Label("읽어 온 레벨이 없다.");
            }
            else
            {
                var names = new string[count];
                Array.Copy(_catalogNames, _fileFrom, names, 0, count);

                int at = _levelIndex - _fileFrom;
                int picked = GUILayout.SelectionGrid(at, names, 2);

                if (picked != at) ApplyLevel(_fileFrom + picked);
            }

            if (_fileProblems.Count == 0) return;

            GUILayout.Space(8f);
            GUILayout.Label($"읽지 못한 파일 {_fileProblems.Count}개");

            _fileScroll = GUILayout.BeginScrollView(_fileScroll, GUILayout.Height(160f));

            for (int i = 0; i < _fileProblems.Count; i++)
                GUILayout.Label(_fileProblems[i]);

            GUILayout.EndScrollView();
        }

        void DrawCatalogTab()
        {
            // 파일에서 온 것은 제 탭에서 고른다.
            var names = new string[_fileFrom];
            Array.Copy(_catalogNames, names, _fileFrom);

            int picked = GUILayout.SelectionGrid(Mathf.Min(_levelIndex, _fileFrom - 1), names, 2);
            if (picked != _levelIndex) ApplyLevel(picked);

            GUILayout.Space(6f);

            bool search = GUILayout.Toggle(_useSearch, " 탐색으로 풀기 (끄면 통로만)");
            if (search != _useSearch)
            {
                _useSearch = search;
                ApplyLevel(_levelIndex);
            }

            GUILayout.Space(6f);

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

            // 탐색을 거친 스테이지만 결과가 있다. 나머지는 통로를 그대로 쓴다.
            if (_searched.TryGetValue(_stage.StageId, out SolveReport report))
            {
                GUILayout.Label($"탐색 결과 — {report}");

                // 지금 보고 있는 것이 답인지 실패작인지 헷갈리면 안 된다.
                if (!report.Cleared && report.Closest != null)
                    GUILayout.Label("화면은 가장 가까이 갔던 시도다 — 답이 아니다");
            }

            DrawTopologyPanel();
        }

        /// <summary>
        /// 재생기. 좌하단에 따로 둔다 —
        /// 탭을 옮겨도 스텝은 계속 만질 수 있어야 한다.
        /// </summary>
        void DrawPlaybackPanel()
        {
            const float height = 132f;
            GUILayout.BeginArea(
                new Rect(10f, Screen.height - height - 10f, 470f, height), GUI.skin.box);

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

            GUILayout.EndArea();
        }

        static readonly string[] PassNames = { "전부", "1 통로", "2 지렛대", "3 지렛대+통로" };

        static readonly string[] OutcomeNames = { "전부", "클리어", "실패", "정지", "시간초과" };

        static readonly string[] SortNames = { "순서", "잉크", "클리어 타임", "영역" };

        /// <summary>
        /// 탐색이 굴려 본 판들. 실패한 것까지 골라 재생할 수 있다 —
        /// 왜 못 풀었는지는 결과 숫자가 아니라 그 판을 봐야 알 수 있다.
        /// </summary>
        void DrawAttemptTab()
        {
            if (!_searched.TryGetValue(_stage.StageId, out SolveReport report) || report.Log == null)
            {
                GUILayout.Label("탐색을 거치지 않은 스테이지다.");
                GUILayout.Label("카탈로그 탭에서 '탐색으로 풀기'를 켠 뒤 레벨을 고른다.");
                return;
            }

            GUILayout.Label($"{report}");

            int pass = GUILayout.Toolbar(_attemptPass, PassNames);
            int outcome = GUILayout.Toolbar(_attemptOutcome, OutcomeNames);

            GUILayout.BeginHorizontal();
            GUILayout.Label("정렬", GUILayout.Width(34f));
            int sort = GUILayout.Toolbar(_attemptSort, SortNames);
            GUILayout.EndHorizontal();

            if (pass != _attemptPass || outcome != _attemptOutcome || sort != _attemptSort)
            {
                _attemptPass = pass;
                _attemptOutcome = outcome;
                _attemptSort = sort;
                RefreshAttemptView();
            }

            if (GUILayout.Button(_attemptAt < 0 ? "▶ 결과 보는 중" : "결과로 돌아가기"))
                ApplyAttempt(-1);

            GUILayout.Label($"{_attemptView.Count} / {report.Log.Count} 개");

            _attemptScroll = GUILayout.BeginScrollView(_attemptScroll, GUILayout.Height(360f));

            for (int v = 0; v < _attemptView.Count; v++)
            {
                int i = _attemptView[v];
                Attempt attempt = report.Log[i];

                // 목표에 닿은 판은 색으로 구분한다. 목록이 길어 눈으로 훑게 된다.
                Color was = GUI.backgroundColor;
                GUI.backgroundColor = attempt.Cleared
                    ? new Color32(0xB8, 0xD4, 0x8A, 0xFF)
                    : i == _attemptAt
                        ? new Color32(0x7A, 0xC7, 0xC7, 0xFF)
                        : was;

                if (GUILayout.Button($"#{i}  {attempt}")) ApplyAttempt(i);

                GUI.backgroundColor = was;
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 거르고 줄 세운다. 필터나 정렬이 바뀔 때만 부른다.
        /// 클리어 타임 정렬은 닿은 판을 앞에 모은다 —
        /// 못 닿은 판의 끝난 스텝은 시간이 아니라 포기한 자리다.
        /// </summary>
        void RefreshAttemptView()
        {
            _attemptView.Clear();

            if (!_searched.TryGetValue(_stage.StageId, out SolveReport report) || report.Log == null)
                return;

            List<Attempt> log = report.Log;

            for (int i = 0; i < log.Count; i++)
            {
                if (_attemptPass != 0 && (int)log[i].Pass != _attemptPass) continue;
                if (_attemptOutcome != 0 && (int)log[i].Outcome != _attemptOutcome - 1) continue;

                _attemptView.Add(i);
            }

            switch (_attemptSort)
            {
                case 1:
                    _attemptView.Sort((x, y) => log[x].Ink.CompareTo(log[y].Ink));
                    break;

                case 2:
                    _attemptView.Sort((x, y) =>
                    {
                        int by = log[y].Cleared.CompareTo(log[x].Cleared);
                        return by != 0 ? by : log[x].EndStep.CompareTo(log[y].EndStep);
                    });
                    break;

                case 3:
                    _attemptView.Sort((x, y) => Size(log[x].Area).CompareTo(Size(log[y].Area)));
                    break;
            }
        }

        /// 그림이 차지한 넓이. 한 줄짜리 그림은 넓이가 0 이라 둘레로 잰다.
        static float Size(Rect area) => area.width + area.height;

        void DrawPresetTab()
        {
            if (_presets == null || _presets.Count == 0)
            {
                GUILayout.Label("프리셋 표가 없다 — LeverPresetTests 를 먼저 돌린다.");
                GUILayout.Label(LeverPresetFile.RelativePath);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"유형 {_clusterCount}개", GUILayout.Width(60f));
            int picked = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(_clusterCount, 2f, 8f, GUILayout.Width(120f)));
            if (picked != _clusterCount)
            {
                _clusterCount = picked;
                Recluster();
            }
            GUILayout.EndHorizontal();

            DrawPresetScatter();
            DrawPresetDetail();
        }

        /// <summary>
        /// 목표 공간 위의 산점도. 한 점이 (공 자리 → 목표) 한 쌍이다.
        /// 반투명이라 겹친 자리가 짙게 보이고, 색이 같은 무리끼리
        /// 어느 방향에 모여 있는지가 그대로 드러난다.
        /// </summary>
        void DrawPresetScatter()
        {
            Bounds(out Vector2 least, out Vector2 most);

            Rect plot = GUILayoutUtility.GetRect(440f, 300f);
            plot = new Rect(plot.x + 30f, plot.y + 6f, plot.width - 40f, plot.height - 26f);

            Fill(plot, new Color(0f, 0f, 0f, 0.05f));

            // 가로 속도 0 은 눈금이 아니라 기준선이다.
            // 앞으로 보내는지 뒤로 넘기는지가 여기서 갈린다.
            float zero = ToPixel(plot, Vector2.zero, least, most).x;
            if (plot.xMin <= zero && zero <= plot.xMax)
                Fill(new Rect(zero, plot.y, 1f, plot.height), new Color(0f, 0f, 0f, 0.25f));

            for (int i = 0; i < _presets.Count; i++)
            {
                Vector2 at = ToPixel(plot, _presets[i].LaunchVelocity, least, most);

                bool chosen = i == _presetAt;
                float size = chosen ? 26f : 18f;

                Color color = ClusterColor(_clusters[i]);
                color.a = chosen ? 1f : 0.35f;

                GUI.color = color;
                GUI.DrawTexture(
                    new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), Dot());
                GUI.color = Color.white;
            }

            PickFromScatter(plot, least, most);

            // 축 눈금. 모서리에만 적어 점을 가리지 않는다.
            var corner = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            GUI.Label(new Rect(plot.x - 30f, plot.y - 2f, 30f, 16f), $"{most.y:F0}", corner);
            GUI.Label(new Rect(plot.x - 30f, plot.yMax - 14f, 30f, 16f), $"{least.y:F0}", corner);
            GUI.Label(new Rect(plot.x, plot.yMax + 2f, 40f, 16f), $"{least.x:F0}", corner);
            GUI.Label(new Rect(plot.xMax - 20f, plot.yMax + 2f, 40f, 16f), $"{most.x:F0}", corner);

            GUILayout.Label("가로 = 수평 발사 속도, 세로 = 수직 발사 속도. 점을 누르면 굴린다");
        }

        static Vector2 ToPixel(Rect plot, Vector2 value, Vector2 least, Vector2 most)
        {
            float x = Mathf.InverseLerp(least.x, most.x, value.x);

            // 화면은 아래로 갈수록 y 가 커진다. 빠르게 쏘는 쪽이 위에 오도록 뒤집는다.
            float y = 1f - Mathf.InverseLerp(least.y, most.y, value.y);

            return new Vector2(plot.x + x * plot.width, plot.y + y * plot.height);
        }

        /// <summary>
        /// 누른 자리에서 가장 가까운 점을 고른다.
        /// 점끼리 겹치므로 사각형 판정 대신 거리로 찾는다.
        /// </summary>
        void PickFromScatter(Rect plot, Vector2 least, Vector2 most)
        {
            Event now = Event.current;

            if (now.type != EventType.MouseDown || !plot.Contains(now.mousePosition)) return;

            int nearest = -1;
            float closest = float.PositiveInfinity;

            for (int i = 0; i < _presets.Count; i++)
            {
                Vector2 at = ToPixel(plot, _presets[i].LaunchVelocity, least, most);

                float gap = Vector2.Distance(at, now.mousePosition);
                if (gap >= closest) continue;

                closest = gap;
                nearest = i;
            }

            if (nearest < 0) return;

            ApplyPreset(nearest);
            now.Use();
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 가장자리가 부드러운 흰 원. 색은 GUI.color 로 입힌다 —
        /// 무리 색마다 텍스처를 만들면 색을 바꿀 때마다 다시 만들어야 한다.
        /// </summary>
        Texture2D Dot()
        {
            if (_dot != null) return _dot;

            const int size = 32;
            _dot = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float away = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                _dot.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, away)));
            }

            _dot.Apply();
            return _dot;
        }

        /// <summary>고른 프리셋의 다섯 축과 유도된 값.</summary>
        void DrawPresetDetail()
        {
            LeverPreset preset = _presets[_presetAt];
            Lever lever = CurrentLever();

            GUILayout.Space(6f);
            GUILayout.Label(
                $"유형 {_clusters[_presetAt]}   "
                + $"잉크 {preset.Ink:F2}   "
                + $"발사까지 {preset.LaunchStep}스텝 "
                + $"({preset.LaunchStep * SimWorld.FixedDt:F2}초)");

            GUILayout.Label(
                $"발사 자리 ({preset.LaunchOffset.x:F2}, {preset.LaunchOffset.y:F2})   "
                + $"발사 속도 ({preset.LaunchVelocity.x:F2}, {preset.LaunchVelocity.y:F2})");

            GUILayout.Label(
                $"판 길이 {lever.Length:F2}   "
                + $"축 자리 {lever.FulcrumAt:F2}   "
                + $"공 자리 {lever.BallAt:F2}");

            GUILayout.Label(
                $"추 {lever.WeightRows}줄 (무게 {lever.Weight.Mass:F2})   "
                + $"낙차 {lever.Drop:F2}");

            GUILayout.Label(
                $"공 팔 {lever.BallArm:F2}   "
                + $"추 팔 {lever.WeightArm:F2}   "
                + $"추 {(lever.WeightLeft ? "왼쪽" : "오른쪽")}   "
                + $"필요 여유 {lever.RequiredHeadroom:F2}");
        }

        /// <summary>
        /// 발사 속도가 퍼져 있는 범위. 점이 가장자리에 붙지 않도록 조금 넓힌다.
        /// </summary>
        void Bounds(out Vector2 least, out Vector2 most)
        {
            least = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            most = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < _presets.Count; i++)
            {
                Vector2 v = _presets[i].LaunchVelocity;

                least = Vector2.Min(least, v);
                most = Vector2.Max(most, v);
            }

            var margin = new Vector2(
                Mathf.Max((most.x - least.x) * 0.08f, 0.5f),
                Mathf.Max((most.y - least.y) * 0.08f, 0.5f));

            least -= margin;
            most += margin;
        }

        /// 밝은 배경에서 서로 갈리는 색들. 유형 수만큼 돌려 쓴다.
        static Color ClusterColor(int cluster)
        {
            switch (cluster % 8)
            {
                case 0: return new Color32(0x7A, 0xC7, 0xC7, 0xFF);
                case 1: return new Color32(0xE8, 0xA0, 0x7A, 0xFF);
                case 2: return new Color32(0xA0, 0xC0, 0xE8, 0xFF);
                case 3: return new Color32(0xC7, 0xA8, 0xE0, 0xFF);
                case 4: return new Color32(0xB8, 0xD4, 0x8A, 0xFF);
                case 5: return new Color32(0xE8, 0xC8, 0x7A, 0xFF);
                case 6: return new Color32(0xE0, 0x9A, 0xB8, 0xFF);
                default: return new Color32(0xB0, 0xB0, 0xB0, 0xFF);
            }
        }

        /// <summary>
        /// 솔버가 보는 구조. 스텝과 무관하므로 재생 표시와 나눠 둔다.
        /// </summary>
        void DrawTopologyPanel()
        {
            GUILayout.Space(6f);

            _showPath = GUILayout.Toggle(_showPath, " 통로", GUILayout.Width(100f));

            if (_solutions.Count == 0)
            {
                GUILayout.Label("통로 없음 — 공이 목표까지 갈 길이 지형에 막혀 있다");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(30f))) Show(_index - 1);
            GUILayout.Label($"통로 {_index + 1} / {_solutions.Count}   " +
                            $"선 {_solution.Strokes.Count}개   잉크 {_solution.TotalInk():F1}",
                            GUILayout.Width(260f));
            if (GUILayout.Button("▶", GUILayout.Width(30f))) Show(_index + 1);
            GUILayout.EndHorizontal();
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

            // 솔버가 그린 선. 바디 위에 덧그려 지형과 색이 갈리게 한다.
            GL.Color(SolutionColor);
            for (int i = 0; i < _solution.Strokes.Count; i++)
            {
                var points = _solution.Strokes[i].Points;
                if (points == null) continue;

                for (int p = 0; p + 1 < points.Count; p++) Line(points[p], points[p + 1]);
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
            if (!_showPath || _paths == null || _index >= _paths.Count) return;

            // 지금 고른 통로만 그린다. 전부 겹쳐 그리면
            // 어느 선이 어느 통로에서 나온 것인지 알 수 없다.
            Vector2[] path = _paths[_index];

            GL.Color(PathColor);
            for (int p = 0; p + 1 < path.Length; p++) Line(path[p], path[p + 1]);
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
            var polygons = body.GetComponents<PolygonCollider2D>();
            if (polygons.Length > 0)
            {
                var transform = body.transform;

                for (int p = 0; p < polygons.Length; p++)
                {
                    var path = polygons[p].GetPath(0);
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
            Circle(level.GoalPosition, LevelData.GoalRadius);

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

            // 지형이 없는 무대는 그림이 화면 밖으로 나간다 —
            // 도구 하나만 놓고 보는 자리가 그렇다.
            for (int i = 0; i < _solution.Strokes.Count; i++)
            {
                var points = _solution.Strokes[i].Points;
                if (points == null) continue;

                for (int p = 0; p < points.Count; p++)
                    Encapsulate(ref min, ref max, points[p]);
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

            /// <summary>
            /// 풀이를 직접 주는 자리. 없거나 null 을 내면 SolutionBuilder 가 낸다.
            /// 낼 때마다 물어보는 것은 탐색을 껐다 켰다 할 수 있어야 해서다.
            /// </summary>
            public readonly Func<Solution> MakeSolution;

            public Entry(string name, Func<StageData> makeStage, Func<Solution> makeSolution = null)
            {
                Name = name;
                MakeStage = makeStage;
                MakeSolution = makeSolution;
            }
        }

        /// <summary>
        /// 레벨 팩토리를 스테이지로 감싼다.
        /// 장치가 없으면 시드는 아무 값이나 같다.
        /// </summary>
        /// <summary>
        /// Levels 폴더의 json 을 목록 뒤에 붙인다.
        /// 못 읽은 것은 항목으로 만들지 않고 이유만 모아 둔다 —
        /// 고를 수 있게 두면 눌렀을 때 빈 판이 뜬다.
        /// </summary>
        void AddLevelFiles(List<Entry> entries)
        {
            _fileFrom = entries.Count;
            _fileProblems.Clear();

            List<LevelFiles.Entry> files = LevelFiles.LoadAll();

            for (int i = 0; i < files.Count; i++)
            {
                LevelFiles.Entry file = files[i];

                if (!file.Usable)
                {
                    _fileProblems.Add($"{file.Name} — {file.Problem}");
                    continue;
                }

                StageData stage = file.Stage;

                entries.Add(new Entry(
                    file.Name,
                    () => stage,
                    () => _useSearch ? Searched(_stage) : null));
            }
        }

        /// <summary>
        /// 목록을 다시 짓는다. 파일이 늘거나 바뀌었을 때 쓴다.
        /// 고르고 있던 자리는 지킬 수 없으므로 앞으로 되돌린다.
        /// </summary>
        void ReloadCatalog()
        {
            _catalog = BuildCatalog();
            _catalogNames = Array.ConvertAll(_catalog, e => e.Name);

            ApplyLevel(Mathf.Clamp(_levelIndex, 0, _catalog.Length - 1));
        }

        /// <summary>
        /// 레벨 하나. 탐색이 켜져 있으면 세 패스를 거친 그림을,
        /// 꺼져 있으면 SolutionBuilder 가 낸 통로들을 보여준다.
        /// </summary>
        Entry Stage(string name, Func<LevelData> makeLevel, int seed = 0)
            => new Entry(
                name,
                () => new StageData { StageId = name, Seed = seed, Level = makeLevel() },
                () => _useSearch ? Searched(_stage) : null);

        /// <summary>
        /// 레벨은 전부 픽스처 어셈블리에서 온다.
        /// 목록의 레벨과 시드는 테스트가 돌리는 것과
        /// 같아야 눈으로 본 것이 근거가 된다.
        /// 풀이를 안 그리므로 레벨마다 한 줄이다.
        /// </summary>
        Entry[] BuildCatalog()
        {
#if UNITY_INCLUDE_TESTS
            var entries = new List<Entry>();

            // 표가 있으면 맨 앞에 둔다. 격자에서 고른 칸을 그대로 굴린다.
            if (_presets != null && _presets.Count > 0)
                entries.Add(new Entry(
                    "지렛대 프리셋 (표)",
                    () => ViewerLevers.Stage(CurrentLever()),
                    () => ViewerLevers.Build(CurrentLever())));

            // 패스를 가르는 최소 레벨들. 테스트가 쓰는 것과 같다.
            entries.Add(Stage("비탈", SearchLevels.Slope));
            entries.Add(Stage("오르막", SearchLevels.Uphill));

            entries.AddRange(new[]
            {
                Stage("뷰어 기본 (폭탄)", ViewerLevels.BombRamp, seed: 3),
                Stage("자유 물체 전시장", ViewerLevels.Showcase),
                Stage("퍼즐", TestLevels.GapPuzzle),
                Stage("기둥 셋 (수평)", TestLevels.PillarRun),
                Stage("기둥 셋 (골이 위)", TestLevels.PillarRunUp),
                Stage("기둥 셋 (골이 아래)", TestLevels.PillarRunDown),
                Stage("L001 (JSON 파일)", SampleLevelFile.Load),
                Stage("L002 전 피처 (JSON)", FeatureLevelFile.LoadLevel),
                Stage("Ramp", TestLevels.RampToGoal),

                // 시드 7 = FragBombPitStage 의 기본값.
                Stage("파편 구덩이", TestLevels.FragBombPit, seed: 7),
                Stage("파편 멀리", TestLevels.FragBombFarAway, seed: 7),
            });

            AddLevelFiles(entries);

            return entries.ToArray();
#else
            // 픽스처가 없는 빌드. 가드를 빼면
            // 없는 타입을 참조해 빌드가 깨진다.
            return Array.Empty<Entry>();
#endif
        }
    }
}
