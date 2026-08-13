namespace LPGDataAnalyzer
{
    public class Helper
    {
        public static double PercentageChange(double baseValue, double newValue)
        {
            return ((newValue - baseValue) / baseValue) * 100;
        }

        public static List<string> GetCheckedValues(CheckedListBox list)
        {
            return list.CheckedItems.Cast<object>()
                .Select(x => x.ToString())
                .ToList();
        }
    }
}
