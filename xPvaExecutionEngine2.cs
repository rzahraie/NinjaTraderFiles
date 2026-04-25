namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaExecutionEngine2
    {
        public xPvaExecutionResult Compute(
			    int currentPosition,
			    in xPvaSignalResult sig,
			    xPvaContainer cnt,
			    int degradingBars,
			    int maxNoneBarsInPosition,
			    bool enableOppositePressureOverride,
			    bool oppositePressureArmed,
			    int oppositePressureBars,
			    bool shockReversalArmed,
			    string shockReason)
        {
			bool containerAllowsLong =
			    cnt != null &&
			    cnt.Direction == xPvaContainerDirection.Up &&
			    cnt.State == xPvaContainerState.SeekingP2 &&
			    cnt.HasP2 &&
			    !cnt.HasP3;
			
			bool containerAllowsShort =
			    cnt != null &&
			    cnt.Direction == xPvaContainerDirection.Down &&
			    (cnt.State == xPvaContainerState.SeekingP2 ||
			     cnt.State == xPvaContainerState.SeekingP3);
			
            switch (currentPosition)
            {
                case 0:
                    if (sig.Phase == SignalPhase.LongValid && sig.Score >= 0.55)
					{
					    if (!containerAllowsLong)
					        return new xPvaExecutionResult(
					            ExecutionIntent.StandAside,
					            $"blocked_long_by_container {xPvaContainerEngine.Format(cnt)}");
					
					    return new xPvaExecutionResult(ExecutionIntent.EnterLong, "enter_long_valid");
					}
					
					// if (sig.Phase == SignalPhase.ShortValid && sig.Score >= 0.55)
					//     return new xPvaExecutionResult(ExecutionIntent.EnterShort, "enter_short_valid");

                    return new xPvaExecutionResult(ExecutionIntent.StandAside, "flat_no_valid_signal");

                case 1:
				{
				    bool shortShockConfirmed =
					    shockReversalArmed &&
					    sig.Phase == SignalPhase.ShortValid;
					
					if (shortShockConfirmed)
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToShort,
					        $"reverse_to_short_shock_confirmed phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} shock={shockReason} oppBars={oppositePressureBars}");
				
				    /*if (enableOppositePressureOverride &&
				        oppositePressureArmed &&
				        oppositePressureBars >= 4 &&
				        degradingBars >= 1)
				    {
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToShort,
				            $"reverse_to_short_opposite_pressure oppBars={oppositePressureBars} deg={degradingBars}");
				    }*/
				
				    bool earlyShortCandidate =
				        sig.Phase == SignalPhase.ShortCandidate &&
				        sig.Score >= 0.40 &&
				        degradingBars >= 2;
					
				  bool controlledEarlyShort =
					    sig.Phase == SignalPhase.ShortCandidate &&
					    sig.Score >= 0.60 &&
					    degradingBars >= 3;
				
				    if (sig.Phase == SignalPhase.ShortValid)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToShort,
				            $"reverse_to_short_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
					
					if ((controlledEarlyShort || earlyShortCandidate) && !containerAllowsShort)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.HoldLong,
					        $"blocked_reverse_to_short_by_container {xPvaContainerEngine.Format(cnt)}");
					}

					if (controlledEarlyShort)
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToShort,
					        $"reverse_to_short_candidate_controlled phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} oppBars={oppositePressureBars}");
				
				    if (earlyShortCandidate)
				        return new xPvaExecutionResult(
				            ExecutionIntent.ReverseToShort,
				            $"reverse_to_short_candidate_early phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
					
					bool longStructureBroken =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.HasP3 &&
					    cnt.State != xPvaContainerState.Completed &&
					    sig.Phase != SignalPhase.LongValid &&
					    sig.Phase != SignalPhase.LongCandidate &&
					    cnt.P3Price > 0.0 &&
					    sig.Score <= 0.0;
					
					if (longStructureBroken)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_structural_exit_p3_break phase={sig.Phase} score={sig.Score:F2} {xPvaContainerEngine.Format(cnt)}");
					}

					
				    int longDecayExitBars = System.Math.Max(1, maxNoneBarsInPosition - 1);

					if (degradingBars >= longDecayExitBars)
					{
					    bool containerStillSupportsLong =
					        cnt != null &&
					        cnt.Direction == xPvaContainerDirection.Up &&
					        (cnt.State == xPvaContainerState.SeekingP2 ||
					         cnt.State == xPvaContainerState.SeekingP3);
					
					    int maxContainerGraceBars = 2;
					    int graceLimit = longDecayExitBars + maxContainerGraceBars;
					
					    if (containerStillSupportsLong && degradingBars <= graceLimit)
					        return new xPvaExecutionResult(
					            ExecutionIntent.HoldLong,
					            $"hold_long_decay_grace_container_supports phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} exitTh={longDecayExitBars} graceLimit={graceLimit} {xPvaContainerEngine.Format(cnt)}");
					
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_decay_exit phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} exitTh={longDecayExitBars} graceLimit={graceLimit} containerSupport={containerStillSupportsLong} earlySC={earlyShortCandidate}");
					}
				
				    return new xPvaExecutionResult(
				        ExecutionIntent.HoldLong,
				        $"hold_long phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
				}
				
				case -1:
				{
					if (shockReversalArmed)
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToLong,
					        $"reverse_to_long_shock shock={shockReason} oppBars={oppositePressureBars}");
					
					if (enableOppositePressureOverride &&
					    oppositePressureArmed &&
					    oppositePressureBars >= 3 &&
					    degradingBars >= 1)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToLong,
					        $"reverse_to_long_opposite_pressure oppBars={oppositePressureBars} deg={degradingBars}");
					}

				    bool earlyLongCandidate =
				        sig.Phase == SignalPhase.LongCandidate
				        && sig.Score >= 0.40
				        && degradingBars >= 1;
				
				    if (sig.Phase == SignalPhase.LongValid)
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToLong,
					        $"reverse_to_long_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlyLC={earlyLongCandidate}");
					
					if (earlyLongCandidate && !containerAllowsLong)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.HoldShort,
					        $"blocked_reverse_to_long_by_container {xPvaContainerEngine.Format(cnt)}");
					}
					
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























