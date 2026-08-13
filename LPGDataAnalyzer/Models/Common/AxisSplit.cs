using System.Numerics;
using System.Runtime.CompilerServices;

namespace LPGDataAnalyzer.Models.Common
{
    public readonly struct AxisSplit<T>(T low, T high, double wLow, double wHigh)
    {
        public readonly T Low = low;
        public readonly T High = high;
        public readonly double WLow = wLow;
        public readonly double WHigh = wHigh;
    }
}
