using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal class FuelCorrectionResult
    {
        public List<FuelCell> UpdatedCells { get; set; } = [];
        public List<FuelCellUpdate> Diagnostics { get; set; } = [];
    }
}