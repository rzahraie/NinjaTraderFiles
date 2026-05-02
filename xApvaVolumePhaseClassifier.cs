using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaVolumePhaseClassifier
    {
        private enum PhaseMachineState
        {
            NeedPP1,
            NeedPP2,
            NeedT1,
            NeedT2P,
            NeedPP3OrT2F
        }

        public static void Classify(
            IReadOnlyList<VolumeSegment> segments,
            ContainerDirection containerDirection)
        {
            if (segments == null || segments.Count == 0)
                return;

            if (containerDirection == ContainerDirection.Unknown)
                return;

            PhaseMachineState state = PhaseMachineState.NeedPP1;

            for (int i = 0; i < segments.Count; i++)
            {
                VolumeSegment segment = segments[i];

                segment.Phase = VolumePhase.Unknown;
                segment.Dominance = DominanceState.Unknown;

                bool dominant = IsDominant(segment, containerDirection);
                bool nonDominant = IsNonDominant(segment, containerDirection);
                bool strongVolume = IsStrongVolume(segment);

                if (dominant)
                    segment.Dominance = DominanceState.Dominant;
                else if (nonDominant)
                    segment.Dominance = DominanceState.NonDominant;
                else
                    continue;

                switch (state)
                {
                    case PhaseMachineState.NeedPP1:
                        if (dominant && strongVolume)
                        {
                            segment.Phase = VolumePhase.PP1;
                            state = PhaseMachineState.NeedPP2;
                        }
                        break;

                    case PhaseMachineState.NeedPP2:
                        if (dominant && strongVolume)
                        {
                            segment.Phase = VolumePhase.PP2;
                            state = PhaseMachineState.NeedT1;
                        }
                        else if (nonDominant)
                        {
                            state = PhaseMachineState.NeedPP1;
                        }
                        break;

                    case PhaseMachineState.NeedT1:
                        if (nonDominant)
                        {
                            segment.Phase = VolumePhase.T1;
                            state = PhaseMachineState.NeedT2P;
                        }
                        break;

                    case PhaseMachineState.NeedT2P:
                        if (dominant)
                        {
                            if (strongVolume)
                            {
                                segment.Phase = VolumePhase.T2P;
                                state = PhaseMachineState.NeedPP3OrT2F;
                            }
                            else
                            {
                                segment.Phase = VolumePhase.T2F;
                                state = PhaseMachineState.NeedPP1;
                            }
                        }
                        break;

                    case PhaseMachineState.NeedPP3OrT2F:
                        if (dominant && strongVolume)
                        {
                            segment.Phase = VolumePhase.PP3;
                            state = PhaseMachineState.NeedT1;
                        }
                        else if (nonDominant)
                        {
                            segment.Phase = VolumePhase.T2F;
                            state = PhaseMachineState.NeedPP1;
                        }
                        break;
                }
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
