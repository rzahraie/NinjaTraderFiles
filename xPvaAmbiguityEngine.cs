#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaGrammarState
    {
        Unknown,
        Clear,
        Ambiguous
    }

    public sealed class xPvaAmbiguityObservation
    {
        public int BarIndex { get; internal set; }
        public DateTime Time { get; internal set; }
        public int SourceSequenceId { get; internal set; }

        public xPvaGrammarState GrammarState { get; internal set; }
        public xPvaBinaryState BinaryState { get; internal set; }
        public xPvaStructureState StructureState { get; internal set; }
        public xPvaDominanceRank DominanceRank { get; internal set; }
        public xPvaDominanceSide DominanceSide { get; internal set; }

        public bool HasSpecialContext { get; internal set; }
        public bool HasLayerDisagreement { get; internal set; }
        public bool HasNonDominantDominance { get; internal set; }
        public bool HasUnknownBinary { get; internal set; }
        public bool HasUnknownStructure { get; internal set; }
        public bool HasCompletionContext { get; internal set; }

        public int ConsecutiveClearCount { get; internal set; }
        public int ConsecutiveAmbiguousCount { get; internal set; }
        public int ConsecutiveUnknownCount { get; internal set; }

        public string Reason { get; internal set; }

        public string Label
        {
            get
            {
                string s = GrammarState.ToString();
                if (HasSpecialContext) s += " Special";
                if (HasLayerDisagreement) s += " Disagree";
                if (!string.IsNullOrEmpty(Reason)) s += " - " + Reason;
                return s;
            }
        }

        internal xPvaAmbiguityObservation Clone()
        {
            return (xPvaAmbiguityObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 6 ambiguity layer.
    ///
    /// This layer does not create Change or Continue. It only describes how clear
    /// the current grammar appears after the lower layers have spoken.
    ///
    /// Rule #4:
    /// A difficult chart is one where volume grammar is difficult.
    ///
    /// Therefore, Clear/Ambiguous/Unknown is derived from agreement or conflict
    /// among dominance, binary, and structure. Special descriptive bars such as
    /// peak-volume/compressed-range and volume/range mismatch increase ambiguity
    /// when they occur before the binary/structure message has resolved.
    /// </summary>
    public sealed class xPvaAmbiguityEngine
    {
        private int consecutiveClearCount;
        private int consecutiveAmbiguousCount;
        private int consecutiveUnknownCount;
        private xPvaAmbiguityObservation lastObservation;

        public xPvaAmbiguityObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaAmbiguityObservation Evaluate(
            xPvaDominanceObservation dominance,
            xPvaBinaryObservation binary,
            xPvaStructureObservation structure)
        {
            if (dominance == null || binary == null || structure == null)
                return null;

            var obs = new xPvaAmbiguityObservation
            {
                BarIndex = structure.BarIndex,
                Time = structure.Time,
                SourceSequenceId = structure.SourceSequenceId,
                BinaryState = binary.BinaryState,
                StructureState = structure.StructureState,
                DominanceRank = dominance.Rank,
                DominanceSide = dominance.Side,
                HasSpecialContext = structure.IsSpecialPeakCompressed || structure.IsVolumeRangeMismatch,
                HasNonDominantDominance = dominance.Rank == xPvaDominanceRank.NonDominantParticipation,
                HasUnknownBinary = binary.BinaryState == xPvaBinaryState.Unknown,
                HasUnknownStructure = structure.StructureState == xPvaStructureState.Unknown,
                HasCompletionContext = structure.StructureState == xPvaStructureState.CompletingCurrentObject
            };

            bool resolvedContinue = binary.BinaryState == xPvaBinaryState.Continue
                && (structure.StructureState == xPvaStructureState.BuildingCurrentObject
                    || structure.StructureState == xPvaStructureState.ContinuingCurrentObject);

            bool resolvedChange = binary.BinaryState == xPvaBinaryState.Change
                && structure.StructureState == xPvaStructureState.BuildingNewObject;

            bool dominantEvidence = dominance.Rank == xPvaDominanceRank.BuildingDominance
                || dominance.Rank == xPvaDominanceRank.EstablishedDominance;

            bool unresolved = obs.HasUnknownBinary || obs.HasUnknownStructure;
            bool specialBeforeResolution = obs.HasSpecialContext && !resolvedChange && !resolvedContinue;
            bool completionNotResolved = obs.HasCompletionContext && binary.BinaryState != xPvaBinaryState.Change;

            obs.HasLayerDisagreement =
                (binary.BinaryState == xPvaBinaryState.Change && structure.StructureState != xPvaStructureState.BuildingNewObject)
                || (binary.BinaryState == xPvaBinaryState.Continue && structure.StructureState == xPvaStructureState.BuildingNewObject)
                || (dominantEvidence && structure.StructureState == xPvaStructureState.Unknown)
                || (obs.HasNonDominantDominance && binary.BinaryState != xPvaBinaryState.Unknown);

            if (resolvedChange && dominantEvidence && !obs.HasLayerDisagreement)
            {
                obs.GrammarState = xPvaGrammarState.Clear;
                obs.Reason = "binary change and new object agree";
            }
            else if (resolvedContinue && dominantEvidence && !obs.HasLayerDisagreement && !structure.IsSpecialPeakCompressed)
            {
                obs.GrammarState = xPvaGrammarState.Clear;
                obs.Reason = "dominance, binary, and structure agree on current object";
            }
            else if (unresolved || specialBeforeResolution || completionNotResolved || obs.HasLayerDisagreement || obs.HasNonDominantDominance)
            {
                obs.GrammarState = xPvaGrammarState.Ambiguous;

                if (obs.HasLayerDisagreement)
                    obs.Reason = "lower layers disagree";
                else if (obs.HasNonDominantDominance)
                    obs.Reason = "non-dominant participation; grammar not decisive";
                else if (completionNotResolved)
                    obs.Reason = "completion context without resolved change";
                else if (specialBeforeResolution)
                    obs.Reason = "special context before binary/structure resolution";
                else
                    obs.Reason = "unknown binary or structure";
            }
            else
            {
                obs.GrammarState = xPvaGrammarState.Unknown;
                obs.Reason = "insufficient grammar evidence";
            }

            UpdateCounters(obs);
            lastObservation = obs.Clone();
            return obs;
        }

        private void UpdateCounters(xPvaAmbiguityObservation obs)
        {
            if (obs.GrammarState == xPvaGrammarState.Clear)
            {
                consecutiveClearCount++;
                consecutiveAmbiguousCount = 0;
                consecutiveUnknownCount = 0;
            }
            else if (obs.GrammarState == xPvaGrammarState.Ambiguous)
            {
                consecutiveClearCount = 0;
                consecutiveAmbiguousCount++;
                consecutiveUnknownCount = 0;
            }
            else
            {
                consecutiveClearCount = 0;
                consecutiveAmbiguousCount = 0;
                consecutiveUnknownCount++;
            }

            obs.ConsecutiveClearCount = consecutiveClearCount;
            obs.ConsecutiveAmbiguousCount = consecutiveAmbiguousCount;
            obs.ConsecutiveUnknownCount = consecutiveUnknownCount;
        }
    }
}
