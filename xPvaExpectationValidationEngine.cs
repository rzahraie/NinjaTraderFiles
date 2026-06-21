#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaExpectationOutcome
    {
        None,

        Pending,

        Satisfied,

        PartiallySatisfied,

        Failed,

        Transitioned,

        Mutated,

        Expired
    }

    public sealed class xPvaExpectationValidation
    {
        public int ExpectationBar;
        public int ResolutionBar;

        public DateTime ExpectationTime;
        public DateTime ResolutionTime;

        public xPvaGaussianExpectation Expectation;

        public xPvaExpectationOutcome Outcome;

        public string ExpectedPattern;
        public string ObservedPattern;

        public string Reason;

        internal xPvaExpectationValidation Clone()
        {
            return (xPvaExpectationValidation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Incrementally validates Gaussian expectations against later Gaussian observations.
    /// This layer does not draw, trade, or use future bars.
    /// </summary>
    public sealed class xPvaExpectationValidationEngine
    {
        private readonly int expirationBars;
        private xPvaExpectationValidation pending;
        private xPvaExpectationValidation lastValidation;

        public xPvaExpectationValidationEngine()
            : this(12)
        {
        }

        public xPvaExpectationValidationEngine(int expirationBars)
        {
            this.expirationBars = Math.Max(3, expirationBars);
        }

        public xPvaExpectationValidation LastValidation
        {
            get { return lastValidation == null ? null : lastValidation.Clone(); }
        }

        public xPvaExpectationValidation Update(
            xPvaGaussianExpectationState expectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context)
        {
            xPvaExpectationValidation resolved = TryResolvePending(expectation, gaussian, narrative, container, level3Context);
            if (resolved != null)
            {
                pending = null;
                lastValidation = resolved;
                return LastValidation;
            }

            if (ShouldStartNewExpectation(expectation))
                pending = BuildPending(expectation);

            lastValidation = pending != null ? pending.Clone() : BuildNone(expectation, gaussian, narrative);
            return LastValidation;
        }

        private xPvaExpectationValidation TryResolvePending(
            xPvaGaussianExpectationState expectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context)
        {
            if (pending == null || pending.Outcome != xPvaExpectationOutcome.Pending || gaussian == null)
                return null;

            if (gaussian.EndBar <= pending.ExpectationBar)
                return null;

            string observed = ObservedPattern(gaussian, narrative, container, level3Context);

            if (IsSatisfied(pending.Expectation, gaussian, narrative))
                return Resolve(xPvaExpectationOutcome.Satisfied, gaussian, observed, "expected Gaussian grammar appeared after expectation");

            if (IsPartiallySatisfied(pending.Expectation, gaussian, narrative))
                return Resolve(xPvaExpectationOutcome.PartiallySatisfied, gaussian, observed, "expected side appeared, but evidence is still partial");

            if (IsFailed(pending.Expectation, gaussian))
                return Resolve(xPvaExpectationOutcome.Failed, gaussian, observed, "opposite completed Gaussian appeared before expected grammar resolved");

            if (IsTransitioned(pending.Expectation, narrative))
                return Resolve(xPvaExpectationOutcome.Transitioned, gaussian, observed, "market entered a different coherent Gaussian narrative");

            if (IsMutated(pending.Expectation, expectation, gaussian, narrative))
                return Resolve(xPvaExpectationOutcome.Mutated, gaussian, observed, "expectation changed form before clean satisfaction or failure");

            if (gaussian.EndBar - pending.ExpectationBar > expirationBars)
                return Resolve(xPvaExpectationOutcome.Expired, gaussian, observed, "expectation remained unresolved beyond conservative bar threshold");

            return null;
        }

        private bool ShouldStartNewExpectation(xPvaGaussianExpectationState expectation)
        {
            if (expectation == null || expectation.Expectation == xPvaGaussianExpectation.None)
                return false;

            if (pending == null)
                return true;

            if (pending.Outcome != xPvaExpectationOutcome.Pending)
                return true;

            return false;
        }

        private xPvaExpectationValidation BuildPending(xPvaGaussianExpectationState expectation)
        {
            return new xPvaExpectationValidation
            {
                ExpectationBar = expectation.EndBar,
                ResolutionBar = 0,
                ExpectationTime = expectation.EndTime,
                ResolutionTime = DateTime.MinValue,
                Expectation = expectation.Expectation,
                Outcome = xPvaExpectationOutcome.Pending,
                ExpectedPattern = expectation.ExpectedPattern,
                ObservedPattern = "",
                Reason = "new Gaussian expectation is pending validation"
            };
        }

        private xPvaExpectationValidation BuildNone(
            xPvaGaussianExpectationState expectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative)
        {
            return new xPvaExpectationValidation
            {
                ExpectationBar = expectation != null ? expectation.EndBar : (gaussian != null ? gaussian.EndBar : 0),
                ResolutionBar = 0,
                ExpectationTime = expectation != null ? expectation.EndTime : (gaussian != null ? gaussian.EndTime : DateTime.MinValue),
                ResolutionTime = DateTime.MinValue,
                Expectation = expectation != null ? expectation.Expectation : xPvaGaussianExpectation.None,
                Outcome = xPvaExpectationOutcome.None,
                ExpectedPattern = expectation != null ? expectation.ExpectedPattern : "",
                ObservedPattern = narrative != null ? narrative.Pattern : (gaussian != null ? gaussian.Pattern : ""),
                Reason = "no pending Gaussian expectation validation"
            };
        }

        private xPvaExpectationValidation Resolve(
            xPvaExpectationOutcome outcome,
            xPvaGaussianCycle gaussian,
            string observedPattern,
            string reason)
        {
            xPvaExpectationValidation validation = pending.Clone();
            validation.Outcome = outcome;
            validation.ResolutionBar = gaussian.EndBar;
            validation.ResolutionTime = gaussian.EndTime;
            validation.ObservedPattern = observedPattern;
            validation.Reason = reason;
            return validation;
        }

        private bool IsSatisfied(
            xPvaGaussianExpectation expectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative)
        {
            if (expectation == xPvaGaussianExpectation.ExpectLeftToRight)
                return narrative != null && narrative.State == xPvaGaussianNarrativeState.LeftToRight;

            if (expectation == xPvaGaussianExpectation.ExpectNewCycle)
                return gaussian.State == xPvaGaussianCycleState.Complete
                    || (gaussian.State == xPvaGaussianCycleState.Building && gaussian.Phase == xPvaGaussianCyclePhase.FirstDominantLeg);

            if (expectation == xPvaGaussianExpectation.ExpectRedResumption)
                return gaussian.Direction == xPvaGaussianCycleDirection.Red && gaussian.State == xPvaGaussianCycleState.Complete;

            if (expectation == xPvaGaussianExpectation.ExpectBlackResumption)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black && gaussian.State == xPvaGaussianCycleState.Complete;

            if (expectation == xPvaGaussianExpectation.ExpectRedRetrace)
                return gaussian.Direction == xPvaGaussianCycleDirection.Red
                    && (gaussian.Phase == xPvaGaussianCyclePhase.RetraceLeg || gaussian.State == xPvaGaussianCycleState.RetraceOnly);

            if (expectation == xPvaGaussianExpectation.ExpectBlackRetrace)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    && (gaussian.Phase == xPvaGaussianCyclePhase.RetraceLeg || gaussian.State == xPvaGaussianCycleState.RetraceOnly);

            return false;
        }

        private bool IsPartiallySatisfied(
            xPvaGaussianExpectation expectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative)
        {
            if (narrative == null)
                return false;

            if (expectation == xPvaGaussianExpectation.ExpectRedResumption)
                return gaussian.Direction == xPvaGaussianCycleDirection.Red
                    && (narrative.State == xPvaGaussianNarrativeState.RedRetrace || narrative.State == xPvaGaussianNarrativeState.BuildingRedCycle);

            if (expectation == xPvaGaussianExpectation.ExpectBlackResumption)
                return gaussian.Direction == xPvaGaussianCycleDirection.Black
                    && (narrative.State == xPvaGaussianNarrativeState.BlackRetrace || narrative.State == xPvaGaussianNarrativeState.BuildingBlackCycle);

            return false;
        }

        private bool IsFailed(xPvaGaussianExpectation expectation, xPvaGaussianCycle gaussian)
        {
            xPvaGaussianCycleDirection expectedDirection = ExpectedDirection(expectation);
            if (expectedDirection == xPvaGaussianCycleDirection.Unknown || gaussian.State != xPvaGaussianCycleState.Complete)
                return false;

            return IsOpposite(expectedDirection, gaussian.Direction);
        }

        private bool IsTransitioned(xPvaGaussianExpectation expectation, xPvaGaussianNarrative narrative)
        {
            if (narrative == null)
                return false;

            if (narrative.State != xPvaGaussianNarrativeState.Transitioning)
                return false;

            return expectation == xPvaGaussianExpectation.ExpectRedResumption
                || expectation == xPvaGaussianExpectation.ExpectBlackResumption
                || expectation == xPvaGaussianExpectation.ExpectNewCycle;
        }

        private bool IsMutated(
            xPvaGaussianExpectation pendingExpectation,
            xPvaGaussianExpectationState currentExpectation,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative)
        {
            if (narrative != null && narrative.State == xPvaGaussianNarrativeState.LeftToRight)
                return pendingExpectation != xPvaGaussianExpectation.ExpectLeftToRight;

            if (currentExpectation == null || currentExpectation.Expectation == xPvaGaussianExpectation.None)
                return false;

            if (currentExpectation.Expectation == pendingExpectation)
                return false;

            if (gaussian.State == xPvaGaussianCycleState.Complete)
                return false;

            return SharesExpectedSide(pendingExpectation, currentExpectation.ExpectedDirection);
        }

        private string ObservedPattern(
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context)
        {
            string observed = narrative != null && !string.IsNullOrEmpty(narrative.Pattern)
                ? narrative.State + ": " + narrative.Pattern
                : (gaussian != null ? gaussian.State + ": " + gaussian.Pattern : "");

            if (container != null && container.Level != xPvaContainerLevel.Unknown)
                observed += " | Container " + container.Level + " " + container.State;

            if (level3Context != null && level3Context.State != xPvaLevel3ContextState.Unknown)
                observed += " | L3 " + level3Context.State;

            return observed;
        }

        private bool SharesExpectedSide(xPvaGaussianExpectation expectation, xPvaGaussianCycleDirection direction)
        {
            xPvaGaussianCycleDirection expected = ExpectedDirection(expectation);
            return expected != xPvaGaussianCycleDirection.Unknown && expected == direction;
        }

        private xPvaGaussianCycleDirection ExpectedDirection(xPvaGaussianExpectation expectation)
        {
            if (expectation == xPvaGaussianExpectation.ExpectBlackRetrace || expectation == xPvaGaussianExpectation.ExpectBlackResumption)
                return xPvaGaussianCycleDirection.Black;

            if (expectation == xPvaGaussianExpectation.ExpectRedRetrace || expectation == xPvaGaussianExpectation.ExpectRedResumption)
                return xPvaGaussianCycleDirection.Red;

            return xPvaGaussianCycleDirection.Unknown;
        }

        private bool IsOpposite(xPvaGaussianCycleDirection first, xPvaGaussianCycleDirection second)
        {
            return (first == xPvaGaussianCycleDirection.Black && second == xPvaGaussianCycleDirection.Red)
                || (first == xPvaGaussianCycleDirection.Red && second == xPvaGaussianCycleDirection.Black);
        }
    }
}
