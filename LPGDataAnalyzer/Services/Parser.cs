using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal class Parser()
    {
        const int ExpectedColumns = 22;
        public DataItem[] Data { get; protected set; } = [];

        public virtual void Load(string _datapath)
        {
            Data = [..File.ReadLines(_datapath)
            .Skip(2) // skip header row and file data info
            .Where(line => !string.IsNullOrWhiteSpace(line) )
            .Select(ParseLine)
            //remove data when the engine is workin on petrol 
            .Where(x => x.GAS_b1 > 0 && x.GAS_b2 > 0 && x.Ratio_b1 > 0 && x.Ratio_b2 > 0)];
        }
        private static DataItem ParseLine(string line)
        {
            Span<Range> ranges = stackalloc Range[32];
            var span = line.AsSpan();

            int count = span.Split(ranges, '\t');

            if (count < ExpectedColumns)
                return new DataItem();

            var item = new DataItem
            {
                TEMPO = span[ranges[0]].ToInt(),
                RPM = span[ranges[1]].ToInt(),
                //LAMBDA_b1 = span[ranges[2]].ToDouble(),
                GAS_b1 = span[ranges[3]].ToDouble(),
                BENZ_b1 = span[ranges[4]].ToDouble(),
                PRESS = span[ranges[5]].ToDouble(),
                MAP = span[ranges[6]].ToDouble(),
                Temp_RID = span[ranges[7]].ToDouble(),
                Temp_GAS = span[ranges[8]].ToDouble(),
                //LIV = span[ranges[9]].ToDouble(),
                SLOW_b1 = span[ranges[10]].ToDouble(),
                FAST_b1 = span[ranges[11]].ToDouble(),
                OX_b1 = span[ranges[12]].ToDouble(),
                //LAMBDA_b2 = span[ranges[13]].ToDouble(),
                GAS_b2 = span[ranges[14]].ToDouble(),
                BENZ_b2 = span[ranges[15]].ToDouble(),
                SLOW_b2 = span[ranges[16]].ToDouble(),
                FAST_b2 = span[ranges[17]].ToDouble(),
                OX_b2 = span[ranges[18]].ToDouble(),
                //MARKER = span[ranges[19]].ToInt(),
                //AUTOMARKER = span[ranges[20]].ToInt(),
                //ECUMARKER = span[ranges[21]].ToInt()
            };

            item.Ratio_b1 = item.BENZ_b1 != 0 ? (item.GAS_b1 / item.BENZ_b1).Round() : 0;

            item.Ratio_b2 = item.BENZ_b2 != 0 ? (item.GAS_b2 / item.BENZ_b2).Round() : 0;

            item.RatioDifference = (item.Ratio_b1 - item.Ratio_b2).Round(1);

            item.Fast = (item.FAST_b1 + item.FAST_b2)/2;
            item.Slow = (item.SLOW_b1 + item.SLOW_b2)/2;
            item.Trim = (item.Slow + item.Fast)/2;
            item.Trim_b1 = (item.SLOW_b1 + item.FAST_b1) / 2;
            item.Trim_b2 = (item.SLOW_b2 + item.FAST_b2) / 2;

            item.AFR_b1 = (15.6 / ((1 + item.FAST_b1 / 100) * (1 + item.SLOW_b1 / 100)));
            item.AFR_b2 = (15.6 / ((1 + item.FAST_b2 / 100) * (1 + item.SLOW_b2 / 100)));
            item.AFR = (15.6 / ((1 + item.Fast / 100) * (1 + item.Slow / 100)));

            item.GAS = (item.GAS_b1 + item.GAS_b2) / 2;
            item.BENZ = (item.BENZ_b1 + item.BENZ_b2) / 2;

            item.BENZ_Diff = item.BENZ_b1.RelDiff(item.BENZ_b2);

            return item;
        }
        void openAllSavedFilesAndparsethem()
        {
            //open all saved files and parse the and use the data. 
            List<string> txtFiles = new List<string>();
            var directoryPath = "C:\\Users\\veselin.ivanov\\Documents\\MultipointInj\\Acquisition";
            try
            {
                // Check if the directory exists
                if (Directory.Exists(directoryPath))
                {
                    // Get all .txt files in the directory (including subdirectories)
                    string[] files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        txtFiles.Add(file); // Add file path to the list
                    }
                }
                else
                {
                    Console.WriteLine("The directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            (string, double)[] result = new (string, double)[txtFiles.Count()];
            int i = 0;
            foreach (var file in txtFiles)
            {
                var p = new Parser();
                p.Load(file);

                var res1 = ExtraInjectionCalculator.CalculateExtraInjectionTime(p.Data.ToList());
                result[i].Item1 = file;
                result[i++].Item2 = res1;
            }
        }
    }
}
