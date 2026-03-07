namespace LPGDataAnalyzer.Models.Common
{
    // ===================== CUSTOM KEY =====================
    public readonly struct CellKey(int rpm, int inj) : IEquatable<CellKey>
    {
        public readonly int Rpm = rpm;
        public readonly int Inj = inj;

        public bool Equals(CellKey other) => Rpm == other.Rpm && Inj.Equals(other.Inj);

        public override bool Equals(object? obj) => obj is CellKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Rpm, Inj);

        public static bool operator ==(CellKey left, CellKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellKey left, CellKey right)
        {
            return !(left == right);
        }
    }
}