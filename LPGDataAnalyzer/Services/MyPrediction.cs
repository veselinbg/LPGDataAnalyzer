using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class MyPrediction
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
        public static double?[,] BuildTable(
                                        DataItem[] logs,
                                        double?[,] cellMap,
                                        HistorySnapshot[]? historySnapshots = null,
                                        int minCount = 0,
                                        bool enableSmooth = true,
                                        bool enableInterpolation = false,
                                        bool showOnlyChanges = false,
                                        bool round = true,
                                        bool preFilter = true,
                                        bool showOnlyMultiplayer = false,
                                        double minChangeValue = 0.5d)
        {
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            var result = new double?[rpmLength, injLength];

            // Precompute logs grouped by injection ranges
            var logsByInjectionB1 = new DataItem[injLength][];
            var logsByInjectionB2 = new DataItem[injLength][];

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];
                logsByInjectionB1[injIndex] = logs
                    .Where(d => (d.BENZ_b1 > inj.Min && d.BENZ_b1 <= inj.Max) && (!preFilter || (d.FAST_b1 >-10 && d.FAST_b1 < 10))).ToArray();

                logsByInjectionB2[injIndex] = logs
                    .Where(d => (d.BENZ_b2 > inj.Min && d.BENZ_b2 <= inj.Max) && (!preFilter || (d.FAST_b2 >-10 && d.FAST_b2 < 10))).ToArray();
            }

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var injLogsB1 = logsByInjectionB1[injIndex];
                var injLogsB2 = logsByInjectionB2[injIndex];

                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var rpm = Settings.RpmColumns[rpmIndex];

                    var rpmLogsB1 = injLogsB1
                        .Where(d => d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        .Select(d => d.Trim_b1)
                        .ToArray();
                    var rpmLogsB2 = injLogsB2
                        .Where(d => d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        .Select(d => d.Trim_b2)
                        .ToArray();
                    var rpmLogs = rpmLogsB1.Merge(rpmLogsB2);
                    bool hasEnoughLogs = rpmLogs.Length > minCount;
                    double median = 0;
                    double trim = 1;

                    // Only compute median if needed
                    if (rpmLogs.Length > 0 && (hasEnoughLogs || !showOnlyMultiplayer))
                    {
                        median = rpmLogs.Median();
                    }

                    // Determine trim value if not showing only multiplayer
                    if (hasEnoughLogs && !showOnlyMultiplayer)
                    {
                        trim = 1 + (Math.Abs(median) > minChangeValue ? (median / 100) : 0);
                    }

                    // Decide whether to update the result
                    bool shouldUpdate = !showOnlyChanges || trim != 1;

                    if (hasEnoughLogs)
                    {
                        if (showOnlyMultiplayer)
                        {
                            result[rpmIndex, injIndex] = median;
                        }
                        else if (shouldUpdate)
                        {
                            double? currentValue = cellMap[rpmIndex, injIndex].SafeMultiply(trim);

                            if (currentValue.HasValue)
                            {
                                if (historySnapshots != null && historySnapshots.Length > 0 && trim != 1)
                                {
                                    var values = HistoryHelper.GetCellHistoryValues(historySnapshots, rpmIndex, injIndex);

                                    values.Add(currentValue.Value);

                                    currentValue = values.Median();
                                }

                                result[rpmIndex, injIndex] = currentValue;
                            }
                        }
                    }
                    else 
                    {
                        if (enableInterpolation)
                        {
                            result[rpmIndex, injIndex] = InterpolationFuelMap(injIndex, rpmIndex, injLogsB1, injLogsB2, cellMap, showOnlyChanges);
                        }
                        else if (shouldUpdate)
                        {
                            result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex].SafeMultiply(trim);
                        }
                    }
                }
            }

            if (enableSmooth)
                FuelMapSmoother.Smooth(result, KernelSize, KernelSigma);
            
            RoundFuelMap(result, round? 0: 2);

            return result;
        }

    }
}
