using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Services
{
    internal class ReducerPrediction
    {
        static bool IsValid(DataItem d) =>
                                d.RPM > 1200 &&
                                d.LAMBDA_b1 is > 0.97 and < 1.03 &&
                                d.LAMBDA_b2 is > 0.97 and < 1.03 &&
                                d.BENZ_b1 > 0 && d.GAS_b1 > 0 &&
                                d.BENZ_b2 > 0 && d.GAS_b2 > 0;

        static string GetTempRange(double tempRid)
        {
            return Settings.ReductorTemperatureRanges
                .First(r => tempRid >= r.Min && tempRid <= r.Max)
                .Label;
        }

        int ComputeStep(double error)
        {
            if (Math.Abs(error) < 1.0)
                return 0;           // dead zone

            if (Math.Abs(error) < 3.0)
                return Math.Sign(error) * 1;

            if (Math.Abs(error) < 6.0)
                return Math.Sign(error) * 2;

            return Math.Sign(error) * 3; // hard limit per update
        }

        public Dictionary<string, int> PredictNewReducerTempCorrections(
            ICollection<DataItem> liveData,
            Dictionary<string, int> currentCorrections,
            double referencePressure
        )
        {
            var errorsByRange =
                liveData
                    .Where(IsValid)
                    .Select(d =>
                    {
                        double fuelError =
                        (
                            (d.SLOW_b1 + d.FAST_b1) / 2.0 +
                            (d.SLOW_b2 + d.FAST_b2) / 2.0
                        ) / 2.0;

                        double pressureError =
                            (referencePressure - d.PRESS) /
                            referencePressure * 100.0;

                        double totalError =
                            fuelError * 0.7 + pressureError * 0.3;

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

            var result = new Dictionary<string, int>();

            foreach (var (range, oldValue) in currentCorrections)
            {
                if (!errorsByRange.TryGetValue(range, out var error))
                {
                    result[range] = oldValue; // no data → freeze
                    continue;
                }

                int step = ComputeStep(error);

                int newValue = oldValue + step;
                newValue = Math.Clamp(newValue, -100, 100);

                result[range] = newValue;
            }

            return SmoothIntCorrections(result);
        }
        Dictionary<string, int> SmoothIntCorrections(
    Dictionary<string, int> table)
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
    }
}
