namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        public readonly struct AxisSplit<T>(T low, T high, double wLow, double wHigh) where T : IComparable<T>
        {
            public readonly T Low = low;
            public readonly T High = high;
            public readonly double WLow = wLow;
            public readonly double WHigh = wHigh;
        }
    }
}
