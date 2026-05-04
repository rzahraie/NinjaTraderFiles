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
    }
	
	public sealed class ApvaAnalyzerState
	{
	    public int WarningStreak = 0;
	    public double PrevDistanceToLtl = 0;
	    public bool HasPrevDistance = false;
	    public int LastFttBarIndex = -1;
		public bool ContinuationAttempted = false;
	}

    public static class xApvaAnalyzer
    {
		private static void BuildAndExtendContainer(
		    ApvaAnalysisResult result,
		    IReadOnlyList<Bar> bars,
		    Bar currentBar,
		    double tickTolerance)
		{
		    result.Container =
		        xApvaContainerBuilder.BuildFromSwings(
		            bars,
		            swingStrength: 1,
		            tickTolerance: tickTolerance);
		
		    if (result.Container == null)
		        return;
		
		    result.Container.TryExtend(
			    currentBar,
			    tickTolerance,
			    allowP3Promotion: false);
		
		    double ltl = result.Container.LTL.ValueAt(currentBar.Index);
		
		    if (result.Container.Direction == ContainerDirection.Up)
		        result.DistanceToLtl = ltl - currentBar.High;
		    else if (result.Container.Direction == ContainerDirection.Down)
		        result.DistanceToLtl = currentBar.Low - ltl;
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
			    result.Container.P3.Index = currentBar.Index;
			    result.Container.P3.Price = currentBar.Close;
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

		private static void UpdateWarningState(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    bool continuationFailed)
		{
		    if (continuationFailed)
		    {
		        state.WarningStreak++;
		    }
		    else
		    {
		        if (state.WarningStreak > 0)
		            state.WarningStreak--;
		    }
		
		    result.WarningDuration = state.WarningStreak;
		
		    result.ImminentFtt =
		        result.WarningDuration >= 2 &&
		        result.ContainerBias == DominanceState.CounterDominant;
		
		    result.IneffectiveDominance =
		        result.CurrentSegmentDominance == DominanceState.Dominant &&
		        result.DistanceToLtl > 0;
		}

		private static void DetectAndGateFtt(
		    ApvaAnalysisResult result,
		    ApvaAnalyzerState state,
		    Bar currentBar,
		    bool continuationFailed,
		    bool continuationAttempted)
		{
		    result.Ftt =
		        xApvaFttDetector.Detect(
		            result.Segments,
		            hasValidP3: result.Container != null && result.Container.HasValidP3,
		            expectedContinuationFailed: continuationFailed,
		            continuationAttempted: continuationAttempted);
		
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
		        if (!result.IsMatureContainer)
		        {
		            result.Ftt.IsConfirmed = false;
		            result.Ftt.Reason += " Blocked by immature container.";
		        }
		        else
		        {
		            state.LastFttBarIndex = currentBar.Index;
		        }
		    }
		
		    if (result.Ftt.IsConfirmed)
		    {
		        state.WarningStreak = 0;
		        state.HasPrevDistance = false;
		        state.ContinuationAttempted = false;
		    }
		}
		
        public static ApvaAnalysisResult Analyze(
		    IReadOnlyList<Bar> bars,
		    IReadOnlyList<ClassifiedBar> classifiedBars,
		    double tickTolerance,
		    ApvaAnalyzerState state)
		{
		    var result = new ApvaAnalysisResult();
		    Bar currentBar = bars[bars.Count - 1];
		
		    BuildAndExtendContainer(result, bars, currentBar, tickTolerance);
		
		    bool continuationFailed =
		        result.Container != null &&
		        result.Container.ExpectedContinuationFailed(
		            currentBar,
		            tickTolerance);
			
			 BuildSegmentsAndDominance(
		        result,
		        bars,
		        classifiedBars);
		
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
		
		    bool continuationAttempted =
		        UpdateContinuationAttemptState(
		            result,
		            state);
		
		    UpdateWarningState(
		        result,
		        state,
		        continuationFailed);
		
		    DetectAndGateFtt(
		        result,
		        state,
		        currentBar,
		        continuationFailed,
		        continuationAttempted);
		
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

