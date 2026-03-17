using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public static class Filter
    {
        public static bool GasBanks(DataItem item)
        {
            return item.GAS_b1 > 0 && item.GAS_b2 > 0;
        }
        public static bool BenzBanks(DataItem item, double val = 0)
        {
            return item.BENZ_b1 > val && item.BENZ_b2 > val;
        }
        public static double PercentageChange(double baseValue, double newValue)
        {
            return ((newValue - baseValue) / baseValue) * 100;
        }
    }
}
