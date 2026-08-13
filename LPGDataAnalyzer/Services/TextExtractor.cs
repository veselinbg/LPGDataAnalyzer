using LPGDataAnalyzer.Models;
using System.Text.RegularExpressions;
using Tesseract;

namespace LPGDataAnalyzer.Services
{
    public class TextExtractor
    {
        public string Parcer(string imagePath)
        {
            using var engine = new TesseractEngine("tessdata", "eng", EngineMode.Default);

            engine.SetVariable("tessedit_char_whitelist", "0123456789");
            engine.SetVariable("load_system_dawg", "0");
            engine.SetVariable("load_freq_dawg", "0");

            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            string text = page.GetText();

            // Normalize line endings
            text = text.Replace("\r\n", "\n")
                       .Replace("\r", "\n");

            // Remove spaces around newlines and collapse multiple blank lines
            text = Regex.Replace(text, @"[ \t]*\n[ \t]*", Environment.NewLine);
            text = Regex.Replace(text, @"(\r?\n){2,}", Environment.NewLine);

            return text.Trim();
        }
        public void Validate(string text)
        {
            var data = text.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in data)
            {
                SplitToThreeDigitInts(item);
            }
        }
        public double?[,] BuildFinalTable(string text)
        {
            var data = text.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var items = new List<int>();

            foreach (var item in data)
            {
                items.AddRange(SplitToThreeDigitInts(item));
            }

            var fuelCellTable = BuildTable(items);

            return fuelCellTable;
        }
        public static double?[,] BuildTable(IList<int> values)
        {
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            var table = new double?[rpmLength, injLength];

            int index = 0;
            for (int inj = 0; inj < injLength; inj++)
            {
                for (int rpm = 0; rpm < rpmLength; rpm++)
                {
                    table[rpm, inj] = index < values.Count ? values[index++] : (double?)null;
                }
            }

            return table;
        }
        public static string ConvertColumnsToRows(string input)
        {
            // Split rows by "new row"
            var rows = input
                .Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToArray();

            // Each number is 3 digits
            int cols = rows[0].Length / 3;

            // Build matrix
            string[,] matrix = new string[rows.Length, cols];

            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    matrix[r, c] = rows[r].Substring(c * 3, 3);
                }
            }

            // Read column by column
            var result = "";

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows.Length; r++)
                {
                    result += matrix[r, c];
                }

                result += Environment.NewLine;
            }

            return result.TrimEnd();
        }
        private static int[] SplitToThreeDigitInts(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty");

            if (input.Length % 3 != 0)
                throw new ArgumentException($"Input '{input}' length must be divisible by 3");

            int count = input.Length / 3;
            var result = new int[count];

            for (int i = 0; i < count; i++)
            {
                string chunk = input.Substring(i * 3, 3);
                result[i] = int.Parse(chunk);
                if (result[i] > 300)
                    result[i] -= 200;
                if (result[i] > 200)
                    result[i] -= 100;
                if (result[i] > 201)
                    throw new ArgumentException($"Input '{input}' has invalid number {result[i]}");
            }

            return result;
        }
    }
}
