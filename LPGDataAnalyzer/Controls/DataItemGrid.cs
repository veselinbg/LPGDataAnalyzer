using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Controls
{
    internal class DataItemGrid : EnterpriseGrid<DataItem>
    {
        public DataItemGrid()
        {
            this.Title = "All logged data";
            this.ReadOnly = true;
        }
    }
}