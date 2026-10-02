using System;

namespace PPS.Core
{
    /// <summary>
    /// 스테이지 한 자리. 테마와 스테이지를 한 값으로 묶어
    /// 두 축이 따로 움직이다 어긋나는 일을 막는다.
    /// </summary>
    [Serializable]
    public struct StageEntry : IComparable<StageEntry>, IEquatable<StageEntry>
    {
        /// 테마 번호. 0 부터 센다.
        public int Theme;

        /// 테마 안에서의 스테이지 번호. 0 부터 센다.
        public int Stage;

        public StageEntry(int theme, int stage)
        {
            Theme = theme;
            Stage = stage;
        }

        /// <summary>
        /// 사전식 비교 — 테마를 먼저 보고 같을 때만 스테이지를
        /// 본다. 테마마다 스테이지 개수가 달라도 순서가 성립한다.
        /// </summary>
        public int CompareTo(StageEntry other)
        {
            if (Theme != other.Theme) return Theme.CompareTo(other.Theme);
            return Stage.CompareTo(other.Stage);
        }

        public bool Equals(StageEntry other) => Theme == other.Theme && Stage == other.Stage;

        public override bool Equals(object obj) => obj is StageEntry other && Equals(other);

        public override int GetHashCode() => (Theme * 397) ^ Stage;

        public override string ToString() => $"({Theme}, {Stage})";

        public static bool operator ==(StageEntry a, StageEntry b) => a.Equals(b);

        public static bool operator !=(StageEntry a, StageEntry b) => !a.Equals(b);

        public static bool operator <(StageEntry a, StageEntry b) => a.CompareTo(b) < 0;

        public static bool operator <=(StageEntry a, StageEntry b) => a.CompareTo(b) <= 0;

        public static bool operator >(StageEntry a, StageEntry b) => a.CompareTo(b) > 0;

        public static bool operator >=(StageEntry a, StageEntry b) => a.CompareTo(b) >= 0;
    }
}
