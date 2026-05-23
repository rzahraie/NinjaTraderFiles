namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01SponsorEngine
    {
        private ApvaSponsorState persistentSponsorState = ApvaSponsorState.Unknown;
        private ApvaDirection persistentSponsorDirection = ApvaDirection.Unknown;
        private int sponsorPersistenceBars;
		private double sponsorStrength;
		private double sponsorPressure;
		private double sponsorFailure;

        private sealed class SponsorCandidate
        {
            public ApvaSponsorState State;
            public double Confidence;
            public int Priority;
            public int PersistenceBars;
        }

		private double Clamp01(double value)
{
    if (value < 0.0)
        return 0.0;

    if (value > 1.0)
        return 1.0;

    return value;
}

		private void UpdateLatentSponsorState(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null || snapshot.Scores == null)
		        return;
		
		    double dominance = snapshot.Scores.DominanceScore;
		    double degradation = snapshot.Scores.DegradationScore;
		    double ambiguity = snapshot.Scores.AmbiguityScore;
		
		    // Natural decay.
		    sponsorStrength *= 0.92;
		    sponsorPressure *= 0.90;
		    sponsorFailure *= 0.88;
		
		    // Reinforcement.
		    if (snapshot.MacroState == ApvaMacroState.Directional &&
		        dominance >= 0.40 &&
		        degradation < 0.55)
		    {
		        sponsorStrength += 0.10 * dominance;
		    }
		
		    // Pressure.
		    if (degradation >= 0.45 || ambiguity >= 0.35)
		        sponsorPressure += 0.08 * degradation + 0.04 * ambiguity;
		
		    // Failure.
		    if (HasEvent(snapshot, ApvaEventType.RejectedReclaim) ||
		        HasEvent(snapshot, ApvaEventType.FailedContinuation))
		    {
		        sponsorFailure += 0.18;
		        sponsorPressure += 0.08;
		    }
		
		    // Successful reclaim slightly repairs sponsorship.
		    if (HasEvent(snapshot, ApvaEventType.AcceptedReclaim))
		    {
		        sponsorStrength += 0.12;
		        sponsorFailure *= 0.70;
		    }
		
		    // Prolonged unresolved should not automatically imply failure.
		    if (snapshot.MacroState == ApvaMacroState.Unresolved)
		    {
		        sponsorPressure *= 0.96;
		        sponsorFailure *= 0.94;
		    }
		
		    sponsorStrength = Clamp01(sponsorStrength);
		    sponsorPressure = Clamp01(sponsorPressure);
		    sponsorFailure = Clamp01(sponsorFailure);
		}

		private void AddLatentSponsorCandidate(
		    ApvaStateSnapshot snapshot,
		    System.Collections.Generic.List<SponsorCandidate> candidates)
		{
		    if (snapshot == null)
		        return;
		
		    if (sponsorFailure >= 0.70)
		    {
		        candidates.Add(new SponsorCandidate
		        {
		            State = ApvaSponsorState.FailedReclaim,
		            Confidence = 0.65 + 0.25 * sponsorFailure,
		            Priority = 68,
		            PersistenceBars = 1
		        });
		
		        return;
		    }
		
		    if (sponsorStrength >= 0.70 &&
		        sponsorPressure < 0.45 &&
		        snapshot.MacroState == ApvaMacroState.Directional)
		    {
		        candidates.Add(new SponsorCandidate
		        {
		            State = ApvaSponsorState.Dominant,
		            Confidence = 0.65 + 0.25 * sponsorStrength,
		            Priority = 62,
		            PersistenceBars = 2
		        });
		
		        return;
		    }
		
		    if (sponsorStrength >= 0.50 &&
		        sponsorPressure >= 0.35 &&
		        sponsorFailure < 0.70)
		    {
		        candidates.Add(new SponsorCandidate
		        {
		            State = ApvaSponsorState.Challenged,
		            Confidence = 0.55 + 0.25 * sponsorPressure,
		            Priority = 58,
		            PersistenceBars = 1
		        });
		
		        return;
		    }
		
		    if (sponsorStrength >= 0.45 &&
		        sponsorPressure < 0.55)
		    {
		        candidates.Add(new SponsorCandidate
		        {
		            State = ApvaSponsorState.Pressured,
		            Confidence = 0.55 + 0.20 * sponsorStrength,
		            Priority = 56,
		            PersistenceBars = 1
		        });
		    }
		}

        public void Evaluate(
            ApvaStateSnapshot snapshot,
            ApvaStateSnapshot prior)
        {
            if (snapshot == null)
                return;

			UpdateLatentSponsorState(snapshot);
			
            var candidates = new System.Collections.Generic.List<SponsorCandidate>();

            AddAcceptedReclaimCandidate(snapshot, candidates);
            AddRejectedReclaimCandidate(snapshot, candidates);
            AddPersistentSponsorCandidate(snapshot, candidates);
            AddTransferredCandidate(snapshot, prior, candidates);
            AddDominanceCandidates(snapshot, prior, candidates);
			AddLatentSponsorCandidate(snapshot, candidates);
            AddEventDrivenReclaimCandidate(snapshot, candidates);
			AddDegradingFallbackCandidate(snapshot, candidates);
			AddDirectionalFallbackCandidate(snapshot, candidates);
            AddMacroFallbackCandidate(snapshot, candidates);

            ApplyBestCandidate(snapshot, candidates);
        }
		
		private void AddDegradingFallbackCandidate(
		    ApvaStateSnapshot snapshot,
		    System.Collections.Generic.List<SponsorCandidate> candidates)
		{
		    if (snapshot.MacroState != ApvaMacroState.Degrading)
		        return;
		
		    if (snapshot.Scores.DegradationScore >= 0.65 ||
		        snapshot.Scores.AmbiguityScore >= 0.40)
		    {
		        candidates.Add(new SponsorCandidate
		        {
		            State = ApvaSponsorState.Failing,
		            Confidence = 0.60,
		            Priority = 22,
		            PersistenceBars = 0
		        });
		
		        return;
		    }
		
		    candidates.Add(new SponsorCandidate
		    {
		        State = ApvaSponsorState.Challenged,
		        Confidence = 0.55,
		        Priority = 22,
		        PersistenceBars = 0
		    });
		}

		private void AddDirectionalFallbackCandidate(
		    ApvaStateSnapshot snapshot,
		    System.Collections.Generic.List<SponsorCandidate> candidates)
		{
		    if (snapshot.MacroState != ApvaMacroState.Directional)
		        return;
		
		    if (snapshot.Scores.DominanceScore < 0.40)
			    return;
			
			if (snapshot.Scores.DegradationScore >= 0.45)
			    return;
			
			if (snapshot.Scores.AmbiguityScore >= 0.28)
			    return;
		
		    candidates.Add(new SponsorCandidate
		    {
		        State = ApvaSponsorState.Dominant,
		        Confidence = 0.55,
		        Priority = 20,
		        PersistenceBars = 0
		    });
		}

        private void AddAcceptedReclaimCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (!HasEvent(snapshot, ApvaEventType.AcceptedReclaim))
                return;

           if (snapshot.Scores.DegradationScore >= 0.60 ||
			    snapshot.Scores.AmbiguityScore >= 0.35)
			{
			    candidates.Add(new SponsorCandidate
			    {
			        State = ApvaSponsorState.Challenged,
			        Confidence = 0.60,
			        Priority = 95,
			        PersistenceBars = 0
			    });
			
			    return;
			}

            candidates.Add(new SponsorCandidate
            {
                State = ApvaSponsorState.Reasserting,
                Confidence = 0.70,
                Priority = 100,
                PersistenceBars = 2
            });
        }

        private void AddRejectedReclaimCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (!HasEvent(snapshot, ApvaEventType.RejectedReclaim))
                return;

            candidates.Add(new SponsorCandidate
            {
                State = ApvaSponsorState.FailedReclaim,
                Confidence = 0.70,
                Priority = 100,
                PersistenceBars = 1
            });
        }

        private void AddPersistentSponsorCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (sponsorPersistenceBars <= 0)
                return;

			if (persistentSponsorState == ApvaSponsorState.FailedReclaim &&
			    snapshot.MacroState == ApvaMacroState.Directional &&
			    snapshot.Scores.DominanceScore >= 0.40 &&
			    snapshot.Scores.DegradationScore < 0.45 &&
			    snapshot.Scores.AmbiguityScore < 0.28)
			{
			    ClearPersistentSponsor();
			    return;
			}

            if (persistentSponsorState == ApvaSponsorState.Unknown)
                return;

            if (snapshot.ActiveDirection != persistentSponsorDirection)
                return;

			if (HasEvent(snapshot, ApvaEventType.ReclaimAttempt))
    			return;
			
            bool reassertingDegraded =
			    persistentSponsorState == ApvaSponsorState.Reasserting &&
			    (snapshot.Scores.DegradationScore >= 0.60 ||
			     snapshot.Scores.AmbiguityScore >= 0.35 ||
			     HasEvent(snapshot, ApvaEventType.FailedContinuation) ||
			     HasEvent(snapshot, ApvaEventType.RejectedReclaim));
			
			bool rejection = reassertingDegraded;

            if (rejection || snapshot.Scores.AmbiguityScore >= 0.70)
            {
                ClearPersistentSponsor();
                return;
            }

            candidates.Add(new SponsorCandidate
            {
                State = persistentSponsorState,
                Confidence = 0.60,
                Priority = 80,
                PersistenceBars = -1
            });
        }

        private void AddTransferredCandidate(
            ApvaStateSnapshot snapshot,
            ApvaStateSnapshot prior,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (prior == null)
                return;

            bool directionChanged =
                prior.ActiveDirection != ApvaDirection.Unknown &&
                snapshot.ActiveDirection != ApvaDirection.Unknown &&
                prior.ActiveDirection != snapshot.ActiveDirection;

            bool priorWasWeak =
                prior.SponsorState == ApvaSponsorState.Challenged ||
                prior.SponsorState == ApvaSponsorState.Failing ||
                prior.SponsorState == ApvaSponsorState.Unresolved;

            bool currentStrong =
                snapshot.Scores.DominanceScore >= 0.45 &&
                snapshot.Scores.DegradationScore < 0.50;

            if (directionChanged && priorWasWeak && currentStrong)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Transferred,
                    Confidence = 0.75,
                    Priority = 70
                });
            }
        }

        private void AddDominanceCandidates(
            ApvaStateSnapshot snapshot,
            ApvaStateSnapshot prior,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            var s = snapshot.Scores;

            if (s.DominanceScore >= 0.65 &&
                s.DegradationScore < 0.35 &&
                s.AmbiguityScore < 0.35)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Dominant,
                    Confidence = 0.85,
                    Priority = 60
                });
            }

            if (s.DominanceScore >= 0.45 &&
                s.DegradationScore >= 0.35 &&
                s.DegradationScore < 0.60)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Pressured,
                    Confidence = 0.65,
                    Priority = 55
                });
            }

            if (s.DominanceScore >= 0.30 &&
                s.DegradationScore >= 0.60 &&
                s.TransitionScore >= 0.20)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Challenged,
                    Confidence = 0.70,
                    Priority = 50
                });
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
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Failing,
                    Confidence = 0.80,
                    Priority = 65
                });
            }
        }

        private void AddEventDrivenReclaimCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (!HasEvent(snapshot, ApvaEventType.ReclaimAttempt))
                return;

            candidates.Add(new SponsorCandidate
            {
                State = ApvaSponsorState.ReclaimAttempt,
                Confidence = 0.45,
                Priority = 75
            });
        }

        private void AddMacroFallbackCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            if (snapshot.MacroState == ApvaMacroState.Balance)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Balance,
                    Confidence = 0.30,
                    Priority = 10
                });
                return;
            }

            if (snapshot.MacroState == ApvaMacroState.Unresolved)
            {
                candidates.Add(new SponsorCandidate
                {
                    State = ApvaSponsorState.Unresolved,
                    Confidence = 0.20,
                    Priority = 10
                });
                return;
            }

            candidates.Add(new SponsorCandidate
            {
                State = ApvaSponsorState.Unknown,
                Confidence = 0.25,
                Priority = 0
            });
        }

        private void ApplyBestCandidate(
            ApvaStateSnapshot snapshot,
            System.Collections.Generic.List<SponsorCandidate> candidates)
        {
            SponsorCandidate best = null;

            foreach (var c in candidates)
            {
                if (best == null ||
                    c.Priority > best.Priority ||
                    (c.Priority == best.Priority && c.Confidence > best.Confidence))
                {
                    best = c;
                }
            }

            if (best == null)
            {
                snapshot.SponsorState = ApvaSponsorState.Unknown;
                snapshot.SponsorConfidence = 0.25;
                return;
            }

            snapshot.SponsorState = best.State;
            snapshot.SponsorConfidence = best.Confidence;

            if (best.PersistenceBars > 0)
            {
                persistentSponsorState = best.State;
                persistentSponsorDirection = snapshot.ActiveDirection;
                sponsorPersistenceBars = best.PersistenceBars;
            }
            else if (best.PersistenceBars == -1)
            {
                sponsorPersistenceBars--;
            }
        }

        private void ClearPersistentSponsor()
        {
            sponsorPersistenceBars = 0;
            persistentSponsorState = ApvaSponsorState.Unknown;
            persistentSponsorDirection = ApvaDirection.Unknown;
        }

        private bool HasEvent(
            ApvaStateSnapshot snapshot,
            ApvaEventType eventType)
        {
            if (snapshot == null || snapshot.Events == null)
                return false;

            foreach (var e in snapshot.Events)
            {
                if (e.EventType == eventType)
                    return true;
            }

            return false;
        }
    }
}


