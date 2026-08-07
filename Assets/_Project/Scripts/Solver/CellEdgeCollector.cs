using System;
using System.Collections.Generic;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 궤적 하나를 간선 여럿으로 바꿔 모은다.
    /// 시작 셀에서 그 배치로 닿을 수 있었던 셀들이므로
    /// 간선은 전부 시작 셀에서 뻗는다.
    /// 여러 시행을 이어 넣으면 맵이 쌓인다.
    /// </summary>
    public sealed class CellEdgeCollector
    {
        readonly BallQuantizer _quantizer;
        readonly HashSet<CellEdge> _edges = new HashSet<CellEdge>();

        public CellEdgeCollector(BallQuantizer quantizer)
        {
            _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
        }

        public IReadOnlyCollection<CellEdge> Edges => _edges;

        /// <summary>
        /// 궤적이 지나간 셀 전부가 대상이다.
        /// 같은 간선이 여러 번 나와도 하나로 합쳐진다 —
        /// 한 셀에 여러 스냅샷이 접히는 일이 흔하다.
        /// 시작 셀로 되돌아온 구간은 자기 자신으로 가는
        /// 간선이라 거리 0인 자리에 1을 넣게 되므로 뺀다.
        /// </summary>
        public void Collect(TrajectoryBuffer trajectory)
        {
            if (trajectory == null) throw new ArgumentNullException(nameof(trajectory));
            if (trajectory.Count == 0) return;

            BallCell start = Quantize(trajectory[0]);
            for (int i = 1; i < trajectory.Count; i++)
            {
                BallCell cell = Quantize(trajectory[i]);
                if (cell == start) continue;

                _edges.Add(new CellEdge(start, cell));
            }
        }

        BallCell Quantize(in BallSample sample)
            => _quantizer.Quantize(sample.Position, sample.Velocity);
    }
}
