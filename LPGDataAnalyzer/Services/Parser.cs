using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal class Parser()
    {
        public ICollection<DataItem> Data { get; protected set; } = [];

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
            string[] f = line.Split('\t', StringSplitOptions.None);
            
            if (f.Length < 22) return new DataItem();

            var lpgDataLine = new DataItem
            {
                TEMPO = f[0].ToInt(),
                RPM = f[1].ToInt(),
                LAMBDA_b1 = f[2].ToDouble(),
                GAS_b1 = f[3].ToDouble(),
                BENZ_b1 = f[4].ToDouble(),
                PRESS = f[5].ToDouble(),
                MAP = f[6].ToDouble(),
                Temp_RID = f[7].ToDouble(),
                Temp_GAS = f[8].ToDouble(),
                LIV = f[9].ToDouble(),
                SLOW_b1 = f[10].ToDouble(),
                FAST_b1 = f[11].ToDouble(),
                OX_b1 = f[12].ToDouble(),
                LAMBDA_b2 = f[13].ToDouble(),
                GAS_b2 = f[14].ToDouble(),
                BENZ_b2 = f[15].ToDouble(),
                SLOW_b2 = f[16].ToDouble(),
                FAST_b2 = f[17].ToDouble(),
                OX_b2 = f[18].ToDouble(),

                MARKER = f[19].ToInt(),
                AUTOMARKER = f[20].ToInt(),
                ECUMARKER = f[21].ToInt()
            };

            lpgDataLine.Ratio_b1 = Math.Round(lpgDataLine.BENZ_b1 != 0 ? lpgDataLine.GAS_b1 / lpgDataLine.BENZ_b1 : 0, 2, MidpointRounding.AwayFromZero);

            lpgDataLine.Ratio_b2 = Math.Round(lpgDataLine.BENZ_b2 != 0 ? lpgDataLine.GAS_b2 / lpgDataLine.BENZ_b2 : 0, 2, MidpointRounding.AwayFromZero);

            lpgDataLine.RatioDifference = Math.Round(lpgDataLine.Ratio_b1 - lpgDataLine.Ratio_b2, 1, MidpointRounding.AwayFromZero);

            return lpgDataLine;
        }
    }
}
