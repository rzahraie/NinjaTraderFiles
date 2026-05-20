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

            var events = new List<ApvaEvent>();

            TryCreatePeakVolumeEvent(current, sequence, landmarkStore, events);
            TryCreateHvcEvent(current, sequence, landmarkStore, events);
            TryCreateFailedContinuationEvent(current, prior, sequence, priorState, events);
            TryCreateLateralSeedEvent(current, prior, events);
            TryCreateSfcCandidateEvent(current, sequence, priorState, events);

            return events;
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

        private static void TryCreateLateralSeedEvent(
            ApvaBarFeatures current,
            ApvaBarFeatures prior,
            List<ApvaEvent> events)
        {
            if (prior == null)
                return;

            bool compressionOrInside =
                current.IsIB ||
                current.OverlapRatio >= HighOverlapThreshold;

            if (!compressionOrInside)
                return;

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.LateralSeed,
                BarIndex = current.BarIndex,
                Direction = ApvaDirection.Mixed,
                Strength = 0.40,
                Confidence = 0.40,
                EffectOnDominance = -0.03,
                EffectOnDegradation = 0.05,
                EffectOnBalance = 0.15,
                EffectOnTransition = 0.00,
                EffectOnAmbiguity = 0.10
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
                priorState.MacroState == ApvaMacroState.Directional ||
                priorState.MacroState == ApvaMacroState.Degrading;

            if (!degradationContext)
                return;

            bool weakClose =
                GetCloseEfficiencyForDirection(current, sequence.Direction) <= WeakCloseEfficiencyThreshold;

            bool highOverlap =
                current.OverlapRatio >= HighOverlapThreshold;

            if (!(weakClose || highOverlap))
                return;

            events.Add(new ApvaEvent
            {
                EventType = ApvaEventType.SFCandidate,
                BarIndex = current.BarIndex,
                Direction = sequence.Direction,
                Strength = 0.50,
                Confidence = 0.45,
                EffectOnDominance = -0.08,
                EffectOnDegradation = 0.15,
                EffectOnBalance = 0.05,
                EffectOnTransition = 0.05,
                EffectOnAmbiguity = 0.10
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
    }
}