using System;

namespace PPS.Solver
{
    /// <summary>
    /// 셀에서 셀로의 이동 하나. 휴리스틱 맵의 간선이다.
    /// 배치 하나가 만들어낸 이동이라 비용은 항상 1이고,
    /// 값이 갈릴 여지가 없어 담지 않는다.
    /// </summary>
    public readonly struct CellEdge : IEquatable<CellEdge>
    {
        const uint FnvOffsetBasis = 2166136261u;
        const uint FnvPrime = 16777619u;

        public readonly BallCell From;
        public readonly BallCell To;

        public CellEdge(BallCell from, BallCell to)
        {
            From = from;
            To = to;
        }

        public bool Equals(CellEdge other) => From == other.From && To == other.To;

        public override bool Equals(object obj) => obj is CellEdge other && Equals(other);

        /// <summary>
        /// BallCell 과 같은 이유로 FNV 다 —
        /// HashCode.Combine 은 실행마다 순회 순서를 뒤집는다.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                uint h = FnvOffsetBasis;
                h = (h ^ (uint)From.GetHashCode()) * FnvPrime;
                h = (h ^ (uint)To.GetHashCode()) * FnvPrime;
                return (int)h;
            }
        }

        public override string ToString() => $"{From} -> {To}";
    }
}
