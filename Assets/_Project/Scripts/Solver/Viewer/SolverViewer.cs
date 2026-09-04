using System;
using System.Collections;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

// UnityEngine 에도 같은 이름이 있다.
using DeviceType = PPS.Core.DeviceType;

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
        static readonly Color StarColor = new Color32(0xB8, 0x7A, 0x00, 0xFF);      // 진한 금색
        static readonly Color KillLineColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);  // 어두운 벽돌
        static readonly Color BombIdleColor = new Color32(0x6B, 0x3F, 0xA0, 0xFF);  // 진한 보라
        static readonly Color BombFiredColor = new Color32(0xC4, 0x00, 0x6B, 0xFF); // 진한 마젠타
        static readonly Color HazardColor = new Color32(0xD4, 0x4A, 0x00, 0xFF);    // 진한 주황 — 닿으면 실패
        static readonly Color WindColor = new Color32(0x00, 0x6E, 0x8A, 0xFF);      // 진한 하늘 — 미는 쪽으로 선
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

        /// 지금 그리고 있는 통로. _paths 의 자리다.
        int _index;

        /// 굴리고 있는 그림. 연산 결과가 없으면 비어 있다.
        Solution _solution;

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

        Vector2 _legendScroll;

        /// 실측이 남긴 표. 파일이 없으면 null 이고 프리셋 항목도 안 생긴다.
        List<LeverPreset> _presets;

        /// 프리셋마다 소속 유형. _presets 와 같은 순서다.
        int[] _clusters;
        int _clusterCount = 4;
        int _presetAt;

        /// 지금 펼친 탭. 0 = 레벨 파일, 1 = 시도, 2 = 프리셋.
        int _tab;

        /// <summary>
        /// 탐색은 레벨을 고를 때가 아니라 버튼으로만 돈다 —
        /// 수백 번 굴리는 일이라 목록을 훑기만 해도 멈춰 버린다.
        /// 아직 안 돌린 레벨은 통로만 그린다.
        /// </summary>
        bool HasSearched => _stage != null && _searched.ContainsKey(_stage.StageId);

        /// 돌고 있는 연산. null 이면 놀고 있다.
        Coroutine _search;

        /// 진행 막대에 쓸 값. 연산이 도는 동안만 뜻이 있다.
        float _searchProgress;
        int _searchTries;

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
            if (_targetStep < _world.CurrentStep) ResetWorld();

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
            var entry = _catalog[_levelIndex];
            _stage = entry.MakeStage();

            _paths = BallPath.Find(_stage.Level);
            _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _paths.Count - 1));

            // 연산 전에는 아무것도 안 그린다 — 통로에 벽을 세워 보는 것은
            // 연산이 할 일이고, 그것까지 여기서 지으면 레벨을 고르는 데 값이 든다.
            _solution = entry.MakeSolution?.Invoke() ?? Solution.Empty;

            ResetWorld();
        }

        /// <summary>
        /// 판만 처음으로 되돌린다. 되감기가 곧 재구축이라
        /// 스텝을 뒤로 옮길 때마다 여기를 지난다 — 통로 탐색까지
        /// 같이 하면 슬라이더를 미는 동안 프레임이 멈춘다.
        /// </summary>
        void ResetWorld()
        {
            _world?.Dispose();

            // 스테이지를 매번 새로 만든다.
            // 장치가 발동 여부를 들고 있는 상태 객체다.
            _world = WorldBuilder.Build(_stage, _solution);
        }

        /// <summary>
        /// 그릴 통로를 바꾼다. 끝에서 넘어가면 반대쪽으로 돈다.
        /// 선만 갈아 끼우므로 굴리던 판은 건드리지 않는다.
        /// </summary>
        void Show(int index) => _index = (index + _paths.Count) % _paths.Count;

        void ApplyLevel(int index)
        {
            // 돌던 연산은 다른 레벨의 것이 된다.
            CancelSearch();

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
            // 프리셋을 바꾸면 굴릴 판도 바뀐다. 돌던 연산은 뜻을 잃는다.
            CancelSearch();

            // 표 항목을 고른 상태여야 이 지렛대가 굴러간다.
            _levelIndex = 0;

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
        /// 연산이 남긴 그림. 아직 안 돌린 스테이지에는 부르지 않는다 —
        /// HasSearched 가 참일 때만 쓰인다.
        /// </summary>
        Solution Searched(StageData stage)
        {
            SolveReport report = _searched[stage.StageId];

            // 시도 목록에서 고른 것이 있으면 그것이 우선이다.
            if (_attemptAt >= 0 && report.Log != null && _attemptAt < report.Log.Count)
                return report.Log[_attemptAt].Solution;

            // 못 풀었으면 가장 가까이 갔던 것을 대신 보여준다.
            // 빈 화면보다는 어디서 어긋났는지가 보이는 편이 낫다.
            return report.Solution ?? report.Closest ?? Solution.Empty;
        }

        /// <summary>
        /// 지금 고른 레벨을 연산에 건다.
        /// 이미 돌린 레벨이면 결과를 버리고 다시 돌린다.
        /// </summary>
        void RunSearch()
        {
            CancelSearch();

            _searched.Remove(_stage.StageId);
            _search = StartCoroutine(SearchRoutine(_stage));
        }

        /// 돌던 연산을 버린다. 굴리던 판은 결과로 남기지 않는다 —
        /// 예산을 다 안 쓴 보고는 "못 풀었다"와 구분이 안 된다.
        void CancelSearch()
        {
            if (_search == null) return;

            StopCoroutine(_search);
            _search = null;
        }

        /// <summary>
        /// 한 프레임에 붙잡고 있을 시간. 판 수로 끊으면 레벨마다
        /// 프레임이 들쭉날쭉하다 — 판 하나 굴리는 값이 제각각이다.
        /// </summary>
        const float FrameBudget = 0.02f;

        /// 프레임을 나눠 굴린다. 다 굴린 뒤에야 결과를 남긴다.
        IEnumerator SearchRoutine(StageData stage)
        {
            var search = new SolutionSearch(
                LeverPresetFile.Exists
                    ? LeverPresets.Load()
                    : new LeverPresets(new List<LeverPreset>()));

            // 첫 답에서 멈추지 않는다. 어떤 답들이 있었는지를 보는 자리다.
            SolutionSearch.Run run = search.Begin(stage, stopAtClear: false);

            float until = Time.realtimeSinceStartup + FrameBudget;

            while (run.Step())
            {
                _searchProgress = run.Progress;
                _searchTries = run.Tries;

                if (Time.realtimeSinceStartup < until) continue;

                yield return null;
                until = Time.realtimeSinceStartup + FrameBudget;
            }

            _searched[stage.StageId] = run.Report;
            _search = null;

            _attemptAt = -1;
            _targetStep = 0;
            _stepInput = "0";

            Rebuild();
            RefreshAttemptView();
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
            DrawSearchPanel();
            DrawLegendPanel();
            DrawPlaybackPanel();
        }

        /// 우측 패널의 가로. 연산기와 색인이 같이 쓴다.
        const float RightWidth = 260f;

        /// <summary>
        /// 무슨 색이 무엇인지. 화면의 선은 전부 이 표에서 온 것이다 —
        /// 색만 다른 원이 여럿이라 표 없이는 가려낼 수 없다.
        /// </summary>
        void DrawLegendPanel()
        {
            const float top = 136f;

            // 창이 낮으면 패널부터 줄인다. 화면 밖으로 나가면 스크롤도 못 잡는다.
            float height = Mathf.Clamp(Screen.height - top - 10f, 60f, 282f);

            GUILayout.BeginArea(
                new Rect(Screen.width - RightWidth - 10f, top, RightWidth, height), GUI.skin.box);

            GUILayout.Label("색인");

            // 창이 낮으면 아래쪽 줄이 잘린다. 잘린 만큼 스크롤한다.
            _legendScroll = GUILayout.BeginScrollView(_legendScroll);

            LegendRow(TerrainColor, "지형 (붙박이)");
            LegendRow(FreeBodyColor, "자유 물체");
            LegendRow(BallColor, "공");
            LegendRow(GoalColor, "골");
            LegendRow(StarColor, "별");
            LegendRow(BombIdleColor, "폭탄 — 대기");
            LegendRow(BombFiredColor, "폭탄 — 발동");
            LegendRow(HazardColor, "스파이크 · 위험 바디");
            LegendRow(WindColor, "바람 (선은 미는 쪽)");
            LegendRow(KillLineColor, "킬 라인");
            LegendRow(PathColor, "통로");
            LegendRow(SolutionColor, "솔버가 그린 선");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static void LegendRow(Color color, string name)
        {
            GUILayout.BeginHorizontal();

            Rect chip = GUILayoutUtility.GetRect(18f, 14f, GUILayout.Width(18f));

            Color was = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(chip, Texture2D.whiteTexture);
            GUI.color = was;

            GUILayout.Label(name);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 연산기. 우상단에 따로 둔다 — 목록 옆에 있으면
        /// 레벨을 고르다 잘못 눌러 수백 번 굴리게 된다.
        /// </summary>
        void DrawSearchPanel()
        {
            GUILayout.BeginArea(
                new Rect(Screen.width - RightWidth - 10f, 10f, RightWidth, 116f), GUI.skin.box);

            GUILayout.Label(_catalogNames[_levelIndex]);

            if (_search != null)
            {
                DrawProgressBar(_searchProgress);
                GUILayout.Label($"{_searchProgress * 100f:F0}%   {_searchTries}판 굴림");

                if (GUILayout.Button("취소", GUILayout.Height(26f))) CancelSearch();
            }
            else
            {
                GUILayout.Label(HasSearched ? "연산 완료" : "연산 전 — 통로만 그린다");

                GUILayout.Space(4f);

                if (GUILayout.Button(HasSearched ? "다시 연산" : "연산", GUILayout.Height(26f)))
                    RunSearch();
            }

            GUILayout.EndArea();
        }

        /// IMGUI 에는 진행 막대가 없다. 틀을 그리고 안을 채운다.
        static void DrawProgressBar(float filled)
        {
            Rect line = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            GUI.Box(line, GUIContent.none);

            Color was = GUI.color;
            GUI.color = new Color32(0x0B, 0x6E, 0x6E, 0xFF);
            GUI.DrawTexture(
                new Rect(line.x + 2f, line.y + 2f,
                    (line.width - 4f) * Mathf.Clamp01(filled), line.height - 4f),
                Texture2D.whiteTexture);
            GUI.color = was;
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

            if (_tab == 0) DrawFileTab();
            else if (_tab == 1) DrawAttemptTab();
            else DrawPresetTab();

            GUILayout.EndArea();
        }

        static readonly string[] TabNames = { "레벨 파일", "시도", "프리셋" };

        /// <summary>
        /// Levels 폴더에서 읽어 온 레벨들과 지금 고른 레벨의 정보.
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

            // 레벨이 늘면 목록이 패널을 넘친다. 넘친 만큼 스크롤한다.
            _fileScroll = GUILayout.BeginScrollView(_fileScroll);

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

            if (_fileProblems.Count > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label($"읽지 못한 파일 {_fileProblems.Count}개");

                for (int i = 0; i < _fileProblems.Count; i++)
                    GUILayout.Label(_fileProblems[i]);
            }

            GUILayout.Space(8f);

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

            GUILayout.EndScrollView();
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
                GUILayout.Label("연산을 거치지 않은 스테이지다.");
                GUILayout.Label("레벨을 고른 뒤 우상단 '연산'을 누른다.");
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

            if (_paths.Count == 0)
            {
                GUILayout.Label("통로 없음 — 공이 목표까지 갈 길이 지형에 막혀 있다");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(30f))) Show(_index - 1);
            GUILayout.Label($"통로 {_index + 1} / {_paths.Count}   " +
                            $"점 {_paths[_index].Length}개",
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

            var stars = level.Stars;
            if (stars != null)
            {
                GL.Color(StarColor);

                for (int i = 0; i < stars.Count; i++)
                    Circle(stars[i], LevelData.StarCaptureRadius);
            }

            GL.Color(KillLineColor);
            Line(new Vector2(-30f, level.KillY), new Vector2(30f, level.KillY));

            var devices = level.Devices;
            if (devices == null) return;

            for (int i = 0; i < devices.Count; i++)
            {
                DeviceData device = devices[i];
                Vector2 at = device.Position;

                GL.Color(DeviceColor(device.Type, i));

                Circle(at, 0.3f);
                Circle(at, device.Radius);

                if (device.Type == DeviceType.Wind)
                {
                    // 바람만 방향이 있다. 미는 쪽으로 선을 하나 뻗는다.
                    float rad = device.Angle * Mathf.Deg2Rad;
                    Line(at, at + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * device.Radius);
                    continue;
                }

                Line(at + new Vector2(-0.45f, 0f), at + new Vector2(0.45f, 0f));
                Line(at + new Vector2(0f, -0.45f), at + new Vector2(0f, 0.45f));
            }
        }

        /// <summary>
        /// 장치 하나의 색. 폭탄만 상태가 갈린다 —
        /// 터진 폭탄은 몸이 사라져 자리만 남는다.
        /// </summary>
        Color DeviceColor(DeviceType type, int index)
        {
            if (type == DeviceType.Spike) return HazardColor;
            if (type == DeviceType.Wind) return WindColor;

            return _world.GetDevice(index).body != null ? BombIdleColor : BombFiredColor;
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

            // 골과 별은 지형 밖에 떠 있을 수 있다. 빼면 화면 밖으로 잘린다.
            Encapsulate(ref min, ref max, level.GoalPosition);

            if (level.Stars != null)
            {
                for (int i = 0; i < level.Stars.Count; i++)
                    Encapsulate(ref min, ref max, level.Stars[i]);
            }

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
            /// 풀이를 직접 주는 자리. 없거나 null 을 내면 빈 판을 굴린다.
            /// 낼 때마다 물어보는 것은 연산 전후로 답이 달라져서다.
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
                    () => HasSearched ? Searched(_stage) : null));
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
        /// 레벨은 Levels 폴더의 json 에서만 온다.
        /// 프리셋 표가 있으면 그것만 맨 앞에 따로 둔다 —
        /// 레벨이 아니라 실측한 지렛대를 굴려 보는 자리다.
        /// </summary>
        Entry[] BuildCatalog()
        {
            var entries = new List<Entry>();

            if (_presets != null && _presets.Count > 0)
                entries.Add(new Entry(
                    "지렛대 프리셋 (표)",
                    () => ViewerLevers.Stage(CurrentLever()),
                    () => ViewerLevers.Build(CurrentLever())));

            AddLevelFiles(entries);

            return entries.ToArray();
        }
    }
}
