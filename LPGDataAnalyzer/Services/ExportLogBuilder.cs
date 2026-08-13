using LPGDataAnalyzer.Models;
using System.Globalization;
using System.Text;

namespace LPGDataAnalyzer.Services
{
    public class ExportLogBuilder
    {
        private const int ColumnCount = 22;

        public static void Build(string path, IEnumerable<DataItem> items)
        {
            using var writer = new StreamWriter(path, false, Encoding.UTF8);

            // Header
            writer.WriteLine($"Ver. 4 8 real time data, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine("TEMPO\tGIRI\tLAMBDA\tGAS\tBENZ\tPRESS\tMAP\tT.RID\tT.GAS\tLIV\tSLOW\tFAST\tOX\tLAMBDA2\tGAS2\tBENZ2\tSLOW2\tFAST2\tOX2\tMARKER\tAUTOMARKER\tECUMARKER");

            int tempo = 0;

            foreach (var x in items)
            {
                var cols = Enumerable.Repeat("0", ColumnCount).ToArray();

                cols[0] = tempo++.ToString(CultureInfo.InvariantCulture);
                cols[1] = x.RPM.ToString(CultureInfo.InvariantCulture);

                // BANK 1
                cols[2] = "0"; // LAMBDA
                cols[3] = F(x.GAS_b1);
                cols[4] = F(x.BENZ_b1);

                // COMMON
                cols[5] = F(x.PRESS);
                cols[6] = F(x.MAP);
                cols[7] = F(x.Temp_RID);
                cols[8] = F(x.Temp_GAS);

                cols[9] = "0"; // LIV

                cols[10] = F(x.SLOW_b1);
                cols[11] = F(x.FAST_b1);
                cols[12] = F(x.OX_b1);

                // BANK 2
                cols[13] = "0"; // LAMBDA2
                cols[14] = F(x.GAS_b2);
                cols[15] = F(x.BENZ_b2);

                cols[16] = F(x.SLOW_b2);
                cols[17] = F(x.FAST_b2);
                cols[18] = F(x.OX_b2);

                // markers
                cols[19] = "0";
                cols[20] = "0";
                cols[21] = "0";

                writer.WriteLine(string.Join('\t', cols));
            }
        }

        private static string F(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}