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
    }

    public static class xApvaAnalyzer
    {
        public static ApvaAnalysisResult Analyze(
		    IReadOnlyList<Bar> bars,
		    IReadOnlyList<ClassifiedBar> classifiedBars,
		    double tickTolerance)
		{
		    var result = new ApvaAnalysisResult();
		
		    result.Container =
		        xApvaContainerBuilder.BuildFromSwings(
		            bars,
		            swingStrength: 1,
		            tickTolerance: tickTolerance);
		
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
		
		    result.ContainerBias =
		        xApvaDominanceEngine.GetContainerBias(result.Segments);
		
		    result.RecentBias =
		        xApvaDominanceEngine.GetRecentBias(result.Segments, 5);
		
		    result.HasDominanceSequence =
		        xApvaDominanceEngine.HasDominanceSequence(result.Segments);
		
		    result.HasFailureSequence =
		        xApvaDominanceEngine.HasFailureSequence(result.Segments);
		
		    result.Ftt =
		        xApvaFttDetector.Detect(
		            result.Segments,
		            hasValidP3: result.Container != null && result.Container.HasValidP3,
		            expectedContinuationFailed:
				    result.Container != null &&
				    result.Container.ExpectedContinuationFailed(
				        bars[bars.Count - 1],
				        tickTolerance));
		
		    return result;
		}
    }
}




