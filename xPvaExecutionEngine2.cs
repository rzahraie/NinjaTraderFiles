namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaExecutionEngine2
    {
        public xPvaExecutionResult Compute(
            int currentPosition,
            in xPvaSignalResult sig,
            int degradingBars,
            int maxNoneBarsInPosition)
        {
            switch (currentPosition)
            {
                case 0:
                    if (sig.Phase == SignalPhase.LongValid)
                        return new xPvaExecutionResult(ExecutionIntent.EnterLong, sig.Reason);

                    if (sig.Phase == SignalPhase.ShortValid)
                        return new xPvaExecutionResult(ExecutionIntent.EnterShort, sig.Reason);

                    return new xPvaExecutionResult(ExecutionIntent.StandAside, sig.Reason);

                case 1:
                    if (sig.Phase == SignalPhase.ShortValid)
                        return new xPvaExecutionResult(ExecutionIntent.ReverseToShort, sig.Reason);

                    if (degradingBars >= maxNoneBarsInPosition)
                        return new xPvaExecutionResult(ExecutionIntent.ExitLong, "long_decay_exit");

                    return new xPvaExecutionResult(ExecutionIntent.HoldLong, sig.Reason);

                case -1:
                    if (sig.Phase == SignalPhase.LongValid)
                        return new xPvaExecutionResult(ExecutionIntent.ReverseToLong, sig.Reason);

                    if (degradingBars >= maxNoneBarsInPosition)
                        return new xPvaExecutionResult(ExecutionIntent.ExitShort, "short_decay_exit");

                    return new xPvaExecutionResult(ExecutionIntent.HoldShort, sig.Reason);

                default:
                    return new xPvaExecutionResult(ExecutionIntent.None, "bad_position_state");
            }
        }
    }
}
