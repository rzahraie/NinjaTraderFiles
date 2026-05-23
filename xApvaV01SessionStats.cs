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
		
		private double currentRunSponsorConfidenceSum;
		private double currentRunSponsorConfidenceMax;
		private double currentRunDominanceScoreSum;
		private double currentRunDegradationScoreSum;
		private double currentRunBalanceScoreSum;
		private int currentRunEventCount;
		private int currentRunLength;
		
		private ApvaMacroState? currentRunState;
		private ApvaMacroState? previousState;
		private ApvaMacroState? previousState1;
		private ApvaMacroState? previousState2;
		private ApvaSponsorState? previousSponsorState;
		
		private Dictionary<string, int> transitions = new Dictionary<string, int>();
		private Dictionary<ApvaMacroState, List<int>> completedRuns = new Dictionary<ApvaMacroState, List<int>>();
		private Dictionary<ApvaSponsorState, int> sponsorStateCounts = new Dictionary<ApvaSponsorState, int>();
		private Dictionary<string, int> entryTransitionCounts = new Dictionary<string, int>();
		private Dictionary<string, int> entryTransitionRunSums = new Dictionary<string, int>();
		private Dictionary<string, int> entryTransitionRunMax = new Dictionary<string, int>();
		private Dictionary<string, List<int>> entryTransitionRunLengths = new Dictionary<string, List<int>>();
		private Dictionary<string, int> durationBucketTransitions = new Dictionary<string, int>();
		private Dictionary<string, int> precursorCounts = new Dictionary<string, int>();
		private Dictionary<string, int> precursorPriorRunLengthSums = new Dictionary<string, int>();
		private Dictionary<string, double> precursorSponsorConfidenceSums = new Dictionary<string, double>();
		private Dictionary<string, double> precursorSponsorConfidenceMax = new Dictionary<string, double>();
		private Dictionary<string, double> precursorDominanceScoreSums = new Dictionary<string, double>();
		private Dictionary<string, double> precursorDegradationScoreSums = new Dictionary<string, double>();
		private Dictionary<string, double> precursorBalanceScoreSums = new Dictionary<string, double>();
		private Dictionary<string, int> precursorEventCountSums = new Dictionary<string, int>();
		private Dictionary<string, int> stateSurvivalCounts = new Dictionary<string, int>();
		private Dictionary<string, int> stateExitCounts = new Dictionary<string, int>();
		private Dictionary<string, int> stateTriplets = new Dictionary<string, int>();
		private Dictionary<string, int> stateTripletTransitions = new Dictionary<string, int>();
		private Dictionary<string, int> sponsorStateTransitions = new Dictionary<string, int>();
		
		private void AccumulateStatePathStats(
		    ApvaMacroState currentState)
		{
		    if (previousState1.HasValue &&
		        previousState2.HasValue)
		    {
		        string triplet =
		            previousState2.Value + "->" +
		            previousState1.Value + "->" +
		            currentState;
		
		        if (!stateTriplets.ContainsKey(triplet))
		            stateTriplets[triplet] = 0;
		
		        stateTriplets[triplet]++;
		    }
		
		    if (previousState1.HasValue)
		    {
		        string transition =
		            previousState1.Value + "->" +
		            currentState;
		
		        if (previousState2.HasValue)
		        {
		            string pathTransition =
		                previousState2.Value + "->" +
		                transition;
		
		            if (!stateTripletTransitions.ContainsKey(pathTransition))
		                stateTripletTransitions[pathTransition] = 0;
		
		            stateTripletTransitions[pathTransition]++;
		        }
		    }
		
		    previousState2 = previousState1;
		    previousState1 = currentState;
		}

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
		
		private int GetSnapshotEventCount(ApvaStateSnapshot snapshot)
		{
		    return snapshot != null && snapshot.Events != null
		        ? snapshot.Events.Count
		        : 0;
		}
		
		private double GetDominanceScore(ApvaStateSnapshot snapshot)
		{
		    return snapshot != null && snapshot.Scores != null
		        ? snapshot.Scores.DominanceScore
		        : 0.0;
		}
		
		private double GetDegradationScore(ApvaStateSnapshot snapshot)
		{
		    return snapshot != null && snapshot.Scores != null
		        ? snapshot.Scores.DegradationScore
		        : 0.0;
		}
		
		private double GetBalanceScore(ApvaStateSnapshot snapshot)
		{
		    return snapshot != null && snapshot.Scores != null
		        ? snapshot.Scores.BalanceScore
		        : 0.0;
		}
		
		private void AddCurrentRunMetrics(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    currentRunSponsorConfidenceSum += snapshot.SponsorConfidence;
		
		    if (snapshot.SponsorConfidence > currentRunSponsorConfidenceMax)
		        currentRunSponsorConfidenceMax = snapshot.SponsorConfidence;
		
		    currentRunDominanceScoreSum += GetDominanceScore(snapshot);
		    currentRunDegradationScoreSum += GetDegradationScore(snapshot);
		    currentRunBalanceScoreSum += GetBalanceScore(snapshot);
		    currentRunEventCount += GetSnapshotEventCount(snapshot);
		}
		
		private void ResetCurrentRunMetrics(ApvaStateSnapshot snapshot)
		{
		    currentRunSponsorConfidenceSum = 0.0;
		    currentRunSponsorConfidenceMax = 0.0;
		    currentRunDominanceScoreSum = 0.0;
		    currentRunDegradationScoreSum = 0.0;
		    currentRunBalanceScoreSum = 0.0;
		    currentRunEventCount = 0;
		
		    AddCurrentRunMetrics(snapshot);
		}

		private void AccumulateHazardStats(
		    ApvaMacroState state,
		    int runLength,
		    bool exited)
		{
		    string key =
		        state + "|" + runLength;
		
		    if (!stateSurvivalCounts.ContainsKey(key))
		        stateSurvivalCounts[key] = 0;
		
		    stateSurvivalCounts[key]++;
		
		    if (exited)
		    {
		        if (!stateExitCounts.ContainsKey(key))
		            stateExitCounts[key] = 0;
		
		        stateExitCounts[key]++;
		    }
		}

		private void AccumulatePrecursorStats(
		    string transition,
		    int priorRunLength)
		{
		    if (priorRunLength <= 0)
		        return;
		
		    if (!precursorCounts.ContainsKey(transition))
		        precursorCounts[transition] = 0;
		
		    if (!precursorPriorRunLengthSums.ContainsKey(transition))
		        precursorPriorRunLengthSums[transition] = 0;
		
		    if (!precursorSponsorConfidenceSums.ContainsKey(transition))
		        precursorSponsorConfidenceSums[transition] = 0.0;
		
		    if (!precursorSponsorConfidenceMax.ContainsKey(transition))
		        precursorSponsorConfidenceMax[transition] = 0.0;
		
		    if (!precursorDominanceScoreSums.ContainsKey(transition))
		        precursorDominanceScoreSums[transition] = 0.0;
		
		    if (!precursorDegradationScoreSums.ContainsKey(transition))
		        precursorDegradationScoreSums[transition] = 0.0;
		
		    if (!precursorBalanceScoreSums.ContainsKey(transition))
		        precursorBalanceScoreSums[transition] = 0.0;
		
		    if (!precursorEventCountSums.ContainsKey(transition))
		        precursorEventCountSums[transition] = 0;
		
		    double meanSponsorConfidence =
		        currentRunSponsorConfidenceSum / priorRunLength;
		
		    double meanDominanceScore =
		        currentRunDominanceScoreSum / priorRunLength;
		
		    double meanDegradationScore =
		        currentRunDegradationScoreSum / priorRunLength;
		
		    double meanBalanceScore =
		        currentRunBalanceScoreSum / priorRunLength;
		
		    precursorCounts[transition]++;
		    precursorPriorRunLengthSums[transition] += priorRunLength;
		    precursorSponsorConfidenceSums[transition] += meanSponsorConfidence;
		    precursorDominanceScoreSums[transition] += meanDominanceScore;
		    precursorDegradationScoreSums[transition] += meanDegradationScore;
		    precursorBalanceScoreSums[transition] += meanBalanceScore;
		    precursorEventCountSums[transition] += currentRunEventCount;
		
		    if (currentRunSponsorConfidenceMax > precursorSponsorConfidenceMax[transition])
		        precursorSponsorConfidenceMax[transition] = currentRunSponsorConfidenceMax;
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
		
		public static string DurationBucketProbabilityCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars," +
		           "Bucket,FromState,Transition,ProbabilityPct";
		}
		
		public string ToDurationBucketProbabilityCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (durationBucketTransitions == null ||
		        durationBucketTransitions.Count == 0)
		        return string.Empty;
		
		    Dictionary<string, int> totals =
		        new Dictionary<string, int>();
		
		    foreach (var kvp in durationBucketTransitions)
		    {
		        string[] parts = kvp.Key.Split('|');
		
		        if (parts.Length != 2)
		            continue;
		
		        string bucket = parts[0];
		
		        string transition = parts[1];
		
		        string[] states =
		            transition.Split(
		                new string[] { "->" },
		                StringSplitOptions.None);
		
		        if (states.Length != 2)
		            continue;
		
		        string fromState = states[0];
		
		        string totalKey =
		            bucket + "|" + fromState;
		
		        if (!totals.ContainsKey(totalKey))
		            totals[totalKey] = 0;
		
		        totals[totalKey] += kvp.Value;
		    }
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in durationBucketTransitions)
		    {
		        string[] parts = kvp.Key.Split('|');
		
		        if (parts.Length != 2)
		            continue;
		
		        string bucket = parts[0];
		
		        string transition = parts[1];
		
		        string[] states =
		            transition.Split(
		                new string[] { "->" },
		                StringSplitOptions.None);
		
		        if (states.Length != 2)
		            continue;
		
		        string fromState = states[0];
		
		        string totalKey =
		            bucket + "|" + fromState;
		
		        int total =
		            totals.ContainsKey(totalKey)
		                ? totals[totalKey]
		                : 0;
		
		        double probability =
		            total > 0
		                ? 100.0 * kvp.Value / total
		                : 0.0;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5},{6:F2}",
		            instrument,
		            sessionContext,
		            totalBars,
		            bucket,
		            fromState,
		            transition,
		            probability));
		    }
		
		    return sb.ToString();
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

		private void AccumulateRunLength(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    ApvaMacroState state = snapshot.MacroState;
		
		    if (!currentRunState.HasValue)
		    {
		        currentRunState = state;
		        currentRunLength = 1;
		        ResetCurrentRunMetrics(snapshot);
		        return;
		    }
		
		    if (currentRunState.Value == state)
		    {
				AccumulateHazardStats(state,currentRunLength,false);
		        currentRunLength++;
		        AddCurrentRunMetrics(snapshot);
		        return;
		    }
		
		    string transition =
		        currentRunState.Value + "->" + state;
		
		    AddCompletedRun(currentRunState.Value, currentRunLength);
		    AccumulateEntryStats(state, currentRunState.Value, currentRunLength);
		    AccumulatePrecursorStats(transition, currentRunLength);
			AccumulateHazardStats(currentRunState.Value,currentRunLength,true);
		
		    currentRunState = state;
		    currentRunLength = 1;
		    ResetCurrentRunMetrics(snapshot);
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
			
			AccumulateSponsorStats(snapshot);
			AccumulateStatePathStats(snapshot.MacroState);
			AccumulateRunLength(snapshot);
			
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

		private void AccumulateSponsorStats(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    ApvaSponsorState sponsorState = snapshot.SponsorState;
		
		    if (!sponsorStateCounts.ContainsKey(sponsorState))
		        sponsorStateCounts[sponsorState] = 0;
		
		    sponsorStateCounts[sponsorState]++;
		
		    if (previousSponsorState.HasValue)
		    {
		        string key =
		            previousSponsorState.Value + "->" + sponsorState;
		
		        if (!sponsorStateTransitions.ContainsKey(key))
		            sponsorStateTransitions[key] = 0;
		
		        sponsorStateTransitions[key]++;
		    }
		
		    previousSponsorState = sponsorState;
		}
		
		public string ToStateTripletCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (stateTriplets == null ||
		        stateTriplets.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in stateTriplets)
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
		
		public string ToStateTripletTransitionCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (stateTripletTransitions == null ||
		        stateTripletTransitions.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in stateTripletTransitions)
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

		public static string StateTripletCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Triplet,Count";
		}
		
		public static string StateTripletTransitionCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,PathTransition,Count";
		}

		public static string SponsorStateCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,SponsorState,Count,Percent";
		}

		public string ToSponsorStateCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (sponsorStateCounts == null || sponsorStateCounts.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in sponsorStateCounts)
		    {
		        double pct =
		            TotalBars > 0
		                ? 100.0 * kvp.Value / TotalBars
		                : 0.0;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5:F2}",
		            instrument,
		            sessionContext,
		            totalBars,
		            kvp.Key,
		            kvp.Value,
		            pct));
		    }
		
		    return sb.ToString();
		}

		public static string SponsorTransitionCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Transition,Count";
		}

		public string ToSponsorTransitionCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (sponsorStateTransitions == null || sponsorStateTransitions.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in sponsorStateTransitions)
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
		
		public static string PrecursorStatsCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Transition,Entries," +
		           "MeanPriorRunLength,MeanSponsorConfidence,MaxSponsorConfidence," +
		           "MeanDominanceScore,MeanDegradationScore,MeanBalanceScore,EventCount";
		}

		public string ToPrecursorStatsCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (precursorCounts == null || precursorCounts.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in precursorCounts)
		    {
		        string transition = kvp.Key;
		        int entries = kvp.Value;
		
		        if (entries <= 0)
		            continue;
		
		        int runLengthSum =
		            precursorPriorRunLengthSums.ContainsKey(transition)
		                ? precursorPriorRunLengthSums[transition]
		                : 0;
		
		        double sponsorSum =
		            precursorSponsorConfidenceSums.ContainsKey(transition)
		                ? precursorSponsorConfidenceSums[transition]
		                : 0.0;
		
		        double sponsorMax =
		            precursorSponsorConfidenceMax.ContainsKey(transition)
		                ? precursorSponsorConfidenceMax[transition]
		                : 0.0;
		
		        double dominanceSum =
		            precursorDominanceScoreSums.ContainsKey(transition)
		                ? precursorDominanceScoreSums[transition]
		                : 0.0;
		
		        double degradationSum =
		            precursorDegradationScoreSums.ContainsKey(transition)
		                ? precursorDegradationScoreSums[transition]
		                : 0.0;
		
		        double balanceSum =
		            precursorBalanceScoreSums.ContainsKey(transition)
		                ? precursorBalanceScoreSums[transition]
		                : 0.0;
		
		        int eventCount =
		            precursorEventCountSums.ContainsKey(transition)
		                ? precursorEventCountSums[transition]
		                : 0;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5:F2},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11}",
		            instrument,
		            sessionContext,
		            totalBars,
		            transition,
		            entries,
		            (double)runLengthSum / entries,
		            sponsorSum / entries,
		            sponsorMax,
		            dominanceSum / entries,
		            degradationSum / entries,
		            balanceSum / entries,
		            eventCount));
		    }
		
		    return sb.ToString();
		}
		
		public static string HazardCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars," +
		           "State,RunLength,SurvivalCount,ExitCount,HazardProbability";
		}
		
		public static string EntryStatsCsvHeader()
		{
		    return "Instrument,SessionContext,TotalBars,Transition,Entries,MeanPriorRunLength,MedianPriorRunLength,MaxPriorRunLength";
		}

		public string ToHazardCsv(
		    string instrument,
		    string sessionContext,
		    int totalBars)
		{
		    if (stateSurvivalCounts == null ||
		        stateSurvivalCounts.Count == 0)
		        return string.Empty;
		
		    System.Text.StringBuilder sb =
		        new System.Text.StringBuilder();
		
		    foreach (var kvp in stateSurvivalCounts)
		    {
		        string[] parts =
		            kvp.Key.Split('|');
		
		        if (parts.Length != 2)
		            continue;
		
		        string state = parts[0];
		        string runLength = parts[1];
		
		        int survivalCount = kvp.Value;
		
		        int exitCount =
		            stateExitCounts.ContainsKey(kvp.Key)
		                ? stateExitCounts[kvp.Key]
		                : 0;
		
		        double hazard =
		            survivalCount > 0
		                ? 100.0 * exitCount / survivalCount
		                : 0.0;
		
		        sb.AppendLine(string.Format(
		            CultureInfo.InvariantCulture,
		            "{0},{1},{2},{3},{4},{5},{6},{7:F2}",
		            instrument,
		            sessionContext,
		            totalBars,
		            state,
		            runLength,
		            survivalCount,
		            exitCount,
		            hazard));
		    }
		
		    return sb.ToString();
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
























