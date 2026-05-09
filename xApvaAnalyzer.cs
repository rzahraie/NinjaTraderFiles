using System;
using System.Collections.Generic;

namespace APVA.Core
{
    public sealed class ApvaAnalysisResult
    {
        public List<VolumeSegment> Segments { get; set; } = new List<VolumeSegment>();

        public DominanceState CurrentDominance { get; set; } = DominanceState.Unknown;
		public DominanceState CurrentSegmentDominance { get; set; } = DominanceState.Unknown;
		public DominanceState ContainerBias { get; set; } = DominanceState.Unknown;
		public DominanceState RecentBias { get; set; } = DominanceState.Unknown;

        public bool HasDominanceSequence { get; set; }
        public bool HasFailureSequence { get; set; }

        public FttResult Ftt { get; set; } = new FttResult();
		
		public xApvaContainerCandidate Container { get; set; }
		
		public int WarningDuration { get; set; }
		
		public bool ImminentFtt { get; set; }
		
		public double DistanceToLtl { get; set; }
		
		public double DistanceToLtlDelta { get; set; }
		
		public bool IneffectiveDominance { get; set; }
		
		public int BarsSinceLastFtt { get; set; }
		
		public double P2P3Distance { get; set; }
		public bool IsWeakContainer { get; set; }
		
		public int ContainerAgeBars { get; set; }
		public bool IsMatureContainer { get; set; }
		
		public bool FttDetectionAllowed { get; set; } = true;
		public string FttDetectionBlockReason { get; set; } = "";
		
		public double SelectedContainerScoreSnapshot { get; set; } = double.NaN;
		public double PrimaryContainerScoreSnapshot { get; set; } = double.NaN;
		public double SecondaryContainerScoreSnapshot { get; set; } = double.NaN;
		
		public List<FttEvent> CompletedFttEvents { get; set; } = new List<FttEvent>();
    }
	
	public sealed class ApvaAnalyzerState
	{
	    public int WarningStreak = 0;
	    public double PrevDistanceToLtl = 0;
	    public bool HasPrevDistance = false;
	    public int LastFttBarIndex = -1;
		public bool ContinuationAttempted = false;
		public xApvaContainerCandidate PrimaryContainer = null;
		public xApvaContainerCandidate SecondaryContainer = null;
		public int PostFttGraceBars = 0;
		public int BarsFarFromStructure = 0;
		public xApvaContainerCandidate PendingSecondaryContainer = null;
		public int PendingSecondaryConfirmBars = 0;
		public List<FttEvent> PendingFttEvents = new List<FttEvent>();
	}
	
	public class FttEvent
	{
	    public int EntryBarIndex;
	    public double EntryPrice;
	    public ContainerDirection Direction;
	
	    public double MaxFavorableExcursion5;
	    public double MaxAdverseExcursion5;
	
	    public double MaxFavorableExcursion10;
	    public double MaxAdverseExcursion10;
	
	    public double MaxFavorableExcursion20;
	    public double MaxAdverseExcursion20;
	
	    public int BarsTracked;
	}

    public static class xApvaAnalyzer
    {
		private static void ComputeDistanceToLtl(
		    ApvaAnalysisResult result,
		    Bar currentBar)
		{
		    if (result.Container == null)
		        return;
		
		    double ltl = result.Container.LTL.ValueAt(currentBar.Index);
		
		    if (result.Container.Direction == ContainerDirection.Up)
		        result.DistanceToLtl = ltl - currentBar.High;
		    else if (result.Container.Direction == ContainerDirection.Down)
		        result.DistanceToLtl = currentBar.Low - ltl;
		}

		private static void BuildAndExtendContainer(
		    ApvaAnalysisResult result,
		    IReadOnlyList<Bar> bars,
		    Bar currentBar,
		    double tickTolerance,
		    ApvaAnalyzerState state)
		{
		    if (state.PrimaryContainer == null)
			{
			    state.PrimaryContainer =
			        xApvaContainerBuilder.BuildFromSwings(
			            bars,
			            swingStrength: 1,
			            tickTolerance: tickTolerance);
			}
			else
			{
			    // Attempt to build alternative container
			    var candidate =
			        xApvaContainerBuilder.BuildFromSwings(
			            bars,
			            swingStrength: 1,
			            tickTolerance: tickTolerance);
			
			    if (candidate != null &&
				    !AreContainersEquivalent(state.PrimaryContainer, candidate))
				{
				    if (AreContainersEquivalent(state.PendingSecondaryContainer, candidate))
				    {
				        state.PendingSecondaryConfirmBars++;
				    }
				    else
				    {
				        state.PendingSecondaryContainer = candidate;
				        state.PendingSecondaryConfirmBars = 1;
				    }
				
				    if (state.PendingSecondaryConfirmBars >= 2)
				    {
				        state.SecondaryContainer = state.PendingSecondaryContainer;
				        state.PendingSecondaryContainer = null;
				        state.PendingSecondaryConfirmBars = 0;
				    }
				}
				else
				{
				    state.PendingSecondaryContainer = null;
				    state.PendingSecondaryConfirmBars = 0;
				}
			}

			// Default: use primary
			result.Container = SelectBestContainer(state, currentBar);
		
		    if (result.Container == null)
		        return;
			
		    state.PrimaryContainer?.TryExtend(currentBar, tickTolerance, false);
			state.SecondaryContainer?.TryExtend(currentBar, tickTolerance, false);
		
		   ComputeDistanceToLtl(result, currentBar);
		}

		private static void PromoteP3OnStrongContinuation(
		    ApvaAnalysisResult result,
		    Bar currentBar,
		    bool continuationFailed,
		    double tickTolerance)
		{
		    if (result.Container == null || !result.Container.HasValidP3)
		        return;
		
		    double minMove = 4 * tickTolerance;
		
		    bool strongContinuation =
			    result.DistanceToLtl < 0 &&
			    Math.Abs(result.DistanceToLtl) > minMove;
			
			// NEW: require dominance agreement
			bool dominanceSupportsContinuation =
			    result.CurrentSegmentDominance == DominanceState.Dominant;
			
			if (strongContinuation &&
			    dominanceSupportsContinuation &&
			    !continuationFailed)
			{
			    result.Container.P3 = new xApvaPoint(
			    currentBar.Index,
			    currentBar.Close);
			
			result.Container.RTL =
			    new xApvaLine(
			        result.Container.P1,
			        result.Container.P3);
			
			double rtlAtP2 =
			    result.Container.RTL.ValueAt(result.Container.P2.Index);
			
			double offset =
			    result.Container.P2.Price - rtlAtP2;
			
			result.Container.LTL =
			    new xApvaLine(
			        new xApvaPoint(
			            result.Container.P1.Index,
			            result.Container.P1.Price + offset),
			        new xApvaPoint(
			            result.Container.P3.Index,
			            result.Container.P3.Price + offset));
			}
		}

		private static void ComputeContainerDiagnostics(
		    ApvaAnalysisResult result,
		    Bar currentBar,
		    double tickTolerance)
		{
		    if (result.Container == null || !result.Container.HasValidP3)
		        return;
		
		    double distance =
		        Math.Abs(result.Container.P2.Price - result.Container.P3.Price);
		
		    result.P2P3Distance = distance;
		
		    double minMove = 4 * tickTolerance;
		
		    result.IsWeakContainer = distance < minMove;
		
		    result.ContainerAgeBars =
		        currentBar.Index - result.Container.P1.Index;
		
		    result.IsMatureContainer =
		        result.ContainerAgeBars >= 2 &&
		        !result.IsWeakContainer;
		}

		private static void UpdateDistanceState(
    		ApvaAnalysisResult result,
   			ApvaAnalyzerState state)
		{
		    if (!state.HasPrevDistance)
		    {
		        result.DistanceToLtlDelta = 0.0;
		        state.HasPrevDistance = true;
		    }
		    else
		    {
		        result.DistanceToLtlDelta =
		            result.DistanceToLtl - state.PrevDistanceToLtl;
		    }
		
		    state.PrevDistanceToLtl = result.DistanceToLtl;
		}
		
		private static bool UpdateContinuationAttemptState(
    		ApvaAnalysisResult result,
    		ApvaAnalyzerState state)
		{
		    if (result.DistanceToLtl < 0)
		        state.ContinuationAttempted = true;
		
		    return state.ContinuationAttempted;
		}
		
		private static void BuildSegmentsAndDominance(
		    ApvaAnalysisResult result,
		    IReadOnlyList<Bar> bars,
		    IReadOnlyList<ClassifiedBar> classifiedBars)
		{
		    ContainerDirection direction =
		        result.Container != null
		            ? result.Container.Direction
		            : xApvaDirectionInferer.InferDirection(bars);
		
		    result.Segments =
		        xApvaVolumeSegmentBuilder.BuildSegments(
		            bars,
		            classifiedBars);
		
		    xApvaVolumePhaseClassifier.Classify(
		        result.Segments,
		        direction);
		
		    result.CurrentDominance =
		        xApvaDominanceEngine.GetContainerDominance(result.Segments);
		
		    result.CurrentSegmentDominance =
		        xApvaDominanceEngine.GetCurrentSegmentDominance(result.Segments);
		
		    List<VolumeSegment> containerSegments =
		        FilterSegmentsByContainer(result.Segments, result.Container);
		
		    if (containerSegments.Count > 0)
		    {
		        result.ContainerBias =
		            xApvaDominanceEngine.GetContainerBias(containerSegments);
		
		        result.RecentBias =
		            xApvaDominanceEngine.GetRecentBias(containerSegments, 5);
		    }
		    else
		    {
		        result.ContainerBias =
		            xApvaDominanceEngine.GetContainerBias(result.Segments);
		
		        result.RecentBias =
		            xApvaDominanceEngine.GetRecentBias(result.Segments, 5);
		    }
		
		    result.HasDominanceSequence =
		        xApvaDominanceEngine.HasDominanceSequence(result.Segments);
		
		    result.HasFailureSequence =
		        xApvaDominanceEngine.HasFailureSequence(result.Segments);
		}

		private static void UpdateStructuralRelevance(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    double tickTolerance)
		{
		    double farThreshold = 10 * tickTolerance;
		
		    bool farFromStructure =
		        Math.Abs(result.DistanceToLtl) > farThreshold;
		
		    if (farFromStructure)
		        state.BarsFarFromStructure++;
		    else
		        state.BarsFarFromStructure = 0;
		}

		private static void UpdateWarningState(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    bool continuationFailed,
		    double tickTolerance)
		{
			if (state.PostFttGraceBars > 0)
			{
			    result.WarningDuration = 0;
			    result.ImminentFtt = false;
			    result.IneffectiveDominance = false;
			    return;
			}

		   double proximityThreshold = 6 * tickTolerance;

			bool nearStructure =
			    Math.Abs(result.DistanceToLtl) <= proximityThreshold;
			
			bool hasT2F =
			    result.Ftt != null &&
			    (
			        result.Ftt.Kind == FttKind.T2F_FailedContinuation ||
			        (result.Ftt.Reason != null &&
			         result.Ftt.Reason.Contains("T2F"))
			    );
			
			if (continuationFailed && (nearStructure || hasT2F))
			{
			    state.WarningStreak++;
			}
			else if (continuationFailed && state.WarningStreak > 0)
			{
			    // Once warning has started, continued failure matures it
			    // even if price has moved away from the LTL.
			    state.WarningStreak++;
			}
			else if (!continuationFailed)
			{
			    if (state.WarningStreak > 0)
			        state.WarningStreak--;   // decay instead of reset
			}

		    result.WarningDuration = state.WarningStreak;
		
		   	result.IneffectiveDominance =
			    result.CurrentSegmentDominance == DominanceState.Dominant &&
			    result.DistanceToLtl > 0;
			
			result.ImminentFtt =
			    result.WarningDuration >= 3 &&
			    result.ContainerBias == DominanceState.CounterDominant &&
			    result.IneffectiveDominance;
		}

		private static void DetectFtt(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    bool continuationFailed,
		    bool continuationAttempted)
		{
			    result.FttDetectionAllowed = true;
			    result.FttDetectionBlockReason = "";
			
			    result.Ftt =
			        xApvaFttDetector.Detect(
			            result.Segments,
			            hasValidP3: result.Container != null && result.Container.HasValidP3,
			            expectedContinuationFailed: continuationFailed,
			            continuationAttempted: continuationAttempted);
		}

		private static void GateFtt(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    Bar currentBar,
		    bool continuationFailed,
		    bool continuationAttempted)
		{
			if (state.PostFttGraceBars > 0)
			{
			    result.Ftt.IsCandidate = false;
			    result.Ftt.IsConfirmed = false;
			    result.Ftt.Reason = "Blocked by post-FTT grace period.";
			    return;
			}
			
			// NEW: require sustained failure before candidate
			int minWarningBars = 3;
			
			if (result.Ftt.IsCandidate && state.WarningStreak < minWarningBars)
			{
			    result.Ftt.IsCandidate = false;
			    result.Ftt.IsConfirmed = false;
			    result.Ftt.Reason += " Blocked by insufficient warning buildup.";
			}
		
		    const int MinFttSeparationBars = 8;
		
		    int barsSinceLastFtt =
		        state.LastFttBarIndex >= 0
		            ? currentBar.Index - state.LastFttBarIndex
		            : int.MaxValue;
		
		    result.BarsSinceLastFtt = barsSinceLastFtt;
		
		    if (result.Ftt.IsCandidate && barsSinceLastFtt < MinFttSeparationBars)
		    {
		        result.Ftt.IsCandidate = false;
		        result.Ftt.IsConfirmed = false;
		        result.Ftt.Reason += " Blocked by FTT candidate cooldown.";
		    }
		   	else if (result.Ftt.IsConfirmed)
			{
			    if (result.CurrentSegmentDominance != DominanceState.Dominant)
			    {
			        result.Ftt.IsConfirmed = false;
			        result.Ftt.Reason += " Blocked by non-dominant current segment.";
			        return;
			    }
			
			    const double MinContainerScoreForFtt = 4.0;
			
			    if (!result.IsMatureContainer ||
			        result.Container == null ||
			        result.Container.Score < MinContainerScoreForFtt)
			    {
			        result.Ftt.IsConfirmed = false;
			        result.Ftt.Reason += " Blocked by weak/low-score container.";
			    }
			    else
			    {
			        state.LastFttBarIndex = currentBar.Index;
			    }
			}
		
		   if (result.Ftt.IsConfirmed)
		   {
			    state.PendingFttEvents.Add(new FttEvent
			    {
			        EntryBarIndex = currentBar.Index,
			        EntryPrice = currentBar.Close,
			        Direction = result.Container != null ? result.Container.Direction : ContainerDirection.Unknown,
			        BarsTracked = 0
			    });
			   
			    state.WarningStreak = 0;
			    state.HasPrevDistance = false;
			    state.ContinuationAttempted = false;
			
			    state.PrimaryContainer = null;
			    state.SecondaryContainer = null;
			    state.PendingSecondaryContainer = null;
			    state.PendingSecondaryConfirmBars = 0;
			
			    result.Container = null;
			
			    state.PostFttGraceBars = 3;
			}
		}
		
		private static xApvaContainerCandidate SelectBestContainer(
		    ApvaAnalyzerState state,
		    Bar currentBar)
		{
		   	if (state.SecondaryContainer == null)
			    return state.PrimaryContainer;
			
			// Do not allow immature secondary to displace primary.
			if (!state.SecondaryContainer.HasValidP3)
			    return state.PrimaryContainer;
			
			int secondaryAge =
			    currentBar.Index - state.SecondaryContainer.P1.Index;
			
			double secondaryP2P3Distance =
			    Math.Abs(state.SecondaryContainer.P2.Price - state.SecondaryContainer.P3.Price);
			
			bool secondaryMature =
			    secondaryAge >= 4 &&
			    secondaryP2P3Distance >= 4.0;
			
			if (!secondaryMature)
			    return state.PrimaryContainer;
			
			
			double primaryScore = state.PrimaryContainer.Score;
			double secondaryScore = state.SecondaryContainer.Score;
			
			if (secondaryScore > primaryScore + 1.0)
			    return state.SecondaryContainer;
			
			if (primaryScore > secondaryScore + 1.0)
			    return state.PrimaryContainer;

			// Secondary must represent forward structural development,
			// not a backward-overlapping reinterpretation of the same area.
			bool secondaryIsForward =
			    state.SecondaryContainer.P1.Index >= state.PrimaryContainer.P3.Index - 2;
			
			if (!secondaryIsForward)
			    return state.PrimaryContainer;
		
		   	double primaryLtl =
		    	state.PrimaryContainer.LTL.ValueAt(currentBar.Index);
		
			double secondaryLtl =
			    state.SecondaryContainer.LTL.ValueAt(currentBar.Index);
			
			double primaryDistance =
			    Math.Abs(primaryLtl - currentBar.Close);
			
			double secondaryDistance =
			    Math.Abs(secondaryLtl - currentBar.Close);
		
		   double improvementRequired = 4.0; // points, temporary fixed threshold

		   bool secondaryMeaningfullyBetter =
			    secondaryDistance + improvementRequired < primaryDistance;
			
		   return secondaryMeaningfullyBetter
			    ? state.SecondaryContainer
			    : state.PrimaryContainer;
		}
		
		private static bool AreContainersEquivalent(
		    xApvaContainerCandidate a,
		    xApvaContainerCandidate b)
		{
		    if (a == null || b == null)
		        return false;
		
		    if (!a.HasValidP3 || !b.HasValidP3)
		        return false;
		
		    // Same structural anchors → equivalent
		    bool sameP1 = a.P1.Index == b.P1.Index;
		    bool sameP2 = a.P2.Index == b.P2.Index;
		    bool sameP3 = a.P3.Index == b.P3.Index;
		
		    return sameP1 && sameP2 && sameP3;
		}
		
		private static double ComputeContainerScore(
		    xApvaContainerCandidate c,
		    IReadOnlyList<VolumeSegment> segments,
		    Bar currentBar)
		{
		    if (c == null || !c.HasValidP3)
		        return double.MinValue;
		
		    double score = 0;
		
		    // 1. Maturity
		    int age = currentBar.Index - c.P1.Index;
		    if (age >= 4)
		        score += 2;
		
		    // 2. P2–P3 distance (strength)
		    double p2p3 = Math.Abs(c.P2.Price - c.P3.Price);
		    if (p2p3 > 4.0)
		        score += 2;
			
			// Penalize weak backward reinterpretations.
			if (c.P3.Index < currentBar.Index - 3)
			    score -= 2;
			
			if (p2p3 < 10.0)
			    score -= 2;
		
		    // 3. Volume alignment (VERY important)
		    var containerSegments = FilterSegmentsByContainer(segments, c);
		
		    var bias = xApvaDominanceEngine.GetContainerBias(containerSegments);
		
		    if (bias == DominanceState.Dominant)
		        score += 3;
		    else if (bias == DominanceState.CounterDominant)
		        score -= 2;
		
		    // 4. Recent bias stability
		    var recent = xApvaDominanceEngine.GetRecentBias(containerSegments, 3);
		
		    if (recent == bias)
		        score += 1;
		
		    return score;
		}

        public static ApvaAnalysisResult Analyze(
		    IReadOnlyList<Bar> bars,
		    IReadOnlyList<ClassifiedBar> classifiedBars,
		    double tickTolerance,
		    ApvaAnalyzerState state)
		{
		    var result = new ApvaAnalysisResult();
		    Bar currentBar = bars[bars.Count - 1];
			
			var toRemove = new List<FttEvent>();
			
			foreach (var evt in state.PendingFttEvents)
			{
			    int barsSince = currentBar.Index - evt.EntryBarIndex;
			    if (barsSince <= 0) continue;
			
			    double favorable = 0.0;
				double adverse = 0.0;
				
				if (evt.Direction == ContainerDirection.Down)
				{
				    // FTT of down container expects upside movement.
				    favorable = currentBar.High - evt.EntryPrice;
				    adverse   = currentBar.Low  - evt.EntryPrice;
				}
				else if (evt.Direction == ContainerDirection.Up)
				{
				    // FTT of up container expects downside movement.
				    favorable = evt.EntryPrice - currentBar.Low;
				    adverse   = evt.EntryPrice - currentBar.High;
				}
			
			    if (barsSince <= 5)
			    {
			        evt.MaxFavorableExcursion5 = Math.Max(evt.MaxFavorableExcursion5, favorable);
			        evt.MaxAdverseExcursion5 = Math.Min(evt.MaxAdverseExcursion5, adverse);
			    }
			
			    if (barsSince <= 10)
			    {
			        evt.MaxFavorableExcursion10 = Math.Max(evt.MaxFavorableExcursion10, favorable);
			        evt.MaxAdverseExcursion10 = Math.Min(evt.MaxAdverseExcursion10, adverse);
			    }
			
			    if (barsSince <= 20)
			    {
			        evt.MaxFavorableExcursion20 = Math.Max(evt.MaxFavorableExcursion20, favorable);
			        evt.MaxAdverseExcursion20 = Math.Min(evt.MaxAdverseExcursion20, adverse);
			    }
			
			    evt.BarsTracked = barsSince;
			
			    if (evt.BarsTracked >= 20)
			    {
			        result.CompletedFttEvents.Add(evt);
			        toRemove.Add(evt);
			    }
			}
			
			foreach (var evt in toRemove)
			{
			    state.PendingFttEvents.Remove(evt);
			}
		
		    BuildAndExtendContainer(
					    result,
					    bars,
					    currentBar,
					    tickTolerance,
					    state);
		
		    bool continuationFailed =
		        result.Container != null &&
		        result.Container.ExpectedContinuationFailed(
		            currentBar,
		            tickTolerance);
			
			 BuildSegmentsAndDominance(
			    result,
			    bars,
			    classifiedBars);
			
			if (state.PrimaryContainer != null)
			    state.PrimaryContainer.Score =
			        ComputeContainerScore(state.PrimaryContainer, result.Segments, currentBar);
			
			if (state.SecondaryContainer != null)
			    state.SecondaryContainer.Score =
			        ComputeContainerScore(state.SecondaryContainer, result.Segments, currentBar);
			
			// Re-select after current-bar scores are known.
			// This prevents SelectBestContainer() from using stale scores.
			result.Container = SelectBestContainer(state, currentBar);
			
			ComputeDistanceToLtl(result, currentBar);
			
			// Recompute continuation failure against the selected container.
			continuationFailed =
			    result.Container != null &&
			    result.Container.ExpectedContinuationFailed(
			        currentBar,
			        tickTolerance);
			
			PromoteP3OnStrongContinuation(
			    result,
			    currentBar,
			    continuationFailed,
			    tickTolerance);
		
		    ComputeContainerDiagnostics(
		        result,
		        currentBar,
		        tickTolerance);
		
		    UpdateDistanceState(
		        result,
		        state);
		
			UpdateStructuralRelevance(
				result,
				state,
				tickTolerance);
			
			const int MaxDriftBars = 5;

			if (state.BarsFarFromStructure >= MaxDriftBars)
			{
			    state.PrimaryContainer = null;
			    state.SecondaryContainer = null;
			    state.PendingSecondaryContainer = null;
			    state.PendingSecondaryConfirmBars = 0;
			    state.BarsFarFromStructure = 0;
			
			    result.Container = null;
			    return result;
			}
			
		    bool continuationAttempted =
		        UpdateContinuationAttemptState(
		            result,
		            state);
		
		    DetectFtt(
			    result,
			    state,
			    continuationFailed,
			    continuationAttempted);
			
			UpdateWarningState(
			    result,
			    state,
			    continuationFailed,
			    tickTolerance);
			
			result.SelectedContainerScoreSnapshot =
			    result.Container != null ? result.Container.Score : double.NaN;
			
			result.PrimaryContainerScoreSnapshot =
			    state.PrimaryContainer != null ? state.PrimaryContainer.Score : double.NaN;
			
			result.SecondaryContainerScoreSnapshot =
			    state.SecondaryContainer != null ? state.SecondaryContainer.Score : double.NaN;
			
			GateFtt(
			    result,
			    state,
			    currentBar,
			    continuationFailed,
			    continuationAttempted);
			
			if (!result.Ftt.IsConfirmed && state.PostFttGraceBars > 0)
    			state.PostFttGraceBars--;
		
		    return result;
		}
		
		private static List<VolumeSegment> FilterSegmentsByContainer(
		    IReadOnlyList<VolumeSegment> segments,
		    xApvaContainerCandidate container)
		{
		    var filtered = new List<VolumeSegment>();
		
		    if (segments == null || container == null)
		        return filtered;
		
		    int start = container.P1.Index;
		    int end = container.P3.Index;
		
		    foreach (VolumeSegment segment in segments)
		    {
		        if (segment.EndIndex >= start && segment.StartIndex <= end)
		            filtered.Add(segment);
		    }
		
		    return filtered;
		}
    }
}



























































