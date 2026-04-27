namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaExecutionEngine2
    {
        public xPvaExecutionResult Compute(
		    int currentPosition,
		    in xPvaSignalResult sig,
		    xPvaContainer cnt,
		    double curClose,
		    double curLow,
		    int degradingBars,
		    int maxNoneBarsInPosition,
		    bool enableOppositePressureOverride,
		    bool oppositePressureArmed,
		    int oppositePressureBars,
		    bool shockReversalArmed,
		    string shockReason,
		    bool recentStrongShortSignal)
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
				cnt.State == xPvaContainerState.SeekingP3 &&
				cnt.HasP3;
			
			int longDecayExitBars = System.Math.Max(1, maxNoneBarsInPosition - 1);
            
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
					
					bool currentShortSignal =
					    sig.Phase == SignalPhase.ShortValid && sig.Score >= 0.55;
					
					bool usablePersistedShortSignal =
					    recentStrongShortSignal &&
					    sig.Phase != SignalPhase.LongValid &&
					    sig.Phase != SignalPhase.LongCandidate;
					
					bool freshContext = degradingBars <= 2;   // key filter

					bool allowEarlyShort =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.State == xPvaContainerState.FttDetected &&
					    cnt.HasP3 &&
					    sig.Score >= 0.65;
					
					bool allowStructuredShort =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.State == xPvaContainerState.SeekingP3 &&
					    cnt.HasP3;
					
					// NEW: structure-driven entry
					// NEW: structure-driven entry
					if (allowStructuredShort)
					{
					    bool validTrigger =
					        (sig.Phase == SignalPhase.ShortValid && sig.Score >= 0.55)
					        || usablePersistedShortSignal;
					
					    if (!validTrigger)
					    {
					        return new xPvaExecutionResult(
					            ExecutionIntent.StandAside,
					            $"blocked_short_no_trigger {xPvaContainerEngine.Format(cnt)}");
					    }
					
					    bool justFormedP3 =
					        cnt != null &&
					        cnt.HasP3 &&
					        cnt.P3Bar == cnt.LastBar;
					
					    if (justFormedP3)
					    {
					        return new xPvaExecutionResult(
					            ExecutionIntent.StandAside,
					            $"wait_short_post_p3_confirmation {xPvaContainerEngine.Format(cnt)}");
					    }
					
					    return new xPvaExecutionResult(
					        ExecutionIntent.EnterShort,
					        "enter_short_structured_persisted");
					}
					
					// early FTT remains unchanged
					if (allowEarlyShort)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.EnterShort,
					        "enter_short_early_ftt");
					}

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
				        degradingBars >= longDecayExitBars;
					
				   bool controlledEarlyShort =
					    sig.Phase == SignalPhase.ShortCandidate &&
					    sig.Score >= 0.60 &&
					    degradingBars >= 3;
				
				    bool protectedStrongLongContext =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    (cnt.State == xPvaContainerState.PostP3 ||
					     cnt.State == xPvaContainerState.FttDetected) &&
					    cnt.HasP3 &&
					    cnt.ImbalanceAtP3 >= 0.25;
					
					// If short signal appears but long context is still strong,
					// EXIT the long but DO NOT reverse unless the short is strong enough.
					if (sig.Phase == SignalPhase.ShortValid)
					{
					    if (protectedStrongLongContext && sig.Score < 0.60)
					    {
					        return new xPvaExecutionResult(
					            ExecutionIntent.ExitLong,
					            $"exit_long_no_reverse strong_long_context shortScore={sig.Score:F2} {xPvaContainerEngine.Format(cnt)}");
					    }
					
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToShort,
					        $"reverse_to_short_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} earlySC={earlyShortCandidate}");
					}
					
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
					
					bool longFttFailure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.State == xPvaContainerState.FttDetected;
					
					if (longFttFailure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_ftt_failure_exit {xPvaContainerEngine.Format(cnt)}");
					}

					bool longImbalanceFailure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.HasP3 &&
					    cnt.ImbalanceAtP3 <= -0.40 &&
					    degradingBars >= 1;
					
					if (longImbalanceFailure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_imbalance_failure_exit imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}

					bool immediateP3Failure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.HasP3 &&
					    cnt.State == xPvaContainerState.SeekingP3 &&
					    cnt.ImbalanceAtP3 <= -0.40;
					
					if (immediateP3Failure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_immediate_p3_failure imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}

					int postLen =
					    cnt.PostP3AttemptEndBar >= cnt.PostP3AttemptStartBar
					        ? cnt.PostP3AttemptEndBar - cnt.PostP3AttemptStartBar + 1
					        : 0;
					
					bool strongPostP3 =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.State == xPvaContainerState.PostP3 &&
					    cnt.ImbalanceAtP3 >= 0.25;
					
					if (strongPostP3)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.HoldLong,
					        $"long_post_p3_strong_hold imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}

					bool weakPostP3 =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Up &&
					    cnt.State == xPvaContainerState.PostP3 &&
					    cnt.PostP3AttemptStartBar >= 0 &&
					    cnt.ImbalanceAtP3 <= -0.10 &&
					    postLen <= 2;
					
					if (weakPostP3)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitLong,
					        $"long_post_p3_weak_exit imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}

					if (degradingBars >= longDecayExitBars)
					{
					    bool containerStillSupportsLong =
					        cnt != null &&
					        cnt.Direction == xPvaContainerDirection.Up &&
					        (cnt.State == xPvaContainerState.SeekingP2 ||
					         cnt.State == xPvaContainerState.SeekingP3);
					
					    int maxContainerGraceBars = 2;
					    int graceLimit = longDecayExitBars + maxContainerGraceBars;
					
					    bool favorableP3Imbalance =
						    cnt != null &&
						    cnt.HasP3 &&
						    cnt.ImbalanceAtP3 >= 0.25;
						
						if (containerStillSupportsLong &&
						    (degradingBars <= graceLimit ||
 							(favorableP3Imbalance && degradingBars <= graceLimit + 2)))
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
				
				   bool protectedStrongShortContext =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    (cnt.State == xPvaContainerState.PostP3 ||
					     cnt.State == xPvaContainerState.FttDetected) &&
					    cnt.HasP3 &&
					    cnt.ImbalanceAtP3 <= -0.25;
					
					if (sig.Phase == SignalPhase.LongValid)
					{
					    if (protectedStrongShortContext && sig.Score < 0.60)
					    {
					        return new xPvaExecutionResult(
					            ExecutionIntent.ExitShort,
					            $"exit_short_no_reverse strong_short_context longScore={sig.Score:F2} {xPvaContainerEngine.Format(cnt)}");
					    }
					
					    return new xPvaExecutionResult(
					        ExecutionIntent.ReverseToLong,
					        $"reverse_to_long_valid phase={sig.Phase} score={sig.Score:F2} deg={degradingBars}");
					}
					
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
					
					bool immediateShortP3Failure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.HasP3 &&
					    cnt.State == xPvaContainerState.SeekingP3 &&
					    cnt.ImbalanceAtP3 >= 0.40;
					
					if (immediateShortP3Failure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_immediate_p3_failure imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}
					
					bool shortImbalanceFailure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.HasP3 &&
					    cnt.ImbalanceAtP3 >= 0.40 &&
					    degradingBars >= 1;
					
					if (shortImbalanceFailure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_imbalance_failure_exit imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}
				
					int shortPostLen =
					    cnt != null && cnt.PostP3AttemptEndBar >= cnt.PostP3AttemptStartBar
					        ? cnt.PostP3AttemptEndBar - cnt.PostP3AttemptStartBar + 1
					        : 0;
					
					bool strongShortPostP3 =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.State == xPvaContainerState.PostP3 &&
					    cnt.ImbalanceAtP3 <= -0.25;
					
					if (strongShortPostP3)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.HoldShort,
					        $"short_post_p3_strong_hold imbP3={cnt.ImbalanceAtP3:F2} {xPvaContainerEngine.Format(cnt)}");
					}
					
					bool weakShortPostP3 =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.State == xPvaContainerState.PostP3 &&
					    cnt.PostP3AttemptStartBar >= 0 &&
					    cnt.ImbalanceAtP3 >= 0.10 &&
					    shortPostLen <= 2;
					
					if (weakShortPostP3)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_post_p3_weak_exit imbP3={cnt.ImbalanceAtP3:F2} postLen={shortPostLen} {xPvaContainerEngine.Format(cnt)}");
					}
					
					bool shortFttFailure =
					    cnt != null &&
					    cnt.Direction == xPvaContainerDirection.Down &&
					    cnt.State == xPvaContainerState.FttDetected;
					
					if (shortFttFailure)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_ftt_failure_exit {xPvaContainerEngine.Format(cnt)}");
					}

					bool deadSignalZone =
					    sig.Phase == SignalPhase.None &&
					    sig.Score == 0.0 &&
					    degradingBars >= 4;
					
					if (deadSignalZone)
					{
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_dead_signal_exit deg={degradingBars}");
					}
					
				    if (degradingBars >= maxNoneBarsInPosition)
					{
					    bool containerStillSupportsShort =
					        cnt != null &&
					        cnt.Direction == xPvaContainerDirection.Down &&
					        (cnt.State == xPvaContainerState.SeekingP2 ||
					         cnt.State == xPvaContainerState.SeekingP3);
					
					    int maxContainerGraceBars = 2;
					    int graceLimit = maxNoneBarsInPosition + maxContainerGraceBars;
					
					    bool favorableShortImbalance =
					        cnt != null &&
					        cnt.HasP3 &&
					        cnt.ImbalanceAtP3 <= -0.25;
					
						

					    if (containerStillSupportsShort &&
					        (degradingBars <= graceLimit ||
					         (favorableShortImbalance && degradingBars <= graceLimit + 2)))
					    {
					        return new xPvaExecutionResult(
					            ExecutionIntent.HoldShort,
					            $"hold_short_decay_grace_container_supports phase={sig.Phase} score={sig.Score:F2} deg={degradingBars} graceLimit={graceLimit} {xPvaContainerEngine.Format(cnt)}");
					    }
					
					    return new xPvaExecutionResult(
					        ExecutionIntent.ExitShort,
					        $"short_decay_exit phase={sig.Phase} score={sig.Score:F2} deg={degradingBars}");
					}
					
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

















































































