namespace LPGDataAnalyzer.Controls
{
    /// <summary>
    /// Many screens were laid out with fixed pixel sizes. At Windows scaling above 100% the font grows
    /// but those sizes don't, so button text gets cut. This makes buttons grow to fit their text.
    /// </summary>
    public static class ControlSizing
    {
        /// <summary>
        /// Lets every button under <paramref name="root"/> grow (never shrink) to fit its text,
        /// and be at least one text line plus padding tall. Call when the form loads.
        /// </summary>
        public static void FitButtons(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child is ButtonBase button && !(button is CheckBox { Appearance: not Appearance.Button }) && button is not RadioButton)
                {
                    int minHeight = TextRenderer.MeasureText("Ag", button.Font).Height + 10;
                    button.MinimumSize = new Size(Math.Max(button.MinimumSize.Width, minHeight), Math.Max(button.MinimumSize.Height, minHeight));

                    if (!button.AutoSize)
                    {
                        if (button is Button b)
                            b.AutoSizeMode = AutoSizeMode.GrowOnly;
                        button.AutoSize = true;
                    }
                }

                FitButtons(child);
            }
        }
    }
}
