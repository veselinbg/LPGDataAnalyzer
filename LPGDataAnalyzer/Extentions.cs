using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer
{
    internal static class Extentions
    {
        public static double Round(this double value)
        {
            return Math.Round(value, 2); 
        }
    }
}
