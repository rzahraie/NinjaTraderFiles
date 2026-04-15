namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaDominanceResult
    {
        public readonly DominanceState State;
        public readonly double Score;

        public xPvaDominanceResult(DominanceState state, double score)
        {
            State = state;
            Score = score;
        }
    }
}