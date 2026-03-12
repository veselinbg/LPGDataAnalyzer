using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Controls
{
    internal class ColorHelper
    {
        public static Color InterpolateDiverging(double value)
        {
            value = Math.Max(-1, Math.Min(1, value));

            Color blue = Color.FromArgb(100, 140, 255);
            Color white = Color.White;
            Color red = Color.FromArgb(255, 120, 120);

            if (value < 0)
                return Blend(blue, white, value + 1);
            else
                return Blend(white, red, value);
        }
        private static Color Blend(Color c1, Color c2, double t)
        {
            t = Math.Max(0, Math.Min(1, t));

            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);

            return Color.FromArgb(r, g, b);
        }
    }
}
