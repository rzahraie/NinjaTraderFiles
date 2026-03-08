using System;

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
	    public readonly ActionType Action;
	    public readonly TrendType TrendType;
	    public readonly TurnType TurnType;
	    public readonly EndEffectKind SourceKind;
	    public readonly Band Band;
	    public readonly long Value;
	
	    public ActionEvent(int barIndex, ActionType action, TrendType trendType, TurnType turnType, EndEffectKind sourceKind, Band band, long value)
	    {
	        BarIndex = barIndex;
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
	
	public readonly struct DirectionBreakEvent
	{
	    public readonly int BarIndex;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public DirectionBreakEvent(int barIndex, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct FttCandidateEvent
	{
	    public readonly int BarIndex;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public FttCandidateEvent(int barIndex, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct FttConfirmedEvent
	{
	    public readonly int BarIndex;
	    public readonly ContainerDirection PriorDirection;
	    public readonly PriceCase SourcePriceCase;
	    public readonly int PriorRunLength;
	
	    public FttConfirmedEvent(int barIndex, ContainerDirection priorDirection, PriceCase sourcePriceCase, int priorRunLength)
	    {
	        BarIndex = barIndex;
	        PriorDirection = priorDirection;
	        SourcePriceCase = sourcePriceCase;
	        PriorRunLength = priorRunLength;
	    }
	}
	
	public readonly struct ContainerEvent
	{
	    public readonly int BarIndex;
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
	    public readonly StructureState State;
	    public readonly TrendType TrendType;
	    public readonly ActionType ActionType;
	    public readonly ContainerDirection Direction;
	
	    public StructureEvent(
	        int barIndex,
	        StructureState state,
	        TrendType trendType,
	        ActionType actionType,
	        ContainerDirection direction)
	    {
	        BarIndex = barIndex;
	        State = state;
	        TrendType = trendType;
	        ActionType = actionType;
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
	        StructureEvent? se)
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
	    }
	
	    public static EngineEvent From(PriceCaseEvent e) =>
	        new EngineEvent(EventKind.PriceCase, e.BarIndex, e.Case.ToString(), e, null, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(PermissionEvent e) =>
	        new EngineEvent(EventKind.Permission, e.BarIndex, $"{e.Permission}: {e.Reason}", null, e, null, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(VolPivotEvent e) =>
	        new EngineEvent(EventKind.VolPivot, e.BarIndex, $"{e.Kind} V={e.Value}", null, null, e, null, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(VolOoeEvent e) =>
	        new EngineEvent(EventKind.VolOoe, e.BarIndex, $"{e.Name} ({e.Band}) V={e.Value}", null, null, null, e, null, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(EndEffectEvent e) =>
	        new EngineEvent(EventKind.EndEffect, e.BarIndex, $"{e.Kind} [{e.Band}] from {e.Source} V={e.Value}", null, null, null, null, e, null, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(TurnEvent e) =>
	        new EngineEvent(EventKind.Turn, e.BarIndex, $"{e.Type} from {e.SourceKind} [{e.Band}] via {e.Source} V={e.Value}", null, null, null, null, null, e, null, null, null, null, null, null, null);
	
	    public static EngineEvent From(TrendTypeEvent e) =>
	        new EngineEvent(EventKind.TrendType, e.BarIndex, $"{e.Type} from turn {e.SourceTurn} / {e.SourceKind} [{e.Band}] V={e.Value}", null, null, null, null, null, null, e, null, null, null, null, null, null);
	
	    public static EngineEvent From(ActionEvent e) =>
	        new EngineEvent(EventKind.Action, e.BarIndex, $"{e.Action} from trend {e.TrendType} / turn {e.TurnType} / {e.SourceKind} [{e.Band}] V={e.Value}", null, null, null, null, null, null, null, e, null, null, null, null, null);
	
	    public static EngineEvent From(ContainerEvent e) =>
	        new EngineEvent(EventKind.Container, e.BarIndex, $"{e.Direction} run={e.RunLength} new={e.IsNewContainer} break={e.HasDirectionBreak} cand={e.HasFttCandidate} conf={e.HasFttConfirmed}", null, null, null, null, null, null, null, null, e, null, null, null, null);
	
	    public static EngineEvent From(DirectionBreakEvent e) =>
	        new EngineEvent(EventKind.DirectionBreak, e.BarIndex, $"DirectionBreak {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}", null, null, null, null, null, null, null, null, null, e, null, null, null);
	
	    public static EngineEvent From(FttCandidateEvent e) =>
	        new EngineEvent(EventKind.FttCandidate, e.BarIndex, $"FTT-Candidate {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}", null, null, null, null, null, null, null, null, null, null, e, null, null);
	
	    public static EngineEvent From(FttConfirmedEvent e) =>
	        new EngineEvent(EventKind.FttConfirmed, e.BarIndex, $"FTT-Confirmed {e.PriorDirection} via {e.SourcePriceCase} run={e.PriorRunLength}", null, null, null, null, null, null, null, null, null, null, null, e, null);
	
	    public static EngineEvent From(StructureEvent e) =>
	        new EngineEvent(EventKind.Structure, e.BarIndex, $"{e.State} dir={e.Direction} trend={e.TrendType} action={e.ActionType}", null, null, null, null, null, null, null, null, null, null, null, null, e);
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














