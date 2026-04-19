namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaExecutionEngine2
    {
        public xPvaExecutionResult Compute(
            int currentPosition,
            in xPvaSignalResult sig,
            int degradingBars,
            int maxNoneBarsInPosition,
            bool enableOppositePressureOverride,
            bool oppositePressureArmed,
            int oppositePressureBars)
        {
            switch (currentPosition)
            {
                case 0:
                    if (sig.Phase == SignalPhase.LongValid)
                        return new xPvaExecutionResult(ExecutionIntent.EnterLong, "enter_long_valid");

                    if (sig.Phase == SignalPhase.ShortValid)
                        return new xPvaExecutionResult(ExecutionIntent.EnterShort, "enter_short_valid");

                    return new xPvaExecutionResult(ExecutionIntent.StandAside, "flat_no_valid_signal");

                case 1:
                    if (sig.Phase == SignalPhase.ShortValid)
                        return new xPvaExecutionResult(ExecutionIntent.ReverseToShort, "reverse_to_short_opposite_valid");

                    if (enableOppositePressureOverride && oppositePressureArmed && oppositePressureBars >= 2)
                        return new xPvaExecutionResult(ExecutionIntent.ExitLong, "long_opposite_override_exit");

                    if (degradingBars >= maxNoneBarsInPosition)
                        return new xPvaExecutionResult(ExecutionIntent.ExitLong, "long_decay_exit");

                    return new xPvaExecutionResult(ExecutionIntent.HoldLong, "hold_long");

                case -1:
                    if (sig.Phase == SignalPhase.LongValid)
                        return new xPvaExecutionResult(ExecutionIntent.ReverseToLong, "reverse_to_long_opposite_valid");

                    if (enableOppositePressureOverride && oppositePressureArmed && oppositePressureBars >= 2)
                        return new xPvaExecutionResult(ExecutionIntent.ExitShort, "short_opposite_override_exit");

                    if (degradingBars >= maxNoneBarsInPosition)
                        return new xPvaExecutionResult(ExecutionIntent.ExitShort, "short_decay_exit");

                    return new xPvaExecutionResult(ExecutionIntent.HoldShort, "hold_short");

                default:
                    return new xPvaExecutionResult(ExecutionIntent.None, "bad_position_state");
            }
        }
    }
}
