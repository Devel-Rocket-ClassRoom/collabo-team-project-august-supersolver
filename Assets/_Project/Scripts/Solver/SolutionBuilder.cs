using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 스테이지를 받아 솔버가 놓아 볼 그림들을 낸다.
    /// 레벨 데이터만 보므로 시뮬을 돌리기 전에 답이 나온다.
    /// </summary>
    public sealed class SolutionBuilder
    {
        /// <summary>
        /// 이 스테이지에 시도해 볼 배치 전부. 하나당 한 판을 굴린다.
        /// 통로 하나가 배치 하나다 — 통로가 서로 다른 갈래이므로
        /// 배치도 그만큼 다르다.
        /// </summary>
        public List<Solution> Build(StageData stage)
        {
            List<Vector2[]> paths = BallPath.Find(stage.Level);
            var solutions = new List<Solution>(paths.Count);

            for (int i = 0; i < paths.Count; i++)
                solutions.Add(ToSolution(PrimitiveCandidates.Select(paths[i])));

            return solutions;
        }

        static Solution ToSolution(Primitive[] primitives)
        {
            var solution = new Solution();

            for (int i = 0; i < primitives.Length; i++)
                solution.Strokes.Add(primitives[i].ToStroke());

            return solution;
        }
    }
}
