namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaExecutionResult
    {
        public readonly ExecutionIntent Intent;
        public readonly string Reason;

        public xPvaExecutionResult(ExecutionIntent intent, string reason)
        {
            Intent = intent;
            Reason = reason ?? string.Empty;
        }
    }
}