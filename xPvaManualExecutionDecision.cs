namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ManualExecutionDecision
    {
        public readonly string Action;
        public readonly string Reason;

        public ManualExecutionDecision(string action, string reason)
        {
            Action = action;
            Reason = reason;
        }
    }
}