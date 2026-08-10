using System;
using System.Collections.Generic;
using PPS.Core;

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
        /// </summary>
        public List<Solution> Build(StageData stage)
        {
            // 무엇을 어디에 놓을지가 아직 안 정해졌다.
            // 빈 목록을 내면 "시도할 것이 없다" 로 읽혀
            // 못 푸는 레벨이라는 판정과 구분되지 않는다.
            throw new NotImplementedException();
        }
    }
}
