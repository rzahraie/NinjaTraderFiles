using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public enum ApvaDirection
    {
        Unknown,
        Up,
        Down,
        Mixed
    }

    public enum ApvaVolumePolarity
    {
        Neutral,
        Black,
        Red
    }

    public enum ApvaSequencePhase
    {
        Unknown,
        B2B,
        R2R,
        TwoB,
        TwoR,
        HalfCycle
    }

    public enum ApvaMacroState
    {
        Unknown,
        Directional,
        Degrading,
        Balance,
        TransitionAttempt,
        Unresolved
    }

    public enum ApvaMaturityLevel
    {
        Unknown,
        Early,
        Developing,
        Mature,
        Late,
        Exhausted,
        Resolved
    }

    public enum ApvaLandmarkType
    {
        PeakVolume,
        Expansion,
        HVC,
        Absorption,
        LateralSeed,
        FailedContinuation,
        FBO,
        SFCandidate
    }

    public enum ApvaEventType
    {
        None,
        PeakVolume,
        HVC,
        FailedContinuation,
        LateralSeed,
        FBO,
        SFCandidate,
        DominanceReassertion,
        SponsorshipTransfer,
        FailedTransition,
		ContinuationAttempt,
		AcceptedContinuation,
		RejectedContinuation,
		ReclaimAttempt,
		AcceptedReclaim,
		RejectedReclaim
    }
	
	public enum ApvaSponsorState
	{
	    Unknown,
	
	    Dominant,
	    Pressured,
	    Challenged,
	    Failing,
	
		ReclaimAttempt,
	    Reasserting,
		FailedReclaim,
	    Transferred,
	
	    Balance,
	    Unresolved
	}

    public sealed class ApvaBarFeatures
    {
        public int BarIndex { get; set; }
        public DateTime Time { get; set; }

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }

        public string PriceCase { get; set; } = "Unknown";
        public ApvaVolumePolarity VolumePolarity { get; set; }

        public double Range { get; set; }
        public double Body { get; set; }
        public double BodyToRangeRatio { get; set; }

        public double CloseEfficiencyUp { get; set; }
        public double CloseEfficiencyDown { get; set; }

        public double OverlapWithPrior { get; set; }
        public double OverlapRatio { get; set; }

        public bool IsIB { get; set; }
        public bool IsOB { get; set; }
        public bool IsStitch { get; set; }
        public bool IsReversal { get; set; }
        public bool IsDoji { get; set; }

        public double DirectionalResultUp { get; set; }
        public double DirectionalResultDown { get; set; }
    }

    public sealed class ApvaSequenceState
    {
        public int SequenceId { get; set; }

        public ApvaDirection Direction { get; set; }
        public ApvaSequencePhase Phase { get; set; }

        public int StartBar { get; set; }
        public int CurrentBar { get; set; }

        public int? P1Bar { get; set; }
        public double P1Volume { get; set; }
        public double P1Range { get; set; }

        public int? P2Bar { get; set; }
        public double P2Volume { get; set; }
        public double P2Range { get; set; }

        public double PeakRatio { get; set; }
        public ApvaMaturityLevel Maturity { get; set; }

        public double AuthorityScore { get; set; }

        public bool IsComplete { get; set; }
        public bool IsFailed { get; set; }
    }

    public sealed class ApvaLandmark
    {
        public Guid LandmarkId { get; set; } = Guid.NewGuid();

        public ApvaLandmarkType Type { get; set; }
        public int BarIndex { get; set; }
        public DateTime Time { get; set; }

        public ApvaDirection Direction { get; set; }

        public double Volume { get; set; }
        public double Range { get; set; }
        public double CloseEfficiency { get; set; }

        public ApvaSequencePhase SequencePhaseAtCreation { get; set; }
        public ApvaMacroState MarketStateAtCreation { get; set; }

        public double Strength { get; set; }
        public int Age { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsConfirmed { get; set; }
        public bool IsInvalidated { get; set; }
    }

    public sealed class ApvaEvent
    {
        public ApvaEventType EventType { get; set; }
        public int BarIndex { get; set; }
        public ApvaDirection Direction { get; set; }

        public double Strength { get; set; }
        public double Confidence { get; set; }

        public Guid? ParentLandmarkId { get; set; }

        public double EffectOnDominance { get; set; }
        public double EffectOnDegradation { get; set; }
        public double EffectOnBalance { get; set; }
        public double EffectOnTransition { get; set; }
        public double EffectOnAmbiguity { get; set; }
    }

    public sealed class ApvaScores
    {
        public double DominanceScore { get; set; }
        public double DegradationScore { get; set; }
        public double BalanceScore { get; set; }
        public double TransitionScore { get; set; }
        public double AmbiguityScore { get; set; }
		public double CompressionScore { get; set; }
		public double ExpansionPressure { get; set; }
    }

    public sealed class ApvaStateSnapshot
    {
        public int BarIndex { get; set; }
        public DateTime Time { get; set; }

        public ApvaMacroState MacroState { get; set; }
        public ApvaDirection ActiveDirection { get; set; }

        public ApvaScores Scores { get; set; } = new ApvaScores();

        public ApvaSequencePhase SequencePhase { get; set; }
        public double SequenceAuthority { get; set; }
        public ApvaMaturityLevel MaturityLevel { get; set; }

        public string SFCStatus { get; set; } = "None";
        public string ExpectedNextBehavior { get; set; } = string.Empty;
        public string InvalidationCondition { get; set; } = string.Empty;
		
		public ApvaSponsorState SponsorState { get; set; }
		public double SponsorConfidence { get; set; }
		
		public List<ApvaEvent> Events { get; set; } = new List<ApvaEvent>();
    }

    public sealed class ApvaContext
    {
        public ApvaBarFeatures CurrentFeatures { get; set; }
        public ApvaBarFeatures PriorFeatures { get; set; }

        public ApvaSequenceState ActiveSequence { get; set; }
        public List<ApvaLandmark> ActiveLandmarks { get; set; } = new List<ApvaLandmark>();
        public List<ApvaEvent> RecentEvents { get; set; } = new List<ApvaEvent>();

        public ApvaStateSnapshot CurrentState { get; set; }
        public ApvaStateSnapshot PriorState { get; set; }

        public string Regime { get; set; } = "Unknown";
		
		public ApvaSponsorState PriorSponsorState { get; set; }
    }
	
	public sealed class ApvaSponsorCandidate
	{
	    public ApvaSponsorState State { get; set; }
	    public double Confidence { get; set; }
	    public int Priority { get; set; }
	    public string Reason { get; set; }
	}
}









