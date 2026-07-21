using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;
using System.Buffers;

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
        private static double? InterpolationFuelMap(
            int injIndex,
            int rpmIndex,
            DataItem[][] gridB1,
            DataItem[][] gridB2,
            double?[,] cellMap, 
            bool showOnlyChanges,
            int rpmLength)
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
                    int lowerIdx = injIndex * rpmLength + lowerRpm;

                    var lowerLogsB1 = gridB1[lowerIdx].Select(x => x.Trim_b1).ToArray();

                    var lowerLogsB2 = gridB2[lowerIdx].Select(x => x.Trim_b2).ToArray();

                    var lowerLogs = lowerLogsB1.Merge(lowerLogsB2);

                    if (lowerLogs.Length != 0)
                    {
                        double tNew = 1 + lowerLogs.MedianCore() / 100;

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
        private static double ComputeCellQuality(DataItem[] cell)
        {
            if (cell.Length < 5)
                return 0;

            double mapSpread =
                cell.Max(x => x.MAP) - cell.Min(x => x.MAP);

            double variance = 0;
            double mean = cell.Average(x => x.MAP);

            foreach (var d in cell)
                variance += (d.MAP - mean) * (d.MAP - mean);

            variance /= cell.Length;

            double stability =
                1.0 / (1.0 + variance);

            double spreadScore =
                Math.Min(1.0, mapSpread / 0.20);

            return stability * spreadScore;
        }

        private static int ProcessLogs(
                                        DataItem[] items,
                                        Func<DataItem, double> trimSelector,
                                        (double Min, double Max) mapRange,
                                        (int Min, int Max, int Label) rpmRange,
                                        double referencePressure,
                                        double[] buffer,
                                        int startIndex,
                                        Dictionary<int, DataItem> invalidItems,
                                        MapRegression? regression)
        {
            int count = startIndex;

            double localQuality = 0;
            if (regression is { Enabled: true }) 
                localQuality = ComputeCellQuality(items);

            foreach (var d in items)
            {
                if (d.MAP < mapRange.Min || d.MAP > mapRange.Max)
                {
                    invalidItems.TryAdd(d.TEMPO, d);
                    continue;
                }

                double trim = trimSelector(d);
               
                if (regression is { Enabled: true })
                {
                    double mapEffect =
                        regression.Slope *
                        (d.MAP - regression.CenterMap);
                    
                    double strength =
                        regression.Confidence *
                        localQuality *
                        0.8;
                    trim -= mapEffect * strength;
                }

                buffer[count++] = ApplyCorrections(
                    trim,
                    d.Temp_GAS,
                    d.Temp_RID,
                    d.PRESS,
                    referencePressure);
            }

            return count;
        }
        private class MapRegression
        {
            public bool Enabled;

            public double Slope;

            public double Intercept;

            public double R2;

            public double Confidence;

            public int SampleCount;

            public double MapSpread;
            public double CenterMap;
        }
        private static MapRegression CalculateMapRegression(
                                    DataItem[] logs,
                                    Func<DataItem, double> trimSelector,
                                    double mapSpread)
        {
            int n = logs.Length;
            if (n < 10)
                return new MapRegression { R2 = 0 };

            double sumX = 0, sumY = 0;

            foreach (var d in logs)
            {
                sumX += d.MAP;
                sumY += trimSelector(d);
            }

            double meanX = sumX / n;
            double meanY = sumY / n;

            double ssXX = 0;
            double ssXY = 0;
            double ssYY = 0;

            foreach (var d in logs)
            {
                double x = d.MAP;
                double y = trimSelector(d);

                double dx = x - meanX;
                double dy = y - meanY;

                ssXX += dx * dx;
                ssXY += dx * dy;
                ssYY += dy * dy;
            }

            if (ssXX < 1e-6)
                return new MapRegression { R2 = 0 };

            double slope = ssXY / ssXX;
            double intercept = meanY - slope * meanX;

            double r2 =
                (ssXX * ssYY < 1e-6)
                ? 0
                : (ssXY * ssXY) / (ssXX * ssYY);

            double quality = ComputeCellQuality(logs);

            double effectiveSamples = n * quality;
            double sampleFactor = Math.Min(1.0, Math.Sqrt(effectiveSamples / 60.0));
            double spreadFactor = Math.Min(1.0, mapSpread / 0.30);
            double r2Factor = Math.Clamp((r2 - 0.05) / 0.35, 0, 1);

            double confidence = sampleFactor * spreadFactor * r2Factor * 0.5;

            return new MapRegression
            {
                Enabled = confidence > 0.05,
                Slope = slope,
                Intercept = intercept,
                R2 = r2,
                Confidence = confidence,
                SampleCount = n,
                MapSpread = mapSpread,
                CenterMap = meanX
            };
        }

        private static void BuildLogGrid(
            DataItem[] logs,
            out DataItem[][] logsGridB1,
            out DataItem[][] logsGridB2,
            out DataItem[][] logsByInjectionB1,
            out DataItem[][] logsByInjectionB2,
            out (double Min, double Max)?[] mapRanges,
            out double[] mapSpreadB1,
            out double[] mapSpreadB2,
            double benzDiffMax)
        {
            int injLength = Settings.InjectionRanges.Length;
            int rpmLength = Settings.RpmColumns.Length;
            int cellCount = injLength * rpmLength;

            var gridB1 = new List<DataItem>[cellCount];
            var gridB2 = new List<DataItem>[cellCount];

            var injB1 = new List<DataItem>[injLength];
            var injB2 = new List<DataItem>[injLength];
            var mapLogs = new List<DataItem>[injLength];

            for (int i = 0; i < injLength; i++)
            {
                injB1[i] = [];
                injB2[i] = [];
                mapLogs[i] = [];
            }

            for (int i = 0; i < cellCount; i++)
            {
                gridB1[i] = [];
                gridB2[i] = [];
            }

            // ---------------- FILL ----------------

            foreach (var log in logs)
            {
                int rpmIndex = log.GetRpmIndex();
                if (rpmIndex < 0)
                    continue;

                int b1 = log.GetInjectionIndex(x => x.BENZ_b1);
                int b2 = log.GetInjectionIndex(x => x.BENZ_b2);

                if (b1 >= 0)
                {
                    int idx = b1 * rpmLength + rpmIndex;
                    gridB1[idx].Add(log);
                    injB1[b1].Add(log);

                    if (Math.Abs(log.BENZ_Diff) < benzDiffMax)
                        mapLogs[b1].Add(log);
                }

                if (b2 >= 0)
                {
                    int idx = b2 * rpmLength + rpmIndex;
                    gridB2[idx].Add(log);
                    injB2[b2].Add(log);

                    if (Math.Abs(log.BENZ_Diff) < benzDiffMax && b2 != b1)
                        mapLogs[b2].Add(log);
                }
            }

            logsGridB1 = new DataItem[cellCount][];
            logsGridB2 = new DataItem[cellCount][];

            logsByInjectionB1 = new DataItem[injLength][];
            logsByInjectionB2 = new DataItem[injLength][];

            mapSpreadB1 = new double[cellCount];
            mapSpreadB2 = new double[cellCount];

            mapRanges = new (double Min, double Max)?[injLength];

            // ---------------- CONVERT + MAP RANGES ----------------

            for (int inj = 0; inj < injLength; inj++)
            {
                logsByInjectionB1[inj] = injB1[inj].ToArray();
                logsByInjectionB2[inj] = injB2[inj].ToArray();

                if (mapLogs[inj].Count > 0)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;

                    foreach (var d in mapLogs[inj])
                    {
                        if (d.MAP < min) min = d.MAP;
                        if (d.MAP > max) max = d.MAP;
                    }

                    mapRanges[inj] = (min, max);
                }
            }

            // ---------------- CONVERT + MAP SPREAD ----------------

            for (int i = 0; i < cellCount; i++)
            {
                logsGridB1[i] = gridB1[i].ToArray();
                logsGridB2[i] = gridB2[i].ToArray();

                if (gridB1[i].Count > 1)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;

                    foreach (var d in gridB1[i])
                    {
                        if (d.MAP < min) min = d.MAP;
                        if (d.MAP > max) max = d.MAP;
                    }

                    mapSpreadB1[i] = max - min;
                }

                if (gridB2[i].Count > 1)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;

                    foreach (var d in gridB2[i])
                    {
                        if (d.MAP < min) min = d.MAP;
                        if (d.MAP > max) max = d.MAP;
                    }

                    mapSpreadB2[i] = max - min;
                }
            }
        }
        public static (double?[,] result, List<DataItem> invalidItems) BuildTable(
            DataItem[] logs,
            double?[,] cellMap,
            double referencePressure,
            IReadOnlyList<HistorySnapshot>? historySnapshots = null,
            int minCount = 0,
            bool enableSmooth = true,
            bool enableInterpolation = false,
            bool showOnlyChanges = false,
            bool round = true,
            bool showOnlyMultiplier = false,
            double minChangeValue = 0.5d,
            double benzDiffMax = 10d,
            bool allwaysApplyNegativeTrim = true,
            bool showOnlyCount = false,
            bool enableMapRegression = true)
        {
            Dictionary<int, DataItem> invalidItems = [];
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            var result = new double?[rpmLength, injLength];
            // 🔥 Precompute injection grouping
            BuildLogGrid(
     logs,
     out var logsGridB1,
     out var logsGridB2,
     out var logsByInjectionB1,
     out var logsByInjectionB2,
     out var mapRanges,
     out var mapSpreadGridB1,
     out var mapSpreadGridB2,
     benzDiffMax);
            // 🔥 Main loop
            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var mapRange = mapRanges[injIndex];

                if (!mapRange.HasValue)
                {
                    if (!showOnlyChanges)
                    {
                        for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                            result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex];
                    }

                    continue;
                }
                
                var range = mapRange.Value;

                var injLogsB1 = logsByInjectionB1[injIndex];
                var injLogsB2 = logsByInjectionB2[injIndex];

                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var rpm = Settings.RpmColumns[rpmIndex];

                    int idx = injIndex * rpmLength + rpmIndex;

                    var cellB1 = logsGridB1[idx];
                    var cellB2 = logsGridB2[idx];

                    var mapSpreadB1 = mapSpreadGridB1[idx];
                    
                    var mapSpreadB2 = mapSpreadGridB2[idx];

                    var mapRegB1 = enableMapRegression ? CalculateMapRegression(cellB1, d => d.FAST_b1, mapSpreadB1) : null;
                    var mapRegB2 = enableMapRegression ? CalculateMapRegression(cellB2, d => d.FAST_b2, mapSpreadB2) : null;

                    int countB1 = 0;
                    int countB2 = 0;
                    double[] bufferB1 = ArrayPool<double>.Shared.Rent(cellB1.Length);
                    double[] bufferB2 = ArrayPool<double>.Shared.Rent(cellB2.Length);
                    // ✅ B1
                    countB1 = ProcessLogs(
                        cellB1,
                        d => d.FAST_b1,
                        range,
                        rpm,
                        referencePressure,
                        bufferB1,
                        countB1,
                        invalidItems,
                        mapRegB1);

                    // ✅ B2
                    countB2 = ProcessLogs(
                        cellB2,
                        d => d.FAST_b2,
                        range,
                        rpm,
                        referencePressure,
                        bufferB2,
                        countB2,
                        invalidItems,
                        mapRegB2);

                    int count = countB1 + countB2;

                    bool hasEnoughLogs = count > minCount;

                    double multiplier = 0;
                    double trim = 1;

                    if (count > 0 && (hasEnoughLogs || !showOnlyMultiplier))
                    {
                        double medianB1 = bufferB1.AsSpan(0, countB1).MedianCore();
                        double medianB2 = bufferB2.AsSpan(0, countB2).MedianCore();

                        multiplier = (medianB1 + medianB2 ) / 4;
                    }
                    if (hasEnoughLogs && !showOnlyMultiplier)
                        trim = TrimCalculation(multiplier, minChangeValue, allwaysApplyNegativeTrim);

                    bool shouldUpdate = !showOnlyChanges || trim != 1;

                    if(showOnlyCount)
                    {
                        result[rpmIndex, injIndex] = count > 0 ? count:  null;
                    }
                    else if (hasEnoughLogs)
                    {
                        if (showOnlyMultiplier)
                        {
                            result[rpmIndex, injIndex] = multiplier;
                        }
                        else if (shouldUpdate)
                        {
                            var currentValue = cellMap[rpmIndex, injIndex];
                            double? newValue = cellMap[rpmIndex, injIndex].SafeMultiply(trim);

                            if (newValue.HasValue &&
                                historySnapshots?.Count > 0 &&
                                trim != 1)
                            {
                                var values = HistoryHelper.GetCellHistoryValues(
                                    historySnapshots, rpmIndex, injIndex);

                                values.Add(newValue.Value);
                                newValue = values.Median();
                            }

                            if((currentValue != newValue.Value.Round(0)) || !showOnlyChanges)
                                result[rpmIndex, injIndex] = newValue;
                        }
                    }
                    else
                    {
                        if (enableInterpolation)
                        {
                            result[rpmIndex, injIndex] =
                                InterpolationFuelMap(
                                    injIndex,
                                    rpmIndex,
                                    logsGridB1,
                                    logsGridB2,
                                    cellMap,
                                    showOnlyChanges,
                                    rpmLength); 
                        }
                        else if (shouldUpdate)
                        {
                            result[rpmIndex, injIndex] =
                                cellMap[rpmIndex, injIndex].SafeMultiply(trim);
                        }
                    }
                    ArrayPool<double>.Shared.Return(bufferB1);
                    ArrayPool<double>.Shared.Return(bufferB2);
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
        
        private static double ApplyCorrections(double trim, double tempGas, double tempRid, double pressure, double referencePressure)
        {
            double lpgCoef = GetTemperatureCoef(tempGas, Settings.GasTemperatureRanges, Settings.GasTemperatureCorrectionCoef);
            double ridCoef = GetTemperatureCoef(tempRid, Settings.ReductorTemperatureRanges, Settings.ReductorTemperatureCorrectionCoef);

            pressure = pressure == 0 ? referencePressure : pressure;

            double pressCoef = trim < 0 ? Math.Sqrt(referencePressure / pressure) : Math.Sqrt(pressure / referencePressure);
            
            return trim * pressCoef * (1 + lpgCoef / 100) * (1 + ridCoef / 100);
        }
        public static double TrimCalculation(double trim, double minChangeValue, bool allwaysApplyNegativeTrim)
        {
            return 1 + (Math.Abs(trim) > minChangeValue || (trim < 0 && allwaysApplyNegativeTrim) ? trim / 100 : 0); //Always apply negative trims to save fuel.
        }
    }
}
