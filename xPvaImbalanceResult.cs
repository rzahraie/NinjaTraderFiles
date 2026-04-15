namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaImbalanceResult
    {
        public readonly double Imbalance;
        public readonly double TotalEffort;
        public readonly double DominantEffort;
        public readonly double NonDominantEffort;

        public xPvaImbalanceResult(
            double imbalance,
            double totalEffort,
            double dominantEffort,
            double nonDominantEffort)
        {
            Imbalance = imbalance;
            TotalEffort = totalEffort;
            DominantEffort = dominantEffort;
            NonDominantEffort = nonDominantEffort;
        }
    }
}