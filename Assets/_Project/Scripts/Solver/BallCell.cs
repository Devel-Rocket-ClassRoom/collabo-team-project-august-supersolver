using System;

namespace PPS.Solver
{
    /// <summary>
    /// 양자화된 공의 위상 상태. 휴리스틱 맵의 키다.
    /// 실수 그대로는 두 시행이 같은 키를 낼 수 없어
    /// 격자 인덱스로 바꾼 것이다.
    /// </summary>
    public readonly struct BallCell : IEquatable<BallCell>
    {
        const uint FnvOffsetBasis = 2166136261u;
        const uint FnvPrime = 16777619u;

        public readonly int X;
        public readonly int Y;
        public readonly int VX;
        public readonly int VY;

        public BallCell(int x, int y, int vx, int vy)
        {
            X = x;
            Y = y;
            VX = vx;
            VY = vy;
        }

        public bool Equals(BallCell other) =>
            X == other.X && Y == other.Y && VX == other.VX && VY == other.VY;

        public override bool Equals(object obj) => obj is BallCell other && Equals(other);

        /// <summary>
        /// HashCode.Combine 은 프로세스마다 시드가 달라
        /// Dictionary 순회 순서가 실행마다 뒤집힌다.
        /// 솔버 재현성이 깨지므로 FNV 로 직접 섞는다.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                uint h = FnvOffsetBasis;
                Mix(ref h, X);
                Mix(ref h, Y);
                Mix(ref h, VX);
                Mix(ref h, VY);
                return (int)h;
            }
        }

        public static bool operator ==(BallCell a, BallCell b) => a.Equals(b);

        public static bool operator !=(BallCell a, BallCell b) => !a.Equals(b);

        public override string ToString() => $"p({X},{Y}) v({VX},{VY})";

        static void Mix(ref uint h, int value)
        {
            unchecked
            {
                for (int shift = 0; shift < 32; shift += 8)
                {
                    h ^= (byte)(value >> shift);
                    h *= FnvPrime;
                }
            }
        }
    }
}
