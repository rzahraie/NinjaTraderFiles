#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaBinaryState
    {
        Unknown,
        Continue,
        Change
    }

    public enum xPvaStructureState
    {
        Unknown,
        BuildingCurrentObject,
        ContinuingCurrentObject,
        CompletingCurrentObject,
        BuildingNewObject
    }

    public sealed class xPvaBinaryObservation
    {
        public int BarIndex { get; internal set; }
        public DateTime Time { get; internal set; }
        public int SourceSequenceId { get; internal set; }

        public xPvaDominanceSide CurrentSide { get; internal set; }
        public xPvaDominanceRank CurrentRank { get; internal set; }
        public xPvaDominanceSide PriorSignificantSide { get; internal set; }

        public xPvaStructureState StructureState { get; internal set; }
        public xPvaBinaryState BinaryState { get; internal set; }

        public bool IsSpecialPeakCompressed { get; internal set; }
        public bool IsVolumeRangeMismatch { get; internal set; }
        public bool IsConservativeUnknown { get; internal set; }

        public string Reason { get; internal set; }

        public string Label
        {
            get
            {
                string s = BinaryState.ToString() + " / " + StructureState.ToString();
                if (IsSpecialPeakCompressed) s += " PVC";
                if (IsVolumeRangeMismatch) s += " VRM";
                if (!string.IsNullOrEmpty(Reason)) s += " - " + Reason;
                return s;
            }
        }

        internal xPvaBinaryObservation Clone()
        {
            return (xPvaBinaryObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 4 binary layer.
    /// This class is intentionally conservative. It does not treat peak volume,
    /// compressed range, volume/range mismatch, or gradient-like behavior as a
    /// Change/Continue decision. Those are descriptive/supporting observations.
    ///
    /// Its only job is to keep a running binary hypothesis:
    /// Unknown, Continue, or Change.
    ///
    /// The decision is based on contextual dominance transition, not raw speed,
    /// size, or gradient. Unknown is a valid and expected output.
    /// </summary>
    public sealed class xPvaBinaryEngine
    {
        private xPvaDominanceObservation lastSignificantDominance;
        private xPvaBinaryObservation lastObservation;

        public xPvaBinaryObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaDominanceObservation LastSignificantDominance
        {
            get { return lastSignificantDominance == null ? null : lastSignificantDominance.Clone(); }
        }

        public xPvaBinaryObservation Evaluate(xPvaDominanceObservation dominance)
        {
            if (dominance == null)
                return null;

            bool significant = IsSignificant(dominance);
            bool specialPeakCompressed = dominance.ContainsPeakVolume && dominance.ContainsCompressedRange;
            bool sameAsPrior = lastSignificantDominance != null && dominance.Side == lastSignificantDominance.Side;
            bool oppositePrior = lastSignificantDominance != null
                              && dominance.Side != xPvaDominanceSide.Neutral
                              && lastSignificantDominance.Side != xPvaDominanceSide.Neutral
                              && dominance.Side != lastSignificantDominance.Side;

            var obs = new xPvaBinaryObservation
            {
                BarIndex = dominance.EndBar,
                Time = dominance.EndTime,
                SourceSequenceId = dominance.SequenceId,
                CurrentSide = dominance.Side,
                CurrentRank = dominance.Rank,
                PriorSignificantSide = lastSignificantDominance == null ? xPvaDominanceSide.Neutral : lastSignificantDominance.Side,
                IsSpecialPeakCompressed = specialPeakCompressed,
                IsVolumeRangeMismatch = dominance.ContainsVolumeRangeMismatch
            };

            if (!significant)
            {
                obs.BinaryState = xPvaBinaryState.Unknown;
                obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                obs.IsConservativeUnknown = true;
                obs.Reason = "non-dominant participation; no binary decision";
            }
            else if (lastSignificantDominance == null)
            {
                obs.BinaryState = xPvaBinaryState.Continue;
                obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                obs.Reason = "first significant dominance observed";
            }
            else if (sameAsPrior)
            {
                obs.BinaryState = xPvaBinaryState.Continue;
                obs.StructureState = specialPeakCompressed
                    ? xPvaStructureState.CompletingCurrentObject
                    : xPvaStructureState.ContinuingCurrentObject;
                obs.Reason = specialPeakCompressed
                    ? "same-side dominance with peak/compressed context"
                    : "same-side significant dominance";
            }
            else if (oppositePrior)
            {
                obs.BinaryState = xPvaBinaryState.Change;
                obs.StructureState = xPvaStructureState.BuildingNewObject;
                obs.Reason = "opposite significant dominance after prior significant side";
            }
            else
            {
                obs.BinaryState = xPvaBinaryState.Unknown;
                obs.StructureState = xPvaStructureState.Unknown;
                obs.IsConservativeUnknown = true;
                obs.Reason = "insufficient context";
            }

            if (significant)
                lastSignificantDominance = dominance.Clone();

            lastObservation = obs.Clone();
            return obs;
        }

        private bool IsSignificant(xPvaDominanceObservation dominance)
        {
            if (dominance == null)
                return false;

            if (dominance.Side == xPvaDominanceSide.Neutral)
                return false;

            return dominance.Rank == xPvaDominanceRank.BuildingDominance
                || dominance.Rank == xPvaDominanceRank.EstablishedDominance;
        }
    }
}
