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

    public static class xApvaAnalyzer
    {
		private static int _warningStreak = 0;
		private static double _prevDistanceToLtl = 0;
		private static bool _hasPrevDistanceToLtl = false;
		private static int _lastFttBarIndex = -1;
		
        public static ApvaAnalysisResult Analyze(
		    IReadOnlyList<Bar> bars,
		    IReadOnlyList<ClassifiedBar> classifiedBars,
		    double tickTolerance)
		{
		    var result = new ApvaAnalysisResult();
			
			Bar currentBar = bars[bars.Count - 1];
		
		    result.Container =
		        xApvaContainerBuilder.BuildFromSwings(
		            bars,
		            swingStrength: 1,
		            tickTolerance: tickTolerance);
			
			if (result.Container != null)
			{
			    result.Container.TryExtend(
			        currentBar,
			        tickTolerance);
			
			    double ltl = result.Container.LTL.ValueAt(currentBar.Index);
			
			    if (result.Container.Direction == ContainerDirection.Up)
			        result.DistanceToLtl = ltl - currentBar.High;
			    else if (result.Container.Direction == ContainerDirection.Down)
			        result.DistanceToLtl = currentBar.Low - ltl;
			}
			
			if (result.Container != null && result.Container.HasValidP3)
			{
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
			
			// THEN delta
			if (!_hasPrevDistanceToLtl)
			{
			    result.DistanceToLtlDelta = 0.0;
			    _hasPrevDistanceToLtl = true;
			}
			else
			{
			    result.DistanceToLtlDelta =
			        result.DistanceToLtl - _prevDistanceToLtl;
			}

			// FINALLY update state
			_prevDistanceToLtl = result.DistanceToLtl;
		
		    ContainerDirection direction =
		        result.Container != null
		            ? result.Container.Direction
		            : xApvaDirectionInferer.InferDirection(bars);
		
		    result.Segments = xApvaVolumeSegmentBuilder.BuildSegments(
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
			
			bool continuationFailed =
			    result.Container != null &&
			    result.Container.ExpectedContinuationFailed(
			        bars[bars.Count - 1],
			        tickTolerance);
			
			if (continuationFailed)
			{
			    _warningStreak++;
			}
			else
			{
			    // NEW: decay instead of hard reset
			    if (_warningStreak > 0)
			        _warningStreak--;
			}
			
			result.WarningDuration = _warningStreak;
			
			result.ImminentFtt =
			    result.WarningDuration >= 2 &&
			    result.ContainerBias == DominanceState.CounterDominant;
			
			result.IneffectiveDominance =
			    result.CurrentSegmentDominance == DominanceState.Dominant &&
			    result.DistanceToLtl > 0;
			
			result.Ftt =
			    xApvaFttDetector.Detect(
			        result.Segments,
			        hasValidP3: result.Container != null && result.Container.HasValidP3,
			        expectedContinuationFailed: continuationFailed);
			
			const int MinFttSeparationBars = 8;

			int barsSinceLastFtt =
			    _lastFttBarIndex >= 0
			        ? currentBar.Index - _lastFttBarIndex
			        : int.MaxValue;
			
			result.BarsSinceLastFtt = barsSinceLastFtt;
			
			if (result.Ftt.IsConfirmed)
			{
			    if (barsSinceLastFtt >= MinFttSeparationBars)
			    {
			        _lastFttBarIndex = currentBar.Index;
			    }
			    else
			    {
			        result.Ftt.IsConfirmed = false;
			        result.Ftt.Reason += " Blocked by FTT cooldown.";
			    }
			}
			
			if (result.Ftt.IsConfirmed)
			{
			    _warningStreak = 0;
			    _hasPrevDistanceToLtl = false;
			}
		
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































