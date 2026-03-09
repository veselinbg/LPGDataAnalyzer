using LPGDataAnalyzer.Models;
using Tesseract;

namespace LPGDataAnalyzer.Services
{
    internal class TextExtractor
    {
        public string Parcer(string imagePath)
        {
            using (var engine = new TesseractEngine("tessdata", "eng", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "0123456789");
                engine.SetVariable("load_system_dawg", "0");
                engine.SetVariable("load_freq_dawg", "0");

                using var img = Pix.LoadFromFile(imagePath);
                using (var page = engine.Process(img))
                {
                    string text = page.GetText();

                    return text.Replace("\n\n", Environment.NewLine).Replace("\n","").Trim();
                }
            }
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
        internal static double?[,] BuildTable(IList<int> values)
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
            }

            return result;
        }
    }
}
