namespace LPGDataAnalyzer.Models
{
    /// <summary>
    /// Every option of a fuel map prediction (the checkboxes and boxes of <c>PredictionControl</c>),
    /// so a stored prediction can be repeated and its options shown again.
    /// </summary>
    public sealed record PredictionOptions
    {
        public bool OnlyChanges { get; init; }
        public bool ShowOnlyMultiplier { get; init; }
        public bool Round { get; init; } = true;
        public bool ShowOnlyCount { get; init; }
        public bool PressureCorrection { get; init; } = true;
        public bool InterpolationWeights { get; init; } = true;
        public bool TrimDelay { get; init; } = true;

        /// <summary>Minimum % of change ("Min Valid % of change").</summary>
        public double MinChangeValue { get; init; }

        /// <summary>Reducer pressure the trims are scaled to ("Reference Press").</summary>
        public double ReferencePressure { get; init; } = 1.49;

        public int MinCount { get; init; }

        /// <summary>"Max Benz Diff in %".</summary>
        public double MaxBenzDiff { get; init; } = 6;

        /// <summary>Trim delay the logs were grouped with (from the car in LPGDbAnalyzer); null when unknown.</summary>
        public int? TrimDelayMs { get; init; }
    }
}
