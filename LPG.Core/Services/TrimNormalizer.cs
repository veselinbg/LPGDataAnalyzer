using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    /// <summary>
    /// What was in the LPG ECU while a log was recorded: the fuel map (with its own axes) and both temperature tables.
    /// </summary>
    /// <param name="Values">Map values as [injection row][rpm column].</param>
    public sealed record EcuSetup(
        double[] RpmAxis,
        double[] InjectionAxis,
        double[][] Values,
        IReadOnlyList<int> GasTempCoef,
        IReadOnlyList<int> ReducerTempCoef)
    {
        /// <summary>Map value at an engine position, bilinear between the breakpoints as the ECU does.</summary>
        internal double MapValueAt(double rpm, double benz)
        {
            var (r0, r1, fr) = FuelMapPrediction.Bracket(RpmAxis, rpm);
            var (i0, i1, fi) = FuelMapPrediction.Bracket(InjectionAxis, benz);

            return Values[i0][r0] * (1 - fr) * (1 - fi)
                 + Values[i0][r1] * fr * (1 - fi)
                 + Values[i1][r0] * (1 - fr) * fi
                 + Values[i1][r1] * fr * fi;
        }

        internal bool SameMap(EcuSetup other) =>
            RpmAxis.SequenceEqual(other.RpmAxis) &&
            InjectionAxis.SequenceEqual(other.InjectionAxis) &&
            Values.Length == other.Values.Length &&
            Values.Zip(other.Values).All(rows => rows.First.SequenceEqual(rows.Second));

        internal bool SameTemperatureTables(EcuSetup other) =>
            GasTempCoef.SequenceEqual(other.GasTempCoef) && ReducerTempCoef.SequenceEqual(other.ReducerTempCoef);
    }

    /// <summary>
    /// Brings the FAST trims of a log recorded with one ECU setup to what they would have been with another, so logs
    /// recorded with different temperature tables (or fuel maps) can be analyzed together.
    /// <para>The ECU's gas time is proportional to the map value at the engine position (bilinear) and to
    /// (1 + table value / 100) of each temperature table at the current temperature (interpolated between the band
    /// upper temperatures; verified from GAS in the logs, 2026-11-14). With the target setup the gas time would be
    /// R = map_target / map_recorded × the temperature factors' ratio times as long: R − 1 more fuel in %, so the trim
    /// would have been that much lower (positive trim = lean). The map ratio is taken at the rows the trim describes
    /// (<see cref="DataItem.DelayedPositions"/>), per bank.</para>
    /// <para>Readings with FAST exactly 0 on both banks (petrol ECU not regulating) are left as they are, so the
    /// predictions still recognise and skip them.</para>
    /// <para>The temperature part follows the verified ECU formula. The map part assumes the trim follows a map change
    /// one to one, as the map prediction itself does; check it with the prediction accuracy once logs of two maps exist.</para>
    /// </summary>
    public static class TrimNormalizer
    {
        /// <summary>Shifts the trims of <paramref name="items"/> (one log) from <paramref name="recorded"/> to <paramref name="target"/>.</summary>
        /// <returns>True when anything was changed (the setups differ).</returns>
        public static bool Normalize(DataItem[] items, EcuSetup recorded, EcuSetup target)
        {
            bool sameMap = recorded.SameMap(target);
            bool sameTables = recorded.SameTemperatureTables(target);

            if (sameMap && sameTables)
                return false;

            foreach (var d in items)
            {
                if (d.FAST_b1 == 0 && d.FAST_b2 == 0)
                    continue;

                double temperature = sameTables ? 1 : TemperatureRatio(d, recorded, target);
                var positions = d.DelayedPositions.Length > 0 ? d.DelayedPositions : d.Positions;

                double ratio1 = temperature * (sameMap ? 1 : MapRatio(positions, static p => p.Benz1, recorded, target) ?? 1);
                double ratio2 = temperature * (sameMap ? 1 : MapRatio(positions, static p => p.Benz2, recorded, target) ?? 1);

                Shift(d, (ratio1 - 1) * 100, (ratio2 - 1) * 100);
            }

            return true;
        }

        private static double TemperatureRatio(DataItem d, EcuSetup recorded, EcuSetup target)
        {
            double Factor(EcuSetup s) =>
                (1 + TemperatureCorrectionPrediction.TableValueAt(d.Temp_GAS, Settings.GasTemperatureRanges, s.GasTempCoef) / 100) *
                (1 + TemperatureCorrectionPrediction.TableValueAt(d.Temp_RID, Settings.ReductorTemperatureRanges, s.ReducerTempCoef) / 100);

            return Factor(target) / Factor(recorded);
        }

        /// <summary>Average of map_target / map_recorded over the rows; null when no row has a petrol time.</summary>
        private static double? MapRatio(EnginePosition[] positions, Func<EnginePosition, double> benzSelector, EcuSetup recorded, EcuSetup target)
        {
            double sum = 0;
            int rows = 0;

            foreach (var p in positions)
            {
                double benz = benzSelector(p);
                if (benz <= 0 || p.Rpm <= 0)
                    continue;

                double before = recorded.MapValueAt(p.Rpm, benz);
                if (before <= 0)
                    continue;

                sum += target.MapValueAt(p.Rpm, benz) / before;
                rows++;
            }

            return rows == 0 ? null : sum / rows;
        }

        /// <summary>Lowers each bank's FAST trim by the extra fuel (%) and updates the fields derived from it.</summary>
        private static void Shift(DataItem d, double extraFuel1, double extraFuel2)
        {
            d.FAST_b1 = (d.FAST_b1 - extraFuel1).Round();
            d.FAST_b2 = (d.FAST_b2 - extraFuel2).Round();

            double trim1 = d.SLOW_b1 + d.FAST_b1;
            double trim2 = d.SLOW_b2 + d.FAST_b2;
            double trim = (trim1 + trim2) / 2;

            d.Fast = ((d.FAST_b1 + d.FAST_b2) / 2).Round();
            d.Trim_b1 = trim1.Round();
            d.Trim_b2 = trim2.Round();
            d.Trim = trim.Round();
            d.TrimDiff = (trim1 - trim2).Round();
            d.AFR_b1 = (15.6 / (1 + trim1 / 100)).Round();
            d.AFR_b2 = (15.6 / (1 + trim2 / 100)).Round();
            d.AFR = (15.6 / (1 + trim / 100)).Round();
        }
    }
}
