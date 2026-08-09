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

        /// 진행 방향 칸. 0 이 +x 이고 반시계로 45° 씩 돈다.
        public readonly int Dir;

        /// 속력 구간 칸. 0 이면 정지이고 그때 Dir 은 뜻이 없다.
        public readonly int Mag;

        /// <summary>
        /// 멈춘 공은 방향이 의미가 없어 Dir 을 0 으로 접는다.
        /// 안 접으면 물리적으로 같은 정지 상태가 8칸으로 갈린다.
        /// 표준형을 여기서 강제해야 부르는 쪽마다 안 챙긴다.
        /// </summary>
        public BallCell(int x, int y, int dir, int mag)
        {
            X = x;
            Y = y;
            Mag = mag;
            Dir = mag == 0 ? 0 : dir;
        }

        public bool Equals(BallCell other) =>
            X == other.X && Y == other.Y && Dir == other.Dir && Mag == other.Mag;

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
                Mix(ref h, Dir);
                Mix(ref h, Mag);
                return (int)h;
            }
        }

        public static bool operator ==(BallCell a, BallCell b) => a.Equals(b);

        public static bool operator !=(BallCell a, BallCell b) => !a.Equals(b);

        public override string ToString() => $"p({X},{Y}) dir {Dir} mag {Mag}";

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
