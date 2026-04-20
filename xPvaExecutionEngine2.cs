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
				{
				    bool earlyShortCandidate =
				        sig.Phase == SignalPhase.ShortCandidate
				        && sig.Score >= 0.40
				        && degradingBars >= 1;
				
				    if (sig.Phase == SignalPhase.ShortValid)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToShort,
				            $"reverse_to_short_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
				
				    if (earlyShortCandidate)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToShort,
				            $"reverse_to_short_candidate_early phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
				
				    if (degradingBars >= maxNoneBarsInPosition)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ExitLong,
				            $"long_decay_exit phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
				
				    return new xPvaExecutionResult(
				        ExecutionIntent.HoldLong,
				        $"hold_long phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
				}
				
				case -1:
				{
				    bool earlyLongCandidate =
				        sig.Phase == SignalPhase.LongCandidate
				        && sig.Score >= 0.40
				        && degradingBars >= 1;
				
				    if (sig.Phase == SignalPhase.LongValid)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToLong,
				            $"reverse_to_long_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlyLC={earlyLongCandidate}");
				
				    if (earlyLongCandidate)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToLong,
				            $"reverse_to_long_candidate_early phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlyLC={earlyLongCandidate}");
				
				    if (degradingBars >= maxNoneBarsInPosition)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ExitShort,
				            $"short_decay_exit phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlyLC={earlyLongCandidate}");
				
				    return new xPvaExecutionResult(
				        ExecutionIntent.HoldShort,
				        $"hold_short phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlyLC={earlyLongCandidate}");
				}

                default:
                    return new xPvaExecutionResult(ExecutionIntent.None, "bad_position_state");
            }
        }
    }
}



