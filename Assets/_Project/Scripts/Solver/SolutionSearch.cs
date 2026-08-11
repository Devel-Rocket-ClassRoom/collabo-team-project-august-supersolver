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

        public Attempt(
            SolvePass pass, Solution solution, SimOutcome outcome, float minGoalDist, int endStep)
        {
            Pass = pass;
            Solution = solution;
            Outcome = outcome;
            MinGoalDist = minGoalDist;
            EndStep = endStep;
        }

        public override string ToString()
            => $"패스{(int)Pass} {Outcome} 거리 {MinGoalDist:F2} @{EndStep}";
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

        /// 굴려 본 횟수.
        public readonly int Tries;

        /// 모든 시도를 통틀어 목표에 가장 가까웠던 거리.
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
        /// 패스 하나가 굴려 볼 최대 횟수.
        /// 패스별로 나누는 것이 중요하다 — 통틀어 세면 앞 패스가
        /// 예산을 다 써서 뒤 패스는 한 번도 안 돌아 본 채 끝난다.
        /// </summary>
        public const int MaxTriesPerPass = 300;

        /// 천장이 없을 때 여유로 칠 값. 어떤 프리셋도 통과한다.
        const float OpenSky = 1000f;

        readonly LeverPresets _presets;

        /// 조회 결과를 담아 돌려 쓴다. 시도마다 새로 잡으면 GC 가 는다.
        readonly List<LeverPreset> _reaching = new List<LeverPreset>();

        public SolutionSearch(LeverPresets presets) => _presets = presets;

        public SolveReport Solve(StageData stage)
        {
            List<Vector2[]> paths = BallPath.Find(stage.Level);
            var attempts = new Attempts();

            // 1. 통로만. 어느 통로든 이것으로 풀리면 가장 싼 답이다.
            for (int i = 0; i < paths.Count; i++)
            {
                Solution corridor = Corridor(stage, paths[i]);

                if (attempts.Run(SolvePass.Corridor, stage, corridor))
                    return attempts.Done(SolvePass.Corridor, corridor);
            }

            // 2. 지렛대로 통로의 점까지 바로 보낸다. 벽은 안 세운다 —
            //    벽이 없으니 판이 걸릴 것도, 날아가는 공이 막힐 것도 없다.
            attempts.Begin();
            for (int i = 0; i < paths.Count && !attempts.Spent; i++)
            {
                Solution found = WithLever(stage, paths[i], attempts);

                if (found != null) return attempts.Done(SolvePass.Lever, found);
            }

            // 3. 목표보다 높은 데로 올려놓고 거기서부터 굴린다.
            attempts.Begin();
            {
                Solution found = FromHighGround(stage, attempts);

                if (found != null) return attempts.Done(SolvePass.LeverThenCorridor, found);
            }

            return attempts.Done(SolvePass.None, null);
        }


        /// <summary>
        /// 굴려 본 것들을 세고, 가장 가까이 갔던 그림을 붙잡아 둔다.
        /// 두 패스가 같은 기준으로 세야 보고가 맞는다.
        /// </summary>
        sealed class Attempts
        {
            public int Tries;

            readonly List<Attempt> _log = new List<Attempt>();

            /// 지금 패스가 시작한 시점의 횟수.
            int _mark;

            /// 새 패스를 연다. 예산은 여기서부터 다시 센다.
            public void Begin() => _mark = Tries;

            /// 이 패스의 예산을 다 썼는가.
            public bool Spent => Tries - _mark >= MaxTriesPerPass;

            float _best = float.PositiveInfinity;
            Solution _closest;

            /// 한 판 굴린다. 목표에 닿았으면 참이다.
            public bool Run(SolvePass pass, StageData stage, Solution solution)
            {
                SimResult result = SimRunner.Run(stage, solution);
                Tries++;

                _log.Add(new Attempt(
                    pass, solution, result.Outcome, result.MinGoalDist, result.EndStep));

                if (result.MinGoalDist < _best)
                {
                    _best = result.MinGoalDist;
                    _closest = solution;
                }

                return result.Cleared;
            }

            public SolveReport Done(SolvePass pass, Solution solution)
                => new SolveReport(pass, solution, _closest, Tries, _best, _log);
        }

        static Solution Corridor(StageData stage, Vector2[] path)
        {
            var solution = new Solution();
            IPrimitive[] walls = PrimitiveCandidates.Select(stage, path);

            for (int i = 0; i < walls.Length; i++) walls[i].AppendTo(solution);

            return solution;
        }

        /// <summary>
        /// 주어진 벽에 지렛대를 하나 더해 본다.
        /// 공의 출발 자리에만 놓는다 — 프리셋의 발사 상태는 공이 판에
        /// 얹힌 채 추가 떨어지는 것을 잰 값이라, 공이 나중에 도착하는
        /// 자리에 놓으면 추가 이미 떨어진 뒤다.
        /// 목표는 통로의 지점들이고, 먼 곳부터 본다 — 멀리 갈수록 이득이다.
        /// </summary>
        Solution WithLever(StageData stage, Vector2[] path, Attempts attempts)
        {
            LevelData level = stage.Level;
            Vector2 seat = level.BallStart;

            for (int at = path.Length - 1; at >= 1; at--)
            {
                Vector2 target = path[at] - seat;
                _presets.Reaching(target, _reaching);

                for (int i = 0; i < _reaching.Count; i++)
                {
                    if (attempts.Spent) return null;

                    Lever lever = _reaching[i].ToLever(seat, target.x >= 0f);
                    if (!Fits(level, lever)) continue;

                    var solution = new Solution();
                    lever.AppendTo(solution);

                    if (attempts.Run(SolvePass.Lever, stage, solution)) return solution;
                }
            }

            return null;
        }

        /// <summary>
        /// 목표보다 높은 자리에 공을 올려놓고, 거기서부터 통로로 굴린다.
        /// 지렛대 하나로 목표까지 닿지 않아도, 높은 데 얹어 두면
        /// 나머지는 중력이 해 준다.
        /// 통로는 착지점에서 다시 찾는다 — 공의 출발점에서 낸 통로는
        /// 착지점과 상관이 없다.
        /// </summary>
        Solution FromHighGround(StageData stage, Attempts attempts)
        {
            LevelData level = stage.Level;
            Vector2 seat = level.BallStart;

            List<Vector2> landings = Landings(level);

            for (int p = 0; p < landings.Count; p++)
            {
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
                        if (attempts.Spent) return null;

                        Solution solution = Corridor(stage, onward[o]);

                        OpenEntry(solution, preset, seat, target);
                        lever.AppendTo(solution);

                        if (attempts.Run(SolvePass.LeverThenCorridor, stage, solution)) return solution;
                    }
                }
            }

            return null;
        }

        /// 착지 후보를 이 간격의 격자로 접는다. 궤적은 이어져 있어 그냥 두면 끝이 없다.
        const float LandingCell = 0.5f;

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

                    var cell = new Vector2(
                        Mathf.Round(at.x / LandingCell) * LandingCell,
                        Mathf.Round(at.y / LandingCell) * LandingCell);

                    if (!seen.Add(Key(cell))) continue;
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

        static long Key(Vector2 cell)
            => (long)Mathf.RoundToInt(cell.x / LandingCell) * 100000L
               + Mathf.RoundToInt(cell.y / LandingCell);

        /// 공이 있을 수 없는 자리인가. 지형에 파묻힌 곳에서 시작할 수는 없다.
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

        /// <summary>
        /// 공이 들어오는 만큼 뚫을 깊이.
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
        static void OpenEntry(
            Solution corridor, in LeverPreset preset, Vector2 seat, Vector2 target)
        {
            int flight = preset.StepTo(target, LeverPresets.ReachTolerance, LeverPresets.MaxFlight)
                         - preset.LaunchStep;
            if (flight <= 0) return;

            Vector2 coming = Ballistic.VelocityAt(preset.LaunchVelocity, flight);
            if (coming.sqrMagnitude <= 1e-6f) return;

            Vector2 at = seat + Ballistic.At(preset.LaunchOffset, preset.LaunchVelocity, flight);
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

        /// <summary>
        /// 판이 돌 수 있어야 하는 각(라디안). 약 34도다.
        /// 공이 뜨기까지 실제로 몇 도 도는지는 아직 안 재 봤다 —
        /// 프리셋에 발사 시점의 각을 같이 담으면 이 값을 지울 수 있다.
        /// </summary>
        const float SwingAngle = 0.6f;

        /// 회전 공간을 몇 자리에서 보는지.
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
    }
}
