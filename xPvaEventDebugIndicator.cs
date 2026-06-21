#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.xPva.Engine;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaEventDebugIndicator : Indicator
    {
        private xPvaDiscreteEventEngine eventEngine;
        private xPvaEventSequenceEngine sequenceEngine;
        private xPvaLevel1ObjectEngine level1ObjectEngine;
        private xPvaDominanceTracker dominanceTracker;
        private xPvaBinaryEngine binaryEngine;
        private xPvaStructureEngine structureEngine;
        private xPvaAmbiguityEngine ambiguityEngine;
        private xPvaExpectationEngine expectationEngine;
        private xPvaReasonChainEngine reasonChainEngine;
        private xPvaContainerContextEngine containerContextEngine;
        private xPvaGaussianCycleEngine gaussianCycleEngine;
        private xPvaGaussianNarrativeEngine gaussianNarrativeEngine;
        private xPvaGaussianExpectationEngine gaussianExpectationEngine;
        private xPvaExpectationValidationEngine expectationValidationEngine;
        private xPvaGaussianFractalLedger gaussianFractalLedger;
        private List<string[]> htmlRows;
        private string resolvedHtmlFilePath;

        [NinjaScriptProperty]
        [Display(Name = "Show event labels", GroupName = "Debug", Order = 1)]
        public bool ShowEventLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show sequence labels", GroupName = "Debug", Order = 2)]
        public bool ShowSequenceLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show dominance labels", GroupName = "Debug", Order = 3)]
        public bool ShowDominanceLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print events", GroupName = "Debug", Order = 4)]
        public bool PrintEvents { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print sequences", GroupName = "Debug", Order = 5)]
        public bool PrintSequences { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print dominance", GroupName = "Debug", Order = 6)]
        public bool PrintDominance { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show binary labels", GroupName = "Debug", Order = 7)]
        public bool ShowBinaryLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print binary", GroupName = "Debug", Order = 8)]
        public bool PrintBinary { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show structure labels", GroupName = "Debug", Order = 9)]
        public bool ShowStructureLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print structure", GroupName = "Debug", Order = 10)]
        public bool PrintStructure { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show ambiguity labels", GroupName = "Debug", Order = 11)]
        public bool ShowAmbiguityLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print ambiguity", GroupName = "Debug", Order = 12)]
        public bool PrintAmbiguity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show expectation labels", GroupName = "Debug", Order = 13)]
        public bool ShowExpectationLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print expectation", GroupName = "Debug", Order = 14)]
        public bool PrintExpectation { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show reason labels", GroupName = "Debug", Order = 15)]
        public bool ShowReasonLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print reason chain", GroupName = "Debug", Order = 16)]
        public bool PrintReasonChain { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Export HTML table", GroupName = "HTML Export", Order = 17)]
        public bool ExportHtmlTable { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "HTML file path", GroupName = "HTML Export", Order = 18)]
        public string HtmlFilePath { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print HTML path on close", GroupName = "HTML Export", Order = 19)]
        public bool PrintHtmlPathOnClose { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaEventDebugIndicator";
                Description = "Debug display for discrete PVA event and candidate sequence grammar.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                ShowEventLabels = false;
                ShowSequenceLabels = false;
                ShowDominanceLabels = false;
                PrintEvents = false;
                PrintSequences = false;
                PrintDominance = false;
                ShowBinaryLabels = false;
                PrintBinary = false;
                ShowStructureLabels = false;
                PrintStructure = false;
                ShowAmbiguityLabels = false;
                PrintAmbiguity = false;
                ShowExpectationLabels = false;
                PrintExpectation = false;
                ShowReasonLabels = false;
                PrintReasonChain = false;
                ExportHtmlTable = true;
                HtmlFilePath = @"C:\Users\rz0\Documents\ApvaAnalysis\PvaHTML\Table.html";
                PrintHtmlPathOnClose = false;
            }
            else if (State == State.DataLoaded)
            {
                eventEngine = new xPvaDiscreteEventEngine(TickSize);
                sequenceEngine = new xPvaEventSequenceEngine();
                level1ObjectEngine = new xPvaLevel1ObjectEngine();
                dominanceTracker = new xPvaDominanceTracker();
                binaryEngine = new xPvaBinaryEngine();
                structureEngine = new xPvaStructureEngine();
                ambiguityEngine = new xPvaAmbiguityEngine();
                expectationEngine = new xPvaExpectationEngine();
                reasonChainEngine = new xPvaReasonChainEngine();
                containerContextEngine = new xPvaContainerContextEngine();
                gaussianCycleEngine = new xPvaGaussianCycleEngine();
                gaussianNarrativeEngine = new xPvaGaussianNarrativeEngine();
                gaussianExpectationEngine = new xPvaGaussianExpectationEngine();
                expectationValidationEngine = new xPvaExpectationValidationEngine();
                gaussianFractalLedger = new xPvaGaussianFractalLedger();
                htmlRows = new List<string[]>();
                resolvedHtmlFilePath = ResolveHtmlFilePath();
            }
            else if (State == State.Terminated)
            {
                WriteHtmlTable();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 || eventEngine == null || sequenceEngine == null || level1ObjectEngine == null || dominanceTracker == null || binaryEngine == null || structureEngine == null || ambiguityEngine == null || expectationEngine == null || reasonChainEngine == null || containerContextEngine == null || gaussianCycleEngine == null || gaussianNarrativeEngine == null || gaussianExpectationEngine == null || expectationValidationEngine == null || gaussianFractalLedger == null)
                return;

            var cur = Facts(0);
            var prev = Facts(1);
            var prev2 = Facts(2);

            var ev = eventEngine.BuildEvent(cur, prev, prev2);
            xPvaLevel1Object level1 = level1ObjectEngine.Update(ev);

            xPvaEventSequence completed;
            var active = sequenceEngine.Update(ev, Volume[0], cur.RangeTicks, out completed);
            xPvaGaussianCycle gaussian = gaussianCycleEngine.Update(ev, level1, active, completed);
            xPvaGaussianNarrative gaussianNarrative = gaussianNarrativeEngine.Update(gaussian);
            xPvaGaussianExpectationState gaussianExpectation = gaussianExpectationEngine.Update(gaussian, gaussianNarrative);
            xPvaContainerContext container = containerContextEngine.Update(level1);
            xPvaLevel3Context level3Context = containerContextEngine.LastLevel3Context;
            xPvaExpectationValidation expectationValidation = expectationValidationEngine.Update(gaussianExpectation, gaussian, gaussianNarrative, container, level3Context);

            if (PrintEvents)
            {
                Print(string.Format("EVT,{0:yyyy-MM-dd HH:mm:ss},Bar={1},{2},V={3},RangeTicks={4:F0},L1State={5},L1Direction={6},L1Role={7},L1RoleConfidence={8},L1Start={9},L1End={10},L1Pattern={11},L1Reason={12},L1RoleReason={13},L1RoleConfidenceReason={14},GaussianState={15},GaussianDirection={16},GaussianPhase={17},GaussianStart={18},GaussianEnd={19},GaussianPattern={20},GaussianPeak={21},GaussianTrough={22},GaussianAccel={23},GaussianReason={24},GaussianNarrativeState={25},GaussianNarrativeDirection={26},GaussianNarrativeStart={27},GaussianNarrativeEnd={28},GaussianNarrativePattern={29},GaussianNarrativeReason={30},GaussianExpectation={31},GaussianExpectedDirection={32},GaussianExpectedPattern={33},GaussianExpectationReason={34},GaussianExpectationMet={35},GaussianExpectationViolated={36},GaussianExpectationResolutionBar={37},GaussianExpectationResolutionReason={38},ExpectationOutcome={39},ExpectationExpectedPattern={40},ExpectationObservedPattern={41},ExpectationResolutionBar={42},ExpectationValidationReason={43},ContainerLevel={44},ContainerDirection={45},ContainerState={46},ContainerStart={47},ContainerEnd={48},ContainerPattern={49},ContainerParent={50},ContainerReason={51},ContainerCompletionReason={52},L3ContextState={53},L3ContextDirection={54},L3ContextStart={55},L3ContextEnd={56},L3ContextPattern={57},L3ContextReason={58}",
                    Time[0], CurrentBar, ev.Label, Volume[0], cur.RangeTicks,
                    level1 != null ? level1.State.ToString() : "",
                    level1 != null ? level1.Direction.ToString() : "",
                    level1 != null ? level1.Role.ToString() : "",
                    level1 != null ? level1.RoleConfidence.ToString() : "",
                    level1 != null ? level1.StartBar.ToString(CultureInfo.InvariantCulture) : "",
                    level1 != null ? level1.EndBar.ToString(CultureInfo.InvariantCulture) : "",
                    level1 != null ? Safe(level1.Pattern) : "",
                    level1 != null ? Safe(level1.Reason) : "",
                    level1 != null ? Safe(level1.RoleReason) : "",
                    level1 != null ? Safe(level1.RoleConfidenceReason) : "",
                    gaussian != null ? gaussian.State.ToString() : "",
                    gaussian != null ? gaussian.Direction.ToString() : "",
                    gaussian != null ? gaussian.Phase.ToString() : "",
                    gaussian != null ? gaussian.StartBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussian != null ? gaussian.EndBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussian != null ? Safe(gaussian.Pattern) : "",
                    gaussian != null && gaussian.HasPeak ? gaussian.PeakBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussian != null && gaussian.HasTrough ? gaussian.TroughBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussian != null ? gaussian.HasAcceleration.ToString() : "",
                    gaussian != null ? Safe(gaussian.Reason) : "",
                    gaussianNarrative != null ? gaussianNarrative.State.ToString() : "",
                    gaussianNarrative != null ? gaussianNarrative.Direction.ToString() : "",
                    gaussianNarrative != null ? gaussianNarrative.StartBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussianNarrative != null ? gaussianNarrative.EndBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussianNarrative != null ? Safe(gaussianNarrative.Pattern) : "",
                    gaussianNarrative != null ? Safe(gaussianNarrative.Reason) : "",
                    gaussianExpectation != null ? gaussianExpectation.Expectation.ToString() : "",
                    gaussianExpectation != null ? gaussianExpectation.ExpectedDirection.ToString() : "",
                    gaussianExpectation != null ? Safe(gaussianExpectation.ExpectedPattern) : "",
                    gaussianExpectation != null ? Safe(gaussianExpectation.Reason) : "",
                    gaussianExpectation != null ? gaussianExpectation.IsExpectationMet.ToString() : "",
                    gaussianExpectation != null ? gaussianExpectation.IsExpectationViolated.ToString() : "",
                    gaussianExpectation != null && gaussianExpectation.ResolutionBar > 0 ? gaussianExpectation.ResolutionBar.ToString(CultureInfo.InvariantCulture) : "",
                    gaussianExpectation != null ? Safe(gaussianExpectation.ResolutionReason) : "",
                    expectationValidation != null ? expectationValidation.Outcome.ToString() : "",
                    expectationValidation != null ? Safe(expectationValidation.ExpectedPattern) : "",
                    expectationValidation != null ? Safe(expectationValidation.ObservedPattern) : "",
                    expectationValidation != null && expectationValidation.ResolutionBar > 0 ? expectationValidation.ResolutionBar.ToString(CultureInfo.InvariantCulture) : "",
                    expectationValidation != null ? Safe(expectationValidation.Reason) : "",
                    container != null ? container.Level.ToString() : "",
                    container != null ? container.Direction.ToString() : "",
                    container != null ? container.State.ToString() : "",
                    container != null ? container.StartBar.ToString(CultureInfo.InvariantCulture) : "",
                    container != null ? container.EndBar.ToString(CultureInfo.InvariantCulture) : "",
                    container != null ? Safe(container.Pattern) : "",
                    container != null ? FormatContainerParent(container) : "",
                    container != null ? Safe(container.Reason) : "",
                    container != null ? Safe(container.CompletionReason) : "",
                    level3Context != null ? level3Context.State.ToString() : "",
                    level3Context != null ? level3Context.Direction.ToString() : "",
                    level3Context != null ? level3Context.StartBar.ToString(CultureInfo.InvariantCulture) : "",
                    level3Context != null ? level3Context.EndBar.ToString(CultureInfo.InvariantCulture) : "",
                    level3Context != null ? Safe(level3Context.Pattern) : "",
                    level3Context != null ? Safe(level3Context.Reason) : ""));
            }

            xPvaDominanceObservation activeDominance = dominanceTracker.Evaluate(active);
            xPvaDominanceObservation completedDominance = completed != null ? dominanceTracker.Evaluate(completed) : null;

            xPvaBinaryObservation activeBinary = binaryEngine.Evaluate(activeDominance);
            xPvaBinaryObservation completedBinary = completedDominance != null ? binaryEngine.Evaluate(completedDominance) : null;

            xPvaStructureObservation activeStructure = structureEngine.Evaluate(activeBinary);
            xPvaStructureObservation completedStructure = completedBinary != null ? structureEngine.Evaluate(completedBinary) : null;

            xPvaAmbiguityObservation activeAmbiguity = ambiguityEngine.Evaluate(activeDominance, activeBinary, activeStructure);
            xPvaAmbiguityObservation completedAmbiguity = (completedDominance != null && completedBinary != null && completedStructure != null)
                ? ambiguityEngine.Evaluate(completedDominance, completedBinary, completedStructure)
                : null;

            xPvaExpectationObservation activeExpectation = expectationEngine.Evaluate(activeDominance, activeBinary, activeStructure, activeAmbiguity);
            xPvaExpectationObservation completedExpectation = (completedDominance != null && completedBinary != null && completedStructure != null)
                ? expectationEngine.Evaluate(completedDominance, completedBinary, completedStructure, completedAmbiguity)
                : null;

            xPvaReasonChainObservation activeReason = reasonChainEngine.Evaluate(ev, active, activeDominance, activeBinary, activeStructure, activeAmbiguity, activeExpectation);
            xPvaReasonChainObservation completedReason = (completed != null && completedDominance != null && completedBinary != null && completedStructure != null)
                ? reasonChainEngine.Evaluate(ev, completed, completedDominance, completedBinary, completedStructure, completedAmbiguity, completedExpectation)
                : null;

            xPvaGaussianFractalLedgerSnapshot gaussianLedger = gaussianFractalLedger.Update(CurrentBar, Time[0], ev != null ? ev.Label : "", gaussian, gaussianNarrative, gaussianExpectation, expectationValidation, container, level3Context, active, completed, activeDominance, completedDominance, level1, activeStructure, completedStructure, High[0], Low[0], true);

            AddHtmlRow(cur, ev, level1, gaussian, gaussianNarrative, gaussianExpectation, expectationValidation, gaussianLedger, container, level3Context, active, completed, activeDominance, completedDominance, activeBinary, completedBinary, activeStructure, completedStructure, activeAmbiguity, completedAmbiguity, activeExpectation, completedExpectation, activeReason, completedReason);

            if (completed != null && PrintSequences)
            {
                Print(string.Format("SEQ_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},MaxVBar={2},MaxRangeBar={3}",
                    Time[0], completed.Label, completed.MaxVolumeBar, completed.MaxRangeBar));
            }

            if (completedDominance != null && PrintDominance)
            {
                Print(string.Format("DOM_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},Seq={2},Bars={3}-{4}",
                    Time[0], completedDominance.Label, completedDominance.SequenceId, completedDominance.StartBar, completedDominance.EndBar));
            }

            if (completedBinary != null && PrintBinary)
            {
                Print(string.Format("BIN_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},Seq={2}",
                    Time[0], completedBinary.Label, completedBinary.SourceSequenceId));
            }

            if (completedStructure != null && PrintStructure)
            {
                Print(string.Format("STRUCT_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},Seq={2},Binary={3}",
                    Time[0], completedStructure.Label, completedStructure.SourceSequenceId, completedStructure.BinaryState));
            }

            if (completedAmbiguity != null && PrintAmbiguity)
            {
                Print(string.Format("GRAMMAR_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},Seq={2},Binary={3},Structure={4}",
                    Time[0], completedAmbiguity.Label, completedAmbiguity.SourceSequenceId,
                    completedAmbiguity.BinaryState, completedAmbiguity.StructureState));
            }

            if (completedExpectation != null && PrintExpectation)
            {
                Print(string.Format("EXPECT_DONE,{0:yyyy-MM-dd HH:mm:ss},{1},Seq={2},Expected={3},Observed={4},MissingCount={5}",
                    Time[0], completedExpectation.Label, completedExpectation.SourceSequenceId,
                    completedExpectation.ExpectedSide, completedExpectation.ObservedSide, completedExpectation.MissingCount));
            }

            if (completedReason != null && PrintReasonChain)
            {
                Print(string.Format("REASON_DONE,{0:yyyy-MM-dd HH:mm:ss},Bar={1},Seq={2},{3}",
                    Time[0], completedReason.BarIndex, completedReason.SourceSequenceId, completedReason.FullText));
            }

            if (activeReason != null && PrintReasonChain)
            {
                Print(string.Format("REASON_ACTIVE,{0:yyyy-MM-dd HH:mm:ss},Bar={1},Seq={2},{3}",
                    Time[0], activeReason.BarIndex, activeReason.SourceSequenceId, activeReason.FullText));
            }

            if (ShowEventLabels && (ev.IsVolumeRangeMismatch || ev.IsAcceleratedPeakVolume || ev.IsStrictPeakVolume))
            {
                Brush brush = EventBrush(ev.VolumePolarity);

                Draw.Text(this,
                    "xPvaEvt" + CurrentBar,
                    ev.Label,
                    0,
                    High[0] + 2 * TickSize,
                    brush);
            }

            if (ShowSequenceLabels && completed != null && IsCandidateSequence(completed.Kind))
            {
                Brush brush = EventBrush(completed.Polarity);
                int barsAgo = Math.Max(0, CurrentBar - completed.EndBar + 1);

                Draw.Text(this,
                    "xPvaSeq" + completed.SequenceId,
                    completed.Kind.ToString(),
                    barsAgo,
                    Low[barsAgo] - 4 * TickSize,
                    brush);
            }

            if (ShowDominanceLabels && activeDominance != null && activeDominance.Rank != xPvaDominanceRank.NonDominantParticipation)
            {
                Brush brush = activeDominance.Side == xPvaDominanceSide.Red ? Brushes.Red :
                              activeDominance.Side == xPvaDominanceSide.Black ? Brushes.Black :
                              Brushes.DimGray;

                Draw.Text(this,
                    "xPvaDomActive" + activeDominance.SequenceId,
                    activeDominance.ContextLabel,
                    0,
                    Low[0] - 7 * TickSize,
                    brush);
            }

            if (ShowBinaryLabels && activeBinary != null && activeBinary.BinaryState != xPvaBinaryState.Unknown)
            {
                Brush brush = activeBinary.BinaryState == xPvaBinaryState.Change ? Brushes.DarkViolet : Brushes.DimGray;

                Draw.Text(this,
                    "xPvaBinActive" + activeBinary.SourceSequenceId,
                    activeBinary.BinaryState.ToString(),
                    0,
                    Low[0] - 10 * TickSize,
                    brush);
            }

            if (ShowStructureLabels && activeStructure != null && activeStructure.StructureState != xPvaStructureState.Unknown)
            {
                Brush brush = StructureBrush(activeStructure.StructureState);

                Draw.Text(this,
                    "xPvaStructActive" + activeStructure.SourceSequenceId,
                    activeStructure.StructureState.ToString(),
                    0,
                    Low[0] - 13 * TickSize,
                    brush);
            }
            if (ShowAmbiguityLabels && activeAmbiguity != null && activeAmbiguity.GrammarState != xPvaGrammarState.Unknown)
            {
                Brush brush = AmbiguityBrush(activeAmbiguity.GrammarState);

                Draw.Text(this,
                    "xPvaGrammarActive" + activeAmbiguity.SourceSequenceId,
                    activeAmbiguity.GrammarState.ToString(),
                    0,
                    Low[0] - 16 * TickSize,
                    brush);
            }

            if (ShowExpectationLabels && activeExpectation != null && activeExpectation.ExpectationState != xPvaExpectationState.None)
            {
                Brush brush = ExpectationBrush(activeExpectation);

                Draw.Text(this,
                    "xPvaExpectActive" + activeExpectation.SourceSequenceId + "_" + CurrentBar,
                    ShortExpectationLabel(activeExpectation),
                    0,
                    Low[0] - 19 * TickSize,
                    brush);
            }

            if (ShowReasonLabels && activeReason != null && activeReason.Severity != xPvaReasonSeverity.Informational)
            {
                Brush brush = ReasonBrush(activeReason.Severity);

                Draw.Text(this,
                    "xPvaReasonActive" + activeReason.SourceSequenceId + "_" + CurrentBar,
                    activeReason.CompactLabel,
                    0,
                    Low[0] - 22 * TickSize,
                    brush);
            }


        }

        private bool IsCandidateSequence(xPvaSequenceKind kind)
        {
            return kind == xPvaSequenceKind.CandidateB2B
                || kind == xPvaSequenceKind.CandidateR2R
                || kind == xPvaSequenceKind.Candidate2B
                || kind == xPvaSequenceKind.Candidate2R;
        }


        private Brush StructureBrush(xPvaStructureState state)
        {
            if (state == xPvaStructureState.BuildingNewObject) return Brushes.DarkViolet;
            if (state == xPvaStructureState.CompletingCurrentObject) return Brushes.DarkOrange;
            if (state == xPvaStructureState.ContinuingCurrentObject) return Brushes.DarkSlateGray;
            if (state == xPvaStructureState.BuildingCurrentObject) return Brushes.DimGray;
            return Brushes.Gray;
        }

        private Brush AmbiguityBrush(xPvaGrammarState state)
        {
            if (state == xPvaGrammarState.Clear) return Brushes.DarkGreen;
            if (state == xPvaGrammarState.Ambiguous) return Brushes.DarkOrange;
            return Brushes.Gray;
        }

        private Brush ExpectationBrush(xPvaExpectationObservation obs)
        {
            if (obs == null) return Brushes.Gray;
            if (obs.ExpectationState == xPvaExpectationState.MissingExpectedEvent) return Brushes.Crimson;
            if (obs.ExpectationState == xPvaExpectationState.ObservedExpectedEvent) return Brushes.DarkGreen;
            if (obs.ExpectedSide == xPvaDominanceSide.Black) return Brushes.Black;
            if (obs.ExpectedSide == xPvaDominanceSide.Red) return Brushes.Red;
            return Brushes.DimGray;
        }

        private string ShortExpectationLabel(xPvaExpectationObservation obs)
        {
            if (obs == null) return "";
            if (obs.ExpectationState == xPvaExpectationState.MissingExpectedEvent) return "Missing " + obs.ExpectedSide.ToString();
            if (obs.ExpectationState == xPvaExpectationState.ObservedExpectedEvent) return "Observed " + obs.ObservedSide.ToString();
            if (obs.ExpectedSide == xPvaDominanceSide.Black) return "Expect Black";
            if (obs.ExpectedSide == xPvaDominanceSide.Red) return "Expect Red";
            return obs.ExpectationState.ToString();
        }

        private Brush ReasonBrush(xPvaReasonSeverity severity)
        {
            if (severity == xPvaReasonSeverity.Warning) return Brushes.Crimson;
            if (severity == xPvaReasonSeverity.Watch) return Brushes.DarkOrange;
            return Brushes.DimGray;
        }

        private Brush EventBrush(xPvaVolumePolarity polarity)
        {
            if (polarity == xPvaVolumePolarity.R) return Brushes.Red;
            if (polarity == xPvaVolumePolarity.B) return Brushes.Black;
            return Brushes.DimGray;
        }

        private void AddHtmlRow(
            xPvaBarFacts facts,
            xPvaDiscreteEvent ev,
            xPvaLevel1Object level1,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative gaussianNarrative,
            xPvaGaussianExpectationState gaussianExpectation,
            xPvaExpectationValidation expectationValidation,
            xPvaGaussianFractalLedgerSnapshot gaussianLedger,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            xPvaEventSequence active,
            xPvaEventSequence completed,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaBinaryObservation activeBinary,
            xPvaBinaryObservation completedBinary,
            xPvaStructureObservation activeStructure,
            xPvaStructureObservation completedStructure,
            xPvaAmbiguityObservation activeAmbiguity,
            xPvaAmbiguityObservation completedAmbiguity,
            xPvaExpectationObservation activeExpectation,
            xPvaExpectationObservation completedExpectation,
            xPvaReasonChainObservation activeReason,
            xPvaReasonChainObservation completedReason)
        {
            if (!ExportHtmlTable || htmlRows == null)
                return;

            htmlRows.Add(new string[]
            {
                CurrentBar.ToString(CultureInfo.InvariantCulture),
                Time[0].ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Safe(ev != null ? ev.Label : null),
                Safe(level1 != null ? level1.State.ToString() : null),
                Safe(level1 != null ? level1.Direction.ToString() : null),
                Safe(level1 != null ? level1.Role.ToString() : null),
                Safe(level1 != null ? level1.RoleConfidence.ToString() : null),
                Safe(level1 != null ? level1.Pattern : null),
                Safe(level1 != null ? level1.Reason : null),
                Safe(level1 != null ? level1.RoleReason : null),
                Safe(level1 != null ? level1.RoleConfidenceReason : null),
                Safe(gaussian != null ? gaussian.State.ToString() : null),
                Safe(gaussian != null ? gaussian.Direction.ToString() : null),
                Safe(gaussian != null ? gaussian.Phase.ToString() : null),
                Safe(gaussian != null ? gaussian.StartBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussian != null ? gaussian.EndBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussian != null ? gaussian.Pattern : null),
                Safe(gaussian != null && gaussian.HasPeak ? gaussian.PeakBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussian != null && gaussian.HasTrough ? gaussian.TroughBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussian != null ? gaussian.HasAcceleration.ToString() : null),
                Safe(gaussian != null ? gaussian.Reason : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.State.ToString() : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.Direction.ToString() : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.StartBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.EndBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.Pattern : null),
                Safe(gaussianNarrative != null ? gaussianNarrative.Reason : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.Expectation.ToString() : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.ExpectedDirection.ToString() : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.ExpectedPattern : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.Reason : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.IsExpectationMet.ToString() : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.IsExpectationViolated.ToString() : null),
                Safe(gaussianExpectation != null && gaussianExpectation.ResolutionBar > 0 ? gaussianExpectation.ResolutionBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(gaussianExpectation != null ? gaussianExpectation.ResolutionReason : null),
                Safe(expectationValidation != null ? expectationValidation.Outcome.ToString() : null),
                Safe(expectationValidation != null ? expectationValidation.ExpectedPattern : null),
                Safe(expectationValidation != null ? expectationValidation.ObservedPattern : null),
                Safe(expectationValidation != null && expectationValidation.ResolutionBar > 0 ? expectationValidation.ResolutionBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(expectationValidation != null ? expectationValidation.Reason : null),
                Safe(gaussianLedger != null ? FormatLedgerActiveNodeIds(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerCompletedNodeIds(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerMembershipNodeIds(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerPrimaryLevel(gaussianLedger) : null),
                Safe(gaussianLedger != null ? gaussianLedger.ParentNodeIds : null),
                Safe(gaussianLedger != null ? gaussianLedger.ChildNodeIds : null),
                Safe(gaussianLedger != null ? FormatLedgerBarRoles(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "P1") : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "P2") : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "P3") : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "FTT") : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "Peak") : null),
                Safe(gaussianLedger != null ? FormatLedgerSpecialBars(gaussianLedger, "Trough") : null),
                Safe(gaussianLedger != null ? FormatLedgerAmbiguity(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? gaussianLedger.Reason : null),
                Safe(gaussianLedger != null ? gaussianLedger.HierarchyParentCandidateNodeIds : null),
                Safe(gaussianLedger != null ? gaussianLedger.HierarchyChildCandidateNodeIds : null),
                Safe(gaussianLedger != null ? gaussianLedger.HierarchyConfidence : null),
                Safe(gaussianLedger != null ? gaussianLedger.HierarchyReason : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorBar(gaussianLedger, "P1") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorStatus(gaussianLedger, "P1") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorBar(gaussianLedger, "P2") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorStatus(gaussianLedger, "P2") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorBar(gaussianLedger, "P3") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorStatus(gaussianLedger, "P3") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorBar(gaussianLedger, "FTT") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorStatus(gaussianLedger, "FTT") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorCollapseWarning(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorGeometryConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorPeakLegitimacy(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorP2ExtremeBasis(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorP2ExtremeReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3CandidateBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3CandidateConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3CandidateBasis(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3ContextSignature(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3CandidateReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3PromotionStatus(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3PromotionBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3PromotionReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3Outcome(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3OutcomeBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3BarsUntilOutcome(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3OutcomeRule(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3CandidateAge(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3OutcomeContextSignature(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3OutcomeReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTCandidateBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTCandidateConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTCandidateBasis(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTContextSignature(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTCandidateReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTPromotionStatus(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTPromotionBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTPromotionReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTOutcome(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTOutcomeBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTBarsUntilOutcome(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTOutcomeContextSignature(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTOutcomeReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerCandidateCreationBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerCandidateReplacementBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerSupersededByCandidateBar(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerCandidateLifecycleViolation(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerSameBarAnchorWarning(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerSameBarAnchorReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerP3SuppressedBySameBarRule(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerFTTSuppressedBySameBarRule(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorPattern(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorReason(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "Ordering") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "Collapse") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "MissingP2") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "MissingP3") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "MissingFTT") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationFlag(gaussianLedger, "PeakConflict") : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationConfidence(gaussianLedger) : null),
                Safe(gaussianLedger != null ? FormatLedgerAnchorValidationReason(gaussianLedger) : null),
                Safe(container != null ? container.Level.ToString() : null),
                Safe(container != null ? container.Direction.ToString() : null),
                Safe(container != null ? container.State.ToString() : null),
                Safe(container != null ? container.StartBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(container != null ? container.EndBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(container != null ? container.Pattern : null),
                Safe(container != null ? FormatContainerParent(container) : null),
                Safe(container != null ? container.Reason : null),
                Safe(container != null ? container.CompletionReason : null),
                Safe(level3Context != null ? level3Context.State.ToString() : null),
                Safe(level3Context != null ? level3Context.Direction.ToString() : null),
                Safe(level3Context != null ? level3Context.StartBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(level3Context != null ? level3Context.EndBar.ToString(CultureInfo.InvariantCulture) : null),
                Safe(level3Context != null ? level3Context.Pattern : null),
                Safe(level3Context != null ? level3Context.Reason : null),
                Safe(active != null ? active.Label : null),
                Safe(completed != null ? completed.Label : null),
                Safe(activeDominance != null ? activeDominance.Label : null),
                Safe(completedDominance != null ? completedDominance.Label : null),
                Safe(activeBinary != null ? activeBinary.Label : null),
                Safe(completedBinary != null ? completedBinary.Label : null),
                Safe(activeStructure != null ? activeStructure.Label : null),
                Safe(completedStructure != null ? completedStructure.Label : null),
                Safe(activeAmbiguity != null ? activeAmbiguity.Label : null),
                Safe(completedAmbiguity != null ? completedAmbiguity.Label : null),
                Safe(activeExpectation != null ? activeExpectation.Label : null),
                Safe(completedExpectation != null ? completedExpectation.Label : null),
                Safe(activeReason != null ? activeReason.CompactLabel : null),
                Safe(completedReason != null ? completedReason.CompactLabel : null),
                Safe(activeReason != null ? activeReason.FullText : null),
                Safe(completedReason != null ? completedReason.FullText : null)
            });
        }

        private string FormatPrice(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value;
        }

        private string FormatContainerParent(xPvaContainerContext container)
        {
            if (container == null || container.ParentLevel == xPvaContainerLevel.Unknown)
                return "";

            return container.ParentLevel.ToString()
                + " "
                + container.ParentStartBar.ToString(CultureInfo.InvariantCulture)
                + "-"
                + container.ParentEndBar.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatLedgerActiveNodeIds(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            return FormatLedgerNodeIds(ledger != null ? ledger.ActiveNodes : null);
        }

        private string FormatLedgerCompletedNodeIds(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            return FormatLedgerNodeIds(ledger != null ? ledger.CompletedNodes : null);
        }

        private string FormatLedgerMembershipNodeIds(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
                AppendUniqueToken(sb, ledger.Memberships[i].GaussianNodeId.ToString(CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        private string FormatLedgerPrimaryLevel(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
                AppendUniqueToken(sb, ledger.Memberships[i].Level.ToString(CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        private string FormatLedgerBarRoles(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianBarMembership membership = ledger.Memberships[i];
                AppendUniqueToken(sb, membership.GaussianNodeId.ToString(CultureInfo.InvariantCulture) + ":" + membership.Role);
            }

            return sb.ToString();
        }

        private string FormatLedgerSpecialBars(xPvaGaussianFractalLedgerSnapshot ledger, string kind)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null)
                    continue;

                int? bar = null;
                if (kind == "P1") bar = node.P1Bar;
                else if (kind == "P2") bar = node.P2Bar;
                else if (kind == "P3") bar = node.P3Bar;
                else if (kind == "FTT") bar = node.FTTBar;
                else if (kind == "Peak") bar = node.PeakBar;
                else if (kind == "Trough") bar = node.TroughBar;

                if (bar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + bar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerAmbiguity(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Ambiguity);
            }

            return sb.ToString();
        }

        private string FormatLedgerConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Confidence.ToString("0.00", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorBar(xPvaGaussianFractalLedgerSnapshot ledger, string anchor)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null || node.Anchors == null)
                    continue;

                int? bar = AnchorBar(node.Anchors, anchor);
                if (bar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + bar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorStatus(xPvaGaussianFractalLedgerSnapshot ledger, string anchor)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null || node.Anchors == null)
                    continue;

                AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + AnchorStatus(node.Anchors, anchor));
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.Confidence);
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorCollapseWarning(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.AnchorCollapseWarning.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorGeometryConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.GeometryConfidence.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorPeakLegitimacy(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.PeakLegitimacy);
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorP2ExtremeBasis(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P2ExtremeBasis.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorP2ExtremeReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P2ExtremeReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerP3CandidateBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.P3CandidateBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3CandidateBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerP3CandidateConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3CandidateConfidence.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerP3CandidateBasis(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3CandidateBasis);
            }

            return sb.ToString();
        }

        private string FormatLedgerP3ContextSignature(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && !string.IsNullOrEmpty(node.Anchors.P3ContextSignature))
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3ContextSignature);
            }

            return sb.ToString();
        }

        private string FormatLedgerP3OutcomeContextSignature(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && !string.IsNullOrEmpty(node.Anchors.P3OutcomeContextSignature))
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3OutcomeContextSignature);
            }

            return sb.ToString();
        }

        private string FormatLedgerP3CandidateReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3CandidateReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerP3PromotionStatus(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3PromotionStatus.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerP3PromotionBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.P3PromotionBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3PromotionBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerP3PromotionReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3PromotionReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerP3Outcome(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3Outcome.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerP3OutcomeBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.P3OutcomeBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3OutcomeBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerP3BarsUntilOutcome(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.P3BarsUntilOutcome.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3BarsUntilOutcome.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerP3OutcomeRule(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && !string.IsNullOrEmpty(node.Anchors.P3OutcomeRule))
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3OutcomeRule);
            }

            return sb.ToString();
        }

        private string FormatLedgerP3CandidateAge(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.P3CandidateAge.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3CandidateAge.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerP3OutcomeReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3OutcomeReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerFTTCandidateBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.FTTCandidateBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTCandidateBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTCandidateConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTCandidateConfidence.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTCandidateBasis(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTCandidateBasis);
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTContextSignature(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && !string.IsNullOrEmpty(node.Anchors.FTTContextSignature))
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTContextSignature);
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTOutcomeContextSignature(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && !string.IsNullOrEmpty(node.Anchors.FTTOutcomeContextSignature))
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTOutcomeContextSignature);
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTCandidateReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTCandidateReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerFTTPromotionStatus(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTPromotionStatus.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTPromotionBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.FTTPromotionBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTPromotionBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTPromotionReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTPromotionReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerFTTOutcome(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTOutcome.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTOutcomeBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.FTTOutcomeBar.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTOutcomeBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTBarsUntilOutcome(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null && node.Anchors.FTTBarsUntilOutcome.HasValue)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTBarsUntilOutcome.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTOutcomeReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTOutcomeReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerCandidateCreationBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null || node.Anchors == null)
                    continue;

                string prefix = node.Id.ToString(CultureInfo.InvariantCulture) + ":";
                if (node.Anchors.P3CandidateCreationBar.HasValue)
                    AppendUniqueToken(sb, prefix + "P3=" + node.Anchors.P3CandidateCreationBar.Value.ToString(CultureInfo.InvariantCulture));
                if (node.Anchors.FTTCandidateCreationBar.HasValue)
                    AppendUniqueToken(sb, prefix + "FTT=" + node.Anchors.FTTCandidateCreationBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerCandidateReplacementBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null || node.Anchors == null)
                    continue;

                string prefix = node.Id.ToString(CultureInfo.InvariantCulture) + ":";
                if (node.Anchors.P3CandidateReplacementBar.HasValue)
                    AppendUniqueToken(sb, prefix + "P3=" + node.Anchors.P3CandidateReplacementBar.Value.ToString(CultureInfo.InvariantCulture));
                if (node.Anchors.FTTCandidateReplacementBar.HasValue)
                    AppendUniqueToken(sb, prefix + "FTT=" + node.Anchors.FTTCandidateReplacementBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerSupersededByCandidateBar(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node == null || node.Anchors == null)
                    continue;

                string prefix = node.Id.ToString(CultureInfo.InvariantCulture) + ":";
                if (node.Anchors.P3SupersededByCandidateBar.HasValue)
                    AppendUniqueToken(sb, prefix + "P3=" + node.Anchors.P3SupersededByCandidateBar.Value.ToString(CultureInfo.InvariantCulture));
                if (node.Anchors.FTTSupersededByCandidateBar.HasValue)
                    AppendUniqueToken(sb, prefix + "FTT=" + node.Anchors.FTTSupersededByCandidateBar.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private string FormatLedgerCandidateLifecycleViolation(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.CandidateLifecycleViolation.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerSameBarAnchorWarning(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.SameBarAnchorWarning.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerSameBarAnchorReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.SameBarAnchorReason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerP3SuppressedBySameBarRule(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.P3SuppressedBySameBarRule.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerFTTSuppressedBySameBarRule(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.FTTSuppressedBySameBarRule.ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorPattern(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.Pattern);
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                if (node != null && node.Anchors != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + node.Anchors.Reason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private string FormatLedgerAnchorValidationFlag(xPvaGaussianFractalLedgerSnapshot ledger, string flag)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                xPvaAnchorValidation validation = node != null && node.Anchors != null ? node.Anchors.Validation : null;
                if (validation == null)
                    continue;

                AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + AnchorValidationFlag(validation, flag).ToString());
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorValidationConfidence(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                xPvaAnchorValidation validation = node != null && node.Anchors != null ? node.Anchors.Validation : null;
                if (validation != null)
                    AppendUniqueToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + validation.Confidence);
            }

            return sb.ToString();
        }

        private string FormatLedgerAnchorValidationReason(xPvaGaussianFractalLedgerSnapshot ledger)
        {
            if (ledger == null || ledger.Memberships == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ledger.Memberships.Count; i++)
            {
                xPvaGaussianNode node = FindLedgerNode(ledger, ledger.Memberships[i].GaussianNodeId);
                xPvaAnchorValidation validation = node != null && node.Anchors != null ? node.Anchors.Validation : null;
                if (validation != null)
                    AppendUniqueReasonToken(sb, node.Id.ToString(CultureInfo.InvariantCulture) + ":" + validation.Reason);
            }

            return DeduplicateReasonCell(sb.ToString());
        }

        private bool AnchorValidationFlag(xPvaAnchorValidation validation, string flag)
        {
            if (flag == "Ordering") return validation.AnchorOrderingViolation;
            if (flag == "Collapse") return validation.AnchorCollapseWarning;
            if (flag == "MissingP2") return validation.MissingP2;
            if (flag == "MissingP3") return validation.MissingP3;
            if (flag == "MissingFTT") return validation.MissingFTT;
            if (flag == "PeakConflict") return validation.PeakLegitimacyConflict;
            return false;
        }

        private int? AnchorBar(xPvaGaussianAnchorSet anchors, string anchor)
        {
            if (anchor == "P1") return anchors.P1Bar;
            if (anchor == "P2") return anchors.P2Bar;
            if (anchor == "P3") return anchors.P3Bar;
            if (anchor == "FTT") return anchors.FTTBar;
            return null;
        }

        private xPvaAnchorStatus AnchorStatus(xPvaGaussianAnchorSet anchors, string anchor)
        {
            if (anchor == "P1") return anchors.P1Status;
            if (anchor == "P2") return anchors.P2Status;
            if (anchor == "P3") return anchors.P3Status;
            if (anchor == "FTT") return anchors.FTTStatus;
            return xPvaAnchorStatus.Unknown;
        }

        private string FormatLedgerNodeIds(List<xPvaGaussianNode> nodes)
        {
            if (nodes == null)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nodes.Count; i++)
                AppendUniqueToken(sb, nodes[i].Id.ToString(CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        private xPvaGaussianNode FindLedgerNode(xPvaGaussianFractalLedgerSnapshot ledger, int id)
        {
            xPvaGaussianNode node = FindLedgerNodeInList(ledger.ActiveNodes, id);
            if (node != null)
                return node;

            return FindLedgerNodeInList(ledger.CompletedNodes, id);
        }

        private xPvaGaussianNode FindLedgerNodeInList(List<xPvaGaussianNode> nodes, int id)
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == id)
                    return nodes[i];
            }

            return null;
        }

        private void AppendUniqueToken(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            string existing = "|" + sb.ToString().Replace(",", "|") + "|";
            if (existing.IndexOf("|" + value + "|", StringComparison.Ordinal) >= 0)
                return;

            if (sb.Length > 0)
                sb.Append(",");

            sb.Append(value);
        }

        private void AppendUniqueReasonToken(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
                return;

            string existing = "|" + sb.ToString() + "|";
            if (existing.IndexOf("|" + trimmed + "|", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (sb.Length > 0)
                sb.Append(" | ");

            sb.Append(trimmed);
        }

        private string DeduplicateReasonCell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string[] fragments = value.Split(new string[] { " | " }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fragments.Length; i++)
            {
                string fragment = fragments[i] != null ? fragments[i].Trim() : "";
                if (fragment.Length == 0)
                    continue;

                string existing = "|" + sb.ToString() + "|";
                if (existing.IndexOf("|" + fragment + "|", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (sb.Length > 0)
                    sb.Append(" | ");

                sb.Append(fragment);
            }

            return sb.ToString();
        }

        private string ResolveHtmlFilePath()
        {
            if (!string.IsNullOrWhiteSpace(HtmlFilePath))
                return HtmlFilePath.Trim();

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "xPvaDebug");
            string instrumentName = Instrument != null ? Instrument.FullName : "Instrument";
            foreach (char c in Path.GetInvalidFileNameChars())
                instrumentName = instrumentName.Replace(c, '_');

            string fileName = string.Format(CultureInfo.InvariantCulture,
                "xPvaDebug_{0}_{1}.html",
                instrumentName.Replace(' ', '_'),
                DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));

            return Path.Combine(folder, fileName);
        }

        private void WriteHtmlTable()
        {
            if (!ExportHtmlTable || htmlRows == null || htmlRows.Count == 0)
                return;

            try
            {
                string path = string.IsNullOrWhiteSpace(resolvedHtmlFilePath) ? ResolveHtmlFilePath() : resolvedHtmlFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, BuildHtmlDocument(), Encoding.UTF8);

                if (PrintHtmlPathOnClose)
                    Print("xPva HTML table exported: " + path);
            }
            catch (Exception ex)
            {
                Print("xPva HTML export failed: " + ex.Message);
            }
        }

        private string BuildHtmlDocument()
        {
            string[] headers = new string[]
            {
                "Bar", "Time",
                "Event",
                "Level1 State", "Level1 Direction", "Level1 Role", "Level1 Role Confidence", "Level1 Pattern", "Level1 Reason", "Level1 Role Reason", "Level1 Role Confidence Reason",
                "Gaussian State", "Gaussian Direction", "Gaussian Phase", "Gaussian Start", "Gaussian End", "Gaussian Pattern", "Gaussian Peak", "Gaussian Trough", "Gaussian Accel", "Gaussian Reason",
                "Gaussian Narrative State", "Gaussian Narrative Direction", "Gaussian Narrative Start", "Gaussian Narrative End", "Gaussian Narrative Pattern", "Gaussian Narrative Reason",
                "Gaussian Expectation", "Gaussian Expected Direction", "Gaussian Expected Pattern", "Gaussian Expectation Reason", "Gaussian Expectation Met", "Gaussian Expectation Violated", "Gaussian Expectation Resolution Bar", "Gaussian Expectation Resolution Reason",
                "Expectation Outcome", "Expectation Expected Pattern", "Expectation Observed Pattern", "Expectation Resolution Bar", "Expectation Validation Reason",
                "Gaussian Ledger Active Node Ids", "Gaussian Ledger Completed Node Ids", "Gaussian Ledger Membership Node Ids", "Gaussian Ledger Primary Level", "Gaussian Ledger Parent Node Ids", "Gaussian Ledger Child Node Ids", "Gaussian Ledger Bar Roles", "Gaussian Ledger P1", "Gaussian Ledger P2", "Gaussian Ledger P3", "Gaussian Ledger FTT", "Gaussian Ledger Peak", "Gaussian Ledger Trough", "Gaussian Ledger Ambiguity", "Gaussian Ledger Confidence", "Gaussian Ledger Reason",
                "Hierarchy Parent Candidates", "Hierarchy Child Candidates", "Hierarchy Confidence", "Hierarchy Reason",
                "Gaussian Anchor P1", "Gaussian Anchor P1 Status", "Gaussian Anchor P2", "Gaussian Anchor P2 Status", "Gaussian Anchor P3", "Gaussian Anchor P3 Status", "Gaussian Anchor FTT", "Gaussian Anchor FTT Status", "Gaussian Anchor Confidence", "Gaussian Anchor Collapse Warning", "Gaussian Anchor Geometry Confidence", "Gaussian Anchor Peak Legitimacy", "Gaussian Anchor P2 Extreme Basis", "Gaussian Anchor P2 Extreme Reason", "Gaussian P3 Candidate", "Gaussian P3 Candidate Confidence", "Gaussian P3 Candidate Basis", "P3 Context Signature", "Gaussian P3 Candidate Reason", "Gaussian P3 Promotion Status", "Gaussian P3 Promotion Bar", "Gaussian P3 Promotion Reason", "Gaussian P3 Outcome", "Gaussian P3 Outcome Bar", "Gaussian P3 Bars Until Outcome", "Gaussian P3 Outcome Rule", "Gaussian P3 Candidate Age", "P3 Outcome Context Signature", "Gaussian P3 Outcome Reason", "Gaussian FTT Candidate", "Gaussian FTT Candidate Confidence", "Gaussian FTT Candidate Basis", "FTT Context Signature", "Gaussian FTT Candidate Reason", "Gaussian FTT Promotion Status", "Gaussian FTT Promotion Bar", "Gaussian FTT Promotion Reason", "Gaussian FTT Outcome", "Gaussian FTT Outcome Bar", "Gaussian FTT Bars Until Outcome", "FTT Outcome Context Signature", "Gaussian FTT Outcome Reason", "Candidate Creation Bar", "Candidate Replacement Bar", "Superseded By Candidate Bar", "CandidateLifecycleViolation", "Same-Bar Anchor Warning", "Same-Bar Anchor Reason", "P3 Suppressed By Same-Bar Rule", "FTT Suppressed By Same-Bar Rule", "Gaussian Anchor Pattern", "Gaussian Anchor Reason",
                "Anchor Ordering Violation", "Anchor Collapse Warning", "Anchor Missing P2", "Anchor Missing P3", "Anchor Missing FTT", "Anchor Peak Legitimacy Conflict", "Anchor Validation Confidence", "Anchor Validation Reason",
                "Container Level", "Container Direction", "Container State", "Container Start", "Container End", "Container Pattern", "Container Parent", "Container Reason", "Container Completion Reason",
                "Level3 Context State", "Level3 Context Direction", "Level3 Context Start", "Level3 Context End", "Level3 Context Pattern", "Level3 Context Reason",
                "Active Sequence", "Done Sequence",
                "Active Dominance", "Done Dominance",
                "Active Binary", "Done Binary",
                "Active Structure", "Done Structure",
                "Active Grammar", "Done Grammar",
                "Active Expectation", "Done Expectation",
                "Active Reason", "Done Reason",
                "Active Full Reason", "Done Full Reason"
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.AppendLine("<title>xPva Debug Table</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{margin:0;font-family:Segoe UI,Arial,sans-serif;background:#111827;color:#e5e7eb;} .wrap{padding:18px;} h1{font-size:20px;margin:0 0 10px;} h2{font-size:18px;margin:22px 0 10px;} h3{font-size:15px;margin:14px 0 8px;} .meta{color:#9ca3af;margin-bottom:14px;font-size:13px;} .toolbar{position:sticky;top:0;z-index:8;background:#111827;padding:10px 0;border-bottom:1px solid #374151;display:flex;gap:10px;align-items:center;flex-wrap:wrap;} input,select,button{background:#1f2937;color:#e5e7eb;border:1px solid #4b5563;border-radius:6px;padding:6px 9px;} button{cursor:pointer;} .tableWrap{overflow:auto;height:calc(100vh - 138px);border:1px solid #374151;border-radius:8px;} table{border-collapse:separate;border-spacing:0;width:100%;font-size:12px;white-space:nowrap;} th,td{border-bottom:1px solid #263244;padding:5px 8px;text-align:left;vertical-align:top;background:#111827;} th{position:sticky;top:0;background:#1f2937;color:#f9fafb;z-index:5;} tr:nth-child(even) td{background:#111f33;} tr:hover td{background:#233047;} th:nth-child(1),td:nth-child(1){position:sticky;left:0;z-index:4;min-width:64px;} th:nth-child(2),td:nth-child(2){position:sticky;left:80px;z-index:4;min-width:142px;} th:nth-child(1),th:nth-child(2){z-index:7;background:#1f2937;} td:nth-child(1),td:nth-child(2){box-shadow:1px 0 0 #374151;} .warn{color:#fca5a5;font-weight:600;} .watch{color:#fcd34d;font-weight:600;} .good{color:#86efac;font-weight:600;} .muted{color:#9ca3af;} .reason{max-width:900px;white-space:normal;} .bar{font-weight:700;color:#bfdbfe;} .changed td{outline:1px solid rgba(252,211,77,.18);} .stats{margin-top:22px;border-top:1px solid #374151;padding-top:12px;} .stats table{width:auto;min-width:760px;margin-bottom:18px;} .stats th{position:static;} .rate{color:#bfdbfe;font-weight:700;}");
            sb.AppendLine("</style></head><body><div class=\"wrap\">");
            sb.AppendLine("<h1>xPva Debug Table</h1>");
            sb.Append("<div class=\"meta\">Rows: ").Append(htmlRows.Count.ToString(CultureInfo.InvariantCulture)).Append(" | Generated: ").Append(HtmlEncode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))).AppendLine("</div>");
            sb.AppendLine("<div class=\"toolbar\"><input id=\"filter\" placeholder=\"Filter rows...\" size=\"34\"><label><input id=\"changesOnly\" type=\"checkbox\"> State changes only</label><label>Rows/page <select id=\"pageSize\"><option>50</option><option selected>100</option><option>250</option><option>500</option><option>1000</option></select></label><button id=\"prev\">Prev</button><span id=\"pageInfo\" class=\"muted\"></span><button id=\"next\">Next</button></div>");
            sb.AppendLine("<div class=\"tableWrap\"><table id=\"grid\"><thead><tr>");
            foreach (string h in headers)
                sb.Append("<th>").Append(HtmlEncode(h)).AppendLine("</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (string[] row in htmlRows)
            {
                sb.AppendLine("<tr>");
                for (int i = 0; i < headers.Length; i++)
                {
                    string value = i < row.Length ? row[i] : "";
                    string cls = "";
                    if (i == 0) cls = " class=\"bar\"";
                    else if (value.IndexOf("MISSING", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Missing", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Violation", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Conflict", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Invalid", StringComparison.OrdinalIgnoreCase) >= 0) cls = " class=\"warn\"";
                    else if (value.IndexOf("Complete", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Observed", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0) cls = " class=\"good\"";
                    else if (value.IndexOf("Fragmented", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0) cls = " class=\"warn\"";
                    else if (value.IndexOf("Suspended", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("COMP", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("CAUTION", StringComparison.OrdinalIgnoreCase) >= 0) cls = " class=\"watch\"";
                    if (i == 8 || i == 9 || i == 10 || i == 16 || i == 20 || i == 25 || i == 26 || i == 29 || i == 30 || i == 34 || i == 36 || i == 37 || i == 39 || i == 41 || i == 43 || i == 46 || i == 55 || i == 67 || i == 69 || i == 73 || i == 76 || i == 80 || i == 84 || i == 87 || i == 91 || i == 97 || i == 100 || i == 101 || i == 109 || i == 115 || i == 117 || i == 118 || i == 123 || i == 124 || i >= 137) cls = " class=\"reason\"";
                    sb.Append("<td").Append(cls).Append(">").Append(HtmlEncode(value)).AppendLine("</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
            AppendAnchorBasisStatisticsSection(sb);
            sb.AppendLine("<script>");
            sb.AppendLine(@"const rows=[...document.querySelectorAll('#grid tbody tr')];
let filtered=rows.slice(); let page=0;
const filter=document.getElementById('filter'), size=document.getElementById('pageSize'), info=document.getElementById('pageInfo'), changesOnly=document.getElementById('changesOnly');
function key(r){const c=r.children; return [3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,101,102,103,104,105,106,107,108,109,110,111,112].map(i=>c[i]?.innerText||'').join('|');}
let prev=''; rows.forEach((r,i)=>{const k=key(r); if(i===0 || k!==prev) r.dataset.changed='1'; else r.dataset.changed='0'; if(r.dataset.changed==='1') r.classList.add('changed'); prev=k;});
function apply(){const q=filter.value.toLowerCase(); filtered=rows.filter(r=>r.innerText.toLowerCase().includes(q) && (!changesOnly.checked || r.dataset.changed==='1')); page=0; render();}
function render(){const n=parseInt(size.value,10); const pages=Math.max(1,Math.ceil(filtered.length/n)); if(page>=pages)page=pages-1; rows.forEach(r=>r.style.display='none'); filtered.slice(page*n,page*n+n).forEach(r=>r.style.display=''); info.textContent=`Page ${page+1} / ${pages} - ${filtered.length} matching rows`;}
document.getElementById('prev').onclick=()=>{if(page>0){page--;render();}};
document.getElementById('next').onclick=()=>{const n=parseInt(size.value,10); if((page+1)*n<filtered.length){page++;render();}};
filter.oninput=apply; size.onchange=()=>{page=0;render();}; changesOnly.onchange=apply; render();");
            sb.AppendLine("</script></div></body></html>");
            return sb.ToString();
        }

        private void AppendAnchorBasisStatisticsSection(StringBuilder sb)
        {
            List<xPvaCandidateEventRecord> rawEvents = gaussianFractalLedger != null ? gaussianFractalLedger.RawCandidateEventRecords : null;
            List<xPvaAnchorBasisStatistic> p3Stats = BuildAnchorBasisStatsFromRaw(rawEvents, "P3");
            List<xPvaAnchorBasisStatistic> fttStats = BuildAnchorBasisStatsFromRaw(rawEvents, "FTT");
            List<xPvaContextSignatureStatistic> p3ContextStats = BuildContextSignatureStatsFromRaw(rawEvents, "P3");
            List<xPvaContextSignatureStatistic> fttContextStats = BuildContextSignatureStatsFromRaw(rawEvents, "FTT");

            sb.AppendLine("<div class=\"stats\">");
            sb.AppendLine("<h2>ANCHOR BASIS ANALYTICS</h2>");
            AppendAnchorBasisStatsTable(sb, "P3 Candidate Basis Statistics", p3Stats);
            AppendAnchorBasisStatsTable(sb, "FTT Candidate Basis Statistics", fttStats);
            AppendTopAnchorBasisStatsTable(sb, "Top P3 Bases By Confirmation Rate", p3Stats);
            AppendTopAnchorBasisStatsTable(sb, "Top FTT Bases By Confirmation Rate", fttStats);
            AppendContextSignatureAnalyticsSection(sb, p3ContextStats, fttContextStats);
            AppendContextPredictivePowerAnalysis(sb, rawEvents);
            AppendAnalyticsIntegrityAuditSection(sb, p3Stats, fttStats, p3ContextStats, fttContextStats, rawEvents);
            AppendUnresolvedRootCauseReport(sb, rawEvents);
            sb.AppendLine("</div>");
        }

        private sealed class AnalyticsTotals
        {
            public int Created;
            public int Confirmed;
            public int Rejected;
            public int Superseded;
            public int Expired;
            public int Unresolved;
            public int Invalidated;
        }

        private sealed class RawCandidateState
        {
            public string Key;
            public string NodeId;
            public string AnchorType;
            public string Basis;
            public string ContextSignature;
            public string Confidence;
            public int CandidateBar;
            public int LastTouchedBar;
            public string CurrentState;
            public string Outcome;
            public int? OutcomeBar;
            public string ResolutionReason;
            public string Narrative;
            public string Expectation;
            public string ContainerLevel;
            public string Level3State;
        }

        private sealed class ContextPredictiveStats
        {
            public string CandidateType;
            public string ContextSignature;
            public int Created;
            public int Confirmed;
            public int Rejected;
            public int Superseded;
            public int Expired;
            public int Unresolved;
            public int Invalidated;
            public int ResolvedCount;
            public int TotalBarsToConfirmation;
            public int ConfirmationBarCount;
            public int TotalBarsToFailure;
            public int FailureBarCount;
            public int TotalBarsToResolution;
            public int ResolutionBarCount;
            public double ConfirmationRate;
            public double RejectionRate;
            public double SupersededRate;
            public double ExpiredRate;
            public double UnresolvedRate;
            public double ResolutionRate;
            public double AvgBarsToConfirmation;
            public double AvgBarsToFailure;
            public double AvgBarsToResolution;
            public double PredictiveScore;
            public double RiskScore;
        }

        private sealed class UnresolvedCandidateRecord
        {
            public string CandidateId;
            public string CandidateType;
            public string Basis;
            public string ContextSignature;
            public int CreationBar;
            public int LastTouchedBar;
            public int AgeBars;
            public string CurrentState;
            public string ResolutionState;
            public string ResolutionReason;
            public string Narrative;
            public string Expectation;
            public string ContainerLevel;
            public string Level3State;
        }

        private sealed class UnresolvedGroupStats
        {
            public string Key;
            public int Count;
            public int TotalAgeBars;
            public int MaxAgeBars;
            public List<int> Ages = new List<int>();
        }

        private sealed class ContributionStats
        {
            public string Key;
            public int TotalCount;
            public int UnresolvedCount;
            public int RejectedCount;
            public int ResolvedCount;
            public int TotalUnresolvedAgeBars;
        }

        private sealed class AgeDistribution
        {
            public string Label;
            public int AllCount;
            public int P3Count;
            public int FTTCount;
        }

        private sealed class ConclusionScore
        {
            public string Label;
            public double Score;
            public string Reason;
        }

        private const int CandidateExpirationThresholdBars = 12;

        private void AppendAnalyticsIntegrityAuditSection(
            StringBuilder sb,
            List<xPvaAnchorBasisStatistic> p3BasisStats,
            List<xPvaAnchorBasisStatistic> fttBasisStats,
            List<xPvaContextSignatureStatistic> p3ContextStats,
            List<xPvaContextSignatureStatistic> fttContextStats,
            List<xPvaCandidateEventRecord> rawEvents)
        {
            AnalyticsTotals p3BasisTotals = SumBasisTotals(p3BasisStats);
            AnalyticsTotals p3ContextTotals = SumContextTotals(p3ContextStats);
            AnalyticsTotals rawP3Totals = SumRawCandidateTotals(rawEvents, "P3");

            AnalyticsTotals fttBasisTotals = SumBasisTotals(fttBasisStats);
            AnalyticsTotals fttContextTotals = SumContextTotals(fttContextStats);
            AnalyticsTotals rawFTTTotals = SumRawCandidateTotals(rawEvents, "FTT");

            bool p3BasisCreatedMismatch = p3BasisTotals.Created != rawP3Totals.Created;
            bool p3ContextCreatedMismatch = p3ContextTotals.Created != rawP3Totals.Created;
            bool p3BasisOutcomeMismatch = HasOutcomeMismatch(p3BasisTotals, rawP3Totals);
            bool p3ContextOutcomeMismatch = HasOutcomeMismatch(p3ContextTotals, rawP3Totals);
            bool p3RawBasisMismatch = HasAnyMismatch(rawP3Totals, p3BasisTotals);
            bool p3RawContextMismatch = HasAnyMismatch(rawP3Totals, p3ContextTotals);

            bool fttBasisCreatedMismatch = fttBasisTotals.Created != rawFTTTotals.Created;
            bool fttContextCreatedMismatch = fttContextTotals.Created != rawFTTTotals.Created;
            bool fttBasisOutcomeMismatch = HasOutcomeMismatch(fttBasisTotals, rawFTTTotals);
            bool fttContextOutcomeMismatch = HasOutcomeMismatch(fttContextTotals, rawFTTTotals);
            bool fttRawBasisMismatch = HasAnyMismatch(rawFTTTotals, fttBasisTotals);
            bool fttRawContextMismatch = HasAnyMismatch(rawFTTTotals, fttContextTotals);

            bool internalCountFailure = HasInternalCountFailure(rawP3Totals)
                || HasInternalCountFailure(rawFTTTotals)
                || HasInternalCountFailure(p3BasisTotals)
                || HasInternalCountFailure(p3ContextTotals)
                || HasInternalCountFailure(fttBasisTotals)
                || HasInternalCountFailure(fttContextTotals);

            string status = IntegrityStatus(
                internalCountFailure,
                p3BasisCreatedMismatch,
                p3ContextCreatedMismatch,
                p3BasisOutcomeMismatch,
                p3ContextOutcomeMismatch,
                p3RawBasisMismatch,
                p3RawContextMismatch,
                fttBasisCreatedMismatch,
                fttContextCreatedMismatch,
                fttBasisOutcomeMismatch,
                fttContextOutcomeMismatch,
                fttRawBasisMismatch,
                fttRawContextMismatch);

            sb.AppendLine("<h2>ANALYTICS INTEGRITY AUDIT</h2>");
            sb.Append("<p><strong>AnalyticsIntegrityStatus:</strong> ").Append(HtmlEncode(status)).AppendLine("</p>");
            sb.AppendLine("<p class=\"muted\">Candidate is counted once per candidate creation event. Candidate outcome is counted once per final outcome state. Suppressed candidates are not counted as created unless explicitly represented as candidate events. Unknown/unavailable candidates are not counted as created. Raw totals are derived from the candidate event ledger; unresolved records preserve one counted event per candidate while updating LastTouchedBar for age analysis.</p>");

            AppendRawCandidateLedgerSummary(sb, rawP3Totals, rawFTTTotals, p3RawBasisMismatch, p3RawContextMismatch, fttRawBasisMismatch, fttRawContextMismatch);

            AppendAuditTotalsTable(sb, "P3 totals", rawP3Totals);
            AppendAuditComparisonTable(sb, "P3 consistency checks", p3BasisTotals, p3ContextTotals, rawP3Totals);
            AppendAuditFlagsTable(sb, "P3 audit booleans",
                p3BasisCreatedMismatch,
                p3ContextCreatedMismatch,
                p3BasisOutcomeMismatch,
                p3ContextOutcomeMismatch,
                p3RawBasisMismatch,
                p3RawContextMismatch);

            AppendAuditTotalsTable(sb, "FTT totals", rawFTTTotals);
            AppendAuditComparisonTable(sb, "FTT consistency checks", fttBasisTotals, fttContextTotals, rawFTTTotals);
            AppendAuditFlagsTable(sb, "FTT audit booleans",
                fttBasisCreatedMismatch,
                fttContextCreatedMismatch,
                fttBasisOutcomeMismatch,
                fttContextOutcomeMismatch,
                fttRawBasisMismatch,
                fttRawContextMismatch);
        }

        private AnalyticsTotals SumBasisTotals(List<xPvaAnchorBasisStatistic> stats)
        {
            AnalyticsTotals totals = new AnalyticsTotals();
            if (stats == null)
                return totals;

            for (int i = 0; i < stats.Count; i++)
            {
                xPvaAnchorBasisStatistic stat = stats[i];
                if (stat == null)
                    continue;

                totals.Created += stat.Created;
                totals.Confirmed += stat.Confirmed;
                totals.Rejected += stat.Rejected;
                totals.Superseded += stat.Superseded;
                totals.Expired += stat.Expired;
                totals.Unresolved += stat.Unresolved;
            }

            return totals;
        }

        private AnalyticsTotals SumContextTotals(List<xPvaContextSignatureStatistic> stats)
        {
            AnalyticsTotals totals = new AnalyticsTotals();
            if (stats == null)
                return totals;

            for (int i = 0; i < stats.Count; i++)
            {
                xPvaContextSignatureStatistic stat = stats[i];
                if (stat == null)
                    continue;

                totals.Created += stat.Created;
                totals.Confirmed += stat.Confirmed;
                totals.Rejected += stat.Rejected;
                totals.Superseded += stat.Superseded;
                totals.Expired += stat.Expired;
                totals.Unresolved += stat.Unresolved;
            }

            return totals;
        }

        private AnalyticsTotals SumRawCandidateTotals(List<xPvaCandidateEventRecord> records, string anchorType)
        {
            AnalyticsTotals totals = new AnalyticsTotals();
            if (records == null || string.IsNullOrEmpty(anchorType))
                return totals;

            HashSet<string> createdKeys = new HashSet<string>();
            Dictionary<string, string> latestStateByKey = new Dictionary<string, string>();

            for (int i = 0; i < records.Count; i++)
            {
                xPvaCandidateEventRecord record = records[i];
                if (record == null || !string.Equals(record.AnchorType, anchorType, StringComparison.OrdinalIgnoreCase) || !record.CandidateBar.HasValue)
                    continue;

                string key = RawCandidateKey(record);
                if (string.Equals(record.EventType, "Created", StringComparison.OrdinalIgnoreCase))
                    createdKeys.Add(key);
                else
                    latestStateByKey[key] = NormalizeRawEventType(record.EventType);
            }

            totals.Created = createdKeys.Count;

            foreach (string key in createdKeys)
            {
                string state;
                if (!latestStateByKey.TryGetValue(key, out state))
                    state = "Unresolved";

                if (state == "Confirmed")
                    totals.Confirmed++;
                else if (state == "Rejected")
                    totals.Rejected++;
                else if (state == "Superseded")
                    totals.Superseded++;
                else if (state == "Expired")
                    totals.Expired++;
                else if (state == "Invalidated")
                    totals.Invalidated++;
                else
                    totals.Unresolved++;
            }

            return totals;
        }

        private List<xPvaAnchorBasisStatistic> BuildAnchorBasisStatsFromRaw(List<xPvaCandidateEventRecord> records, string anchorType)
        {
            Dictionary<string, xPvaAnchorBasisStatistic> stats = new Dictionary<string, xPvaAnchorBasisStatistic>();
            Dictionary<string, RawCandidateState> states = BuildRawCandidateStates(records, anchorType);

            foreach (RawCandidateState state in states.Values)
            {
                string basis = string.IsNullOrEmpty(state.Basis) ? "Unavailable" : state.Basis;
                xPvaAnchorBasisStatistic stat;
                if (!stats.TryGetValue(basis, out stat))
                {
                    stat = new xPvaAnchorBasisStatistic { Basis = basis, BasisName = basis };
                    stats[basis] = stat;
                }

                stat.Created++;
                IncrementRawConfidenceCount(stat, state.Confidence);
                ApplyRawOutcomeToBasisStatistic(stat, state);
            }

            return FinalizeRawBasisStats(stats);
        }

        private List<xPvaContextSignatureStatistic> BuildContextSignatureStatsFromRaw(List<xPvaCandidateEventRecord> records, string anchorType)
        {
            Dictionary<string, xPvaContextSignatureStatistic> stats = new Dictionary<string, xPvaContextSignatureStatistic>();
            Dictionary<string, RawCandidateState> states = BuildRawCandidateStates(records, anchorType);

            foreach (RawCandidateState state in states.Values)
            {
                string signature = string.IsNullOrEmpty(state.ContextSignature) ? "Unknown" : state.ContextSignature;
                xPvaContextSignatureStatistic stat;
                if (!stats.TryGetValue(signature, out stat))
                {
                    stat = new xPvaContextSignatureStatistic { Signature = signature };
                    stats[signature] = stat;
                }

                stat.Created++;
                ApplyRawOutcomeToContextStatistic(stat, state);
            }

            return FinalizeRawContextStats(stats);
        }

        private Dictionary<string, RawCandidateState> BuildRawCandidateStates(List<xPvaCandidateEventRecord> records, string anchorType)
        {
            Dictionary<string, RawCandidateState> states = new Dictionary<string, RawCandidateState>();
            if (records == null || string.IsNullOrEmpty(anchorType))
                return states;

            for (int i = 0; i < records.Count; i++)
            {
                xPvaCandidateEventRecord record = records[i];
                if (record == null || !record.CandidateBar.HasValue || !string.Equals(record.AnchorType, anchorType, StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = RawCandidateKey(record);
                RawCandidateState state;
                if (!states.TryGetValue(key, out state))
                {
                    state = new RawCandidateState
                    {
                        Key = key,
                        NodeId = string.IsNullOrEmpty(record.NodeId) ? "Unknown" : record.NodeId,
                        AnchorType = string.IsNullOrEmpty(record.AnchorType) ? anchorType : record.AnchorType,
                        Basis = "Unavailable",
                        ContextSignature = "Unknown",
                        Confidence = "Unknown",
                        CandidateBar = record.CandidateBar.Value,
                        LastTouchedBar = record.LastTouchedBar > 0 ? record.LastTouchedBar : record.Bar,
                        CurrentState = "Unknown",
                        Outcome = "Unresolved",
                        ResolutionReason = "",
                        Narrative = "Unknown",
                        Expectation = "Unknown",
                        ContainerLevel = "Unknown",
                        Level3State = "Unknown"
                    };
                    states[key] = state;
                }

                int lastTouched = record.LastTouchedBar > 0 ? record.LastTouchedBar : record.Bar;
                if (lastTouched > state.LastTouchedBar)
                    state.LastTouchedBar = lastTouched;

                if (!string.IsNullOrEmpty(record.CurrentState))
                    state.CurrentState = record.CurrentState;

                if (!string.IsNullOrEmpty(record.ResolutionReason))
                    state.ResolutionReason = record.ResolutionReason;

                if (!string.IsNullOrEmpty(record.Narrative))
                    state.Narrative = record.Narrative;

                if (!string.IsNullOrEmpty(record.Expectation))
                    state.Expectation = record.Expectation;

                if (!string.IsNullOrEmpty(record.ContainerLevel))
                    state.ContainerLevel = record.ContainerLevel;

                if (!string.IsNullOrEmpty(record.Level3State))
                    state.Level3State = record.Level3State;

                if (string.Equals(record.EventType, "Created", StringComparison.OrdinalIgnoreCase))
                {
                    state.Basis = string.IsNullOrEmpty(record.Basis) ? "Unavailable" : record.Basis;
                    state.ContextSignature = string.IsNullOrEmpty(record.ContextSignature) ? "Unknown" : record.ContextSignature;
                    state.Confidence = string.IsNullOrEmpty(record.Confidence) ? "Unknown" : record.Confidence;
                }
                else
                {
                    string outcome = NormalizeRawEventType(record.EventType);
                    if (outcome != "Unresolved" || state.Outcome == "Unresolved")
                    {
                        state.Outcome = outcome;
                        state.OutcomeBar = record.OutcomeBar.HasValue ? record.OutcomeBar : (int?)record.Bar;
                    }
                }
            }

            return states;
        }

        private void ApplyRawOutcomeToBasisStatistic(xPvaAnchorBasisStatistic stat, RawCandidateState state)
        {
            if (stat == null || state == null)
                return;

            int barsToResolution = state.OutcomeBar.HasValue ? Math.Max(0, state.OutcomeBar.Value - state.CandidateBar) : 0;

            if (state.Outcome == "Confirmed")
            {
                stat.Confirmed++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.TotalBarsToConfirmation += barsToResolution;
            }
            else if (state.Outcome == "Rejected")
            {
                stat.Rejected++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.TotalBarsToRejection += barsToResolution;
            }
            else if (state.Outcome == "Superseded")
            {
                stat.Superseded++;
                stat.TotalBarsToResolution += barsToResolution;
            }
            else if (state.Outcome == "Expired")
            {
                stat.Expired++;
                stat.TotalBarsToResolution += barsToResolution;
            }
            else
            {
                stat.Unresolved++;
            }
        }

        private void ApplyRawOutcomeToContextStatistic(xPvaContextSignatureStatistic stat, RawCandidateState state)
        {
            if (stat == null || state == null)
                return;

            if (state.Outcome == "Confirmed")
                stat.Confirmed++;
            else if (state.Outcome == "Rejected")
                stat.Rejected++;
            else if (state.Outcome == "Superseded")
                stat.Superseded++;
            else if (state.Outcome == "Expired")
                stat.Expired++;
            else
                stat.Unresolved++;
        }

        private void IncrementRawConfidenceCount(xPvaAnchorBasisStatistic stat, string confidence)
        {
            if (stat == null)
                return;

            if (string.Equals(confidence, "High", StringComparison.OrdinalIgnoreCase))
                stat.HighConfidence++;
            else if (string.Equals(confidence, "Medium", StringComparison.OrdinalIgnoreCase))
                stat.MediumConfidence++;
            else if (string.Equals(confidence, "Low", StringComparison.OrdinalIgnoreCase))
                stat.LowConfidence++;
            else
                stat.UnknownConfidence++;
        }

        private List<xPvaAnchorBasisStatistic> FinalizeRawBasisStats(Dictionary<string, xPvaAnchorBasisStatistic> stats)
        {
            List<xPvaAnchorBasisStatistic> finalized = new List<xPvaAnchorBasisStatistic>();
            foreach (xPvaAnchorBasisStatistic stat in stats.Values)
            {
                xPvaAnchorBasisStatistic clone = stat.Clone();
                clone.Unresolved = Math.Max(0, clone.Created - clone.Confirmed - clone.Rejected - clone.Superseded - clone.Expired);
                clone.ConfirmationRate = clone.Created > 0 ? (double)clone.Confirmed / (double)clone.Created : 0.0;
                clone.RejectionRate = clone.Created > 0 ? (double)clone.Rejected / (double)clone.Created : 0.0;
                clone.SupersessionRate = clone.Created > 0 ? (double)clone.Superseded / (double)clone.Created : 0.0;
                clone.ExpirationRate = clone.Created > 0 ? (double)clone.Expired / (double)clone.Created : 0.0;
                clone.UnresolvedRate = clone.Created > 0 ? (double)clone.Unresolved / (double)clone.Created : 0.0;

                int resolvedCount = clone.Confirmed + clone.Rejected + clone.Superseded + clone.Expired;
                clone.AvgBarsToResolution = resolvedCount > 0 ? (double)clone.TotalBarsToResolution / (double)resolvedCount : 0.0;
                clone.AvgBarsToConfirmation = clone.Confirmed > 0 ? (double)clone.TotalBarsToConfirmation / (double)clone.Confirmed : 0.0;
                clone.AvgBarsToRejection = clone.Rejected > 0 ? (double)clone.TotalBarsToRejection / (double)clone.Rejected : 0.0;
                finalized.Add(clone);
            }

            finalized.Sort(delegate (xPvaAnchorBasisStatistic left, xPvaAnchorBasisStatistic right)
            {
                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.Basis, right.Basis, StringComparison.OrdinalIgnoreCase);
            });

            return finalized;
        }

        private List<xPvaContextSignatureStatistic> FinalizeRawContextStats(Dictionary<string, xPvaContextSignatureStatistic> stats)
        {
            List<xPvaContextSignatureStatistic> finalized = new List<xPvaContextSignatureStatistic>();
            foreach (xPvaContextSignatureStatistic stat in stats.Values)
            {
                xPvaContextSignatureStatistic clone = stat.Clone();
                clone.Unresolved = Math.Max(0, clone.Created - clone.Confirmed - clone.Rejected - clone.Superseded - clone.Expired);
                clone.ConfirmationRate = clone.Created > 0 ? (double)clone.Confirmed / (double)clone.Created : 0.0;
                finalized.Add(clone);
            }

            finalized.Sort(delegate (xPvaContextSignatureStatistic left, xPvaContextSignatureStatistic right)
            {
                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.Signature, right.Signature, StringComparison.OrdinalIgnoreCase);
            });

            return finalized;
        }

        private string RawCandidateKey(xPvaCandidateEventRecord record)
        {
            return (record.NodeId ?? "Unknown")
                + "|" + (record.AnchorType ?? "Unknown")
                + "|" + (record.CandidateBar.HasValue ? record.CandidateBar.Value.ToString(CultureInfo.InvariantCulture) : "Unknown");
        }

        private string NormalizeRawEventType(string eventType)
        {
            if (string.Equals(eventType, "Confirmed", StringComparison.OrdinalIgnoreCase))
                return "Confirmed";
            if (string.Equals(eventType, "Rejected", StringComparison.OrdinalIgnoreCase))
                return "Rejected";
            if (string.Equals(eventType, "Superseded", StringComparison.OrdinalIgnoreCase))
                return "Superseded";
            if (string.Equals(eventType, "Expired", StringComparison.OrdinalIgnoreCase))
                return "Expired";
            if (string.Equals(eventType, "Invalidated", StringComparison.OrdinalIgnoreCase))
                return "Invalidated";

            return "Unresolved";
        }

        private void AppendContextPredictivePowerAnalysis(StringBuilder sb, List<xPvaCandidateEventRecord> rawEvents)
        {
            List<ContextPredictiveStats> p3Stats = BuildContextPredictiveStats(rawEvents, "P3");
            List<ContextPredictiveStats> fttStats = BuildContextPredictiveStats(rawEvents, "FTT");

            sb.AppendLine("<h2>CONTEXT PREDICTIVE POWER ANALYSIS</h2>");
            AppendContextPredictiveCandidateSection(sb, "P3", p3Stats);
            AppendContextPredictiveCandidateSection(sb, "FTT", fttStats);
            AppendContextPredictiveFinalSummary(sb, p3Stats, fttStats);
        }

        private List<ContextPredictiveStats> BuildContextPredictiveStats(List<xPvaCandidateEventRecord> rawEvents, string anchorType)
        {
            Dictionary<string, ContextPredictiveStats> stats = new Dictionary<string, ContextPredictiveStats>();
            Dictionary<string, RawCandidateState> states = BuildRawCandidateStates(rawEvents, anchorType);

            foreach (RawCandidateState state in states.Values)
            {
                string signature = NormalizeText(state.ContextSignature, "Unknown");
                ContextPredictiveStats stat;
                if (!stats.TryGetValue(signature, out stat))
                {
                    stat = new ContextPredictiveStats
                    {
                        CandidateType = anchorType,
                        ContextSignature = signature
                    };
                    stats[signature] = stat;
                }

                stat.Created++;
                ApplyContextPredictiveOutcome(stat, state);
            }

            List<ContextPredictiveStats> list = new List<ContextPredictiveStats>(stats.Values);
            for (int i = 0; i < list.Count; i++)
                FinalizeContextPredictiveStats(list[i]);

            list.Sort(delegate (ContextPredictiveStats left, ContextPredictiveStats right)
            {
                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.ContextSignature, right.ContextSignature, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private void ApplyContextPredictiveOutcome(ContextPredictiveStats stat, RawCandidateState state)
        {
            if (stat == null || state == null)
                return;

            int barsToResolution = state.OutcomeBar.HasValue ? Math.Max(0, state.OutcomeBar.Value - state.CandidateBar) : 0;

            if (state.Outcome == "Confirmed")
            {
                stat.Confirmed++;
                stat.TotalBarsToConfirmation += barsToResolution;
                stat.ConfirmationBarCount++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.ResolutionBarCount++;
            }
            else if (state.Outcome == "Rejected")
            {
                stat.Rejected++;
                stat.TotalBarsToFailure += barsToResolution;
                stat.FailureBarCount++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.ResolutionBarCount++;
            }
            else if (state.Outcome == "Superseded")
            {
                stat.Superseded++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.ResolutionBarCount++;
            }
            else if (state.Outcome == "Expired")
            {
                stat.Expired++;
                stat.TotalBarsToFailure += barsToResolution;
                stat.FailureBarCount++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.ResolutionBarCount++;
            }
            else if (state.Outcome == "Invalidated")
            {
                stat.Invalidated++;
                stat.TotalBarsToFailure += barsToResolution;
                stat.FailureBarCount++;
                stat.TotalBarsToResolution += barsToResolution;
                stat.ResolutionBarCount++;
            }
            else
            {
                stat.Unresolved++;
            }
        }

        private void FinalizeContextPredictiveStats(ContextPredictiveStats stat)
        {
            if (stat == null)
                return;

            stat.ResolvedCount = stat.Confirmed + stat.Rejected + stat.Superseded + stat.Expired + stat.Invalidated;
            stat.ConfirmationRate = stat.Created > 0 ? (double)stat.Confirmed / (double)stat.Created : 0.0;
            stat.RejectionRate = stat.Created > 0 ? (double)stat.Rejected / (double)stat.Created : 0.0;
            stat.SupersededRate = stat.Created > 0 ? (double)stat.Superseded / (double)stat.Created : 0.0;
            stat.ExpiredRate = stat.Created > 0 ? (double)stat.Expired / (double)stat.Created : 0.0;
            stat.UnresolvedRate = stat.Created > 0 ? (double)stat.Unresolved / (double)stat.Created : 0.0;
            stat.ResolutionRate = stat.Created > 0 ? (double)stat.ResolvedCount / (double)stat.Created : 0.0;
            stat.AvgBarsToConfirmation = stat.ConfirmationBarCount > 0 ? (double)stat.TotalBarsToConfirmation / (double)stat.ConfirmationBarCount : 0.0;
            stat.AvgBarsToFailure = stat.FailureBarCount > 0 ? (double)stat.TotalBarsToFailure / (double)stat.FailureBarCount : 0.0;
            stat.AvgBarsToResolution = stat.ResolutionBarCount > 0 ? (double)stat.TotalBarsToResolution / (double)stat.ResolutionBarCount : 0.0;

            double sampleWeight = Math.Log(1.0 + (double)stat.Created);
            stat.PredictiveScore = stat.ConfirmationRate * sampleWeight * stat.ResolutionRate;
            stat.RiskScore = (stat.RejectionRate + stat.ExpiredRate + stat.UnresolvedRate) * sampleWeight;
        }

        private void AppendContextPredictiveCandidateSection(StringBuilder sb, string candidateType, List<ContextPredictiveStats> stats)
        {
            sb.Append("<h3>").Append(HtmlEncode(candidateType)).AppendLine(" Context Predictive Power</h3>");
            AppendContextPredictiveStatsTable(sb, "All " + candidateType + " Context Signatures", stats, 0, "Created", 0);

            AppendContextPredictiveStatsTable(sb, "Top " + candidateType + " Contexts By Confirmation Rate, Created &gt;= 3", stats, 3, "ConfirmationRate", 25);
            AppendContextPredictiveStatsTable(sb, "Top " + candidateType + " Contexts By Resolution Rate, Created &gt;= 3", stats, 3, "ResolutionRate", 25);
            AppendContextPredictiveStatsTable(sb, "Worst " + candidateType + " Contexts By Rejection/Expiration Rate, Created &gt;= 3", stats, 3, "FailureRate", 25);
            AppendContextPredictiveStatsTable(sb, "Worst " + candidateType + " Contexts By Unresolved Rate, Created &gt;= 3", stats, 3, "UnresolvedRate", 25);

            if (HasContextPredictiveMinimum(stats, 10))
            {
                AppendContextPredictiveStatsTable(sb, "Top " + candidateType + " Contexts By Confirmation Rate, Created &gt;= 10", stats, 10, "ConfirmationRate", 25);
                AppendContextPredictiveStatsTable(sb, "Top " + candidateType + " Contexts By Resolution Rate, Created &gt;= 10", stats, 10, "ResolutionRate", 25);
                AppendContextPredictiveStatsTable(sb, "Worst " + candidateType + " Contexts By Rejection/Expiration Rate, Created &gt;= 10", stats, 10, "FailureRate", 25);
                AppendContextPredictiveStatsTable(sb, "Worst " + candidateType + " Contexts By Unresolved Rate, Created &gt;= 10", stats, 10, "UnresolvedRate", 25);
            }

            AppendContextPredictiveStatsTable(sb, "Best " + candidateType + " Contexts", stats, 3, "PredictiveScore", 25);
            AppendContextPredictiveStatsTable(sb, "Worst " + candidateType + " Contexts", stats, 3, "RiskScore", 25);
            AppendContextPredictiveStatsTable(sb, "Most Common " + candidateType + " Contexts", stats, 0, "Created", 25);
            AppendContextPredictiveStatsTable(sb, "Most Unresolved " + candidateType + " Contexts", stats, 0, "UnresolvedCount", 25);
        }

        private void AppendContextPredictiveStatsTable(
            StringBuilder sb,
            string title,
            List<ContextPredictiveStats> stats,
            int minimumCreated,
            string sortMode,
            int limit)
        {
            List<ContextPredictiveStats> rows = RankContextPredictiveStats(stats, minimumCreated, sortMode);
            if (limit > 0 && rows.Count > limit)
                rows = rows.GetRange(0, limit);

            sb.Append("<h4>").Append(HtmlEncode(title)).AppendLine("</h4>");
            sb.AppendLine("<table><thead><tr><th>ContextSignature</th><th>Created</th><th>Confirmed</th><th>Rejected</th><th>Superseded</th><th>Expired</th><th>Unresolved</th><th>ResolvedCount</th><th>ConfirmationRate</th><th>RejectionRate</th><th>SupersededRate</th><th>ExpiredRate</th><th>UnresolvedRate</th><th>ResolutionRate</th><th>AvgBarsToConfirmation</th><th>AvgBarsToFailure</th><th>AvgBarsToResolution</th><th>PredictiveScore</th><th>RiskScore</th></tr></thead><tbody>");

            if (rows.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"19\">No context signatures match this sample filter.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            for (int i = 0; i < rows.Count; i++)
                AppendContextPredictiveStatsRow(sb, rows[i]);

            sb.AppendLine("</tbody></table>");
        }

        private void AppendContextPredictiveStatsRow(StringBuilder sb, ContextPredictiveStats stat)
        {
            sb.AppendLine("<tr>");
            sb.Append("<td>").Append(HtmlEncode(stat.ContextSignature)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Created.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Rejected.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Superseded.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Expired.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.Unresolved.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.ResolvedCount.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td class=\"rate\">").Append(FormatPercent(stat.ConfirmationRate)).AppendLine("</td>");
            sb.Append("<td>").Append(FormatPercent(stat.RejectionRate)).AppendLine("</td>");
            sb.Append("<td>").Append(FormatPercent(stat.SupersededRate)).AppendLine("</td>");
            sb.Append("<td>").Append(FormatPercent(stat.ExpiredRate)).AppendLine("</td>");
            sb.Append("<td>").Append(FormatPercent(stat.UnresolvedRate)).AppendLine("</td>");
            sb.Append("<td>").Append(FormatPercent(stat.ResolutionRate)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.AvgBarsToConfirmation.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.AvgBarsToFailure.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.AvgBarsToResolution.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.PredictiveScore.ToString("F3", CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(stat.RiskScore.ToString("F3", CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.AppendLine("</tr>");
        }

        private List<ContextPredictiveStats> RankContextPredictiveStats(List<ContextPredictiveStats> stats, int minimumCreated, string sortMode)
        {
            List<ContextPredictiveStats> ranked = new List<ContextPredictiveStats>();
            if (stats == null)
                return ranked;

            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] != null && stats[i].Created >= minimumCreated)
                    ranked.Add(stats[i]);
            }

            ranked.Sort(delegate (ContextPredictiveStats left, ContextPredictiveStats right)
            {
                double leftScore = ContextPredictiveSortValue(left, sortMode);
                double rightScore = ContextPredictiveSortValue(right, sortMode);
                int scoreCompare = rightScore.CompareTo(leftScore);
                if (scoreCompare != 0)
                    return scoreCompare;

                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.ContextSignature, right.ContextSignature, StringComparison.OrdinalIgnoreCase);
            });

            return ranked;
        }

        private double ContextPredictiveSortValue(ContextPredictiveStats stat, string sortMode)
        {
            if (stat == null)
                return 0.0;

            if (string.Equals(sortMode, "ConfirmationRate", StringComparison.OrdinalIgnoreCase))
                return stat.ConfirmationRate;
            if (string.Equals(sortMode, "ResolutionRate", StringComparison.OrdinalIgnoreCase))
                return stat.ResolutionRate;
            if (string.Equals(sortMode, "FailureRate", StringComparison.OrdinalIgnoreCase))
                return stat.RejectionRate + stat.ExpiredRate;
            if (string.Equals(sortMode, "UnresolvedRate", StringComparison.OrdinalIgnoreCase))
                return stat.UnresolvedRate;
            if (string.Equals(sortMode, "PredictiveScore", StringComparison.OrdinalIgnoreCase))
                return stat.PredictiveScore;
            if (string.Equals(sortMode, "RiskScore", StringComparison.OrdinalIgnoreCase))
                return stat.RiskScore;
            if (string.Equals(sortMode, "UnresolvedCount", StringComparison.OrdinalIgnoreCase))
                return stat.Unresolved;
            if (string.Equals(sortMode, "Confirmed", StringComparison.OrdinalIgnoreCase))
                return stat.Confirmed;

            return stat.Created;
        }

        private bool HasContextPredictiveMinimum(List<ContextPredictiveStats> stats, int minimumCreated)
        {
            if (stats == null)
                return false;

            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] != null && stats[i].Created >= minimumCreated)
                    return true;
            }

            return false;
        }

        private void AppendContextPredictiveFinalSummary(StringBuilder sb, List<ContextPredictiveStats> p3Stats, List<ContextPredictiveStats> fttStats)
        {
            ContextPredictiveStats bestP3 = FirstRankedContext(p3Stats, "PredictiveScore", 3);
            ContextPredictiveStats worstP3 = FirstRankedContext(p3Stats, "RiskScore", 3);
            ContextPredictiveStats bestFTT = FirstRankedContext(fttStats, "PredictiveScore", 3);
            ContextPredictiveStats worstFTT = FirstRankedContext(fttStats, "RiskScore", 3);

            sb.AppendLine("<h3>Context Predictive Power Summary</h3>");
            sb.AppendLine("<table><tbody>");
            AppendContextPredictiveSummaryRow(sb, "Best P3 Context Signature", bestP3);
            AppendContextPredictiveSummaryRow(sb, "Worst P3 Context Signature", worstP3);
            AppendContextPredictiveSummaryRow(sb, "Best FTT Context Signature", bestFTT);
            AppendContextPredictiveSummaryRow(sb, "Worst FTT Context Signature", worstFTT);
            sb.Append("<tr><th>P3 Predictive Concentration</th><td>")
                .Append(FormatPercent(ConfirmationConcentrationTopFive(p3Stats)))
                .AppendLine(" of confirmations explained by top 5 context signatures</td></tr>");
            sb.Append("<tr><th>FTT Predictive Concentration</th><td>")
                .Append(FormatPercent(ConfirmationConcentrationTopFive(fttStats)))
                .AppendLine(" of confirmations explained by top 5 context signatures</td></tr>");
            sb.AppendLine("</tbody></table>");
        }

        private void AppendContextPredictiveSummaryRow(StringBuilder sb, string label, ContextPredictiveStats stat)
        {
            sb.Append("<tr><th>").Append(HtmlEncode(label)).Append("</th><td>");
            if (stat == null)
            {
                sb.Append("No context with Created &gt;= 3");
            }
            else
            {
                sb.Append(HtmlEncode(stat.ContextSignature))
                    .Append(" | Created=").Append(stat.Created.ToString(CultureInfo.InvariantCulture))
                    .Append(" | Confirmed=").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture))
                    .Append(" | Confirmation=").Append(FormatPercent(stat.ConfirmationRate))
                    .Append(" | Resolution=").Append(FormatPercent(stat.ResolutionRate))
                    .Append(" | PredictiveScore=").Append(stat.PredictiveScore.ToString("F3", CultureInfo.InvariantCulture))
                    .Append(" | RiskScore=").Append(stat.RiskScore.ToString("F3", CultureInfo.InvariantCulture));
            }

            sb.AppendLine("</td></tr>");
        }

        private ContextPredictiveStats FirstRankedContext(List<ContextPredictiveStats> stats, string sortMode, int minimumCreated)
        {
            List<ContextPredictiveStats> ranked = RankContextPredictiveStats(stats, minimumCreated, sortMode);
            return ranked.Count > 0 ? ranked[0] : null;
        }

        private double ConfirmationConcentrationTopFive(List<ContextPredictiveStats> stats)
        {
            if (stats == null || stats.Count == 0)
                return 0.0;

            int totalConfirmations = 0;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] != null)
                    totalConfirmations += stats[i].Confirmed;
            }

            if (totalConfirmations <= 0)
                return 0.0;

            List<ContextPredictiveStats> ranked = RankContextPredictiveStats(stats, 0, "Confirmed");
            int topConfirmations = 0;
            int limit = Math.Min(5, ranked.Count);
            for (int i = 0; i < limit; i++)
                topConfirmations += ranked[i].Confirmed;

            return (double)topConfirmations / (double)totalConfirmations;
        }

        private void AppendUnresolvedRootCauseReport(StringBuilder sb, List<xPvaCandidateEventRecord> rawEvents)
        {
            Dictionary<string, RawCandidateState> allStates = BuildAllRawCandidateStates(rawEvents);
            List<UnresolvedCandidateRecord> unresolved = BuildUnresolvedCandidateRecords(allStates);

            AnalyticsTotals totals = BuildRootCauseTotals(allStates);

            sb.AppendLine("<h2>UNRESOLVED ROOT CAUSE REPORT</h2>");
            sb.AppendLine("<p class=\"muted\">Resolved means Confirmed, Rejected, Invalidated, or Superseded. Expired is treated as closed, not unresolved. Unresolved means latest candidate state is Unknown, Candidate, Pending, or Unresolved at report generation time.</p>");

            AppendUnresolvedCandidateDetailTable(sb, unresolved);
            AppendUnresolvedGroupTable(sb, "SECTION 1 - ROOT CAUSE BREAKDOWN", BuildUnresolvedGroups(unresolved, "Reason"), unresolved.Count);
            AppendUnresolvedGroupTable(sb, "SECTION 2 - P3 UNRESOLVED ANALYSIS", BuildUnresolvedGroups(FilterUnresolvedByType(unresolved, "P3"), "Reason"), CountUnresolvedByType(unresolved, "P3"));
            AppendUnresolvedGroupTable(sb, "SECTION 3 - FTT UNRESOLVED ANALYSIS", BuildUnresolvedGroups(FilterUnresolvedByType(unresolved, "FTT"), "Reason"), CountUnresolvedByType(unresolved, "FTT"));
            AppendContributionTable(sb, "SECTION 4 - BASIS CONTRIBUTION", BuildContributionStats(allStates, unresolved, true), unresolved.Count);
            AppendContributionTable(sb, "SECTION 5 - CONTEXT CONTRIBUTION", BuildContributionStats(allStates, unresolved, false), unresolved.Count);
            AppendLongestLivedCandidatesTable(sb, unresolved);
            AppendAgeDistributionTable(sb, unresolved);
            AppendExpiredButNeverClosedSection(sb, unresolved);
            AppendUnresolvedSummarySection(sb, totals, unresolved, allStates);
        }

        private Dictionary<string, RawCandidateState> BuildAllRawCandidateStates(List<xPvaCandidateEventRecord> rawEvents)
        {
            Dictionary<string, RawCandidateState> states = BuildRawCandidateStates(rawEvents, "P3");
            Dictionary<string, RawCandidateState> fttStates = BuildRawCandidateStates(rawEvents, "FTT");

            foreach (KeyValuePair<string, RawCandidateState> pair in fttStates)
                states[pair.Key] = pair.Value;

            return states;
        }

        private List<UnresolvedCandidateRecord> BuildUnresolvedCandidateRecords(Dictionary<string, RawCandidateState> states)
        {
            List<UnresolvedCandidateRecord> unresolved = new List<UnresolvedCandidateRecord>();
            if (states == null)
                return unresolved;

            foreach (RawCandidateState state in states.Values)
            {
                if (state == null || IsClosedCandidateOutcome(state.Outcome))
                    continue;

                int lastTouched = state.LastTouchedBar > 0 ? state.LastTouchedBar : state.CandidateBar;
                int ageBars = Math.Max(0, lastTouched - state.CandidateBar);
                string reason = ClassifyUnresolvedReason(state, ageBars);
                unresolved.Add(new UnresolvedCandidateRecord
                {
                    CandidateId = CandidateIdFor(state),
                    CandidateType = NormalizeText(state.AnchorType, "Unknown"),
                    Basis = NormalizeText(state.Basis, "Unavailable"),
                    ContextSignature = NormalizeText(state.ContextSignature, "Unknown"),
                    CreationBar = state.CandidateBar,
                    LastTouchedBar = lastTouched,
                    AgeBars = ageBars,
                    CurrentState = NormalizeText(state.CurrentState, "Unknown"),
                    ResolutionState = NormalizeText(state.Outcome, "Unresolved"),
                    ResolutionReason = reason,
                    Narrative = NormalizeText(state.Narrative, "Unknown"),
                    Expectation = NormalizeText(state.Expectation, "Unknown"),
                    ContainerLevel = NormalizeText(state.ContainerLevel, "Unknown"),
                    Level3State = NormalizeText(state.Level3State, "Unknown")
                });
            }

            unresolved.Sort(delegate (UnresolvedCandidateRecord left, UnresolvedCandidateRecord right)
            {
                int ageCompare = right.AgeBars.CompareTo(left.AgeBars);
                if (ageCompare != 0)
                    return ageCompare;

                return left.CreationBar.CompareTo(right.CreationBar);
            });

            return unresolved;
        }

        private string ClassifyUnresolvedReason(RawCandidateState state, int ageBars)
        {
            string reason = state != null ? NormalizeText(state.ResolutionReason, "") : "";
            string basis = state != null ? NormalizeText(state.Basis, "") : "";
            string current = state != null ? NormalizeText(state.CurrentState, "") : "";
            string context = reason + " " + basis + " " + current + " " + (state != null ? NormalizeText(state.ContextSignature, "") : "");

            if (ContainsIgnoreCase(context, "P3 candidate is missing") || ContainsIgnoreCase(context, "no P3"))
                return "NoP3";

            if (ContainsIgnoreCase(context, "FTT candidate") || ContainsIgnoreCase(context, "no FTT") || ContainsIgnoreCase(context, "FTT left Unknown"))
                return "NoFTT";

            if (ContainsIgnoreCase(context, "same-bar") || ContainsIgnoreCase(context, "suppressed") || ContainsIgnoreCase(context, "missing"))
                return "MissingPrerequisite";

            if (ContainsIgnoreCase(context, "lateral") || ContainsIgnoreCase(context, "LeftToRight") || ContainsIgnoreCase(context, "Mixed"))
                return "LateralPersistence";

            if (ContainsIgnoreCase(context, "invalid") || ContainsIgnoreCase(context, "context invalid"))
                return "ContextInvalidated";

            if (ContainsIgnoreCase(context, "replacement") || ContainsIgnoreCase(context, "supersed"))
                return "ReplacementPending";

            if (ageBars > CandidateExpirationThresholdBars)
                return "CandidateAgeExceeded";

            if (ageBars <= 5)
                return "RecentNoEvidence";

            if (ContainsIgnoreCase(context, "observation") || ContainsIgnoreCase(context, "waiting") || ContainsIgnoreCase(context, "candidate"))
                return "WaitingForConfirmation";

            return "Unknown";
        }

        private List<UnresolvedCandidateRecord> FilterUnresolvedByType(List<UnresolvedCandidateRecord> unresolved, string candidateType)
        {
            List<UnresolvedCandidateRecord> filtered = new List<UnresolvedCandidateRecord>();
            if (unresolved == null)
                return filtered;

            for (int i = 0; i < unresolved.Count; i++)
            {
                if (unresolved[i] != null && string.Equals(unresolved[i].CandidateType, candidateType, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(unresolved[i]);
            }

            return filtered;
        }

        private int CountUnresolvedByType(List<UnresolvedCandidateRecord> unresolved, string candidateType)
        {
            int count = 0;
            if (unresolved == null)
                return count;

            for (int i = 0; i < unresolved.Count; i++)
            {
                if (unresolved[i] != null && string.Equals(unresolved[i].CandidateType, candidateType, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        private List<UnresolvedGroupStats> BuildUnresolvedGroups(List<UnresolvedCandidateRecord> unresolved, string groupBy)
        {
            Dictionary<string, UnresolvedGroupStats> byKey = new Dictionary<string, UnresolvedGroupStats>();
            if (unresolved == null)
                return new List<UnresolvedGroupStats>();

            for (int i = 0; i < unresolved.Count; i++)
            {
                UnresolvedCandidateRecord record = unresolved[i];
                if (record == null)
                    continue;

                string key = record.ResolutionReason;
                if (string.Equals(groupBy, "Basis", StringComparison.OrdinalIgnoreCase))
                    key = record.Basis;
                else if (string.Equals(groupBy, "Context", StringComparison.OrdinalIgnoreCase))
                    key = record.ContextSignature;

                key = NormalizeText(key, "Unknown");
                UnresolvedGroupStats stat;
                if (!byKey.TryGetValue(key, out stat))
                {
                    stat = new UnresolvedGroupStats { Key = key };
                    byKey[key] = stat;
                }

                stat.Count++;
                stat.TotalAgeBars += record.AgeBars;
                stat.MaxAgeBars = Math.Max(stat.MaxAgeBars, record.AgeBars);
                stat.Ages.Add(record.AgeBars);
            }

            List<UnresolvedGroupStats> groups = new List<UnresolvedGroupStats>(byKey.Values);
            groups.Sort(delegate (UnresolvedGroupStats left, UnresolvedGroupStats right)
            {
                int countCompare = right.Count.CompareTo(left.Count);
                if (countCompare != 0)
                    return countCompare;

                return string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase);
            });

            return groups;
        }

        private void AppendUnresolvedCandidateDetailTable(StringBuilder sb, List<UnresolvedCandidateRecord> unresolved)
        {
            sb.AppendLine("<h3>UNRESOLVED CANDIDATE DETAIL</h3>");
            sb.AppendLine("<table><thead><tr><th>CandidateType</th><th>Basis</th><th>ContextSignature</th><th>CreationBar</th><th>LastTouchedBar</th><th>AgeBars</th><th>CurrentState</th><th>ResolutionState</th><th>ResolutionReason</th><th>Narrative</th><th>Expectation</th><th>ContainerLevel</th><th>Level3State</th></tr></thead><tbody>");

            if (unresolved == null || unresolved.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"13\">No unresolved candidates recorded.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            for (int i = 0; i < unresolved.Count; i++)
            {
                UnresolvedCandidateRecord record = unresolved[i];
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append(HtmlEncode(record.CandidateType)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.Basis)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.ContextSignature)).AppendLine("</td>");
                sb.Append("<td>").Append(record.CreationBar.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(record.LastTouchedBar.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(record.AgeBars.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.CurrentState)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.ResolutionState)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.ResolutionReason)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.Narrative)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.Expectation)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.ContainerLevel)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(record.Level3State)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendUnresolvedGroupTable(StringBuilder sb, string title, List<UnresolvedGroupStats> groups, int total)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>ResolutionReason</th><th>Count</th><th>Percent</th><th>AverageAgeBars</th><th>MedianAgeBars</th><th>MaxAgeBars</th></tr></thead><tbody>");

            if (groups == null || groups.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"6\">No unresolved candidates recorded.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                UnresolvedGroupStats stat = groups[i];
                double avg = stat.Count > 0 ? (double)stat.TotalAgeBars / (double)stat.Count : 0.0;
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append(HtmlEncode(stat.Key)).AppendLine("</td>");
                sb.Append("<td>").Append(stat.Count.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(FormatPercent(total > 0 ? (double)stat.Count / (double)total : 0.0)).AppendLine("</td>");
                sb.Append("<td>").Append(avg.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(MedianAge(stat.Ages).ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(stat.MaxAgeBars.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private List<ContributionStats> BuildContributionStats(Dictionary<string, RawCandidateState> allStates, List<UnresolvedCandidateRecord> unresolved, bool byBasis)
        {
            Dictionary<string, ContributionStats> stats = new Dictionary<string, ContributionStats>();
            if (allStates != null)
            {
                foreach (RawCandidateState state in allStates.Values)
                {
                    if (state == null)
                        continue;

                    string key = byBasis ? NormalizeText(state.Basis, "Unavailable") : NormalizeText(state.ContextSignature, "Unknown");
                    ContributionStats stat = GetContributionStats(stats, key);
                    stat.TotalCount++;
                    if (state.Outcome == "Rejected")
                        stat.RejectedCount++;
                    if (IsResolvedCandidateOutcome(state.Outcome))
                        stat.ResolvedCount++;
                }
            }

            if (unresolved != null)
            {
                for (int i = 0; i < unresolved.Count; i++)
                {
                    UnresolvedCandidateRecord record = unresolved[i];
                    if (record == null)
                        continue;

                    string key = byBasis ? NormalizeText(record.Basis, "Unavailable") : NormalizeText(record.ContextSignature, "Unknown");
                    ContributionStats stat = GetContributionStats(stats, key);
                    stat.UnresolvedCount++;
                    stat.TotalUnresolvedAgeBars += record.AgeBars;
                }
            }

            List<ContributionStats> list = new List<ContributionStats>(stats.Values);
            list.Sort(delegate (ContributionStats left, ContributionStats right)
            {
                int countCompare = right.UnresolvedCount.CompareTo(left.UnresolvedCount);
                if (countCompare != 0)
                    return countCompare;

                return string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private ContributionStats GetContributionStats(Dictionary<string, ContributionStats> stats, string key)
        {
            ContributionStats stat;
            if (!stats.TryGetValue(key, out stat))
            {
                stat = new ContributionStats { Key = key };
                stats[key] = stat;
            }

            return stat;
        }

        private void AppendContributionTable(StringBuilder sb, string title, List<ContributionStats> stats, int unresolvedTotal)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Value</th><th>Count</th><th>Percent</th><th>AvgAge</th><th>ResolvedRate</th><th>RejectedRate</th><th>UnresolvedRate</th></tr></thead><tbody>");

            if (stats == null || stats.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"7\">No unresolved contribution data.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            int limit = Math.Min(50, stats.Count);
            for (int i = 0; i < limit; i++)
            {
                ContributionStats stat = stats[i];
                if (stat.UnresolvedCount <= 0)
                    continue;

                double avgAge = stat.UnresolvedCount > 0 ? (double)stat.TotalUnresolvedAgeBars / (double)stat.UnresolvedCount : 0.0;
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append(HtmlEncode(stat.Key)).AppendLine("</td>");
                sb.Append("<td>").Append(stat.UnresolvedCount.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(FormatPercent(unresolvedTotal > 0 ? (double)stat.UnresolvedCount / (double)unresolvedTotal : 0.0)).AppendLine("</td>");
                sb.Append("<td>").Append(avgAge.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(FormatPercent(stat.TotalCount > 0 ? (double)stat.ResolvedCount / (double)stat.TotalCount : 0.0)).AppendLine("</td>");
                sb.Append("<td>").Append(FormatPercent(stat.TotalCount > 0 ? (double)stat.RejectedCount / (double)stat.TotalCount : 0.0)).AppendLine("</td>");
                sb.Append("<td>").Append(FormatPercent(stat.TotalCount > 0 ? (double)stat.UnresolvedCount / (double)stat.TotalCount : 0.0)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendLongestLivedCandidatesTable(StringBuilder sb, List<UnresolvedCandidateRecord> unresolved)
        {
            sb.AppendLine("<h3>SECTION 6 - LONGEST LIVED CANDIDATES</h3>");
            sb.AppendLine("<table><thead><tr><th>CandidateId</th><th>CandidateType</th><th>Basis</th><th>ContextSignature</th><th>AgeBars</th><th>CreationBar</th><th>LastTouchedBar</th><th>Narrative</th><th>Expectation</th><th>CurrentState</th><th>ResolutionReason</th></tr></thead><tbody>");

            if (unresolved == null || unresolved.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"11\">No unresolved candidates recorded.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            int limit = Math.Min(100, unresolved.Count);
            for (int i = 0; i < limit; i++)
                AppendLongestLivedCandidateRow(sb, unresolved[i]);

            sb.AppendLine("</tbody></table>");
        }

        private void AppendLongestLivedCandidateRow(StringBuilder sb, UnresolvedCandidateRecord record)
        {
            sb.AppendLine("<tr>");
            sb.Append("<td>").Append(HtmlEncode(record.CandidateId)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.CandidateType)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.Basis)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.ContextSignature)).AppendLine("</td>");
            sb.Append("<td>").Append(record.AgeBars.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(record.CreationBar.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(record.LastTouchedBar.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.Narrative)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.Expectation)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.CurrentState)).AppendLine("</td>");
            sb.Append("<td>").Append(HtmlEncode(record.ResolutionReason)).AppendLine("</td>");
            sb.AppendLine("</tr>");
        }

        private void AppendAgeDistributionTable(StringBuilder sb, List<UnresolvedCandidateRecord> unresolved)
        {
            List<AgeDistribution> buckets = BuildAgeDistribution(unresolved);
            sb.AppendLine("<h3>SECTION 7 - AGE DISTRIBUTION</h3>");
            sb.AppendLine("<table><thead><tr><th>Bucket</th><th>All unresolved</th><th>P3 unresolved</th><th>FTT unresolved</th></tr></thead><tbody>");

            for (int i = 0; i < buckets.Count; i++)
            {
                AgeDistribution bucket = buckets[i];
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append(HtmlEncode(bucket.Label)).AppendLine("</td>");
                sb.Append("<td>").Append(bucket.AllCount.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(bucket.P3Count.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(bucket.FTTCount.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private List<AgeDistribution> BuildAgeDistribution(List<UnresolvedCandidateRecord> unresolved)
        {
            List<AgeDistribution> buckets = new List<AgeDistribution>
            {
                new AgeDistribution { Label = "0-5 bars" },
                new AgeDistribution { Label = "6-10 bars" },
                new AgeDistribution { Label = "11-20 bars" },
                new AgeDistribution { Label = "21-40 bars" },
                new AgeDistribution { Label = "41-80 bars" },
                new AgeDistribution { Label = "81-160 bars" },
                new AgeDistribution { Label = "160+ bars" }
            };

            if (unresolved == null)
                return buckets;

            for (int i = 0; i < unresolved.Count; i++)
            {
                UnresolvedCandidateRecord record = unresolved[i];
                int index = AgeBucketIndex(record.AgeBars);
                buckets[index].AllCount++;
                if (string.Equals(record.CandidateType, "P3", StringComparison.OrdinalIgnoreCase))
                    buckets[index].P3Count++;
                else if (string.Equals(record.CandidateType, "FTT", StringComparison.OrdinalIgnoreCase))
                    buckets[index].FTTCount++;
            }

            return buckets;
        }

        private int AgeBucketIndex(int ageBars)
        {
            if (ageBars <= 5)
                return 0;
            if (ageBars <= 10)
                return 1;
            if (ageBars <= 20)
                return 2;
            if (ageBars <= 40)
                return 3;
            if (ageBars <= 80)
                return 4;
            if (ageBars <= 160)
                return 5;
            return 6;
        }

        private void AppendExpiredButNeverClosedSection(StringBuilder sb, List<UnresolvedCandidateRecord> unresolved)
        {
            List<UnresolvedCandidateRecord> expiredOpen = new List<UnresolvedCandidateRecord>();
            int totalAge = 0;
            int maxAge = 0;
            if (unresolved != null)
            {
                for (int i = 0; i < unresolved.Count; i++)
                {
                    UnresolvedCandidateRecord record = unresolved[i];
                    if (record != null && record.AgeBars > CandidateExpirationThresholdBars)
                    {
                        expiredOpen.Add(record);
                        totalAge += record.AgeBars;
                        maxAge = Math.Max(maxAge, record.AgeBars);
                    }
                }
            }

            sb.AppendLine("<h3>SECTION 8 - EXPIRED BUT NEVER CLOSED</h3>");
            sb.AppendLine("<table><tbody>");
            AppendAuditTotalRow(sb, "Count", expiredOpen.Count);
            sb.Append("<tr><th>Percent</th><td>").Append(FormatPercent(unresolved != null && unresolved.Count > 0 ? (double)expiredOpen.Count / (double)unresolved.Count : 0.0)).AppendLine("</td></tr>");
            sb.Append("<tr><th>AverageAge</th><td>").Append(expiredOpen.Count > 0 ? ((double)totalAge / (double)expiredOpen.Count).ToString("F1", CultureInfo.InvariantCulture) : "0.0").AppendLine("</td></tr>");
            AppendAuditTotalRow(sb, "MaximumAge", maxAge);
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<table><thead><tr><th>CandidateId</th><th>CandidateType</th><th>Basis</th><th>ContextSignature</th><th>AgeBars</th><th>CreationBar</th><th>LastTouchedBar</th><th>Narrative</th><th>Expectation</th><th>CurrentState</th><th>ResolutionReason</th></tr></thead><tbody>");
            if (expiredOpen.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"11\">No unresolved candidates exceed the configured expiration threshold.</td></tr>");
            }
            else
            {
                int limit = Math.Min(100, expiredOpen.Count);
                for (int i = 0; i < limit; i++)
                    AppendLongestLivedCandidateRow(sb, expiredOpen[i]);
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendUnresolvedSummarySection(StringBuilder sb, AnalyticsTotals totals, List<UnresolvedCandidateRecord> unresolved, Dictionary<string, RawCandidateState> allStates)
        {
            int totalCandidates = totals.Created;
            int resolvedCandidates = totals.Confirmed + totals.Rejected + totals.Invalidated + totals.Superseded;
            double upr = totalCandidates > 0 ? (double)totals.Unresolved / (double)totalCandidates : 0.0;

            sb.AppendLine("<h3>SECTION 9 - SUMMARY</h3>");
            sb.AppendLine("<table><tbody>");
            AppendAuditTotalRow(sb, "TotalCandidates", totalCandidates);
            AppendAuditTotalRow(sb, "ResolvedCandidates", resolvedCandidates);
            AppendAuditTotalRow(sb, "RejectedCandidates", totals.Rejected);
            AppendAuditTotalRow(sb, "InvalidatedCandidates", totals.Invalidated);
            AppendAuditTotalRow(sb, "SupersededCandidates", totals.Superseded);
            AppendAuditTotalRow(sb, "ExpiredCandidates", totals.Expired);
            AppendAuditTotalRow(sb, "UnresolvedCandidates", totals.Unresolved);
            sb.Append("<tr><th>UPR</th><td>").Append(FormatPercent(upr)).AppendLine("</td></tr>");
            sb.AppendLine("</tbody></table>");

            AppendTopList(sb, "Top 10 ResolutionReasons causing unresolved outcomes", TopUnresolvedKeys(unresolved, "Reason", 10));
            AppendTopList(sb, "Top 10 Basis values causing unresolved outcomes", TopUnresolvedKeys(unresolved, "Basis", 10));
            AppendTopList(sb, "Top 10 ContextSignatures causing unresolved outcomes", TopUnresolvedKeys(unresolved, "Context", 10));
            AppendConclusionRanking(sb, unresolved, allStates, totals);
        }

        private AnalyticsTotals BuildRootCauseTotals(Dictionary<string, RawCandidateState> allStates)
        {
            AnalyticsTotals totals = new AnalyticsTotals();
            if (allStates == null)
                return totals;

            foreach (RawCandidateState state in allStates.Values)
            {
                if (state == null)
                    continue;

                totals.Created++;
                if (state.Outcome == "Confirmed")
                    totals.Confirmed++;
                else if (state.Outcome == "Rejected")
                    totals.Rejected++;
                else if (state.Outcome == "Superseded")
                    totals.Superseded++;
                else if (state.Outcome == "Expired")
                    totals.Expired++;
                else if (state.Outcome == "Invalidated")
                    totals.Invalidated++;
                else
                    totals.Unresolved++;
            }

            return totals;
        }

        private List<UnresolvedGroupStats> TopUnresolvedKeys(List<UnresolvedCandidateRecord> unresolved, string groupBy, int limit)
        {
            List<UnresolvedGroupStats> groups = BuildUnresolvedGroups(unresolved, groupBy);
            if (groups.Count <= limit)
                return groups;

            return groups.GetRange(0, limit);
        }

        private void AppendTopList(StringBuilder sb, string title, List<UnresolvedGroupStats> groups)
        {
            sb.Append("<h4>").Append(HtmlEncode(title)).AppendLine("</h4>");
            sb.AppendLine("<table><thead><tr><th>Value</th><th>Count</th></tr></thead><tbody>");
            if (groups == null || groups.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"2\">No unresolved candidates.</td></tr>");
            }
            else
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    sb.Append("<tr><td>").Append(HtmlEncode(groups[i].Key)).Append("</td><td>")
                        .Append(groups[i].Count.ToString(CultureInfo.InvariantCulture)).AppendLine("</td></tr>");
                }
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendConclusionRanking(StringBuilder sb, List<UnresolvedCandidateRecord> unresolved, Dictionary<string, RawCandidateState> allStates, AnalyticsTotals totals)
        {
            List<ConclusionScore> scores = BuildConclusionScores(unresolved, allStates, totals);
            sb.AppendLine("<h4>Final Conclusion Ranking</h4>");
            sb.AppendLine("<table><thead><tr><th>Rank</th><th>Conclusion</th><th>Score</th><th>Reason</th></tr></thead><tbody>");

            for (int i = 0; i < scores.Count; i++)
            {
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append((i + 1).ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(scores[i].Label)).AppendLine("</td>");
                sb.Append("<td>").Append(scores[i].Score.ToString("F2", CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(HtmlEncode(scores[i].Reason)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private List<ConclusionScore> BuildConclusionScores(List<UnresolvedCandidateRecord> unresolved, Dictionary<string, RawCandidateState> allStates, AnalyticsTotals totals)
        {
            int unresolvedCount = unresolved != null ? unresolved.Count : 0;
            int totalCandidates = totals != null ? totals.Created : 0;
            double unresolvedRate = totalCandidates > 0 ? (double)unresolvedCount / (double)totalCandidates : 0.0;
            double avgAge = AverageUnresolvedAge(unresolved);
            int expiredOpenCount = CountExpiredOpen(unresolved);
            int uniqueContextCount = CountUniqueUnresolvedContexts(unresolved);
            double contextFragmentation = unresolvedCount > 0 ? (double)uniqueContextCount / (double)unresolvedCount : 0.0;
            int waitingCount = CountReason(unresolved, "WaitingForConfirmation") + CountReason(unresolved, "NoFTT") + CountReason(unresolved, "NoP3");

            List<ConclusionScore> scores = new List<ConclusionScore>();
            scores.Add(new ConclusionScore
            {
                Label = "A) Over-generation problem",
                Score = unresolvedRate * 2.0 + (unresolvedCount > 50 ? 0.5 : 0.0),
                Reason = "Unresolved population rate and absolute count indicate whether candidates are being created faster than they resolve."
            });
            scores.Add(new ConclusionScore
            {
                Label = "B) Expiration problem",
                Score = unresolvedCount > 0 ? (double)expiredOpenCount / (double)unresolvedCount * 3.0 : 0.0,
                Reason = "Measures unresolved candidates older than the configured expiration threshold."
            });
            scores.Add(new ConclusionScore
            {
                Label = "C) Context fragmentation problem",
                Score = contextFragmentation * 1.5,
                Reason = "High unique-context density means unresolved candidates are spread across many signatures."
            });
            scores.Add(new ConclusionScore
            {
                Label = "D) Missing confirmation logic problem",
                Score = unresolvedCount > 0 ? ((double)waitingCount / (double)unresolvedCount) + (avgAge > CandidateExpirationThresholdBars ? 0.75 : 0.0) : 0.0,
                Reason = "Long-lived waiting, NoP3, or NoFTT outcomes suggest candidate creation has more evidence than closure."
            });
            scores.Add(new ConclusionScore
            {
                Label = "E) Healthy unresolved population",
                Score = (1.0 - Math.Min(1.0, unresolvedRate)) + (avgAge <= CandidateExpirationThresholdBars ? 0.5 : 0.0),
                Reason = "Scores higher when unresolved rate is low and unresolved candidates are recent."
            });

            scores.Sort(delegate (ConclusionScore left, ConclusionScore right)
            {
                int scoreCompare = right.Score.CompareTo(left.Score);
                if (scoreCompare != 0)
                    return scoreCompare;

                return string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
            });

            return scores;
        }

        private double AverageUnresolvedAge(List<UnresolvedCandidateRecord> unresolved)
        {
            if (unresolved == null || unresolved.Count == 0)
                return 0.0;

            int total = 0;
            for (int i = 0; i < unresolved.Count; i++)
                total += unresolved[i].AgeBars;

            return (double)total / (double)unresolved.Count;
        }

        private int CountExpiredOpen(List<UnresolvedCandidateRecord> unresolved)
        {
            int count = 0;
            if (unresolved == null)
                return count;

            for (int i = 0; i < unresolved.Count; i++)
            {
                if (unresolved[i].AgeBars > CandidateExpirationThresholdBars)
                    count++;
            }

            return count;
        }

        private int CountUniqueUnresolvedContexts(List<UnresolvedCandidateRecord> unresolved)
        {
            HashSet<string> contexts = new HashSet<string>();
            if (unresolved == null)
                return 0;

            for (int i = 0; i < unresolved.Count; i++)
                contexts.Add(NormalizeText(unresolved[i].ContextSignature, "Unknown"));

            return contexts.Count;
        }

        private int CountReason(List<UnresolvedCandidateRecord> unresolved, string reason)
        {
            int count = 0;
            if (unresolved == null)
                return count;

            for (int i = 0; i < unresolved.Count; i++)
            {
                if (string.Equals(unresolved[i].ResolutionReason, reason, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        private bool IsResolvedCandidateOutcome(string outcome)
        {
            return outcome == "Confirmed"
                || outcome == "Rejected"
                || outcome == "Invalidated"
                || outcome == "Superseded";
        }

        private bool IsClosedCandidateOutcome(string outcome)
        {
            return IsResolvedCandidateOutcome(outcome) || outcome == "Expired";
        }

        private string CandidateIdFor(RawCandidateState state)
        {
            if (state == null)
                return "Unknown";

            return NormalizeText(state.AnchorType, "Unknown")
                + ":" + NormalizeText(state.NodeId, "Unknown")
                + ":" + state.CandidateBar.ToString(CultureInfo.InvariantCulture);
        }

        private string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private bool ContainsIgnoreCase(string text, string value)
        {
            return !string.IsNullOrEmpty(text)
                && !string.IsNullOrEmpty(value)
                && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private double MedianAge(List<int> ages)
        {
            if (ages == null || ages.Count == 0)
                return 0.0;

            List<int> sorted = new List<int>(ages);
            sorted.Sort();
            int mid = sorted.Count / 2;
            if ((sorted.Count % 2) == 1)
                return sorted[mid];

            return ((double)sorted[mid - 1] + (double)sorted[mid]) / 2.0;
        }

        private bool HasOutcomeMismatch(AnalyticsTotals actual, AnalyticsTotals expected)
        {
            return actual.Confirmed != expected.Confirmed
                || actual.Rejected != expected.Rejected
                || actual.Superseded != expected.Superseded
                || actual.Expired != expected.Expired
                || actual.Invalidated != expected.Invalidated
                || actual.Unresolved != expected.Unresolved;
        }

        private bool HasAnyMismatch(AnalyticsTotals left, AnalyticsTotals right)
        {
            return left.Created != right.Created || HasOutcomeMismatch(left, right);
        }

        private bool HasInternalCountFailure(AnalyticsTotals totals)
        {
            if (totals == null)
                return false;

            int outcomeTotal = totals.Confirmed + totals.Rejected + totals.Superseded + totals.Expired + totals.Invalidated + totals.Unresolved;
            return outcomeTotal > totals.Created;
        }

        private string IntegrityStatus(bool failed, params bool[] mismatches)
        {
            if (failed)
                return "Failed";

            bool hasMismatch = false;
            for (int i = 0; i < mismatches.Length; i++)
            {
                if (mismatches[i])
                {
                    hasMismatch = true;
                    break;
                }
            }

            return hasMismatch ? "Warning" : "Clean";
        }

        private void AppendRawCandidateLedgerSummary(
            StringBuilder sb,
            AnalyticsTotals rawP3Totals,
            AnalyticsTotals rawFTTTotals,
            bool p3RawBasisMismatch,
            bool p3RawContextMismatch,
            bool fttRawBasisMismatch,
            bool fttRawContextMismatch)
        {
            sb.AppendLine("<h2>RAW CANDIDATE EVENT LEDGER SUMMARY</h2>");
            sb.AppendLine("<table><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>");
            AppendAuditTotalRow(sb, "RawP3CreatedTotal", rawP3Totals.Created);
            AppendAuditTotalRow(sb, "RawP3ConfirmedTotal", rawP3Totals.Confirmed);
            AppendAuditTotalRow(sb, "RawP3RejectedTotal", rawP3Totals.Rejected);
            AppendAuditTotalRow(sb, "RawP3SupersededTotal", rawP3Totals.Superseded);
            AppendAuditTotalRow(sb, "RawP3ExpiredTotal", rawP3Totals.Expired);
            AppendAuditTotalRow(sb, "RawP3UnresolvedTotal", rawP3Totals.Unresolved);
            AppendAuditTotalRow(sb, "RawFTTCreatedTotal", rawFTTTotals.Created);
            AppendAuditTotalRow(sb, "RawFTTConfirmedTotal", rawFTTTotals.Confirmed);
            AppendAuditTotalRow(sb, "RawFTTRejectedTotal", rawFTTTotals.Rejected);
            AppendAuditTotalRow(sb, "RawFTTSupersededTotal", rawFTTTotals.Superseded);
            AppendAuditTotalRow(sb, "RawFTTExpiredTotal", rawFTTTotals.Expired);
            AppendAuditTotalRow(sb, "RawFTTUnresolvedTotal", rawFTTTotals.Unresolved);
            AppendAuditFlagRow(sb, "P3RawBasisMismatch", p3RawBasisMismatch);
            AppendAuditFlagRow(sb, "P3RawContextMismatch", p3RawContextMismatch);
            AppendAuditFlagRow(sb, "FTTRawBasisMismatch", fttRawBasisMismatch);
            AppendAuditFlagRow(sb, "FTTRawContextMismatch", fttRawContextMismatch);
            sb.AppendLine("</tbody></table>");
        }

        private void AppendAuditTotalsTable(StringBuilder sb, string title, AnalyticsTotals totals)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><tbody>");
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Candidates Created" : "Total FTT Candidates Created", totals.Created);
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Confirmed" : "Total FTT Confirmed", totals.Confirmed);
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Rejected" : "Total FTT Rejected", totals.Rejected);
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Superseded" : "Total FTT Superseded", totals.Superseded);
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Expired" : "Total FTT Expired", totals.Expired);
            AppendAuditTotalRow(sb, title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0 ? "Total P3 Unresolved" : "Total FTT Unresolved", totals.Unresolved);
            sb.AppendLine("</tbody></table>");
        }

        private void AppendAuditTotalRow(StringBuilder sb, string label, int value)
        {
            sb.Append("<tr><th>").Append(HtmlEncode(label)).Append("</th><td>")
                .Append(value.ToString(CultureInfo.InvariantCulture)).AppendLine("</td></tr>");
        }

        private void AppendAuditComparisonTable(StringBuilder sb, string title, AnalyticsTotals basis, AnalyticsTotals context, AnalyticsTotals audit)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Metric</th><th>Basis Sum</th><th>Context Signature Sum</th><th>Audit Total</th><th>Basis Matches</th><th>Context Matches</th></tr></thead><tbody>");
            AppendAuditComparisonRow(sb, "Created", basis.Created, context.Created, audit.Created);
            AppendAuditComparisonRow(sb, "Confirmed", basis.Confirmed, context.Confirmed, audit.Confirmed);
            AppendAuditComparisonRow(sb, "Rejected", basis.Rejected, context.Rejected, audit.Rejected);
            AppendAuditComparisonRow(sb, "Superseded", basis.Superseded, context.Superseded, audit.Superseded);
            AppendAuditComparisonRow(sb, "Expired", basis.Expired, context.Expired, audit.Expired);
            AppendAuditComparisonRow(sb, "Unresolved", basis.Unresolved, context.Unresolved, audit.Unresolved);
            sb.AppendLine("</tbody></table>");
        }

        private void AppendAuditComparisonRow(StringBuilder sb, string metric, int basisValue, int contextValue, int auditValue)
        {
            sb.AppendLine("<tr>");
            sb.Append("<td>").Append(HtmlEncode(metric)).AppendLine("</td>");
            sb.Append("<td>").Append(basisValue.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(contextValue.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append(auditValue.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
            sb.Append("<td>").Append((basisValue == auditValue).ToString()).AppendLine("</td>");
            sb.Append("<td>").Append((contextValue == auditValue).ToString()).AppendLine("</td>");
            sb.AppendLine("</tr>");
        }

        private void AppendAuditFlagsTable(
            StringBuilder sb,
            string title,
            bool basisCreatedMismatch,
            bool contextCreatedMismatch,
            bool basisOutcomeMismatch,
            bool contextOutcomeMismatch,
            bool rawBasisMismatch,
            bool rawContextMismatch)
        {
            bool isP3 = title.IndexOf("P3", StringComparison.OrdinalIgnoreCase) >= 0;
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><tbody>");
            AppendAuditFlagRow(sb, isP3 ? "P3BasisCreatedMismatch" : "FTTBasisCreatedMismatch", basisCreatedMismatch);
            AppendAuditFlagRow(sb, isP3 ? "P3ContextCreatedMismatch" : "FTTContextCreatedMismatch", contextCreatedMismatch);
            AppendAuditFlagRow(sb, isP3 ? "P3BasisOutcomeMismatch" : "FTTBasisOutcomeMismatch", basisOutcomeMismatch);
            AppendAuditFlagRow(sb, isP3 ? "P3ContextOutcomeMismatch" : "FTTContextOutcomeMismatch", contextOutcomeMismatch);
            AppendAuditFlagRow(sb, isP3 ? "P3RawBasisMismatch" : "FTTRawBasisMismatch", rawBasisMismatch);
            AppendAuditFlagRow(sb, isP3 ? "P3RawContextMismatch" : "FTTRawContextMismatch", rawContextMismatch);
            sb.AppendLine("</tbody></table>");
        }

        private void AppendAuditFlagRow(StringBuilder sb, string label, bool value)
        {
            sb.Append("<tr><th>").Append(HtmlEncode(label)).Append("</th><td>")
                .Append(value.ToString()).AppendLine("</td></tr>");
        }

        private void AppendAnchorBasisStatsTable(StringBuilder sb, string title, List<xPvaAnchorBasisStatistic> stats)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Basis</th><th>Created</th><th>Confirmed</th><th>Rejected</th><th>Superseded</th><th>Expired</th><th>Unresolved</th><th>Confirmation %</th><th>Rejection %</th><th>Superseded %</th><th>Expired %</th><th>Unresolved %</th><th>Avg Bars To Resolution</th><th>Avg Bars To Confirmation</th><th>Avg Bars To Rejection</th><th>Low Confidence</th><th>Medium Confidence</th><th>High Confidence</th></tr></thead><tbody>");

            if (stats == null || stats.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"18\">No candidates recorded.</td></tr>");
            }
            else
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    xPvaAnchorBasisStatistic stat = stats[i];
                    sb.AppendLine("<tr>");
                    sb.Append("<td>").Append(HtmlEncode(stat.Basis)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Created.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Rejected.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Superseded.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Expired.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Unresolved.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td class=\"rate\">").Append(FormatPercent(stat.ConfirmationRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(FormatPercent(stat.RejectionRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(FormatPercent(stat.SupersessionRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(FormatPercent(stat.ExpirationRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(FormatPercent(stat.UnresolvedRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.AvgBarsToResolution.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.AvgBarsToConfirmation.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.AvgBarsToRejection.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.LowConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.MediumConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.HighConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendTopAnchorBasisStatsTable(StringBuilder sb, string title, List<xPvaAnchorBasisStatistic> stats)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Rank</th><th>Basis</th><th>Created</th><th>Confirmed</th><th>Confirmation %</th><th>Avg Bars To Confirmation</th><th>Low Confidence</th><th>Medium Confidence</th><th>High Confidence</th></tr></thead><tbody>");

            List<xPvaAnchorBasisStatistic> ranked = BuildConfirmationRateRanking(stats);
            if (ranked.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"9\">No candidates recorded.</td></tr>");
            }
            else
            {
                int limit = Math.Min(10, ranked.Count);
                for (int i = 0; i < limit; i++)
                {
                    xPvaAnchorBasisStatistic stat = ranked[i];
                    sb.AppendLine("<tr>");
                    sb.Append("<td>").Append((i + 1).ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(HtmlEncode(stat.Basis)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Created.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td class=\"rate\">").Append(FormatPercent(stat.ConfirmationRate)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.AvgBarsToConfirmation.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.LowConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.MediumConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.HighConfidence.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendContextSignatureAnalyticsSection(
            StringBuilder sb,
            List<xPvaContextSignatureStatistic> p3Stats,
            List<xPvaContextSignatureStatistic> fttStats)
        {
            sb.AppendLine("<h2>CONTEXT SIGNATURE ANALYTICS</h2>");
            AppendContextSignatureStatsTable(sb, "P3 Context Signature Statistics", p3Stats);
            AppendContextSignatureStatsTable(sb, "FTT Context Signature Statistics", fttStats);
            AppendTopContextSignatureStatsTable(sb, "Top P3 Context Signatures By Confirmation Rate", p3Stats);
            AppendTopContextSignatureStatsTable(sb, "Top FTT Context Signatures By Confirmation Rate", fttStats);
        }

        private void AppendContextSignatureStatsTable(StringBuilder sb, string title, List<xPvaContextSignatureStatistic> stats)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Signature</th><th>Created</th><th>Confirmed</th><th>Rejected</th><th>Superseded</th><th>Expired</th><th>Unresolved</th><th>Confirmation %</th></tr></thead><tbody>");

            if (stats == null || stats.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"8\">No context signatures recorded.</td></tr>");
            }
            else
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    xPvaContextSignatureStatistic stat = stats[i];
                    sb.AppendLine("<tr>");
                    sb.Append("<td>").Append(HtmlEncode(stat.Signature)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Created.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Rejected.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Superseded.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Expired.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td>").Append(stat.Unresolved.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    sb.Append("<td class=\"rate\">").Append(FormatPercent(stat.ConfirmationRate)).AppendLine("</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody></table>");
        }

        private void AppendTopContextSignatureStatsTable(StringBuilder sb, string title, List<xPvaContextSignatureStatistic> stats)
        {
            sb.Append("<h3>").Append(HtmlEncode(title)).AppendLine("</h3>");
            sb.AppendLine("<table><thead><tr><th>Signature</th><th>Created</th><th>Confirmed</th><th>Confirmation %</th></tr></thead><tbody>");

            if (stats == null || stats.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"4\">No context signatures recorded with Created &gt;= 3.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            List<xPvaContextSignatureStatistic> ranked = new List<xPvaContextSignatureStatistic>();
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] != null && stats[i].Created >= 3)
                    ranked.Add(stats[i]);
            }

            if (ranked.Count == 0)
            {
                sb.AppendLine("<tr><td class=\"muted\" colspan=\"4\">No context signatures recorded with Created &gt;= 3.</td></tr>");
                sb.AppendLine("</tbody></table>");
                return;
            }

            ranked.Sort(delegate (xPvaContextSignatureStatistic left, xPvaContextSignatureStatistic right)
            {
                int rateCompare = right.ConfirmationRate.CompareTo(left.ConfirmationRate);
                if (rateCompare != 0)
                    return rateCompare;

                int confirmedCompare = right.Confirmed.CompareTo(left.Confirmed);
                if (confirmedCompare != 0)
                    return confirmedCompare;

                return right.Created.CompareTo(left.Created);
            });

            int limit = Math.Min(10, ranked.Count);
            for (int i = 0; i < limit; i++)
            {
                xPvaContextSignatureStatistic stat = ranked[i];
                sb.AppendLine("<tr>");
                sb.Append("<td>").Append(HtmlEncode(stat.Signature)).AppendLine("</td>");
                sb.Append("<td>").Append(stat.Created.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td>").Append(stat.Confirmed.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                sb.Append("<td class=\"rate\">").Append(FormatPercent(stat.ConfirmationRate)).AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        private List<xPvaAnchorBasisStatistic> BuildConfirmationRateRanking(List<xPvaAnchorBasisStatistic> stats)
        {
            List<xPvaAnchorBasisStatistic> ranked = new List<xPvaAnchorBasisStatistic>();
            if (stats == null)
                return ranked;

            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i] != null && stats[i].Created > 0)
                    ranked.Add(stats[i]);
            }

            ranked.Sort(delegate (xPvaAnchorBasisStatistic left, xPvaAnchorBasisStatistic right)
            {
                int rateCompare = right.ConfirmationRate.CompareTo(left.ConfirmationRate);
                if (rateCompare != 0)
                    return rateCompare;

                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.Basis, right.Basis, StringComparison.OrdinalIgnoreCase);
            });

            return ranked;
        }

        private string FormatPercent(double value)
        {
            return (value * 100.0).ToString("F1", CultureInfo.InvariantCulture) + "%";
        }

        private string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#39;");
        }

        private xPvaBarFacts Facts(int barsAgo)
        {
            return new xPvaBarFacts(
                CurrentBar - barsAgo,
                Time[barsAgo],
                Open[barsAgo],
                High[barsAgo],
                Low[barsAgo],
                Close[barsAgo],
                Volume[barsAgo],
                TickSize);
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaEventDebugIndicator[] cachexPvaEventDebugIndicator;
		public xPvaEventDebugIndicator xPvaEventDebugIndicator(bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			return xPvaEventDebugIndicator(Input, showEventLabels, showSequenceLabels, showDominanceLabels, printEvents, printSequences, printDominance, showBinaryLabels, printBinary, showStructureLabels, printStructure, showAmbiguityLabels, printAmbiguity, showExpectationLabels, printExpectation, showReasonLabels, printReasonChain, exportHtmlTable, htmlFilePath, printHtmlPathOnClose);
		}

		public xPvaEventDebugIndicator xPvaEventDebugIndicator(ISeries<double> input, bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			if (cachexPvaEventDebugIndicator != null)
				for (int idx = 0; idx < cachexPvaEventDebugIndicator.Length; idx++)
					if (cachexPvaEventDebugIndicator[idx] != null && cachexPvaEventDebugIndicator[idx].ShowEventLabels == showEventLabels && cachexPvaEventDebugIndicator[idx].ShowSequenceLabels == showSequenceLabels && cachexPvaEventDebugIndicator[idx].ShowDominanceLabels == showDominanceLabels && cachexPvaEventDebugIndicator[idx].PrintEvents == printEvents && cachexPvaEventDebugIndicator[idx].PrintSequences == printSequences && cachexPvaEventDebugIndicator[idx].PrintDominance == printDominance && cachexPvaEventDebugIndicator[idx].ShowBinaryLabels == showBinaryLabels && cachexPvaEventDebugIndicator[idx].PrintBinary == printBinary && cachexPvaEventDebugIndicator[idx].ShowStructureLabels == showStructureLabels && cachexPvaEventDebugIndicator[idx].PrintStructure == printStructure && cachexPvaEventDebugIndicator[idx].ShowAmbiguityLabels == showAmbiguityLabels && cachexPvaEventDebugIndicator[idx].PrintAmbiguity == printAmbiguity && cachexPvaEventDebugIndicator[idx].ShowExpectationLabels == showExpectationLabels && cachexPvaEventDebugIndicator[idx].PrintExpectation == printExpectation && cachexPvaEventDebugIndicator[idx].ShowReasonLabels == showReasonLabels && cachexPvaEventDebugIndicator[idx].PrintReasonChain == printReasonChain && cachexPvaEventDebugIndicator[idx].ExportHtmlTable == exportHtmlTable && cachexPvaEventDebugIndicator[idx].HtmlFilePath == htmlFilePath && cachexPvaEventDebugIndicator[idx].PrintHtmlPathOnClose == printHtmlPathOnClose && cachexPvaEventDebugIndicator[idx].EqualsInput(input))
						return cachexPvaEventDebugIndicator[idx];
			return CacheIndicator<xPvaEventDebugIndicator>(new xPvaEventDebugIndicator(){ ShowEventLabels = showEventLabels, ShowSequenceLabels = showSequenceLabels, ShowDominanceLabels = showDominanceLabels, PrintEvents = printEvents, PrintSequences = printSequences, PrintDominance = printDominance, ShowBinaryLabels = showBinaryLabels, PrintBinary = printBinary, ShowStructureLabels = showStructureLabels, PrintStructure = printStructure, ShowAmbiguityLabels = showAmbiguityLabels, PrintAmbiguity = printAmbiguity, ShowExpectationLabels = showExpectationLabels, PrintExpectation = printExpectation, ShowReasonLabels = showReasonLabels, PrintReasonChain = printReasonChain, ExportHtmlTable = exportHtmlTable, HtmlFilePath = htmlFilePath, PrintHtmlPathOnClose = printHtmlPathOnClose }, input, ref cachexPvaEventDebugIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaEventDebugIndicator xPvaEventDebugIndicator(bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			return indicator.xPvaEventDebugIndicator(Input, showEventLabels, showSequenceLabels, showDominanceLabels, printEvents, printSequences, printDominance, showBinaryLabels, printBinary, showStructureLabels, printStructure, showAmbiguityLabels, printAmbiguity, showExpectationLabels, printExpectation, showReasonLabels, printReasonChain, exportHtmlTable, htmlFilePath, printHtmlPathOnClose);
		}

		public Indicators.xPvaEventDebugIndicator xPvaEventDebugIndicator(ISeries<double> input , bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			return indicator.xPvaEventDebugIndicator(input, showEventLabels, showSequenceLabels, showDominanceLabels, printEvents, printSequences, printDominance, showBinaryLabels, printBinary, showStructureLabels, printStructure, showAmbiguityLabels, printAmbiguity, showExpectationLabels, printExpectation, showReasonLabels, printReasonChain, exportHtmlTable, htmlFilePath, printHtmlPathOnClose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaEventDebugIndicator xPvaEventDebugIndicator(bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			return indicator.xPvaEventDebugIndicator(Input, showEventLabels, showSequenceLabels, showDominanceLabels, printEvents, printSequences, printDominance, showBinaryLabels, printBinary, showStructureLabels, printStructure, showAmbiguityLabels, printAmbiguity, showExpectationLabels, printExpectation, showReasonLabels, printReasonChain, exportHtmlTable, htmlFilePath, printHtmlPathOnClose);
		}

		public Indicators.xPvaEventDebugIndicator xPvaEventDebugIndicator(ISeries<double> input , bool showEventLabels, bool showSequenceLabels, bool showDominanceLabels, bool printEvents, bool printSequences, bool printDominance, bool showBinaryLabels, bool printBinary, bool showStructureLabels, bool printStructure, bool showAmbiguityLabels, bool printAmbiguity, bool showExpectationLabels, bool printExpectation, bool showReasonLabels, bool printReasonChain, bool exportHtmlTable, string htmlFilePath, bool printHtmlPathOnClose)
		{
			return indicator.xPvaEventDebugIndicator(input, showEventLabels, showSequenceLabels, showDominanceLabels, printEvents, printSequences, printDominance, showBinaryLabels, printBinary, showStructureLabels, printStructure, showAmbiguityLabels, printAmbiguity, showExpectationLabels, printExpectation, showReasonLabels, printReasonChain, exportHtmlTable, htmlFilePath, printHtmlPathOnClose);
		}
	}
}

#endregion
