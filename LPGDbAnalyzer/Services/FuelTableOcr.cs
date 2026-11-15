using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using Tesseract;

namespace LPGDbAnalyzer.Services
{
    /// <summary>
    /// Reads a fuel table screenshot: RPM header row, injection-time label column and the cell values.
    /// <para>
    /// The ECU software draws black digits on pink/red/blue cells with red borders, and the axis labels
    /// in white on red. Tested on FuelTable/Capture2.PNG (2026-11-13):
    /// <list type="bullet">
    /// <item>The plain colour image lost the blue (selected) cell and read 131 as 1893, and found no axis labels.</item>
    /// <item>The brightest channel of each pixel (max of R, G, B) turns every coloured background and the red
    /// borders white while black digits stay black: all 144 cells read correctly.</item>
    /// <item>The axis labels are read one by one from their own crop, inverted (255 - min of R, G, B) so white
    /// text becomes black. The crop must stay inside the label cell (75% of the column width) or the
    /// border is read as an extra "1".</item>
    /// </list>
    /// </para>
    /// Every number is placed by its position, so a value the OCR misses leaves an empty cell in the right
    /// place instead of shifting the row. The result always has to be checked by the user.
    /// </summary>
    public static class FuelTableOcr
    {
        /// <summary>
        /// Axes are null when they could not be found (the user enters them).
        /// Values are [injection row][rpm column]; null = not read.
        /// </summary>
        public sealed record Result(double?[]? RpmAxis, double?[]? InjectionAxis, double?[][] Values);

        private sealed record Word(string Text, double X, double Y, double Height);

        /// <summary>Converts one pixel (R, G, B) to a grey level.</summary>
        private delegate byte GreyLevel(byte r, byte g, byte b);

        private static readonly GreyLevel BrightestChannel = static (r, g, b) => Math.Max(r, Math.Max(g, b));
        private static readonly GreyLevel InvertedDarkestChannel = static (r, g, b) => (byte)(255 - Math.Min(r, Math.Min(g, b)));

        private const int TableScale = 3;
        private const int LabelScale = 4;
        private const int LabelPadding = 20;

        public static Result Read(byte[] image, string tessdataPath)
        {
            using var source = new Bitmap(new MemoryStream(image));

            using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
            engine.SetVariable("tessedit_char_whitelist", "0123456789.");
            engine.SetVariable("load_system_dawg", "0");
            engine.SetVariable("load_freq_dawg", "0");

            // ---- 1. Cell values (and axes, when they are drawn in dark text) ----
            var words = ReadWords(engine, source)
                .Where(w => DigitCount(w.Text) is >= 1 and <= 5) // drops dotted borders read as "7777777"
                .ToList();

            var lines = GroupIntoLines(words);

            int headerIndex = lines.FindIndex(IsRpmHeader);
            var header = headerIndex >= 0 ? lines[headerIndex] : null;

            var dataLines = (headerIndex >= 0 ? lines.Skip(headerIndex + 1) : lines)
                .Where(l => l.Count >= 2)
                .ToList();

            if (dataLines.Count == 0)
                return new Result(null, null, []);

            // Column centres: from the header, or else from the line with the most numbers.
            var columnSource = header ?? dataLines.MaxBy(l => l.Count)!;
            var centres = columnSource.Select(w => w.X).ToArray();
            double columnSpacing = centres.Length > 1 ? (centres[^1] - centres[0]) / (centres.Length - 1) : 60;

            var rowCentres = dataLines.Select(l => l.Average(w => w.Y)).ToArray();
            double rowSpacing = rowCentres.Length > 1 ? (rowCentres[^1] - rowCentres[0]) / (rowCentres.Length - 1) : 30;

            var values = new List<double?[]>();
            var labels = new List<double?>();
            bool anyLabel = false;

            foreach (var line in dataLines)
            {
                var row = new double?[centres.Length];
                double? label = null;

                foreach (var w in line)
                {
                    // Left of the first column: the injection-time label of this row.
                    if (w.X < centres[0] - 0.6 * columnSpacing)
                    {
                        label = ParseLabel(w.Text);
                        anyLabel |= label is not null;
                        continue;
                    }

                    int col = Nearest(centres, w.X);
                    if (Math.Abs(centres[col] - w.X) > 0.5 * columnSpacing)
                        continue;

                    row[col] ??= ParseCell(w.Text);
                }

                values.Add(row);
                labels.Add(label);
            }

            double?[]? rpmAxis = header?.Select(w => ParseCell(w.Text)).ToArray();
            double?[]? injAxis = anyLabel ? labels.ToArray() : null;

            // ---- 2. Axes drawn as light text on coloured cells: read each label from its own crop ----
            rpmAxis ??= ReadRpmLabels(engine, source, centres, columnSpacing, rowCentres[0], rowSpacing);
            injAxis ??= ReadInjectionLabels(engine, source, centres[0], columnSpacing, rowCentres, rowSpacing);

            return new Result(rpmAxis, injAxis, values.ToArray());
        }

        // ---------------- Axis labels ----------------

        /// <summary>One label per column, in the row above the first value row. Null when there is no room for it.</summary>
        private static double?[]? ReadRpmLabels(TesseractEngine engine, Bitmap source, double[] centres, double columnSpacing,
            double firstRowY, double rowSpacing)
        {
            // The header may be cut at the top edge of the screenshot (Capture2.PNG): the crop is clipped,
            // which is fine as long as the label's centre is on the image.
            double y = firstRowY - rowSpacing;
            if (y < 0)
                return null;

            var result = centres
                .Select(x => ParseCell(ReadLabel(engine, source, new RectangleF(
                    (float)(x - 0.375 * columnSpacing), (float)(y - 0.4 * rowSpacing),
                    (float)(0.75 * columnSpacing), (float)(0.8 * rowSpacing)))))
                .ToArray();

            return result.Any(v => v is not null) ? result : null;
        }

        /// <summary>One label per row, from the left edge of the image to the first value column.</summary>
        private static double?[]? ReadInjectionLabels(TesseractEngine engine, Bitmap source, double firstColumnX, double columnSpacing,
            double[] rowCentres, double rowSpacing)
        {
            double right = firstColumnX - 0.55 * columnSpacing;
            if (right < 10)
                return null;

            var result = rowCentres
                .Select(y => ParseLabel(ReadLabel(engine, source, new RectangleF(
                    0, (float)(y - 0.45 * rowSpacing), (float)right, (float)(0.9 * rowSpacing)))))
                .ToArray();

            return result.Any(v => v is not null) ? result : null;
        }

        /// <summary>
        /// Reads one label. Tries light text on a dark background first (inverted), then dark text on light.
        /// </summary>
        private static string ReadLabel(TesseractEngine engine, Bitmap source, RectangleF area)
        {
            var crop = Rectangle.Intersect(Rectangle.Round(area), new Rectangle(0, 0, source.Width, source.Height));
            if (crop.Width < 4 || crop.Height < 4)
                return "";

            foreach (var grey in new[] { InvertedDarkestChannel, BrightestChannel })
            {
                using var image = Prepare(source, crop, LabelScale, LabelPadding, grey);
                using var pix = ToPix(image);
                using var page = engine.Process(pix, PageSegMode.SingleLine);

                var text = page.GetText().Trim();
                if (text.Any(char.IsDigit))
                    return text;
            }

            return "";
        }

        // ---------------- Cell values ----------------

        private static List<Word> ReadWords(TesseractEngine engine, Bitmap source)
        {
            using var image = Prepare(source, new Rectangle(0, 0, source.Width, source.Height), TableScale, 0, BrightestChannel);
            using var pix = ToPix(image);
            using var page = engine.Process(pix, PageSegMode.SparseText);
            using var iterator = page.GetIterator();

            var words = new List<Word>();
            iterator.Begin();

            do
            {
                var text = iterator.GetText(PageIteratorLevel.Word)?.Trim();
                if (string.IsNullOrEmpty(text) || !text.Any(char.IsDigit))
                    continue;

                if (!iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var box))
                    continue;

                // Back to source image pixels.
                words.Add(new Word(text,
                    (box.X1 + box.X2) / 2.0 / TableScale,
                    (box.Y1 + box.Y2) / 2.0 / TableScale,
                    (double)box.Height / TableScale));
            }
            while (iterator.Next(PageIteratorLevel.Word));

            return words;
        }

        /// <summary>Groups words into text lines by their vertical centre, top to bottom, each sorted left to right.</summary>
        private static List<List<Word>> GroupIntoLines(List<Word> words)
        {
            var lines = new List<List<Word>>();
            if (words.Count == 0)
                return lines;

            double tolerance = 0.6 * words.Select(w => w.Height).Order().ElementAt(words.Count / 2);

            foreach (var w in words.OrderBy(w => w.Y))
            {
                var last = lines.LastOrDefault();
                if (last is not null && w.Y - last.Average(x => x.Y) <= tolerance)
                    last.Add(w);
                else
                    lines.Add([w]);
            }

            foreach (var line in lines)
                line.Sort((a, b) => a.X.CompareTo(b.X));

            return lines;
        }

        private static bool IsRpmHeader(List<Word> line)
        {
            if (line.Count < 3)
                return false;

            var numbers = line.Select(w => ParseCell(w.Text)).ToArray();
            if (numbers.Any(n => n is null))
                return false;

            for (int i = 1; i < numbers.Length; i++)
                if (numbers[i] <= numbers[i - 1])
                    return false;

            return numbers[^1] >= 1000;
        }

        private static int Nearest(double[] centres, double x)
        {
            int best = 0;
            for (int i = 1; i < centres.Length; i++)
                if (Math.Abs(centres[i] - x) < Math.Abs(centres[best] - x))
                    best = i;
            return best;
        }

        private static int DigitCount(string text) => text.Count(char.IsDigit);

        /// <summary>Whole number; dots read by mistake are dropped.</summary>
        private static double? ParseCell(string text)
        {
            var digits = new string(text.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int v) ? v : null;
        }

        /// <summary>
        /// Injection time label such as "1.90" or "10.80". When the dot was not read ("190"),
        /// the last two digits are taken as decimals, as the ECU shows two decimals.
        /// </summary>
        private static double? ParseLabel(string text)
        {
            text = text.Trim().TrimEnd('.');

            if (text.Contains('.') &&
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double withDot))
                return withDot;

            return ParseCell(text) is double v ? v / 100 : null;
        }

        // ---------------- Image preparation ----------------

        /// <summary>
        /// Cuts <paramref name="area"/> out of the source, enlarges it, converts it to grey with
        /// <paramref name="grey"/> and adds a white margin (Tesseract reads text at the edge poorly).
        /// </summary>
        private static Bitmap Prepare(Bitmap source, Rectangle area, int scale, int padding, GreyLevel grey)
        {
            var result = new Bitmap(area.Width * scale + 2 * padding, area.Height * scale + 2 * padding, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, new Rectangle(padding, padding, area.Width * scale, area.Height * scale), area, GraphicsUnit.Pixel);
            }

            var data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                var pixels = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                for (int y = 0; y < data.Height; y++)
                {
                    int rowStart = y * data.Stride;
                    bool marginRow = y < padding || y >= data.Height - padding;

                    for (int x = 0; x < data.Width; x++)
                    {
                        int i = rowStart + x * 4; // B, G, R, A
                        byte v = marginRow || x < padding || x >= data.Width - padding
                            ? (byte)255
                            : grey(pixels[i + 2], pixels[i + 1], pixels[i]);

                        pixels[i] = pixels[i + 1] = pixels[i + 2] = v;
                        pixels[i + 3] = 255;
                    }
                }

                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                result.UnlockBits(data);
            }

            return result;
        }

        private static Pix ToPix(Bitmap image)
        {
            using var png = new MemoryStream();
            image.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            return Pix.LoadFromMemory(png.ToArray());
        }
    }
}
