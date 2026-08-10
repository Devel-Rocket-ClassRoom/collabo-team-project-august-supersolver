using System;
using System.Collections.Generic;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 앵커를 뽑는 규칙의 종류. 딕셔너리 키다.
    /// 장치 쪽은 DeviceType 과 1:1 이지만 값을 맞춰 캐스팅하지 않는다 —
    /// DeviceType 은 직렬화 때문에 순서가 묶여 있고
    /// 이쪽은 지형까지 함께 담는다.
    /// </summary>
    public enum SolverAnchorSource
    {
        StaticSegment,
        Bomb,
        FragBomb,
    }

    /// <summary>
    /// 스테이지를 받아 솔버가 놓아 볼 그림을 낸다.
    /// 레벨 데이터만 보므로 시뮬을 돌리기 전에 답이 나온다.
    /// </summary>
    public sealed class SolverPrimitiveAnchorSelectService
    {
        readonly Dictionary<SolverAnchorSource, SolverPrimitiveAnchorSelector> _selectors;

        public SolverPrimitiveAnchorSelectService()
        {
            // 장치는 아직 종류별 규칙이 없어 하나를 나눠 쓴다.
            // 종류가 갈리면 이 자리만 갈아 끼운다.
            var device = new DeviceAnchorSelector();

            _selectors = new Dictionary<SolverAnchorSource, SolverPrimitiveAnchorSelector>
            {
                [SolverAnchorSource.StaticSegment] = new StaticSegmentAnchorSelector(),
                [SolverAnchorSource.Bomb] = device,
                [SolverAnchorSource.FragBomb] = device,
            };
        }

        /// <summary>
        /// 스테이지 하나가 내는 그림 전부를 획으로 옮긴다.
        /// 지형 먼저, 그다음 장치다 — 레벨 데이터의 등록 순서를 그대로
        /// 따라야 같은 스테이지가 언제나 같은 그림을 낸다.
        /// </summary>
        public Solution Select(StageData stage)
        {
            LevelData level = stage.Level;
            var anchors = new List<SolverAnchor>();

            for (int i = 0; i < level.Terrain.Count; i++)
                Collect(anchors, Select(level.Terrain[i], level));

            for (int i = 0; i < level.Devices.Count; i++)
                Collect(anchors, Select(level.Devices[i]));

            var solution = new Solution();

            foreach (SolverAnchor anchor in anchors)
                foreach (Primitive primitive in anchor.Primitives)
                    solution.Strokes.Add(primitive.ToStroke(anchor.Position));

            return solution;
        }

        SolverAnchor[] Select(StaticSegment segment, LevelData level)
            => ((StaticSegmentAnchorSelector)_selectors[SolverAnchorSource.StaticSegment])
                .Select(segment, level);

        SolverAnchor[] Select(DeviceData device)
            => ((DeviceAnchorSelector)_selectors[Source(device.Type)]).Select(device);

        /// <summary>
        /// 아직 없는 앵커만 담는다.
        /// 이어진 선분은 끝점을 공유해서 그냥 담으면 같은 자리가 두 번 나오고,
        /// 중복 앵커는 그대로 중복 시뮬이 된다.
        /// 앵커 수가 지형 조각 수 남짓이라 훑어서 찾는다.
        /// </summary>
        static void Collect(List<SolverAnchor> anchors, SolverAnchor[] found)
        {
            foreach (SolverAnchor anchor in found)
            {
                bool seen = false;

                for (int i = 0; i < anchors.Count && !seen; i++)
                    seen = anchors[i].Matches(anchor);

                if (!seen) anchors.Add(anchor);
            }
        }

        /// 장치 종류를 셀렉터 키로 옮긴다.
        static SolverAnchorSource Source(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.Bomb: return SolverAnchorSource.Bomb;
                case DeviceType.FragBomb: return SolverAnchorSource.FragBomb;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type), type, "앵커 규칙이 없는 장치다.");
            }
        }
    }
}
