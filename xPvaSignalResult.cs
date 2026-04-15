namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaSignalResult
    {
        public readonly SignalPhase Phase;
        public readonly double Score;
        public readonly string Reason;

        public xPvaSignalResult(SignalPhase phase, double score, string reason)
        {
            Phase = phase;
            Score = score;
            Reason = reason ?? string.Empty;
        }
    }
}