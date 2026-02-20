using LPGDataAnalyzer.Models;
using Tesseract;

namespace LPGDataAnalyzer
{
    internal class TextExtractor
    {
        public string Parcer(string imagePath)
        {
            using (var engine = new TesseractEngine("tessdata", "eng", EngineMode.LstmOnly))
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
        public List<FuelCell> BuildFinalTable(string text)
        {
            var data = text.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var items = new List<int>();

            foreach (var item in data)
            {
                items.AddRange(SplitToThreeDigitInts(item));
            }

            int i = 0;

            var fuelCellTable = FuelCellBuilder.BuildTable();

            foreach (var fuelCell in fuelCellTable)
            {
                fuelCell.Value = items[i++];
            }
            return fuelCellTable;
        }
        private static List<int> SplitToThreeDigitInts(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty");

            if (input.Length % 3 != 0)
            {
                throw new ArgumentException($"Input {input} length must be divisible by 3");
            }

            var result = new List<int>();

            for (int i = 0; i < input.Length; i += 3)
            {
                string chunk = input.Substring(i, 3);
                result.Add(int.Parse(chunk));
            }

            return result;
        }
    }
}
