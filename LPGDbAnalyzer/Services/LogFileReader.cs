using LPGDbAnalyzer.Data;
using System.Globalization;
using System.Security.Cryptography;

namespace LPGDbAnalyzer.Services
{
    /// <summary>
    /// Reads a real-time log exported by the LPG ECU software: 2 header lines, then tab-separated
    /// rows of 22 columns (TEMPO GIRI LAMBDA GAS BENZ PRESS MAP T.RID T.GAS LIV SLOW FAST OX
    /// LAMBDA2 GAS2 BENZ2 SLOW2 FAST2 OX2 MARKER AUTOMARKER ECUMARKER).
    /// </summary>
    public static class LogFileReader
    {
        private const int Columns = 22;

        public sealed record Result(LogFile Log, List<LogRow> Rows, int SkippedLines, int LpgRows);

        public static Result Read(string path)
        {
            var lines = File.ReadAllLines(path);
            var rows = new List<LogRow>(lines.Length);
            int skipped = 0;

            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                if (TryParse(lines[i], out var row))
                    rows.Add(row);
                else
                    skipped++;
            }

            var log = new LogFile
            {
                FileName = Path.GetFileName(path),
                RecordedAt = ReadRecordedAt(lines.FirstOrDefault(), path),
                FileHash = Hash(path),
            };

            int lpgRows = rows.Count(r => r.Rpm > 0 && r.Gas1 > 0);

            return new Result(log, rows, skipped, lpgRows);
        }

        /// <summary>
        /// Date from the first header line, e.g. "Ver. 4 8 real time data, Friday, November 13, 2026 12:31:19 PM".
        /// Falls back to the file's last write time.
        /// </summary>
        public static DateTime ReadRecordedAt(string? header, string path)
        {
            if (header is not null)
            {
                const string marker = "real time data,";
                int start = header.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (start >= 0)
                {
                    var text = header[(start + marker.Length)..].Trim();

                    if (DateTime.TryParseExact(text, "dddd, MMMM d, yyyy h:mm:ss tt",
                            CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
                        return date;

                    if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
                        return date;
                }
            }

            return File.GetLastWriteTime(path);
        }

        private static bool TryParse(string line, out LogRow row)
        {
            row = new LogRow();

            var span = line.AsSpan();
            Span<Range> ranges = stackalloc Range[Columns + 1];
            int count = span.Split(ranges, '\t');

            if (count < Columns)
                return false;

            Span<double> v = stackalloc double[Columns];
            for (int i = 0; i < Columns; i++)
            {
                if (!double.TryParse(span[ranges[i]], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
                    return false;
            }

            row.TimeMs = (int)v[0];
            row.Rpm = (int)v[1];
            row.Lambda1 = v[2];
            row.Gas1 = v[3];
            row.Benz1 = v[4];
            row.Press = v[5];
            row.Map = v[6];
            row.TempReducer = v[7];
            row.TempGas = v[8];
            row.Level = v[9];
            row.Slow1 = v[10];
            row.Fast1 = v[11];
            row.Ox1 = v[12];
            row.Lambda2 = v[13];
            row.Gas2 = v[14];
            row.Benz2 = v[15];
            row.Slow2 = v[16];
            row.Fast2 = v[17];
            row.Ox2 = v[18];
            row.Marker = (int)v[19];
            row.AutoMarker = (int)v[20];
            row.EcuMarker = (int)v[21];
            return true;
        }

        private static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
    }
}
