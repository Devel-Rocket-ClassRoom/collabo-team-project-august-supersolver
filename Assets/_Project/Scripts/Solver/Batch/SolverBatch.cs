using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using PPS.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PPS.Solver.Batch
{
    /// <summary>
    /// 뷰어의 '연산' 을 화면 없이 전부 돌리고 결과를 json 으로 남긴다.
    /// 굴리는 순서와 결과는 뷰어와 같다 — 같은 SolutionSearch 를
    /// 같은 인자(첫 답에서 멈추지 않음)로 돌린다.
    /// </summary>
    public static class SolverBatch
    {
        /// <summary>
        /// 한 틱에 붙잡고 있을 시간.
        /// 통째로 돌리지 못하는 것은 SimWorld 가 씬을 비동기로 내리기 때문이다 —
        /// 프레임을 넘기지 않으면 굴린 판의 씬이 전부 메모리에 쌓인다.
        /// </summary>
        const float TickBudget = 0.02f;

        /// 따로 일러 주지 않았을 때 리포트가 떨어지는 자리.
        const string DefaultFolder = "SolverReports";

        /// 굴리는 중에 형편을 알리는 간격.
        const float LogEvery = 2f;

        static float _nextLog;

        static SolutionSearch _search;
        static List<StageData> _queue;
        static int _at;
        static SolutionSearch.Run _run;
        static Stopwatch _stageWatch;
        static Stopwatch _watch;
        static BatchReport _report;
        static string _out;

        /// 플레이 모드로 들어가면서 도메인이 다시 로드되면
        /// 정적 상태가 날아간다. 이어서 돌아야 한다는 사실만 여기 남긴다.
        const string ResumeKey = "PPS.SolverBatch.Resume";

        /// 플레이 모드가 안 열릴 때 배치가 영영 안 끝나는 것을 막는다.
        const float EnterTimeout = 120f;

        static float _enterUntil;

        /// <summary>
        /// 배치 진입점. -quit 은 붙이지 않는다 —
        /// 플레이 모드를 거쳐야 해서 끝내는 것은 여기가 한다.
        /// Unity.exe -batchmode -nographics -projectPath .         ///   -executeMethod PPS.Solver.Batch.SolverBatch.Run -out 경로.json
        /// -stage 이름 을 주면 그 레벨 하나만 굴린다.
        /// </summary>
        public static void Run()
        {
            // SimWorld 의 씬은 플레이 모드에서만 만들어진다.
            // 빈 씬으로 들어가는 것은 게임 부팅을 태우지 않기 위해서다.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SessionState.SetBool(ResumeKey, true);
            EditorApplication.EnterPlaymode();
        }

        /// 도메인이 다시 로드된 뒤 이어 붙는 자리.
        [InitializeOnLoadMethod]
        static void Resume()
        {
            if (!SessionState.GetBool(ResumeKey, false)) return;

            _enterUntil = Time.realtimeSinceStartup + EnterTimeout;
            EditorApplication.update += WaitForPlay;
        }

        /// 플레이 모드가 열릴 때까지 기다렸다가 굴리기 시작한다.
        static void WaitForPlay()
        {
            if (!EditorApplication.isPlaying)
            {
                if (Time.realtimeSinceStartup < _enterUntil) return;

                Stop();
                Fail(new TimeoutException($"플레이 모드가 {EnterTimeout:F0}초 안에 안 열렸다."));
                return;
            }

            Stop();

            try
            {
                Begin();
            }
            catch (Exception e)
            {
                Fail(e);
                return;
            }

            EditorApplication.update += Tick;
        }

        /// 기다리기를 그만둔다. 표시를 지워야 다음 로드에서 또 붙지 않는다.
        static void Stop()
        {
            EditorApplication.update -= WaitForPlay;
            SessionState.SetBool(ResumeKey, false);
        }

        /// <summary>
        /// 프리셋과 레벨을 읽고 첫 스테이지를 건다.
        /// 프리셋이 없으면 지렛대 패스가 표를 못 받아 통로만 본다 —
        /// 조용히 넘어가면 못 푼 이유를 리포트에서 알 수 없어 개수를 남긴다.
        /// </summary>
        static void Begin()
        {
            _watch = Stopwatch.StartNew();
            _out = OutPath();

            var presets = LeverPresetFile.Exists
                ? LeverPresets.Load()
                : new LeverPresets(new List<LeverPreset>());

            _search = new SolutionSearch(presets);

            string only = Arg("-stage");

            _report = new BatchReport
            {
                generatedAt = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
                levelFolder = LevelFiles.RelativeFolder,
                stageFilter = only,
                presetCount = presets.Count,
            };

            _queue = new List<StageData>();

            List<LevelFiles.Entry> entries = LevelFiles.LoadAll();

            for (int i = 0; i < entries.Count; i++)
            {
                LevelFiles.Entry entry = entries[i];

                if (only != null && entry.Name != only) continue;

                if (entry.Usable) _queue.Add(entry.Stage);
                else _report.unreadable.Add($"{entry.Name} — {entry.Problem}");
            }

            // 이름을 잘못 적었는데 빈 리포트가 나오면
            // 그 스테이지가 없는 것인지 안 걸린 것인지 알 수 없다.
            if (only != null && _queue.Count == 0)
                throw new ArgumentException(
                    $"-stage {only} 에 걸리는 레벨이 없다. " +
                    $"{LevelFiles.RelativeFolder} 의 파일 이름으로 적는다.");

            Debug.Log($"[SolverBatch] 레벨 {_queue.Count}개, 프리셋 {presets.Count}개 — {_out}");

            _at = 0;
            StartStage();
        }

        /// 남은 스테이지를 하나 건다. 다 돌았으면 _run 이 null 로 남는다.
        static void StartStage()
        {
            if (_at >= _queue.Count)
            {
                _run = null;
                return;
            }

            // 첫 답에서 멈추지 않는다 — 뷰어의 '시도' 탭이 보는 것이
            // 답 하나가 아니라 굴린 판 전부다.
            _run = _search.Begin(_queue[_at], stopAtClear: false);
            _stageWatch = Stopwatch.StartNew();
            _nextLog = Time.realtimeSinceStartup + LogEvery;
        }

        /// <summary>
        /// 도는 중에도 형편을 알린다. 스테이지 하나가 수천 판이라
        /// 끝나야 한 줄 나오면 콘솔이 몇 분을 조용하다.
        /// </summary>
        static void Progress()
        {
            if (Time.realtimeSinceStartup < _nextLog) return;

            _nextLog = Time.realtimeSinceStartup + LogEvery;

            Debug.Log($"[SolverBatch] {_queue[_at].StageId} — " +
                      $"{_run.Progress * 100f:F0}%  {_run.Tries}판 굴림");
        }

        /// 시간이 찰 때까지 굴리고 프레임을 넘긴다.
        static void Tick()
        {
            try
            {
                float until = Time.realtimeSinceStartup + TickBudget;

                while (_run != null)
                {
                    if (_run.Step())
                    {
                        if (Time.realtimeSinceStartup < until) continue;

                        Progress();
                        return;
                    }

                    FinishStage();
                }

                Finish();
            }
            catch (Exception e)
            {
                EditorApplication.update -= Tick;
                Fail(e);
            }
        }

        /// 다 굴린 스테이지를 리포트에 담고 다음으로 넘어간다.
        static void FinishStage()
        {
            StageData stage = _queue[_at];
            SolveReport result = _run.Report;

            var devices = stage.Level.Devices;

            var report = new StageReport
            {
                stageId = stage.StageId,
                seed = stage.Seed,
                deviceCount = devices == null ? 0 : devices.Count,
                cleared = result.Cleared,
                pass = (int)result.Pass,
                passName = result.Pass.ToString(),
                tries = result.Tries,
                bestGoalDist = result.BestGoalDist,
                seconds = (float)_stageWatch.Elapsed.TotalSeconds,

                // 못 풀었으면 가장 가까이 갔던 그림이 대신 들어간다.
                // 답인지 아닌지는 cleared 로 가른다 —
                // JsonUtility 는 null 도 빈 객체로 적어 여기서는 못 가린다.
                solution = result.Solution ?? result.Closest ?? Solution.Empty,
            };

            List<Attempt> log = result.Log;

            for (int i = 0; log != null && i < log.Count; i++)
            {
                Attempt attempt = log[i];

                report.attempts.Add(new AttemptRow
                {
                    index = i,
                    pass = (int)attempt.Pass,
                    passName = attempt.Pass.ToString(),
                    outcome = attempt.Outcome.ToString(),
                    minGoalDist = attempt.MinGoalDist,
                    endStep = attempt.EndStep,
                    ink = attempt.Ink,
                    area = attempt.Area,
                });
            }

            _report.stages.Add(report);

            Debug.Log($"[SolverBatch] [{_at + 1}/{_queue.Count}] {stage.StageId} — " +
                      $"{result} ({report.seconds:F1}초)");

            _at++;
            StartStage();
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;

            Directory.CreateDirectory(Path.GetDirectoryName(_out));
            File.WriteAllText(_out, JsonUtility.ToJson(_report, true));

            Debug.Log($"[SolverBatch] 끝 — {_watch.Elapsed.TotalSeconds:F1}초, {_out}");
            Quit(0);
        }

        static void Fail(Exception e)
        {
            Debug.LogError($"[SolverBatch] 실패 — {e}");
            Quit(1);
        }

        /// 명령줄에서 name 뒤에 온 값. 없으면 null.
        static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name)
                    return args[i + 1];

            return null;
        }

        /// -out 뒤의 경로. 없으면 시각을 붙인 기본 자리다.
        static string OutPath()
        {
            string given = Arg("-out");
            if (given != null) return Path.GetFullPath(given);

            return Path.GetFullPath(Path.Combine(
                DefaultFolder,
                $"solver-{DateTime.Now:yyyyMMdd-HHmmss}.json"));
        }

        /// 배치로 돌 때만 끝낸다. 사람이 띄운 에디터를 닫으면 안 된다.
        static void Quit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
