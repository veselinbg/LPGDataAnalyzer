using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using LPGDbAnalyzer.Data;

namespace LPGDbAnalyzer.Services
{
    /// <summary>
    /// Turns stored log rows into the readings the analysis screens of LPG.Core work with.
    /// Each log is grouped on its own by the same <see cref="Parser"/> as a log file, and the logs are
    /// joined in recording order, exactly as LPGDataAnalyzer loads several checked files.
    /// </summary>
    public static class AnalysisLoader
    {
        /// <param name="recordedWith">The ECU setup each log was recorded with (its fuel table and temperature tables).</param>
        /// <param name="target">When set, every log's trims are brought to this setup (<see cref="TrimNormalizer"/>),
        /// so logs recorded with other temperature tables or fuel maps can be analyzed together.</param>
        /// <param name="normalizedLogs">Number of logs whose trims were changed.</param>
        public static DataItem[] Load(Database database, IEnumerable<LogFile> logs, int trimDelayMs,
            Func<LogFile, EcuSetup>? recordedWith, EcuSetup? target, out int normalizedLogs)
        {
            var all = new List<DataItem>();
            normalizedLogs = 0;

            foreach (var log in logs.OrderBy(l => l.RecordedAt).ThenBy(l => l.Id))
            {
                var rows = database.GetLogRows(log.Id).Select(r => Parser.CreateRow(
                    tempo: r.TimeMs, rpm: r.Rpm,
                    gasB1: r.Gas1, benzB1: r.Benz1,
                    press: r.Press, map: r.Map,
                    tempReducer: r.TempReducer, tempGas: r.TempGas,
                    slowB1: r.Slow1, fastB1: r.Fast1,
                    gasB2: r.Gas2, benzB2: r.Benz2,
                    slowB2: r.Slow2, fastB2: r.Fast2));

                var parser = new Parser(trimDelayMs);
                parser.Load(rows, log.FileName);

                if (recordedWith is not null && target is not null && TrimNormalizer.Normalize(parser.Data, recordedWith(log), target))
                    normalizedLogs++;

                all.AddRange(parser.Data);
            }

            return all.ToArray();
        }
    }
}
