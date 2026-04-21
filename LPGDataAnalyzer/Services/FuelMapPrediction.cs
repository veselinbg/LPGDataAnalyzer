using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class FuelMapPrediction
    {
        private const int KernelSize = 5;
        private const double KernelSigma = 1.2;
        private static void RoundFuelMap(double?[,] cellMap, int digits = 0)
        {
            int rows = cellMap.GetLength(0);
            int cols = cellMap.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    cellMap[i, j] = cellMap[i, j]?.Round(digits);
                }
            }
        }
        private static double? InterpolationFuelMap(int injIndex,
            int rpmIndex,
            DataItem[] injLogsB1,
            DataItem[] injLogsB2,
            double?[,]? cellMap, 
            bool showOnlyChanges)
        {
            var inj = Settings.InjectionRanges[injIndex];
            var rpm = Settings.RpmColumns[rpmIndex];
            // Only skip filling if showOnlyChanges is true AND trim is 1
           
            if (rpm.Label <= 3400 || inj.Label <= 5.8)
            {
                return showOnlyChanges ? null : cellMap[rpmIndex, injIndex];
            }
            else
            {
                double t = 1.0;
                int rpmSave = rpmIndex;

                // Find the maximum t from lower RPMs
                for (int lowerRpm = rpmIndex - 1; lowerRpm >= 0; lowerRpm--)
                {
                    var lowerLogsB1 = injLogsB1
                        .Where(d => d.RPM > Settings.RpmColumns[lowerRpm].Min &&
                                    d.RPM <= Settings.RpmColumns[lowerRpm].Max)
                        .Select(d => d.Trim_b1)
                        .ToArray();

                    var lowerLogsB2 = injLogsB2
                        .Where(d => d.RPM > Settings.RpmColumns[lowerRpm].Min &&
                                    d.RPM <= Settings.RpmColumns[lowerRpm].Max)
                        .Select(d => d.Trim_b2)
                        .ToArray();

                    var lowerLogs = lowerLogsB1.Merge(lowerLogsB2);

                    if (lowerLogs.Length != 0)
                    {
                        double tNew = 1 + lowerLogs.Median() / 100;

                        if (tNew > t)
                        {
                            t = tNew;
                            rpmSave = lowerRpm;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                // Compute final value once
                double? newValue = cellMap[rpmIndex, injIndex].SafeMultiply(t);
                if (inj.Label > 4.8)
                    newValue += rpmIndex - rpmSave;

                return newValue;
            }
        }

        public static (double?[,] result, List<DataItem> invalidItems) BuildTable(
            DataItem[] logs,
            double?[,] cellMap,
            double referencePressure,
            HistorySnapshot[]? historySnapshots = null,
            int minCount = 0,
            bool enableSmooth = true,
            bool enableInterpolation = false,
            bool showOnlyChanges = false,
            bool round = true,
            bool showOnlyMultiplayer = false,
            double minChangeValue = 0.5d,
            double benzDiffMax = 10d)
        {
            Dictionary<int, DataItem> invalidItems = [];
            Dictionary<int, (double Min, double Max)> MapRanges = [];
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            var result = new double?[rpmLength, injLength];
            // 🔥 Precompute injection grouping
            var logsByInjectionB1 = new DataItem[injLength][];
            var logsByInjectionB2 = new DataItem[injLength][];

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];

                logsByInjectionB1[injIndex] = logs
                    .Where(d => d.BENZ_b1 > inj.Min && d.BENZ_b1 <= inj.Max)
                    .ToArray();

                logsByInjectionB2[injIndex] = logs
                    .Where(d => d.BENZ_b2 > inj.Min && d.BENZ_b2 <= inj.Max)
                    .ToArray();

                if (logsByInjectionB1.Length != 0 || logsByInjectionB2.Length != 0)
                {
                    var logByInjection = logsByInjectionB1[injIndex].Concat(logsByInjectionB2[injIndex]).Where(x=>Math.Abs(x.BENZ_Diff) < benzDiffMax);
                    var mapMin = logByInjection.Min(x => x.MAP);
                    var mapMedian = logByInjection.Max(x => x.MAP);
                    var mapMax = mapMedian;//mapMedian*25/100;

                    MapRanges.Add(injIndex, (mapMin, mapMax.Round()));
                }
            }
            // 🔥 Main loop
            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var injLogsB1 = logsByInjectionB1[injIndex];
                var injLogsB2 = logsByInjectionB2[injIndex];
                var mapRange = MapRanges[injIndex];

                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var rpm = Settings.RpmColumns[rpmIndex];

                    int count = 0;
                    double[] buffer = new double[injLogsB1.Length + injLogsB2.Length];
                    
                    // ✅ B1
                    for (int i = 0; i < injLogsB1.Length; i++)
                    {
                        var d = injLogsB1[i];

                        if (d.MAP < mapRange.Min || d.MAP > mapRange.Max)
                        {
                            invalidItems.TryAdd(d.TEMPO, d);
                            continue;
                        }

                        if (d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        {
                            buffer[count++] = ApplyCorrections(
                                d.Trim_b1, d.Temp_GAS, d.Temp_RID, d.PRESS, referencePressure);
                        }
                    }

                    // ✅ B2
                    for (int i = 0; i < injLogsB2.Length; i++)
                    {
                        var d = injLogsB2[i];

                        if (d.MAP < mapRange.Min || d.MAP > mapRange.Max)
                        {
                            invalidItems.TryAdd(d.TEMPO, d);
                            continue;
                        }

                        if (d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        {
                            buffer[count++] = ApplyCorrections(
                                d.Trim_b2, d.Temp_GAS, d.Temp_RID, d.PRESS, referencePressure);
                        }
                    }

                    bool hasEnoughLogs = count > minCount;

                    double multiplayer = 0;
                    double trim = 1;

                    if (count > 0 && (hasEnoughLogs || !showOnlyMultiplayer))
                        multiplayer = buffer.AsSpan(0, count).Median();

                    if (hasEnoughLogs && !showOnlyMultiplayer)
                        trim = TrimCalulation(multiplayer, minChangeValue);

                    bool shouldUpdate = !showOnlyChanges || trim != 1;

                    if (hasEnoughLogs)
                    {
                        if (showOnlyMultiplayer)
                        {
                            result[rpmIndex, injIndex] = multiplayer;
                        }
                        else if (shouldUpdate)
                        {
                            double? currentValue = cellMap[rpmIndex, injIndex].SafeMultiply(trim);

                            if (currentValue.HasValue &&
                                historySnapshots?.Length > 0 &&
                                trim != 1)
                            {
                                var values = HistoryHelper.GetCellHistoryValues(
                                    historySnapshots, rpmIndex, injIndex);

                                values.Add(currentValue.Value);
                                currentValue = values.Median();
                            }

                            result[rpmIndex, injIndex] = currentValue;
                        }
                    }
                    else
                    {
                        if (enableInterpolation)
                        {
                            result[rpmIndex, injIndex] =
                                InterpolationFuelMap(injIndex, rpmIndex,
                                    injLogsB1, injLogsB2, cellMap, showOnlyChanges);
                        }
                        else if (shouldUpdate)
                        {
                            result[rpmIndex, injIndex] =
                                cellMap[rpmIndex, injIndex].SafeMultiply(trim);
                        }
                    }
                }
            }

            if (enableSmooth)
                FuelMapSmoother.Smooth(result, KernelSize, KernelSigma);

            RoundFuelMap(result, round ? 0 : 2);

            return (result, invalidItems.Values.ToList());
        }
        private static double GetTemperatureCoef(double value, (int Min, int Max, string Label)[] ranges, double[] coefs)
        {
            for (int i = 0; i < ranges.Length; i++)
                if (value >= ranges[i].Min && value < ranges[i].Max)
                    return coefs[i];
            return 0;
        }

        private static double ApplyCorrections(double trim, double tempGas, double tempRid, double press, double referencePressure)
        {
            double lpgCoef = GetTemperatureCoef(tempGas, Settings.GasTemperatureRanges, Settings.GasTemperatureCorrectionCoef);
            double ridCoef = GetTemperatureCoef(tempRid, Settings.ReductorTemperatureRanges, Settings.ReductorTemperatureCorrectionCoef);
            double pressCoef = (referencePressure - press) / referencePressure;

            return trim * (1 + pressCoef + lpgCoef / 100 + ridCoef / 100);
        }
        public static double TrimCalulation(double trim, double minChangeValue)
        {
            return 1 + (Math.Abs(trim) > minChangeValue || trim < 0 ? trim / 100 : 0); //Always apply negative trims to save fuel.

        }
    }
}
