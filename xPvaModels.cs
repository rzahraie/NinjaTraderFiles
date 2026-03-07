#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    public enum xPvaLabelType { Tape, Traverse, Channel, BBT, Other }
    public enum xPvaDirection { Up, Down, Unknown }
    public enum xPvaFractal { L0, L1, L2, Unknown }
    public enum xPvaAnchorRole { Start, End, RTL, LTL, VE, Other }

    public sealed class xPvaInstrumentInfo
    {
        public string Name { get; set; }
        public string Master { get; set; }
    }

    public sealed class xPvaBarsInfo
    {
        public string Type { get; set; }     // "Minute"
        public int Value { get; set; }       // 5
    }

    public sealed class xPvaSessionInfo
    {
        public bool EthEnabled { get; set; }
        public string RthTemplate { get; set; }
        public string EthTemplate { get; set; }
    }

    public sealed class xPvaAt
    {
        public int BarIndex { get; set; }
		public int BarsAgo { get; set; }
        public DateTime TimeUtc { get; set; }
    }

    public sealed class xPvaAmbiguity
    {
        public string GroupId { get; set; }     // same across alternatives
        public string Choice { get; set; }      // "A" or "B"
        public double Confidence { get; set; }  // 0..1 (human-entered is fine)
    }

    public sealed class xPvaAnchor
    {
        public xPvaAnchorRole Role { get; set; }
        public int BarIndex { get; set; }
		public int BarsAgo { get; set; }
        public DateTime TimeUtc { get; set; }
        public double Price { get; set; }
		public DateTime? ChartTime { get; set; }   // raw chart time as reported by ChartBars
    }

    public sealed class xPvaLabel
    {
        public string Id { get; set; } = xPvaIds.NewId();

        public xPvaLabelType Type { get; set; } = xPvaLabelType.Other;
        public xPvaDirection Direction { get; set; } = xPvaDirection.Unknown;
        public xPvaFractal Fractal { get; set; } = xPvaFractal.Unknown;

        public xPvaAt CreatedAt { get; set; } = new xPvaAt();
        public xPvaAt FinalizedAt { get; set; } = null; // null => not frozen yet

        public xPvaAmbiguity Ambiguity { get; set; } = null;

        public List<xPvaAnchor> Anchors { get; set; } = new List<xPvaAnchor>();
        public List<string> Tags { get; set; } = new List<string>();

        public string Text { get; set; } = "";
    }

    public sealed class xPvaNote
    {
        public DateTime TimeUtc { get; set; }
        public string Text { get; set; }
    }

    public sealed class xPvaDataset
    {
        public string SchemaVersion { get; set; } = xPvaIds.SchemaVersion;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public xPvaInstrumentInfo Instrument { get; set; } = new xPvaInstrumentInfo();
        public xPvaBarsInfo Bars { get; set; } = new xPvaBarsInfo();
        public string ChartTimeZone { get; set; } = "America/Phoenix";
        public xPvaSessionInfo Session { get; set; } = new xPvaSessionInfo();

        public List<xPvaLabel> Labels { get; set; } = new List<xPvaLabel>();
        public List<xPvaNote> Notes { get; set; } = new List<xPvaNote>();
    }
}



