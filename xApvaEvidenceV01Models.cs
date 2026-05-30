using System;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    // Instrumentation-only contracts for the APVA Evidence v0.1 exporter.
    // These types intentionally contain no feature logic or legacy macro ontology.
    public enum ApvaEvidenceGeometry
    {
        Unknown,
        HHHL,
        LLLH,
        InsideBar,
        OutsideBar,
        Symmetric,
        FTP,
        FBP,
        StitchHigh,
        StitchLow,
        Reversal
    }

    public enum ApvaEvidencePolarity
    {
        Unknown,
        Black,
        Red,
        Neutral
    }

    public enum ApvaEvidenceDeltaState
    {
        Unknown,
        Up,
        Down,
        Flat
    }

    public enum ApvaParticipationState
    {
        Unknown,
        Silent,
        Normal,
        Rising,
        Falling,
        Peak,
        Climactic,
        Dissipating
    }

    public enum ApvaExpansionState
    {
        Unknown,
        Absent,
        Local,
        Strong,
        Failed,
        Climactic
    }

    public enum ApvaCompressionState
    {
        Unknown,
        Absent,
        Local,
        Clustered,
        Lateral,
        Resolving,
        FailedResolution
    }

    public enum ApvaDissipationState
    {
        Unknown,
        Absent,
        Local,
        Repeated,
        Strong,
        Climactic
    }

    public enum ApvaAcceptanceState
    {
        Unknown,
        Accepted,
        Rejected,
        Contained,
        Unresolved
    }

    public enum ApvaSignificanceState
    {
        Unknown,
        Minor,
        Moderate,
        Major,
        Structural,
        SessionDefining
    }

    // One exported evidence observation for a completed bar.
    public sealed class ApvaEvidenceRow
    {
        public int BarIndex { get; set; }
        public DateTime Time { get; set; }

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }

        public ApvaEvidenceGeometry Geometry { get; set; }
        public ApvaEvidencePolarity VolumePolarity { get; set; }

        public ApvaEvidenceDeltaState VolumeDelta { get; set; }
        public double VolumeRank20 { get; set; }
        public double VolumeRank50 { get; set; }

        public double Range { get; set; }
        public ApvaEvidenceDeltaState RangeDelta { get; set; }
        public double RangeRank20 { get; set; }

        public double Body { get; set; }
        public ApvaEvidenceDeltaState BodyDelta { get; set; }
        public double BodyRank20 { get; set; }

        public double CloseLocation { get; set; }
        public double OverlapRatio { get; set; }

        public bool CloseInsidePriorRange { get; set; }
        public bool CloseInsidePriorBody { get; set; }
        public bool BreaksPriorHigh { get; set; }
        public bool BreaksPriorLow { get; set; }

        public ApvaParticipationState ParticipationState { get; set; }
        public ApvaExpansionState ExpansionState { get; set; }
        public ApvaCompressionState CompressionState { get; set; }
        public ApvaDissipationState DissipationState { get; set; }
        public ApvaAcceptanceState AcceptanceState { get; set; }
        public ApvaSignificanceState SignificanceState { get; set; }

        public string EvidenceFlags { get; set; }
    }
}
