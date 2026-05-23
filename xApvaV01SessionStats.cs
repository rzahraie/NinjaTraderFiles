#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using NinjaTrader.NinjaScript.APVA.V01;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class xApvaV01SessionStats
    {
        public int TotalBars;

        public int DirectionalBars;
        public int DegradingBars;
        public int BalanceBars;
        public int UnresolvedBars;
        public int UnknownBars;

        public int AcceptedReclaimCount;
        public int RejectedReclaimCount;
        public int FailedContinuationCount;
        public int PeakVolumeCount;
        public int LateralSeedCount;
		
		private Dictionary<string, int> transitions = new Dictionary<string, int>();
		private ApvaMacroState? previousState;

        public void Accumulate(ApvaStateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            TotalBars++;

            switch (snapshot.MacroState)
            {
                case ApvaMacroState.Directional:
                    DirectionalBars++;
                    break;

                case ApvaMacroState.Degrading:
                    DegradingBars++;
                    break;

                case ApvaMacroState.Balance:
                    BalanceBars++;
                    break;

                case ApvaMacroState.Unresolved:
                    UnresolvedBars++;
                    break;

                case ApvaMacroState.Unknown:
                default:
                    UnknownBars++;
                    break;
            }

            if (snapshot.Events == null)
                return;

            foreach (ApvaEvent evt in snapshot.Events)
            {
				if (evt == null)
        			continue;
				
                switch (evt.EventType)
                {
                    case ApvaEventType.AcceptedReclaim:
                        AcceptedReclaimCount++;
                        break;

                    case ApvaEventType.RejectedReclaim:
                        RejectedReclaimCount++;
                        break;

                    case ApvaEventType.FailedContinuation:
                        FailedContinuationCount++;
                        break;

                    case ApvaEventType.PeakVolume:
                        PeakVolumeCount++;
                        break;

                    case ApvaEventType.LateralSeed:
                        LateralSeedCount++;
                        break;
                }
            }
			
			if (previousState.HasValue)
			{
			    string key =
			        previousState.Value + "->" + snapshot.MacroState;
			
			    if (!transitions.ContainsKey(key))
			        transitions[key] = 0;
			
			    transitions[key]++;
			}
			
			previousState = snapshot.MacroState;
        }

		public string ToCsvSummary(
		    string instrument,
		    string sessionContext)
		{
		    return string.Format(
		        CultureInfo.InvariantCulture,
		        "{0},{1},{2},{3:F1},{4:F1},{5:F1},{6:F1},{7:F1},{8},{9},{10},{11},{12}",
		        instrument,
		        sessionContext,
		        TotalBars,
		        Percent(DirectionalBars),
		        Percent(DegradingBars),
		        Percent(BalanceBars),
		        Percent(UnresolvedBars),
		        Percent(UnknownBars),
		        AcceptedReclaimCount,
		        RejectedReclaimCount,
		        FailedContinuationCount,
		        PeakVolumeCount,
		        LateralSeedCount);
}

        public string ToSummaryString(
            string instrument,
            string sessionContext)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}\n" +
                "TotalBars: {2}\n\n" +
                "Directional: {3:F1}%\n" +
                "Degrading: {4:F1}%\n" +
                "Balance: {5:F1}%\n" +
                "Unresolved: {6:F1}%\n" +
                "Unknown: {7:F1}%\n\n" +
                "AcceptedReclaim: {8}\n" +
                "RejectedReclaim: {9}\n" +
                "FailedContinuation: {10}\n" +
                "PeakVolume: {11}\n" +
                "LateralSeed: {12}",
                string.IsNullOrEmpty(instrument) ? "Unknown" : instrument,
                string.IsNullOrEmpty(sessionContext) ? "UnknownSession" : sessionContext,
                TotalBars,
                Percent(DirectionalBars),
                Percent(DegradingBars),
                Percent(BalanceBars),
                Percent(UnresolvedBars),
                Percent(UnknownBars),
                AcceptedReclaimCount,
                RejectedReclaimCount,
                FailedContinuationCount,
                PeakVolumeCount,
                LateralSeedCount);
        }

		public static string CsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars," +
		           "DirectionalPct,DegradingPct,BalancePct,UnresolvedPct,UnknownPct," +
		           "AcceptedReclaim,RejectedReclaim,FailedContinuation,PeakVolume,LateralSeed";
		}

        private double Percent(int count)
        {
            if (TotalBars <= 0)
                return 0.0;

            return 100.0 * count / TotalBars;
        }
		
		public string ToTransitionCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (transitions == null || transitions.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb = new System.Text.StringBuilder();
		
		    foreach (var kvp in transitions)
		    {
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		           "{0},{1},{2},{3},{4}",
		            instrument,
					sessionContext,
					totalBars,
					kvp.Key,
					kvp.Value));
		    }
		
		    return sb.ToString();
		}
		
		public string ToTransitionProbabilityCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (transitions == null || transitions.Count == 0)
		        return string.Empty;
		
		    Dictionary<string, int> totals =
		        new Dictionary<string, int>();
		
		    foreach (var kvp in transitions)
		    {
		        string[] parts = kvp.Key.Split(
		            new string[] { "->" },
		            StringSplitOptions.None);
		
		        if (parts.Length != 2)
		            continue;
		
		        string fromState = parts[0];
		
		        if (!totals.ContainsKey(fromState))
		            totals[fromState] = 0;
		
		        totals[fromState] += kvp.Value;
		    }
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in transitions)
		    {
		        string[] parts = kvp.Key.Split(
		            new string[] { "->" },
		            StringSplitOptions.None);
		
		        if (parts.Length != 2)
		            continue;
		
		        string fromState = parts[0];
		
		        int total =
		            totals.ContainsKey(fromState)
		                ? totals[fromState]
		                : 0;
		
		        double probability =
		            total > 0
		                ? 100.0 * kvp.Value / total
		                : 0.0;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5:F2}",
		            instrument,
		            sessionContext,
		            totalBars,
		            fromState,
		            kvp.Key,
		            probability));
		    }
		
		    return sb.ToString();
		}
		
		public static string TransitionCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Transition,Count";
		}
		
		public static string TransitionProbabilityCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,FromState,Transition,ProbabilityPct";
		}
    }
}











