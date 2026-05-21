using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01EventEngine
    {
        private const double HighVolumeVsPeakRatio = 1.20;
        private const double HvcRangeCompressionRatio = 0.60;
        private const double WeakCloseEfficiencyThreshold = 0.45;
        private const double HighOverlapThreshold = 0.70;
		private int balancePressureRun;
		private int lastLateralSeedBar = -1;
		private bool priorReclaimAttempt;
		private ApvaDirection priorReclaimDirection = ApvaDirection.Unknown;
		private bool priorRejectedReclaimEligible;
		private ApvaDirection priorRejectedReclaimDirection = ApvaDirection.Unknown;
		private static int reclaimCooldownBars;

        public List<ApvaEvent> GenerateEvents(
		    ApvaBarFeatures current,
		    ApvaBarFeatures prior,
		    ApvaSequenceState sequence,
		    ApvaV01LandmarkStore landmarkStore,
		    ApvaStateSnapshot priorState)
		{
		    if (current == null)
		        throw new ArgumentNullException(nameof(current));
		
		    if (landmarkStore == null)
		        throw new ArgumentNullException(nameof(landmarkStore));
		
		    if (reclaimCooldownBars > 0)
		        reclaimCooldownBars--;
		
		    var events = new List<ApvaEvent>();
		
		    TryCreatePeakVolumeEvent(current, sequence, landmarkStore, events);
		    TryCreateHvcEvent(current, sequence, landmarkStore, events);
		    TryCreateFailedContinuationEvent(current, prior, sequence, priorState, events);
		    TryCreateLateralSeedEvent(current, prior, events);
		    TryCreateSfcCandidateEvent(current, sequence, priorState, events);
		
		    // Resolve prior reclaim attempt first.
		    TryCreateAcceptedReclaimFromPriorAttempt(current, sequence, events);
		    TryCreateRejectedReclaimFromPriorAttempt(current, sequence, events);
		
		    bool priorReclaimResolved =
		        HasEvent(events, ApvaEventType.AcceptedReclaim) ||
		        HasEvent(events, ApvaEventType.RejectedReclaim);
		
		    // Only create a new reclaim attempt if the prior one did not resolve.
		    if (!priorReclaimResolved)
		    {
		        TryCreateReclaimEvents(current, prior, sequence, priorState, events);
		    }
		
		    bool sawReclaimAttempt = false;
		    ApvaDirection sawReclaimDirection = ApvaDirection.Unknown;
		    bool sawAcceptedReclaim = false;
		    bool sawRejectedReclaim = false;
		
		    foreach (var e in events)
		    {
		        if (e.EventType == ApvaEventType.AcceptedReclaim)
		            sawAcceptedReclaim = true;
		
		        if (e.EventType == ApvaEventType.RejectedReclaim)
		            sawRejectedReclaim = true;
		
		        if (e.EventType == ApvaEventType.ReclaimAttempt)
		        {
		            sawReclaimAttempt = true;
		            sawReclaimDirection = e.Direction;
		        }
		    }
		
		    if (sawAcceptedReclaim || sawRejectedReclaim)
		    {
		        priorReclaimAttempt = false;
		        priorReclaimDirection = ApvaDirection.Unknown;
		
		        priorRejectedReclaimEligible = false;
		        priorRejectedReclaimDirection = ApvaDirection.Unknown;
		
		        reclaimCooldownBars = 2;
		    }
		    else if (sawReclaimAttempt)
		    {
		        priorReclaimAttempt = true;
		        priorReclaimDirection = sawReclaimDirection;
		
		        priorRejectedReclaimEligible = true;
		        priorRejectedReclaimDirection = sawReclaimDirection;
		
		        reclaimCooldownBars = 3;
		    }
		    else
		    {
		        priorReclaimAttempt = false;
		        priorReclaimDirection = ApvaDirection.Unknown;
		
		        priorRejectedReclaimEligible = false;
		        priorRejectedReclaimDirection = ApvaDirection.Unknown;
		    }
		
		    return events;
		}
		
		private void TryCreateRejectedReclaimFromPriorAttempt(
		    ApvaBarFeatures current,
		    ApvaSequenceState sequence,
		    List<ApvaEvent> events)
		{
		    if (!priorRejectedReclaimEligible || sequence == null)
		        return;
		
		    if (sequence.Direction != priorRejectedReclaimDirection)
		        return;
		
		    bool failedProgress =
		        sequence.Direction == ApvaDirection.Up
		            ? current.DirectionalResultUp <= 0.0
		            : current.DirectionalResultDown <= 0.0;
		
		    bool highOverlap =
		        current.OverlapRatio >= 0.70;
		
		    if (!(failedProgress || highOverlap))
		        return;
		
		    events.Add(new ApvaEvent
		    {
		        EventType = ApvaEventType.RejectedReclaim,
		        BarIndex = current.BarIndex,
		        Direction = sequence.Direction,
		        Strength = 0.60,
		        Confidence = 0.60,
		        EffectOnDominance = -0.08,
		        EffectOnDegradation = 0.10,
		        EffectOnBalance = 0.06,
		        EffectOnTransition = 0.06,
		        EffectOnAmbiguity = 0.08
		    });
		}

		private void TryCreateAcceptedReclaimFromPriorAttempt(
		    ApvaBarFeatures current,
		    ApvaSequenceState sequence,
		    List<ApvaEvent> events)
		{
		    if (!priorReclaimAttempt || sequence == null)
		        return;
		
		    if (sequence.Direction != priorReclaimDirection)
		        return;
		
		    bool directionalFollowThrough =
		        sequence.Direction == ApvaDirection.Up
		            ? current.DirectionalResultUp > 0.0
		            : current.DirectionalResultDown > 0.0;
		
		    bool strongClose =
		        GetCloseEfficiencyForDirection(current, sequence.Direction) > 0.60;
		
		    bool lowOverlap =
		        current.OverlapRatio < 0.50;
		
		    if (!(directionalFollowThrough && strongClose && lowOverlap))
		        return;
		
		    events.Add(new ApvaEvent
		    {
		        EventType = ApvaEventType.AcceptedReclaim,
		        BarIndex = current.BarIndex,
		        Direction = sequence.Direction,
		        Strength = 0.75,
		        Confidence = 0.70,
		        EffectOnDominance = 0.15,
		        EffectOnDegradation = -0.10,
		        EffectOnBalance = -0.08,
		        EffectOnTransition = -0.06,
		        EffectOnAmbiguity = -0.10
		    });
		}
		
        private static void TryCreatePeakVolumeEvent(
            ApvaBarFeatures current,
            ApvaSequenceState sequence,
            ApvaV01LandmarkStore landmarkStore,
            List<ApvaEvent> events)
        {
            var direction = DirectionFromPolarity(current.VolumePolarity);

            if (direction == ApvaDirection.Unknown)
                return;

            var priorPeak = landmarkStore.GetHighestVolumeActive(
                ApvaLandmarkType.PeakVolume,
                direction);

            bool isPeak =
                priorPeak == null ||
                current.Volume > priorPeak.Volume * HighVolumeVsPeakRatio;

            if (!isPeak)
                return;

            var landmark = new ApvaLandmark
            {
                Type = ApvaLandmarkType.PeakVolume,
                BarIndex = current.BarIndex,
                Time = current.Time,
                Direction = direction,
                Volume = current.Volume,
                Range = current.Range,
                CloseEfficiency = GetCloseEfficiencyForDirection(current, direction),
                SequencePhaseAtCreation = sequence != null ? sequence.Phase : ApvaSequencePhase.Unknown,
                MarketStateAtCreation = ApvaMacroState.Unknown,
                Strength = priorPeak == null
                    ? 0.50
                    : Clamp01(current.Volume / Math.Max(priorPeak.Volume, 1.0) - 1.0)
            };

            landmarkStore.Add(landmark);

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.PeakVolume,
                BarIndex = current.BarIndex,
                Direction = direction,
                Strength = landmark.Strength,
                Confidence = 0.50,
                ParentLandmarkId = landmark.LandmarkId,
                EffectOnDominance = 0.05,
                EffectOnDegradation = 0.00,
                EffectOnBalance = 0.00,
                EffectOnTransition = 0.00,
                EffectOnAmbiguity = 0.00
            });
        }

        private static void TryCreateHvcEvent(
            ApvaBarFeatures current,
            ApvaSequenceState sequence,
            ApvaV01LandmarkStore landmarkStore,
            List<ApvaEvent> events)
        {
            var direction = DirectionFromPolarity(current.VolumePolarity);

            if (direction == ApvaDirection.Unknown)
                return;

            var priorPeak = landmarkStore.GetHighestVolumeActive(direction);

            if (priorPeak == null)
                return;

            bool highVolume =
                current.Volume >= priorPeak.Volume * 0.80;

            bool compressedRange =
                priorPeak.Range > 0.0 &&
                current.Range <= priorPeak.Range * HvcRangeCompressionRatio;

            bool weakClose =
                GetCloseEfficiencyForDirection(current, direction) <= WeakCloseEfficiencyThreshold;

            if (!(highVolume && compressedRange && weakClose))
                return;

            var landmark = new ApvaLandmark
            {
                Type = ApvaLandmarkType.HVC,
                BarIndex = current.BarIndex,
                Time = current.Time,
                Direction = direction,
                Volume = current.Volume,
                Range = current.Range,
                CloseEfficiency = GetCloseEfficiencyForDirection(current, direction),
                SequencePhaseAtCreation = sequence != null ? sequence.Phase : ApvaSequencePhase.Unknown,
                MarketStateAtCreation = ApvaMacroState.Unknown,
                Strength = 0.70
            };

            landmarkStore.Add(landmark);

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.HVC,
                BarIndex = current.BarIndex,
                Direction = direction,
                Strength = 0.70,
                Confidence = 0.60,
                ParentLandmarkId = landmark.LandmarkId,
                EffectOnDominance = -0.05,
                EffectOnDegradation = 0.20,
                EffectOnBalance = 0.10,
                EffectOnTransition = 0.05,
                EffectOnAmbiguity = 0.10
            });
        }

        private static void TryCreateFailedContinuationEvent(
            ApvaBarFeatures current,
            ApvaBarFeatures prior,
            ApvaSequenceState sequence,
            ApvaStateSnapshot priorState,
            List<ApvaEvent> events)
        {
            if (prior == null || sequence == null || priorState == null)
                return;

            if (priorState.MacroState != ApvaMacroState.Directional &&
                priorState.MacroState != ApvaMacroState.Degrading)
                return;

            var expectedDirection = sequence.Direction;

            if (expectedDirection == ApvaDirection.Unknown)
                return;

            bool weakResult =
                expectedDirection == ApvaDirection.Up
                    ? current.DirectionalResultUp <= 0.0
                    : current.DirectionalResultDown <= 0.0;

            bool highOverlap = current.OverlapRatio >= HighOverlapThreshold;

            bool oppositePolarity =
                DirectionFromPolarity(current.VolumePolarity) != ApvaDirection.Unknown &&
                DirectionFromPolarity(current.VolumePolarity) != expectedDirection;

            if (!(weakResult || highOverlap || oppositePolarity))
                return;

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.FailedContinuation,
                BarIndex = current.BarIndex,
                Direction = expectedDirection,
                Strength = 0.55,
                Confidence = 0.50,
                EffectOnDominance = -0.10,
                EffectOnDegradation = 0.15,
                EffectOnBalance = 0.10,
                EffectOnTransition = 0.05,
                EffectOnAmbiguity = 0.10
            });
        }

        private void TryCreateLateralSeedEvent(
		    ApvaBarFeatures current,
		    ApvaBarFeatures prior,
		    List<ApvaEvent> events)
		{
		    if (prior == null)
		        return;
		
		    bool overlapPressure =
		        current.IsIB ||
		        current.OverlapRatio >= HighOverlapThreshold;
		
		    bool compressionPressure =
		        current.BodyToRangeRatio <= 0.35 &&
		        current.OverlapRatio >= 0.50;
		
		    bool balancePressure =
		        overlapPressure || compressionPressure;
		
		    if (balancePressure)
		        balancePressureRun++;
		    else
		        balancePressureRun = 0;
		
		    if (balancePressureRun < 2)
		        return;
		
		    if (lastLateralSeedBar >= 0 &&
		        current.BarIndex - lastLateralSeedBar < 4)
		        return;
		
		    lastLateralSeedBar = current.BarIndex;
		
		    events.Add(new ApvaEvent
		    {
		        EventType = ApvaEventType.LateralSeed,
		        BarIndex = current.BarIndex,
		        Direction = ApvaDirection.Mixed,
		        Strength = 0.45,
		        Confidence = 0.50,
		        EffectOnDominance = -0.04,
		        EffectOnDegradation = 0.04,
		        EffectOnBalance = 0.16,
		        EffectOnTransition = 0.00,
		        EffectOnAmbiguity = 0.08
		    });
		}

        private static void TryCreateSfcCandidateEvent(
            ApvaBarFeatures current,
            ApvaSequenceState sequence,
            ApvaStateSnapshot priorState,
            List<ApvaEvent> events)
        {
            if (sequence == null || priorState == null)
                return;

            bool matureEnough =
                sequence.Maturity == ApvaMaturityLevel.Mature ||
                sequence.Maturity == ApvaMaturityLevel.Late ||
                sequence.Maturity == ApvaMaturityLevel.Exhausted ||
                sequence.Maturity == ApvaMaturityLevel.Resolved;

            if (!matureEnough)
                return;

            bool degradationContext =
                priorState.MacroState == ApvaMacroState.Degrading ||
                priorState.SFCStatus == "Candidate";

            if (!degradationContext)
                return;

            bool weakClose =
                GetCloseEfficiencyForDirection(current, sequence.Direction) <= WeakCloseEfficiencyThreshold;

            bool highOverlap =
                current.OverlapRatio >= HighOverlapThreshold;

            bool poorDirectionalResult =
                sequence.Direction == ApvaDirection.Up
                    ? current.DirectionalResultUp <= 0.0
                    : current.DirectionalResultDown <= 0.0;

            int evidenceCount = 0;

            if (weakClose)
                evidenceCount++;

            if (highOverlap)
                evidenceCount++;

            if (poorDirectionalResult)
                evidenceCount++;

            if (evidenceCount < 2)
                return;

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.SFCandidate,
                BarIndex = current.BarIndex,
                Direction = sequence.Direction,
                Strength = 0.45,
                Confidence = 0.40,
                EffectOnDominance = -0.05,
                EffectOnDegradation = 0.10,
                EffectOnBalance = 0.04,
                EffectOnTransition = 0.03,
                EffectOnAmbiguity = 0.06
            });
        }

        private static ApvaDirection DirectionFromPolarity(ApvaVolumePolarity polarity)
        {
            if (polarity == ApvaVolumePolarity.Black)
                return ApvaDirection.Up;

            if (polarity == ApvaVolumePolarity.Red)
                return ApvaDirection.Down;

            return ApvaDirection.Unknown;
        }

        private static double GetCloseEfficiencyForDirection(
            ApvaBarFeatures f,
            ApvaDirection direction)
        {
            if (direction == ApvaDirection.Up)
                return f.CloseEfficiencyUp;

            if (direction == ApvaDirection.Down)
                return f.CloseEfficiencyDown;

            return 0.0;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0)
                return 0.0;

            if (value > 1.0)
                return 1.0;

            return value;
        }
		
		private static void TryCreateReclaimEvents(
		    ApvaBarFeatures current,
		    ApvaBarFeatures prior,
		    ApvaSequenceState sequence,
		    ApvaStateSnapshot priorState,
		    List<ApvaEvent> events)
		{
			if (HasEvent(events, ApvaEventType.FailedContinuation))
    			return;
			
		    if (current == null ||
		        prior == null ||
		        sequence == null ||
		        priorState == null)
		        return;
			
			if (reclaimCooldownBars > 0)
    			return;
		
		    bool priorWeak =
			    priorState.SponsorState == ApvaSponsorState.Challenged ||
			    priorState.SponsorState == ApvaSponsorState.Failing ||
			    priorState.SponsorState == ApvaSponsorState.Balance ||
			    priorState.SponsorState == ApvaSponsorState.Unresolved;
		
		    if (!priorWeak)
		        return;
		
		    bool continuationDirection =
		        sequence.Direction == ApvaDirection.Up
		            ? current.DirectionalResultUp > 0.0
		            : current.DirectionalResultDown > 0.0;
		
		    bool overlapLow =
		        current.OverlapRatio < 0.40;
		
		    bool strongClose =
		        GetCloseEfficiencyForDirection(current, sequence.Direction) > 0.60;
		
		    bool usefulAuthority =
			    sequence.Direction != ApvaDirection.Unknown &&
			    sequence.Direction != ApvaDirection.Mixed;
			
			bool meaningfulDirection =
			    continuationDirection &&
			    GetCloseEfficiencyForDirection(current, sequence.Direction) > 0.45;
			
			bool meaningfulClose =
			    strongClose &&
			    current.OverlapRatio < 0.65;
			
			bool reclaimAttempt =
			    usefulAuthority &&
			    meaningfulDirection;
		
		    if (!reclaimAttempt)
		        return;
		
		    events.Add(new ApvaEvent
		    {
		        EventType = ApvaEventType.ReclaimAttempt,
		        BarIndex = current.BarIndex,
		        Direction = sequence.Direction,
		        Strength = 0.40,
		        Confidence = 0.40,
		        EffectOnDominance = 0.04,
		        EffectOnDegradation = -0.02,
		        EffectOnBalance = -0.01,
		        EffectOnTransition = -0.01,
		        EffectOnAmbiguity = -0.02
		    });
		
		    bool accepted =
		        continuationDirection &&
		        overlapLow &&
		        strongClose;
		
		    /*if (accepted)
		    {
		        events.Add(new ApvaEvent
		        {
		            EventType = ApvaEventType.AcceptedReclaim,
		            BarIndex = current.BarIndex,
		            Direction = sequence.Direction,
		            Strength = 0.75,
		            Confidence = 0.70,
		            EffectOnDominance = 0.15,
		            EffectOnDegradation = -0.10,
		            EffectOnBalance = -0.08,
		            EffectOnTransition = -0.06,
		            EffectOnAmbiguity = -0.10
		        });
		
		        return;
		    }*/
		
		    /*bool rejected =
		        current.OverlapRatio >= 0.70 ||
		        (sequence.Direction == ApvaDirection.Up
		            ? current.DirectionalResultUp <= 0.0
		            : current.DirectionalResultDown <= 0.0);
		
		    if (rejected)
		    {
		        events.Add(new ApvaEvent
		        {
		            EventType = ApvaEventType.RejectedReclaim,
		            BarIndex = current.BarIndex,
		            Direction = sequence.Direction,
		            Strength = 0.60,
		            Confidence = 0.60,
		            EffectOnDominance = -0.08,
		            EffectOnDegradation = 0.10,
		            EffectOnBalance = 0.06,
		            EffectOnTransition = 0.06,
		            EffectOnAmbiguity = 0.08
		        });
		    }*/
		}
		
		private static bool HasEvent(
		    System.Collections.Generic.List<ApvaEvent> events,
		    ApvaEventType eventType)
		{
		    if (events == null)
		        return false;
		
		    foreach (var e in events)
		    {
		        if (e.EventType == eventType)
		            return true;
		    }
		
		    return false;
		}
    }
}


























