#region Using declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaGaussianNodeDirection
    {
        Unknown,
        Black,
        Red,
        Lateral
    }

    public enum xPvaGaussianNodeState
    {
        Unknown,
        Building,
        Complete,
        Failed,
        Suspended,
        Ambiguous
    }

    public enum xPvaGaussianNodePhase
    {
        Unknown,
        FirstDominantLeg,
        RetraceLeg,
        FinalDominantLeg,
        InternalCycle
    }

    public enum xPvaGaussianBarRole
    {
        Unknown,
        Member,
        P1,
        P2,
        P3,
        FTT,
        Peak,
        Trough,
        InternalPeak,
        InternalTrough
    }

    public enum xPvaGaussianAmbiguity
    {
        None,
        LeftToRight,
        GeometryConflict,
        GaussianCoherentButPriceInvalid,
        InsufficientContext
    }

    public enum xPvaAnchorConfidence
    {
        Unknown,
        Low,
        Medium,
        High
    }

    public enum xPvaAnchorStatus
    {
        Unknown,
        Candidate,
        Confirmed,
        Rejected,
        Adjusted
    }

    public enum xPvaAnchorPromotionStatus
    {
        Unknown,
        Candidate,
        Confirmed,
        Rejected,
        Superseded
    }

    public enum xPvaAnchorOutcome
    {
        Unknown,
        Unresolved,
        Confirmed,
        Rejected,
        Superseded,
        Expired
    }

    public enum xPvaP2ExtremeBasis
    {
        Unavailable,
        HighestHigh,
        LowestLow,
        InferredFromLabel
    }

    public enum xPvaAnchorValidationConfidence
    {
        Unknown,
        Low,
        Medium,
        High,
        Invalid
    }

    public sealed class xPvaAnchorValidation
    {
        public bool AnchorOrderingViolation;
        public bool AnchorCollapseWarning;
        public bool MissingP2;
        public bool MissingP3;
        public bool MissingFTT;
        public bool PeakLegitimacyConflict;
        public xPvaAnchorValidationConfidence Confidence;
        public string Reason;

        public xPvaAnchorValidation Clone()
        {
            return (xPvaAnchorValidation)MemberwiseClone();
        }
    }

    public sealed class xPvaGaussianAnchorSet
    {
        public int? P1Bar;
        public int? P2Bar;
        public int? P3Bar;
        public int? FTTBar;

        public xPvaAnchorStatus P1Status;
        public xPvaAnchorStatus P2Status;
        public xPvaAnchorStatus P3Status;
        public xPvaAnchorStatus FTTStatus;

        public xPvaAnchorConfidence Confidence;
        public xPvaAnchorConfidence GeometryConfidence;

        public string Pattern;
        public string Reason;
        public string PeakLegitimacy;
        public xPvaP2ExtremeBasis P2ExtremeBasis;
        public string P2ExtremeReason;
        public int? P3CandidateBar;
        public xPvaAnchorConfidence P3CandidateConfidence;
        public string P3CandidateBasis;
        public string P3CandidateReason;
        public int? FTTCandidateBar;
        public xPvaAnchorConfidence FTTCandidateConfidence;
        public string FTTCandidateBasis;
        public string FTTCandidateReason;
        public xPvaAnchorPromotionStatus P3PromotionStatus;
        public string P3PromotionReason;
        public int? P3PromotionBar;
        public int? P3CandidateCreationBar;
        public int? P3CandidateReplacementBar;
        public int? P3SupersededByCandidateBar;
        public xPvaAnchorOutcome P3Outcome;
        public int? P3OutcomeBar;
        public int? P3BarsUntilOutcome;
        public string P3OutcomeReason;
        public string P3OutcomeRule;
        public int? P3CandidateAge;
        public xPvaAnchorPromotionStatus FTTPromotionStatus;
        public string FTTPromotionReason;
        public int? FTTPromotionBar;
        public int? FTTCandidateCreationBar;
        public int? FTTCandidateReplacementBar;
        public int? FTTSupersededByCandidateBar;
        public xPvaAnchorOutcome FTTOutcome;
        public int? FTTOutcomeBar;
        public int? FTTBarsUntilOutcome;
        public string FTTOutcomeReason;
        public bool CandidateLifecycleViolation;
        public bool SameBarAnchorWarning;
        public string SameBarAnchorReason;
        public bool P3SuppressedBySameBarRule;
        public bool FTTSuppressedBySameBarRule;
        public xPvaContextSnapshot P3CandidateSnapshot;
        public xPvaContextSnapshot P3OutcomeSnapshot;
        public xPvaContextSnapshot FTTCandidateSnapshot;
        public xPvaContextSnapshot FTTOutcomeSnapshot;
        public string P3ContextSignature;
        public string P3OutcomeContextSignature;
        public string FTTContextSignature;
        public string FTTOutcomeContextSignature;

        public bool AnchorCollapseWarning;

        public xPvaAnchorValidation Validation;

        public xPvaGaussianAnchorSet Clone()
        {
            xPvaGaussianAnchorSet clone = (xPvaGaussianAnchorSet)MemberwiseClone();
            clone.Validation = Validation != null ? Validation.Clone() : null;
            clone.P3CandidateSnapshot = P3CandidateSnapshot != null ? P3CandidateSnapshot.Clone() : null;
            clone.P3OutcomeSnapshot = P3OutcomeSnapshot != null ? P3OutcomeSnapshot.Clone() : null;
            clone.FTTCandidateSnapshot = FTTCandidateSnapshot != null ? FTTCandidateSnapshot.Clone() : null;
            clone.FTTOutcomeSnapshot = FTTOutcomeSnapshot != null ? FTTOutcomeSnapshot.Clone() : null;
            return clone;
        }
    }

    public sealed class xPvaContextSnapshot
    {
        public int Bar;

        public string Level1State;
        public string Level1Direction;
        public string Level1Role;

        public string GaussianState;
        public string GaussianDirection;
        public string GaussianPhase;

        public string NarrativeState;
        public string NarrativeDirection;

        public string ExpectationState;
        public string ExpectedDirection;

        public string ContainerLevel;
        public string ContainerDirection;
        public string ContainerState;

        public string ActiveDominance;
        public string DoneDominance;

        public string ActiveGrammar;
        public string DoneGrammar;

        public string ActiveStructure;
        public string DoneStructure;

        public string Level3State;

        public string ContextSignature;

        public xPvaContextSnapshot Clone()
        {
            return (xPvaContextSnapshot)MemberwiseClone();
        }
    }

    public sealed class xPvaAnchorBasisStatistic
    {
        public string Basis;
        public string BasisName;
        public int Created;
        public int Confirmed;
        public int Rejected;
        public int Superseded;
        public int Expired;
        public int Unresolved;
        public int TotalBarsToResolution;
        public int TotalBarsToConfirmation;
        public int TotalBarsToRejection;
        public double ConfirmationRate;
        public double RejectionRate;
        public double SupersessionRate;
        public double ExpirationRate;
        public double UnresolvedRate;
        public double AvgBarsToResolution;
        public double AvgBarsToConfirmation;
        public double AvgBarsToRejection;
        public int UnknownConfidence;
        public int LowConfidence;
        public int MediumConfidence;
        public int HighConfidence;

        public xPvaAnchorBasisStatistic Clone()
        {
            return (xPvaAnchorBasisStatistic)MemberwiseClone();
        }
    }

    public sealed class xPvaContextSignatureStatistic
    {
        public string Signature;
        public int Created;
        public int Confirmed;
        public int Rejected;
        public int Superseded;
        public int Expired;
        public int Unresolved;
        public double ConfirmationRate;

        public xPvaContextSignatureStatistic Clone()
        {
            return (xPvaContextSignatureStatistic)MemberwiseClone();
        }
    }

    public sealed class xPvaCandidateEventRecord
    {
        public int Bar;
        public string NodeId;
        public string AnchorType;
        public string EventType;
        public string Basis;
        public string ContextSignature;
        public string Confidence;
        public int? CandidateBar;
        public int? OutcomeBar;
        public int LastTouchedBar;
        public string CurrentState;
        public string ResolutionReason;
        public string Narrative;
        public string Expectation;
        public string ContainerLevel;
        public string Level3State;

        public xPvaCandidateEventRecord Clone()
        {
            return (xPvaCandidateEventRecord)MemberwiseClone();
        }
    }

    public sealed class xPvaAnchorValidator
    {
        public xPvaAnchorValidation Validate(xPvaGaussianNode node)
        {
            xPvaGaussianAnchorSet anchors = node != null ? node.Anchors : null;
            var validation = new xPvaAnchorValidation
            {
                Confidence = xPvaAnchorValidationConfidence.Unknown,
                Reason = "no anchor set"
            };

            if (anchors == null)
                return validation;

            validation.MissingP2 = !anchors.P2Bar.HasValue;
            validation.MissingP3 = !anchors.P3Bar.HasValue;
            validation.MissingFTT = !anchors.FTTBar.HasValue;
            validation.AnchorCollapseWarning = anchors.AnchorCollapseWarning;
            validation.PeakLegitimacyConflict = IsPeakLegitimacyConflict(anchors.PeakLegitimacy);
            validation.AnchorOrderingViolation = HasOrderingViolation(anchors);
            validation.Confidence = ConfidenceFor(validation, anchors);
            validation.Reason = ReasonFor(validation, anchors);
            return validation;
        }

        private bool HasOrderingViolation(xPvaGaussianAnchorSet anchors)
        {
            if (anchors.P1Bar.HasValue && anchors.P2Bar.HasValue && anchors.P2Bar.Value < anchors.P1Bar.Value)
                return true;

            if (anchors.P2Bar.HasValue && anchors.P3Bar.HasValue && anchors.P3Bar.Value < anchors.P2Bar.Value)
                return true;

            if (anchors.P3Bar.HasValue && anchors.FTTBar.HasValue && anchors.FTTBar.Value < anchors.P3Bar.Value)
                return true;

            return false;
        }

        private bool IsPeakLegitimacyConflict(string peakLegitimacy)
        {
            return !string.IsNullOrEmpty(peakLegitimacy)
                && peakLegitimacy.IndexOf("rejected:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private xPvaAnchorValidationConfidence ConfidenceFor(xPvaAnchorValidation validation, xPvaGaussianAnchorSet anchors)
        {
            if (validation.AnchorOrderingViolation || validation.PeakLegitimacyConflict)
                return xPvaAnchorValidationConfidence.Invalid;

            if (validation.AnchorCollapseWarning)
                return xPvaAnchorValidationConfidence.Low;

            if (validation.MissingP2 || validation.MissingP3)
                return xPvaAnchorValidationConfidence.Low;

            if (validation.MissingFTT)
                return xPvaAnchorValidationConfidence.Medium;

            if (anchors.P1Status == xPvaAnchorStatus.Confirmed
                && anchors.P2Status == xPvaAnchorStatus.Confirmed
                && anchors.P3Status == xPvaAnchorStatus.Confirmed
                && anchors.FTTStatus == xPvaAnchorStatus.Confirmed)
                return xPvaAnchorValidationConfidence.High;

            if (anchors.P1Bar.HasValue && anchors.P2Bar.HasValue && anchors.P3Bar.HasValue && anchors.FTTBar.HasValue)
                return xPvaAnchorValidationConfidence.Medium;

            return xPvaAnchorValidationConfidence.Low;
        }

        private string ReasonFor(xPvaAnchorValidation validation, xPvaGaussianAnchorSet anchors)
        {
            var sb = new StringBuilder();

            if (validation.AnchorOrderingViolation)
                sb.Append("anchor ordering violation; ");

            if (validation.AnchorCollapseWarning)
                sb.Append("anchor collapse: P2/P3/FTT assigned to same bar; geometry not yet proven; ");

            if (validation.MissingP2)
                sb.Append("missing P2; ");

            if (validation.MissingP3)
                sb.Append("missing P3; ");

            if (validation.MissingFTT)
                sb.Append("missing FTT; ");

            if (validation.PeakLegitimacyConflict)
                sb.Append("peak legitimacy conflict; ");

            if (sb.Length == 0)
                sb.Append("anchors ordered and present with no peak conflict; ");

            sb.Append("confidence=").Append(validation.Confidence);
            return sb.ToString();
        }
    }

    public sealed class xPvaGaussianNode
    {
        public int Id;
        public int Level;

        public int StartBar;
        public int EndBar;

        public DateTime StartTime;
        public DateTime EndTime;

        public int? P1Bar;
        public int? P2Bar;
        public int? P3Bar;
        public int? FTTBar;

        public xPvaGaussianNodeDirection Direction;
        public xPvaGaussianNodeState State;
        public xPvaGaussianNodePhase Phase;

        public int? PeakBar;
        public int? TroughBar;

        public int? ParentId;
        public List<int> ChildIds;
        public List<int> ParentCandidateNodeIds;
        public List<int> ChildCandidateNodeIds;
        public string HierarchyConfidence;
        public string HierarchyReason;

        public string Pattern;
        public double Confidence;
        public string Reason;

        public xPvaGaussianAmbiguity Ambiguity;
        public xPvaGaussianAnchorSet Anchors;

        public xPvaGaussianNode Clone()
        {
            return new xPvaGaussianNode
            {
                Id = Id,
                Level = Level,
                StartBar = StartBar,
                EndBar = EndBar,
                StartTime = StartTime,
                EndTime = EndTime,
                P1Bar = P1Bar,
                P2Bar = P2Bar,
                P3Bar = P3Bar,
                FTTBar = FTTBar,
                Direction = Direction,
                State = State,
                Phase = Phase,
                PeakBar = PeakBar,
                TroughBar = TroughBar,
                ParentId = ParentId,
                ChildIds = ChildIds != null ? new List<int>(ChildIds) : new List<int>(),
                ParentCandidateNodeIds = ParentCandidateNodeIds != null ? new List<int>(ParentCandidateNodeIds) : new List<int>(),
                ChildCandidateNodeIds = ChildCandidateNodeIds != null ? new List<int>(ChildCandidateNodeIds) : new List<int>(),
                HierarchyConfidence = HierarchyConfidence,
                HierarchyReason = HierarchyReason,
                Pattern = Pattern,
                Confidence = Confidence,
                Reason = Reason,
                Ambiguity = Ambiguity,
                Anchors = Anchors != null ? Anchors.Clone() : null
            };
        }
    }

    public sealed class xPvaGaussianBarMembership
    {
        public int Bar;
        public int GaussianNodeId;
        public int Level;

        public xPvaGaussianBarRole Role;

        public string RoleReason;

        public xPvaGaussianBarMembership Clone()
        {
            return (xPvaGaussianBarMembership)MemberwiseClone();
        }
    }

    public sealed class xPvaGaussianFractalLedgerSnapshot
    {
        public int Bar;

        public List<xPvaGaussianNode> ActiveNodes;
        public List<xPvaGaussianNode> CompletedNodes;
        public List<xPvaGaussianBarMembership> Memberships;

        public string PrimaryNodeIds;
        public string ParentNodeIds;
        public string ChildNodeIds;
        public string HierarchyParentCandidateNodeIds;
        public string HierarchyChildCandidateNodeIds;
        public string HierarchyConfidence;
        public string HierarchyReason;

        public string Reason;
    }

    public sealed class xPvaGaussianOhlcBar
    {
        public int Bar;
        public double High;
        public double Low;
    }

    /// <summary>
    /// Internal Gaussian fractal membership ledger. It does not draw, trade, or own container logic.
    /// </summary>
    public sealed class xPvaGaussianFractalLedger
    {
        private readonly List<xPvaGaussianNode> activeNodes = new List<xPvaGaussianNode>();
        private readonly List<xPvaGaussianNode> completedNodes = new List<xPvaGaussianNode>();
        private readonly List<xPvaGaussianBarMembership> memberships = new List<xPvaGaussianBarMembership>();
        private readonly Dictionary<int, xPvaGaussianOhlcBar> ohlcByBar = new Dictionary<int, xPvaGaussianOhlcBar>();
        private readonly Dictionary<string, xPvaAnchorBasisStatistic> p3BasisStats = new Dictionary<string, xPvaAnchorBasisStatistic>();
        private readonly Dictionary<string, xPvaAnchorBasisStatistic> fttBasisStats = new Dictionary<string, xPvaAnchorBasisStatistic>();
        private readonly Dictionary<string, xPvaContextSignatureStatistic> p3ContextSignatureStats = new Dictionary<string, xPvaContextSignatureStatistic>();
        private readonly Dictionary<string, xPvaContextSignatureStatistic> fttContextSignatureStats = new Dictionary<string, xPvaContextSignatureStatistic>();
        private readonly HashSet<string> countedCandidateCreations = new HashSet<string>();
        private readonly HashSet<string> countedCandidateOutcomes = new HashSet<string>();
        private readonly HashSet<string> countedContextCreations = new HashSet<string>();
        private readonly HashSet<string> countedContextOutcomes = new HashSet<string>();
        private readonly List<xPvaCandidateEventRecord> rawCandidateEvents = new List<xPvaCandidateEventRecord>();
        private readonly HashSet<string> countedRawCandidateEvents = new HashSet<string>();
        private readonly Dictionary<string, xPvaCandidateEventRecord> rawCandidateEventByKey = new Dictionary<string, xPvaCandidateEventRecord>();
        private readonly xPvaAnchorValidator anchorValidator = new xPvaAnchorValidator();
        private const int P3CandidateMaxUnresolvedBars = 12;
        private int nextNodeId = 1;

        public List<xPvaAnchorBasisStatistic> P3CandidateBasisStatistics
        {
            get { return BuildStatsSnapshot(p3BasisStats); }
        }

        public List<xPvaAnchorBasisStatistic> FTTCandidateBasisStatistics
        {
            get { return BuildStatsSnapshot(fttBasisStats); }
        }

        public List<xPvaContextSignatureStatistic> P3ContextSignatureStatistics
        {
            get { return BuildContextSignatureStatsSnapshot(p3ContextSignatureStats); }
        }

        public List<xPvaContextSignatureStatistic> FTTContextSignatureStatistics
        {
            get { return BuildContextSignatureStatsSnapshot(fttContextSignatureStats); }
        }

        public List<xPvaCandidateEventRecord> RawCandidateEventRecords
        {
            get
            {
                List<xPvaCandidateEventRecord> snapshot = new List<xPvaCandidateEventRecord>();
                for (int i = 0; i < rawCandidateEvents.Count; i++)
                    snapshot.Add(rawCandidateEvents[i].Clone());

                return snapshot;
            }
        }

        public xPvaGaussianFractalLedgerSnapshot Update(
            int bar,
            DateTime time,
            string eventText,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            xPvaStructureObservation activeStructure,
            xPvaStructureObservation completedStructure,
            double high,
            double low,
            bool hasOhlc)
        {
            string reason = "no Gaussian ledger node update";

            if (hasOhlc)
                ohlcByBar[bar] = new xPvaGaussianOhlcBar { Bar = bar, High = high, Low = low };

            if (gaussian != null && IsLedgerCandidate(gaussian))
            {
                xPvaGaussianNode node = FindOrCreateNode(gaussian, time);
                UpdateNode(node, bar, gaussian, narrative, expectation, expectationValidation, container, level3Context, eventText, activeSequence, completedSequence, activeDominance, completedDominance, level1, activeStructure, completedStructure);
                LinkParent(node);
                AssignMemberships(node);
                MoveCompletedNodeIfNeeded(node);
                UpdateHierarchyCandidateRelationships();
                reason = "ledger updated from Gaussian " + gaussian.State + " " + gaussian.Direction + " " + gaussian.StartBar + "-" + gaussian.EndBar;
            }

            return BuildSnapshot(bar, reason);
        }

        public bool IsLegitimatePeakCandidate(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return false;

            if (eventText.IndexOf("HHHL-R", StringComparison.OrdinalIgnoreCase) >= 0
                || eventText.IndexOf("LLLH-R", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return eventText.IndexOf("B+", StringComparison.Ordinal) >= 0
                || eventText.IndexOf("b+", StringComparison.Ordinal) >= 0
                || eventText.IndexOf("R+", StringComparison.Ordinal) >= 0
                || eventText.IndexOf("r+", StringComparison.Ordinal) >= 0;
        }

        private bool IsLedgerCandidate(xPvaGaussianCycle gaussian)
        {
            return gaussian.State == xPvaGaussianCycleState.Building
                || gaussian.State == xPvaGaussianCycleState.Complete
                || gaussian.State == xPvaGaussianCycleState.Failed
                || gaussian.State == xPvaGaussianCycleState.Suspended
                || gaussian.State == xPvaGaussianCycleState.LeftToRight
                || gaussian.State == xPvaGaussianCycleState.RetraceOnly;
        }

        private xPvaGaussianNode FindOrCreateNode(xPvaGaussianCycle gaussian, DateTime time)
        {
            xPvaGaussianNode node = FindNode(activeNodes, gaussian.StartBar, gaussian.Direction);
            if (node == null)
                node = FindNode(completedNodes, gaussian.StartBar, gaussian.Direction);

            if (node != null)
                return node;

            node = new xPvaGaussianNode
            {
                Id = nextNodeId++,
                Level = 1,
                StartBar = gaussian.StartBar,
                EndBar = gaussian.EndBar,
                StartTime = gaussian.StartTime == DateTime.MinValue ? time : gaussian.StartTime,
                EndTime = gaussian.EndTime == DateTime.MinValue ? time : gaussian.EndTime,
                Direction = DirectionFor(gaussian.Direction),
                State = xPvaGaussianNodeState.Building,
                Phase = xPvaGaussianNodePhase.Unknown,
                ChildIds = new List<int>(),
                ParentCandidateNodeIds = new List<int>(),
                ChildCandidateNodeIds = new List<int>(),
                HierarchyConfidence = "Unknown",
                HierarchyReason = "no hierarchy candidate evaluated",
                Anchors = new xPvaGaussianAnchorSet(),
                Pattern = "",
                Reason = "created from Gaussian cycle",
                Ambiguity = xPvaGaussianAmbiguity.None
            };

            activeNodes.Add(node);
            return node;
        }

        private xPvaGaussianNode FindNode(List<xPvaGaussianNode> nodes, int startBar, xPvaGaussianCycleDirection direction)
        {
            xPvaGaussianNodeDirection nodeDirection = DirectionFor(direction);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].StartBar == startBar && nodes[i].Direction == nodeDirection)
                    return nodes[i];
            }

            return null;
        }

        private void UpdateNode(
            xPvaGaussianNode node,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            string eventText,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            xPvaStructureObservation activeStructure,
            xPvaStructureObservation completedStructure)
        {
            node.EndBar = Math.Max(node.EndBar, gaussian.EndBar);
            node.EndTime = gaussian.EndTime;
            node.Direction = DirectionFor(gaussian.Direction);
            node.State = StateFor(gaussian.State);
            node.Phase = PhaseFor(gaussian.Phase);
            node.Pattern = gaussian.Pattern;

            string peakReason = PeakLegitimacyReason(currentBar, gaussian, eventText);

            if (gaussian.HasPeak && gaussian.PeakBar == currentBar && IsLegitimatePeakCandidate(eventText))
                node.PeakBar = gaussian.PeakBar;
            else if (gaussian.HasPeak && !node.PeakBar.HasValue)
                node.Reason = AppendReason(node.Reason, "peak marker observed but rejected for node peak: " + peakReason);

            if (gaussian.HasTrough)
                node.TroughBar = gaussian.TroughBar;

            ExtractAnchors(node, currentBar, gaussian, narrative, expectation, expectationValidation, container, level3Context, eventText, peakReason, activeSequence, completedSequence, activeDominance, completedDominance, level1, activeStructure, completedStructure);
            node.Ambiguity = AmbiguityFor(gaussian, narrative, container, level3Context);
            node.Confidence = ConfidenceFor(node.State, node.Ambiguity);
            node.Reason = BuildNodeReason(gaussian, narrative, expectation, container, level3Context);
        }

        private void ExtractAnchors(
            xPvaGaussianNode node,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            string eventText,
            string peakReason,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            xPvaStructureObservation activeStructure,
            xPvaStructureObservation completedStructure)
        {
            xPvaGaussianAnchorSet previous = node.Anchors != null ? node.Anchors.Clone() : null;
            xPvaGaussianAnchorSet anchors = node.Anchors ?? new xPvaGaussianAnchorSet();
            StringBuilder extractionReason = new StringBuilder();
            anchors.SameBarAnchorWarning = false;
            anchors.SameBarAnchorReason = "";
            anchors.P3SuppressedBySameBarRule = false;
            anchors.FTTSuppressedBySameBarRule = false;

            SetAnchor(ref anchors.P1Bar, ref anchors.P1Status, node.StartBar, previous != null ? previous.P1Bar : null, InitialP1Status(gaussian, eventText));
            ClearAnchor(ref anchors.P2Bar, ref anchors.P2Status);
            ClearAnchor(ref anchors.P3Bar, ref anchors.P3Status);
            ClearAnchor(ref anchors.FTTBar, ref anchors.FTTStatus);

            int? p2 = InferP2Bar(node, gaussian, eventText, anchors, previous, extractionReason);
            if (p2.HasValue && AcceptAnchorProposal("P2", p2.Value, anchors.P1Bar, extractionReason))
                SetAnchor(ref anchors.P2Bar, ref anchors.P2Status, p2.Value, previous != null ? previous.P2Bar : null, InitialP2Status(gaussian));
            else if (!anchors.P2Bar.HasValue)
                anchors.P2Status = xPvaAnchorStatus.Unknown;

            if (!anchors.P2Bar.HasValue)
            {
                ClearAnchor(ref anchors.P3Bar, ref anchors.P3Status);
                ClearAnchor(ref anchors.FTTBar, ref anchors.FTTStatus);
                ClearP3Candidate(anchors, "P3 candidate unavailable because P2 is missing");
                ClearFTTCandidate(anchors, "FTT candidate unavailable because P3 candidate is missing");
                AppendExtractionReason(extractionReason, "P3 and FTT left Unknown because P2 is missing");
            }
            else
            {
                UpdateP3Candidate(anchors, previous, currentBar, eventText, activeSequence, completedSequence, activeDominance, completedDominance, level1, extractionReason);
            }

            UpdateFTTCandidate(anchors, previous, currentBar, eventText, gaussian, narrative, expectation, expectationValidation, activeDominance, completedDominance, level1, extractionReason);
            xPvaContextSnapshot contextSnapshot = BuildContextSnapshot(currentBar, level1, gaussian, narrative, expectation, container, level3Context, activeSequence, completedSequence, activeDominance, completedDominance, activeStructure, completedStructure);
            UpdateAnchorPromotions(node.Id, anchors, previous, currentBar, gaussian, narrative, expectation, expectationValidation, activeDominance, completedDominance, level1, contextSnapshot, extractionReason);

            ClearAnchor(ref anchors.P3Bar, ref anchors.P3Status);
            if (anchors.P3CandidateBar.HasValue)
                AppendExtractionReason(extractionReason, "P3 anchor remains Unknown; candidate exported separately");

            if (!anchors.P3Bar.HasValue)
            {
                ClearAnchor(ref anchors.FTTBar, ref anchors.FTTStatus);
                AppendExtractionReason(extractionReason, "FTT left Unknown because P3 is missing");
            }

            int? ftt = anchors.P3Bar.HasValue ? InferFTTBar(node, gaussian, narrative, expectation, eventText, anchors.P3Bar, extractionReason) : null;
            if (ftt.HasValue && AcceptAnchorProposal("FTT", ftt.Value, anchors.P3Bar, extractionReason))
                SetAnchor(ref anchors.FTTBar, ref anchors.FTTStatus, ftt.Value, previous != null ? previous.FTTBar : null, InitialFTTStatus(gaussian, narrative, expectation, eventText));
            else if (!anchors.FTTBar.HasValue)
                anchors.FTTStatus = xPvaAnchorStatus.Unknown;

            anchors.AnchorCollapseWarning = IsAnchorCollapse(anchors);
            anchors.GeometryConfidence = GeometryConfidenceFor(anchors, gaussian);
            anchors.Confidence = AnchorConfidenceFor(anchors, gaussian, peakReason);
            anchors.Pattern = AnchorPattern(anchors);
            anchors.PeakLegitimacy = peakReason;
            anchors.Reason = AnchorReason(anchors, gaussian, narrative, expectation, container, level3Context, eventText, peakReason, extractionReason.ToString());

            node.Anchors = anchors;
            node.P1Bar = anchors.P1Bar;
            node.P2Bar = anchors.P2Bar;
            node.P3Bar = anchors.P3Bar;
            node.FTTBar = anchors.FTTBar;
            node.Anchors.Validation = anchorValidator.Validate(node);
        }

        private void SetAnchor(ref int? anchor, ref xPvaAnchorStatus status, int value, int? previous, xPvaAnchorStatus initialStatus)
        {
            if (previous.HasValue && previous.Value != value)
            {
                anchor = value;
                status = xPvaAnchorStatus.Adjusted;
                return;
            }

            anchor = value;
            status = initialStatus;
        }

        private void ClearAnchor(ref int? anchor, ref xPvaAnchorStatus status)
        {
            anchor = null;
            status = xPvaAnchorStatus.Unknown;
        }

        private xPvaAnchorStatus InitialP1Status(xPvaGaussianCycle gaussian, string eventText)
        {
            return xPvaAnchorStatus.Candidate;
        }

        private xPvaAnchorStatus InitialP2Status(xPvaGaussianCycle gaussian)
        {
            return xPvaAnchorStatus.Candidate;
        }

        private xPvaAnchorStatus InitialP3Status(xPvaGaussianCycle gaussian)
        {
            return xPvaAnchorStatus.Candidate;
        }

        private xPvaAnchorStatus InitialFTTStatus(
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            string eventText)
        {
            if (gaussian.State == xPvaGaussianCycleState.Failed || IsFailureLikeEvent(eventText) || (expectation != null && expectation.IsExpectationViolated))
                return xPvaAnchorStatus.Confirmed;

            return xPvaAnchorStatus.Candidate;
        }

        private bool AcceptAnchorProposal(string anchorName, int value, int? priorAnchor, StringBuilder extractionReason)
        {
            if (priorAnchor.HasValue && value < priorAnchor.Value)
            {
                AppendExtractionReason(extractionReason, anchorName + " proposal rejected because it violates required order");
                return false;
            }

            return true;
        }

        private int? InferP2Bar(
            xPvaGaussianNode node,
            xPvaGaussianCycle gaussian,
            string eventText,
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            StringBuilder extractionReason)
        {
            if (gaussian.HasTrough)
                AppendExtractionReason(extractionReason, "trough marker observed but not used as P2 without structural extreme support");

            if (anchors == null)
                return null;

            anchors.P2ExtremeBasis = xPvaP2ExtremeBasis.Unavailable;
            anchors.P2ExtremeReason = "OHLC unavailable for P2 structural extreme scan";

            if (!anchors.P1Bar.HasValue)
                return null;

            int startBar = anchors.P1Bar.Value + 1;
            int endBar = gaussian.EndBar;
            if (endBar < startBar)
            {
                anchors.P2ExtremeReason = "P2 Unknown because no bars exist after P1 inside node window";
                AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);
                return null;
            }

            xPvaGaussianNodeDirection direction = DirectionFor(gaussian.Direction);
            if (direction == xPvaGaussianNodeDirection.Red)
            {
                int? extremeBar = null;
                double extreme = double.MinValue;
                if (!TryFindHighestHigh(startBar, endBar, ref extremeBar, ref extreme))
                {
                    AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);
                    return null;
                }

                anchors.P2ExtremeBasis = xPvaP2ExtremeBasis.HighestHigh;
                anchors.P2ExtremeReason = "P2 selected as highest high within Red node window using OHLC";
                AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);
                if (previous != null && previous.P2Bar.HasValue && previous.P2Bar.Value != extremeBar.Value)
                    AppendExtractionReason(extractionReason, "P2 adjusted because a new stronger structural high appeared");

                return extremeBar;
            }

            if (direction == xPvaGaussianNodeDirection.Black)
            {
                int? extremeBar = null;
                double extreme = double.MaxValue;
                if (!TryFindLowestLow(startBar, endBar, ref extremeBar, ref extreme))
                {
                    AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);
                    return null;
                }

                anchors.P2ExtremeBasis = xPvaP2ExtremeBasis.LowestLow;
                anchors.P2ExtremeReason = "P2 selected as lowest low within Black node window using OHLC";
                AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);
                if (previous != null && previous.P2Bar.HasValue && previous.P2Bar.Value != extremeBar.Value)
                    AppendExtractionReason(extractionReason, "P2 adjusted because a new stronger structural low appeared");

                return extremeBar;
            }

            anchors.P2ExtremeReason = "P2 Unknown because Gaussian direction is not Black or Red";
            AppendExtractionReason(extractionReason, anchors.P2ExtremeReason);

            return null;
        }

        private bool TryFindHighestHigh(int startBar, int endBar, ref int? extremeBar, ref double extreme)
        {
            bool found = false;
            for (int bar = startBar; bar <= endBar; bar++)
            {
                xPvaGaussianOhlcBar ohlc;
                if (!ohlcByBar.TryGetValue(bar, out ohlc))
                    continue;

                if (!found || ohlc.High > extreme)
                {
                    found = true;
                    extreme = ohlc.High;
                    extremeBar = bar;
                }
            }

            return found;
        }

        private bool TryFindLowestLow(int startBar, int endBar, ref int? extremeBar, ref double extreme)
        {
            bool found = false;
            for (int bar = startBar; bar <= endBar; bar++)
            {
                xPvaGaussianOhlcBar ohlc;
                if (!ohlcByBar.TryGetValue(bar, out ohlc))
                    continue;

                if (!found || ohlc.Low < extreme)
                {
                    found = true;
                    extreme = ohlc.Low;
                    extremeBar = bar;
                }
            }

            return found;
        }

        private void UpdateP3Candidate(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            string eventText,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            StringBuilder extractionReason)
        {
            if (anchors == null || !anchors.P2Bar.HasValue)
            {
                ClearP3Candidate(anchors, "P3 candidate unavailable because P2 is missing");
                return;
            }

            if (previous != null
                && previous.P3CandidateBar.HasValue
                && previous.P3CandidateBar.Value >= anchors.P2Bar.Value
                && previous.P2Bar.HasValue
                && previous.P2Bar.Value == anchors.P2Bar.Value)
            {
                anchors.P3CandidateBar = previous.P3CandidateBar;
                anchors.P3CandidateConfidence = previous.P3CandidateConfidence;
                anchors.P3CandidateBasis = previous.P3CandidateBasis;
                anchors.P3CandidateReason = previous.P3CandidateReason;
                return;
            }

            if (currentBar < anchors.P2Bar.Value)
            {
                ClearP3Candidate(anchors, "P3 candidate unavailable because current bar is before P2");
                return;
            }

            string basis;
            string reason;
            xPvaAnchorConfidence confidence;
            if (!TryBuildP3CandidateEvidence(currentBar, anchors.P2Bar.Value, eventText, activeSequence, completedSequence, activeDominance, completedDominance, level1, out basis, out confidence, out reason))
            {
                ClearP3Candidate(anchors, "P3 candidate unavailable because no post-P2 structural return/turn evidence exists");
                return;
            }

            anchors.P3CandidateBar = currentBar;
            anchors.P3CandidateBasis = basis;
            anchors.P3CandidateConfidence = confidence;
            anchors.P3CandidateReason = reason;
            if (anchors.P2Bar.HasValue && anchors.P3CandidateBar.Value == anchors.P2Bar.Value && !IsStrongSameBarP3Evidence(basis))
            {
                SuppressP3BySameBarRule(anchors, "P3 candidate suppressed because candidate bar equals newly selected P2; future evidence required.");
                AppendExtractionReason(extractionReason, anchors.SameBarAnchorReason);
                return;
            }

            AppendExtractionReason(extractionReason, reason);
        }

        private void ClearP3Candidate(xPvaGaussianAnchorSet anchors, string reason)
        {
            if (anchors == null)
                return;

            anchors.P3CandidateBar = null;
            anchors.P3CandidateConfidence = xPvaAnchorConfidence.Unknown;
            anchors.P3CandidateBasis = "Unavailable";
            anchors.P3CandidateReason = reason;
            anchors.P3CandidateCreationBar = null;
            anchors.P3CandidateReplacementBar = null;
            anchors.P3SupersededByCandidateBar = null;
            anchors.P3PromotionStatus = xPvaAnchorPromotionStatus.Unknown;
            anchors.P3PromotionBar = null;
            anchors.P3PromotionReason = "no P3 candidate available";
            anchors.P3Outcome = xPvaAnchorOutcome.Unknown;
            anchors.P3OutcomeBar = null;
            anchors.P3BarsUntilOutcome = null;
            anchors.P3OutcomeRule = "Unavailable";
            anchors.P3CandidateAge = null;
            anchors.P3OutcomeReason = "no P3 candidate available";
        }

        private void SuppressP3BySameBarRule(xPvaGaussianAnchorSet anchors, string reason)
        {
            if (anchors == null)
                return;

            ClearP3Candidate(anchors, reason);
            anchors.P3SuppressedBySameBarRule = true;
            MarkSameBarAnchorWarning(anchors, reason);
            ClearFTTCandidate(anchors, "FTT candidate suppressed because P3 candidate is missing after same-bar P2/P3 suppression.");
            anchors.FTTSuppressedBySameBarRule = true;
            MarkSameBarAnchorWarning(anchors, anchors.FTTCandidateReason);
        }

        private bool TryBuildP3CandidateEvidence(
            int currentBar,
            int p2Bar,
            string eventText,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            out string basis,
            out xPvaAnchorConfidence confidence,
            out string reason)
        {
            basis = "Unavailable";
            confidence = xPvaAnchorConfidence.Unknown;
            reason = "";

            bool sameBar = currentBar == p2Bar;
            if (Contains(eventText, "Reversal") || Contains(eventText, "REV"))
                return BuildP3CandidateResult(currentBar, sameBar, "ReversalAfterP2", xPvaAnchorConfidence.High, "P3 candidate from reversal event after P2", out basis, out confidence, out reason);

            if (Contains(eventText, "Compressed") || Contains(eventText, "CR"))
                return BuildP3CandidateResult(currentBar, sameBar, "CompressionAfterP2", xPvaAnchorConfidence.Medium, "P3 candidate from compression after P2", out basis, out confidence, out reason);

            if (Contains(eventText, "OB") || Contains(eventText, "IB"))
                return BuildP3CandidateResult(currentBar, sameBar, "OBIBAfterP2", xPvaAnchorConfidence.High, "P3 candidate from OB/IB structural event after P2", out basis, out confidence, out reason);

            bool hasPostP2DominanceResponse = activeDominance != null
                && completedDominance != null
                && activeDominance.StartBar >= p2Bar
                && activeDominance.Side != completedDominance.Side;
            bool hasPostP2SequenceResponse = activeSequence != null
                && activeSequence.StartBar >= p2Bar
                && completedSequence != null
                && completedSequence.EndBar >= p2Bar;

            if (Contains(eventText, "VE") && (hasPostP2DominanceResponse || hasPostP2SequenceResponse))
                return BuildP3CandidateResult(currentBar, sameBar, "VERejectionAfterP2", xPvaAnchorConfidence.Medium, "P3 candidate from VE response after P2", out basis, out confidence, out reason);

            if (hasPostP2SequenceResponse)
                return BuildP3CandidateResult(currentBar, sameBar, "SequenceTurnAfterP2", xPvaAnchorConfidence.Medium, "P3 candidate from sequence transition after P2", out basis, out confidence, out reason);

            if (hasPostP2DominanceResponse)
                return BuildP3CandidateResult(currentBar, sameBar, "SequenceTurnAfterP2", xPvaAnchorConfidence.Medium, "P3 candidate from dominance change after P2", out basis, out confidence, out reason);

            if (level1 != null
                && level1.StartBar >= p2Bar
                && (level1.Role == xPvaLevel1Role.RetraceLeg || level1.Role == xPvaLevel1Role.DominantFinalLeg))
                return BuildP3CandidateResult(currentBar, sameBar, "ReturnAfterP2", xPvaAnchorConfidence.Medium, "P3 candidate from Level1 role transition after P2", out basis, out confidence, out reason);

            return false;
        }

        private bool BuildP3CandidateResult(
            int currentBar,
            bool sameBar,
            string candidateBasis,
            xPvaAnchorConfidence baseConfidence,
            string baseReason,
            out string basis,
            out xPvaAnchorConfidence confidence,
            out string reason)
        {
            basis = candidateBasis;
            confidence = sameBar ? xPvaAnchorConfidence.Low : baseConfidence;
            reason = baseReason + "; candidate bar=" + currentBar.ToString() + "; confidence=" + confidence.ToString();
            if (sameBar)
                reason += "; same-bar P2/P3 candidate kept conservative";

            return true;
        }

        private void UpdateFTTCandidate(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            string eventText,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            StringBuilder extractionReason)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
            {
                ClearFTTCandidate(anchors, "FTT candidate unavailable because P3 candidate is missing");
                return;
            }

            if (previous != null
                && previous.FTTCandidateBar.HasValue
                && previous.FTTCandidateBar.Value >= anchors.P3CandidateBar.Value
                && previous.P3CandidateBar.HasValue
                && previous.P3CandidateBar.Value == anchors.P3CandidateBar.Value)
            {
                anchors.FTTCandidateBar = previous.FTTCandidateBar;
                anchors.FTTCandidateConfidence = previous.FTTCandidateConfidence;
                anchors.FTTCandidateBasis = previous.FTTCandidateBasis;
                anchors.FTTCandidateReason = previous.FTTCandidateReason;
                return;
            }

            if (currentBar < anchors.P3CandidateBar.Value)
            {
                ClearFTTCandidate(anchors, "FTT candidate unavailable because current bar is before P3 candidate");
                return;
            }

            string basis;
            string reason;
            xPvaAnchorConfidence confidence;
            if (!TryBuildFTTCandidateEvidence(currentBar, anchors.P3CandidateBar.Value, eventText, gaussian, narrative, expectation, expectationValidation, activeDominance, completedDominance, level1, out basis, out confidence, out reason))
            {
                ClearFTTCandidate(anchors, "FTT candidate unavailable because no post-P3 failure/change/compression evidence exists");
                return;
            }

            anchors.FTTCandidateBar = currentBar;
            anchors.FTTCandidateBasis = basis;
            anchors.FTTCandidateConfidence = confidence;
            anchors.FTTCandidateReason = reason;
            if (ShouldSuppressFTTBySameBarRule(anchors, basis, currentBar, out reason))
            {
                SuppressFTTBySameBarRule(anchors, reason);
                AppendExtractionReason(extractionReason, anchors.SameBarAnchorReason);
                return;
            }

            AppendExtractionReason(extractionReason, reason);
        }

        private void ClearFTTCandidate(xPvaGaussianAnchorSet anchors, string reason)
        {
            if (anchors == null)
                return;

            anchors.FTTCandidateBar = null;
            anchors.FTTCandidateConfidence = xPvaAnchorConfidence.Unknown;
            anchors.FTTCandidateBasis = "Unavailable";
            anchors.FTTCandidateReason = reason;
            anchors.FTTCandidateCreationBar = null;
            anchors.FTTCandidateReplacementBar = null;
            anchors.FTTSupersededByCandidateBar = null;
            anchors.FTTPromotionStatus = xPvaAnchorPromotionStatus.Unknown;
            anchors.FTTPromotionBar = null;
            anchors.FTTPromotionReason = "no FTT candidate available";
            anchors.FTTOutcome = xPvaAnchorOutcome.Unknown;
            anchors.FTTOutcomeBar = null;
            anchors.FTTBarsUntilOutcome = null;
            anchors.FTTOutcomeReason = "no FTT candidate available";
        }

        private void SuppressFTTBySameBarRule(xPvaGaussianAnchorSet anchors, string reason)
        {
            if (anchors == null)
                return;

            ClearFTTCandidate(anchors, reason);
            anchors.FTTSuppressedBySameBarRule = true;
            MarkSameBarAnchorWarning(anchors, reason);
        }

        private bool ShouldSuppressFTTBySameBarRule(xPvaGaussianAnchorSet anchors, string basis, int currentBar, out string reason)
        {
            reason = "";
            if (anchors == null || !anchors.FTTCandidateBar.HasValue)
                return false;

            if (anchors.P2Bar.HasValue && anchors.FTTCandidateBar.Value == anchors.P2Bar.Value)
            {
                reason = "FTT candidate suppressed because candidate bar equals P2; no separate failure-to-traverse evidence.";
                return true;
            }

            if (anchors.P3CandidateBar.HasValue && anchors.FTTCandidateBar.Value == anchors.P3CandidateBar.Value && !IsStrongSameBarFTTEvidence(basis))
            {
                reason = "FTT candidate suppressed because candidate bar equals P3 candidate; future failure evidence required.";
                return true;
            }

            if (anchors.P3CandidateBar.HasValue && currentBar == anchors.P3CandidateBar.Value && !IsStrongSameBarFTTEvidence(basis))
            {
                reason = "FTT candidate suppressed because same-bar P3/FTT evidence is not explicit failure evidence.";
                return true;
            }

            return false;
        }

        private bool TryBuildFTTCandidateEvidence(
            int currentBar,
            int p3CandidateBar,
            string eventText,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            out string basis,
            out xPvaAnchorConfidence confidence,
            out string reason)
        {
            basis = "Unavailable";
            confidence = xPvaAnchorConfidence.Unknown;
            reason = "";

            bool sameBar = currentBar == p3CandidateBar;
            if (Contains(eventText, "SFC") || Contains(eventText, "FTT"))
                return BuildFTTCandidateResult(currentBar, sameBar, "SFC", xPvaAnchorConfidence.High, true, "FTT candidate from SFC/FTT event after P3 candidate", out basis, out confidence, out reason);

            if (Contains(eventText, "Compressed") || Contains(eventText, "CR"))
                return BuildFTTCandidateResult(currentBar, sameBar, "CompressionAfterP3", xPvaAnchorConfidence.High, true, "FTT candidate from compression after P3 candidate", out basis, out confidence, out reason);

            if (Contains(eventText, "Reversal") || Contains(eventText, "REV"))
                return BuildFTTCandidateResult(currentBar, sameBar, "ReversalAfterP3", xPvaAnchorConfidence.High, true, "FTT candidate from reversal event after P3 candidate", out basis, out confidence, out reason);

            if (Contains(eventText, "OB") || Contains(eventText, "IB"))
                return BuildFTTCandidateResult(currentBar, sameBar, "OBIBAfterP3", xPvaAnchorConfidence.High, true, "FTT candidate from OB/IB boundary event after P3 candidate", out basis, out confidence, out reason);

            bool hasPostP3DominanceInterruption = activeDominance != null
                && completedDominance != null
                && activeDominance.StartBar >= p3CandidateBar
                && activeDominance.Side != completedDominance.Side;

            if (Contains(eventText, "VE") && hasPostP3DominanceInterruption)
                return BuildFTTCandidateResult(currentBar, sameBar, "VERejectionAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from VE rejection/response after P3 candidate", out basis, out confidence, out reason);

            if (expectationValidation != null
                && expectationValidation.Outcome == xPvaExpectationOutcome.Failed
                && expectationValidation.ResolutionBar >= p3CandidateBar)
                return BuildFTTCandidateResult(currentBar, sameBar, "ExpectationFailureAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from failed expectation after P3 candidate", out basis, out confidence, out reason);

            if (expectation != null && expectation.IsExpectationViolated && expectation.ResolutionBar >= p3CandidateBar)
                return BuildFTTCandidateResult(currentBar, sameBar, "ExpectationFailureAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from expectation violation after P3 candidate", out basis, out confidence, out reason);

            if ((gaussian != null && gaussian.State == xPvaGaussianCycleState.Failed)
                || (narrative != null && (narrative.State == xPvaGaussianNarrativeState.BlackCycleFailure || narrative.State == xPvaGaussianNarrativeState.RedCycleFailure)))
                return BuildFTTCandidateResult(currentBar, sameBar, "FailedContinuationAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from failed continuation after P3 candidate", out basis, out confidence, out reason);

            if (hasPostP3DominanceInterruption)
                return BuildFTTCandidateResult(currentBar, sameBar, "FailedContinuationAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from dominance interruption after P3 candidate", out basis, out confidence, out reason);

            if (level1 != null
                && level1.StartBar >= p3CandidateBar
                && (level1.Role == xPvaLevel1Role.RetraceLeg || level1.Role == xPvaLevel1Role.DominantFinalLeg || level1.Role == xPvaLevel1Role.StandaloneTape))
                return BuildFTTCandidateResult(currentBar, sameBar, "RoleTransitionAfterP3", xPvaAnchorConfidence.Medium, false, "FTT candidate from Level1 role transition after P3 candidate", out basis, out confidence, out reason);

            return false;
        }

        private bool BuildFTTCandidateResult(
            int currentBar,
            bool sameBar,
            string candidateBasis,
            xPvaAnchorConfidence baseConfidence,
            bool strongSameBarEvidence,
            string baseReason,
            out string basis,
            out xPvaAnchorConfidence confidence,
            out string reason)
        {
            basis = candidateBasis;
            confidence = sameBar
                ? (strongSameBarEvidence ? xPvaAnchorConfidence.Medium : xPvaAnchorConfidence.Low)
                : baseConfidence;
            reason = baseReason + "; candidate bar=" + currentBar.ToString() + "; confidence=" + confidence.ToString();
            if (sameBar)
                reason += strongSameBarEvidence ? "; same-bar P3/FTT candidate allowed only as Medium despite strong boundary evidence" : "; same-bar P3/FTT candidate kept Low";

            return true;
        }

        private bool IsStrongSameBarP3Evidence(string basis)
        {
            return basis == "ReversalAfterP2" || basis == "OBIBAfterP2";
        }

        private bool IsStrongSameBarFTTEvidence(string basis)
        {
            return basis == "SFC"
                || basis == "ReversalAfterP3"
                || basis == "OBIBAfterP3"
                || basis == "VERejectionAfterP3"
                || basis == "ExpectationFailureAfterP3"
                || basis == "FailedContinuationAfterP3";
        }

        private void MarkSameBarAnchorWarning(xPvaGaussianAnchorSet anchors, string reason)
        {
            if (anchors == null)
                return;

            anchors.SameBarAnchorWarning = true;
            if (string.IsNullOrEmpty(anchors.SameBarAnchorReason))
            {
                anchors.SameBarAnchorReason = reason;
                return;
            }

            if (anchors.SameBarAnchorReason.IndexOf(reason, StringComparison.OrdinalIgnoreCase) < 0)
                anchors.SameBarAnchorReason += " | " + reason;
        }

        private void UpdateAnchorPromotions(
            int nodeId,
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            xPvaContextSnapshot contextSnapshot,
            StringBuilder extractionReason)
        {
            UpdateP3Promotion(anchors, previous, currentBar, expectationValidation, level1, extractionReason);
            UpdateFTTPromotion(anchors, previous, currentBar, gaussian, narrative, expectation, expectationValidation, activeDominance, completedDominance, level1, extractionReason);
            ValidateCandidateLifecycle(anchors, currentBar);
            if (anchors != null && anchors.CandidateLifecycleViolation)
                AppendExtractionReason(extractionReason, "CandidateLifecycleViolation: candidate was created and superseded on the same evaluation pass");
            UpdateAnchorOutcomes(anchors, previous, currentBar, gaussian, expectationValidation, level1);
            UpdateContextSnapshots(anchors, previous, contextSnapshot);
            UpdateRawCandidateEventLedger(nodeId, anchors, currentBar);
            UpdateAnchorBasisStatistics(nodeId, anchors);
            UpdateContextSignatureStatistics(nodeId, anchors);
        }

        private void UpdateP3Promotion(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            xPvaExpectationValidation expectationValidation,
            xPvaLevel1Object level1,
            StringBuilder extractionReason)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
            {
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Unknown, null, "no P3 candidate available");
                return;
            }

            int candidate = anchors.P3CandidateBar.Value;
            InitializeP3CandidateLifecycle(anchors, previous, currentBar);
            if (!PromotionOrderingValidForP3(anchors))
            {
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, "P3 candidate rejected because anchor ordering is invalid");
                AppendExtractionReason(extractionReason, anchors.P3PromotionReason);
                return;
            }

            if (previous != null && previous.P3CandidateBar.HasValue && previous.P3CandidateBar.Value != candidate)
            {
                if (AnchorConfidenceRank(anchors.P3CandidateConfidence) > AnchorConfidenceRank(previous.P3CandidateConfidence))
                {
                    anchors.P3CandidateReplacementBar = currentBar;
                    anchors.P3SupersededByCandidateBar = candidate;
                    SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Candidate, candidate, "P3 candidate replaced prior candidate " + previous.P3CandidateBar.Value.ToString() + "; new candidate remains under observation");
                    AppendExtractionReason(extractionReason, anchors.P3PromotionReason);
                    return;
                }
            }

            if (anchors.P2Bar.HasValue && candidate < anchors.P2Bar.Value)
            {
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, "P3 candidate rejected because P2 adjusted beyond the candidate");
                AppendExtractionReason(extractionReason, anchors.P3PromotionReason);
                return;
            }

            if (anchors.P3CandidateConfidence == xPvaAnchorConfidence.Low
                && candidate == anchors.P2Bar.GetValueOrDefault(candidate)
                && currentBar > candidate + 2
                && !anchors.FTTCandidateBar.HasValue)
            {
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, "P3 candidate rejected because weak same-bar P2/P3 evidence did not receive support");
                AppendExtractionReason(extractionReason, anchors.P3PromotionReason);
                return;
            }

            bool hasLaterFttCandidate = anchors.FTTCandidateBar.HasValue && anchors.FTTCandidateBar.Value > candidate;
            bool hasExpectationSupport = expectationValidation != null
                && (expectationValidation.Outcome == xPvaExpectationOutcome.Satisfied || expectationValidation.Outcome == xPvaExpectationOutcome.PartiallySatisfied)
                && expectationValidation.ResolutionBar > candidate;
            bool hasRoleSupport = level1 != null
                && level1.StartBar > candidate
                && (level1.Role == xPvaLevel1Role.DominantFinalLeg || level1.Role == xPvaLevel1Role.StandaloneTape)
                && level1.RoleConfidence != xPvaRoleConfidence.Low
                && level1.RoleConfidence != xPvaRoleConfidence.Unknown;

            if (currentBar > candidate
                && anchors.P3CandidateConfidence == xPvaAnchorConfidence.High
                && (hasLaterFttCandidate || hasExpectationSupport || hasRoleSupport))
            {
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Confirmed, currentBar, "P3 candidate confirmed by subsequent supporting structure");
                AppendExtractionReason(extractionReason, anchors.P3PromotionReason);
                return;
            }

            SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Candidate, candidate, "P3 candidate remains under observation; no confirming or rejecting evidence yet");
        }

        private void UpdateFTTPromotion(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1,
            StringBuilder extractionReason)
        {
            if (anchors == null || !anchors.FTTCandidateBar.HasValue)
            {
                SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Unknown, null, "no FTT candidate available");
                return;
            }

            int candidate = anchors.FTTCandidateBar.Value;
            InitializeFTTCandidateLifecycle(anchors, previous, currentBar);
            if (!PromotionOrderingValidForFTT(anchors))
            {
                SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, "FTT candidate rejected because anchor ordering is invalid");
                AppendExtractionReason(extractionReason, anchors.FTTPromotionReason);
                return;
            }

            if (previous != null && previous.FTTCandidateBar.HasValue && previous.FTTCandidateBar.Value != candidate)
            {
                if (AnchorConfidenceRank(anchors.FTTCandidateConfidence) > AnchorConfidenceRank(previous.FTTCandidateConfidence))
                {
                    anchors.FTTCandidateReplacementBar = currentBar;
                    anchors.FTTSupersededByCandidateBar = candidate;
                    SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Candidate, candidate, "FTT candidate replaced prior candidate " + previous.FTTCandidateBar.Value.ToString() + "; new candidate remains under observation");
                    AppendExtractionReason(extractionReason, anchors.FTTPromotionReason);
                    return;
                }
            }

            if (previous != null
                && previous.P3CandidateBar.HasValue
                && anchors.P3CandidateBar.HasValue
                && previous.P3CandidateBar.Value != anchors.P3CandidateBar.Value)
            {
                SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Candidate, candidate, "FTT candidate re-evaluated because P3 candidate changed; no supersession without a replacement FTT candidate");
                AppendExtractionReason(extractionReason, anchors.FTTPromotionReason);
                return;
            }

            if (anchors.FTTCandidateConfidence == xPvaAnchorConfidence.Low
                && anchors.P3CandidateBar.HasValue
                && candidate == anchors.P3CandidateBar.Value
                && currentBar > candidate + 2
                && !HasPostFttFailureSupport(candidate, expectation, expectationValidation, activeDominance, completedDominance, level1))
            {
                SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, "FTT candidate rejected because weak same-bar P3/FTT evidence did not receive follow-through");
                AppendExtractionReason(extractionReason, anchors.FTTPromotionReason);
                return;
            }

            bool hasFailureSupport = HasPostFttFailureSupport(candidate, expectation, expectationValidation, activeDominance, completedDominance, level1)
                || (gaussian != null && gaussian.State == xPvaGaussianCycleState.Failed && currentBar > candidate)
                || (narrative != null && (narrative.State == xPvaGaussianNarrativeState.BlackCycleFailure || narrative.State == xPvaGaussianNarrativeState.RedCycleFailure) && currentBar > candidate);

            if (currentBar > candidate
                && anchors.FTTCandidateConfidence == xPvaAnchorConfidence.High
                && hasFailureSupport)
            {
                SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Confirmed, currentBar, "FTT candidate confirmed by subsequent failure/change evidence");
                AppendExtractionReason(extractionReason, anchors.FTTPromotionReason);
                return;
            }

            SetFTTPromotion(anchors, xPvaAnchorPromotionStatus.Candidate, candidate, "FTT candidate remains under observation; no confirming or rejecting evidence yet");
        }

        private bool HasPostFttFailureSupport(
            int fttCandidateBar,
            xPvaGaussianExpectationState expectation,
            xPvaExpectationValidation expectationValidation,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaLevel1Object level1)
        {
            if (expectationValidation != null
                && (expectationValidation.Outcome == xPvaExpectationOutcome.Failed || expectationValidation.Outcome == xPvaExpectationOutcome.Transitioned || expectationValidation.Outcome == xPvaExpectationOutcome.Mutated)
                && expectationValidation.ResolutionBar > fttCandidateBar)
                return true;

            if (expectation != null && expectation.IsExpectationViolated && expectation.ResolutionBar > fttCandidateBar)
                return true;

            if (activeDominance != null
                && completedDominance != null
                && activeDominance.StartBar > fttCandidateBar
                && activeDominance.Side != completedDominance.Side)
                return true;

            return level1 != null
                && level1.StartBar > fttCandidateBar
                && (level1.Role == xPvaLevel1Role.RetraceLeg || level1.Role == xPvaLevel1Role.DominantFinalLeg || level1.Role == xPvaLevel1Role.StandaloneTape)
                && level1.RoleConfidence != xPvaRoleConfidence.Unknown;
        }

        private void InitializeP3CandidateLifecycle(xPvaGaussianAnchorSet anchors, xPvaGaussianAnchorSet previous, int currentBar)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
                return;

            anchors.CandidateLifecycleViolation = false;
            bool sameCandidate = previous != null
                && previous.P3CandidateBar.HasValue
                && previous.P3CandidateBar.Value == anchors.P3CandidateBar.Value;

            anchors.P3CandidateCreationBar = sameCandidate && previous.P3CandidateCreationBar.HasValue
                ? previous.P3CandidateCreationBar
                : currentBar;
            anchors.P3CandidateReplacementBar = sameCandidate ? previous.P3CandidateReplacementBar : null;
            anchors.P3SupersededByCandidateBar = sameCandidate ? previous.P3SupersededByCandidateBar : null;
        }

        private void InitializeFTTCandidateLifecycle(xPvaGaussianAnchorSet anchors, xPvaGaussianAnchorSet previous, int currentBar)
        {
            if (anchors == null || !anchors.FTTCandidateBar.HasValue)
                return;

            anchors.CandidateLifecycleViolation = false;
            bool sameCandidate = previous != null
                && previous.FTTCandidateBar.HasValue
                && previous.FTTCandidateBar.Value == anchors.FTTCandidateBar.Value;

            anchors.FTTCandidateCreationBar = sameCandidate && previous.FTTCandidateCreationBar.HasValue
                ? previous.FTTCandidateCreationBar
                : currentBar;
            anchors.FTTCandidateReplacementBar = sameCandidate ? previous.FTTCandidateReplacementBar : null;
            anchors.FTTSupersededByCandidateBar = sameCandidate ? previous.FTTSupersededByCandidateBar : null;
        }

        private void ValidateCandidateLifecycle(xPvaGaussianAnchorSet anchors, int currentBar)
        {
            if (anchors == null)
                return;

            bool p3BornSuperseded = anchors.P3PromotionStatus == xPvaAnchorPromotionStatus.Superseded
                && anchors.P3CandidateCreationBar.HasValue
                && anchors.P3CandidateCreationBar.Value == currentBar;
            bool fttBornSuperseded = anchors.FTTPromotionStatus == xPvaAnchorPromotionStatus.Superseded
                && anchors.FTTCandidateCreationBar.HasValue
                && anchors.FTTCandidateCreationBar.Value == currentBar;

            anchors.CandidateLifecycleViolation = p3BornSuperseded || fttBornSuperseded;
        }

        private void UpdateAnchorOutcomes(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaExpectationValidation expectationValidation,
            xPvaLevel1Object level1)
        {
            UpdateP3Outcome(anchors, previous, currentBar, gaussian, expectationValidation, level1);
            UpdateFTTOutcome(anchors, previous, currentBar);
        }

        private void UpdateP3Outcome(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianAnchorSet previous,
            int currentBar,
            xPvaGaussianCycle gaussian,
            xPvaExpectationValidation expectationValidation,
            xPvaLevel1Object level1)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
            {
                if (anchors != null)
                {
                    anchors.P3Outcome = xPvaAnchorOutcome.Unknown;
                    anchors.P3OutcomeBar = null;
                    anchors.P3BarsUntilOutcome = null;
                    anchors.P3OutcomeRule = "Unavailable";
                    anchors.P3CandidateAge = null;
                    anchors.P3OutcomeReason = "no P3 candidate available";
                }
                return;
            }

            int candidate = anchors.P3CandidateBar.Value;
            anchors.P3CandidateAge = Math.Max(0, currentBar - candidate);

            if (previous != null
                && previous.P3CandidateBar.HasValue
                && previous.P3CandidateBar.Value == candidate
                && IsResolvedOutcome(previous.P3Outcome))
            {
                anchors.P3Outcome = previous.P3Outcome;
                anchors.P3OutcomeBar = previous.P3OutcomeBar;
                anchors.P3BarsUntilOutcome = previous.P3BarsUntilOutcome;
                anchors.P3OutcomeReason = previous.P3OutcomeReason;
                anchors.P3OutcomeRule = previous.P3OutcomeRule;
                return;
            }

            if (anchors.P2Bar.HasValue && anchors.P2Bar.Value > candidate)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Rejected, currentBar, "P2AdjustedBeyondP3", "rejected because P2 adjusted beyond P3 candidate");
                return;
            }

            if (!PromotionOrderingValidForP3(anchors))
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Rejected, currentBar, "OrderingViolation", "rejected because candidate no longer occurs after P2");
                return;
            }

            if (anchors.P3SuppressedBySameBarRule
                || (anchors.P2Bar.HasValue && anchors.P2Bar.Value == candidate && anchors.P3CandidateConfidence == xPvaAnchorConfidence.Low && currentBar > candidate + 2))
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Rejected, currentBar, "WeakSameBarNoSupport", "rejected because weak same-bar P2/P3 evidence did not receive later support");
                return;
            }

            if (anchors.FTTCandidateBar.HasValue && anchors.FTTCandidateBar.Value > candidate)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Confirmed, currentBar, "FTTCandidateAfterP3", "confirmed because FTT candidate appeared after P3");
                return;
            }

            if (gaussian != null
                && gaussian.Phase == xPvaGaussianCyclePhase.FinalDominantLeg
                && gaussian.EndBar > candidate)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Confirmed, currentBar, "FinalDominantLegAfterP3", "confirmed because final dominant leg continued after P3");
                return;
            }

            if (expectationValidation != null
                && expectationValidation.Outcome == xPvaExpectationOutcome.Satisfied
                && expectationValidation.ResolutionBar > candidate)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Confirmed, currentBar, "ExpectationSatisfiedAfterP3", "confirmed because expectation validation supports the P3 interpretation");
                return;
            }

            if (level1 != null
                && level1.StartBar > candidate
                && (level1.Role == xPvaLevel1Role.DominantFinalLeg || level1.Role == xPvaLevel1Role.StandaloneTape)
                && level1.RoleConfidence != xPvaRoleConfidence.Unknown)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Confirmed, currentBar, "Level1ContinuationAfterP3", "confirmed because later Level1 role continued after P3");
                return;
            }

            if (gaussian != null
                && gaussian.State == xPvaGaussianCycleState.Complete
                && gaussian.EndBar > candidate
                && (!anchors.P2Bar.HasValue || anchors.P2Bar.Value <= candidate))
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Confirmed, currentBar, "NodeCompletedWithValidP3", "confirmed because node completed while P3 candidate remained structurally valid");
                return;
            }

            if (anchors.P3CandidateAge.HasValue && anchors.P3CandidateAge.Value > P3CandidateMaxUnresolvedBars)
            {
                SetP3Outcome(anchors, xPvaAnchorOutcome.Expired, currentBar, "MaxUnresolvedBars", "expired after 12 bars without confirmation or rejection");
                return;
            }

            anchors.P3Outcome = xPvaAnchorOutcome.Unresolved;
            anchors.P3OutcomeBar = null;
            anchors.P3BarsUntilOutcome = null;
            anchors.P3OutcomeRule = "RecentNoEvidence";
            anchors.P3OutcomeReason = "P3 candidate remains unresolved";
        }

        private void SetP3Outcome(xPvaGaussianAnchorSet anchors, xPvaAnchorOutcome outcome, int currentBar, string rule, string reason)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
                return;

            anchors.P3Outcome = outcome;
            anchors.P3OutcomeBar = currentBar;
            anchors.P3BarsUntilOutcome = currentBar - anchors.P3CandidateBar.Value;
            anchors.P3OutcomeRule = rule;
            anchors.P3OutcomeReason = reason;

            if (outcome == xPvaAnchorOutcome.Confirmed)
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Confirmed, currentBar, reason);
            else if (outcome == xPvaAnchorOutcome.Rejected || outcome == xPvaAnchorOutcome.Expired)
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Rejected, currentBar, reason);
            else if (outcome == xPvaAnchorOutcome.Superseded)
                SetP3Promotion(anchors, xPvaAnchorPromotionStatus.Superseded, currentBar, reason);
        }

        private void UpdateFTTOutcome(xPvaGaussianAnchorSet anchors, xPvaGaussianAnchorSet previous, int currentBar)
        {
            if (anchors == null || !anchors.FTTCandidateBar.HasValue)
            {
                anchors.FTTOutcome = xPvaAnchorOutcome.Unknown;
                anchors.FTTOutcomeBar = null;
                anchors.FTTBarsUntilOutcome = null;
                anchors.FTTOutcomeReason = "no FTT candidate available";
                return;
            }

            if (previous != null
                && previous.FTTCandidateBar.HasValue
                && previous.FTTCandidateBar.Value == anchors.FTTCandidateBar.Value
                && IsResolvedOutcome(previous.FTTOutcome))
            {
                anchors.FTTOutcome = previous.FTTOutcome;
                anchors.FTTOutcomeBar = previous.FTTOutcomeBar;
                anchors.FTTBarsUntilOutcome = previous.FTTBarsUntilOutcome;
                anchors.FTTOutcomeReason = previous.FTTOutcomeReason;
                return;
            }

            xPvaAnchorOutcome outcome = OutcomeForPromotion(anchors.FTTPromotionStatus);
            anchors.FTTOutcome = outcome;
            if (IsResolvedOutcome(outcome))
            {
                anchors.FTTOutcomeBar = currentBar;
                anchors.FTTBarsUntilOutcome = currentBar - anchors.FTTCandidateBar.Value;
                anchors.FTTOutcomeReason = "FTT outcome derived from promotion status " + anchors.FTTPromotionStatus.ToString();
                return;
            }

            anchors.FTTOutcomeBar = null;
            anchors.FTTBarsUntilOutcome = null;
            anchors.FTTOutcomeReason = "FTT candidate remains unresolved";
        }

        private xPvaAnchorOutcome OutcomeForPromotion(xPvaAnchorPromotionStatus status)
        {
            if (status == xPvaAnchorPromotionStatus.Confirmed)
                return xPvaAnchorOutcome.Confirmed;

            if (status == xPvaAnchorPromotionStatus.Rejected)
                return xPvaAnchorOutcome.Rejected;

            if (status == xPvaAnchorPromotionStatus.Superseded)
                return xPvaAnchorOutcome.Superseded;

            if (status == xPvaAnchorPromotionStatus.Candidate)
                return xPvaAnchorOutcome.Unresolved;

            return xPvaAnchorOutcome.Unknown;
        }

        private bool IsResolvedOutcome(xPvaAnchorOutcome outcome)
        {
            return outcome == xPvaAnchorOutcome.Confirmed
                || outcome == xPvaAnchorOutcome.Rejected
                || outcome == xPvaAnchorOutcome.Superseded
                || outcome == xPvaAnchorOutcome.Expired;
        }

        private xPvaContextSnapshot BuildContextSnapshot(
            int bar,
            xPvaLevel1Object level1,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            xPvaEventSequence activeSequence,
            xPvaEventSequence completedSequence,
            xPvaDominanceObservation activeDominance,
            xPvaDominanceObservation completedDominance,
            xPvaStructureObservation activeStructure,
            xPvaStructureObservation completedStructure)
        {
            var snapshot = new xPvaContextSnapshot
            {
                Bar = bar,
                Level1State = level1 != null ? level1.State.ToString() : "Unknown",
                Level1Direction = level1 != null ? level1.Direction.ToString() : "Unknown",
                Level1Role = level1 != null ? level1.Role.ToString() : "Unknown",
                GaussianState = gaussian != null ? gaussian.State.ToString() : "Unknown",
                GaussianDirection = gaussian != null ? gaussian.Direction.ToString() : "Unknown",
                GaussianPhase = gaussian != null ? gaussian.Phase.ToString() : "Unknown",
                NarrativeState = narrative != null ? narrative.State.ToString() : "Unknown",
                NarrativeDirection = narrative != null ? narrative.Direction.ToString() : "Unknown",
                ExpectationState = expectation != null ? expectation.Expectation.ToString() : "Unknown",
                ExpectedDirection = expectation != null ? expectation.ExpectedDirection.ToString() : "Unknown",
                ContainerLevel = container != null ? container.Level.ToString() : "Unknown",
                ContainerDirection = container != null ? container.Direction.ToString() : "Unknown",
                ContainerState = container != null ? container.State.ToString() : "Unknown",
                Level3State = level3Context != null ? level3Context.State.ToString() : "Unknown",
                ActiveDominance = DominanceSignature(activeDominance),
                DoneDominance = DominanceSignature(completedDominance),
                ActiveGrammar = SequenceSignature(activeSequence),
                DoneGrammar = SequenceSignature(completedSequence),
                ActiveStructure = StructureSignature(activeStructure),
                DoneStructure = StructureSignature(completedStructure)
            };

            snapshot.ContextSignature = BuildContextSignature(snapshot);
            return snapshot;
        }

        private string BuildContextSignature(xPvaContextSnapshot snapshot)
        {
            if (snapshot == null)
                return "Unknown";

            return SafePart(snapshot.NarrativeState)
                + "|" + SafePart(snapshot.ExpectationState)
                + "|" + SafePart(snapshot.ContainerLevel) + SafePart(snapshot.ContainerState)
                + "|" + SafePart(snapshot.Level1Role)
                + "|" + SafePart(snapshot.ActiveDominance)
                + "|" + SafePart(snapshot.DoneDominance)
                + "|" + SafePart(snapshot.ActiveGrammar)
                + "|" + SafePart(snapshot.DoneGrammar)
                + "|" + SafePart(snapshot.ActiveStructure)
                + "|" + SafePart(snapshot.DoneStructure);
        }

        private string SafePart(string value)
        {
            return string.IsNullOrEmpty(value) ? "Unknown" : value.Replace("|", "/");
        }

        private string DominanceSignature(xPvaDominanceObservation dominance)
        {
            if (dominance == null)
                return "Unknown";

            string label = string.IsNullOrEmpty(dominance.ContextLabel) ? dominance.Side.ToString() : dominance.ContextLabel;
            return label + dominance.Rank.ToString();
        }

        private string SequenceSignature(xPvaEventSequence sequence)
        {
            if (sequence == null)
                return "Unknown";

            string pattern = string.IsNullOrEmpty(sequence.ChangePattern) ? "" : ":" + sequence.ChangePattern;
            return sequence.Kind.ToString() + pattern;
        }

        private string StructureSignature(xPvaStructureObservation structure)
        {
            if (structure == null)
                return "Unknown";

            return structure.StructureState.ToString() + structure.CurrentObjectSide.ToString();
        }

        private void UpdateContextSnapshots(xPvaGaussianAnchorSet anchors, xPvaGaussianAnchorSet previous, xPvaContextSnapshot currentSnapshot)
        {
            if (anchors == null)
                return;

            if (anchors.P3CandidateBar.HasValue)
            {
                if (previous != null && previous.P3CandidateBar.HasValue && previous.P3CandidateBar.Value == anchors.P3CandidateBar.Value && previous.P3CandidateSnapshot != null)
                    anchors.P3CandidateSnapshot = previous.P3CandidateSnapshot.Clone();
                else
                    anchors.P3CandidateSnapshot = currentSnapshot != null ? currentSnapshot.Clone() : null;

                anchors.P3ContextSignature = SignatureForSnapshot(anchors.P3CandidateSnapshot);

                if (IsResolvedOutcome(anchors.P3Outcome))
                {
                    if (previous != null && previous.P3CandidateBar.HasValue && previous.P3CandidateBar.Value == anchors.P3CandidateBar.Value && previous.P3Outcome == anchors.P3Outcome && previous.P3OutcomeSnapshot != null)
                        anchors.P3OutcomeSnapshot = previous.P3OutcomeSnapshot.Clone();
                    else
                        anchors.P3OutcomeSnapshot = currentSnapshot != null ? currentSnapshot.Clone() : null;

                    anchors.P3OutcomeContextSignature = SignatureForSnapshot(anchors.P3OutcomeSnapshot);
                }
                else
                {
                    anchors.P3OutcomeSnapshot = null;
                    anchors.P3OutcomeContextSignature = "";
                }
            }
            else
            {
                anchors.P3CandidateSnapshot = null;
                anchors.P3OutcomeSnapshot = null;
                anchors.P3ContextSignature = "";
                anchors.P3OutcomeContextSignature = "";
            }

            if (anchors.FTTCandidateBar.HasValue)
            {
                if (previous != null && previous.FTTCandidateBar.HasValue && previous.FTTCandidateBar.Value == anchors.FTTCandidateBar.Value && previous.FTTCandidateSnapshot != null)
                    anchors.FTTCandidateSnapshot = previous.FTTCandidateSnapshot.Clone();
                else
                    anchors.FTTCandidateSnapshot = currentSnapshot != null ? currentSnapshot.Clone() : null;

                anchors.FTTContextSignature = SignatureForSnapshot(anchors.FTTCandidateSnapshot);

                if (IsResolvedOutcome(anchors.FTTOutcome))
                {
                    if (previous != null && previous.FTTCandidateBar.HasValue && previous.FTTCandidateBar.Value == anchors.FTTCandidateBar.Value && previous.FTTOutcome == anchors.FTTOutcome && previous.FTTOutcomeSnapshot != null)
                        anchors.FTTOutcomeSnapshot = previous.FTTOutcomeSnapshot.Clone();
                    else
                        anchors.FTTOutcomeSnapshot = currentSnapshot != null ? currentSnapshot.Clone() : null;

                    anchors.FTTOutcomeContextSignature = SignatureForSnapshot(anchors.FTTOutcomeSnapshot);
                }
                else
                {
                    anchors.FTTOutcomeSnapshot = null;
                    anchors.FTTOutcomeContextSignature = "";
                }
            }
            else
            {
                anchors.FTTCandidateSnapshot = null;
                anchors.FTTOutcomeSnapshot = null;
                anchors.FTTContextSignature = "";
                anchors.FTTOutcomeContextSignature = "";
            }
        }

        private string SignatureForSnapshot(xPvaContextSnapshot snapshot)
        {
            return snapshot != null && !string.IsNullOrEmpty(snapshot.ContextSignature) ? snapshot.ContextSignature : "";
        }

        private void UpdateRawCandidateEventLedger(int nodeId, xPvaGaussianAnchorSet anchors, int currentBar)
        {
            if (anchors == null)
                return;

            if (anchors.P3CandidateBar.HasValue)
            {
                RecordRawCandidateEvent(
                    currentBar,
                    nodeId,
                    "P3",
                    "Created",
                    anchors.P3CandidateBasis,
                    anchors.P3ContextSignature,
                    anchors.P3CandidateConfidence.ToString(),
                    anchors.P3CandidateBar,
                    null,
                    anchors.P3PromotionStatus.ToString(),
                    !string.IsNullOrEmpty(anchors.P3PromotionReason) ? anchors.P3PromotionReason : anchors.P3CandidateReason,
                    anchors.P3CandidateSnapshot);

                if (IsResolvedOutcome(anchors.P3Outcome))
                {
                    RecordRawCandidateEvent(
                        anchors.P3OutcomeBar.HasValue ? anchors.P3OutcomeBar.Value : currentBar,
                        nodeId,
                        "P3",
                        anchors.P3Outcome.ToString(),
                        anchors.P3CandidateBasis,
                        !string.IsNullOrEmpty(anchors.P3OutcomeContextSignature) ? anchors.P3OutcomeContextSignature : anchors.P3ContextSignature,
                        anchors.P3CandidateConfidence.ToString(),
                        anchors.P3CandidateBar,
                        anchors.P3OutcomeBar,
                        anchors.P3PromotionStatus.ToString(),
                        !string.IsNullOrEmpty(anchors.P3OutcomeReason) ? anchors.P3OutcomeReason : anchors.P3PromotionReason,
                        anchors.P3OutcomeSnapshot != null ? anchors.P3OutcomeSnapshot : anchors.P3CandidateSnapshot);
                }
                else
                {
                    RecordRawCandidateEvent(
                        currentBar,
                        nodeId,
                        "P3",
                        "Unresolved",
                        anchors.P3CandidateBasis,
                        anchors.P3ContextSignature,
                        anchors.P3CandidateConfidence.ToString(),
                        anchors.P3CandidateBar,
                        null,
                        anchors.P3PromotionStatus.ToString(),
                        !string.IsNullOrEmpty(anchors.P3PromotionReason) ? anchors.P3PromotionReason : anchors.P3OutcomeReason,
                        anchors.P3CandidateSnapshot);
                }
            }

            if (anchors.FTTCandidateBar.HasValue)
            {
                RecordRawCandidateEvent(
                    currentBar,
                    nodeId,
                    "FTT",
                    "Created",
                    anchors.FTTCandidateBasis,
                    anchors.FTTContextSignature,
                    anchors.FTTCandidateConfidence.ToString(),
                    anchors.FTTCandidateBar,
                    null,
                    anchors.FTTPromotionStatus.ToString(),
                    !string.IsNullOrEmpty(anchors.FTTPromotionReason) ? anchors.FTTPromotionReason : anchors.FTTCandidateReason,
                    anchors.FTTCandidateSnapshot);

                if (IsResolvedOutcome(anchors.FTTOutcome))
                {
                    RecordRawCandidateEvent(
                        anchors.FTTOutcomeBar.HasValue ? anchors.FTTOutcomeBar.Value : currentBar,
                        nodeId,
                        "FTT",
                        anchors.FTTOutcome.ToString(),
                        anchors.FTTCandidateBasis,
                        !string.IsNullOrEmpty(anchors.FTTOutcomeContextSignature) ? anchors.FTTOutcomeContextSignature : anchors.FTTContextSignature,
                        anchors.FTTCandidateConfidence.ToString(),
                        anchors.FTTCandidateBar,
                        anchors.FTTOutcomeBar,
                        anchors.FTTPromotionStatus.ToString(),
                        !string.IsNullOrEmpty(anchors.FTTOutcomeReason) ? anchors.FTTOutcomeReason : anchors.FTTPromotionReason,
                        anchors.FTTOutcomeSnapshot != null ? anchors.FTTOutcomeSnapshot : anchors.FTTCandidateSnapshot);
                }
                else
                {
                    RecordRawCandidateEvent(
                        currentBar,
                        nodeId,
                        "FTT",
                        "Unresolved",
                        anchors.FTTCandidateBasis,
                        anchors.FTTContextSignature,
                        anchors.FTTCandidateConfidence.ToString(),
                        anchors.FTTCandidateBar,
                        null,
                        anchors.FTTPromotionStatus.ToString(),
                        !string.IsNullOrEmpty(anchors.FTTPromotionReason) ? anchors.FTTPromotionReason : anchors.FTTOutcomeReason,
                        anchors.FTTCandidateSnapshot);
                }
            }
        }

        private void RecordRawCandidateEvent(
            int bar,
            int nodeId,
            string anchorType,
            string eventType,
            string basis,
            string contextSignature,
            string confidence,
            int? candidateBar,
            int? outcomeBar,
            string currentState,
            string resolutionReason,
            xPvaContextSnapshot contextSnapshot)
        {
            if (!candidateBar.HasValue || string.IsNullOrEmpty(anchorType) || string.IsNullOrEmpty(eventType))
                return;

            string nodeIdText = nodeId.ToString();
            string normalizedBasis = string.IsNullOrEmpty(basis) ? "Unknown" : basis;
            string normalizedSignature = string.IsNullOrEmpty(contextSignature) ? "Unknown" : contextSignature;
            string normalizedConfidence = string.IsNullOrEmpty(confidence) ? "Unknown" : confidence;
            string normalizedCurrentState = string.IsNullOrEmpty(currentState) ? eventType : currentState;
            string normalizedResolutionReason = string.IsNullOrEmpty(resolutionReason) ? "" : resolutionReason;
            string narrative = contextSnapshot != null && !string.IsNullOrEmpty(contextSnapshot.NarrativeState) ? contextSnapshot.NarrativeState : "Unknown";
            string expectation = contextSnapshot != null && !string.IsNullOrEmpty(contextSnapshot.ExpectationState) ? contextSnapshot.ExpectationState : "Unknown";
            string containerLevel = contextSnapshot != null && !string.IsNullOrEmpty(contextSnapshot.ContainerLevel) ? contextSnapshot.ContainerLevel : "Unknown";
            string level3State = contextSnapshot != null && !string.IsNullOrEmpty(contextSnapshot.Level3State) ? contextSnapshot.Level3State : "Unknown";
            string rawKey = nodeIdText
                + "|" + anchorType
                + "|" + candidateBar.Value.ToString()
                + "|" + eventType;

            if (countedRawCandidateEvents.Contains(rawKey))
            {
                xPvaCandidateEventRecord existing;
                if (string.Equals(eventType, "Unresolved", StringComparison.OrdinalIgnoreCase)
                    && rawCandidateEventByKey.TryGetValue(rawKey, out existing))
                {
                    existing.LastTouchedBar = Math.Max(existing.LastTouchedBar, bar);
                    existing.CurrentState = normalizedCurrentState;
                    existing.ResolutionReason = normalizedResolutionReason;
                    existing.Narrative = narrative;
                    existing.Expectation = expectation;
                    existing.ContainerLevel = containerLevel;
                    existing.Level3State = level3State;
                }

                return;
            }

            xPvaCandidateEventRecord record = new xPvaCandidateEventRecord
            {
                Bar = bar,
                NodeId = nodeIdText,
                AnchorType = anchorType,
                EventType = eventType,
                Basis = normalizedBasis,
                ContextSignature = normalizedSignature,
                Confidence = normalizedConfidence,
                CandidateBar = candidateBar,
                OutcomeBar = outcomeBar,
                LastTouchedBar = bar,
                CurrentState = normalizedCurrentState,
                ResolutionReason = normalizedResolutionReason,
                Narrative = narrative,
                Expectation = expectation,
                ContainerLevel = containerLevel,
                Level3State = level3State
            };

            rawCandidateEvents.Add(record);
            countedRawCandidateEvents.Add(rawKey);
            rawCandidateEventByKey[rawKey] = record;
        }

        private void UpdateAnchorBasisStatistics(int nodeId, xPvaGaussianAnchorSet anchors)
        {
            if (anchors == null)
                return;

            if (anchors.P3CandidateBar.HasValue)
                UpdateBasisStatistic(
                    p3BasisStats,
                    nodeId,
                    "P3",
                    anchors.P3CandidateBar.Value,
                    anchors.P3CandidateBasis,
                    anchors.P3CandidateConfidence,
                    anchors.P3Outcome,
                    anchors.P3BarsUntilOutcome);

            if (anchors.FTTCandidateBar.HasValue)
                UpdateBasisStatistic(
                    fttBasisStats,
                    nodeId,
                    "FTT",
                    anchors.FTTCandidateBar.Value,
                    anchors.FTTCandidateBasis,
                    anchors.FTTCandidateConfidence,
                    anchors.FTTOutcome,
                    anchors.FTTBarsUntilOutcome);
        }

        private void UpdateContextSignatureStatistics(int nodeId, xPvaGaussianAnchorSet anchors)
        {
            if (anchors == null)
                return;

            if (anchors.P3CandidateBar.HasValue)
            {
                UpdateContextSignatureStatistic(
                    p3ContextSignatureStats,
                    nodeId,
                    "P3",
                    anchors.P3CandidateBar.Value,
                    anchors.P3CandidateSnapshot,
                    anchors.P3Outcome);
            }

            if (anchors.FTTCandidateBar.HasValue)
            {
                UpdateContextSignatureStatistic(
                    fttContextSignatureStats,
                    nodeId,
                    "FTT",
                    anchors.FTTCandidateBar.Value,
                    anchors.FTTCandidateSnapshot,
                    anchors.FTTOutcome);
            }
        }

        private void UpdateContextSignatureStatistic(
            Dictionary<string, xPvaContextSignatureStatistic> stats,
            int nodeId,
            string candidateKind,
            int candidateBar,
            xPvaContextSnapshot candidateSnapshot,
            xPvaAnchorOutcome outcome)
        {
            if (stats == null)
                return;

            string signature = candidateSnapshot != null && !string.IsNullOrEmpty(candidateSnapshot.ContextSignature)
                ? candidateSnapshot.ContextSignature
                : "Unknown";
            string candidateKey = candidateKind + ":" + nodeId.ToString() + ":" + candidateBar.ToString() + ":" + signature;
            xPvaContextSignatureStatistic stat = GetOrCreateContextSignatureStatistic(stats, signature);

            if (!countedContextCreations.Contains(candidateKey))
            {
                stat.Created++;
                countedContextCreations.Add(candidateKey);
            }

            if (!IsResolvedOutcome(outcome))
                return;

            string outcomeKey = candidateKind + ":" + nodeId.ToString() + ":" + candidateBar.ToString() + ":" + outcome.ToString() + ":" + signature;
            if (countedContextOutcomes.Contains(outcomeKey))
                return;

            if (outcome == xPvaAnchorOutcome.Confirmed)
                stat.Confirmed++;
            else if (outcome == xPvaAnchorOutcome.Rejected)
                stat.Rejected++;
            else if (outcome == xPvaAnchorOutcome.Superseded)
                stat.Superseded++;
            else if (outcome == xPvaAnchorOutcome.Expired)
                stat.Expired++;

            countedContextOutcomes.Add(outcomeKey);
        }

        private xPvaContextSignatureStatistic GetOrCreateContextSignatureStatistic(Dictionary<string, xPvaContextSignatureStatistic> stats, string signature)
        {
            string normalized = string.IsNullOrEmpty(signature) ? "Unknown" : signature;
            xPvaContextSignatureStatistic stat;
            if (stats.TryGetValue(normalized, out stat))
                return stat;

            stat = new xPvaContextSignatureStatistic { Signature = normalized };
            stats[normalized] = stat;
            return stat;
        }

        private void UpdateBasisStatistic(
            Dictionary<string, xPvaAnchorBasisStatistic> stats,
            int nodeId,
            string candidateKind,
            int candidateBar,
            string basis,
            xPvaAnchorConfidence confidence,
            xPvaAnchorOutcome outcome,
            int? barsUntilOutcome)
        {
            if (stats == null || string.IsNullOrEmpty(candidateKind))
                return;

            string normalizedBasis = string.IsNullOrEmpty(basis) ? "Unavailable" : basis;
            string candidateKey = candidateKind + ":" + nodeId.ToString() + ":" + candidateBar.ToString() + ":" + normalizedBasis;
            xPvaAnchorBasisStatistic stat = GetOrCreateBasisStatistic(stats, normalizedBasis);

            if (!countedCandidateCreations.Contains(candidateKey))
            {
                stat.Created++;
                IncrementConfidenceCount(stat, confidence);
                countedCandidateCreations.Add(candidateKey);
            }

            if (!IsResolvedOutcome(outcome))
                return;

            string outcomeKey = candidateKey + ":" + outcome.ToString();
            if (countedCandidateOutcomes.Contains(outcomeKey))
                return;

            int resolvedBars = barsUntilOutcome.HasValue ? Math.Max(0, barsUntilOutcome.Value) : 0;
            stat.TotalBarsToResolution += resolvedBars;

            if (outcome == xPvaAnchorOutcome.Confirmed)
            {
                stat.Confirmed++;
                stat.TotalBarsToConfirmation += resolvedBars;
            }
            else if (outcome == xPvaAnchorOutcome.Rejected)
            {
                stat.Rejected++;
                stat.TotalBarsToRejection += resolvedBars;
            }
            else if (outcome == xPvaAnchorOutcome.Superseded)
            {
                stat.Superseded++;
            }
            else if (outcome == xPvaAnchorOutcome.Expired)
            {
                stat.Expired++;
            }

            countedCandidateOutcomes.Add(outcomeKey);
        }

        private void IncrementConfidenceCount(xPvaAnchorBasisStatistic stat, xPvaAnchorConfidence confidence)
        {
            if (stat == null)
                return;

            if (confidence == xPvaAnchorConfidence.High)
                stat.HighConfidence++;
            else if (confidence == xPvaAnchorConfidence.Medium)
                stat.MediumConfidence++;
            else if (confidence == xPvaAnchorConfidence.Low)
                stat.LowConfidence++;
            else
                stat.UnknownConfidence++;
        }

        private xPvaAnchorBasisStatistic GetOrCreateBasisStatistic(Dictionary<string, xPvaAnchorBasisStatistic> stats, string basis)
        {
            xPvaAnchorBasisStatistic stat;
            if (stats.TryGetValue(basis, out stat))
                return stat;

            stat = new xPvaAnchorBasisStatistic { Basis = basis, BasisName = basis };
            stats[basis] = stat;
            return stat;
        }

        private List<xPvaAnchorBasisStatistic> BuildStatsSnapshot(Dictionary<string, xPvaAnchorBasisStatistic> stats)
        {
            List<xPvaAnchorBasisStatistic> snapshot = new List<xPvaAnchorBasisStatistic>();
            if (stats == null)
                return snapshot;

            foreach (xPvaAnchorBasisStatistic stat in stats.Values)
            {
                xPvaAnchorBasisStatistic clone = stat.Clone();
                clone.BasisName = string.IsNullOrEmpty(clone.BasisName) ? clone.Basis : clone.BasisName;
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
                snapshot.Add(clone);
            }

            snapshot.Sort(delegate (xPvaAnchorBasisStatistic left, xPvaAnchorBasisStatistic right)
            {
                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.Basis, right.Basis, StringComparison.OrdinalIgnoreCase);
            });

            return snapshot;
        }

        private List<xPvaContextSignatureStatistic> BuildContextSignatureStatsSnapshot(Dictionary<string, xPvaContextSignatureStatistic> stats)
        {
            List<xPvaContextSignatureStatistic> snapshot = new List<xPvaContextSignatureStatistic>();
            if (stats == null)
                return snapshot;

            foreach (xPvaContextSignatureStatistic stat in stats.Values)
            {
                xPvaContextSignatureStatistic clone = stat.Clone();
                clone.Unresolved = Math.Max(0, clone.Created - clone.Confirmed - clone.Rejected - clone.Superseded - clone.Expired);
                clone.ConfirmationRate = clone.Created > 0 ? (double)clone.Confirmed / (double)clone.Created : 0.0;
                snapshot.Add(clone);
            }

            snapshot.Sort(delegate (xPvaContextSignatureStatistic left, xPvaContextSignatureStatistic right)
            {
                int createdCompare = right.Created.CompareTo(left.Created);
                if (createdCompare != 0)
                    return createdCompare;

                return string.Compare(left.Signature, right.Signature, StringComparison.OrdinalIgnoreCase);
            });

            return snapshot;
        }

        private bool PromotionOrderingValidForP3(xPvaGaussianAnchorSet anchors)
        {
            if (anchors == null || !anchors.P3CandidateBar.HasValue)
                return false;

            return (!anchors.P1Bar.HasValue || anchors.P1Bar.Value <= anchors.P3CandidateBar.Value)
                && (!anchors.P2Bar.HasValue || anchors.P2Bar.Value <= anchors.P3CandidateBar.Value);
        }

        private bool PromotionOrderingValidForFTT(xPvaGaussianAnchorSet anchors)
        {
            if (anchors == null || !anchors.FTTCandidateBar.HasValue || !anchors.P3CandidateBar.HasValue)
                return false;

            return PromotionOrderingValidForP3(anchors)
                && anchors.P3CandidateBar.Value <= anchors.FTTCandidateBar.Value;
        }

        private void SetP3Promotion(xPvaGaussianAnchorSet anchors, xPvaAnchorPromotionStatus status, int? promotionBar, string reason)
        {
            if (anchors == null)
                return;

            anchors.P3PromotionStatus = status;
            anchors.P3PromotionBar = promotionBar;
            anchors.P3PromotionReason = reason;
        }

        private void SetFTTPromotion(xPvaGaussianAnchorSet anchors, xPvaAnchorPromotionStatus status, int? promotionBar, string reason)
        {
            if (anchors == null)
                return;

            anchors.FTTPromotionStatus = status;
            anchors.FTTPromotionBar = promotionBar;
            anchors.FTTPromotionReason = reason;
        }

        private int AnchorConfidenceRank(xPvaAnchorConfidence confidence)
        {
            if (confidence == xPvaAnchorConfidence.High)
                return 3;

            if (confidence == xPvaAnchorConfidence.Medium)
                return 2;

            if (confidence == xPvaAnchorConfidence.Low)
                return 1;

            return 0;
        }

        private int? InferP3Bar(xPvaGaussianNode node, xPvaGaussianCycle gaussian, string eventText, int? p2Bar, StringBuilder extractionReason)
        {
            if (!p2Bar.HasValue)
                return null;

            if (HasP3Evidence(eventText) && gaussian.EndBar > p2Bar.Value)
                return gaussian.EndBar;

            if (HasP3Evidence(eventText))
                AppendExtractionReason(extractionReason, "P3 proposal rejected because same-bar P2/P3 geometry is not explicitly justified");

            if (gaussian.State == xPvaGaussianCycleState.Complete || gaussian.Phase == xPvaGaussianCyclePhase.FinalDominantLeg)
                AppendExtractionReason(extractionReason, "P3 not assigned from Gaussian completion or FinalDominantLeg alone");

            return null;
        }

        private int? InferFTTBar(
            xPvaGaussianNode node,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            string eventText,
            int? p3Bar,
            StringBuilder extractionReason)
        {
            if (!p3Bar.HasValue)
                return null;

            if (HasFttEvidence(eventText, expectation))
                return gaussian.EndBar;

            if (gaussian.State == xPvaGaussianCycleState.Failed || (expectation != null && expectation.IsExpectationViolated))
                AppendExtractionReason(extractionReason, "FTT not assigned from Gaussian failure or expectation change without explicit post-P3 evidence");

            return null;
        }

        private bool HasStructuralExtremeEvidence(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return false;

            return Contains(eventText, "HHHL")
                || Contains(eventText, "LLLH")
                || Contains(eventText, "OB")
                || Contains(eventText, "IB");
        }

        private bool HasP3Evidence(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return false;

            return Contains(eventText, "RTL")
                || Contains(eventText, "Touch")
                || Contains(eventText, "Return")
                || Contains(eventText, "OB")
                || Contains(eventText, "IB")
                || Contains(eventText, "Boundary");
        }

        private bool HasFttEvidence(string eventText, xPvaGaussianExpectationState expectation)
        {
            if (string.IsNullOrEmpty(eventText) && (expectation == null || !expectation.IsExpectationViolated))
                return false;

            return Contains(eventText, "SFC")
                || Contains(eventText, "FTT")
                || Contains(eventText, "Compressed")
                || Contains(eventText, "Failure")
                || Contains(eventText, "Reversal")
                || Contains(eventText, "REV")
                || Contains(eventText, "OB")
                || Contains(eventText, "IB")
                || Contains(eventText, "VE rejection")
                || Contains(eventText, "VE bounce")
                || (expectation != null && expectation.IsExpectationViolated && !string.IsNullOrEmpty(eventText));
        }

        private bool IsDominantAnchorEvent(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return false;

            return Contains(eventText, "HHHL")
                || Contains(eventText, "LLLH")
                || Contains(eventText, "OB")
                || Contains(eventText, "IB")
                || Contains(eventText, "REV")
                || Contains(eventText, "Reversal")
                || IsLegitimatePeakCandidate(eventText);
        }

        private bool IsFailureLikeEvent(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return false;

            return Contains(eventText, "SFC")
                || Contains(eventText, "Compressed")
                || Contains(eventText, "CR")
                || Contains(eventText, "Reversal")
                || Contains(eventText, "REV")
                || Contains(eventText, "OB")
                || Contains(eventText, "IB")
                || Contains(eventText, "VE");
        }

        private xPvaAnchorConfidence AnchorConfidenceFor(xPvaGaussianAnchorSet anchors, xPvaGaussianCycle gaussian, string peakReason)
        {
            if (anchors == null || !anchors.P1Bar.HasValue)
                return xPvaAnchorConfidence.Unknown;

            if (anchors.AnchorCollapseWarning)
                return xPvaAnchorConfidence.Low;

            if (anchors.P1Status == xPvaAnchorStatus.Confirmed
                && anchors.P2Status == xPvaAnchorStatus.Confirmed
                && anchors.P3Status == xPvaAnchorStatus.Confirmed
                && anchors.FTTStatus == xPvaAnchorStatus.Confirmed
                && anchors.P1Bar.HasValue
                && anchors.P2Bar.HasValue
                && anchors.P3Bar.HasValue
                && anchors.FTTBar.HasValue
                && gaussian.State == xPvaGaussianCycleState.Complete)
                return xPvaAnchorConfidence.High;

            if (anchors.P1Bar.HasValue && anchors.P2Bar.HasValue && anchors.P3Bar.HasValue)
                return xPvaAnchorConfidence.Medium;

            return xPvaAnchorConfidence.Low;
        }

        private xPvaAnchorConfidence GeometryConfidenceFor(xPvaGaussianAnchorSet anchors, xPvaGaussianCycle gaussian)
        {
            if (anchors == null || !anchors.P1Bar.HasValue)
                return xPvaAnchorConfidence.Unknown;

            if (anchors.AnchorCollapseWarning)
                return xPvaAnchorConfidence.Low;

            if (anchors.P2Bar.HasValue && anchors.P3Bar.HasValue && anchors.P2Bar.Value != anchors.P3Bar.Value)
                return gaussian.State == xPvaGaussianCycleState.Complete ? xPvaAnchorConfidence.Medium : xPvaAnchorConfidence.Low;

            return xPvaAnchorConfidence.Low;
        }

        private bool IsAnchorCollapse(xPvaGaussianAnchorSet anchors)
        {
            return anchors != null
                && anchors.P2Bar.HasValue
                && anchors.P3Bar.HasValue
                && anchors.FTTBar.HasValue
                && anchors.P2Bar.Value == anchors.P3Bar.Value
                && anchors.P3Bar.Value == anchors.FTTBar.Value;
        }

        private string AnchorPattern(xPvaGaussianAnchorSet anchors)
        {
            if (anchors == null)
                return "";

            return "P1="
                + FormatNullableBar(anchors.P1Bar)
                + " P2="
                + FormatNullableBar(anchors.P2Bar)
                + " P3="
                + FormatNullableBar(anchors.P3Bar)
                + " FTT="
                + FormatNullableBar(anchors.FTTBar);
        }

        private string AnchorReason(
            xPvaGaussianAnchorSet anchors,
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context,
            string eventText,
            string peakReason,
            string extractionReason)
        {
            var sb = new StringBuilder();
            sb.Append("anchors inferred conservatively from explicit structural event evidence");
            sb.Append("; P1=").Append(anchors.P1Status);
            sb.Append(", P2=").Append(anchors.P2Status);
            sb.Append(", P3=").Append(anchors.P3Status);
            sb.Append(", FTT=").Append(anchors.FTTStatus);

            if (!string.IsNullOrEmpty(extractionReason))
                sb.Append("; ").Append(extractionReason);

            if (narrative != null)
                sb.Append("; narrative=").Append(narrative.State);

            if (expectation != null && expectation.Expectation != xPvaGaussianExpectation.None)
                sb.Append("; expectation=").Append(expectation.Expectation);

            if (container != null && container.Level != xPvaContainerLevel.Unknown)
                sb.Append("; container=").Append(container.Level).Append(" ").Append(container.State);

            if (level3Context != null && level3Context.State != xPvaLevel3ContextState.Unknown)
                sb.Append("; level3=").Append(level3Context.State);

            return sb.ToString();
        }

        private void AppendExtractionReason(StringBuilder sb, string reason)
        {
            if (sb == null || string.IsNullOrEmpty(reason))
                return;

            string existing = "|" + sb.ToString().Replace("; ", "|") + "|";
            if (existing.IndexOf("|" + reason + "|", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (sb.Length > 0)
                sb.Append("; ");

            sb.Append(reason);
        }

        private string FormatNullableBar(int? bar)
        {
            return bar.HasValue ? bar.Value.ToString() : "";
        }

        private string PeakLegitimacyReason(int currentBar, xPvaGaussianCycle gaussian, string eventText)
        {
            if (gaussian == null || !gaussian.HasPeak)
                return "no peak marker";

            if (gaussian.PeakBar != currentBar)
                return "peak candidate event text unavailable for peak bar " + gaussian.PeakBar;

            if (string.IsNullOrEmpty(eventText))
                return "no event text";

            if (eventText.IndexOf("HHHL-R", StringComparison.OrdinalIgnoreCase) >= 0)
                return "rejected: HHHL-R cannot be a peak-volume bar";

            if (eventText.IndexOf("LLLH-R", StringComparison.OrdinalIgnoreCase) >= 0)
                return "rejected: LLLH-R cannot be a peak-volume bar";

            if (IsLegitimatePeakCandidate(eventText))
                return "accepted: event contains B+/b+/R+/r+";

            return "not a peak candidate: event lacks B+/b+/R+/r+";
        }

        private void AssignMemberships(xPvaGaussianNode node)
        {
            for (int bar = node.StartBar; bar <= node.EndBar; bar++)
            {
                AddMembership(node, bar, xPvaGaussianBarRole.Member, "bar is inside Gaussian node range");
            }

            AddSpecialMembership(node, node.P1Bar, xPvaGaussianBarRole.P1, "node P1 / first dominant anchor");
            AddSpecialMembership(node, node.P2Bar, xPvaGaussianBarRole.P2, "node P2 / retrace anchor");
            AddSpecialMembership(node, node.P3Bar, xPvaGaussianBarRole.P3, "node P3 / final dominant anchor");
            AddSpecialMembership(node, node.FTTBar, xPvaGaussianBarRole.FTT, "node failure-through/FTT marker");
            AddSpecialMembership(node, node.PeakBar, xPvaGaussianBarRole.Peak, "node peak marker");
            AddSpecialMembership(node, node.TroughBar, xPvaGaussianBarRole.Trough, "node trough marker");
        }

        private void AddSpecialMembership(xPvaGaussianNode node, int? bar, xPvaGaussianBarRole role, string reason)
        {
            if (!bar.HasValue)
                return;

            AddMembership(node, bar.Value, role, reason);
        }

        private void AddMembership(xPvaGaussianNode node, int bar, xPvaGaussianBarRole role, string reason)
        {
            for (int i = 0; i < memberships.Count; i++)
            {
                if (memberships[i].Bar == bar && memberships[i].GaussianNodeId == node.Id && memberships[i].Role == role)
                {
                    memberships[i].Level = node.Level;
                    memberships[i].RoleReason = reason;
                    return;
                }
            }

            memberships.Add(new xPvaGaussianBarMembership
            {
                Bar = bar,
                GaussianNodeId = node.Id,
                Level = node.Level,
                Role = role,
                RoleReason = reason
            });
        }

        private void LinkParent(xPvaGaussianNode node)
        {
            if (node == null)
                return;

            node.ParentId = null;
            node.Level = Math.Max(1, node.Level);
            node.ChildIds = new List<int>();
        }

        private void UpdateHierarchyCandidateRelationships()
        {
            List<xPvaGaussianNode> nodes = AllNodes();
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].ParentId = null;
                nodes[i].ChildIds = new List<int>();
                nodes[i].ParentCandidateNodeIds = new List<int>();
                nodes[i].ChildCandidateNodeIds = new List<int>();
                nodes[i].HierarchyConfidence = "Unknown";
                nodes[i].HierarchyReason = "no hierarchy candidate relationship";
            }

            for (int childIndex = 0; childIndex < nodes.Count; childIndex++)
            {
                xPvaGaussianNode child = nodes[childIndex];
                if (child == null || child.State != xPvaGaussianNodeState.Complete || !IsKnownDirection(child.Direction))
                    continue;

                for (int parentIndex = 0; parentIndex < nodes.Count; parentIndex++)
                {
                    xPvaGaussianNode parent = nodes[parentIndex];
                    if (parent == null || parent.Id == child.Id || !IsKnownDirection(parent.Direction))
                        continue;

                    if (parent.StartBar >= child.StartBar)
                        continue;

                    if (parent.StartBar > child.StartBar || parent.EndBar < child.EndBar)
                        continue;

                    bool anchorContained = IsInsideParentAnchorRegion(parent, child);
                    bool sameDirection = parent.Direction == child.Direction;
                    string confidence = HierarchyConfidenceFor(anchorContained, sameDirection);
                    string reason = BuildHierarchyReason(parent, child, anchorContained, sameDirection);

                    AddUniqueInt(child.ParentCandidateNodeIds, parent.Id);
                    AddUniqueInt(parent.ChildCandidateNodeIds, child.Id);
                    MergeHierarchyAssessment(child, confidence, reason);
                    MergeHierarchyAssessment(parent, confidence, reason);
                }
            }
        }

        private List<xPvaGaussianNode> AllNodes()
        {
            List<xPvaGaussianNode> nodes = new List<xPvaGaussianNode>();
            for (int i = 0; i < activeNodes.Count; i++)
                AddUniqueNode(nodes, activeNodes[i]);

            for (int i = 0; i < completedNodes.Count; i++)
                AddUniqueNode(nodes, completedNodes[i]);

            return nodes;
        }

        private void AddUniqueNode(List<xPvaGaussianNode> nodes, xPvaGaussianNode node)
        {
            if (nodes == null || node == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == node.Id)
                    return;
            }

            nodes.Add(node);
        }

        private bool IsKnownDirection(xPvaGaussianNodeDirection direction)
        {
            return direction == xPvaGaussianNodeDirection.Black || direction == xPvaGaussianNodeDirection.Red;
        }

        private bool IsInsideParentAnchorRegion(xPvaGaussianNode parent, xPvaGaussianNode child)
        {
            if (parent == null || child == null || parent.Anchors == null || !parent.Anchors.P1Bar.HasValue)
                return false;

            int? parentEnd = null;
            if (parent.Anchors.FTTCandidateBar.HasValue)
                parentEnd = parent.Anchors.FTTCandidateBar.Value;
            else if (parent.Anchors.FTTBar.HasValue)
                parentEnd = parent.Anchors.FTTBar.Value;

            if (!parentEnd.HasValue)
                return false;

            return parent.Anchors.P1Bar.Value <= child.StartBar && parentEnd.Value >= child.EndBar;
        }

        private string HierarchyConfidenceFor(bool anchorContained, bool sameDirection)
        {
            if (anchorContained && sameDirection)
                return "High";

            if (anchorContained || sameDirection)
                return "Medium";

            return "Low";
        }

        private string BuildHierarchyReason(xPvaGaussianNode parent, xPvaGaussianNode child, bool anchorContained, bool sameDirection)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("candidate child node ")
                .Append(child.Id.ToString())
                .Append(" lies inside parent candidate node ")
                .Append(parent.Id.ToString())
                .Append(" time range ")
                .Append(parent.StartBar.ToString())
                .Append("-")
                .Append(parent.EndBar.ToString());

            sb.Append(anchorContained
                ? "; child lies inside parent P1-to-FTT candidate region"
                : "; parent anchor region not fully proven");

            sb.Append(sameDirection
                ? "; parent and child directions match"
                : "; parent and child directions differ but are known");

            return sb.ToString();
        }

        private void MergeHierarchyAssessment(xPvaGaussianNode node, string confidence, string reason)
        {
            if (node == null)
                return;

            if (HierarchyConfidenceRank(confidence) > HierarchyConfidenceRank(node.HierarchyConfidence))
                node.HierarchyConfidence = confidence;

            node.HierarchyReason = AppendUniqueReason(node.HierarchyReason, reason);
        }

        private int HierarchyConfidenceRank(string confidence)
        {
            if (string.Equals(confidence, "High", StringComparison.OrdinalIgnoreCase))
                return 3;

            if (string.Equals(confidence, "Medium", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (string.Equals(confidence, "Low", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 0;
        }

        private string AppendUniqueReason(string existing, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return existing;

            string trimmed = reason.Trim();
            if (string.IsNullOrWhiteSpace(existing)
                || string.Equals(existing, "no hierarchy candidate relationship", StringComparison.OrdinalIgnoreCase)
                || string.Equals(existing, "no hierarchy candidate evaluated", StringComparison.OrdinalIgnoreCase))
                return trimmed;

            if (existing.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                return existing;

            return existing + " | " + trimmed;
        }

        private void AddUniqueInt(List<int> values, int value)
        {
            if (values == null || values.Contains(value))
                return;

            values.Add(value);
        }

        private void FindContainingParent(List<xPvaGaussianNode> nodes, xPvaGaussianNode node, ref xPvaGaussianNode parent, ref int parentRange)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                xPvaGaussianNode candidate = nodes[i];
                if (candidate.Id == node.Id)
                    continue;

                if (candidate.StartBar <= node.StartBar && candidate.EndBar >= node.EndBar)
                {
                    int range = candidate.EndBar - candidate.StartBar;
                    if (range < parentRange)
                    {
                        parent = candidate;
                        parentRange = range;
                    }
                }
            }
        }

        private void MoveCompletedNodeIfNeeded(xPvaGaussianNode node)
        {
            if (node.State != xPvaGaussianNodeState.Complete)
                return;

            if (!completedNodes.Contains(node))
                completedNodes.Add(node);

            activeNodes.Remove(node);
        }

        private xPvaGaussianFractalLedgerSnapshot BuildSnapshot(int bar, string reason)
        {
            List<xPvaGaussianBarMembership> barMemberships = MembershipsForBar(bar);

            return new xPvaGaussianFractalLedgerSnapshot
            {
                Bar = bar,
                ActiveNodes = CloneNodes(activeNodes),
                CompletedNodes = CloneNodes(completedNodes),
                Memberships = CloneMemberships(barMemberships),
                PrimaryNodeIds = PrimaryNodeIds(barMemberships),
                ParentNodeIds = ParentNodeIds(barMemberships),
                ChildNodeIds = ChildNodeIds(barMemberships),
                HierarchyParentCandidateNodeIds = HierarchyParentCandidateNodeIds(barMemberships),
                HierarchyChildCandidateNodeIds = HierarchyChildCandidateNodeIds(barMemberships),
                HierarchyConfidence = HierarchyConfidence(barMemberships),
                HierarchyReason = HierarchyReason(barMemberships),
                Reason = reason
            };
        }

        private List<xPvaGaussianBarMembership> MembershipsForBar(int bar)
        {
            var result = new List<xPvaGaussianBarMembership>();
            for (int i = 0; i < memberships.Count; i++)
            {
                if (memberships[i].Bar == bar)
                    result.Add(memberships[i]);
            }

            return result;
        }

        private List<xPvaGaussianNode> CloneNodes(List<xPvaGaussianNode> nodes)
        {
            var result = new List<xPvaGaussianNode>();
            for (int i = 0; i < nodes.Count; i++)
                result.Add(nodes[i].Clone());

            return result;
        }

        private List<xPvaGaussianBarMembership> CloneMemberships(List<xPvaGaussianBarMembership> source)
        {
            var result = new List<xPvaGaussianBarMembership>();
            for (int i = 0; i < source.Count; i++)
                result.Add(source[i].Clone());

            return result;
        }

        private string PrimaryNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            return UniqueMembershipNodeIds(barMemberships);
        }

        private string ParentNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node != null && node.ParentId.HasValue)
                    AppendUnique(sb, node.ParentId.Value.ToString());
            }

            return sb.ToString();
        }

        private string ChildNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node == null || node.ChildIds == null)
                    continue;

                for (int j = 0; j < node.ChildIds.Count; j++)
                    AppendUnique(sb, node.ChildIds[j].ToString());
            }

            return sb.ToString();
        }

        private string HierarchyParentCandidateNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node == null || node.ParentCandidateNodeIds == null)
                    continue;

                for (int j = 0; j < node.ParentCandidateNodeIds.Count; j++)
                    AppendUnique(sb, node.ParentCandidateNodeIds[j].ToString());
            }

            return sb.ToString();
        }

        private string HierarchyChildCandidateNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node == null || node.ChildCandidateNodeIds == null)
                    continue;

                for (int j = 0; j < node.ChildCandidateNodeIds.Count; j++)
                    AppendUnique(sb, node.ChildCandidateNodeIds[j].ToString());
            }

            return sb.ToString();
        }

        private string HierarchyConfidence(List<xPvaGaussianBarMembership> barMemberships)
        {
            string best = "";
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node == null)
                    continue;

                if (HierarchyConfidenceRank(node.HierarchyConfidence) > HierarchyConfidenceRank(best))
                    best = node.HierarchyConfidence;
            }

            return best;
        }

        private string HierarchyReason(List<xPvaGaussianBarMembership> barMemberships)
        {
            string reason = "";
            for (int i = 0; i < barMemberships.Count; i++)
            {
                xPvaGaussianNode node = FindNodeById(barMemberships[i].GaussianNodeId);
                if (node == null)
                    continue;

                reason = AppendUniqueReason(reason, node.HierarchyReason);
            }

            return reason;
        }

        private string UniqueMembershipNodeIds(List<xPvaGaussianBarMembership> barMemberships)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < barMemberships.Count; i++)
                AppendUnique(sb, barMemberships[i].GaussianNodeId.ToString());

            return sb.ToString();
        }

        private void AppendUnique(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            string token = "|" + value + "|";
            if (("|" + sb.ToString().Replace(",", "|") + "|").IndexOf(token, StringComparison.Ordinal) >= 0)
                return;

            if (sb.Length > 0)
                sb.Append(",");

            sb.Append(value);
        }

        private xPvaGaussianNode FindNodeById(int id)
        {
            for (int i = 0; i < activeNodes.Count; i++)
            {
                if (activeNodes[i].Id == id)
                    return activeNodes[i];
            }

            for (int i = 0; i < completedNodes.Count; i++)
            {
                if (completedNodes[i].Id == id)
                    return completedNodes[i];
            }

            return null;
        }

        private xPvaGaussianNodeDirection DirectionFor(xPvaGaussianCycleDirection direction)
        {
            if (direction == xPvaGaussianCycleDirection.Black)
                return xPvaGaussianNodeDirection.Black;

            if (direction == xPvaGaussianCycleDirection.Red)
                return xPvaGaussianNodeDirection.Red;

            return xPvaGaussianNodeDirection.Unknown;
        }

        private xPvaGaussianNodeState StateFor(xPvaGaussianCycleState state)
        {
            if (state == xPvaGaussianCycleState.Building || state == xPvaGaussianCycleState.RetraceOnly || state == xPvaGaussianCycleState.LeftToRight)
                return xPvaGaussianNodeState.Building;

            if (state == xPvaGaussianCycleState.Complete)
                return xPvaGaussianNodeState.Complete;

            if (state == xPvaGaussianCycleState.Failed)
                return xPvaGaussianNodeState.Failed;

            if (state == xPvaGaussianCycleState.Suspended)
                return xPvaGaussianNodeState.Suspended;

            return xPvaGaussianNodeState.Unknown;
        }

        private xPvaGaussianNodePhase PhaseFor(xPvaGaussianCyclePhase phase)
        {
            if (phase == xPvaGaussianCyclePhase.FirstDominantLeg)
                return xPvaGaussianNodePhase.FirstDominantLeg;

            if (phase == xPvaGaussianCyclePhase.RetraceLeg)
                return xPvaGaussianNodePhase.RetraceLeg;

            if (phase == xPvaGaussianCyclePhase.FinalDominantLeg)
                return xPvaGaussianNodePhase.FinalDominantLeg;

            return xPvaGaussianNodePhase.Unknown;
        }

        private xPvaGaussianAmbiguity AmbiguityFor(
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context)
        {
            string text = "";
            if (gaussian != null)
                text += " " + gaussian.State + " " + gaussian.Reason + " " + gaussian.Pattern;
            if (narrative != null)
                text += " " + narrative.State + " " + narrative.Reason + " " + narrative.Pattern;
            if (container != null)
                text += " " + container.Direction + " " + container.State + " " + container.Reason + " " + container.Pattern;
            if (level3Context != null)
                text += " " + level3Context.State + " " + level3Context.Reason + " " + level3Context.Pattern;

            if (Contains(text, "GeometryConflict"))
                return xPvaGaussianAmbiguity.GeometryConflict;

            if (Contains(text, "Gaussian coherent") || Contains(text, "price invalid"))
                return xPvaGaussianAmbiguity.GaussianCoherentButPriceInvalid;

            if (Contains(text, "LeftToRight") || Contains(text, "Lateral"))
                return xPvaGaussianAmbiguity.LeftToRight;

            if (Contains(text, "Mixed") || Contains(text, "Insufficient"))
                return xPvaGaussianAmbiguity.InsufficientContext;

            return xPvaGaussianAmbiguity.None;
        }

        private bool Contains(string text, string value)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private double ConfidenceFor(xPvaGaussianNodeState state, xPvaGaussianAmbiguity ambiguity)
        {
            if (ambiguity != xPvaGaussianAmbiguity.None)
                return 0.35;

            if (state == xPvaGaussianNodeState.Complete)
                return 0.75;

            if (state == xPvaGaussianNodeState.Building)
                return 0.50;

            return 0.0;
        }

        private string BuildNodeReason(
            xPvaGaussianCycle gaussian,
            xPvaGaussianNarrative narrative,
            xPvaGaussianExpectationState expectation,
            xPvaContainerContext container,
            xPvaLevel3Context level3Context)
        {
            string reason = "Gaussian ledger node from cycle state " + gaussian.State + "; " + gaussian.Reason;

            if (narrative != null && !string.IsNullOrEmpty(narrative.Reason))
                reason += " | Narrative: " + narrative.Reason;

            if (expectation != null && expectation.Expectation != xPvaGaussianExpectation.None)
                reason += " | Expectation: " + expectation.Expectation;

            if (container != null && container.Level != xPvaContainerLevel.Unknown)
                reason += " | Container context: " + container.Level + " " + container.State;

            if (level3Context != null && level3Context.State != xPvaLevel3ContextState.Unknown)
                reason += " | L3 context: " + level3Context.State;

            return reason;
        }

        private string AppendReason(string existing, string addition)
        {
            if (string.IsNullOrEmpty(existing))
                return addition;

            return existing + " | " + addition;
        }
    }
}
