namespace APVA.Core
{
    public enum VolumeColor
    {
        Neutral,
        Black,
        Red
    }

    public enum TwoBarType
    {
        Unknown,
        HHHL,
        LHLL,
        IB,
        OB,
        SH,
        SL,
        FTP,
        FBP,
        SHSL
    }

    public enum SegmentDirection
    {
        Unknown,
        Up,
        Down,
        Sideways
    }

    public enum VolumeRank
    {
        Unknown,
        Low,
        Normal,
        Elevated,
        Peak,
        Climax
    }

    public enum VolumePhase
    {
        Unknown,
        PP1,
        PP2,
        PP3,
        T1,
        T2P,
        T2F
    }

    public enum DominanceState
    {
        Unknown,
        Dominant,
        NonDominant,
        CounterDominant,
        Exhaustion
    }

    public enum ContainerDirection
    {
        Unknown,
        Up,
        Down
    }
	
	public enum FttKind
	{
	    None,
		StructuralWarning,
	    T2F_FailedContinuation,
	    CounterDominantShock,
	    PriceBoundaryBreak
	}

    public sealed class Bar
    {
        public int Index { get; set; }
        public System.DateTime Time { get; set; }

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }

        public double Volume { get; set; }
    }

    public sealed class ClassifiedBar
    {
        public int Index { get; set; }

        public TwoBarType TwoBarType { get; set; } = TwoBarType.Unknown;
        public VolumeColor VolumeColor { get; set; } = VolumeColor.Neutral;
    }

    public sealed class VolumeSegment
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }

        public VolumeColor Color { get; set; }
        public SegmentDirection Direction { get; set; }

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }

        public double TotalVolume { get; set; }

        public int BarCount
        {
            get { return EndIndex - StartIndex + 1; }
        }

        public double AverageVolume
        {
            get { return BarCount > 0 ? TotalVolume / BarCount : 0.0; }
        }

        public VolumeRank Rank { get; set; } = VolumeRank.Unknown;
        public VolumePhase Phase { get; set; } = VolumePhase.Unknown;
        public DominanceState Dominance { get; set; } = DominanceState.Unknown;
    }
}

