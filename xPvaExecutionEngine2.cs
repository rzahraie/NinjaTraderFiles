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
		    int oppositePressureBars,
		    bool shockReversalArmed,
		    string shockReason)
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
				    // HARD REVERSAL (existing)
				    if (sig.Phase == SignalPhase.ShortValid)
				        return new xPvaExecutionResult(ExecutionIntent.ReverseToShort, "reverse_to_short_valid");
				
				    // EARLY REVERSAL (NEW — replaces shock)
				    if (sig.Phase == SignalPhase.ShortCandidate
				        && sig.Score >= 0.40
				        && degradingBars >= 1)
				        return new xPvaExecutionResult(ExecutionIntent.ReverseToShort, "reverse_to_short_candidate_early");
				
				    // OPTIONAL: EXIT WITHOUT REVERSAL
				    if (degradingBars >= maxNoneBarsInPosition)
				        return new xPvaExecutionResult(ExecutionIntent.ExitLong, "long_decay_exit");
				
				    return new xPvaExecutionResult(ExecutionIntent.HoldLong, "hold_long");

                case -1:
				    // HARD REVERSAL
				    if (sig.Phase == SignalPhase.LongValid)
				        return new xPvaExecutionResult(ExecutionIntent.ReverseToLong, "reverse_to_long_valid");
				
				    // EARLY REVERSAL (mirror)
				    if (sig.Phase == SignalPhase.LongCandidate
				        && sig.Score >= 0.40
				        && degradingBars >= 1)
				        return new xPvaExecutionResult(ExecutionIntent.ReverseToLong, "reverse_to_long_candidate_early");
				
				    // OPTIONAL EXIT
				    if (degradingBars >= maxNoneBarsInPosition)
				        return new xPvaExecutionResult(ExecutionIntent.ExitShort, "short_decay_exit");
				
				    return new xPvaExecutionResult(ExecutionIntent.HoldShort, "hold_short");

                default:
                    return new xPvaExecutionResult(ExecutionIntent.None, "bad_position_state");
            }
        }
    }
}


