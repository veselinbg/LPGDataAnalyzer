namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        public class FuelCellUpdate
        {
            public int Rpm { get; set; }
            public double Inj { get; set; }
            public double AppliedDelta { get; set; }
            public double Confidence { get; set; }
            public bool Propagated { get; set; } = false;
        }
    }
}
