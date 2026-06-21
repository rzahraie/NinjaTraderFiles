#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaExpectationState
    {
        None,
        ExpectBlackDominance,
        ExpectRedDominance,
        ExpectContinuation,
        ExpectCompletion,
        ObservedExpectedEvent,
        MissingExpectedEvent
    }

    public sealed class xPvaExpectationObservation
    {
        public int BarIndex { get; internal set; }
        public DateTime Time { get; internal set; }
        public int SourceSequenceId { get; internal set; }

        public xPvaExpectationState ExpectationState { get; internal set; }
        public xPvaDominanceSide ExpectedSide { get; internal set; }
        public xPvaDominanceSide ObservedSide { get; internal set; }
        public xPvaDominanceRank ObservedRank { get; internal set; }
        public xPvaBinaryState BinaryState { get; internal set; }
        public xPvaStructureState StructureState { get; internal set; }
        public xPvaGrammarState GrammarState { get; internal set; }

        public bool HasActiveExpectation { get; internal set; }
        public bool IsExpectedSidePresent { get; internal set; }
        public bool IsExpectedSideDominant { get; internal set; }
        public bool IsExpectedSideWeak { get; internal set; }
        public bool IsOppositeDominancePresent { get; internal set; }
        public bool IsMissingExpectedEvent { get; internal set; }
        public bool IsObservedExpectedEvent { get; internal set; }

        public int ExpectationAge { get; internal set; }
        public int MissingCount { get; internal set; }
        public int ObservedCount { get; internal set; }

        public string Reason { get; internal set; }

        public string Label
        {
            get
            {
                string s = ExpectationState.ToString();
                if (HasActiveExpectation) s += " exp=" + ExpectedSide.ToString();
                if (IsMissingExpectedEvent) s += " MISSING";
                if (IsObservedExpectedEvent) s += " OBSERVED";
                if (!string.IsNullOrEmpty(Reason)) s += " - " + Reason;
                return s;
            }
        }

        internal xPvaExpectationObservation Clone()
        {
            return (xPvaExpectationObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 7 expectation layer.
    ///
    /// This layer does not predict direction and does not create Change/Continue.
    /// It records the grammar-level expectation created by the lower layers and
    /// then compares that expectation with the next observed dominance event.
    ///
    /// Important PVA rule captured here:
    /// The absence of an expected event is itself an event.
    ///
    /// Example:
    /// Completed/possibly completing red object -> expect black dominance.
    /// If subsequent black activity is weak/non-dominant, MissingExpectedEvent is emitted.
    /// </summary>
    public sealed class xPvaExpectationEngine
    {
        private xPvaDominanceSide expectedSide = xPvaDominanceSide.Neutral;
        private int expectationStartBar = -1;
        private int expectationSourceSequenceId = -1;
        private int missingCount;
        private int observedCount;
        private xPvaExpectationObservation lastObservation;

        public xPvaExpectationObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaExpectationObservation Evaluate(
            xPvaDominanceObservation dominance,
            xPvaBinaryObservation binary,
            xPvaStructureObservation structure,
            xPvaAmbiguityObservation grammar)
        {
            if (dominance == null || binary == null || structure == null)
                return null;

            MaybeCreateOrUpdateExpectation(dominance, binary, structure);

            var obs = new xPvaExpectationObservation
            {
                BarIndex = structure.BarIndex,
                Time = structure.Time,
                SourceSequenceId = structure.SourceSequenceId,
                ExpectedSide = expectedSide,
                ObservedSide = dominance.Side,
                ObservedRank = dominance.Rank,
                BinaryState = binary.BinaryState,
                StructureState = structure.StructureState,
                GrammarState = grammar == null ? xPvaGrammarState.Unknown : grammar.GrammarState,
                HasActiveExpectation = expectedSide != xPvaDominanceSide.Neutral,
                ExpectationAge = expectationStartBar < 0 ? 0 : Math.Max(0, structure.BarIndex - expectationStartBar),
                MissingCount = missingCount,
                ObservedCount = observedCount
            };

            if (!obs.HasActiveExpectation)
            {
                obs.ExpectationState = xPvaExpectationState.None;
                obs.Reason = "no active grammar expectation";
                lastObservation = obs.Clone();
                return obs;
            }

            bool expectedPresent = dominance.Side == expectedSide;
            bool expectedDominant = expectedPresent && IsSignificant(dominance);
            bool expectedWeak = expectedPresent && !IsSignificant(dominance);
            bool oppositeDominant = dominance.Side != xPvaDominanceSide.Neutral
                                 && dominance.Side != expectedSide
                                 && IsSignificant(dominance);

            obs.IsExpectedSidePresent = expectedPresent;
            obs.IsExpectedSideDominant = expectedDominant;
            obs.IsExpectedSideWeak = expectedWeak;
            obs.IsOppositeDominancePresent = oppositeDominant;

            if (expectedDominant)
            {
                observedCount++;
                missingCount = 0;

                obs.ObservedCount = observedCount;
                obs.MissingCount = missingCount;
                obs.ExpectationState = xPvaExpectationState.ObservedExpectedEvent;
                obs.IsObservedExpectedEvent = true;
                obs.Reason = "expected dominance appeared";

                // Once the expected side is observed, leave the next expectation to
                // later completion/structure evidence. Do not immediately forecast.
                ClearExpectation();
            }
            else if (expectedWeak || oppositeDominant)
            {
                missingCount++;
                observedCount = 0;

                obs.ObservedCount = observedCount;
                obs.MissingCount = missingCount;
                obs.ExpectationState = xPvaExpectationState.MissingExpectedEvent;
                obs.IsMissingExpectedEvent = true;
                obs.Reason = expectedWeak
                    ? "expected side appeared only as weak/non-dominant participation"
                    : "opposite dominance appeared before expected side";
            }
            else
            {
                obs.ExpectationState = expectedSide == xPvaDominanceSide.Black
                    ? xPvaExpectationState.ExpectBlackDominance
                    : xPvaExpectationState.ExpectRedDominance;
                obs.Reason = "awaiting expected dominance";
            }

            lastObservation = obs.Clone();
            return obs;
        }

        private void MaybeCreateOrUpdateExpectation(
            xPvaDominanceObservation dominance,
            xPvaBinaryObservation binary,
            xPvaStructureObservation structure)
        {
            if (dominance == null || binary == null || structure == null)
                return;

            bool completionContext = structure.StructureState == xPvaStructureState.CompletingCurrentObject
                                  || (binary.IsSpecialPeakCompressed && binary.BinaryState == xPvaBinaryState.Continue)
                                  || (dominance.ContainsPeakVolume && dominance.ContainsCompressedRange && IsSignificant(dominance));

            bool newObjectObserved = structure.StructureState == xPvaStructureState.BuildingNewObject
                                  && IsSignificant(dominance);

            if (completionContext || newObjectObserved)
            {
                xPvaDominanceSide nextSide = Opposite(dominance.Side);
                if (nextSide != xPvaDominanceSide.Neutral && nextSide != expectedSide)
                {
                    expectedSide = nextSide;
                    expectationStartBar = dominance.EndBar;
                    expectationSourceSequenceId = dominance.SequenceId;
                    missingCount = 0;
                    observedCount = 0;
                }
            }
        }

        private void ClearExpectation()
        {
            expectedSide = xPvaDominanceSide.Neutral;
            expectationStartBar = -1;
            expectationSourceSequenceId = -1;
            missingCount = 0;
            observedCount = 0;
        }

        private bool IsSignificant(xPvaDominanceObservation dominance)
        {
            if (dominance == null)
                return false;

            return dominance.Side != xPvaDominanceSide.Neutral
                && (dominance.Rank == xPvaDominanceRank.BuildingDominance
                    || dominance.Rank == xPvaDominanceRank.EstablishedDominance);
        }

        private xPvaDominanceSide Opposite(xPvaDominanceSide side)
        {
            if (side == xPvaDominanceSide.Black) return xPvaDominanceSide.Red;
            if (side == xPvaDominanceSide.Red) return xPvaDominanceSide.Black;
            return xPvaDominanceSide.Neutral;
        }
    }
}
