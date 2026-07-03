using LPGDataAnalyzer.Models;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer
{
    public static class Extentions
    {
        public static double Round(this double value, int digits = 2)
        {
            return Math.Round(value, digits); 
        }
        public static int ToInt(this ReadOnlySpan<char> value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        public static double ToDouble(this ReadOnlySpan<char> value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result
                : 0.0;
        }
        public static double Median(this List<double> values)
        {
            return MedianCore(CollectionsMarshal.AsSpan(values));
        }
        public static double Median(this double[] values)
        {
            return MedianCore(values);
        }
        public static double Median(this IEnumerable<double> numbers)
        {
            ArgumentNullException.ThrowIfNull(numbers);

            return MedianCore(numbers.ToArray());
        }
        public static double MedianCore(this Span<double> span)
        {
            if (span.IsEmpty)
                return 0;// throw new ArgumentException("Median of empty span is not defined.", nameof(span));

            span.Sort();  

            int mid = span.Length / 2;
            return (span.Length % 2 != 0)
                ? span[mid]
                : (span[mid - 1] + span[mid]) / 2.0;
        }
        public static double Average(this ReadOnlySpan<double> span)
        {
            if (span.IsEmpty)
                throw new ArgumentException("Average of empty span is not defined.", nameof(span));

            double sum = 0;
            double compensation = 0;

            foreach (double value in span)
            {
                double y = value - compensation;
                double t = sum + y;
                compensation = (t - sum) - y;
                sum = t;
            }

            return sum / span.Length;
        }
        public static double StdDev(this IEnumerable<double> list)
        {
            if (list is null || list.Count() == 0)
                return 0;

            double avg = list.Average();
            double sumSq = list.Sum(v => Math.Pow(v - avg, 2));

            return Math.Sqrt(sumSq / list.Count());
        }
        public static string ToText(this double?[,] table)
        {
            if(table != null)
            {
                var text = new StringBuilder();

                for (int inj = 0; inj < table.GetLength(1); inj++)
                {
                    for (int rpm = 0; rpm < table.GetLength(0); rpm++)
                    {
                        text.Append(table[rpm, inj]);
                    }
                    text.AppendLine();
                }
                return text.ToString();
            }
            return string.Empty;
        }
        // Helper to safely multiply nullable double
        public static double? SafeMultiply(this double? value, double factor)
        {
            return value.HasValue ? value.Value * factor : null;
        }
        public static Func<DataItem, double?> GetFieldValue(this FieldsToShow field, Banks bank = Banks.ALL) 
        {
            return bank switch
            {
                Banks.ALL => field switch
                {
                    FieldsToShow.Trim => item => item.Trim,
                    FieldsToShow.FastTrim => item => item.Fast,
                    FieldsToShow.Ratio => item=> item.RatioDifference,
                    FieldsToShow.AFR => item => item.AFR,
                    FieldsToShow.GasTime => item => item.GAS,
                    FieldsToShow.Press => item => item.PRESS,
                    _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
                },
                Banks.B1 => field switch
                {
                    FieldsToShow.Trim => item => item.Trim_b1,
                    FieldsToShow.FastTrim => item => item.FAST_b1,
                    FieldsToShow.Ratio => item => item.Ratio_b1,
                    FieldsToShow.AFR => item => item.AFR_b1,
                    FieldsToShow.GasTime => item => item.GAS_b1,
                    FieldsToShow.Press => item => item.PRESS,
                    _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
                },
                Banks.B2 => field switch
                {
                    FieldsToShow.Trim => item => item.Trim_b2,
                    FieldsToShow.FastTrim => item => item.FAST_b2,
                    FieldsToShow.Ratio => item => item.Ratio_b2,
                    FieldsToShow.AFR => item => item.AFR_b2,
                    FieldsToShow.GasTime => item => item.GAS_b2,
                    FieldsToShow.Press => item => item.PRESS,
                    _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
                },
            };
        }

        public static double Average<T>(this IEnumerable<T> arr,Func<T, double> selector)
        {
            double sum = 0;
            int count = 0;

            foreach (var item in arr)
            {
                double v = selector(item);

                if (double.IsNaN(v))
                    continue;

                sum += v;
                count++;
            }

            return count == 0 ? double.NaN : sum / count;
        }

        public static double Min<T>(this IEnumerable<T> arr, Func<T, double> selector)
        {
            double min = double.PositiveInfinity;
            bool found = false;

            foreach (var item in arr)
            {
                double v = selector(item);

                if (double.IsNaN(v))
                    continue;

                if (!found || v < min)
                {
                    min = v;
                    found = true;
                }
            }

            return found ? min : double.NaN;
        }
        public static double Max<T>(this IEnumerable<T> arr, Func<T, double> selector)
        {
            double max = double.NegativeInfinity;
            bool found = false;

            foreach (var item in arr)
            {
                double v = selector(item);

                if (double.IsNaN(v))
                    continue;

                if (!found || v > max)
                {
                    max = v;
                    found = true;
                }
            }

            return found ? max : double.NaN;
        }
        public static double RelDiff(this double a, double b)
            => (a == 0 && b == 0) ? 0d : (((a - b) / ((a + b) / 2.0)) * 100).Round();
        public static T[] Merge<T>(this T[] a, T[] b)
        {
            T[] result = new T[a.Length + b.Length];

            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);

            return result;
        }
        public static bool TryGetDouble(this object v, out double result)
        {
            switch (v)
            {
                case int i: result = i; return true;
                case long l: result = l; return true;
                case float f: result = f; return true;
                case double d: result = d; return true;
                case decimal m: result = (double)m; return true;
                case short s: result = s; return true;
                case byte b: result = b; return true;
                default:
                    result = 0;
                    return false;
            }
        }
        public static int GetRpmIndex(this DataItem d)
        {
            return FindIndex(d.RPM, RpmColumns);
        }
        public static int GetInjectionIndex(this DataItem d)
        {
            return FindIndex(d.BENZ, InjectionRanges);
        }
        public static int GetInjectionIndex(this DataItem d, Func<DataItem, double> injectorSelector)
        {
            return FindIndex(injectorSelector(d), InjectionRanges);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindIndex(int value, ReadOnlySpan<(int Min, int Max, int Label)> ranges)
        {
            int lo = 0;
            int hi = ranges.Length - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);

                var range = ranges[mid];

                if (value <= range.Min)
                    hi = mid - 1;
                else if (value > range.Max)
                    lo = mid + 1;
                else
                    return mid;
            }

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindIndex(double value, ReadOnlySpan<(double Min, double Max, double Label)> ranges)
        {
            int lo = 0;
            int hi = ranges.Length - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);

                var range = ranges[mid];

                if (value <= range.Min)
                    hi = mid - 1;
                else if (value > range.Max)
                    lo = mid + 1;
                else
                    return mid;
            }

            throw new ArgumentOutOfRangeException(nameof(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AverageFast(this List<double> values)
        {
            var span = CollectionsMarshal.AsSpan(values);

            double sum = 0;
            int count = 0;

            foreach (var v in span)
            {
                if (double.IsNaN(v))
                    continue;

                sum += v;
                count++;
            }

            return count == 0 ? double.NaN : sum / count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MinFast(this List<double> values)
        {
            var span = CollectionsMarshal.AsSpan(values);

            double min = double.MaxValue;
            bool found = false;

            foreach (var v in span)
            {
                if (double.IsNaN(v))
                    continue;

                if (!found || v < min)
                {
                    min = v;
                    found = true;
                }
            }

            return found ? min : double.NaN;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MaxFast(this List<double> values)
        {
            var span = CollectionsMarshal.AsSpan(values);

            double max = double.MinValue;
            bool found = false;

            foreach (var v in span)
            {
                if (double.IsNaN(v))
                    continue;

                if (!found || v > max)
                {
                    max = v;
                    found = true;
                }
            }

            return found ? max : double.NaN;
        }
    }
}
