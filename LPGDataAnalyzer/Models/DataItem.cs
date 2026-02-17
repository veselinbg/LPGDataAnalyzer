namespace LPGDataAnalyzer.Models
{
    internal class DataItem
    {
        public int TEMPO { get; set; }
        public int RPM { get; set; }
        public double LAMBDA_b1 { get; set; }
        public double GAS_b1 { get; set; }
        public double BENZ_b1 { get; set; }
        public double PRESS { get; set; }
        public double MAP { get; set; }
        public double Temp_RID { get; set; }
        public double Temp_GAS { get; set; }
        public double LIV { get; set; }
        public double SLOW_b1 { get; set; }
        public double FAST_b1 { get; set; }
        public double OX_b1 { get; set; }
        public double LAMBDA_b2 { get; set; }
        public double GAS_b2 { get; set; }
        public double BENZ_b2 { get; set; }
        public double SLOW_b2 { get; set; }
        public double FAST_b2 { get; set; }
        public double OX_b2 { get; set; }
        public int MARKER { get; set; }
        public int AUTOMARKER { get; set; }
        public int ECUMARKER { get; set; }

        // Calculated fields
        public double Ratio_b1 { get; set; }
        public double Ratio_b2 { get; set; }
        public double RatioDifference { get; set; }
    }
}
