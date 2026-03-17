using LPGDataAnalyzer.Models;
using System.Globalization;
using System.Text;

namespace LPGDataAnalyzer
{
    internal static class Extentions
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
        public static double Median(this IEnumerable<double> numbers)
        {
            if (numbers == null)
                throw new ArgumentNullException(nameof(numbers));

            var sorted = numbers.ToArray();
            if (sorted.Length == 0)
                throw new ArgumentException("Median of empty sequence is not defined.", nameof(numbers));

            Array.Sort(sorted);

            int mid = sorted.Length / 2;
            return (sorted.Length % 2 != 0)
                ? sorted[mid]
                : (sorted[mid - 1] + sorted[mid]) / 2.0;
        }
        public static double StdDev(this IEnumerable<double> list)
        {
            if (list.Count() == 0)
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

                for (int inj = 0; inj < Settings.InjectionRanges.Length; inj++)
                {
                    for (int rpm = 0; rpm < Settings.RpmColumns.Length; rpm++)

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
        public static double AggregateValues(this IEnumerable<double> values, Settings.Aggregation aggregation)
        {
            return aggregation switch
            {
                Settings.Aggregation.Median => values.Median(),
                Settings.Aggregation.Min => values.Min(),
                Settings.Aggregation.Max => values.Max(),
                Settings.Aggregation.Average => values.Average(),
                _ => 0
            };
        }
        public static double Avg<T>(this IEnumerable<T> arr, Func<T, double> selector)
    => arr.Select(selector).DefaultIfEmpty(0).Average();

        public static double Min<T>(this IEnumerable<T> arr, Func<T, double> selector)
            => arr.Select(selector).DefaultIfEmpty(0).Min();

        public static double Max<T>(this IEnumerable<T> arr, Func<T, double> selector)
            => arr.Select(selector).DefaultIfEmpty(0).Max();

        public static double RelDiff(this double a, double b)
            => (a == 0 && b == 0) ? 0d : ((Math.Abs(a - b) / ((a + b) / 2.0)) * 100).Round();
        public static double[] Merge(this double[] a, double[] b)
        {
            var result = new double[a.Length + b.Length];
            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }
    }
}
