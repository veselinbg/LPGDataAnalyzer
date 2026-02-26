using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        public class FuelCorrectionResult
        {
            public List<FuelCell> UpdatedCells { get; set; } = [];
            public List<FuelCellUpdate> Diagnostics { get; set; } = [];
        }
    }
}
