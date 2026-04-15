using System;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public enum PricePolarity
    {
        Unknown = 0,
        Black = 1,
        Red = 2,
        Doji = 3
    }

    public enum VolumeBehavior
    {
        Unknown = 0,
        Expanding = 1,
        Contracting = 2,
        Flat = 3
    }

    public enum DirectionContext
    {
        Unknown = 0,
        Up = 1,
        Down = 2,
        Neutral = 3
    }

    public enum DominanceState
    {
        Unknown = 0,
        Dominant = 1,
        NonDominant = 2
    }

    public enum LateralStateKind
    {
        None = 0,
        Seeding = 1,
        Active = 2,
        BrokenUp = 3,
        BrokenDown = 4
    }

    public enum LateralBias
    {
        Unknown = 0,
        Up = 1,
        Down = 2,
        Neutral = 3
    }

    public enum SignalPhase
    {
        None = 0,
        LongCandidate = 1,
        ShortCandidate = 2,
        LongValid = 3,
        ShortValid = 4,
        LongDegrading = 5,
        ShortDegrading = 6,
        Invalidated = 7
    }

    public enum ExecutionIntent
    {
        None = 0,
        EnterLong = 1,
        EnterShort = 2,
        HoldLong = 3,
        HoldShort = 4,
        ExitLong = 5,
        ExitShort = 6,
        ReverseToLong = 7,
        ReverseToShort = 8,
        StandAside = 9
    }
	
	public readonly struct BarSnapshot
    {
        public readonly DateTime TimeUtc;
        public readonly double O;
        public readonly double H;
        public readonly double L;
        public readonly double C;
        public readonly long V;
        public readonly int Index;

        public BarSnapshot(
            DateTime timeUtc,
            double open,
            double high,
            double low,
            double close,
            long volume,
            int index)
        {
            TimeUtc = timeUtc;
            O = open;
            H = high;
            L = low;
            C = close;
            V = volume;
            Index = index;
        }
    }
}

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum Sentiment
    {
        Unknown = 0,
        Long = 1,
        Short = -1,
        Neutral = 2,
    }

    public enum Dominance
    {
        Unknown = 0,
        Dom = 1,
        NonDom = -1,
    }

    public enum PriceCase
    {
        Unknown = 0,

        XB = 1,
        XR = 2,

        OUTB = 3,
        OUTR = 4,
        OUT_DOJI = 5,

        HITCH = 10,
        FTP = 11,
        FBP = 12,
        SYM = 13,
        STB = 14,
        STR = 15,
    }

    public enum VolPivotKind
    {
        Unknown = 0,
        Peak = 1,
        Trough = 2,
    }

    public enum VolOoeName
    {
        Unknown = 0,
        P1 = 1,
        T1 = 2,
        P2 = 3,
        T2P = 4,
        T2F = 5,
    }

    public enum Band
    {
        Unknown = 0,
        PP = 1,
        A = 2,
        B = 3,
        C = 4,
    }

    public enum Permission
    {
        Denied = 0,
        Granted = 1,
    }

    public enum EventKind
    {
        None = 0,
	    PriceCase = 1,
	    Permission = 2,
	    VolPivot = 3,
	    VolOoe = 4,
	    EndEffect = 5,
	    Turn = 6,
	    TrendType = 7,
	    Action = 8,
	    Container = 9,
	    DirectionBreak = 10,
	    FttCandidate = 11,
	    FttConfirmed = 12,
		Structure = 13,
		ContainerReport = 14,
		PersistentContainer = 15,
		ContainerGeometry = 16,
		TradeIntent = 17,
		ContainerGeometrySnapshot = 18,
    }
	
	public enum EndEffectKind
	{
	    Unknown = 0,
	
	    // Minimal deterministic subset
	    Prelim = 1,       // PP-style preliminary condition
	    PeakContext = 2,  // A/B/C peak-side structural condition
	    TroughContext = 3,// A/B/C trough-side structural condition
	    Continuation = 4, // structure continues
	    CandidateEnd = 5, // possible end condition
	}
	
	public enum TrendType
	{
	    Unknown = 0,
	    A = 1,
	    B = 2,
	    C = 3,
	    D = 4,
	}
	
	public enum ContainerLifecycleState
	{
	    Unknown = 0,
	    Developing = 1,
	    CandidateBreak = 2,
	    ConfirmedBreak = 3,
	    Closed = 4,
	}
	
	public readonly struct PersistentContainerEvent
	{
	    public readonly int ContainerId;
	    public readonly ContainerLifecycleState LifecycleState;
	    public readonly ContainerDirection Direction;
	
	    public readonly int StartBarIndex;
	    public readonly int LastBarIndex;
	
	    public readonly int ExtremeBarIndex;
	    public readonly double ExtremePrice;
	
	    public readonly int? BreakBarIndex;
	    public readonly int? ConfirmBarIndex;
	
	    public PersistentContainerEvent(
	        int containerId,
	        ContainerLifecycleState lifecycleState,
	        ContainerDirection direction,
	        int startBarIndex,
	        int lastBarIndex,
	        int extremeBarIndex,
	        double extremePrice,
	        int? breakBarIndex,
	        int? confirmBarIndex)
	    {
	        ContainerId = containerId;
	        LifecycleState = lifecycleState;
	        Direction = direction;
	        StartBarIndex = startBarIndex;
	        LastBarIndex = lastBarIndex;
	        ExtremeBarIndex = extremeBarIndex;
	        ExtremePrice = extremePrice;
	        BreakBarIndex = breakBarIndex;
	        ConfirmBarIndex = confirmBarIndex;
	    }
	}
	
	public readonly struct ContainerReportEvent
	{
	    public readonly int ContainerId;
	    public readonly int StartBarIndex;
	    public readonly int? DirectionBreakBarIndex;
	    public readonly int? FttCandidateBarIndex;
	    public readonly int? FttConfirmedBarIndex;
	    public readonly StructureState StructureState;
	    public readonly ActionType ActionType;
	
	    public ContainerReportEvent(
	        int containerId,
	        int startBarIndex,
	        int? directionBreakBarIndex,
	        int? fttCandidateBarIndex,
	        int? fttConfirmedBarIndex,
	        StructureState structureState,
	        ActionType actionType)
	    {
	        ContainerId = containerId;
	        StartBarIndex = startBarIndex;
	        DirectionBreakBarIndex = directionBreakBarIndex;
	        FttCandidateBarIndex = fttCandidateBarIndex;
	        FttConfirmedBarIndex = fttConfirmedBarIndex;
	        StructureState = structureState;
	        ActionType = actionType;
	    }
	}
	
	
	public readonly struct TrendTypeEvent
	{
	    public readonly int BarIndex;
	    public readonly TrendType Type;
	    public readonly TurnType SourceTurn;
	    public readonly EndEffectKind SourceKind;
	    public readonly Band Band;
	    public readonly long Value;
	
	    public TrendTypeEvent(int barIndex, TrendType type, TurnType sourceTurn, EndEffectKind sourceKind, Band band, long value)
	    {
	        BarIndex = barIndex;
	        Type = type;
	        SourceTurn = sourceTurn;
	        SourceKind = sourceKind;
	        Band = band;
	        Value = value;
	    }
	}
	
	public enum ActionType
	{
	    Unknown = 0,
	    Enter = 1,
	    Hold = 2,
	    Reverse = 3,
	    Sideline = 4,
	    StayIn = 5,
	}

	public readonly struct ActionEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly ActionType Action;
	    public readonly TrendType TrendType;
	    public readonly TurnType TurnType;
	    public readonly EndEffectKind SourceKind;
	    public readonly Band Band;
	    public readonly long Value;
	
	    public ActionEvent(int barIndex, int containerId, ActionType action, TrendType trendType, TurnType turnType, EndEffectKind sourceKind, Band band, long value)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        Action = action;
	        TrendType = trendType;
	        TurnType = turnType;
	        SourceKind = sourceKind;
	        Band = band;
	        Value = value;
	    }
	}
	
	public enum ContainerDirection
	{
	    Unknown = 0,
	    Up = 1,
	    Down = 2,
	}
	
	public readonly struct ContainerGeometryEvent
	{
	    public readonly int ContainerId;
	    public readonly ContainerDirection Direction;
	
	    public readonly int StartBarIndex;
	    public readonly int ExtremeBarIndex;
	    public readonly int ConfirmBarIndex;
	
	    public readonly double StartPrice;
	    public readonly double ExtremePrice;
	    public readonly double ConfirmPrice;
	
	    public ContainerGeometryEvent(
	        int containerId,
	        ContainerDirection direction,
	        int startBarIndex,
	        int extremeBarIndex,
	        int confirmBarIndex,
	        double startPrice,
	        double extremePrice,
	        double confirmPrice)
	    {
	        ContainerId = containerId;
	        Direction = direction;
	        StartBarIndex = startBarIndex;
	        ExtremeBarIndex = extremeBarIndex;
	        ConfirmBarIndex = confirmBarIndex;
	        StartPrice = startPrice;
	        ExtremePrice = extremePrice;
	        ConfirmPrice = confirmPrice;
	    }
	}
	
	public readonly struct DirectionBreakEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public DirectionBreakEvent(int barIndex, int containerId, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct FttCandidateEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public FttCandidateEvent(int barIndex, int containerId, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct FttConfirmedEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public FttConfirmedEvent(int barIndex, int containerId, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct ContainerEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly ContainerDirection Direction;
	    public readonly int RunLength;
	    public readonly bool IsNewContainer;
	
	    public readonly bool HasDirectionBreak;
	    public readonly DirectionBreakEvent? DirectionBreak;
	
	    public readonly bool HasFttCandidate;
	    public readonly FttCandidateEvent? FttCandidate;
	
	    public readonly bool HasFttConfirmed;
	    public readonly FttConfirmedEvent? FttConfirmed;
	
	    public ContainerEvent(
	        int barIndex,
	        int containerId,
	        ContainerDirection direction,
	        int runLength,
	        bool isNewContainer,
	        bool hasDirectionBreak,
	        DirectionBreakEvent? directionBreak,
	        bool hasFttCandidate,
	        FttCandidateEvent? fttCandidate,
	        bool hasFttConfirmed,
	        FttConfirmedEvent? fttConfirmed)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        Direction = direction;
	        RunLength = runLength;
	        IsNewContainer = isNewContainer;
	        HasDirectionBreak = hasDirectionBreak;
	        DirectionBreak = directionBreak;
	        HasFttCandidate = hasFttCandidate;
	        FttCandidate = fttCandidate;
	        HasFttConfirmed = hasFttConfirmed;
	        FttConfirmed = fttConfirmed;
	    }
	}
	
	public enum StructureState
	{
	    Unknown = 0,
	    Building = 1,
	    Transition = 2,
	    Mature = 3,
	    Broken = 4,
	}
	
	public readonly struct StructureEvent
	{
	    public readonly int BarIndex;
	    public readonly int ContainerId;
	    public readonly StructureState State;
	    public readonly TrendType TrendType;
	    public readonly ContainerDirection Direction;
	
	    public StructureEvent(
	        int barIndex,
	        int containerId,
	        StructureState state,
	        TrendType trendType,
	        ContainerDirection direction)
	    {
	        BarIndex = barIndex;
	        ContainerId = containerId;
	        State = state;
	        TrendType = trendType;
	        Direction = direction;
	    }
	}
	
	public readonly struct FttEvent
	{
	    public readonly int BarIndex;
	    public readonly ContainerDirection Direction;
	    public readonly PriceCase SourcePriceCase;
	
	    public FttEvent(int barIndex, ContainerDirection direction, PriceCase sourcePriceCase)
	    {
	        BarIndex = barIndex;
	        Direction = direction;
	        SourcePriceCase = sourcePriceCase;
	    }
	}

	public readonly struct EndEffectEvent
	{
	    public readonly int BarIndex;
	    public readonly EndEffectKind Kind;
	    public readonly Band Band;
	    public readonly VolOoeName Source;
	    public readonly long Value;
	
	    public EndEffectEvent(int barIndex, EndEffectKind kind, Band band, VolOoeName source, long value)
	    {
	        BarIndex = barIndex;
	        Kind = kind;
	        Band = band;
	        Source = source;
	        Value = value;
	    }
	}

	public enum TurnType
{
    Unknown = 0,
    A = 1, // dominant -> non-dominant
    B = 2, // non-dominant -> dominant
    C = 3, // dominant -> opposite dominant
}

	public readonly struct TurnEvent
	{
	    public readonly int BarIndex;
	    public readonly TurnType Type;
	    public readonly EndEffectKind SourceKind;
	    public readonly Band Band;
	    public readonly VolOoeName Source;
	    public readonly long Value;
	
	    public TurnEvent(int barIndex, TurnType type, EndEffectKind sourceKind, Band band, VolOoeName source, long value)
	    {
	        BarIndex = barIndex;
	        Type = type;
	        SourceKind = sourceKind;
	        Band = band;
	        Source = source;
	        Value = value;
	    }
	}
	
    public readonly struct BarSnapshot
    {
        public readonly DateTime TimeUtc;
        public readonly double O;
        public readonly double H;
        public readonly double L;
        public readonly double C;
        public readonly long V;
        public readonly int Index;

        public BarSnapshot(DateTime timeUtc, double o, double h, double l, double c, long v, int index)
        {
            TimeUtc = timeUtc;
            O = o;
            H = h;
            L = l;
            C = c;
            V = v;
            Index = index;
        }
    }

    public readonly struct PriceCaseEvent
    {
        public readonly int BarIndex;
        public readonly PriceCase Case;

        public PriceCaseEvent(int barIndex, PriceCase pc)
        {
            BarIndex = barIndex;
            Case = pc;
        }
    }

    public readonly struct PermissionEvent
    {
        public readonly int BarIndex;
        public readonly Permission Permission;
        public readonly string Reason;

        public PermissionEvent(int barIndex, Permission permission, string reason)
        {
            BarIndex = barIndex;
            Permission = permission;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct VolPivotEvent
    {
        public readonly int BarIndex;
        public readonly VolPivotKind Kind;
        public readonly long Value;

        public VolPivotEvent(int barIndex, VolPivotKind kind, long value)
        {
            BarIndex = barIndex;
            Kind = kind;
            Value = value;
        }
    }

    public readonly struct VolOoeEvent
    {
        public readonly int BarIndex;
        public readonly VolOoeName Name;
        public readonly Band Band;
        public readonly long Value;

        public VolOoeEvent(int barIndex, VolOoeName name, Band band, long value)
        {
            BarIndex = barIndex;
            Name = name;
            Band = band;
            Value = value;
        }
    }

    public readonly struct EngineEvent
	{
	    public readonly EventKind Kind;
	    public readonly int BarIndex;
	    public readonly string Text;
	
	    public readonly PriceCaseEvent? PriceCase;
	    public readonly PermissionEvent? Permission;
	    public readonly VolPivotEvent? VolPivot;
	    public readonly VolOoeEvent? VolOoe;
	    public readonly EndEffectEvent? EndEffect;
	    public readonly TurnEvent? Turn;
	    public readonly TrendTypeEvent? TrendType;
	    public readonly ActionEvent? Action;
	    public readonly ContainerEvent? Container;
	    public readonly DirectionBreakEvent? DirectionBreak;
	    public readonly FttCandidateEvent? FttCandidate;
	    public readonly FttConfirmedEvent? FttConfirmed;
	    public readonly StructureEvent? Structure;
		public readonly ContainerReportEvent? ContainerReport;
		public readonly PersistentContainerEvent? PersistentContainer;
		public readonly ContainerGeometryEvent? ContainerGeometry;
		public readonly TradeIntentEvent? TradeIntent;
		public readonly ContainerGeometrySnapshot? ContainerGeometrySnapshot;
	
	    private EngineEvent(
	        EventKind kind,
	        int barIndex,
	        string text,
	        PriceCaseEvent? pce,
	        PermissionEvent? pe,
	        VolPivotEvent? vpe,
	        VolOoeEvent? voe,
	        EndEffectEvent? eee,
	        TurnEvent? te,
	        TrendTypeEvent? tte,
	        ActionEvent? ae,
	        ContainerEvent? ce,
	        DirectionBreakEvent? dbe,
	        FttCandidateEvent? fce,
	        FttConfirmedEvent? ffe,
	        StructureEvent? se,
			ContainerReportEvent? cre,
			PersistentContainerEvent? pce2,
			ContainerGeometryEvent? cge2,
			TradeIntentEvent? tie,
			ContainerGeometrySnapshot? cgse)
	    {
	        Kind = kind;
	        BarIndex = barIndex;
	        Text = text ?? string.Empty;
	        PriceCase = pce;
	        Permission = pe;
	        VolPivot = vpe;
	        VolOoe = voe;
	        EndEffect = eee;
	        Turn = te;
	        TrendType = tte;
	        Action = ae;
	        Container = ce;
	        DirectionBreak = dbe;
	        FttCandidate = fce;
	        FttConfirmed = ffe;
	        Structure = se;
			ContainerReport = cre;
			PersistentContainer = pce2;
			ContainerGeometry = cge2;
			TradeIntent = tie;
			ContainerGeometrySnapshot = cgse;
	    }
	
	    public static EngineEvent From(PriceCaseEvent e) =>
		    new EngineEvent(
		        EventKind.PriceCase,
		        e.BarIndex,
		        e.Case.ToString(),
		        e, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(PermissionEvent e) =>
		    new EngineEvent(
		        EventKind.Permission,
		        e.BarIndex,
		        $"{e.Permission}: {e.Reason}",
		        null, e, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(VolPivotEvent e) =>
		    new EngineEvent(
		        EventKind.VolPivot,
		        e.BarIndex,
		        $"{e.Kind} V={e.Value}",
		        null, null, e, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(VolOoeEvent e) =>
		    new EngineEvent(
		        EventKind.VolOoe,
		        e.BarIndex,
		        $"{e.Name} ({e.Band}) V={e.Value}",
		        null, null, null, e, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(EndEffectEvent e) =>
		    new EngineEvent(
		        EventKind.EndEffect,
		        e.BarIndex,
		        $"{e.Kind} [{e.Band}] from {e.Source} V={e.Value}",
		        null, null, null, null, e, null, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(TurnEvent e) =>
		    new EngineEvent(
		        EventKind.Turn,
		        e.BarIndex,
		        $"{e.Type} from {e.SourceKind} [{e.Band}] via {e.Source} V={e.Value}",
		        null, null, null, null, null, e, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(TrendTypeEvent e) =>
		    new EngineEvent(
		        EventKind.TrendType,
		        e.BarIndex,
		        $"{e.Type} from turn {e.SourceTurn} / {e.SourceKind} [{e.Band}] V={e.Value}",
		        null, null, null, null, null, null, e, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(ActionEvent e) =>
		    new EngineEvent(
		        EventKind.Action,
		        e.BarIndex,
		        $"C#{e.ContainerId} {e.Action} trend={e.TrendType} turn={e.TurnType} {e.SourceKind} [{e.Band}] V={e.Value}",
		        null, null, null, null, null, null, null, e, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(ContainerEvent e) =>
		    new EngineEvent(
		        EventKind.Container,
		        e.BarIndex,
		        $"C#{e.ContainerId} {e.Direction} run={e.RunLength} new={e.IsNewContainer} break={e.HasDirectionBreak} cand={e.HasFttCandidate} conf={e.HasFttConfirmed}",
		        null, null, null, null, null, null, null, null, e, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(DirectionBreakEvent e) =>
		    new EngineEvent(
		        EventKind.DirectionBreak,
		        e.BarIndex,
		        $"C#{e.ContainerId} DirectionBreak {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}",
		        null, null, null, null, null, null, null, null, null, e, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(FttCandidateEvent e) =>
		    new EngineEvent(
		        EventKind.FttCandidate,
		        e.BarIndex,
		        $"C#{e.ContainerId} FTT-Candidate {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}",
		        null, null, null, null, null, null, null, null, null, null, e, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(FttConfirmedEvent e) =>
		    new EngineEvent(
		        EventKind.FttConfirmed,
		        e.BarIndex,
		        $"C#{e.ContainerId} FTT-Confirmed {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}",
		        null, null, null, null, null, null, null, null, null, null, null, e, null, null, null, null, null, null);
	
	   public static EngineEvent From(StructureEvent e) =>
		    new EngineEvent(
		        EventKind.Structure,
		        e.BarIndex,
		        $"C#{e.ContainerId} {e.State} dir={e.Direction} trend={e.TrendType}",
		        null, null, null, null, null, null, null, null, null, null, null, null, e, null, null, null, null, null);
		
		public static EngineEvent From(ContainerReportEvent e) =>
		    new EngineEvent(
		        EventKind.ContainerReport,
		        e.FttConfirmedBarIndex ?? e.StartBarIndex,
		        $"Report C#{e.ContainerId} start={e.StartBarIndex} break={e.DirectionBreakBarIndex} cand={e.FttCandidateBarIndex} conf={e.FttConfirmedBarIndex} struct={e.StructureState} action={e.ActionType}",
		        null, null, null, null, null, null, null, null, null, null, null, null, null, e, null, null, null, null);
		
		public static EngineEvent From(PersistentContainerEvent e) =>
		    new EngineEvent(
		        EventKind.PersistentContainer,
		        e.LastBarIndex,
		        $"PC#{e.ContainerId} {e.LifecycleState} dir={e.Direction} start={e.StartBarIndex} last={e.LastBarIndex} extremeBar={e.ExtremeBarIndex} extreme={e.ExtremePrice} break={e.BreakBarIndex} conf={e.ConfirmBarIndex}",
		        null, null, null, null, null, null, null, null, null, null, null, null, null, null, e, null, null, null);
		
		public static EngineEvent From(ContainerGeometryEvent e) =>
		    new EngineEvent(
		        EventKind.ContainerGeometry,
		        e.ConfirmBarIndex,
		        $"CG#{e.ContainerId} dir={e.Direction} start={e.StartBarIndex}@{e.StartPrice} extreme={e.ExtremeBarIndex}@{e.ExtremePrice} confirm={e.ConfirmBarIndex}@{e.ConfirmPrice}",
		        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, e, null, null);
		
		public static EngineEvent From(TradeIntentEvent e) =>
		    new EngineEvent(
		        EventKind.TradeIntent,
		        e.BarIndex,
		        $"TI C#{e.ContainerId} {e.Intent} action={e.ActionType} struct={e.StructureState} trend={e.TrendType} turn={e.TurnType}",
		        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, e, null);
		
		public static EngineEvent From(ContainerGeometrySnapshot e) =>
		    new EngineEvent(
		        EventKind.ContainerGeometrySnapshot,
		        e.CurrentBarIndex,
		        $"CGS C#{e.ContainerId} state={e.State} p1={(e.P1.HasValue ? e.P1.Value.BarIndex.ToString() : "-")} p2={(e.P2.HasValue ? e.P2.Value.BarIndex.ToString() : "-")} p3={(e.P3.HasValue ? e.P3.Value.BarIndex.ToString() : "-")}",
		        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, e);
	}

    public sealed class EngineEvents
    {
        public EngineEvent[] Events { get; }

        public EngineEvents(EngineEvent[] eventsArray)
        {
            Events = eventsArray ?? Array.Empty<EngineEvent>();
        }

        public static EngineEvents Empty => new EngineEvents(Array.Empty<EngineEvent>());
    }
}







































