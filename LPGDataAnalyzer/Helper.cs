using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer
{
    public class Helper
    {
        public static double PercentageChange(double baseValue, double newValue)
        {
            return ((newValue - baseValue) / baseValue) * 100;
        }
        public static int FindIndex<T, TValue>(
              TValue value,
              T[] ranges,
              Func<T, (TValue Min, TValue Max)> selector)
              where TValue : IComparable<TValue>
        {
            for (int i = 0; i < ranges.Length; i++)
            {
                var (min, max) = selector(ranges[i]);

                if (value.CompareTo(min) > 0 && value.CompareTo(max) <= 0)
                    return i;
            }

            throw new ArgumentOutOfRangeException($"Unable to find index of {value}");
        }
        public static List<string> GetCheckedValues(CheckedListBox list)
        {
            return list.CheckedItems.Cast<object>()
                .Select(x => x.ToString())
                .ToList();
        }
    }
}
