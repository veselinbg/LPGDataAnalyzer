using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPGDataAnalyzer.Services
{
    public class ReducerPrediction
    {
        public static Dictionary<string, double> PredictNewReducerTempCorrections(
            ICollection<DataItem> liveData,
            Dictionary<string, int> currentCorrections,
            double referencePressure,
            bool enableSmooth = true,
            double referenceMAP = 1.0,
            double maxRPM = 6200
        )
        {
            // Order and filter valid data
            var ordered = liveData
                .Where(IsValid)
                .OrderBy(d => d.TEMPO)
                .ToList();

            // Pair each item with previous
            var errorsByRange = ordered
                .Select((d, i) =>
                {
                    DataItem previous = i > 0 ? ordered[i - 1] : null;

                    double totalError = CalculateError(d, previous, referencePressure, referenceMAP, maxRPM);

                    return new
                    {
                        TempRange = GetTempRange(d.Temp_RID),
                        Error = totalError
                    };
                })
                .GroupBy(x => x.TempRange)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.Error)
                );

            // Compute new corrections
            var result = new Dictionary<string, double>();

            foreach (var (range, oldValue) in currentCorrections)
            {
                if (!errorsByRange.TryGetValue(range, out var error))
                {
                    result[range] = oldValue; // no data → freeze
                    continue;
                }

                //int step = ComputeStep(error);
                //int newValue = oldValue + step;

                double newValue = oldValue + error;
                newValue = Math.Clamp(newValue, -100, 100);

                result[range] = newValue.Round();
            }
            return result;
            //return enableSmooth ? SmoothIntCorrections(result) : result;
        }

        private static bool IsValid(DataItem d) =>
            d.BENZ_b1 > 0 && d.GAS_b1 > 0 &&
            d.BENZ_b2 > 0 && d.GAS_b2 > 0;

        private static string GetTempRange(double tempRid)
        {
            return Settings.ReductorTemperatureRanges
                .First(r => tempRid >= r.Min && tempRid <= r.Max)
                .Label;
        }

        private static int ComputeStep(double error)
        {
            if (Math.Abs(error) < 1.0) return 0;           // dead zone
            if (Math.Abs(error) < 3.0) return Math.Sign(error) * 1;
            if (Math.Abs(error) < 6.0) return Math.Sign(error) * 2;
            return Math.Sign(error) * 3;                   // hard limit per update
        }

        private static Dictionary<string, int> SmoothIntCorrections(Dictionary<string, int> table)
        {
            var keys = Settings.ReductorTemperatureRanges
                .Select(r => r.Label)
                .Where(table.ContainsKey)
                .ToList();

            var smoothed = new Dictionary<string, int>();

            for (int i = 0; i < keys.Count; i++)
            {
                int sum = table[keys[i]];
                int count = 1;

                if (i > 0) { sum += table[keys[i - 1]]; count++; }
                if (i < keys.Count - 1) { sum += table[keys[i + 1]]; count++; }

                smoothed[keys[i]] = (int)Math.Round(sum / (double)count);
            }

            return smoothed;
        }

        // ---------------- Core Error Calculation ----------------
        private static double CalculateError(
            DataItem d,
            DataItem previous,
            double referencePressure,
            double referenceMAP,
            double maxRPM)
        {
            double fuelError = d.Trim;

            // Pressure effect
            double pressureRatio = d.PRESS / referencePressure;
            if (pressureRatio <= 0) pressureRatio = 1.0;
            double pressureEffect = (Math.Sqrt(pressureRatio) - 1.0) * 100.0;
            pressureEffect = Math.Clamp(pressureEffect, -20.0, 20.0);

            // Transient detection
            double pressureDelta = previous != null ? d.PRESS - previous.PRESS : 0.0;
            double transientBoost = Math.Abs(pressureDelta) > 0.05 * referencePressure ? 0.15 : 0.0;

            // Load-based weighting
            double normalizedLoad = Math.Clamp(d.MAP / referenceMAP, 0.0, 1.0);
            double normalizedRPM = Math.Clamp(d.RPM / maxRPM, 0.0, 1.0);
            double loadFactor = 0.5 * normalizedLoad + 0.5 * normalizedRPM;

            // Trim weight (heuristic for open-loop)
            double trimWeight = 1.0;
            if (d.RPM < 800 || d.MAP > 0.95 * referenceMAP)
                trimWeight = 0.5;

            // Adaptive pressure weight
            double basePressureWeight = 0.25 * Math.Exp(-Math.Abs(fuelError) / 10.0);
            basePressureWeight *= 0.5 + 0.5 * loadFactor;
            double pressureWeight = basePressureWeight * (1.0 - trimWeight) + transientBoost;
            pressureWeight = Math.Clamp(pressureWeight, 0.05, 0.45);

            // Conflict correction
            if (Math.Sign(fuelError) != Math.Sign(pressureEffect))
                pressureWeight *= 0.5;

            double totalError = fuelError * trimWeight + pressureEffect * pressureWeight;

            return totalError;
        }
    }
}