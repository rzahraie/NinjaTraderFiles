#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaDominanceSide
    {
        Neutral,
        Black,
        Red
    }

    public enum xPvaDominanceRank
    {
        Unknown,
        Neutral,
        NonDominantParticipation,
        BuildingDominance,
        EstablishedDominance
    }

    public sealed class xPvaDominanceObservation
    {
        public int SequenceId { get; internal set; }
        public int StartBar { get; internal set; }
        public int EndBar { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public DateTime EndTime { get; internal set; }

        public xPvaDominanceSide Side { get; internal set; }
        public xPvaDominanceRank Rank { get; internal set; }
        public xPvaSequenceKind SourceSequenceKind { get; internal set; }

        public int BarCount { get; internal set; }
        public int PlusCount { get; internal set; }
        public int MinusCount { get; internal set; }
        public bool HasMinusToPlusTransition { get; internal set; }
        public bool HasPlusToMinusTransition { get; internal set; }
        public bool ContainsPeakVolume { get; internal set; }
        public bool ContainsCompressedRange { get; internal set; }
        public bool ContainsVolumeRangeMismatch { get; internal set; }

        public string ContextLabel { get; internal set; }

        public string Label
        {
            get
            {
                string s = ContextLabel;
                s += " " + Rank.ToString();
                if (HasMinusToPlusTransition) s += " M2P";
                if (HasPlusToMinusTransition) s += " P2M";
                if (ContainsPeakVolume) s += " PV";
                if (ContainsCompressedRange) s += " CR";
                if (ContainsVolumeRangeMismatch) s += " VRM";
                return s;
            }
        }

        internal xPvaDominanceObservation Clone()
        {
            return (xPvaDominanceObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 3 dominance layer.
    /// This does not build cycles, Gaussians, containers, FTTs, nodes, or trade signals.
    /// It translates same-polarity event sequences into contextual dominance observations.
    /// Uppercase labels indicate established/building dominance; lowercase labels indicate
    /// non-dominant participation within the current evidence available to this layer.
    /// </summary>
    public sealed class xPvaDominanceTracker
    {
        private xPvaDominanceObservation lastObservation;

        public xPvaDominanceObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaDominanceObservation Evaluate(xPvaEventSequence seq)
        {
            if (seq == null)
                return null;

            var obs = new xPvaDominanceObservation
            {
                SequenceId = seq.SequenceId,
                StartBar = seq.StartBar,
                EndBar = seq.EndBar,
                StartTime = seq.StartTime,
                EndTime = seq.EndTime,
                SourceSequenceKind = seq.Kind,
                BarCount = seq.BarCount,
                PlusCount = seq.PlusCount,
                MinusCount = seq.MinusCount,
                HasMinusToPlusTransition = seq.HasMinusToPlusTransition,
                HasPlusToMinusTransition = seq.HasPlusToMinusTransition,
                ContainsPeakVolume = seq.ContainsPeakVolume,
                ContainsCompressedRange = seq.ContainsCompressedRange,
                ContainsVolumeRangeMismatch = seq.ContainsVolumeRangeMismatch
            };

            obs.Side = MapSide(seq.Polarity);
            obs.Rank = ClassifyRank(seq);
            obs.ContextLabel = BuildContextLabel(seq, obs.Rank);

            lastObservation = obs.Clone();
            return obs;
        }

        private xPvaDominanceSide MapSide(xPvaVolumePolarity polarity)
        {
            if (polarity == xPvaVolumePolarity.B) return xPvaDominanceSide.Black;
            if (polarity == xPvaVolumePolarity.R) return xPvaDominanceSide.Red;
            return xPvaDominanceSide.Neutral;
        }

        private xPvaDominanceRank ClassifyRank(xPvaEventSequence seq)
        {
            if (seq.Polarity == xPvaVolumePolarity.Neutral)
                return xPvaDominanceRank.Neutral;

            if (seq.BarCount <= 0)
                return xPvaDominanceRank.Unknown;

            if (seq.BarCount == 1)
                return xPvaDominanceRank.NonDominantParticipation;

            // The common Spyder pattern is not simply R+ R+ or B+ B+.
            // It is often R- -> R+ or B- -> B+, sometimes after multiple minus bars.
            if (seq.HasMinusToPlusTransition)
                return xPvaDominanceRank.BuildingDominance;

            if (seq.PlusCount >= 2)
                return xPvaDominanceRank.EstablishedDominance;

            if (seq.PlusCount == 1 && seq.MinusCount >= 1)
                return xPvaDominanceRank.BuildingDominance;

            return xPvaDominanceRank.NonDominantParticipation;
        }

        private string BuildContextLabel(xPvaEventSequence seq, xPvaDominanceRank rank)
        {
            bool dominant = rank == xPvaDominanceRank.BuildingDominance
                         || rank == xPvaDominanceRank.EstablishedDominance;

            if (seq.Polarity == xPvaVolumePolarity.Neutral)
                return "neutral";

            if (seq.BarCount <= 1)
            {
                string letter = seq.Polarity == xPvaVolumePolarity.B ? "b" : "r";
                string sign = seq.LastVolumeChange == xPvaVolumeChange.Plus ? "+" : seq.LastVolumeChange == xPvaVolumeChange.Minus ? "-" : "=";
                if (dominant) letter = letter.ToUpper();
                return letter + sign;
            }

            switch (seq.Kind)
            {
                case xPvaSequenceKind.CandidateB2B:
                    return dominant ? "B2B" : "b2b";

                case xPvaSequenceKind.CandidateR2R:
                    return dominant ? "R2R" : "r2r";

                case xPvaSequenceKind.Candidate2B:
                    return dominant ? "2B" : "2b";

                case xPvaSequenceKind.Candidate2R:
                    return dominant ? "2R" : "2r";

                case xPvaSequenceKind.SingleB:
                    return dominant ? "B2B?" : "b2b?";

                case xPvaSequenceKind.SingleR:
                    return dominant ? "R2R?" : "r2r?";

                default:
                    if (seq.Polarity == xPvaVolumePolarity.B)
                        return dominant ? "B2B?" : "b2b?";
                    if (seq.Polarity == xPvaVolumePolarity.R)
                        return dominant ? "R2R?" : "r2r?";
                    return "unknown";
            }
        }
    }
}
