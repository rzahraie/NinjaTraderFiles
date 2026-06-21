#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaGaussianExpectation
    {
        None,

        ExpectBlackRetrace,
        ExpectRedRetrace,

        ExpectBlackResumption,
        ExpectRedResumption,

        ExpectTransition,

        ExpectLeftToRight,

        ExpectFailure,

        ExpectNewCycle
    }

    public sealed class xPvaGaussianExpectationState
    {
        public int StartBar;
        public int EndBar;

        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaGaussianExpectation Expectation;

        public xPvaGaussianCycleDirection ExpectedDirection;

        public string ExpectedPattern;
        public string Reason;

        public bool IsExpectationMet;
        public bool IsExpectationViolated;

        public int ResolutionBar;
        public string ResolutionReason;

        internal xPvaGaussianExpectationState Clone()
        {
            return (xPvaGaussianExpectationState)MemberwiseClone();
        }
    }

    /// <summary>
    /// Grammatical Gaussian expectation layer.
    /// This layer describes what should come next if Gaussian grammar continues; it is not a trade signal.
    /// </summary>
    public sealed class xPvaGaussianExpectationEngine
    {
        private xPvaGaussianExpectationState pendingExpectation;
        private xPvaGaussianExpectationState lastExpectation;

        public xPvaGaussianExpectationState LastExpectation
        {
            get { return lastExpectation == null ? null : lastExpectation.Clone(); }
        }

        public xPvaGaussianExpectationState Update(xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative)
        {
            xPvaGaussianExpectationState resolution = TryResolvePendingExpectation(gaussian, narrative);
            xPvaGaussianExpectationState current = BuildExpectation(gaussian, narrative);

            if (resolution != null)
            {
                current.IsExpectationMet = resolution.IsExpectationMet;
                current.IsExpectationViolated = resolution.IsExpectationViolated;
                current.ResolutionBar = resolution.ResolutionBar;
                current.ResolutionReason = resolution.ResolutionReason;
            }

            if (current.Expectation != xPvaGaussianExpectation.None)
                pendingExpectation = UnresolvedCopy(current);
            else if (resolution != null)
                pendingExpectation = null;

            lastExpectation = current;
            return LastExpectation;
        }

        private xPvaGaussianExpectationState BuildExpectation(xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative)
        {
            if (narrative == null || narrative.State == xPvaGaussianNarrativeState.Unknown)
                return BuildNone(gaussian, narrative, "no Gaussian narrative expectation");

            xPvaGaussianExpectationState state = BaseState(gaussian, narrative);

            if (narrative.State == xPvaGaussianNarrativeState.BuildingRedCycle)
                return Fill(state, xPvaGaussianExpectation.ExpectRedResumption, xPvaGaussianCycleDirection.Red, "final red leg / 2r resumption", "red cycle has begun but has not completed");

            if (narrative.State == xPvaGaussianNarrativeState.RedRetrace)
                return Fill(state, xPvaGaussianExpectation.ExpectRedResumption, xPvaGaussianCycleDirection.Red, "final red leg / 2r resumption", "red cycle is in retrace phase; final red leg is expected if grammar continues");

            if (narrative.State == xPvaGaussianNarrativeState.RedCycleComplete)
                return Fill(state, xPvaGaussianExpectation.ExpectBlackRetrace, xPvaGaussianCycleDirection.Black, "black retrace / 2b component", "completed red cycle usually creates expectation for black retrace");

            if (narrative.State == xPvaGaussianNarrativeState.BuildingBlackCycle)
                return Fill(state, xPvaGaussianExpectation.ExpectBlackResumption, xPvaGaussianCycleDirection.Black, "final black leg / 2b resumption", "black cycle has begun but has not completed");

            if (narrative.State == xPvaGaussianNarrativeState.BlackRetrace)
                return Fill(state, xPvaGaussianExpectation.ExpectBlackResumption, xPvaGaussianCycleDirection.Black, "final black leg / 2b resumption", "black cycle is in retrace phase; final black leg is expected if grammar continues");

            if (narrative.State == xPvaGaussianNarrativeState.BlackCycleComplete)
                return Fill(state, xPvaGaussianExpectation.ExpectRedRetrace, xPvaGaussianCycleDirection.Red, "red retrace / 2r component", "completed black cycle usually creates expectation for red retrace");

            if (narrative.State == xPvaGaussianNarrativeState.Transitioning)
                return Fill(state, xPvaGaussianExpectation.ExpectNewCycle, xPvaGaussianCycleDirection.Unknown, "new Gaussian cycle or failed transition", "transition is developing; expecting a new cycle only if grammar persists");

            if (narrative.State == xPvaGaussianNarrativeState.LeftToRight)
                return Fill(state, xPvaGaussianExpectation.ExpectLeftToRight, xPvaGaussianCycleDirection.Unknown, "continued lateral Gaussian behavior", "left-to-right narrative present; directional expectation is not forced");

            if (narrative.State == xPvaGaussianNarrativeState.BlackCycleFailure || narrative.State == xPvaGaussianNarrativeState.RedCycleFailure)
                return Fill(state, xPvaGaussianExpectation.ExpectNewCycle, xPvaGaussianCycleDirection.Unknown, "new Gaussian cycle after failure", "prior Gaussian expectation failed; waiting for a new cycle to clarify grammar");

            return BuildNone(gaussian, narrative, "Gaussian narrative does not provide a conservative expectation");
        }

        private xPvaGaussianExpectationState TryResolvePendingExpectation(xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative)
        {
            if (pendingExpectation == null || pendingExpectation.Expectation == xPvaGaussianExpectation.None || gaussian == null || narrative == null)
                return null;

            if (gaussian.EndBar <= pendingExpectation.EndBar)
                return null;

            if (ExpectationMet(pendingExpectation, gaussian, narrative))
                return Resolution(pendingExpectation, true, false, gaussian.EndBar, "expected Gaussian side/phase appeared: " + narrative.State);

            if (ExpectationViolated(pendingExpectation, gaussian))
                return Resolution(pendingExpectation, false, true, gaussian.EndBar, "opposite completed Gaussian appeared before expected grammar resolved");

            return null;
        }

        private bool ExpectationMet(xPvaGaussianExpectationState pending, xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative)
        {
            if (pending.Expectation == xPvaGaussianExpectation.ExpectLeftToRight)
                return narrative.State == xPvaGaussianNarrativeState.LeftToRight;

            if (pending.Expectation == xPvaGaussianExpectation.ExpectNewCycle)
                return gaussian.State == xPvaGaussianCycleState.Building || gaussian.State == xPvaGaussianCycleState.Complete;

            if (pending.ExpectedDirection == xPvaGaussianCycleDirection.Unknown)
                return false;

            if (gaussian.Direction != pending.ExpectedDirection)
                return false;

            if (pending.Expectation == xPvaGaussianExpectation.ExpectBlackRetrace || pending.Expectation == xPvaGaussianExpectation.ExpectRedRetrace)
                return gaussian.Phase == xPvaGaussianCyclePhase.RetraceLeg || gaussian.State == xPvaGaussianCycleState.RetraceOnly;

            if (pending.Expectation == xPvaGaussianExpectation.ExpectBlackResumption || pending.Expectation == xPvaGaussianExpectation.ExpectRedResumption)
                return gaussian.State == xPvaGaussianCycleState.Complete || gaussian.Phase == xPvaGaussianCyclePhase.FinalDominantLeg || gaussian.Phase == xPvaGaussianCyclePhase.FirstDominantLeg;

            return false;
        }

        private bool ExpectationViolated(xPvaGaussianExpectationState pending, xPvaGaussianCycle gaussian)
        {
            if (pending.ExpectedDirection == xPvaGaussianCycleDirection.Unknown)
                return false;

            if (gaussian.State != xPvaGaussianCycleState.Complete)
                return false;

            return IsOpposite(pending.ExpectedDirection, gaussian.Direction);
        }

        private xPvaGaussianExpectationState BaseState(xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative)
        {
            return new xPvaGaussianExpectationState
            {
                StartBar = narrative != null ? narrative.StartBar : (gaussian != null ? gaussian.StartBar : 0),
                EndBar = narrative != null ? narrative.EndBar : (gaussian != null ? gaussian.EndBar : 0),
                StartTime = narrative != null ? narrative.StartTime : (gaussian != null ? gaussian.StartTime : DateTime.MinValue),
                EndTime = narrative != null ? narrative.EndTime : (gaussian != null ? gaussian.EndTime : DateTime.MinValue),
                Expectation = xPvaGaussianExpectation.None,
                ExpectedDirection = xPvaGaussianCycleDirection.Unknown,
                ExpectedPattern = "",
                Reason = ""
            };
        }

        private xPvaGaussianExpectationState Fill(
            xPvaGaussianExpectationState state,
            xPvaGaussianExpectation expectation,
            xPvaGaussianCycleDirection expectedDirection,
            string expectedPattern,
            string reason)
        {
            state.Expectation = expectation;
            state.ExpectedDirection = expectedDirection;
            state.ExpectedPattern = expectedPattern;
            state.Reason = reason;
            return state;
        }

        private xPvaGaussianExpectationState BuildNone(xPvaGaussianCycle gaussian, xPvaGaussianNarrative narrative, string reason)
        {
            xPvaGaussianExpectationState state = BaseState(gaussian, narrative);
            state.Reason = reason;
            return state;
        }

        private xPvaGaussianExpectationState Resolution(
            xPvaGaussianExpectationState pending,
            bool met,
            bool violated,
            int resolutionBar,
            string reason)
        {
            xPvaGaussianExpectationState state = pending.Clone();
            state.IsExpectationMet = met;
            state.IsExpectationViolated = violated;
            state.ResolutionBar = resolutionBar;
            state.ResolutionReason = reason;
            return state;
        }

        private xPvaGaussianExpectationState UnresolvedCopy(xPvaGaussianExpectationState state)
        {
            xPvaGaussianExpectationState copy = state.Clone();
            copy.IsExpectationMet = false;
            copy.IsExpectationViolated = false;
            copy.ResolutionBar = 0;
            copy.ResolutionReason = "";
            return copy;
        }

        private bool IsOpposite(xPvaGaussianCycleDirection first, xPvaGaussianCycleDirection second)
        {
            return (first == xPvaGaussianCycleDirection.Black && second == xPvaGaussianCycleDirection.Red)
                || (first == xPvaGaussianCycleDirection.Red && second == xPvaGaussianCycleDirection.Black);
        }
    }
}
