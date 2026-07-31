using LPGDataAnalyzer.Models;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer
{
    public static class DataItemExtentions
    {
        public static string[] GetExistGasTemperatureRanges(this DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var usedRanges = GasTemperatureRanges
                .Where(range => data.Any(item => item.Temp_GAS >= range.Min && item.Temp_GAS <= range.Max)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }
        public static string[] GetExistReductorTempGroups(this DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var usedRanges = ReductorTemperatureRanges
                .Where(range => data.Any(item => item.Temp_RID >= range.Min && item.Temp_RID <= range.Max)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }
    }
}
