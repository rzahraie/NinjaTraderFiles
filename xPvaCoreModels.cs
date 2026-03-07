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

        private EngineEvent(
            EventKind kind,
            int barIndex,
            string text,
            PriceCaseEvent? pce,
            PermissionEvent? pe,
            VolPivotEvent? vpe,
            VolOoeEvent? voe)
        {
            Kind = kind;
            BarIndex = barIndex;
            Text = text ?? string.Empty;
            PriceCase = pce;
            Permission = pe;
            VolPivot = vpe;
            VolOoe = voe;
        }

        public static EngineEvent From(PriceCaseEvent e) =>
            new EngineEvent(EventKind.PriceCase, e.BarIndex, e.Case.ToString(), e, null, null, null);

        public static EngineEvent From(PermissionEvent e) =>
            new EngineEvent(EventKind.Permission, e.BarIndex, $"{e.Permission}: {e.Reason}", null, e, null, null);

        public static EngineEvent From(VolPivotEvent e) =>
            new EngineEvent(EventKind.VolPivot, e.BarIndex, $"{e.Kind} V={e.Value}", null, null, e, null);

        public static EngineEvent From(VolOoeEvent e) =>
            new EngineEvent(EventKind.VolOoe, e.BarIndex, $"{e.Name} ({e.Band}) V={e.Value}", null, null, null, e);
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
