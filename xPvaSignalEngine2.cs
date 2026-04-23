namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaSignalEngine2
    {
        private readonly xPvaEngineParameters p;

        public xPvaSignalEngine2(xPvaEngineParameters parameters)
        {
            p = parameters;
        }

        public xPvaSignalResult Compute(
            in xPvaDirectionResult dir,
            in xPvaDominanceResult dom,
            in xPvaSequenceStats seq,
            in xPvaImbalanceResult imb,
            in xPvaLateralResult lat)
        {
            if (dir.Context == DirectionContext.Up)
			{
			    if (imb.Imbalance >= p.StrongImbalanceThreshold
			        && seq.FlipCount <= 2
			        && (dom.State == DominanceState.Dominant
			            || (dom.State == DominanceState.NonDominant
			                && seq.DominanceRunLength <= p.MaxPullbackBars))
			        && lat.State != LateralStateKind.BrokenDown)
			    {
			        return new xPvaSignalResult(
			            SignalPhase.LongValid,
			            imb.Imbalance,
			            "up_imbalance_valid");
			    }
			
			if (imb.Imbalance > p.NeutralImbalanceThreshold)
			{
			    if (dom.State == DominanceState.NonDominant &&
			        seq.DominanceRunLength > p.MaxPullbackBars)
			    {
			        return new xPvaSignalResult(
			            SignalPhase.None,
			            0.0,
			            "up_candidate_suppressed_rollover");
			    }
			
			    return new xPvaSignalResult(
			        SignalPhase.LongCandidate,
			        imb.Imbalance,
			        "up_candidate");
			}
			
			    // NEW: opposition pressure while still in up context
			    if (dom.State == DominanceState.NonDominant
			        && imb.Imbalance < -p.NeutralImbalanceThreshold
			        && lat.State != LateralStateKind.BrokenUp)
			    {
			        return new xPvaSignalResult(
			            SignalPhase.ShortCandidate,
			            -imb.Imbalance,
			            "up_opposition_candidate");
			    }
			
			    return new xPvaSignalResult(SignalPhase.None, 0.0, "up_none");
			}
			else if (dir.Context == DirectionContext.Down)
			{
			    if (imb.Imbalance <= -p.StrongImbalanceThreshold
			        && seq.FlipCount <= 2
			        && (dom.State == DominanceState.Dominant
			            || (dom.State == DominanceState.NonDominant
			                && seq.DominanceRunLength <= p.MaxPullbackBars))
			        && lat.State != LateralStateKind.BrokenUp)
			    {
			        return new xPvaSignalResult(
			            SignalPhase.ShortValid,
			            -imb.Imbalance,
			            "down_imbalance_valid");
			    }
			
			    if (imb.Imbalance < -p.NeutralImbalanceThreshold)
				{
				    if (dom.State == DominanceState.NonDominant &&
				        seq.DominanceRunLength > p.MaxPullbackBars)
				    {
				        return new xPvaSignalResult(
				            SignalPhase.None,
				            0.0,
				            "down_candidate_suppressed_rollover");
				    }
				
				    return new xPvaSignalResult(
				        SignalPhase.ShortCandidate,
				        -imb.Imbalance,
				        "down_candidate");
				}
			
			    // NEW: opposition pressure while still in down context
			    if (dom.State == DominanceState.NonDominant
			        && imb.Imbalance > p.NeutralImbalanceThreshold
			        && lat.State != LateralStateKind.BrokenDown)
			    {
			        return new xPvaSignalResult(
			            SignalPhase.LongCandidate,
			            imb.Imbalance,
			            "down_opposition_candidate");
			    }
			
			    return new xPvaSignalResult(SignalPhase.None, 0.0, "down_none");
			}

            return new xPvaSignalResult(SignalPhase.None, 0.0, "none");
        }
    }
}



