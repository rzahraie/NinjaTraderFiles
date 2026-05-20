using System;
using System.Globalization;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public static class ApvaV01SnapshotFormatter
    {
        public static string CsvHeader()
        {
            return string.Join(",",
                "BarIndex",
                "Time",
                "MacroState",
                "ActiveDirection",
                "SequencePhase",
                "SequenceAuthority",
                "MaturityLevel",
                "DominanceScore",
                "DegradationScore",
                "BalanceScore",
                "TransitionScore",
                "AmbiguityScore",
                "SFCStatus",
                "ExpectedNextBehavior",
                "InvalidationCondition");
        }

        public static string ToCsv(ApvaStateSnapshot s)
        {
            if (s == null)
                return string.Empty;

            return string.Join(",",
                s.BarIndex.ToString(CultureInfo.InvariantCulture),
                Escape(s.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                Escape(s.MacroState.ToString()),
                Escape(s.ActiveDirection.ToString()),
                Escape(s.SequencePhase.ToString()),
                s.SequenceAuthority.ToString("0.000", CultureInfo.InvariantCulture),
                Escape(s.MaturityLevel.ToString()),
                s.Scores.DominanceScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.DegradationScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.BalanceScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.TransitionScore.ToString("0.000", CultureInfo.InvariantCulture),
                s.Scores.AmbiguityScore.ToString("0.000", CultureInfo.InvariantCulture),
                Escape(s.SFCStatus),
                Escape(s.ExpectedNextBehavior),
                Escape(s.InvalidationCondition));
        }

        private static string Escape(string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}