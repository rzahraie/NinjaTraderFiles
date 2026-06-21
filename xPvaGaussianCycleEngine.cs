#region Using declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaGaussianCycleDirection
    {
        Unknown,
        Black,
        Red
    }

    public enum xPvaGaussianCycleState
    {
        Unknown,
        Building,
        Complete,
        Failed,
        Suspended,
        LeftToRight,
        RetraceOnly
    }

    public enum xPvaGaussianCyclePhase
    {
        Unknown,
        FirstDominantLeg,
        RetraceLeg,
        FinalDominantLeg
    }

    public sealed class xPvaGaussianCycle
    {
        public int StartBar;
        public int EndBar;
        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaGaussianCycleDirection Direction;
        public xPvaGaussianCycleState State;
        public xPvaGaussianCyclePhase Phase;

        public string Pattern;
        public string Reason;

        public int PeakBar;
        public int TroughBar;

        public bool HasPeak;
        public bool HasTrough;
        public bool HasAcceleration;
        public bool IsDominant;
        public bool IsRetrace;
        public bool IsLeftToRight;

        internal xPvaGaussianCycle Clone()
        {
            return (xPvaGaussianCycle)MemberwiseClone();
        }
    }

    /// <summary>
    /// Conservative Gaussian / volume-cycle interpretation from event sequence language.
    /// This layer does not draw, trade, or depend on container completion.
    /// </summary>
    public sealed class xPvaGaussianCycleEngine
    {
        private sealed class Leg
        {
            public int StartBar;
            public int EndBar;
            public DateTime StartTime;
            public DateTime EndTime;
            public xPvaGaussianCycleDirection Direction;
            public xPvaGaussianCyclePhase Phase;
            public xPvaSequenceKind Kind;
            public string Label;
            public int PeakBar;
            public bool HasPeak;
            public bool HasAcceleration;
        }

        private readonly List<Leg> committedLegs = new List<Leg>();
        private xPvaGaussianCycle lastCycle;
        private int lastCommittedSequenceId = -1;

        public xPvaGaussianCycle LastCycle
        {
            get { return lastCycle == null ? null : lastCycle.Clone(); }
        }

        public xPvaGaussianCycle Update(
            xPvaDiscreteEvent ev,
            xPvaLevel1Object level1,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence)
        {
            if (completedSequence != null && completedSequence.SequenceId != lastCommittedSequenceId)
            {
                Leg completedLeg = BuildLeg(completedSequence);
                if (completedLeg != null)
                {
                    committedLegs.Add(completedLeg);
                    TrimCommittedLegs();
                }
                lastCommittedSequenceId = completedSequence.SequenceId;
            }

            Leg activeLeg = BuildLeg(activeSequence);
            lastCycle = BuildCycle(activeLeg, ev, level1);
            return LastCycle;
        }

        private xPvaGaussianCycle BuildCycle(Leg activeLeg, xPvaDiscreteEvent ev, xPvaLevel1Object level1)
        {
            List<Leg> legs = EffectiveLegs(activeLeg);
            if (legs.Count == 0)
                return BuildUnknownCycle(ev);

            xPvaGaussianCycle complete = TryBuildCompleteCycle(legs);
            if (complete != null)
                return complete;

            xPvaGaussianCycle partial = TryBuildPartialCycle(legs);
            if (partial != null)
                return partial;

            xPvaGaussianCycle failed = TryBuildFailedCycle(legs);
            if (failed != null)
                return failed;

            Leg last = legs[legs.Count - 1];
            return BuildSingleLegCycle(last, level1);
        }

        private xPvaGaussianCycle TryBuildCompleteCycle(List<Leg> legs)
        {
            if (legs.Count < 3)
                return null;

            Leg first = legs[legs.Count - 3];
            Leg retrace = legs[legs.Count - 2];
            Leg final = legs[legs.Count - 1];

            if (first.Phase != xPvaGaussianCyclePhase.FirstDominantLeg
                || retrace.Phase != xPvaGaussianCyclePhase.RetraceLeg
                || final.Phase != xPvaGaussianCyclePhase.RetraceLeg)
                return null;

            if (first.Direction == xPvaGaussianCycleDirection.Unknown || final.Direction == xPvaGaussianCycleDirection.Unknown)
                return null;

            if (first.Direction != final.Direction)
                return null;

            if (!IsOpposite(first.Direction, retrace.Direction))
                return null;

            return new xPvaGaussianCycle
            {
                StartBar = first.StartBar,
                EndBar = final.EndBar,
                StartTime = first.StartTime,
                EndTime = final.EndTime,
                Direction = first.Direction,
                State = xPvaGaussianCycleState.Complete,
                Phase = xPvaGaussianCyclePhase.FinalDominantLeg,
                Pattern = Pattern(first, retrace, final),
                Reason = first.Direction + " Gaussian complete: " + ExpectedPattern(first.Direction) + " detected from sequence language; price geometry not asserted",
                PeakBar = PeakBar(first, retrace, final),
                TroughBar = retrace.EndBar,
                HasPeak = HasPeak(first, retrace, final),
                HasTrough = true,
                HasAcceleration = HasAcceleration(first, retrace, final),
                IsDominant = true,
                IsRetrace = false,
                IsLeftToRight = false
            };
        }

        private xPvaGaussianCycle TryBuildPartialCycle(List<Leg> legs)
        {
            if (legs.Count < 2)
                return null;

            Leg first = legs[legs.Count - 2];
            Leg second = legs[legs.Count - 1];

            if (first.Phase != xPvaGaussianCyclePhase.FirstDominantLeg || second.Phase != xPvaGaussianCyclePhase.RetraceLeg)
                return null;

            if (!IsOpposite(first.Direction, second.Direction))
                return null;

            return new xPvaGaussianCycle
            {
                StartBar = first.StartBar,
                EndBar = second.EndBar,
                StartTime = first.StartTime,
                EndTime = second.EndTime,
                Direction = first.Direction,
                State = xPvaGaussianCycleState.Building,
                Phase = xPvaGaussianCyclePhase.RetraceLeg,
                Pattern = Pattern(first, second),
                Reason = first.Direction + " Gaussian building: first dominant leg and retrace leg present; waiting for final dominant leg",
                PeakBar = PeakBar(first, second),
                TroughBar = second.EndBar,
                HasPeak = first.HasPeak || second.HasPeak,
                HasTrough = true,
                HasAcceleration = first.HasAcceleration || second.HasAcceleration,
                IsDominant = false,
                IsRetrace = true,
                IsLeftToRight = false
            };
        }

        private xPvaGaussianCycle TryBuildFailedCycle(List<Leg> legs)
        {
            if (legs.Count < 2)
                return null;

            Leg first = legs[legs.Count - 2];
            Leg second = legs[legs.Count - 1];

            if (first.Phase != xPvaGaussianCyclePhase.FirstDominantLeg || second.Phase != xPvaGaussianCyclePhase.FirstDominantLeg)
                return null;

            if (!IsOpposite(first.Direction, second.Direction))
                return null;

            return new xPvaGaussianCycle
            {
                StartBar = first.StartBar,
                EndBar = second.EndBar,
                StartTime = first.StartTime,
                EndTime = second.EndTime,
                Direction = first.Direction,
                State = xPvaGaussianCycleState.Failed,
                Phase = xPvaGaussianCyclePhase.FirstDominantLeg,
                Pattern = Pattern(first, second),
                Reason = first.Label + " was contradicted by opposite first-dominant leg " + second.Label + " before retrace/final completion",
                PeakBar = PeakBar(first, second),
                TroughBar = 0,
                HasPeak = first.HasPeak || second.HasPeak,
                HasTrough = false,
                HasAcceleration = first.HasAcceleration || second.HasAcceleration,
                IsDominant = true,
                IsRetrace = false,
                IsLeftToRight = false
            };
        }

        private xPvaGaussianCycle BuildSingleLegCycle(Leg leg, xPvaLevel1Object level1)
        {
            xPvaGaussianCycleState state = leg.Phase == xPvaGaussianCyclePhase.RetraceLeg
                ? xPvaGaussianCycleState.RetraceOnly
                : xPvaGaussianCycleState.Building;

            string reason = leg.Phase == xPvaGaussianCyclePhase.RetraceLeg
                ? leg.Label + " observed without a mature first-dominant/final-dominant Gaussian context"
                : leg.Label + " observed as first dominant leg only";

            bool leftToRight = level1 != null
                && level1.State == xPvaLevel1ObjectState.Complete
                && level1.Role == xPvaLevel1Role.StandaloneTape
                && leg.Phase == xPvaGaussianCyclePhase.RetraceLeg;

            if (leftToRight)
            {
                state = xPvaGaussianCycleState.LeftToRight;
                reason += "; Level 1 standalone tape suggests lateral/left-to-right behavior";
            }

            return new xPvaGaussianCycle
            {
                StartBar = leg.StartBar,
                EndBar = leg.EndBar,
                StartTime = leg.StartTime,
                EndTime = leg.EndTime,
                Direction = leg.Direction,
                State = state,
                Phase = leg.Phase,
                Pattern = leg.Label,
                Reason = reason,
                PeakBar = leg.PeakBar,
                TroughBar = 0,
                HasPeak = leg.HasPeak,
                HasTrough = false,
                HasAcceleration = leg.HasAcceleration,
                IsDominant = leg.Phase == xPvaGaussianCyclePhase.FirstDominantLeg || leg.Phase == xPvaGaussianCyclePhase.FinalDominantLeg,
                IsRetrace = leg.Phase == xPvaGaussianCyclePhase.RetraceLeg,
                IsLeftToRight = leftToRight
            };
        }

        private xPvaGaussianCycle BuildUnknownCycle(xPvaDiscreteEvent ev)
        {
            return new xPvaGaussianCycle
            {
                StartBar = ev != null ? ev.BarIndex : 0,
                EndBar = ev != null ? ev.BarIndex : 0,
                StartTime = ev != null ? ev.Time : DateTime.MinValue,
                EndTime = ev != null ? ev.Time : DateTime.MinValue,
                Direction = xPvaGaussianCycleDirection.Unknown,
                State = xPvaGaussianCycleState.Unknown,
                Phase = xPvaGaussianCyclePhase.Unknown,
                Pattern = "",
                Reason = "no directional Gaussian volume-cycle evidence"
            };
        }

        private Leg BuildLeg(xPvaEventSequence sequence)
        {
            if (sequence == null)
                return null;

            xPvaGaussianCyclePhase phase = PhaseFor(sequence.Kind);
            if (phase == xPvaGaussianCyclePhase.Unknown)
                return null;

            xPvaGaussianCycleDirection direction = DirectionFor(sequence.Kind);

            return new Leg
            {
                StartBar = sequence.StartBar,
                EndBar = sequence.EndBar,
                StartTime = sequence.StartTime,
                EndTime = sequence.EndTime,
                Direction = direction,
                Phase = phase,
                Kind = sequence.Kind,
                Label = SequenceLabel(sequence),
                PeakBar = sequence.ContainsPeakVolume ? sequence.MaxVolumeBar : 0,
                HasPeak = sequence.ContainsPeakVolume,
                HasAcceleration = sequence.AcceleratedPeakCount > 0 || sequence.ContainsCompressedRange || sequence.ContainsVolumeRangeMismatch
            };
        }

        private xPvaGaussianCyclePhase PhaseFor(xPvaSequenceKind kind)
        {
            if (kind == xPvaSequenceKind.CandidateB2B || kind == xPvaSequenceKind.CandidateR2R)
                return xPvaGaussianCyclePhase.FirstDominantLeg;

            if (kind == xPvaSequenceKind.Candidate2B || kind == xPvaSequenceKind.Candidate2R)
                return xPvaGaussianCyclePhase.RetraceLeg;

            return xPvaGaussianCyclePhase.Unknown;
        }

        private xPvaGaussianCycleDirection DirectionFor(xPvaSequenceKind kind)
        {
            if (kind == xPvaSequenceKind.CandidateB2B || kind == xPvaSequenceKind.Candidate2B)
                return xPvaGaussianCycleDirection.Black;

            if (kind == xPvaSequenceKind.CandidateR2R || kind == xPvaSequenceKind.Candidate2R)
                return xPvaGaussianCycleDirection.Red;

            return xPvaGaussianCycleDirection.Unknown;
        }

        private List<Leg> EffectiveLegs(Leg activeLeg)
        {
            var legs = new List<Leg>(committedLegs);

            if (activeLeg != null)
            {
                if (legs.Count == 0 || legs[legs.Count - 1].StartBar != activeLeg.StartBar || legs[legs.Count - 1].EndBar != activeLeg.EndBar)
                    legs.Add(activeLeg);
            }

            return legs;
        }

        private string SequenceLabel(xPvaEventSequence sequence)
        {
            return sequence.Kind + " [" + sequence.StartBar + "-" + sequence.EndBar + "]";
        }

        private string ExpectedPattern(xPvaGaussianCycleDirection direction)
        {
            if (direction == xPvaGaussianCycleDirection.Black)
                return "b2b -> 2r -> 2b";

            if (direction == xPvaGaussianCycleDirection.Red)
                return "r2r -> 2b -> 2r";

            return "unknown";
        }

        private bool IsOpposite(xPvaGaussianCycleDirection first, xPvaGaussianCycleDirection second)
        {
            return (first == xPvaGaussianCycleDirection.Black && second == xPvaGaussianCycleDirection.Red)
                || (first == xPvaGaussianCycleDirection.Red && second == xPvaGaussianCycleDirection.Black);
        }

        private string Pattern(params Leg[] legs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null)
                    continue;

                if (sb.Length > 0)
                    sb.Append(" -> ");

                sb.Append(legs[i].Label);
            }

            return sb.ToString();
        }

        private int PeakBar(params Leg[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] != null && legs[i].HasPeak)
                    return legs[i].PeakBar;
            }

            return 0;
        }

        private bool HasPeak(params Leg[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] != null && legs[i].HasPeak)
                    return true;
            }

            return false;
        }

        private bool HasAcceleration(params Leg[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] != null && legs[i].HasAcceleration)
                    return true;
            }

            return false;
        }

        private void TrimCommittedLegs()
        {
            while (committedLegs.Count > 8)
                committedLegs.RemoveAt(0);
        }
    }
}
