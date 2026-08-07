using System;
using System.Collections.Generic;
using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 궤적 하나를 간선 여럿으로 바꿔 모은다.
    /// 시작 셀에서 그 배치로 닿을 수 있었던 셀들이므로
    /// 간선은 전부 시작 셀에서 뻗는다.
    /// 비용은 프리미티브를 놓았으면 1, 아니면 0 이다.
    /// 여러 시행을 이어 넣으면 맵이 쌓인다.
    /// </summary>
    public sealed class CellEdgeCollector
    {
        const int Placed = 1;
        const int Free = 0;

        readonly BallQuantizer _quantizer;
        readonly Dictionary<CellEdge, int> _edges = new Dictionary<CellEdge, int>();

        public CellEdgeCollector(BallQuantizer quantizer)
        {
            _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
        }

        /// 간선과 그 비용. 0-1 BFS 가 그대로 읽는다.
        public IReadOnlyDictionary<CellEdge, int> Edges => _edges;

        /// <summary>프리미티브 하나를 놓고 굴린 궤적. 비용 1.</summary>
        public void CollectPlaced(TrajectoryBuffer trajectory) => Collect(trajectory, Placed);

        /// <summary>
        /// 아무것도 놓지 않고 굴린 궤적. 비용 0.
        /// 중력만으로 되는 이동은 잉크를 안 쓰므로
        /// h 를 올리면 안 된다.
        /// </summary>
        public void CollectFree(TrajectoryBuffer trajectory) => Collect(trajectory, Free);

        /// <summary>
        /// 궤적이 지나간 셀 전부가 대상이다.
        /// 같은 간선이 여러 번 나와도 하나로 합쳐진다 —
        /// 한 셀에 여러 스냅샷이 접히는 일이 흔하다.
        /// 시작 셀로 되돌아온 구간은 자기 자신으로 가는
        /// 간선이라 거리 0인 자리에 1을 넣게 되므로 뺀다.
        /// </summary>
        void Collect(TrajectoryBuffer trajectory, int cost)
        {
            if (trajectory == null) throw new ArgumentNullException(nameof(trajectory));
            if (trajectory.Count == 0) return;

            BallCell start = Quantize(trajectory[0]);
            for (int i = 1; i < trajectory.Count; i++)
            {
                BallCell cell = Quantize(trajectory[i]);
                if (cell == start) continue;

                Add(new CellEdge(start, cell), cost);
            }
        }

        /// <summary>
        /// 같은 간선이 0 으로도 1 로도 나온다 — 중력만으로 닿는 셀에
        /// 프리미티브를 놓고도 닿을 수 있기 때문이다.
        /// 실제 비용은 작은 쪽이다. 큰 쪽을 남기면
        /// 잉크가 안 드는 이동을 1로 세어 h 가 부푼다.
        /// </summary>
        void Add(CellEdge edge, int cost)
        {
            if (_edges.TryGetValue(edge, out int known) && known <= cost) return;
            _edges[edge] = cost;
        }

        BallCell Quantize(in BallSample sample)
            => _quantizer.Quantize(sample.Position, sample.Velocity);
    }
}
