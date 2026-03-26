
namespace LPGDataAnalyzer.Controls
{
    public class ColorHelper
    {
        public static readonly Color Blue = Color.FromArgb(100, 140, 255);
        public static readonly Color White = Color.White;
        public static readonly Color Red = Color.FromArgb(255, 120, 120);

        public static readonly Font BoldFont = new("Segoe UI", 9, FontStyle.Bold);
        public static readonly Color DarkBackColor = Color.FromArgb(45, 45, 48);
        public static readonly Font TitleFontBold = new("Segoe UI", 16, FontStyle.Bold);
        public static Color InterpolateDiverging(double value)
        {
            value = Math.Max(-1, Math.Min(1, value));

            if (value < 0)
                return Blend(Blue, White, value + 1);
            else
                return Blend(White, Red, value);
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
