#region Using declarations
using System;
using System.Text;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaReasonSeverity
    {
        Informational,
        Watch,
        Warning
    }

    public sealed class xPvaReasonChainObservation
    {
        public int BarIndex { get; internal set; }
        public DateTime Time { get; internal set; }
        public int SourceSequenceId { get; internal set; }

        public xPvaReasonSeverity Severity { get; internal set; }
        public string Headline { get; internal set; }
        public string EventText { get; internal set; }
        public string SequenceText { get; internal set; }
        public string DominanceText { get; internal set; }
        public string BinaryText { get; internal set; }
        public string StructureText { get; internal set; }
        public string GrammarText { get; internal set; }
        public string ExpectationText { get; internal set; }
        public string Interpretation { get; internal set; }
        public string CompactLabel { get; internal set; }

        public string FullText
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Headline);
                if (!string.IsNullOrEmpty(EventText)) sb.Append(" | Event: ").Append(EventText);
                if (!string.IsNullOrEmpty(SequenceText)) sb.Append(" | Sequence: ").Append(SequenceText);
                if (!string.IsNullOrEmpty(DominanceText)) sb.Append(" | Dominance: ").Append(DominanceText);
                if (!string.IsNullOrEmpty(BinaryText)) sb.Append(" | Binary: ").Append(BinaryText);
                if (!string.IsNullOrEmpty(StructureText)) sb.Append(" | Structure: ").Append(StructureText);
                if (!string.IsNullOrEmpty(GrammarText)) sb.Append(" | Grammar: ").Append(GrammarText);
                if (!string.IsNullOrEmpty(ExpectationText)) sb.Append(" | Expectation: ").Append(ExpectationText);
                if (!string.IsNullOrEmpty(Interpretation)) sb.Append(" | Interpretation: ").Append(Interpretation);
                return sb.ToString();
            }
        }

        internal xPvaReasonChainObservation Clone()
        {
            return (xPvaReasonChainObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 8 reason-chain layer.
    ///
    /// This layer makes no new market decision. It exposes the parse path already
    /// produced by the lower layers: event, sequence, dominance, binary, structure,
    /// grammar, and expectation. Its job is auditability: the engine must explain
    /// why it is reading the chart the way it is reading it.
    /// </summary>
    public sealed class xPvaReasonChainEngine
    {
        private xPvaReasonChainObservation lastObservation;

        public xPvaReasonChainObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaReasonChainObservation Evaluate(
            xPvaDiscreteEvent ev,
            xPvaEventSequence sequence,
            xPvaDominanceObservation dominance,
            xPvaBinaryObservation binary,
            xPvaStructureObservation structure,
            xPvaAmbiguityObservation grammar,
            xPvaExpectationObservation expectation)
        {
            if (ev == null || dominance == null || binary == null || structure == null)
                return null;

            var obs = new xPvaReasonChainObservation
            {
                BarIndex = ev.BarIndex,
                Time = ev.Time,
                SourceSequenceId = dominance.SequenceId,
                EventText = ev.Label,
                SequenceText = sequence == null ? "none" : sequence.Kind.ToString() + " n=" + sequence.BarCount + " " + sequence.ChangePattern,
                DominanceText = dominance.ContextLabel + " / " + dominance.Rank.ToString(),
                BinaryText = binary.BinaryState.ToString(),
                StructureText = structure.StructureState.ToString(),
                GrammarText = grammar == null ? "Unknown" : grammar.GrammarState.ToString(),
                ExpectationText = expectation == null ? "None" : expectation.ExpectationState.ToString()
            };

            BuildHeadlineAndInterpretation(obs, ev, dominance, binary, structure, grammar, expectation);

            lastObservation = obs.Clone();
            return obs;
        }

        private void BuildHeadlineAndInterpretation(
            xPvaReasonChainObservation obs,
            xPvaDiscreteEvent ev,
            xPvaDominanceObservation dominance,
            xPvaBinaryObservation binary,
            xPvaStructureObservation structure,
            xPvaAmbiguityObservation grammar,
            xPvaExpectationObservation expectation)
        {
            if (expectation != null && expectation.IsMissingExpectedEvent)
            {
                obs.Severity = xPvaReasonSeverity.Warning;
                obs.Headline = "Missing expected event";
                obs.CompactLabel = "MISSING " + expectation.ExpectedSide.ToString();
                obs.Interpretation = expectation.Reason;
                return;
            }

            if (expectation != null && expectation.IsObservedExpectedEvent)
            {
                obs.Severity = xPvaReasonSeverity.Informational;
                obs.Headline = "Expected event observed";
                obs.CompactLabel = "OBS " + expectation.ObservedSide.ToString();
                obs.Interpretation = expectation.Reason;
                return;
            }

            if (structure.StructureState == xPvaStructureState.BuildingNewObject)
            {
                obs.Severity = xPvaReasonSeverity.Watch;
                obs.Headline = "Building new object";
                obs.CompactLabel = "NEW OBJ";
                obs.Interpretation = structure.Reason;
                return;
            }

            if (structure.StructureState == xPvaStructureState.CompletingCurrentObject)
            {
                obs.Severity = xPvaReasonSeverity.Watch;
                obs.Headline = "Completion context";
                obs.CompactLabel = "COMP?";
                obs.Interpretation = structure.Reason;
                return;
            }

            if (binary.BinaryState == xPvaBinaryState.Continue
                && (structure.StructureState == xPvaStructureState.BuildingCurrentObject
                    || structure.StructureState == xPvaStructureState.ContinuingCurrentObject))
            {
                obs.Severity = xPvaReasonSeverity.Informational;
                obs.Headline = "Same object continues";
                obs.CompactLabel = "CONT";
                obs.Interpretation = structure.Reason;
                return;
            }

            if (grammar != null && grammar.GrammarState == xPvaGrammarState.Ambiguous)
            {
                obs.Severity = xPvaReasonSeverity.Watch;
                obs.Headline = "Grammar requires caution";
                obs.CompactLabel = "CAUTION";
                obs.Interpretation = grammar.Reason;
                return;
            }

            if (ev.IsVolumeRangeMismatch || ev.IsAcceleratedPeakVolume || ev.IsStrictPeakVolume || ev.IsCompressedRange)
            {
                obs.Severity = xPvaReasonSeverity.Watch;
                obs.Headline = "Special descriptive event";
                obs.CompactLabel = "SPECIAL";
                obs.Interpretation = "special bar is descriptive only; lower layers did not resolve change/continue";
                return;
            }

            obs.Severity = xPvaReasonSeverity.Informational;
            obs.Headline = "Reading current grammar";
            obs.CompactLabel = "READ";
            obs.Interpretation = "no higher-order grammar warning on this bar";
        }
    }
}
