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
			
			ComputeEnergyScores(snapshot, features, priorState);

            snapshot.MacroState = ClassifyMacroState(snapshot, priorState);
            snapshot.SFCStatus = ClassifySfcStatus(snapshot);
			
            NormalizeMacroState(snapshot);
			return snapshot;
        }

		private static void ComputeEnergyScores(
		    ApvaStateSnapshot snapshot,
		    ApvaBarFeatures features,
		    ApvaStateSnapshot priorState)
		{
		    if (snapshot == null || snapshot.Scores == null || features == null)
		        return;
		
		    double overlap = Clamp01(features.OverlapRatio);
		    double narrowBody = Clamp01(1.0 - features.BodyToRangeRatio);
		    double ambiguity = Clamp01(snapshot.Scores.AmbiguityScore);
		    double degradation = Clamp01(snapshot.Scores.DegradationScore);
		    double dominance = Clamp01(snapshot.Scores.DominanceScore);
		    double balance = Clamp01(snapshot.Scores.BalanceScore);
		
		    double priorCompression =
		        priorState != null && priorState.Scores != null
		            ? priorState.Scores.CompressionScore
		            : 0.0;
		
		    double rawCompression =
		        0.35 * overlap +
		        0.25 * narrowBody +
		        0.20 * ambiguity +
		        0.20 * balance;
		
		    // Persistent unresolved/balance should allow compression to accumulate.
		    if (priorState != null &&
		        (priorState.MacroState == ApvaMacroState.Unresolved ||
		         priorState.MacroState == ApvaMacroState.Balance))
		    {
		        rawCompression =
		            0.70 * rawCompression +
		            0.30 * priorCompression;
		    }
		
		    // Heavy degradation means this is not clean compression.
		    rawCompression *= (1.0 - 0.35 * degradation);
		
		    double rawExpansion =
		        0.45 * dominance +
		        0.25 * snapshot.SequenceAuthority +
		        0.20 * features.BodyToRangeRatio +
		        0.10 * (1.0 - overlap);
		
		    // Compression can fuel expansion, but only if degradation is not dominant.
		    if (priorCompression >= 0.45 && degradation < 0.60)
		        rawExpansion += 0.15 * priorCompression;
		
		    snapshot.Scores.CompressionScore = Clamp01(rawCompression);
		    snapshot.Scores.ExpansionPressure = Clamp01(rawExpansion);
			
			double structuralCompression =
			    0.40 * balance +
			    0.30 * (1.0 - degradation) +
			    0.20 * snapshot.SequenceAuthority +
			    0.10 * overlap;
			
			double entropicCompression =
			    0.40 * ambiguity +
			    0.30 * degradation +
			    0.20 * overlap +
			    0.10 * (1.0 - snapshot.SequenceAuthority);
			
			snapshot.Scores.StructuralCompression =
			    Clamp01(structuralCompression);
			
			snapshot.Scores.EntropicCompression =
			    Clamp01(entropicCompression);
		}
		
		private static double Clamp01(double value)
		{
		    if (value < 0.0)
		        return 0.0;
		
		    if (value > 1.0)
		        return 1.0;
		
		    return value;
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








