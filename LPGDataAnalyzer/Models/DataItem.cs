namespace LPGDataAnalyzer.Models
{
    internal class DataItem
    {
        public int TEMPO { get; set; }
        public int RPM { get; set; }
        //public double LAMBDA_b1 { get; set; }
        public double GAS_b1 { get; set; }
        public double BENZ_b1 { get; set; }
        public double PRESS { get; set; }
        public double MAP { get; set; }
        public double Temp_RID { get; set; }
        public double Temp_GAS { get; set; }
       // public double LIV { get; set; }
        public double SLOW_b1 { get; set; }
        public double FAST_b1 { get; set; }
        public double OX_b1 { get; set; }
        //public double LAMBDA_b2 { get; set; }
        public double GAS_b2 { get; set; }
        public double BENZ_b2 { get; set; }
        public double SLOW_b2 { get; set; }
        public double FAST_b2 { get; set; }
        public double OX_b2 { get; set; }
        //public int MARKER { get; set; }
        //public int AUTOMARKER { get; set; }
        //public int ECUMARKER { get; set; }

        // Calculated fields
        public double Ratio_b1 { get; set; }
        public double Ratio_b2 { get; set; }
        public double RatioDifference { get; set; }
        /// <summary>
        /// Fast Trim sum of two banks /2
        /// </summary>
        public double Fast { get; set; }
        /// <summary>
        /// Slow Trim sum of two banks /2
        /// </summary>
        public double Slow { get; set; }
        /// <summary>
        /// Slow + fast trim for two banks /2
        /// </summary>
        public double Trim{ get; set; }
        public double Trim_b1 { get; set; }
        public double Trim_b2 { get; set; }
        public double AFR { get; set; }
        public double AFR_b1 { get;set;  }
        public double AFR_b2 { get; set; }
        /// <summary>
        /// GAS time sum of two banks /2
        /// </summary>
        public double GAS { get; set; }
        /// <summary>
        /// BENZ time sum of two banks /2
        /// </summary>
        public double BENZ { get; set; }
        /// <summary>
        /// This is difference between Benz_b1 and Benz_b2 in %
        /// </summary>
        public double BENZ_Diff { get; set; }

    }
}
