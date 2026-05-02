using System;
using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaVolumePhaseClassifier
    {
        public static void Classify(
            IReadOnlyList<VolumeSegment> segments,
            ContainerDirection containerDirection)
        {
            if (segments == null || segments.Count == 0)
                return;

            if (containerDirection == ContainerDirection.Unknown)
                return;

            VolumePhase lastPhase = VolumePhase.Unknown;
            int dominantCount = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                VolumeSegment segment = segments[i];

                bool dominant = IsDominant(segment, containerDirection);
                bool nonDominant = IsNonDominant(segment, containerDirection);
                bool strongVolume = IsStrongVolume(segment);

                if (dominant)
                {
                    segment.Dominance = DominanceState.Dominant;

                    if (lastPhase == VolumePhase.Unknown)
                    {
                        segment.Phase = VolumePhase.PP1;
                        dominantCount = 1;
                    }
                    else if (lastPhase == VolumePhase.PP1)
                    {
                        segment.Phase = VolumePhase.PP2;
                        dominantCount = 2;
                    }
                    else if (lastPhase == VolumePhase.T1)
                    {
                        segment.Phase = strongVolume ? VolumePhase.T2P : VolumePhase.T2F;
                    }
                    else if (lastPhase == VolumePhase.T2P)
                    {
                        segment.Phase = strongVolume ? VolumePhase.PP3 : VolumePhase.T2F;
                    }
                    else
                    {
                        dominantCount++;
                        segment.Phase = dominantCount >= 3 ? VolumePhase.PP3 : VolumePhase.PP2;
                    }

                    lastPhase = segment.Phase;
                    continue;
                }

                if (nonDominant)
                {
                    segment.Dominance = DominanceState.NonDominant;

                    if (lastPhase == VolumePhase.PP1 ||
                        lastPhase == VolumePhase.PP2 ||
                        lastPhase == VolumePhase.PP3)
                    {
                        segment.Phase = VolumePhase.T1;
                    }
                    else if (lastPhase == VolumePhase.T2P)
                    {
                        segment.Phase = VolumePhase.T2F;
                    }
                    else
                    {
                        segment.Phase = VolumePhase.T1;
                    }

                    lastPhase = segment.Phase;
                    continue;
                }

                segment.Dominance = DominanceState.Unknown;
                segment.Phase = VolumePhase.Unknown;
            }
        }

        private static bool IsDominant(
            VolumeSegment segment,
            ContainerDirection containerDirection)
        {
            if (containerDirection == ContainerDirection.Up)
            {
                return segment.Color == VolumeColor.Black &&
                       segment.Direction == SegmentDirection.Up;
            }

            if (containerDirection == ContainerDirection.Down)
            {
                return segment.Color == VolumeColor.Red &&
                       segment.Direction == SegmentDirection.Down;
            }

            return false;
        }

        private static bool IsNonDominant(
            VolumeSegment segment,
            ContainerDirection containerDirection)
        {
            if (containerDirection == ContainerDirection.Up)
            {
                return segment.Color == VolumeColor.Red &&
                       segment.Direction == SegmentDirection.Down;
            }

            if (containerDirection == ContainerDirection.Down)
            {
                return segment.Color == VolumeColor.Black &&
                       segment.Direction == SegmentDirection.Up;
            }

            return false;
        }

        private static bool IsStrongVolume(VolumeSegment segment)
        {
            return segment.Rank == VolumeRank.Elevated ||
                   segment.Rank == VolumeRank.Peak ||
                   segment.Rank == VolumeRank.Climax;
        }
    }
}