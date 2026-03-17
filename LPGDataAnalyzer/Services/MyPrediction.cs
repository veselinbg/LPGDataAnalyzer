using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
        public static double?[,] BuildTable(DataItem[] logs, double?[,] cellMap, int minCount = 0,
            bool enableSmooth = true, bool enableInterpolation = false, bool showOnlyChanges = false, bool round = true, bool preFilter = true)
        {
            int rpmLength = cellMap.GetLength(0);
            int injLength = cellMap.GetLength(1);
            var result = new double?[rpmLength, injLength];

            // Precompute logs grouped by injection ranges
            var logsByInjectionB1 = new List<DataItem>[injLength];
            var logsByInjectionB2 = new List<DataItem>[injLength];

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];
                logsByInjectionB1[injIndex] = logs
                    .Where(d => (d.BENZ_b1 > inj.Min && d.BENZ_b1 <= inj.Max) && (!preFilter || (d.FAST_b1 >-10 && d.FAST_b1 < 10))).ToList();

                logsByInjectionB2[injIndex] = logs
                    .Where(d => (d.BENZ_b2 > inj.Min && d.BENZ_b2 <= inj.Max) && (!preFilter || (d.FAST_b2 >-10 && d.FAST_b2 < 10))).ToList();
            }

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];

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
                    double trim = 1.0;

                    if (rpmLogs.Length > minCount)
                    {
                        
                        trim = 1 + (Math.Abs(rpmLogs.Median())>0.5? (rpmLogs.Median() / 100) :0);
                        //result[rpmIndex, injIndex] = Math.Abs(rpmLogs.Median()) > 0.5 ? (rpmLogs.Median() / 100) : null;
                        result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex].SafeMultiply(trim);
                        continue;
                    }
                    if (enableInterpolation && rpm.Label > 3400 && inj.Label > 5.8)
                    {
                        double t = 1.0;
                        int rpmSave = rpmIndex;

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
                            
                            if (lowerLogs.Length > 0)
                            {
                                double tNew = 1 + lowerLogs.Median() / 100;

                                if (tNew > t)
                                {
                                    t = tNew;
                                    rpmSave = lowerRpm;
                                }
                                else
                                {
                                    rpmSave = rpmIndex;
                                    break;
                                }
                            }
                        }
                        if (!showOnlyChanges || trim != 1)
                        {
                            var newValue = cellMap[rpmIndex, injIndex].SafeMultiply(t);

                            if (inj.Label > 4.8) //increate table for more rpms 
                                newValue += rpmIndex - rpmSave;

                            result[rpmIndex, injIndex] = newValue;
                        }
                    }
                    else
                    {
                        if (!showOnlyChanges || trim != 1)
                            result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex].SafeMultiply(trim);
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
