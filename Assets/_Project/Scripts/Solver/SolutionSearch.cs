using System.Collections;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>어느 패스에서 풀렸는지.</summary>
    public enum SolvePass
    {
        /// 못 풀었다.
        None = 0,

        /// 통로만으로 풀렸다.
        Corridor = 1,

        /// 지렛대만으로 풀렸다. 벽은 하나도 안 세웠다.
        Lever = 2,

        /// 지렛대로 높은 데 올려놓고, 거기서부터 통로로 굴려 풀렸다.
        LeverThenCorridor = 3,
    }

    /// <summary>
    /// 굴려 본 한 판. 실패한 것도 남긴다 —
    /// 어떤 배치가 어떻게 어긋났는지는 눈으로 봐야 알 수 있다.
    /// </summary>
    public readonly struct Attempt
    {
        public readonly SolvePass Pass;
        public readonly Solution Solution;
        public readonly SimOutcome Outcome;
        public readonly float MinGoalDist;
        public readonly int EndStep;
        public readonly float Ink;

        /// 그림이 차지한 사각형. 정렬에 쓰려고 미리 재 둔다 —
        /// 목록을 그릴 때마다 수백 개를 다시 재면 화면이 느려진다.
        public readonly Rect Area;

        public Attempt(
            SolvePass pass, Solution solution, SimOutcome outcome,
            float minGoalDist, int endStep, float ink, Rect area)
        {
            Pass = pass;
            Solution = solution;
            Outcome = outcome;
            MinGoalDist = minGoalDist;
            EndStep = endStep;
            Ink = ink;
            Area = area;
        }

        public bool Cleared => Outcome == SimOutcome.Clear;

        public override string ToString()
            => $"패스{(int)Pass} {Outcome} 거리 {MinGoalDist:F2} @{EndStep} "
               + $"잉크 {Ink:F1} 영역 {Area.width:F1}x{Area.height:F1}";
    }

    /// <summary>
    /// 한 스테이지를 풀어 본 결과.
    /// 못 푼 경우에도 가장 가까웠던 거리를 남긴다 —
    /// 레벨 검증에서 "못 풀었다"와 "아깝게 못 풀었다"는 다른 신호다.
    /// </summary>
    public readonly struct SolveReport
    {
        public readonly SolvePass Pass;

        /// 풀렸으면 그 그림. 아니면 null.
        public readonly Solution Solution;

        /// <summary>
        /// 목표에 가장 가까이 갔던 그림. 못 푼 경우에 볼 것이다 —
        /// 어디까지 갔다가 어긋났는지를 봐야 무엇이 모자란지 알 수 있다.
        /// </summary>
        public readonly Solution Closest;

        public readonly int Tries;
        public readonly float BestGoalDist;

        /// 굴려 본 순서 그대로. 실패한 것도 들어 있다.
        public readonly List<Attempt> Log;

        public SolveReport(
            SolvePass pass, Solution solution, Solution closest,
            int tries, float bestGoalDist, List<Attempt> log)
        {
            Pass = pass;
            Solution = solution;
            Closest = closest;
            Tries = tries;
            BestGoalDist = bestGoalDist;
            Log = log;
        }

        public bool Cleared => Pass != SolvePass.None;

        public override string ToString()
            => Cleared
                ? $"{Pass} 로 풀림 ({Tries}회 시도)"
                : $"못 풀음 ({Tries}회 시도, 가장 가까웠던 거리 {BestGoalDist:F3})";
    }

    /// <summary>
    /// 통로를 세워 보고, 안 되면 지렛대로 날려 본다.
    /// 순서가 곧 값의 순서다 — 통로가 제일 싸고, 지렛대는 잉크가 적은 것부터다.
    /// 먼저 통하는 것이 답이므로 더 볼 이유가 없다.
    /// </summary>
    public sealed class SolutionSearch
    {
        /// <summary>
        /// 패스 2가 굴려 볼 최대 횟수. 표를 통째로 굴리고 실패마다
        /// 통로까지 이어 보므로 다른 패스보다 크다 —
        /// 표가 500개면 통로를 이어 보기 전에 이미 500회다.
        /// </summary>
        public const int LeverTries = 2000;

        public const int HighGroundTries = 300;

        readonly LeverPresets _presets;

        /// 도달 조회 결과를 담아 돌려 쓴다. 시도마다 새로 잡으면 GC 가 는다.
        readonly List<LeverPreset> _reaching = new List<LeverPreset>();

        public SolutionSearch(LeverPresets presets) => _presets = presets;

        /// <param name="stopAtClear">첫 답에서 멈출지.
        /// 끄면 예산을 다 쓸 때까지 굴려 본다 — 답 하나가 아니라
        /// 어떤 답들이 있었는지를 보고 싶을 때다.</param>
        public SolveReport Solve(StageData stage, bool stopAtClear = true)
        {
            Run run = Begin(stage, stopAtClear);

            while (run.Step()) { }

            return run.Report;
        }

        /// <summary>
        /// 한 판씩 끊어 굴린다. 프레임을 나눠 돌리려는 쪽에서 쓴다 —
        /// 통째로 굴리면 수백 판 도는 동안 화면이 멈춘다.
        /// </summary>
        public Run Begin(StageData stage, bool stopAtClear = true)
            => new Run(this, stage, stopAtClear);

        /// <summary>돌고 있는 탐색 하나. Step 이 거짓을 내면 끝난 것이다.</summary>
        public sealed class Run
        {
            readonly Attempts _attempts;
            readonly IEnumerator _steps;

            internal Run(SolutionSearch search, StageData stage, bool stopAtClear)
            {
                _attempts = new Attempts(stopAtClear);
                _steps = search.Steps(stage, _attempts);
            }

            /// 0~1. 패스 셋을 고르게 나눈 어림값이다 — 실제로 쓰는 예산은
            /// 레벨마다 달라서 굴린 판 수로는 끝을 가늠할 수 없다.
            public float Progress => _attempts.Progress;

            public int Tries => _attempts.Tries;

            /// 한 판 굴린다. 더 굴릴 것이 없으면 거짓.
            public bool Step() => _steps.MoveNext();

            /// 도중에 읽으면 그때까지 굴린 것만 담긴다.
            public SolveReport Report => _attempts.Report();
        }

        /// 패스 하나가 끝날 때마다 진행도가 이만큼씩 찬다.
        const int PassCount = 3;

        static float PassProgress(int pass, int at, int of)
            => (pass + (of == 0 ? 0f : (float)at / of)) / PassCount;

        /// 한 판 굴릴 때마다 멈춘다.
        IEnumerator Steps(StageData stage, Attempts attempts)
        {
            List<Vector2[]> paths = BallPath.Find(stage.Level);

            // 통로 수가 곧 상한이라 이 패스만 예산을 안 연다.
            for (int i = 0; i < paths.Count && !attempts.Satisfied; i++)
            {
                attempts.Progress = PassProgress(0, i, paths.Count);
                attempts.Run(SolvePass.Corridor, stage, Corridor(stage, paths[i]), out _);
                yield return null;
            }

            attempts.BeginPass(LeverTries);

            IEnumerator lever = ByLever(stage, attempts);
            while (lever.MoveNext()) yield return null;

            attempts.BeginPass(HighGroundTries);

            IEnumerator high = FromHighGround(stage, attempts);
            while (high.MoveNext()) yield return null;

            attempts.Progress = 1f;
        }

        static Solution Corridor(StageData stage, Vector2[] path)
        {
            var solution = new Solution();
            IPrimitive[] walls = PrimitiveCandidates.Select(stage, path);

            for (int i = 0; i < walls.Length; i++) walls[i].AppendTo(solution);

            return solution;
        }

        // ── 패스 2 ──

        /// <summary>
        /// 표에 있는 지렛대를 하나씩, 벽 없이 굴려 본다. 벽이 없으니
        /// 판이 걸릴 것도, 날아가는 공이 막힐 것도 없다.
        /// 도달 조회로 후보를 추리지 않는다 — 어디로 날아갈지 어긋나는 것이
        /// 이 패스가 다루려는 일인데, 예측으로 걸러 내면 어긋난 것부터 빠진다.
        /// 판은 공의 출발 자리에만 놓는다 — 프리셋의 발사 상태는 공이 판에
        /// 얹힌 채 추가 떨어지는 것을 잰 값이라, 공이 나중에 도착하는
        /// 자리에 놓으면 추가 이미 떨어진 뒤다.
        /// </summary>
        IEnumerator ByLever(StageData stage, Attempts attempts)
        {
            LevelData level = stage.Level;
            Vector2 seat = level.BallStart;

            // 표는 오른쪽으로만 잰 것이라 방향은 여기서 정한다.
            bool towardGoal = level.GoalPosition.x >= seat.x;

            var reachedPeaks = new HashSet<long>();

            for (int i = 0; i < _presets.Count; i++)
            {
                if (attempts.Spent || attempts.Satisfied) yield break;

                attempts.Progress = PassProgress(1, i, _presets.Count);

                Lever lever = _presets[i].ToLever(seat, towardGoal);
                if (!Fits(level, lever)) continue;

                var solution = new Solution();
                lever.AppendTo(solution);

                bool cleared = attempts.Run(SolvePass.Lever, stage, solution, out BallSample peak);
                yield return null;

                if (cleared) continue;

                IEnumerator onward = FromPeak(stage, lever, peak, reachedPeaks, attempts);
                while (onward.MoveNext()) yield return null;
            }
        }

        /// <summary>
        /// 지렛대가 실제로 공을 올려놓은 꼭짓점에서 통로를 이어 본다.
        /// 겨눈 자리와 공이 간 자리는 어긋난다 — 판이 미는 힘은 공이
        /// 얹힌 자리와 맞물린 상태에 따라 달라서 프리셋대로 날지 않는다.
        /// 어긋난 자리라도 목표보다 높으면 거기서부터는 굴려서 갈 수 있다.
        /// </summary>
        /// <param name="reachedPeaks">이미 통로를 이어 본 칸들. 비슷한 프리셋은
        /// 거의 같은 자리로 가는데 통로를 다시 찾는 것이 비싸다.</param>
        IEnumerator FromPeak(
            StageData stage, Lever lever, BallSample peak,
            HashSet<long> reachedPeaks, Attempts attempts)
        {
            LevelData level = stage.Level;

            // 목표보다 낮은 자리에 올려놓은 것은 아무 이득이 없다.
            if (peak.Step == 0 || peak.Position.y <= level.GoalPosition.y) yield break;
            if (!reachedPeaks.Add(CellKey(peak.Position))) yield break;

            List<Vector2[]> onward = BallPath.Find(Rebased(level, peak.Position));

            for (int o = 0; o < onward.Count; o++)
            {
                if (attempts.Spent || attempts.Satisfied) yield break;

                Solution solution = Corridor(stage, onward[o]);

                OpenEntry(solution, peak.Position, peak.Velocity);
                lever.AppendTo(solution);

                attempts.Run(SolvePass.LeverThenCorridor, stage, solution, out _);
                yield return null;
            }
        }

        // ── 패스 3 ──

        /// <summary>
        /// 목표보다 높은 자리에 공을 올려놓고, 거기서부터 통로로 굴린다.
        /// 지렛대 하나로 목표까지 닿지 않아도, 높은 데 얹어 두면
        /// 나머지는 중력이 해 준다.
        /// 통로는 착지점에서 다시 찾는다 — 공의 출발점에서 낸 통로는
        /// 착지점과 상관이 없다.
        /// </summary>
        IEnumerator FromHighGround(StageData stage, Attempts attempts)
        {
            if (attempts.Satisfied) yield break;

            LevelData level = stage.Level;
            Vector2 seat = level.BallStart;

            List<Vector2> landings = Landings(level);

            for (int p = 0; p < landings.Count; p++)
            {
                attempts.Progress = PassProgress(2, p, landings.Count);

                Vector2 landing = landings[p];

                // 통로는 착지점에만 달렸다. 프리셋마다 다시 찾을 이유가 없다.
                List<Vector2[]> onward = BallPath.Find(Rebased(level, landing));
                if (onward.Count == 0) continue;

                Vector2 target = landing - seat;
                _presets.Reaching(target, _reaching);

                for (int i = 0; i < _reaching.Count; i++)
                {
                    LeverPreset preset = _reaching[i];
                    Lever lever = preset.ToLever(seat, target.x >= 0f);
                    if (!Fits(level, lever)) continue;

                    for (int o = 0; o < onward.Count; o++)
                    {
                        if (attempts.Spent || attempts.Satisfied) yield break;

                        Solution solution = Corridor(stage, onward[o]);

                        OpenPredictedEntry(solution, preset, seat, target);
                        lever.AppendTo(solution);

                        attempts.Run(SolvePass.LeverThenCorridor, stage, solution, out _);
                        yield return null;
                    }
                }
            }
        }

        /// 통로를 다시 찾는 것이 비싸서 후보 수를 여기서 끊는다.
        const int MaxLandings = 24;

        /// <summary>
        /// 지렛대로 갈 수 있는 자리 가운데 목표보다 높은 것들.
        /// 통로의 점이 아니라 프리셋 궤적이 지나가는 자리 전부에서 뽑는다 —
        /// 통로의 점은 지형 모서리에 붙어 있어 착지 자리로는 치우쳐 있다.
        /// 목표에 가까운 것부터 본다. 남은 통로가 짧을수록 어긋날 데가 적다.
        /// </summary>
        List<Vector2> Landings(LevelData level)
        {
            Vector2 seat = level.BallStart;
            float goalY = level.GoalPosition.y;

            var seen = new HashSet<long>();
            var found = new List<Vector2>();

            for (int i = 0; i < _presets.Count; i++)
            {
                LeverPreset preset = _presets[i];
                Vector2 from = preset.LaunchOffset;

                for (int step = 0; step <= LeverPresets.MaxFlight; step++)
                {
                    Vector2 at = seat + Ballistic.At(from, preset.LaunchVelocity, step);

                    if (at.y <= goalY)
                    {
                        // 목표보다 낮은데 내려가는 중이면 다시 올라오지 않는다.
                        // 올라가는 중이면 아직 지나갈 자리가 남았다.
                        if (Ballistic.VelocityAt(preset.LaunchVelocity, step).y < 0f) break;

                        continue;
                    }

                    Vector2 cell = CellCenter(at);

                    if (!seen.Add(CellKey(cell))) continue;
                    if (Buried(level, cell)) continue;

                    found.Add(cell);
                }
            }

            Vector2 goal = level.GoalPosition;
            found.Sort((x, y) =>
                (x - goal).sqrMagnitude.CompareTo((y - goal).sqrMagnitude));

            if (found.Count > MaxLandings) found.RemoveRange(MaxLandings, found.Count - MaxLandings);

            return found;
        }

        // ── 자리 ──

        /// 궤적 위의 자리를 이 간격의 격자로 접는다. 궤적은 이어져 있어
        /// 그냥 두면 후보가 끝없이 나온다.
        const float LandingCell = 0.5f;

        static Vector2 CellCenter(Vector2 at)
            => new Vector2(
                Mathf.Round(at.x / LandingCell) * LandingCell,
                Mathf.Round(at.y / LandingCell) * LandingCell);

        static long CellKey(Vector2 at)
            => (long)Mathf.RoundToInt(at.x / LandingCell) * 100000L
               + Mathf.RoundToInt(at.y / LandingCell);

        /// 지형에 파묻힌 자리인가. 거기서 공이 출발할 수는 없다.
        static bool Buried(LevelData level, Vector2 at)
        {
            var terrain = level.Terrain;
            if (terrain == null) return false;

            for (int i = 0; i < terrain.Count; i++)
                if (Headroom.Gap(at, at, terrain[i].A, terrain[i].B) < LevelData.BallRadius)
                    return true;

            return false;
        }

        /// <summary>
        /// BallPath 에 물어보려고 출발점만 바꾼 레벨.
        /// 지형과 장치는 읽기만 하므로 그대로 나눠 쓴다 —
        /// 실제로 굴릴 때 쓰는 레벨은 이것이 아니다.
        /// </summary>
        static LevelData Rebased(LevelData level, Vector2 start)
            => new LevelData
            {
                InkLimit = level.InkLimit,
                BallStart = start,
                GoalPosition = level.GoalPosition,
                Terrain = level.Terrain,
                Devices = level.Devices,
                Stars = level.Stars,
                KillY = level.KillY,
            };

        // ── 통로 입구 ──

        /// <summary>
        /// 공이 들어오는 만큼만 뚫을 깊이.
        /// 입구만 열고 안쪽은 남겨야 공이 통로 안에 머문다 —
        /// 궤적 전체로 뚫으면 먼 데까지 구멍이 나서 굴러가다 새어 나간다.
        /// </summary>
        const float EntryDepth = LevelData.BallRadius * 6f;

        /// <summary>
        /// 공이 들어오는 쪽 벽을 걷어낸다.
        /// 착지점에서 낸 통로는 공이 굴러서 올 것을 전제로 둘러싸는데,
        /// 이 공은 날아든다 — 어느 쪽을 열지는 진입 속도가 알려 준다.
        /// 지렛대로 올려친 공이면 아래에서 오므로 아래가 열린다.
        /// </summary>
        static void OpenEntry(Solution corridor, Vector2 at, Vector2 coming)
        {
            if (coming.sqrMagnitude <= 1e-6f) return;

            Vector2 mouth = at - coming.normalized * EntryDepth;
            float clear = LevelData.BallRadius + ColliderFactory.FreeBodyHalfWidth;

            for (int i = corridor.Strokes.Count - 1; i >= 0; i--)
            {
                var points = corridor.Strokes[i].Points;
                if (points == null || points.Count < 2) continue;

                if (Headroom.Gap(mouth, at, points[0], points[1]) < clear)
                    corridor.Strokes.RemoveAt(i);
            }
        }

        /// 아직 안 굴려 본 판이라 진입 자리와 속도를 탄도로 미리 푼다.
        static void OpenPredictedEntry(
            Solution corridor, in LeverPreset preset, Vector2 seat, Vector2 target)
        {
            int flight = preset.StepTo(target, LeverPresets.ReachTolerance, LeverPresets.MaxFlight)
                         - preset.LaunchStep;
            if (flight <= 0) return;

            OpenEntry(
                corridor,
                seat + Ballistic.At(preset.LaunchOffset, preset.LaunchVelocity, flight),
                Ballistic.VelocityAt(preset.LaunchVelocity, flight));
        }

        // ── 지렛대가 들어갈 자리 ──

        /// 천장이 없을 때 여유로 칠 값. 어떤 프리셋도 통과한다.
        const float OpenSky = 1000f;

        /// <summary>
        /// 판이 돌 수 있어야 하는 각(라디안). 약 34도다.
        /// 공이 뜨기까지 실제로 몇 도 도는지는 아직 안 재 봤다 —
        /// 프리셋에 발사 시점의 각을 같이 담으면 이 값을 지울 수 있다.
        /// </summary>
        const float SwingAngle = 0.6f;

        const int SwingSamples = 6;

        /// <summary>
        /// 이 자리에 지렛대가 들어가는가.
        /// 판이 지형에 박힌 채로 시작하거나 도는 길이 막혀 있으면
        /// 물리가 밀어내며 튀어서, 그 결과는 실측한 프리셋과 상관이 없다.
        /// </summary>
        static bool Fits(LevelData level, in Lever lever)
        {
            float margin = ColliderFactory.FreeBodyHalfWidth * 2f;

            // 1. 추가 떨어질 자리가 위로 나와야 한다.
            if (Headroom.Above(level.Terrain, lever.WeightFoot, lever.WeightWidth, OpenSky)
                < lever.RequiredHeadroom)
                return false;

            // 2. 판이 도는 길이 비어 있어야 한다.
            for (int s = 1; s <= SwingSamples; s++)
            {
                lever.Swept(SwingAngle * s / SwingSamples, out Vector2 from, out Vector2 to);

                if (!Headroom.Clear(level.Terrain, from, to, margin)) return false;
            }

            // 3. 시작부터 지형에 박혀 있으면 안 된다.
            return Headroom.Clear(level.Terrain, lever.Origin, lever.PlankEnd, margin);
        }

        /// <summary>
        /// 굴려 본 것들을 세고, 가장 가까이 갔던 그림을 붙잡아 둔다.
        /// 모든 패스가 같은 기준으로 세야 보고가 맞는다.
        /// </summary>
        sealed class Attempts
        {
            /// <summary>
            /// 궤적을 몇 스텝마다 찍는지. 꼭짓점 언저리는 수직 속도가
            /// 0 에 가까워 평평하므로 성기게 찍어도 높이가 크게 안 어긋난다.
            /// </summary>
            const int PeakInterval = 4;

            readonly bool _stopAtClear;
            readonly List<Attempt> _log = new List<Attempt>();

            /// 궤적을 담아 돌려 쓴다. 시도마다 새로 잡으면 GC 가 는다.
            readonly TrajectoryBuffer _trajectory = new TrajectoryBuffer(PeakInterval);

            int _passStart;
            int _passBudget;

            float _bestGoalDist = float.PositiveInfinity;
            Solution _closest;

            /// 처음으로 목표에 닿은 판. 답으로 삼는다.
            SolvePass _answerPass;
            Solution _answer;

            public Attempts(bool stopAtClear) => _stopAtClear = stopAtClear;

            /// 0~1. 굴리는 쪽이 패스 진행에 맞춰 채운다.
            public float Progress;

            public int Tries { get; private set; }

            public bool Spent => Tries - _passStart >= _passBudget;

            /// 답을 찾았고 거기서 멈추기로 했는가.
            public bool Satisfied => _answer != null && _stopAtClear;

            /// 새 패스를 연다. 예산은 여기서부터 다시 센다.
            public void BeginPass(int budget)
            {
                _passStart = Tries;
                _passBudget = budget;
            }

            /// 한 판 굴린다. 목표에 닿았으면 참이다.
            /// <param name="peak">이 판에서 공이 가장 높았던 시점.
            /// 샘플이 하나도 없었으면 Step 이 0 이다.</param>
            public bool Run(
                SolvePass pass, StageData stage, Solution solution, out BallSample peak)
            {
                SimResult result = SimRunner.RunSampled(
                    stage.Level, solution, stage.Seed, _trajectory);

                peak = Highest(_trajectory);
                Tries++;

                _log.Add(new Attempt(
                    pass, solution, result.Outcome, result.MinGoalDist, result.EndStep,
                    solution.TotalInk(), Extent(solution)));

                if (result.MinGoalDist < _bestGoalDist)
                {
                    _bestGoalDist = result.MinGoalDist;
                    _closest = solution;
                }

                // 첫 답만 붙잡는다. 뒤에 더 나와도 앞의 것이 더 싼 패스에서 나온 것이다.
                if (result.Cleared && _answer == null)
                {
                    _answer = solution;
                    _answerPass = pass;
                }

                return result.Cleared;
            }

            public SolveReport Report()
                => new SolveReport(_answerPass, _answer, _closest, Tries, _bestGoalDist, _log);

            /// 샘플이 없으면 Step 0 인 기본값이다 — 실제 샘플은 첫 스텝보다 뒤에서만 찍힌다.
            static BallSample Highest(TrajectoryBuffer trajectory)
            {
                var top = default(BallSample);

                for (int i = 0; i < trajectory.Count; i++)
                    if (top.Step == 0 || trajectory[i].Position.y > top.Position.y)
                        top = trajectory[i];

                return top;
            }

            /// 그림이 차지한 사각형. 점이 없으면 넓이 0 이다.
            static Rect Extent(Solution solution)
            {
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                bool any = false;

                for (int i = 0; i < solution.Strokes.Count; i++)
                {
                    var points = solution.Strokes[i].Points;
                    if (points == null) continue;

                    for (int p = 0; p < points.Count; p++)
                    {
                        min = Vector2.Min(min, points[p]);
                        max = Vector2.Max(max, points[p]);
                        any = true;
                    }
                }

                return any ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : new Rect();
            }
        }
    }
}
