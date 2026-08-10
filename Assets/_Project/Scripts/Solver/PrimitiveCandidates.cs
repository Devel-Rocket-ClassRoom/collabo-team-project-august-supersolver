using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 통로 하나를 따라가게 하는 그림 후보를 고른다.
    /// 양쪽에 벽을 세워 공을 가둔다 — 받치기만 하면 공이 옆으로 새지만
    /// 가두면 통로를 벗어날 수 없다.
    /// 위치·각도·크기가 전부 통로에서 유도되어 고를 여지가 없다.
    /// </summary>
    public static class PrimitiveCandidates
    {
        /// <summary>
        /// 통로 반폭을 공 반지름의 몇 배로 잡을지.
        /// 딱 반지름이면 공이 양쪽 벽에 동시에 닿아 마찰로 멎는다.
        /// 넓히면 헐거워지는데, 통로 자체가 지형에서 반지름만큼밖에
        /// 안 떨어져 있어 너무 넓히면 벽이 지형을 파고든다.
        /// </summary>
        const float WallSpread = 1.6f;

        /// <summary>
        /// 바깥쪽 이음을 몇 라디안마다 꺾을지.
        /// 이음이 호라서 점으로 근사한다 — 성기면 각지고, 촘촘하면 벽이 는다.
        /// </summary>
        const float ArcStep = 0.5f;

        /// <summary>
        /// 벽을 남길 기준. 반폭의 이 비율보다 통로에 가까우면 잘라 낸다.
        /// 1 이 아닌 이유는 호를 현으로 근사하면서 반폭보다 2% 남짓
        /// 안쪽으로 처지기 때문이다. 실제 침범은 25% 넘게 파고들어
        /// 이 사이에서 확실히 갈린다.
        /// </summary>
        const float KeepRatio = 0.9f;

        /// <summary>
        /// 통로를 감싸는 양쪽 벽.
        /// 통로는 BallPath 가 낸 점 목록이고 공에서 목표 순이다.
        /// 구간이 없으면(점이 둘 미만) 빈 배열이다.
        /// </summary>
        public static Primitive[] Select(Vector2[] path)
        {
            float half = LevelData.BallRadius * WallSpread;
            var picked = new List<Primitive>();

            Wall(picked, Offset(path, half), path, half);
            Wall(picked, Offset(path, -half), path, half);

            return picked.ToArray();
        }

        /// <summary>
        /// 통로 중심선을 width 만큼 옆으로 민 벽들.
        /// 꺾임의 바깥쪽이면 반지름 width 의 호로 둘러 이어 붙이고,
        /// 안쪽이면 잇지 않고 끊는다 — 안쪽은 두 벽이 이미 겹치는 자리라
        /// 무엇을 놓아도 통로 쪽으로 파고든다. 겹친 채 두면 그만이다.
        /// 그래서 벽의 모든 점이 통로에서 정확히 width 만큼 떨어진다.
        /// </summary>
        static List<List<Vector2>> Offset(Vector2[] path, float width)
        {
            var chains = new List<List<Vector2>>();
            var chain = new List<Vector2> { path[0] + Normal(path[0], path[1]) * width };

            for (int i = 1; i + 1 < path.Length; i++)
            {
                Vector2 before = Normal(path[i - 1], path[i]);
                Vector2 after = Normal(path[i], path[i + 1]);

                // 꺾임의 방향과 벽이 놓인 쪽이 같으면 이 벽이 안쪽이다.
                float turn = Cross(path[i] - path[i - 1], path[i + 1] - path[i]);

                if (turn * width > 0f)
                {
                    chain.Add(path[i] + before * width);
                    chains.Add(chain);

                    chain = new List<Vector2> { path[i] + after * width };
                }
                else
                {
                    Arc(chain, path[i], before, after, width);
                }
            }

            int last = path.Length - 1;
            chain.Add(path[last] + Normal(path[last - 1], path[last]) * width);
            chains.Add(chain);

            return chains;
        }

        /// <summary>
        /// before 에서 after 까지 도는 호 위의 자리들.
        /// 점을 반지름에 딱 맞춰 찍으면 이웃한 두 점을 잇는 현이 안쪽으로
        /// 파고드니, 현의 가장 깊은 곳이 반지름에 오도록 외접시킨다.
        /// </summary>
        static void Arc(
            List<Vector2> chain, Vector2 at, Vector2 before, Vector2 after, float width)
        {
            float sweep = Vector2.SignedAngle(before, after) * Mathf.Deg2Rad;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / ArcStep));
            float bulge = 1f / Mathf.Cos(Mathf.Abs(sweep) / (2f * steps));

            for (int s = 0; s <= steps; s++)
            {
                // 양 끝은 이웃한 벽과 맞닿아야 해서 부풀리지 않는다.
                float scale = s == 0 || s == steps ? 1f : bulge;

                chain.Add(at + Turn(before, sweep * s / steps) * (width * scale));
            }
        }

        /// 구간의 왼쪽 법선.
        static Vector2 Normal(Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            return new Vector2(-along.y, along.x).normalized;
        }

        static Vector2 Turn(Vector2 vector, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>
        /// 벽 하나가 이어진 구간마다 프리미티브 하나씩.
        /// 통로에 너무 붙는 부분은 잘라 낸다 — 벽은 제 구간에서만
        /// half 만큼 떨어져 있고, 급하게 꺾이면 이웃 구간의 통로 안으로
        /// 들어간다. 잘라 낸 뒤에 아무것도 안 남는 벽이 있는데,
        /// 그 자리에는 애초에 벽이 들어갈 틈이 없다는 뜻이다.
        /// </summary>
        static void Wall(
            List<Primitive> picked, List<List<Vector2>> chains, Vector2[] path, float half)
        {
            for (int c = 0; c < chains.Count; c++)
            {
                List<Vector2> chain = chains[c];

                for (int i = 0; i + 1 < chain.Count; i++)
                    Keep(picked, chain[i], chain[i + 1], path, half);
            }
        }

        /// <summary>
        /// from~to 중 통로에서 충분히 떨어진 토막만 남겨 벽으로 만든다.
        /// 훑어서 찾는다 — 잘리는 자리를 식으로 풀면 통로 구간마다
        /// 이차식을 풀어야 하는데, 통로가 짧아 그 값을 못 한다.
        /// </summary>
        static void Keep(
            List<Primitive> picked, Vector2 from, Vector2 to, Vector2[] path, float half)
        {
            float length = Vector2.Distance(from, to);
            if (length <= 1e-4f) return;

            // 호의 현은 반지름보다 아주 조금 안쪽으로 처진다.
            // 그것까지 자르면 바깥쪽 벽에 구멍이 난다.
            float need = half * KeepRatio;

            int steps = Mathf.Max(2, Mathf.CeilToInt(length / (half * 0.25f)));
            bool open = false;
            Vector2 start = from;
            Vector2 last = from;

            for (int s = 0; s <= steps; s++)
            {
                Vector2 at = Vector2.Lerp(from, to, (float)s / steps);

                if (Clearance(at, path) >= need)
                {
                    if (!open)
                    {
                        open = true;
                        start = at;
                    }

                    last = at;
                    continue;
                }

                if (open) Add(picked, start, last);
                open = false;
            }

            if (open) Add(picked, start, last);
        }

        /// 이 자리에서 통로까지의 거리.
        static float Clearance(Vector2 at, Vector2[] path)
        {
            float least = float.PositiveInfinity;

            for (int i = 0; i + 1 < path.Length; i++)
                least = Mathf.Min(least, PointToSegment(at, path[i], path[i + 1]));

            return least;
        }

        static float PointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 along = b - a;
            float lengthSq = along.sqrMagnitude;

            if (lengthSq <= Mathf.Epsilon) return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / lengthSq);
            return Vector2.Distance(point, a + along * t);
        }

        static void Add(List<Primitive> picked, Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            if (along.sqrMagnitude <= 1e-8f) return;

            picked.Add(new Primitive(
                (from + to) * 0.5f,
                PrimitiveShape.Line,
                along.magnitude * 0.5f,
                ToolType.FixedLine,
                Mathf.Atan2(along.y, along.x)));
        }
    }
}
