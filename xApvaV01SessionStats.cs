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
		private Dictionary<ApvaMacroState, List<int>> completedRuns = new Dictionary<ApvaMacroState, List<int>>();
		private Dictionary<string, int> entryTransitionCounts = new Dictionary<string, int>();
		private Dictionary<string, int> entryTransitionRunSums = new Dictionary<string, int>();
		private Dictionary<string, int> entryTransitionRunMax = new Dictionary<string, int>();
		private Dictionary<string, List<int>> entryTransitionRunLengths = new Dictionary<string, List<int>>();
		private Dictionary<string, int> durationBucketTransitions = new Dictionary<string, int>();
		
		private ApvaMacroState? currentRunState;
		private ApvaMacroState? previousState;
		
		private int currentRunLength;
		
		private string GetDurationBucket(int length)
		{
		    if (length <= 2)
		        return "1-2";
		
		    if (length <= 5)
		        return "3-5";
		
		    if (length <= 10)
		        return "6-10";
		
		    if (length <= 20)
		        return "11-20";
		
		    return "21+";
		}
		
		private void AccumulateDurationBucketTransition(string transition, int priorRunLength)
		{
		    string bucket = GetDurationBucket(priorRunLength);
		    string key = bucket + "|" + transition;
		
		    if (!durationBucketTransitions.ContainsKey(key))
		        durationBucketTransitions[key] = 0;
		
		    durationBucketTransitions[key]++;
		}
		
		private void AccumulateEntryStats(
		    ApvaMacroState newState,
		    ApvaMacroState priorState,
		    int priorRunLength)
		{
		    string key =
		        priorState + "->" + newState;
		
		    if (!entryTransitionCounts.ContainsKey(key))
		        entryTransitionCounts[key] = 0;
		
		    if (!entryTransitionRunSums.ContainsKey(key))
		        entryTransitionRunSums[key] = 0;
		
		    if (!entryTransitionRunMax.ContainsKey(key))
		        entryTransitionRunMax[key] = 0;
			
			if (!entryTransitionRunLengths.ContainsKey(key))
			    entryTransitionRunLengths[key] = new List<int>();
			
			entryTransitionRunLengths[key].Add(priorRunLength);
		
		    AccumulateDurationBucketTransition(key, priorRunLength);
		}

		public static string DurationBucketTransitionCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Bucket,Transition,Count";
		}
		
		public string ToDurationBucketTransitionCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (durationBucketTransitions == null || durationBucketTransitions.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in durationBucketTransitions)
		    {
		        string[] parts = kvp.Key.Split('|');
		
		        if (parts.Length != 2)
		            continue;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5}",
		            instrument,
		            sessionContext,
		            totalBars,
		            parts[0],
		            parts[1],
		            kvp.Value));
		    }
		
		    return sb.ToString();
		}

		private void AccumulateRunLength(ApvaMacroState state)
		{
		    if (!currentRunState.HasValue)
		    {
		        currentRunState = state;
		        currentRunLength = 1;
		        return;
		    }
		
		    if (currentRunState.Value == state)
		    {
		        currentRunLength++;
		        return;
		    }
		
		    AddCompletedRun(currentRunState.Value, currentRunLength);
			AccumulateEntryStats(state, currentRunState.Value, currentRunLength);
		
		    currentRunState = state;
		    currentRunLength = 1;
		}
		
		private void AddCompletedRun(ApvaMacroState state, int length)
		{
		    if (!completedRuns.ContainsKey(state))
		        completedRuns[state] = new List<int>();
		
		    completedRuns[state].Add(length);
		}

		public static string RunLengthCsvHeader()
		{
		   return "Instrument,SessionContext,TotalBars,State,CompletedRuns,MeanRunLength,MedianRunLength,MaxRunLength";
		}

		public string ToRunLengthCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    System.Text.StringBuilder sb = new System.Text.StringBuilder();
		
		    foreach (var kvp in completedRuns)
		    {
		        List<int> runs = kvp.Value;
		
		        if (runs == null || runs.Count == 0)
		            continue;
		
		        int sum = 0;
		        int max = 0;
		
		        foreach (int length in runs)
		        {
		            sum += length;
		
		            if (length > max)
		                max = length;
		        }
		
		        double mean = runs.Count > 0
		            ? (double)sum / runs.Count
		            : 0.0;
				
				List<int> sorted = new List<int>(runs);
				sorted.Sort();
				
				double median;
				
				int n = sorted.Count;
				
				if (n == 0)
				{
				    median = 0.0;
				}
				else if (n % 2 == 1)
				{
				    median = sorted[n / 2];
				}
				else
				{
				    median = 0.5 * (sorted[(n / 2) - 1] + sorted[n / 2]);
				}
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5:F2},{6:F2},{7}",
		            instrument,
		            sessionContext,
		            totalBars,
		            kvp.Key,
		            runs.Count,
		            mean,
					median,
		            max));
		    }
		
		    return sb.ToString();
		}
		
        public void Accumulate(ApvaStateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            TotalBars++;
			
			AccumulateRunLength(snapshot.MacroState);

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
		
		public static string EntryStatsCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Transition,Entries,MeanPriorRunLength,MedianPriorRunLength,MaxPriorRunLength";
		}

		public string ToEntryStatsCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in entryTransitionCounts)
		    {
		        string key = kvp.Key;
		
		        int entries = kvp.Value;
		
		        int sum =
		            entryTransitionRunSums.ContainsKey(key)
		                ? entryTransitionRunSums[key]
		                : 0;
		
		        int max =
		            entryTransitionRunMax.ContainsKey(key)
		                ? entryTransitionRunMax[key]
		                : 0;
		
		        double mean =
		            entries > 0
		                ? (double)sum / entries
		                : 0.0;
				
				double median = 0.0;

				if (entryTransitionRunLengths.ContainsKey(key))
				{
				    List<int> sorted = new List<int>(entryTransitionRunLengths[key]);
				    sorted.Sort();
				
				    int n = sorted.Count;
				
				    if (n > 0)
				    {
				        if (n % 2 == 1)
				            median = sorted[n / 2];
				        else
				            median = 0.5 * (sorted[(n / 2) - 1] + sorted[n / 2]);
				    }
				}
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5:F2},{6:F2},{7}",
		            instrument,
		            sessionContext,
		            totalBars,
		            key,
		            entries,
		            mean,
					median,
		            max));
		    }
		
		    return sb.ToString();
		}
    }
}



















