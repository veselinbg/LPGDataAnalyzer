using LPGDataAnalyzer.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LPGDbAnalyzer.Services
{
    /// <summary>
    /// The AEB configuration file (<c>.ml8</c>) that the ECU software saves and loads.
    /// <para>
    /// Format: the clear header <c>ENCRYPTED_MLTP_FILE\r\n</c>, then the body XOR'd with a repeating
    /// 10-byte key. XOR is symmetric, so decoding and encoding are the same step, and decode → encode
    /// gives the original file byte for byte. The body is INI-like text (CRLF, <c>[Section]</c>,
    /// <c>key=value</c>) with some binary blocks inside. It is read as Latin-1, so every byte maps to one
    /// character and the binary blocks survive unchanged.
    /// </para>
    /// <para>
    /// Verified with the user's configs and the AEB software (2026-11-18):
    /// the map is <c>[MappaCoefficientiK]</c> <c>MappaK{row}_{col}</c> (rows = injection time,
    /// plus one hidden extra row, a copy of the last one, used above the last breakpoint);
    /// the RPM axis is <c>RifGiriK{n}</c>; the injection axis is <c>RifTempiK{n}</c> in ticks of 2.56 µs
    /// (390.625 per ms); in <c>[Temperatura]</c> the LPG temperature table is <c>CoeffMot{n}</c> and the
    /// reducer table <c>Coeff{n}</c>, both stored as 100 + value in reverse band order (hottest band first).
    /// </para>
    /// </summary>
    public sealed class Ml8Config
    {
        public const string FileExtension = ".ml8";

        private static readonly byte[] Header = Encoding.ASCII.GetBytes("ENCRYPTED_MLTP_FILE\r\n");
        private static readonly byte[] Key = [0x41, 0x8A, 0x08, 0xC0, 0x10, 0x45, 0x84, 0xC0, 0x80, 0x50];
        private static readonly Encoding Latin1 = Encoding.Latin1;

        /// <summary>Injection times are stored in ticks of 2.56 µs.</summary>
        public const double TicksPerMs = 390.625;

        /// <summary>The decoded body as Latin-1 text.</summary>
        public string Text { get; }

        /// <summary>"DescrizAuto", the name shown by the AEB software (e.g. "baracuda v2.94").</summary>
        public string Name { get; }
        public double[] RpmAxis { get; }
        /// <summary>Petrol injection time breakpoints in ms (rounded to 0.01 ms, as the AEB software shows them).</summary>
        public double[] InjectionAxis { get; }
        /// <summary>[injection row][rpm column], without the hidden extra row.</summary>
        public double[][] Values { get; }
        /// <summary>LPG temperature table, one whole number per band of <see cref="Settings.GasTemperatureRanges"/>.</summary>
        public int[] GasTempCoef { get; }
        /// <summary>Reducer temperature table, one per band of <see cref="Settings.ReductorTemperatureRanges"/>.</summary>
        public int[] ReducerTempCoef { get; }

        private Ml8Config(string text)
        {
            Text = text;
            Name = TextValue("DescrizAuto")?.Trim() ?? "";

            int rows = IntValue("NumeroRighe");
            int columns = IntValue("NumeroColonne");
            if (rows < 2 || columns < 2)
                throw new InvalidDataException($"The fuel map has {rows} x {columns} cells.");

            RpmAxis = Enumerable.Range(0, columns).Select(c => (double)IntValue($"RifGiriK{c}")).ToArray();
            InjectionAxis = Enumerable.Range(0, rows)
                .Select(r => Math.Round(IntValue($"RifTempiK{r}") / TicksPerMs, 2))
                .ToArray();
            Values = Enumerable.Range(0, rows)
                .Select(r => Enumerable.Range(0, columns).Select(c => (double)IntValue($"MappaK{r}_{c}")).ToArray())
                .ToArray();

            string temperature = Section("Temperatura");
            GasTempCoef = ReadTemperatureTable(temperature, "CoeffMot", Settings.GasTemperatureRanges.Length);
            ReducerTempCoef = ReadTemperatureTable(temperature, "Coeff", Settings.ReductorTemperatureRanges.Length);
        }

        /// <summary>Reads and parses a .ml8 file. Throws <see cref="InvalidDataException"/> when it is not one.</summary>
        public static Ml8Config Load(byte[] file) => new(Latin1.GetString(Decode(file)));

        public static byte[] Decode(byte[] file)
        {
            if (file.Length < Header.Length || !file.AsSpan(0, Header.Length).SequenceEqual(Header))
                throw new InvalidDataException("This is not an AEB configuration file (the ENCRYPTED_MLTP_FILE header is missing).");

            var body = file.AsSpan(Header.Length).ToArray();
            Xor(body);
            return body;
        }

        public static byte[] Encode(byte[] body)
        {
            var file = new byte[Header.Length + body.Length];
            Header.CopyTo(file, 0);
            body.CopyTo(file, Header.Length);
            Xor(file.AsSpan(Header.Length));
            return file;
        }

        private static void Xor(Span<byte> data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= Key[i % Key.Length];
        }

        // ---------------- Writing ----------------

        /// <summary>Highest map value written; the user's maps are 100–200, so a larger value is an error.</summary>
        public const int MaxMapValue = 255;

        /// <summary>
        /// A copy of the .ml8 <paramref name="template"/> with a new fuel map. Only the <c>MappaK</c> lines change
        /// (the hidden extra row is written as a copy of the last row), and the name lines when
        /// <paramref name="description"/> is given; every other byte stays as it was.
        /// </summary>
        /// <param name="values">[injection row][rpm column], the same size as the template's map.</param>
        /// <param name="description">New "DescrizAuto" (e.g. "baracuda v2.95"); "NomeConfigurazione" becomes description + ".ml8".</param>
        public static byte[] WithMap(byte[] template, int[][] values, string? description)
        {
            var config = Load(template);
            int rows = config.Values.Length;
            int columns = config.Values[0].Length;

            if (values.Length != rows || values.Any(r => r.Length != columns))
                throw new InvalidOperationException($"The map must be {rows} rows x {columns} columns, like the configuration file.");
            var bad = values.SelectMany(r => r).Where(v => v < 1 || v > MaxMapValue).ToList();
            if (bad.Count > 0)
                throw new InvalidOperationException($"Map value {bad[0]} is outside 1–{MaxMapValue}.");

            string text = config.Text;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    text = ReplaceValue(text, $"MappaK{r}_{c}", values[r][c].ToString(CultureInfo.InvariantCulture), required: true);

            // The hidden row above the last breakpoint: a copy of the last row in every file the AEB software saved.
            for (int c = 0; c < columns; c++)
                text = ReplaceValue(text, $"MappaK{rows}_{c}", values[rows - 1][c].ToString(CultureInfo.InvariantCulture), required: false);

            if (!string.IsNullOrWhiteSpace(description))
            {
                text = ReplaceValue(text, "DescrizAuto", description.Trim(), required: false);
                text = ReplaceValue(text, "NomeConfigurazione", description.Trim() + FileExtension, required: false);
            }

            var file = Encode(Latin1.GetBytes(text));

            // Read it back: the map must come out exactly as written.
            var check = Load(file);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    if (check.Values[r][c] != values[r][c])
                        throw new InvalidOperationException($"Check failed: MappaK{r}_{c} reads back as {check.Values[r][c]}, not {values[r][c]}.");

            return file;
        }

        /// <summary>Replaces the value of the single line "key=..." (at the start of a line).</summary>
        private static string ReplaceValue(string text, string key, string value, bool required)
        {
            var pattern = new Regex($@"(?m)^{Regex.Escape(key)}=[^\r\n]*(?=\r?$)");
            int count = pattern.Matches(text).Count;
            if (count == 0 && !required)
                return text;
            if (count != 1)
                throw new InvalidDataException($"\"{key}\" occurs {count} times in the configuration file; expected once.");
            return pattern.Replace(text, $"{key}={value}", 1);
        }

        // ---------------- Reading values ----------------

        /// <summary>The value of the first line "key=..." anywhere in the file, or null.</summary>
        private string? TextValue(string key)
        {
            var m = Regex.Match(Text, $@"(?m)^{Regex.Escape(key)}=([^\r\n]*)\r?$");
            return m.Success ? m.Groups[1].Value : null;
        }

        private int IntValue(string key) => IntValue(Text, key);

        private static int IntValue(string text, string key)
        {
            var m = Regex.Match(text, $@"(?m)^{Regex.Escape(key)}=(-?\d+)\r?$");
            if (!m.Success)
                throw new InvalidDataException($"\"{key}\" is missing in the configuration file.");
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>The text of one [Section], up to the next section header.</summary>
        private string Section(string name)
        {
            int start = Text.IndexOf($"[{name}]\r\n", StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidDataException($"Section [{name}] is missing in the configuration file.");
            int end = Text.IndexOf("\r\n[", start + name.Length + 2, StringComparison.Ordinal);
            return end < 0 ? Text[start..] : Text[start..(end + 2)];
        }

        /// <summary>Stored as 100 + value, hottest band first; returned coldest band first.</summary>
        private static int[] ReadTemperatureTable(string section, string prefix, int bands) =>
            Enumerable.Range(0, bands)
                .Select(i => IntValue(section, $"{prefix}{bands - 1 - i}") - 100)
                .ToArray();
    }
}
