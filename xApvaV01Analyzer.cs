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

        private readonly List<ApvaStateSnapshot> snapshots = new List<ApvaStateSnapshot>();

        private ApvaBarFeatures priorFeatures;
        private ApvaSequenceState activeSequence;
        private ApvaStateSnapshot priorState;
        private ApvaScores priorScores = new ApvaScores();
		
		private readonly ApvaV01SponsorEngine sponsorEngine = new ApvaV01SponsorEngine();

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

			sponsorEngine.Evaluate(snapshot, priorState);
			
            expectationEngine.ApplyExpectations(snapshot);

            snapshots.Add(snapshot);

            priorFeatures = features;
            priorState = snapshot;
            priorScores = scores;

            return snapshot;
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


