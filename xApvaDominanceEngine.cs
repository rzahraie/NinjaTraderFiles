using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaDominanceEngine
    {
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
    }
}