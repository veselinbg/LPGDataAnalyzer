using LPGDataAnalyzer.Models;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer
{
    /// <summary>
    /// Provides extension methods specifically for DataItem arrays, including filtering
    /// and range detection for temperature data analysis.
    /// </summary>
    public static class DataItemExtensions
    {
        public static string[] GetExistGasTemperatureRanges(this DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var used = data.Select(item => item.Temp_GAS.GetTempBandIndex(GasTemperatureRanges)).ToHashSet();
            var usedRanges = GasTemperatureRanges.Where((_, i) => used.Contains(i)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }
        public static string[] GetExistReductorTempGroups(this DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var used = data.Select(item => item.Temp_RID.GetTempBandIndex(ReductorTemperatureRanges)).ToHashSet();
            var usedRanges = ReductorTemperatureRanges.Where((_, i) => used.Contains(i)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }
    }
}
