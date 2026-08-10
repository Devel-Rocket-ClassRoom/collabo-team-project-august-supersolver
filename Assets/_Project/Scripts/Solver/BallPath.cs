using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공이 목표까지 갈 수 있는 가장 짧은 길.
    /// 지형을 공 반지름만큼 부풀린 공간에서 찾으므로
    /// 나온 경로는 공이 실제로 지날 수 있는 폭이 확보되어 있다.
    /// 궤적이 아니라 통로다 — 중력은 보지 않는다.
    /// </summary>
    public static class BallPath
    {
        /// 같은 자리로 볼 노드 간 거리. 이어진 선분이 공유하는 끝점을 접는다.
        const float NodeMerge = 0.0001f;

        /// <summary>
        /// 지형 끝점 둘레에 자리를 몇 개 찍을지.
        /// 부푼 지형의 모서리는 원호인데 노드는 점이라, 경로가 그 원호를
        /// 이 각도만큼 각지게 돈다. 촘촘할수록 매끄럽고 노드가 는다.
        /// </summary>
        const int RingSamples = 12;

        /// <summary>
        /// 거리 비교에 둘 여유.
        /// 노드가 정확히 공 반지름 위에 놓여서, 없으면 제 자리를 만든
        /// 그 선분에 걸려 전부 버려진다.
        /// </summary>
        const float Slack = 0.0001f;

        /// <summary>
        /// 지형 끝점마다 하나씩, 그 끝점을 돌아 목표까지 가는 최단 경로.
        /// 끝점이 곧 갈림길이라 여기서 서로 다른 길이 갈린다 —
        /// 최단 하나만 내면 나머지 갈래가 전부 묻힌다.
        /// 같은 길이 여러 번 나오면 한 번만 담는다.
        /// </summary>
        public static List<Vector2[]> Find(LevelData level)
        {
            Vector2[] nodes = Nodes(level, out int[] owner, out int corners);

            float[] fromBall = Walk(level, nodes, 0, out int[] backBall);
            float[] fromGoal = Walk(level, nodes, 1, out int[] backGoal);

            var paths = new List<Vector2[]>();

            for (int corner = 0; corner < corners; corner++)
            {
                int best = -1;
                float least = float.PositiveInfinity;

                // 한 끝점 둘레의 자리 중 양쪽 합이 가장 짧은 곳으로 지난다.
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (owner[i] != corner) continue;

                    float total = fromBall[i] + fromGoal[i];
                    if (total >= least) continue;

                    least = total;
                    best = i;
                }

                if (best < 0 || float.IsPositiveInfinity(least)) continue;

                Vector2[] path = Pull(Stitch(nodes, backBall, backGoal, best), level);

                // 팽팽하게 당기면 같은 갈래로 도는 길들이 하나로 겹친다.
                // 겹치면 짧은 쪽만 남긴다.
                int same = IndexOfSame(paths, path);

                if (same < 0) paths.Add(path);
                else if (Length(path) < Length(paths[same])) paths[same] = path;
            }

            return paths;
        }

        /// <summary>
        /// 지날 이유가 없는 꼭짓점을 걷어낸다.
        /// 끝점을 하나씩 강제로 지나게 해서 뽑은 길이라, 그 끝점이
        /// 실은 돌아갈 필요가 없으면 길이 불룩하게 남는다.
        /// 당기고 나면 같은 갈래는 같은 모양이 되어 서로 겹쳐진다.
        /// </summary>
        static Vector2[] Pull(Vector2[] path, LevelData level)
        {
            var points = new List<Vector2>(path);

            for (int i = 1; i + 1 < points.Count; )
            {
                if (Blocked(level, points[i - 1], points[i + 1]))
                {
                    i++;
                    continue;
                }

                points.RemoveAt(i);

                // 하나 지우면 앞쪽도 다시 볼 수 있게 된다.
                if (i > 1) i--;
            }

            return points.ToArray();
        }

        static float Length(Vector2[] path)
        {
            float sum = 0f;
            for (int i = 0; i + 1 < path.Length; i++)
                sum += Vector2.Distance(path[i], path[i + 1]);

            return sum;
        }

        /// <summary>
        /// source 에서 모든 노드까지의 최단 거리. back 에는 오던 길을 남긴다.
        /// 노드가 수십 개라 힙 없이 훑어 꺼낸다.
        /// </summary>
        static float[] Walk(LevelData level, Vector2[] nodes, int source, out int[] back)
        {
            var best = new float[nodes.Length];
            var settled = new bool[nodes.Length];
            back = new int[nodes.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                best[i] = float.PositiveInfinity;
                back[i] = -1;
            }

            best[source] = 0f;

            while (true)
            {
                int at = -1;
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (settled[i] || float.IsPositiveInfinity(best[i])) continue;
                    if (at < 0 || best[i] < best[at]) at = i;
                }

                if (at < 0) break;
                settled[at] = true;

                for (int next = 0; next < nodes.Length; next++)
                {
                    if (settled[next] || next == at) continue;
                    if (Blocked(level, nodes[at], nodes[next])) continue;

                    float length = best[at] + Vector2.Distance(nodes[at], nodes[next]);
                    if (length >= best[next]) continue;

                    best[next] = length;
                    back[next] = at;
                }
            }

            return best;
        }

        /// 공에서 through 까지 오던 길과, 거기서 목표까지 가던 길을 잇는다.
        static Vector2[] Stitch(Vector2[] nodes, int[] backBall, int[] backGoal, int through)
        {
            var points = new List<Vector2>();

            for (int at = through; at >= 0; at = backBall[at]) points.Add(nodes[at]);
            points.Reverse();

            for (int at = backGoal[through]; at >= 0; at = backGoal[at]) points.Add(nodes[at]);

            return points.ToArray();
        }

        /// 같은 길이 이미 있으면 그 자리. 없으면 -1.
        static int IndexOfSame(List<Vector2[]> paths, Vector2[] path)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                Vector2[] other = paths[i];
                if (other.Length != path.Length) continue;

                bool same = true;
                for (int p = 0; p < path.Length && same; p++)
                    same = Vector2.Distance(other[p], path[p]) < NodeMerge;

                if (same) return i;
            }

            return -1;
        }

        // ── 노드 ──

        /// <summary>
        /// 0 번은 공, 1 번은 목표, 나머지는 지형 끝점 둘레에
        /// 공 반지름만큼 띄운 자리들이다.
        /// 부푼 지형의 경계 위에 노드를 깔아 두는 것이고,
        /// 지형에 파묻히는 자리만 버린다.
        /// </summary>
        /// <param name="owner">그 자리가 어느 끝점의 둘레인지. 공과 목표는 -1.</param>
        /// <param name="cornerCount">겹치는 것을 접고 남은 끝점 수.</param>
        static Vector2[] Nodes(LevelData level, out int[] owner, out int cornerCount)
        {
            var nodes = new List<Vector2> { level.BallStart, level.GoalPosition };
            var owners = new List<int> { -1, -1 };

            cornerCount = 0;
            var terrain = level.Terrain;

            if (terrain == null)
            {
                owner = owners.ToArray();
                return nodes.ToArray();
            }

            var corners = new List<Vector2>();
            for (int i = 0; i < terrain.Count; i++)
            {
                Include(corners, terrain[i].A);
                Include(corners, terrain[i].B);
            }

            cornerCount = corners.Count;

            // 노드를 반지름에 딱 맞춰 놓으면 이웃한 두 노드를 잇는 현이
            // 원 안쪽으로 파고들어, 모서리를 감아 도는 간선이 전부 막힌다.
            // 외접시켜 두면 현의 가장 깊은 곳이 정확히 반지름에 온다.
            float ring = level.BallRadius / Mathf.Cos(Mathf.PI / RingSamples);

            for (int i = 0; i < corners.Count; i++)
            {
                for (int k = 0; k < RingSamples; k++)
                {
                    float angle = 2f * Mathf.PI * k / RingSamples;
                    Vector2 at = corners[i] + new Vector2(
                        Mathf.Cos(angle), Mathf.Sin(angle)) * ring;

                    if (Clearance(terrain, at) < level.BallRadius - Slack) continue;

                    int before = nodes.Count;
                    Include(nodes, at);

                    if (nodes.Count > before) owners.Add(i);
                }
            }

            owner = owners.ToArray();
            return nodes.ToArray();
        }

        static void Include(List<Vector2> nodes, Vector2 point)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (Vector2.Distance(nodes[i], point) < NodeMerge) return;

            nodes.Add(point);
        }

        // ── 간선 ──

        /// <summary>
        /// 이 간선으로 공이 지나갈 수 없는가.
        /// 뚫는지가 아니라 공 반지름만큼 떨어져 있는지를 본다 —
        /// 지형을 안 뚫어도 스치듯 지나면 공이 낀다.
        /// </summary>
        static bool Blocked(LevelData level, Vector2 from, Vector2 to)
        {
            var terrain = level.Terrain;
            if (terrain == null) return false;

            // 양 끝이 이미 지형에 더 가까우면 그만큼만 요구한다.
            // 목표가 바닥에 붙어 있으면 목표로 가는 간선이 통째로 사라진다.
            float need = Mathf.Min(level.BallRadius,
                Mathf.Min(Clearance(terrain, from), Clearance(terrain, to)));

            for (int i = 0; i < terrain.Count; i++)
                if (Distance(from, to, terrain[i].A, terrain[i].B) < need - Slack)
                    return true;

            return false;
        }

        /// 이 자리에서 가장 가까운 지형까지의 거리.
        static float Clearance(List<StaticSegment> terrain, Vector2 point)
        {
            float least = float.PositiveInfinity;

            for (int i = 0; i < terrain.Count; i++)
                least = Mathf.Min(least, PointToSegment(point, terrain[i].A, terrain[i].B));

            return least;
        }

        static float Distance(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            if (Crosses(a0, a1, b0, b1)) return 0f;

            return Mathf.Min(
                Mathf.Min(PointToSegment(a0, b0, b1), PointToSegment(a1, b0, b1)),
                Mathf.Min(PointToSegment(b0, a0, a1), PointToSegment(b1, a0, a1)));
        }

        static float PointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 along = b - a;
            float lengthSq = along.sqrMagnitude;

            if (lengthSq <= Mathf.Epsilon) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / lengthSq);
            return Vector2.Distance(point, a + along * t);
        }

        /// <summary>
        /// 두 선분이 서로의 안쪽에서 교차하는가.
        /// 끝점이 닿기만 한 경우는 거짓이다.
        /// </summary>
        static bool Crosses(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            float d1 = Cross(b1 - b0, a0 - b0);
            float d2 = Cross(b1 - b0, a1 - b0);
            float d3 = Cross(a1 - a0, b0 - a0);
            float d4 = Cross(a1 - a0, b1 - a0);

            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
