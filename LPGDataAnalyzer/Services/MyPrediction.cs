using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Services
{
    internal class MyPrediction
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
        public static double?[,] BuildTable(DataItem[] logs, double?[,] cellMap,
            bool enableSmooth, bool enableInterpolation, bool showOnlyChanges = false, bool round = true)
        {
            int rpmLength = cellMap.GetLength(0);
            int injLength = cellMap.GetLength(1);
            var result = new double?[rpmLength, injLength];

            // Precompute logs grouped by injection ranges
            var logsByInjection = new List<DataItem>[injLength];
            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];
                logsByInjection[injIndex] = logs
                    .Where(d => (d.BENZ_b1 > inj.Min && d.BENZ_b1 <= inj.Max) ||
                                (d.BENZ_b2 > inj.Min && d.BENZ_b2 <= inj.Max))
                    .ToList();
            }

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];
                var injLogs = logsByInjection[injIndex];

                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var rpm = Settings.RpmColumns[rpmIndex];
                    var rpmLogs = injLogs
                        .Where(d => d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        .Select(d => d.Trim)
                        .ToArray();

                    double trim = 1.0;

                    if (rpmLogs.Length > 0)
                    {
                        trim = 1 + rpmLogs.Median() / 100;
                        result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex].SafeMultiply(trim);
                        continue;
                    }
                    if (enableInterpolation && rpm.Label > 3400 && inj.Label > 5.8)
                    {
                        double t = 1.0;
                        int rpmSave = rpmIndex;

                        for (int lowerRpm = rpmIndex - 1; lowerRpm >= 0; lowerRpm--)
                        {
                            var lowerLogs = injLogs
                                .Where(d => d.RPM > Settings.RpmColumns[lowerRpm].Min &&
                                            d.RPM <= Settings.RpmColumns[lowerRpm].Max)
                                .Select(d => d.Trim)
                                .ToArray();

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
            
            RoundFuelMap(result, round? 0: 1);

            return result;
        }

    }
}
