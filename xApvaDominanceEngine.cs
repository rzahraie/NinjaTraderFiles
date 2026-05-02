using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaDominanceEngine
    {
		public static DominanceState GetCurrentSegmentDominance(
    			IReadOnlyList<VolumeSegment> segments)
		{
		    if (segments == null || segments.Count == 0)
		        return DominanceState.Unknown;
		
		    for (int i = segments.Count - 1; i >= 0; i--)
		    {
		        if (segments[i].Dominance != DominanceState.Unknown)
		            return segments[i].Dominance;
		    }
		
		    return DominanceState.Unknown;
		}
		
		public static DominanceState GetContainerBias(
		    IReadOnlyList<VolumeSegment> segments)
		{
		    if (segments == null || segments.Count == 0)
		        return DominanceState.Unknown;
		
		    bool hasPP1 = false;
		    bool hasPP2 = false;
		    bool hasT1 = false;
		    bool hasT2P = false;
		    bool hasCounterDominance = false;
		
		    foreach (VolumeSegment segment in segments)
		    {
		        if (segment.Dominance == DominanceState.CounterDominant)
		            hasCounterDominance = true;
		
		        if (segment.Phase == VolumePhase.PP1)
		            hasPP1 = true;
		
		        if (hasPP1 && segment.Phase == VolumePhase.PP2)
		            hasPP2 = true;
		
		        if (hasPP2 && segment.Phase == VolumePhase.T1)
		            hasT1 = true;
		
		        if (hasT1 && segment.Phase == VolumePhase.T2P)
		            hasT2P = true;
		    }
		
		    if (hasCounterDominance)
		        return DominanceState.CounterDominant;
		
		    if (hasPP1 && hasPP2 && hasT1 && hasT2P)
		        return DominanceState.Dominant;
		
		    if (hasPP1 && hasPP2)
		        return DominanceState.Dominant;
		
		    if (hasT1)
		        return DominanceState.NonDominant;
		
		    return DominanceState.Unknown;
		}

        public static DominanceState GetContainerDominance(
            IReadOnlyList<VolumeSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return DominanceState.Unknown;

            VolumeSegment lastKnown = null;

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i].Dominance != DominanceState.Unknown)
                {
                    lastKnown = segments[i];
                    break;
                }
            }

            if (lastKnown == null)
                return DominanceState.Unknown;

            if (lastKnown.Phase == VolumePhase.PP3)
                return DominanceState.Exhaustion;

            if (lastKnown.Phase == VolumePhase.T2F)
                return DominanceState.CounterDominant;

            return lastKnown.Dominance;
        }

        public static bool HasDominanceSequence(
            IReadOnlyList<VolumeSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return false;

            bool hasPP1 = false;
            bool hasPP2 = false;
            bool hasT1 = false;
            bool hasT2P = false;

            foreach (VolumeSegment segment in segments)
            {
                if (segment.Phase == VolumePhase.PP1)
                    hasPP1 = true;

                if (hasPP1 && segment.Phase == VolumePhase.PP2)
                    hasPP2 = true;

                if (hasPP2 && segment.Phase == VolumePhase.T1)
                    hasT1 = true;

                if (hasT1 && segment.Phase == VolumePhase.T2P)
                    hasT2P = true;
            }

            return hasPP1 && hasPP2 && hasT1 && hasT2P;
        }

        public static bool HasFailureSequence(
            IReadOnlyList<VolumeSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return false;

            bool hadT2POrPP3 = false;

            foreach (VolumeSegment segment in segments)
            {
                if (segment.Phase == VolumePhase.T2P ||
                    segment.Phase == VolumePhase.PP3)
                {
                    hadT2POrPP3 = true;
                }

                if (hadT2POrPP3 &&
                    segment.Phase == VolumePhase.T2F)
                {
                    return true;
                }
            }

            return false;
        }
		
		public static DominanceState GetRecentBias(
		    IReadOnlyList<VolumeSegment> segments,
		    int lookbackSegments)
		{
		    if (segments == null || segments.Count == 0)
		        return DominanceState.Unknown;
		
		    int start = segments.Count - lookbackSegments;
		    if (start < 0)
		        start = 0;
		
		    int dominant = 0;
		    int nonDominant = 0;
		    int counterDominant = 0;
		
		    for (int i = start; i < segments.Count; i++)
		    {
		        if (segments[i].Dominance == DominanceState.Dominant)
		            dominant++;
		
		        if (segments[i].Dominance == DominanceState.NonDominant)
		            nonDominant++;
		
		        if (segments[i].Dominance == DominanceState.CounterDominant)
		            counterDominant++;
		    }
		
		   // If the LAST segment is counter-dominant, that overrides everything
			if (segments[segments.Count - 1].Dominance == DominanceState.CounterDominant)
			    return DominanceState.CounterDominant;
			
			// Strong dominance recovery
			if (dominant > nonDominant + counterDominant)
			    return DominanceState.Dominant;
			
			// Persistent counter-dominance
			if (counterDominant > dominant)
			    return DominanceState.CounterDominant;
			
			// Weak pullback bias
			if (nonDominant > dominant)
			    return DominanceState.NonDominant;
			
			return DominanceState.Unknown;
		}
    }
}


