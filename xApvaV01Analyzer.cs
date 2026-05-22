using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01Analyzer
    {
        private readonly ApvaV01BarFeatureEngine featureEngine = new ApvaV01BarFeatureEngine();
        private readonly ApvaV01SequenceEngine sequenceEngine = new ApvaV01SequenceEngine();
        private readonly ApvaV01LandmarkStore landmarkStore = new ApvaV01LandmarkStore();
        private readonly ApvaV01EventEngine eventEngine = new ApvaV01EventEngine();
        private readonly ApvaV01ScoringEngine scoringEngine = new ApvaV01ScoringEngine();
        private readonly ApvaV01StateEngine stateEngine = new ApvaV01StateEngine();
        private readonly ApvaV01ExpectationEngine expectationEngine = new ApvaV01ExpectationEngine();
        private readonly ApvaV01SponsorEngine sponsorEngine = new ApvaV01SponsorEngine();

        private readonly List<ApvaStateSnapshot> snapshots = new List<ApvaStateSnapshot>();

        private ApvaBarFeatures priorFeatures;
        private ApvaSequenceState activeSequence;
        private ApvaStateSnapshot priorState;
        private ApvaScores priorScores = new ApvaScores();

        public IReadOnlyList<ApvaStateSnapshot> Snapshots
        {
            get { return snapshots.AsReadOnly(); }
        }

        public ApvaV01LandmarkStore LandmarkStore
        {
            get { return landmarkStore; }
        }

        public ApvaStateSnapshot Update(
            int barIndex,
            System.DateTime time,
            double open,
            double high,
            double low,
            double close,
            double volume)
        {
            landmarkStore.AgeActiveLandmarks();

            var features = featureEngine.Build(
                barIndex,
                time,
                open,
                high,
                low,
                close,
                volume,
                priorFeatures);

            activeSequence = sequenceEngine.Update(features, activeSequence);

            var events = eventEngine.GenerateEvents(
                features,
                priorFeatures,
                activeSequence,
                landmarkStore,
                priorState);

            var scores = scoringEngine.UpdateScores(
                priorScores,
                events,
                activeSequence);

            var snapshot = stateEngine.BuildSnapshot(
                features,
                activeSequence,
                scores,
                priorState);

            snapshot.Events.AddRange(events);

            NormalizeEventMacroCoherence(snapshot);

            sponsorEngine.Evaluate(snapshot, priorState);
			
			NormalizeAcceptedReclaimMacroState(snapshot);

            NormalizeSponsorMacroCoherence(snapshot);

            expectationEngine.ApplyExpectations(snapshot);

            snapshots.Add(snapshot);

            priorFeatures = features;
            priorState = snapshot;
            priorScores = scores;

            return snapshot;
        }

        private static void NormalizeEventMacroCoherence(
            ApvaStateSnapshot snapshot)
        {
            foreach (var e in snapshot.Events)
            {
                if (e.EventType == ApvaEventType.ReclaimAttempt ||
                    e.EventType == ApvaEventType.AcceptedReclaim ||
                    e.EventType == ApvaEventType.RejectedReclaim)
                {
                    if (snapshot.MacroState == ApvaMacroState.Unknown)
                        snapshot.MacroState = ApvaMacroState.Unresolved;

                    return;
                }
            }
        }

        private static void NormalizeSponsorMacroCoherence(
            ApvaStateSnapshot snapshot)
        {
            if (snapshot.MacroState == ApvaMacroState.Unknown &&
                (snapshot.SponsorState == ApvaSponsorState.ReclaimAttempt ||
                 snapshot.SponsorState == ApvaSponsorState.Reasserting ||
                 snapshot.SponsorState == ApvaSponsorState.FailedReclaim))
            {
                snapshot.MacroState = ApvaMacroState.Unresolved;
            }
        }

		private static void NormalizeAcceptedReclaimMacroState(
		    ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    if (snapshot.MacroState != ApvaMacroState.Unresolved)
		        return;
		
		    bool acceptedReclaim = false;
		
		    foreach (var e in snapshot.Events)
		    {
		        if (e.EventType == ApvaEventType.AcceptedReclaim)
		        {
		            acceptedReclaim = true;
		            break;
		        }
		    }
		
		    if (!acceptedReclaim)
		        return;
		
		    if (snapshot.SponsorState != ApvaSponsorState.Reasserting)
		        return;
		
		    if (snapshot.Scores.DominanceScore >= 0.24 &&
		        snapshot.Scores.DegradationScore < 0.50 &&
		        snapshot.Scores.AmbiguityScore < 0.35)
		    {
		        snapshot.MacroState = ApvaMacroState.Directional;
		    }
		}

        public void Reset()
        {
            snapshots.Clear();

            priorFeatures = null;
            activeSequence = null;
            priorState = null;
            priorScores = new ApvaScores();

            landmarkStore.Clear();
        }
    }
}

