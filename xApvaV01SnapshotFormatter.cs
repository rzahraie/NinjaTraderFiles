using System;
using System.Globalization;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public static class ApvaV01SnapshotFormatter
    {
        public static string CsvHeader()
		{
		    return string.Join(",",
		        "Instrument",
		        "SessionContext",
		        "BarIndex",
		        "Time",
		        "MacroState",
		        "ActiveDirection",
		        "SponsorState",
		        "SponsorConfidence",
		        "SequencePhase",
		        "SequenceAuthority",
		        "MaturityLevel",
		        "DominanceScore",
		        "DegradationScore",
		        "BalanceScore",
		        "TransitionScore",
		        "AmbiguityScore",
		        "Events",
		        "SFCStatus",
		        "ExpectedNextBehavior",
		        "InvalidationCondition",
				"Volume",
				"VolumeSMA",
				"RelativeVolume",
				"VolumeZScore",
				"BarDirection",
				"SignedVolume",
				"UpVolume",
				"DownVolume",
				"FlatVolume",
				"UpDownVolumeDelta",
				"SpyderDominantVolume",
				"SpyderNonDominantVolume",
				"SpyderDominantVolumeShare",
				"SpyderNonDominantVolumeShare",
				"SpyderNonDominantColor",
				"SpyderSplitMethod");
		}

       public static string ToCsv(ApvaStateSnapshot s, string instrument, string sessionContext)
        {
            if (s == null)
                return string.Empty;

            return string.Join(",",
				Escape(instrument),
				Escape(sessionContext),
                s.BarIndex.ToString(CultureInfo.InvariantCulture),
                Escape(s.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                Escape(s.MacroState.ToString()),
                Escape(s.ActiveDirection.ToString()),
				Escape(s.SponsorState.ToString()),
				s.SponsorConfidence.ToString("0.000", CultureInfo.InvariantCulture),
                Escape(s.SequencePhase.ToString()),
                s.SequenceAuthority.ToString("0.000", CultureInfo.InvariantCulture),
                Escape(s.MaturityLevel.ToString()),
                s.Scores.DominanceScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.DegradationScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.BalanceScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.TransitionScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.AmbiguityScore.ToString("0.000", CultureInfo.InvariantCulture),
				Escape(FormatEvents(s.Events)),
                Escape(s.SFCStatus),
                Escape(s.ExpectedNextBehavior),
                Escape(s.InvalidationCondition),
				FormatDouble(s.Volume),
				FormatDouble(s.VolumeSMA),
				FormatDouble(s.RelativeVolume),
				FormatDouble(s.VolumeZScore),
				Escape(s.BarDirection),
				FormatDouble(s.SignedVolume),
				FormatDouble(s.UpVolume),
				FormatDouble(s.DownVolume),
				FormatDouble(s.FlatVolume),
				FormatDouble(s.UpDownVolumeDelta),
				FormatDouble(s.SpyderDominantVolume),
				FormatDouble(s.SpyderNonDominantVolume),
				FormatDouble(s.SpyderDominantVolumeShare),
				FormatDouble(s.SpyderNonDominantVolumeShare),
				Escape(s.SpyderNonDominantColor),
				Escape(s.SpyderSplitMethod));
        }

        private static string Escape(string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
		
		private static string FormatDouble(double value)
		{
		    return value.ToString("0.##########", CultureInfo.InvariantCulture);
		}

		private static string FormatEvents(System.Collections.Generic.IEnumerable<ApvaEvent> events)
		{
		    if (events == null)
		        return string.Empty;
		
		    var parts = new System.Collections.Generic.List<string>();
		
		    foreach (var e in events)
		    {
		        parts.Add(e.EventType.ToString());
		    }
		
		    return string.Join("|", parts);
		}
    }
}



