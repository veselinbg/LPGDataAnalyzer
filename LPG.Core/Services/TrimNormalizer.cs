using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    /// <summary>
    /// The fixed times the ECU adds to the gas injection time (AEB .ml8, in ms): the gas injector dead time, and below
    /// the "Extra-injection ident. time" (petrol time) the "Extrainj. sensitivity" time instead (verified from GAS in the
    /// logs, 2026-11-19: the gas time steps by dead time − sensitivity at exactly the ident time).
    /// </summary>
    public sealed record GasTimeOffsets(double DeadTimeMs, double SensitivityMs, double IdentTimeMs)
    {
        /// <summary>The time added to the gas time at petrol time <paramref name="benz"/>.</summary>
        public double At(double benz) => benz < IdentTimeMs ? SensitivityMs : DeadTimeMs;
    }

    /// <summary>
    /// What was in the LPG ECU while a log was recorded: the fuel map (with its own axes), both temperature tables and,
    /// when known (from the .ml8), the fixed times added to the gas time.
    /// </summary>
    /// <param name="Values">Map values as [injection row][rpm column].</param>
    /// <param name="Offsets">Null when unknown (a table read from a screenshot): map changes then count one to one.</param>
    public sealed record EcuSetup(
        double[] RpmAxis,
        double[] InjectionAxis,
        double[][] Values,
        IReadOnlyList<int> GasTempCoef,
        IReadOnlyList<int> ReducerTempCoef,
        GasTimeOffsets? Offsets = null)
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

        /// <summary>
        /// Factor both temperature tables apply to the gas time: (1 + LPG table / 100) × (1 + reducer table / 100), each
        /// interpolated between the band upper temperatures as the ECU does.
        /// </summary>
        internal double TemperatureFactor(double tempGas, double tempReducer) =>
            (1 + TemperatureCorrectionPrediction.TableValueAt(tempGas, Settings.GasTemperatureRanges, GasTempCoef) / 100) *
            (1 + TemperatureCorrectionPrediction.TableValueAt(tempReducer, Settings.ReductorTemperatureRanges, ReducerTempCoef) / 100);

        /// <summary>Same map and same fixed times, so the same gas time at every engine position.</summary>
        internal bool SameMap(EcuSetup other) =>
            Equals(Offsets, other.Offsets) &&
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
    /// <para>The temperature part follows the verified ECU formula. The map part uses the fixed times added to the gas
    /// time when both setups know them (<see cref="EcuSetup.Offsets"/>), so a change of the gas dead time compensated by
    /// the map counts as no change; without them it assumes the trim follows a map change one to one.</para>
    /// </summary>
    public static class TrimNormalizer
    {
        /// <summary>Lower limit of GAS − fixed time, so a short or odd reading can't blow up the ratio.</summary>
        private const double MinScaledGasMs = 0.1;

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

                double ratio1 = temperature * (sameMap ? 1 : MapRatio(positions, static p => p.Benz1, d.GAS_b1, recorded, target) ?? 1);
                double ratio2 = temperature * (sameMap ? 1 : MapRatio(positions, static p => p.Benz2, d.GAS_b2, recorded, target) ?? 1);

                Shift(d, (ratio1 - 1) * 100, (ratio2 - 1) * 100);
            }

            return true;
        }

        private static double TemperatureRatio(DataItem d, EcuSetup recorded, EcuSetup target)
        {
            return target.TemperatureFactor(d.Temp_GAS, d.Temp_RID) / recorded.TemperatureFactor(d.Temp_GAS, d.Temp_RID);
        }

        /// <summary>
        /// Average over the rows of gas time with the target setup / gas time as recorded; null when no row has a
        /// petrol time. When both setups know their fixed times, only the part of the gas time above them (GAS − fixed
        /// time, from the reading's gas time) scales with the map, and the fixed times are swapped. Otherwise it is
        /// map_target / map_recorded (map changes one to one).
        /// <para>On the 4 v2.94 logs with the older logs brought to v2.94 (leave one file out, 2026-11-19): 5.17 %
        /// against 5.08 % one to one.</para>
        /// </summary>
        private static double? MapRatio(EnginePosition[] positions, Func<EnginePosition, double> benzSelector, double gas,
            EcuSetup recorded, EcuSetup target)
        {
            bool offsets = recorded.Offsets is not null && target.Offsets is not null && gas > 0;
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

                double mapRatio = target.MapValueAt(p.Rpm, benz) / before;
                if (offsets)
                {
                    double fixedBefore = recorded.Offsets!.At(benz);
                    double scaled = Math.Max(gas - fixedBefore, MinScaledGasMs);
                    sum += (scaled * mapRatio + target.Offsets!.At(benz)) / (scaled + fixedBefore);
                }
                else
                {
                    sum += mapRatio;
                }
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
