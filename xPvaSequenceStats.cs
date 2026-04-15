namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaSequenceStats
    {
        public readonly int PolarityRunLength;
        public readonly int DominanceRunLength;
        public readonly int FlipCount;
        public readonly int MaxDominantRun;
        public readonly int MaxNonDominantRun;

        public xPvaSequenceStats(
            int polarityRunLength,
            int dominanceRunLength,
            int flipCount,
            int maxDominantRun,
            int maxNonDominantRun)
        {
            PolarityRunLength = polarityRunLength;
            DominanceRunLength = dominanceRunLength;
            FlipCount = flipCount;
            MaxDominantRun = maxDominantRun;
            MaxNonDominantRun = maxNonDominantRun;
        }
    }
}