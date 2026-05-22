namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01StateEngine
    {
        public ApvaStateSnapshot BuildSnapshot(
            ApvaBarFeatures features,
            ApvaSequenceState sequence,
            ApvaScores scores,
            ApvaStateSnapshot priorState)
        {
            var snapshot = new ApvaStateSnapshot
            {
                BarIndex = features != null ? features.BarIndex : 0,
                Time = features != null ? features.Time : default(System.DateTime),

                Scores = scores ?? new ApvaScores(),

                SequencePhase = sequence != null
                    ? sequence.Phase
                    : ApvaSequencePhase.Unknown,

                SequenceAuthority = sequence != null
                    ? sequence.AuthorityScore
                    : 0.0,

                MaturityLevel = sequence != null
                    ? sequence.Maturity
                    : ApvaMaturityLevel.Unknown,

                ActiveDirection = sequence != null
                    ? sequence.Direction
                    : ApvaDirection.Unknown
            };

            snapshot.MacroState = ClassifyMacroState(snapshot, priorState);
            snapshot.SFCStatus = ClassifySfcStatus(snapshot);

            NormalizeMacroState(snapshot);
			return snapshot;
        }

        private static ApvaMacroState ClassifyMacroState(
            ApvaStateSnapshot current,
            ApvaStateSnapshot prior)
        {
            var s = current.Scores;

            if (s.AmbiguityScore >= 0.65)
                return ApvaMacroState.Unresolved;

            if (s.TransitionScore >= 0.65 &&
                s.DegradationScore >= 0.45)
                return ApvaMacroState.TransitionAttempt;

            if (s.BalanceScore >= 0.60)
                return ApvaMacroState.Balance;

            if (s.DegradationScore >= 0.55 &&
                s.DominanceScore >= 0.30)
                return ApvaMacroState.Degrading;

            if (s.DominanceScore >= 0.45)
                return ApvaMacroState.Directional;

            if (prior != null &&
                prior.MacroState == ApvaMacroState.Directional &&
                s.DominanceScore >= 0.35)
                return ApvaMacroState.Directional;
			
			if (s.DegradationScore >= 0.45 ||
			    s.BalanceScore >= 0.40 ||
			    s.AmbiguityScore >= 0.40)
			    return ApvaMacroState.Unresolved;
			
			if (prior != null &&
			    prior.SponsorState == ApvaSponsorState.Reasserting &&
			    current.ActiveDirection == prior.ActiveDirection)
			{
			    return ApvaMacroState.Unresolved;
			}
			
			if (current.Events != null)
			{
			    foreach (var e in current.Events)
			    {
			        if (e.EventType == ApvaEventType.ReclaimAttempt ||
			            e.EventType == ApvaEventType.RejectedReclaim ||
			            e.EventType == ApvaEventType.AcceptedReclaim)
			            return ApvaMacroState.Unresolved;
			    }
			}

            return ApvaMacroState.Unknown;
        }

        private static string ClassifySfcStatus(ApvaStateSnapshot current)
        {
            var s = current.Scores;

            if (s.DegradationScore >= 0.65 &&
                s.TransitionScore >= 0.45)
                return "Confirmed Structural";

            if (s.DegradationScore >= 0.50)
                return "Candidate";

            return "None";
        }
		
		private static void NormalizeMacroState(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    if (snapshot.MacroState != ApvaMacroState.Unknown)
		        return;
		
		    if (snapshot.Scores == null)
		        return;
		
		    bool enoughAuctionEvidence =
		        snapshot.Scores.DominanceScore >= 0.20 ||
		        snapshot.Scores.DegradationScore >= 0.20 ||
		        snapshot.Scores.BalanceScore >= 0.20 ||
		        snapshot.Scores.TransitionScore >= 0.10 ||
		        snapshot.Scores.AmbiguityScore >= 0.20;
		
		    if (enoughAuctionEvidence)
		        snapshot.MacroState = ApvaMacroState.Unresolved;
			
			if (snapshot.MacroState == ApvaMacroState.Unresolved &&
			    snapshot.Scores.DominanceScore >= 0.40 &&
			    snapshot.Scores.DegradationScore < 0.45 &&
			    snapshot.Scores.AmbiguityScore < 0.25 &&
			    snapshot.SequenceAuthority >= 0.67)
			{
			    snapshot.MacroState = ApvaMacroState.Directional;
			    return;
			}
		}
    }
}





