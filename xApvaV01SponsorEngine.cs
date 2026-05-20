namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01SponsorEngine
    {
        public void Evaluate(
            ApvaStateSnapshot snapshot,
            ApvaStateSnapshot prior)
        {
            if (snapshot == null)
                return;

            var s = snapshot.Scores;

            bool hasUsefulDirection =
                snapshot.ActiveDirection != ApvaDirection.Unknown &&
                snapshot.ActiveDirection != ApvaDirection.Mixed;

            bool authorityHigh =
                snapshot.SequenceAuthority >= 0.75;

            bool dominanceRising =
                prior != null &&
                s.DominanceScore > prior.Scores.DominanceScore;

            bool degradationFalling =
                prior != null &&
                s.DegradationScore < prior.Scores.DegradationScore;

            bool ambiguityFalling =
                prior != null &&
                s.AmbiguityScore < prior.Scores.AmbiguityScore;

            bool balanceOrUnresolved =
                snapshot.MacroState == ApvaMacroState.Balance ||
                snapshot.MacroState == ApvaMacroState.Unresolved;

            if (balanceOrUnresolved &&
			    hasUsefulDirection &&
			    dominanceRising)
			{
			    bool accepted =
				    authorityHigh &&
				    snapshot.Scores.DominanceScore >= 0.35 &&
				    snapshot.Scores.DegradationScore <= 0.65 &&
				    degradationFalling &&
				    ambiguityFalling &&
				    snapshot.Scores.AmbiguityScore < prior.Scores.AmbiguityScore * 0.90;
			
			    if (accepted)
			    {
			        snapshot.SponsorState = ApvaSponsorState.Reasserting;
			        snapshot.SponsorConfidence = 0.65;
			    }
			    else
			    {
			        snapshot.SponsorState = ApvaSponsorState.ReclaimAttempt;
			        snapshot.SponsorConfidence = 0.45;
			    }
			
			    return;
			}

            if (balanceOrUnresolved &&
                hasUsefulDirection &&
                authorityHigh &&
                dominanceRising &&
                ambiguityFalling)
            {
                snapshot.SponsorState = ApvaSponsorState.Pressured;
                snapshot.SponsorConfidence = 0.55;
                return;
            }

            if (s.DominanceScore >= 0.65 &&
                s.DegradationScore < 0.35 &&
                s.AmbiguityScore < 0.35)
            {
                snapshot.SponsorState = ApvaSponsorState.Dominant;
                snapshot.SponsorConfidence = 0.85;
                return;
            }

            if (s.DominanceScore >= 0.45 &&
                s.DegradationScore >= 0.35 &&
                s.DegradationScore < 0.60)
            {
                snapshot.SponsorState = ApvaSponsorState.Pressured;
                snapshot.SponsorConfidence = 0.65;
                return;
            }

            if (s.DominanceScore >= 0.30 &&
			    s.DegradationScore >= 0.60 &&
			    s.TransitionScore >= 0.20)
            {
                snapshot.SponsorState = ApvaSponsorState.Challenged;
                snapshot.SponsorConfidence = 0.70;
                return;
            }

          bool priorHadSponsor =
			    prior != null &&
			    (prior.SponsorState == ApvaSponsorState.Dominant ||
			     prior.SponsorState == ApvaSponsorState.Pressured ||
			     prior.SponsorState == ApvaSponsorState.Challenged);
			
			if (priorHadSponsor &&
			    s.DegradationScore >= 0.75 &&
			    s.DominanceScore < 0.30)
            {
                snapshot.SponsorState = ApvaSponsorState.Failing;
                snapshot.SponsorConfidence = 0.80;
                return;
            }

            if (prior != null)
            {
                bool directionChanged =
                    prior.ActiveDirection != ApvaDirection.Unknown &&
                    snapshot.ActiveDirection != ApvaDirection.Unknown &&
                    prior.ActiveDirection != snapshot.ActiveDirection;

                bool priorWasWeak =
                    prior.SponsorState == ApvaSponsorState.Challenged ||
                    prior.SponsorState == ApvaSponsorState.Failing ||
                    prior.SponsorState == ApvaSponsorState.Unresolved;

                bool currentStrong =
                    s.DominanceScore >= 0.45 &&
                    s.DegradationScore < 0.50;

                if (directionChanged && priorWasWeak && currentStrong)
                {
                    snapshot.SponsorState = ApvaSponsorState.Transferred;
                    snapshot.SponsorConfidence = 0.75;
                    return;
                }

                bool reclaiming =
                    (prior.SponsorState == ApvaSponsorState.Challenged ||
                     prior.SponsorState == ApvaSponsorState.Failing ||
                     prior.SponsorState == ApvaSponsorState.Balance ||
                     prior.SponsorState == ApvaSponsorState.Unresolved) &&
                    s.DominanceScore > prior.Scores.DominanceScore &&
                    s.DegradationScore < prior.Scores.DegradationScore;

                if (reclaiming)
				{
				   bool accepted =
					    authorityHigh &&
					    snapshot.Scores.DominanceScore >= 0.35 &&
					    snapshot.Scores.DegradationScore <= 0.65 &&
					    degradationFalling &&
					    ambiguityFalling &&
					    snapshot.Scores.AmbiguityScore < prior.Scores.AmbiguityScore * 0.90;
				
				    if (accepted)
				    {
				        snapshot.SponsorState = ApvaSponsorState.Reasserting;
				        snapshot.SponsorConfidence = 0.70;
				    }
				    else
				    {
				        snapshot.SponsorState = ApvaSponsorState.ReclaimAttempt;
				        snapshot.SponsorConfidence = 0.45;
				    }
				
				    return;
				}
            }

            if (snapshot.MacroState == ApvaMacroState.Balance)
            {
                snapshot.SponsorState = ApvaSponsorState.Balance;
                snapshot.SponsorConfidence = 0.30;
                return;
            }

            if (snapshot.MacroState == ApvaMacroState.Unresolved)
            {
                snapshot.SponsorState = ApvaSponsorState.Unresolved;
                snapshot.SponsorConfidence = 0.20;
                return;
            }

            snapshot.SponsorState = ApvaSponsorState.Unknown;
            snapshot.SponsorConfidence = 0.25;
        }
    }
}






