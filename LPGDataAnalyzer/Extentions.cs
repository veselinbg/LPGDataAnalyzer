using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
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
        public static int ToInt(this string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        public static double ToDouble(this string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result
                : 0.0;
        }
        public static double StdDev(this IEnumerable<double> list)
        {
            if (list.Count() == 0)
                return 0;

            double avg = list.Average();
            double sumSq = list.Sum(v => Math.Pow(v - avg, 2));

            return Math.Sqrt(sumSq / list.Count());
        }
        public static string ToText(this IEnumerable<TableRow> list)
        {
            return list == null
                ? string.Empty
                : string.Concat(
                    list.OrderBy(r => r.Key)
                        .Select(row =>
                            string.Concat(
                                row.Columns
                                   .OrderBy(c => c.Key)
                                   .Select(c => c.Value)
                            ) + Environment.NewLine
                        )
                  );
        }
    }
}
