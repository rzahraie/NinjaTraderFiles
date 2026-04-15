namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaDirectionResult
    {
        public readonly DirectionContext Context;
        public readonly double Score;

        public xPvaDirectionResult(DirectionContext context, double score)
        {
            Context = context;
            Score = score;
        }
    }
}