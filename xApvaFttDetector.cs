using System.Collections.Generic;

namespace APVA.Core
{
    public sealed class FttResult
	{
	    public bool IsCandidate { get; set; }
	    public bool IsConfirmed { get; set; }
	
	    public FttKind Kind { get; set; } = FttKind.None;
	
	    public int SegmentIndex { get; set; } = -1;
	    public int BarIndex { get; set; } = -1;
	
	    public string Reason { get; set; } = string.Empty;
		
		public int WarningDuration { get; set; } = 0;
	}

    public static class xApvaFttDetector
    {
        public static FttResult Detect(
		    IReadOnlyList<VolumeSegment> segments,
		    bool hasValidP3,
		    bool expectedContinuationFailed)
		{
		    var result = new FttResult();
		
		    if (segments == null || segments.Count == 0)
		        return result;
		
		    int warningCount = CountTrailingWarningSegments(segments);
		
		    for (int i = 0; i < segments.Count; i++)
		    {
		        VolumeSegment segment = segments[i];
		
		        if (segment.Phase != VolumePhase.T2F)
		            continue;
		
		        result.IsCandidate = true;
		        result.SegmentIndex = i;
		        result.BarIndex = segment.EndIndex;
		        result.Reason = "T2F detected after dominance expectation.";
		        result.WarningDuration = warningCount;
		
		        if (hasValidP3 && expectedContinuationFailed)
		        {
		            result.IsConfirmed = true;
		            result.Kind = FttKind.T2F_FailedContinuation;
		            result.Reason = "T2F plus valid P3 and failed structural continuation.";
		        }
		
		        return result;
		    }
		
		    if (hasValidP3 && expectedContinuationFailed)
		    {
		        result.IsCandidate = true;
		        result.IsConfirmed = false;
		        result.Kind = FttKind.StructuralWarning;
		        result.WarningDuration = warningCount;
		        result.Reason = "Valid P3 and failed structural continuation, but no T2F confirmation.";
		    }
		
		    return result;
		}
		
		private static int CountTrailingWarningSegments(
	    	IReadOnlyList<VolumeSegment> segments)
		{
		    int count = 0;
		
		    for (int i = segments.Count - 1; i >= 0; i--)
		    {
		        DominanceState dominance = segments[i].Dominance;
		
		        if (dominance == DominanceState.NonDominant ||
		            dominance == DominanceState.CounterDominant)
		        {
		            count++;
		        }
		        else
		        {
		            break;
		        }
		    }
		
		    return count;
		}	
    }
}
