#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaGaussianNarrativeState
    {
        Unknown,

        BuildingBlackCycle,
        BuildingRedCycle,

        BlackRetrace,
        RedRetrace,

        BlackCycleComplete,
        RedCycleComplete,

        BlackCycleFailure,
        RedCycleFailure,

        LeftToRight,

        Transitioning
    }

    public sealed class xPvaGaussianNarrative
    {
        public int StartBar;
        public int EndBar;

        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaGaussianNarrativeState State;

        public xPvaGaussianCycleDirection Direction;

        public string Pattern;
        public string Reason;

        public int ActiveCycleStartBar;
        public int ActiveCycleEndBar;

        public int PeakBar;
        public int TroughBar;

        public bool HasAcceleration;

        internal xPvaGaussianNarrative Clone()
        {
            return (xPvaGaussianNarrative)MemberwiseClone();
        }
    }

    /// <summary>
    /// Expresses the current Gaussian story from Gaussian cycle state.
    /// This layer does not draw, trade, or depend on container completion.
    /// </summary>
    public sealed class xPvaGaussianNarrativeEngine
    {
        private xPvaGaussianNarrative lastNarrative;
        private xPvaGaussianCycleDirection lastCompletedDirection = xPvaGaussianCycleDirection.Unknown;
        private int lastCompletedEndBar = -1;

        public xPvaGaussianNarrative LastNarrative
        {
            get { return lastNarrative == null ? null : lastNarrative.Clone(); }
        }

        public xPvaGaussianNarrative Update(xPvaGaussianCycle gaussian)
        {
            lastNarrative = BuildNarrative(gaussian);
            return LastNarrative;
        }

        private xPvaGaussianNarrative BuildNarrative(xPvaGaussianCycle gaussian)
        {
            if (gaussian == null || gaussian.Direction == xPvaGaussianCycleDirection.Unknown || gaussian.State == xPvaGaussianCycleState.Unknown)
                return BuildUnknownNarrative(gaussian, "no active Gaussian cycle narrative");

            xPvaGaussianNarrativeState state = StateFor(gaussian);
            string reason = ReasonFor(gaussian, state);

            if (IsOppositeCompletedCycleDeveloping(gaussian, state))
            {
                state = xPvaGaussianNarrativeState.Transitioning;
                reason = "opposing Gaussian cycle is developing after prior completed "
                    + lastCompletedDirection
                    + " cycle ending at "
                    + lastCompletedEndBar
                    + "; not asserting new dominance";
            }

            xPvaGaussianNarrative narrative = new xPvaGaussianNarrative
            {
                StartBar = gaussian.StartBar,
                EndBar = gaussian.EndBar,
                StartTime = gaussian.StartTime,
                EndTime = gaussian.EndTime,
                State = state,
                Direction = gaussian.Direction,
                Pattern = gaussian.Pattern,
                Reason = reason,
                ActiveCycleStartBar = gaussian.StartBar,
                ActiveCycleEndBar = gaussian.EndBar,
                PeakBar = gaussian.HasPeak ? gaussian.PeakBar : 0,
                TroughBar = gaussian.HasTrough ? gaussian.TroughBar : 0,
                HasAcceleration = gaussian.HasAcceleration
            };

            RememberCompletedCycle(gaussian);
            return narrative;
        }

        private xPvaGaussianNarrativeState StateFor(xPvaGaussianCycle gaussian)
        {
            if (gaussian.State == xPvaGaussianCycleState.LeftToRight || gaussian.IsLeftToRight)
                return xPvaGaussianNarrativeState.LeftToRight;

            if (gaussian.State == xPvaGaussianCycleState.Failed)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    ? xPvaGaussianNarrativeState.BlackCycleFailure
                    : xPvaGaussianNarrativeState.RedCycleFailure;

            if (gaussian.State == xPvaGaussianCycleState.Complete)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    ? xPvaGaussianNarrativeState.BlackCycleComplete
                    : xPvaGaussianNarrativeState.RedCycleComplete;

            if (gaussian.Phase == xPvaGaussianCyclePhase.RetraceLeg || gaussian.State == xPvaGaussianCycleState.RetraceOnly)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    ? xPvaGaussianNarrativeState.BlackRetrace
                    : xPvaGaussianNarrativeState.RedRetrace;

            if (gaussian.Phase == xPvaGaussianCyclePhase.FirstDominantLeg || gaussian.State == xPvaGaussianCycleState.Building)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    ? xPvaGaussianNarrativeState.BuildingBlackCycle
                    : xPvaGaussianNarrativeState.BuildingRedCycle;

            return xPvaGaussianNarrativeState.Unknown;
        }

        private string ReasonFor(xPvaGaussianCycle gaussian, xPvaGaussianNarrativeState state)
        {
            if (state == xPvaGaussianNarrativeState.BlackCycleComplete)
                return "Black Gaussian story complete from b2b -> 2r -> 2b; " + gaussian.Reason;

            if (state == xPvaGaussianNarrativeState.RedCycleComplete)
                return "Red Gaussian story complete from r2r -> 2b -> 2r; " + gaussian.Reason;

            if (state == xPvaGaussianNarrativeState.BlackRetrace)
                return "Black-side retrace narrative: counter leg is active or isolated; waiting for final black continuation";

            if (state == xPvaGaussianNarrativeState.RedRetrace)
                return "Red-side retrace narrative: counter leg is active or isolated; waiting for final red continuation";

            if (state == xPvaGaussianNarrativeState.BuildingBlackCycle)
                return "Black Gaussian narrative building from first dominant leg; cycle is not complete";

            if (state == xPvaGaussianNarrativeState.BuildingRedCycle)
                return "Red Gaussian narrative building from first dominant leg; cycle is not complete";

            if (state == xPvaGaussianNarrativeState.BlackCycleFailure)
                return "Black Gaussian narrative failed before completion; " + gaussian.Reason;

            if (state == xPvaGaussianNarrativeState.RedCycleFailure)
                return "Red Gaussian narrative failed before completion; " + gaussian.Reason;

            if (state == xPvaGaussianNarrativeState.LeftToRight)
                return "Gaussian cycle evidence is lateral/left-to-right; directional narrative is not forced";

            return string.IsNullOrEmpty(gaussian.Reason) ? "Gaussian narrative unknown" : gaussian.Reason;
        }

        private bool IsOppositeCompletedCycleDeveloping(xPvaGaussianCycle gaussian, xPvaGaussianNarrativeState state)
        {
            if (lastCompletedDirection == xPvaGaussianCycleDirection.Unknown || gaussian.Direction == xPvaGaussianCycleDirection.Unknown)
                return false;

            if (!IsOpposite(lastCompletedDirection, gaussian.Direction))
                return false;

            return state == xPvaGaussianNarrativeState.BuildingBlackCycle
                || state == xPvaGaussianNarrativeState.BuildingRedCycle
                || state == xPvaGaussianNarrativeState.BlackRetrace
                || state == xPvaGaussianNarrativeState.RedRetrace
                || state == xPvaGaussianNarrativeState.BlackCycleFailure
                || state == xPvaGaussianNarrativeState.RedCycleFailure;
        }

        private void RememberCompletedCycle(xPvaGaussianCycle gaussian)
        {
            if (gaussian.State != xPvaGaussianCycleState.Complete || gaussian.EndBar == lastCompletedEndBar)
                return;

            lastCompletedDirection = gaussian.Direction;
            lastCompletedEndBar = gaussian.EndBar;
        }

        private xPvaGaussianNarrative BuildUnknownNarrative(xPvaGaussianCycle gaussian, string reason)
        {
            return new xPvaGaussianNarrative
            {
                StartBar = gaussian != null ? gaussian.StartBar : 0,
                EndBar = gaussian != null ? gaussian.EndBar : 0,
                StartTime = gaussian != null ? gaussian.StartTime : DateTime.MinValue,
                EndTime = gaussian != null ? gaussian.EndTime : DateTime.MinValue,
                State = xPvaGaussianNarrativeState.Unknown,
                Direction = gaussian != null ? gaussian.Direction : xPvaGaussianCycleDirection.Unknown,
                Pattern = gaussian != null ? gaussian.Pattern : "",
                Reason = reason,
                ActiveCycleStartBar = gaussian != null ? gaussian.StartBar : 0,
                ActiveCycleEndBar = gaussian != null ? gaussian.EndBar : 0,
                PeakBar = gaussian != null && gaussian.HasPeak ? gaussian.PeakBar : 0,
                TroughBar = gaussian != null && gaussian.HasTrough ? gaussian.TroughBar : 0,
                HasAcceleration = gaussian != null && gaussian.HasAcceleration
            };
        }

        private bool IsOpposite(xPvaGaussianCycleDirection first, xPvaGaussianCycleDirection second)
        {
            return (first == xPvaGaussianCycleDirection.Black && second == xPvaGaussianCycleDirection.Red)
                || (first == xPvaGaussianCycleDirection.Red && second == xPvaGaussianCycleDirection.Black);
        }
    }
}
