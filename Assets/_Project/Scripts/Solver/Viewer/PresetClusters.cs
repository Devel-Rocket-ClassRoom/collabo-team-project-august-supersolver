using System.Collections.Generic;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 프리셋을 유형별로 묶는다.
    /// 목표 칸마다 답이 하나씩 있어도 실제로는 몇 가지 지렛대가
    /// 되풀이될 뿐이라, 묶어 봐야 몇 종류인지가 보인다.
    /// </summary>
    public static class PresetClusters
    {
        /// 자리가 흔들리면 볼 때마다 색이 바뀐다. 반복 횟수를 고정한다.
        const int Rounds = 40;

        /// <summary>
        /// 프리셋마다 소속 무리 번호. 다섯 축을 0~1 로 편 뒤 k-means 를 돈다 —
        /// 줄 수는 수십이고 축 자리는 1 미만이라 펴지 않으면 줄 수만 본다.
        /// 첫 중심을 고르게 집어 같은 입력이면 같은 결과가 나온다.
        /// </summary>
        public static int[] Assign(List<LeverPreset> presets, int k)
        {
            int count = presets.Count;
            var groups = new int[count];

            if (count == 0) return groups;

            k = Mathf.Clamp(k, 1, count);

            float[][] points = Normalize(presets);
            float[][] centers = new float[k][];

            // 고르게 집어 초기 중심으로 둔다.
            for (int c = 0; c < k; c++)
                centers[c] = (float[])points[c * count / k].Clone();

            for (int round = 0; round < Rounds; round++)
            {
                bool moved = false;

                for (int i = 0; i < count; i++)
                {
                    int nearest = Nearest(points[i], centers);
                    if (nearest == groups[i]) continue;

                    groups[i] = nearest;
                    moved = true;
                }

                if (!moved && round > 0) break;
                Recenter(points, groups, centers);
            }

            return groups;
        }

        const int Axes = 5;

        static float[][] Normalize(List<LeverPreset> presets)
        {
            var raw = new float[presets.Count][];

            for (int i = 0; i < presets.Count; i++)
            {
                LeverPreset p = presets[i];
                raw[i] = new[]
                {
                    p.Length, p.FulcrumAt, p.BallAt, p.WeightRows, p.Drop,
                };
            }

            for (int a = 0; a < Axes; a++)
            {
                float least = float.PositiveInfinity;
                float most = float.NegativeInfinity;

                for (int i = 0; i < raw.Length; i++)
                {
                    least = Mathf.Min(least, raw[i][a]);
                    most = Mathf.Max(most, raw[i][a]);
                }

                // 값이 하나뿐인 축은 무리를 가르지 못한다. 0 으로 눕힌다.
                float span = most - least;
                for (int i = 0; i < raw.Length; i++)
                    raw[i][a] = span <= Mathf.Epsilon ? 0f : (raw[i][a] - least) / span;
            }

            return raw;
        }

        static int Nearest(float[] point, float[][] centers)
        {
            int at = 0;
            float least = float.PositiveInfinity;

            for (int c = 0; c < centers.Length; c++)
            {
                float sum = 0f;
                for (int a = 0; a < Axes; a++)
                {
                    float gap = point[a] - centers[c][a];
                    sum += gap * gap;
                }

                if (sum >= least) continue;

                least = sum;
                at = c;
            }

            return at;
        }

        /// <summary>
        /// 무리마다 평균으로 중심을 옮긴다.
        /// 빈 무리는 그대로 둔다 — 옮길 근거가 없다.
        /// </summary>
        static void Recenter(float[][] points, int[] groups, float[][] centers)
        {
            var sums = new float[centers.Length][];
            var counts = new int[centers.Length];

            for (int c = 0; c < centers.Length; c++) sums[c] = new float[Axes];

            for (int i = 0; i < points.Length; i++)
            {
                int c = groups[i];
                counts[c]++;

                for (int a = 0; a < Axes; a++) sums[c][a] += points[i][a];
            }

            for (int c = 0; c < centers.Length; c++)
            {
                if (counts[c] == 0) continue;

                for (int a = 0; a < Axes; a++) centers[c][a] = sums[c][a] / counts[c];
            }
        }
    }
}
