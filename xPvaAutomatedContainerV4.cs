#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.xPva.Engine;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaAutomatedContainerV4 : Indicator
    {
        private enum CandidateDirection
        {
            Up,
            Down
        }

        private enum CandidateStatus
        {
            Provisional,
            Committed,
            Broken,
            Discarded
        }

        private enum FormationStatus
        {
            Pending,
            ResolvedUp,
            ResolvedDown,
            Failed
        }

        private enum OrdinaryStatus
        {
            Active,
            Invalidated,
            Broken
        }

        private sealed class BarSnapshot
        {
            public readonly int Index;
            public readonly DateTime Time;
            public readonly double Open;
            public readonly double High;
            public readonly double Low;
            public readonly double Close;
            public readonly double Volume;
            public readonly xPvaBarRelation Relation;

            public BarSnapshot(int index, DateTime time, double open, double high, double low,
                double close, double volume, xPvaBarRelation relation)
            {
                Index = index;
                Time = time;
                Open = open;
                High = high;
                Low = low;
                Close = close;
                Volume = volume;
                Relation = relation;
            }
        }

        private sealed class ProvisionalCandidate
        {
            public readonly int Id;
            public readonly CandidateDirection Direction;
            public readonly int OriginBar;
            public readonly double OriginPrice;
            public CandidateStatus Status;
            public int SupportBar;
            public double SupportPrice;
            public int EndBar;
            public int BreakBar;
            public int Level;
            public int JoinedParentId;

            public ProvisionalCandidate(int id, CandidateDirection direction, int originBar,
                double originPrice, int supportBar, double supportPrice)
            {
                Id = id;
                Direction = direction;
                OriginBar = originBar;
                OriginPrice = originPrice;
                SupportBar = supportBar;
                SupportPrice = supportPrice;
                EndBar = supportBar;
                BreakBar = -1;
                Level = 1;
                Status = CandidateStatus.Provisional;
            }
        }

        private sealed class AmbiguousFormation
        {
            public readonly int Id;
            public readonly int OriginBar;
            public readonly double BoundaryHigh;
            public readonly double BoundaryLow;
            public readonly ProvisionalCandidate Up;
            public readonly ProvisionalCandidate Down;
            public FormationStatus Status;
            public int EndBar;
            public int ContainedBarCount;
            public bool LateralConfirmed;
            public bool LateralClosed;
            public int LateralEndBar;
            public int LateralBreakBar;

            public AmbiguousFormation(int id, BarSnapshot origin, BarSnapshot firstContained,
                ProvisionalCandidate up, ProvisionalCandidate down)
            {
                Id = id;
                OriginBar = origin.Index;
                BoundaryHigh = origin.High;
                BoundaryLow = origin.Low;
                Up = up;
                Down = down;
                Status = FormationStatus.Pending;
                EndBar = firstContained.Index;
                ContainedBarCount = 1;
                LateralEndBar = firstContained.Index;
                LateralBreakBar = -1;
            }
        }

        private sealed class OrdinaryContainer
        {
            public readonly int Id;
            public readonly CandidateDirection Direction;
            public readonly int OriginBar;
            public readonly double RtlOriginPrice;
            public int P2Bar;
            public double P2Price;
            public readonly double LtlOriginPrice;
            public readonly List<OrdinaryOuterAnchor> OuterAnchors;
            public OrdinaryStatus Status;
            public int SupportBar;
            public double SupportPrice;
            public int EndBar;
            public int BreakBar;
            public int OwnerFormationId;
            public int OwnerOrdinaryContainerId;
            public int ResponseSourceContainerId;
            public int ParentFormationId;
            public int ParentOrdinaryContainerId;
            public int PromotedFromFormationId;
            public int JoinedParentId;
            public int Level;

            public OrdinaryContainer(int id, CandidateDirection direction, BarSnapshot origin,
                BarSnapshot end, BarSnapshot outerAnchor)
            {
                Id = id;
                Direction = direction;
                OriginBar = origin.Index;
                EndBar = end.Index;
                RtlOriginPrice = direction == CandidateDirection.Up ? origin.Low : origin.High;
                SupportBar = end.Index;
                SupportPrice = direction == CandidateDirection.Up ? end.Low : end.High;
                P2Bar = outerAnchor.Index;
                P2Price = direction == CandidateDirection.Up ? outerAnchor.High : outerAnchor.Low;
                LtlOriginPrice = direction == CandidateDirection.Up ? origin.High : origin.Low;
                OuterAnchors = new List<OrdinaryOuterAnchor>
                {
                    new OrdinaryOuterAnchor(origin.Index, LtlOriginPrice, false)
                };
                Status = OrdinaryStatus.Active;
                BreakBar = -1;
                Level = 1;
            }
        }

        private sealed class OrdinaryOuterAnchor
        {
            public readonly int Bar;
            public readonly double Price;
            public readonly bool IsVe;

            public OrdinaryOuterAnchor(int bar, double price, bool isVe)
            {
                Bar = bar;
                Price = price;
                IsVe = isVe;
            }
        }

        private sealed class JoinedContainer
        {
            public int Id;
            public CandidateDirection Direction;
            public int Level;
            public int ParentFormationId;
            public int ParentOrdinaryContainerId;
            public int LeftId;
            public int MiddleId;
            public int RightId;
            public int StartBar;
            public int EndBar;
            public double P1Price;
            public int P2Bar;
            public double P2Price;
            public int P3Bar;
            public double P3Price;
            public double Slope;
            public string Kind;
            public readonly List<OrdinaryOuterAnchor> OuterAnchors
                = new List<OrdinaryOuterAnchor>();
        }

        private sealed class AuditFinding
        {
            public string Severity;
            public string Code;
            public string Subject;
            public string Message;
        }

        private readonly List<BarSnapshot> snapshots = new List<BarSnapshot>();
        private readonly List<AmbiguousFormation> formations = new List<AmbiguousFormation>();
        private readonly List<OrdinaryContainer> ordinaryContainers = new List<OrdinaryContainer>();
        private readonly List<JoinedContainer> joinedContainers = new List<JoinedContainer>();
        private readonly List<AuditFinding> auditFindings = new List<AuditFinding>();
        private xPvaDiscreteEventEngine eventEngine;
        private int nextFormationId;
        private int nextCandidateId;
        private int nextOrdinaryContainerId;
        private int nextJoinedContainerId;
        private bool ordinaryUpTerminatedThisBar;
        private bool ordinaryDownTerminatedThisBar;
        private int ordinaryUpTerminatedContainerIdThisBar;
        private int ordinaryDownTerminatedContainerIdThisBar;
        private string lastBuildSignature;
        private SolidColorBrush upBrushDx;
        private SolidColorBrush downBrushDx;
        private SolidColorBrush lateralOutlineBrushDx;
        private StrokeStyle dashStrokeStyle;
        private StrokeStyle dotStrokeStyle;

        [NinjaScriptProperty]
        [Display(Name = "Debug", GroupName = "Diagnostics", Order = 1)]
        public bool Debug { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start Bar", GroupName = "Diagnostics", Order = 2)]
        public int StartBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End Bar", GroupName = "Diagnostics", Order = 3)]
        public int EndBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable JSON Export", GroupName = "Export", Order = 1)]
        public bool EnableJsonExport { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Export Folder", GroupName = "Export", Order = 2)]
        public string ExportFolder { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Export File Name", GroupName = "Export", Order = 3)]
        public string ExportFileName { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaAutomatedContainerV4";
                Description = "APVA V4 stage 14: deterministic audit and JSON export.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                PrintTo = PrintTo.OutputTab2;
                Debug = true;
                StartBar = 0;
                EndBar = 0;
                EnableJsonExport = false;
                ExportFolder = Path.Combine(Core.Globals.UserDataDir, "APVAExports");
                ExportFileName = string.Empty;
            }
            else if (State == State.DataLoaded)
            {
                eventEngine = new xPvaDiscreteEventEngine(TickSize);
                lastBuildSignature = null;
            }
            else if (State == State.Terminated)
            {
                DisposeRenderResources();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || eventEngine == null)
                return;

            int start;
            int end;
            string rejection;
            if (!TryResolveBounds(out start, out end, out rejection))
            {
                if (Debug && CurrentBar >= Math.Max(1, EndBar))
                    Print("[APVA V4] bounds rejected: " + rejection);
                return;
            }

            if (CurrentBar < end)
                return;

            string signature = start.ToString(CultureInfo.InvariantCulture)
                + ":" + end.ToString(CultureInfo.InvariantCulture)
                + ":" + Debug.ToString()
                + ":" + EnableJsonExport.ToString()
                + ":" + (ExportFolder ?? string.Empty)
                + ":" + (ExportFileName ?? string.Empty);
            if (signature == lastBuildSignature)
                return;

            Replay(start, end);
            lastBuildSignature = signature;
            ForceRefresh();
        }

        public override void OnRenderTargetChanged()
        {
            DisposeRenderResources();
            if (RenderTarget == null)
                return;

            upBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 102, 255, 220));
            downBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(220, 38, 38, 220));
            lateralOutlineBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(80, 80, 80, 150));
            dashStrokeStyle = new StrokeStyle(Core.Globals.D2DFactory,
                new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
            dotStrokeStyle = new StrokeStyle(Core.Globals.D2DFactory,
                new StrokeStyleProperties
                {
                    DashStyle = SharpDX.Direct2D1.DashStyle.Dot,
                    DashCap = CapStyle.Round,
                    StartCap = CapStyle.Round,
                    EndCap = CapStyle.Round
                });
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (chartControl == null || chartScale == null || ChartBars == null || RenderTarget == null)
                return;
            if (upBrushDx == null || downBrushDx == null || lateralOutlineBrushDx == null)
                OnRenderTargetChanged();
            if (upBrushDx == null || downBrushDx == null || lateralOutlineBrushDx == null)
                return;

            AntialiasMode priorMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            for (int i = 0; i < formations.Count; i++)
            {
                AmbiguousFormation formation = formations[i];
                DrawLateral(chartControl, chartScale, formation);
                DrawCandidate(chartControl, chartScale, formation, formation.Up);
                DrawCandidate(chartControl, chartScale, formation, formation.Down);
            }
            for (int i = 0; i < ordinaryContainers.Count; i++)
                DrawOrdinaryContainer(chartControl, chartScale, ordinaryContainers[i]);
            for (int i = 0; i < joinedContainers.Count; i++)
                DrawJoinedContainer(chartControl, chartScale, joinedContainers[i]);
            RenderTarget.AntialiasMode = priorMode;
        }

        private void DrawJoinedContainer(ChartControl chartControl, ChartScale chartScale,
            JoinedContainer container)
        {
            if (container.EndBar < ChartBars.FromIndex || container.StartBar > ChartBars.ToIndex)
                return;
            float x1 = chartControl.GetXByBarIndex(ChartBars, container.StartBar);
            float x2 = chartControl.GetXByBarIndex(ChartBars, container.EndBar);
            double rtlEnd = container.P1Price
                + container.Slope * (container.EndBar - container.StartBar);
            SolidColorBrush brush = container.Direction == CandidateDirection.Up ? upBrushDx : downBrushDx;
            StrokeStyle strokeStyle = StrokeForLevel(container.Level);
            float width = WidthForLevel(container.Level);
            RenderTarget.DrawLine(
                new Vector2(x1, chartScale.GetYByValue(container.P1Price)),
                new Vector2(x2, chartScale.GetYByValue(rtlEnd)),
                brush,
                width,
                strokeStyle);

            for (int i = 0; i < container.OuterAnchors.Count; i++)
            {
                OrdinaryOuterAnchor anchor = container.OuterAnchors[i];
                int segmentEndBar = i + 1 < container.OuterAnchors.Count
                    ? container.OuterAnchors[i + 1].Bar
                    : container.EndBar;
                double segmentEndPrice = anchor.Price
                    + container.Slope * (segmentEndBar - anchor.Bar);
                float outerX1 = chartControl.GetXByBarIndex(ChartBars, anchor.Bar);
                float outerX2 = chartControl.GetXByBarIndex(ChartBars, segmentEndBar);
                RenderTarget.DrawLine(
                    new Vector2(outerX1, chartScale.GetYByValue(anchor.Price)),
                    new Vector2(outerX2, chartScale.GetYByValue(segmentEndPrice)),
                    brush,
                    width,
                    strokeStyle);
            }
        }

        private void DrawOrdinaryContainer(ChartControl chartControl, ChartScale chartScale,
            OrdinaryContainer container)
        {
            if (container.EndBar < ChartBars.FromIndex || container.OriginBar > ChartBars.ToIndex)
                return;
            float x1 = chartControl.GetXByBarIndex(ChartBars, container.OriginBar);
            float x2 = chartControl.GetXByBarIndex(ChartBars, container.EndBar);
            double rtlEndPrice = ProjectOrdinary(container, container.EndBar);
            SolidColorBrush brush = container.Direction == CandidateDirection.Up ? upBrushDx : downBrushDx;
            StrokeStyle strokeStyle = StrokeForLevel(container.Level);
            float width = WidthForLevel(container.Level);
            RenderTarget.DrawLine(
                new Vector2(x1, chartScale.GetYByValue(container.RtlOriginPrice)),
                new Vector2(x2, chartScale.GetYByValue(rtlEndPrice)),
                brush,
                width,
                strokeStyle);
            double slope = OrdinarySlope(container);
            for (int i = 0; i < container.OuterAnchors.Count; i++)
            {
                OrdinaryOuterAnchor anchor = container.OuterAnchors[i];
                int segmentEndBar = i + 1 < container.OuterAnchors.Count
                    ? container.OuterAnchors[i + 1].Bar
                    : container.EndBar;
                if (segmentEndBar < anchor.Bar)
                    continue;
                double segmentEndPrice = anchor.Price + slope * (segmentEndBar - anchor.Bar);
                float outerX1 = chartControl.GetXByBarIndex(ChartBars, anchor.Bar);
                float outerX2 = chartControl.GetXByBarIndex(ChartBars, segmentEndBar);
                RenderTarget.DrawLine(
                    new Vector2(outerX1, chartScale.GetYByValue(anchor.Price)),
                    new Vector2(outerX2, chartScale.GetYByValue(segmentEndPrice)),
                    brush,
                    width,
                    strokeStyle);
            }
        }

        private void DrawLateral(ChartControl chartControl, ChartScale chartScale,
            AmbiguousFormation formation)
        {
            if (!formation.LateralConfirmed)
                return;
            if (formation.LateralEndBar < ChartBars.FromIndex || formation.OriginBar > ChartBars.ToIndex)
                return;

            float x1 = chartControl.GetXByBarIndex(ChartBars, formation.OriginBar);
            float x2 = chartControl.GetXByBarIndex(ChartBars, formation.LateralEndBar);
            float y1 = chartScale.GetYByValue(formation.BoundaryHigh);
            float y2 = chartScale.GetYByValue(formation.BoundaryLow);
            var rectangle = new RectangleF(
                Math.Min(x1, x2),
                Math.Min(y1, y2),
                Math.Abs(x2 - x1),
                Math.Abs(y2 - y1));
            RenderTarget.DrawRectangle(rectangle, lateralOutlineBrushDx, 1.0f);
        }

        private void DrawCandidate(ChartControl chartControl, ChartScale chartScale,
            AmbiguousFormation formation, ProvisionalCandidate candidate)
        {
            if (candidate.Status == CandidateStatus.Discarded)
                return;
            if (candidate.EndBar < ChartBars.FromIndex || candidate.OriginBar > ChartBars.ToIndex)
                return;

            int endBar = Math.Max(candidate.SupportBar, candidate.EndBar);
            double rtlEnd = Project(candidate, endBar);
            double outerOrigin = candidate.Direction == CandidateDirection.Up
                ? formation.BoundaryHigh
                : formation.BoundaryLow;
            double outerEnd = outerOrigin + (rtlEnd - candidate.OriginPrice);
            float x1 = chartControl.GetXByBarIndex(ChartBars, candidate.OriginBar);
            float x2 = chartControl.GetXByBarIndex(ChartBars, endBar);
            SolidColorBrush brush = candidate.Direction == CandidateDirection.Up ? upBrushDx : downBrushDx;
            StrokeStyle strokeStyle = StrokeForLevel(candidate.Level);
            float width = WidthForLevel(candidate.Level);

            RenderTarget.DrawLine(
                new Vector2(x1, chartScale.GetYByValue(candidate.OriginPrice)),
                new Vector2(x2, chartScale.GetYByValue(rtlEnd)),
                brush,
                width,
                strokeStyle);
            RenderTarget.DrawLine(
                new Vector2(x1, chartScale.GetYByValue(outerOrigin)),
                new Vector2(x2, chartScale.GetYByValue(outerEnd)),
                brush,
                width,
                strokeStyle);
        }

        private bool TryResolveBounds(out int start, out int end, out string rejection)
        {
            start = Debug ? StartBar : Math.Max(0, CurrentBar - 300);
            end = Debug ? (EndBar <= 0 ? CurrentBar : EndBar) : CurrentBar;
            rejection = null;

            if (start < 0)
            {
                rejection = "Start Bar must be non-negative";
                return false;
            }
            if (end < start)
            {
                rejection = "End Bar must be greater than or equal to Start Bar";
                return false;
            }
            if (end > CurrentBar)
                return false;
            return true;
        }

        private void Replay(int start, int end)
        {
            snapshots.Clear();
            formations.Clear();
            ordinaryContainers.Clear();
            joinedContainers.Clear();
            nextFormationId = 1;
            nextCandidateId = 1;
            nextOrdinaryContainerId = 1;
            nextJoinedContainerId = 1;

            for (int bar = start; bar <= end; bar++)
                snapshots.Add(CreateSnapshot(bar));

            Print("[APVA V4] replay begin build=stage14-audit-export-v1 start=" + start
                + " end=" + end + " count=" + snapshots.Count);

            AmbiguousFormation active = null;
            BarSnapshot pendingOrdinaryOrigin = null;
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot current = snapshots[i];
                Print(FormatSnapshot(current));
                AdvanceConfirmedLaterals(current);
                ordinaryUpTerminatedThisBar = false;
                ordinaryDownTerminatedThisBar = false;
                ordinaryUpTerminatedContainerIdThisBar = 0;
                ordinaryDownTerminatedContainerIdThisBar = 0;
                AdvanceOrdinaryContainers(current);

                bool pendingAtBarStart = active != null && active.Status == FormationStatus.Pending;
                CandidateDirection? ownerAtBarStart = ActiveFormationDirection(active);
                int ownerFormationIdAtStart = ActiveFormationId(active);

                if (active != null)
                {
                    if (active.Status == FormationStatus.Pending)
                        ProcessFormation(active, current);
                    else
                        ProcessCommittedFormation(active, current);
                }

                if ((active == null || IsFormationClosed(active)) && i > 0)
                {
                    BarSnapshot previous = snapshots[i - 1];
                    if (CanSeedAmbiguity(previous, current))
                        active = CreateFormation(previous, current);
                }

                bool pendingAtBarEnd = active != null && active.Status == FormationStatus.Pending;
                CandidateDirection? ownerAtBarEnd = ActiveFormationDirection(active);
                int ownerFormationIdAtEnd = ActiveFormationId(active);
                if (i > 0)
                {
                    if (pendingOrdinaryOrigin != null && !IsInsideLike(current.Relation))
                    {
                        ProcessOrdinaryConstruction(pendingOrdinaryOrigin, current,
                            pendingAtBarStart || pendingAtBarEnd,
                            ownerAtBarStart.HasValue ? ownerAtBarStart : ownerAtBarEnd,
                            ownerFormationIdAtStart != 0 ? ownerFormationIdAtStart : ownerFormationIdAtEnd,
                            "skipped inside-like construction");
                        pendingOrdinaryOrigin = null;
                    }
                    ProcessOrdinaryConstruction(snapshots[i - 1], current,
                        pendingAtBarStart || pendingAtBarEnd,
                        ownerAtBarStart.HasValue ? ownerAtBarStart : ownerAtBarEnd,
                        ownerFormationIdAtStart != 0 ? ownerFormationIdAtStart : ownerFormationIdAtEnd,
                        "adjacent construction");

                    if (IsInsideLike(current.Relation)
                        && !(pendingAtBarStart || pendingAtBarEnd)
                        && pendingOrdinaryOrigin == null)
                        pendingOrdinaryOrigin = snapshots[i - 1];
                }
            }

            DeriveRecursiveHierarchy();
            DeriveFormationLineageJoins();
            DeriveOrdinaryTriadJoins();
            RunInvariantAudit();
            string fingerprint = ComputeStateFingerprint();
            Print("[APVA V4] audit summary errors=" + CountAuditSeverity("Error")
                + " warnings=" + CountAuditSeverity("Warning")
                + " fingerprint=" + fingerprint);
            if (EnableJsonExport)
                ExportJson(start, end, fingerprint);

            Print("[APVA V4] replay end start=" + start + " end=" + end
                + " formations=" + formations.Count
                + " ordinaryContainers=" + ordinaryContainers.Count
                + " joinedContainers=" + joinedContainers.Count);
        }

        private void DeriveFormationLineageJoins()
        {
            for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
            {
                AmbiguousFormation formation = formations[formationIndex];
                ProvisionalCandidate root = CommittedCandidate(formation);
                if (root == null)
                    continue;

                OrdinaryContainer middle = null;
                OrdinaryContainer right = null;
                for (int i = 0; i < ordinaryContainers.Count; i++)
                {
                    OrdinaryContainer candidate = ordinaryContainers[i];
                    if (candidate.OwnerFormationId != formation.Id
                        || candidate.Direction == root.Direction
                        || candidate.JoinedParentId != 0)
                        continue;
                    OrdinaryContainer response = FindResponseTo(candidate.Id, root.Direction);
                    if (response == null || response.JoinedParentId != 0)
                        continue;
                    middle = candidate;
                    right = response;
                    break;
                }
                if (middle == null || right == null)
                    continue;

                string rejection;
                JoinedContainer joined;
                if (!TryBuildFormationLineageJoin(formation, root, middle, right,
                    out joined, out rejection))
                {
                    Print("[APVA V4] formation lineage join rejected formation=" + formation.Id
                        + " triad=root," + middle.Id + "," + right.Id
                        + " reason=" + rejection);
                    continue;
                }

                joinedContainers.Add(joined);
                root.JoinedParentId = joined.Id;
                root.Level = joined.Level + 1;
                AttachLineageComponent(joined, middle);
                AttachLineageComponent(joined, right);
                Print("[APVA V4] formation lineage join accepted id=" + joined.Id
                    + " formation=" + formation.Id + " origin=" + joined.StartBar
                    + " triad=root," + middle.Id + "," + right.Id
                    + " direction=" + joined.Direction + " level=" + joined.Level
                    + " p3=" + joined.P3Bar + "@" + FormatPrice(joined.P3Price));

                OrdinaryContainer lastResponse = right;
                while (true)
                {
                    OrdinaryContainer nextOpposite = FindResponseTo(lastResponse.Id,
                        Opposite(lastResponse.Direction));
                    if (nextOpposite == null || nextOpposite.JoinedParentId != 0)
                        break;
                    OrdinaryContainer nextResponse = FindResponseTo(nextOpposite.Id,
                        root.Direction);
                    if (nextResponse == null || nextResponse.JoinedParentId != 0)
                        break;
                    if (!TryExtendFormationLineageJoin(joined, root, nextOpposite,
                        nextResponse, out rejection))
                    {
                        Print("[APVA V4] formation lineage extension rejected join=" + joined.Id
                            + " pair=" + nextOpposite.Id + "," + nextResponse.Id
                            + " reason=" + rejection);
                        break;
                    }

                    AttachLineageComponent(joined, nextOpposite);
                    AttachLineageComponent(joined, nextResponse);
                    lastResponse = nextResponse;
                    Print("[APVA V4] formation lineage extended join=" + joined.Id
                        + " pair=" + nextOpposite.Id + "," + nextResponse.Id
                        + " end=" + joined.EndBar + " p3=" + joined.P3Bar
                        + "@" + FormatPrice(joined.P3Price));
                }
            }
        }

        private bool TryBuildFormationLineageJoin(AmbiguousFormation formation,
            ProvisionalCandidate root, OrdinaryContainer middle, OrdinaryContainer right,
            out JoinedContainer joined, out string rejection)
        {
            joined = null;
            rejection = null;
            if (middle.ResponseSourceContainerId != 0
                || right.ResponseSourceContainerId != middle.Id)
            {
                rejection = "ordinary response chain does not begin at formation root";
                return false;
            }

            bool p1Valid = root.Direction == CandidateDirection.Up
                ? root.OriginPrice < middle.RtlOriginPrice
                    && middle.RtlOriginPrice > right.RtlOriginPrice
                : root.OriginPrice > middle.RtlOriginPrice
                    && middle.RtlOriginPrice < right.RtlOriginPrice;
            if (!p1Valid)
            {
                rejection = "formation lineage P1 geometry failed";
                return false;
            }

            bool middleFtt = root.Direction == CandidateDirection.Up
                ? middle.P2Price > root.OriginPrice + TickSize * 0.5
                : middle.P2Price < root.OriginPrice - TickSize * 0.5;
            if (!middleFtt)
            {
                rejection = "middle FTT failed";
                return false;
            }

            double rootP2 = root.Direction == CandidateDirection.Up
                ? formation.BoundaryHigh
                : formation.BoundaryLow;
            bool sameDirectionExtreme = root.Direction == CandidateDirection.Up
                ? rootP2 - right.P2Price <= TickSize * 0.5
                : right.P2Price - rootP2 <= TickSize * 0.5;
            if (!sameDirectionExtreme)
            {
                rejection = "formation lineage same-direction extreme failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            double slope;
            if (!TryFitFormationJoinedRtl(root, right, out p3Bar, out p3Price,
                out slope, out rejection))
                return false;

            joined = new JoinedContainer
            {
                Id = nextJoinedContainerId++,
                Kind = "FormationLineage",
                Direction = root.Direction,
                Level = 1,
                LeftId = 0,
                MiddleId = middle.Id,
                RightId = right.Id,
                StartBar = root.OriginBar,
                EndBar = right.EndBar,
                P1Price = root.OriginPrice,
                P2Bar = formation.OriginBar,
                P2Price = root.Direction == CandidateDirection.Up
                    ? formation.BoundaryHigh
                    : formation.BoundaryLow,
                P3Bar = p3Bar,
                P3Price = p3Price,
                Slope = slope
            };
            BuildJoinedOuterAnchors(joined);
            return true;
        }

        private bool TryExtendFormationLineageJoin(JoinedContainer joined,
            ProvisionalCandidate root, OrdinaryContainer middle, OrdinaryContainer right,
            out string rejection)
        {
            rejection = null;
            bool p1Valid = root.Direction == CandidateDirection.Up
                ? joined.P1Price < middle.RtlOriginPrice
                    && middle.RtlOriginPrice > right.RtlOriginPrice
                : joined.P1Price > middle.RtlOriginPrice
                    && middle.RtlOriginPrice < right.RtlOriginPrice;
            if (!p1Valid)
            {
                rejection = "continuation P1 geometry failed";
                return false;
            }
            bool middleFtt = root.Direction == CandidateDirection.Up
                ? middle.P2Price > joined.P1Price + TickSize * 0.5
                : middle.P2Price < joined.P1Price - TickSize * 0.5;
            if (!middleFtt)
            {
                rejection = "continuation middle FTT failed";
                return false;
            }

            bool sameDirectionExtreme = root.Direction == CandidateDirection.Up
                ? joined.P2Price - right.P2Price <= TickSize * 0.5
                : right.P2Price - joined.P2Price <= TickSize * 0.5;
            if (!sameDirectionExtreme)
            {
                rejection = "continuation same-direction extreme failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            double slope;
            if (!TryFitFormationJoinedRtl(root, right, out p3Bar, out p3Price,
                out slope, out rejection))
                return false;
            joined.RightId = right.Id;
            joined.EndBar = right.EndBar;
            joined.P3Bar = p3Bar;
            joined.P3Price = p3Price;
            joined.Slope = slope;
            joined.OuterAnchors.Clear();
            BuildJoinedOuterAnchors(joined);
            return true;
        }

        private bool TryFitFormationJoinedRtl(ProvisionalCandidate root,
            OrdinaryContainer right, out int p3Bar, out double p3Price, out double slope,
            out string rejection)
        {
            int startBar = root.OriginBar;
            double p1 = root.OriginPrice;
            p3Bar = right.SupportBar;
            p3Price = right.SupportPrice;
            slope = (p3Price - p1) / Math.Max(1, p3Bar - startBar);
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= startBar || bar.Index > right.EndBar)
                    continue;
                double wick = root.Direction == CandidateDirection.Up ? bar.Low : bar.High;
                double projected = p1 + slope * (bar.Index - startBar);
                bool traversed = root.Direction == CandidateDirection.Up
                    ? wick < projected - TickSize * 0.5
                    : wick > projected + TickSize * 0.5;
                if (!traversed)
                    continue;
                double candidateSlope = (wick - p1) / (bar.Index - startBar);
                if ((root.Direction == CandidateDirection.Up && candidateSlope < slope)
                    || (root.Direction == CandidateDirection.Down && candidateSlope > slope))
                {
                    slope = candidateSlope;
                    p3Bar = bar.Index;
                    p3Price = wick;
                }
            }

            bool directional = root.Direction == CandidateDirection.Up
                ? p3Price > p1 + TickSize * 0.5
                : p3Price < p1 - TickSize * 0.5;
            if (!directional)
            {
                rejection = "formation joined P1/P3 geometry is not directional";
                return false;
            }
            rejection = null;
            return true;
        }

        private OrdinaryContainer FindResponseTo(int sourceId, CandidateDirection direction)
        {
            OrdinaryContainer selected = null;
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer candidate = ordinaryContainers[i];
                if (candidate.ResponseSourceContainerId != sourceId
                    || candidate.Direction != direction)
                    continue;
                if (selected == null || candidate.OriginBar < selected.OriginBar)
                    selected = candidate;
            }
            return selected;
        }

        private CandidateDirection Opposite(CandidateDirection direction)
        {
            return direction == CandidateDirection.Up
                ? CandidateDirection.Down
                : CandidateDirection.Up;
        }

        private void AttachLineageComponent(JoinedContainer joined, OrdinaryContainer component)
        {
            component.JoinedParentId = joined.Id;
            component.ParentFormationId = 0;
            component.ParentOrdinaryContainerId = 0;
            component.Level = joined.Level + 1;
        }

        private void DeriveResponseLineageJoins()
        {
            var ordered = new List<OrdinaryContainer>(ordinaryContainers);
            ordered.Sort((a, b) =>
            {
                int startCompare = a.OriginBar.CompareTo(b.OriginBar);
                return startCompare != 0 ? startCompare : a.Id.CompareTo(b.Id);
            });

            for (int i = 0; i + 2 < ordered.Count; i++)
            {
                OrdinaryContainer left = ordered[i];
                OrdinaryContainer middle = ordered[i + 1];
                OrdinaryContainer right = ordered[i + 2];
                if (!IsAlternating(left, middle, right))
                    continue;
                if (middle.ResponseSourceContainerId != left.Id
                    || right.ResponseSourceContainerId != middle.Id)
                    continue;
                if (left.JoinedParentId != 0 || middle.JoinedParentId != 0 || right.JoinedParentId != 0)
                    continue;

                string rejection;
                JoinedContainer joined;
                if (!TryBuildResponseLineageJoin(left, middle, right, out joined, out rejection))
                {
                    Print("[APVA V4] lineage join rejected triad=" + left.Id + ","
                        + middle.Id + "," + right.Id + " reason=" + rejection);
                    continue;
                }

                joinedContainers.Add(joined);
                AttachComponentsToJoin(joined, left, middle, right);
                Print("[APVA V4] lineage join accepted id=" + joined.Id + " triad="
                    + left.Id + "," + middle.Id + "," + right.Id
                    + " direction=" + joined.Direction + " level=" + joined.Level
                    + " p1=" + joined.StartBar + "@" + FormatPrice(joined.P1Price)
                    + " p2=" + joined.P2Bar + "@" + FormatPrice(joined.P2Price)
                    + " p3=" + joined.P3Bar + "@" + FormatPrice(joined.P3Price));
            }
        }

        private bool TryBuildResponseLineageJoin(OrdinaryContainer left,
            OrdinaryContainer middle, OrdinaryContainer right, out JoinedContainer joined,
            out string rejection)
        {
            joined = null;
            rejection = null;
            bool p1Valid = left.Direction == CandidateDirection.Up
                ? left.RtlOriginPrice < middle.RtlOriginPrice
                    && middle.RtlOriginPrice > right.RtlOriginPrice
                : left.RtlOriginPrice > middle.RtlOriginPrice
                    && middle.RtlOriginPrice < right.RtlOriginPrice;
            if (!p1Valid)
            {
                rejection = "lineage P1 geometry failed";
                return false;
            }

            bool middleFtt = left.Direction == CandidateDirection.Up
                ? middle.P2Price > left.RtlOriginPrice + TickSize * 0.5
                : middle.P2Price < left.RtlOriginPrice - TickSize * 0.5;
            if (!middleFtt)
            {
                rejection = "middle FTT failed";
                return false;
            }

            bool sameDirectionExtreme = left.Direction == CandidateDirection.Up
                ? left.P2Price - right.P2Price <= TickSize * 0.5
                : right.P2Price - left.P2Price <= TickSize * 0.5;
            if (!sameDirectionExtreme)
            {
                rejection = "V1 same-direction extreme failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            double slope;
            if (!TryFitJoinedRtl(left, right, out p3Bar, out p3Price, out slope, out rejection))
                return false;

            OrdinaryContainer shallowest = ShallowestLineageMember(left, middle, right);
            joined = new JoinedContainer
            {
                Id = nextJoinedContainerId++,
                Kind = "ResponseLineage",
                Direction = left.Direction,
                Level = Math.Min(left.Level, Math.Min(middle.Level, right.Level)),
                ParentFormationId = shallowest.ParentFormationId,
                ParentOrdinaryContainerId = shallowest.ParentOrdinaryContainerId,
                LeftId = left.Id,
                MiddleId = middle.Id,
                RightId = right.Id,
                StartBar = left.OriginBar,
                EndBar = right.EndBar,
                P1Price = left.RtlOriginPrice,
                P2Bar = left.P2Bar,
                P2Price = left.P2Price,
                P3Bar = p3Bar,
                P3Price = p3Price,
                Slope = slope
            };
            BuildJoinedOuterAnchors(joined);
            return true;
        }

        private OrdinaryContainer ShallowestLineageMember(OrdinaryContainer left,
            OrdinaryContainer middle, OrdinaryContainer right)
        {
            OrdinaryContainer selected = left;
            if (middle.Level < selected.Level)
                selected = middle;
            if (right.Level < selected.Level)
                selected = right;
            return selected;
        }

        private void AttachComponentsToJoin(JoinedContainer joined, OrdinaryContainer left,
            OrdinaryContainer middle, OrdinaryContainer right)
        {
            OrdinaryContainer[] components = { left, middle, right };
            for (int i = 0; i < components.Length; i++)
            {
                OrdinaryContainer component = components[i];
                component.JoinedParentId = joined.Id;
                component.ParentFormationId = 0;
                component.ParentOrdinaryContainerId = 0;
                component.Level = joined.Level + 1;
            }
        }

        private void DeriveOrdinaryTriadJoins()
        {
            var ordered = new List<OrdinaryContainer>(ordinaryContainers);
            ordered.Sort((a, b) =>
            {
                int startCompare = a.OriginBar.CompareTo(b.OriginBar);
                return startCompare != 0 ? startCompare : a.Id.CompareTo(b.Id);
            });

            for (int i = 0; i + 2 < ordered.Count; i++)
            {
                OrdinaryContainer left = ordered[i];
                OrdinaryContainer middle = ordered[i + 1];
                OrdinaryContainer right = ordered[i + 2];
                if (!IsAlternating(left, middle, right))
                    continue;
                string rejection;
                JoinedContainer joined;
                if (!TryBuildOrdinaryJoin(left, middle, right, out joined, out rejection))
                {
                    Print("[APVA V4] join rejected triad=" + left.Id + "," + middle.Id
                        + "," + right.Id + " reason=" + rejection);
                    continue;
                }

                joinedContainers.Add(joined);
                left.JoinedParentId = joined.Id;
                middle.JoinedParentId = joined.Id;
                right.JoinedParentId = joined.Id;
                left.ParentFormationId = 0;
                middle.ParentFormationId = 0;
                right.ParentFormationId = 0;
                left.ParentOrdinaryContainerId = 0;
                middle.ParentOrdinaryContainerId = 0;
                right.ParentOrdinaryContainerId = 0;
                left.Level = joined.Level + 1;
                middle.Level = joined.Level + 1;
                right.Level = joined.Level + 1;
                Print("[APVA V4] join accepted id=" + joined.Id + " triad="
                    + left.Id + "," + middle.Id + "," + right.Id
                    + " direction=" + joined.Direction + " level=" + joined.Level
                    + " parentFormation=" + joined.ParentFormationId
                    + " parentOrdinary=" + joined.ParentOrdinaryContainerId
                    + " p1=" + joined.StartBar + "@" + FormatPrice(joined.P1Price)
                    + " p2=" + joined.P2Bar + "@" + FormatPrice(joined.P2Price)
                    + " p3=" + joined.P3Bar + "@" + FormatPrice(joined.P3Price));
            }
        }

        private bool IsAlternating(OrdinaryContainer left, OrdinaryContainer middle,
            OrdinaryContainer right)
        {
            return left.Direction == right.Direction && left.Direction != middle.Direction
                && left.OriginBar < middle.OriginBar && middle.OriginBar < right.OriginBar;
        }

        private bool TryBuildOrdinaryJoin(OrdinaryContainer left, OrdinaryContainer middle,
            OrdinaryContainer right, out JoinedContainer joined, out string rejection)
        {
            joined = null;
            rejection = null;
            if (left.JoinedParentId != 0 || middle.JoinedParentId != 0 || right.JoinedParentId != 0)
            {
                rejection = "component already belongs to a joined parent";
                return false;
            }
            if (left.Level != middle.Level || middle.Level != right.Level)
            {
                rejection = "levels differ " + left.Level + "," + middle.Level + "," + right.Level;
                return false;
            }
            if (left.ParentFormationId != middle.ParentFormationId
                || middle.ParentFormationId != right.ParentFormationId
                || left.ParentOrdinaryContainerId != middle.ParentOrdinaryContainerId
                || middle.ParentOrdinaryContainerId != right.ParentOrdinaryContainerId)
            {
                rejection = "parent scope differs";
                return false;
            }

            bool p1Valid = left.Direction == CandidateDirection.Up
                ? left.RtlOriginPrice < middle.RtlOriginPrice
                    && middle.RtlOriginPrice > right.RtlOriginPrice
                : left.RtlOriginPrice > middle.RtlOriginPrice
                    && middle.RtlOriginPrice < right.RtlOriginPrice;
            if (!p1Valid)
            {
                rejection = "alternating P1 geometry failed";
                return false;
            }

            bool oppositeBreakException = left.Status == OrdinaryStatus.Active
                && middle.Status != OrdinaryStatus.Active
                && right.Status == OrdinaryStatus.Active;
            bool resumed = left.Direction == CandidateDirection.Up
                ? right.P2Price > left.P2Price + TickSize * 0.5
                : right.P2Price < left.P2Price - TickSize * 0.5;
            if (!oppositeBreakException && !resumed)
            {
                rejection = left.Direction == CandidateDirection.Up
                    ? "right Up has not exceeded left P2"
                    : "right Down has not traversed below left P2";
                return false;
            }

            int p3Bar;
            double p3Price;
            double slope;
            if (!TryFitJoinedRtl(left, right, out p3Bar, out p3Price, out slope, out rejection))
                return false;

            joined = new JoinedContainer
            {
                Id = nextJoinedContainerId++,
                Kind = "Ordinary",
                Direction = left.Direction,
                Level = left.Level,
                ParentFormationId = left.ParentFormationId,
                ParentOrdinaryContainerId = left.ParentOrdinaryContainerId,
                LeftId = left.Id,
                MiddleId = middle.Id,
                RightId = right.Id,
                StartBar = left.OriginBar,
                EndBar = right.EndBar,
                P1Price = left.RtlOriginPrice,
                P2Bar = left.P2Bar,
                P2Price = left.P2Price,
                P3Bar = p3Bar,
                P3Price = p3Price,
                Slope = slope
            };
            BuildJoinedOuterAnchors(joined);
            return true;
        }

        private bool TryFitJoinedRtl(OrdinaryContainer left, OrdinaryContainer right,
            out int p3Bar, out double p3Price, out double slope, out string rejection)
        {
            int startBar = left.OriginBar;
            double p1 = left.RtlOriginPrice;
            p3Bar = right.SupportBar;
            p3Price = right.SupportPrice;
            slope = (p3Price - p1) / Math.Max(1, p3Bar - startBar);
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= startBar || bar.Index > right.EndBar)
                    continue;
                double wick = left.Direction == CandidateDirection.Up ? bar.Low : bar.High;
                double projected = p1 + slope * (bar.Index - startBar);
                bool traversed = left.Direction == CandidateDirection.Up
                    ? wick < projected - TickSize * 0.5
                    : wick > projected + TickSize * 0.5;
                if (!traversed)
                    continue;
                double candidateSlope = (wick - p1) / (bar.Index - startBar);
                if ((left.Direction == CandidateDirection.Up && candidateSlope < slope)
                    || (left.Direction == CandidateDirection.Down && candidateSlope > slope))
                {
                    slope = candidateSlope;
                    p3Bar = bar.Index;
                    p3Price = wick;
                }
            }

            bool directional = left.Direction == CandidateDirection.Up
                ? p3Price > p1 + TickSize * 0.5
                : p3Price < p1 - TickSize * 0.5;
            if (!directional || p3Bar <= startBar)
            {
                rejection = "joined P1/P3 geometry is not strictly directional";
                return false;
            }
            rejection = null;
            return true;
        }

        private void BuildJoinedOuterAnchors(JoinedContainer joined)
        {
            joined.OuterAnchors.Add(new OrdinaryOuterAnchor(joined.P2Bar, joined.P2Price, false));
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= joined.P2Bar || bar.Index > joined.EndBar)
                    continue;
                OrdinaryOuterAnchor active = joined.OuterAnchors[joined.OuterAnchors.Count - 1];
                double projected = active.Price + joined.Slope * (bar.Index - active.Bar);
                double extreme = joined.Direction == CandidateDirection.Up ? bar.High : bar.Low;
                bool traversed = joined.Direction == CandidateDirection.Up
                    ? extreme > projected + TickSize * 0.5
                    : extreme < projected - TickSize * 0.5;
                if (traversed)
                    joined.OuterAnchors.Add(new OrdinaryOuterAnchor(bar.Index, extreme, true));
            }
        }

        private CandidateDirection? ActiveFormationDirection(AmbiguousFormation formation)
        {
            if (formation == null)
                return null;
            ProvisionalCandidate candidate = CommittedCandidate(formation);
            if (candidate == null || candidate.Status != CandidateStatus.Committed)
                return null;
            return candidate.Direction;
        }

        private int ActiveFormationId(AmbiguousFormation formation)
        {
            return ActiveFormationDirection(formation).HasValue ? formation.Id : 0;
        }

        private void ProcessOrdinaryConstruction(BarSnapshot origin, BarSnapshot current,
            bool pendingAmbiguity, CandidateDirection? committedOwnerDirection,
            int committedOwnerFormationId, string reason)
        {
            if (IsInsideLike(current.Relation))
            {
                Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                    + " reason=second bar is inside-like");
                return;
            }
            if (IsOutside(current.Relation))
            {
                Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                    + " reason=second bar is Outside Bar");
                return;
            }

            bool up = current.Low > origin.Low && current.High >= origin.High;
            bool down = current.High < origin.High && current.Low <= origin.Low;
            if (up == down)
            {
                Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                    + " reason=no unique strict directional geometry");
                return;
            }

            CandidateDirection direction = up ? CandidateDirection.Up : CandidateDirection.Down;
            int responseSourceId = direction == CandidateDirection.Up
                ? ordinaryDownTerminatedContainerIdThisBar
                : ordinaryUpTerminatedContainerIdThisBar;
            if (pendingAmbiguity)
            {
                Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                    + " reason=bar owned by pending ambiguous formation");
                return;
            }
            if (committedOwnerDirection.HasValue && committedOwnerDirection.Value == direction)
            {
                bool oppositeTerminated = direction == CandidateDirection.Up
                    ? ordinaryDownTerminatedThisBar
                    : ordinaryUpTerminatedThisBar;
                if (!oppositeTerminated)
                {
                    Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                        + " direction=" + direction
                        + " reason=same direction as committed ambiguous owner");
                    return;
                }
            }
            if (HasActiveOrdinaryContainer(direction))
            {
                Print("[APVA V4] ordinary rejected start=" + origin.Index + " end=" + current.Index
                    + " direction=" + direction
                    + " reason=active same-direction ordinary container already owns pair");
                return;
            }

            BarSnapshot outerAnchor = FindOuterAnchor(origin, current, direction);
            var container = new OrdinaryContainer(nextOrdinaryContainerId++,
                direction, origin, current, outerAnchor);
            container.OwnerFormationId = committedOwnerFormationId;
            container.ResponseSourceContainerId = responseSourceId;
            if (container.OwnerFormationId == 0 && responseSourceId == 0)
            {
                OrdinaryContainer ordinaryOwner = FindContainingActiveOrdinary(direction, current);
                container.OwnerOrdinaryContainerId = ordinaryOwner == null ? 0 : ordinaryOwner.Id;
            }
            ordinaryContainers.Add(container);
            UpdateOrdinaryOuterExpansion(container, current, true);
            Print("[APVA V4] ordinary created id=" + container.Id
                + " direction=" + container.Direction + " start=" + container.OriginBar
                + " end=" + container.EndBar + " rtl=" + FormatPrice(container.RtlOriginPrice)
                + "->" + FormatPrice(container.SupportPrice) + " p2="
                + container.P2Bar + "@" + FormatPrice(container.P2Price)
                + " ltlOrigin=" + FormatPrice(container.LtlOriginPrice)
                + " veCount=" + (container.OuterAnchors.Count - 1)
                + " ownerFormation=" + container.OwnerFormationId
                + " ownerOrdinary=" + container.OwnerOrdinaryContainerId
                + " responseSource=" + container.ResponseSourceContainerId
                + " reason=" + reason);
        }

        private void DeriveRecursiveHierarchy()
        {
            // Base ownership is immutable creation provenance.
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                container.ParentFormationId = FindFormation(container.OwnerFormationId) != null
                    ? container.OwnerFormationId
                    : 0;
                container.ParentOrdinaryContainerId = 0;
                container.PromotedFromFormationId = 0;
                container.Level = container.ParentFormationId == 0 ? 1 : 2;
                if (container.ParentFormationId == 0 && container.OwnerOrdinaryContainerId != 0)
                {
                    OrdinaryContainer parent = FindOrdinaryContainer(container.OwnerOrdinaryContainerId);
                    if (parent != null && parent.Id != container.Id)
                    {
                        container.ParentOrdinaryContainerId = parent.Id;
                        container.Level = parent.Level + 1;
                    }
                }
            }

            ApplyResponseInheritance(false);
            PromoteFormationSurvivors();
            ApplyResponseInheritance(true);

            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                Print("[APVA V4] hierarchy assigned ordinary=" + container.Id
                    + " parentFormation=" + container.ParentFormationId
                    + " parentOrdinary=" + container.ParentOrdinaryContainerId
                    + " level=" + container.Level
                    + " promotedFromFormation=" + container.PromotedFromFormationId
                    + " responseSource=" + container.ResponseSourceContainerId);
            }
        }

        private void ApplyResponseInheritance(bool preservePromoted)
        {
            // A response inherits the terminated source's final parent scope and level.
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                if (container.ResponseSourceContainerId == 0)
                    continue;
                if (preservePromoted && container.PromotedFromFormationId != 0)
                    continue;
                OrdinaryContainer source = FindOrdinaryContainer(container.ResponseSourceContainerId);
                if (source == null)
                    continue;
                container.ParentFormationId = source.ParentFormationId;
                container.ParentOrdinaryContainerId = source.ParentOrdinaryContainerId;
                container.Level = source.Level;
            }
        }

        private void PromoteFormationSurvivors()
        {
            for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
            {
                AmbiguousFormation formation = formations[formationIndex];
                ProvisionalCandidate root = CommittedCandidate(formation);
                if (root == null || root.BreakBar < 0)
                    continue;

                OrdinaryContainer selected = null;
                for (int i = 0; i < ordinaryContainers.Count; i++)
                {
                    OrdinaryContainer candidate = ordinaryContainers[i];
                    if (candidate.ParentFormationId != formation.Id
                        || candidate.Direction == root.Direction
                        || candidate.OriginBar > root.BreakBar
                        || candidate.EndBar < root.BreakBar)
                        continue;
                    if (selected == null || candidate.OriginBar > selected.OriginBar)
                        selected = candidate;
                }
                if (selected == null)
                    continue;

                selected.ParentFormationId = 0;
                selected.ParentOrdinaryContainerId = 0;
                selected.PromotedFromFormationId = formation.Id;
                selected.Level = 1;
                Print("[APVA V4] hierarchy promoted ordinary=" + selected.Id
                    + " fromFormation=" + formation.Id + " breakBar=" + root.BreakBar
                    + " inheritedLevel=1");
            }
        }

        private OrdinaryContainer FindContainingActiveOrdinary(CandidateDirection childDirection,
            BarSnapshot current)
        {
            OrdinaryContainer selected = null;
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer candidate = ordinaryContainers[i];
                if (candidate.Status != OrdinaryStatus.Active
                    || candidate.Direction == childDirection
                    || current.Index < candidate.OriginBar
                    || !OrdinaryContainsBar(candidate, current))
                    continue;
                if (selected == null || candidate.OriginBar > selected.OriginBar)
                    selected = candidate;
            }
            return selected;
        }

        private bool OrdinaryContainsBar(OrdinaryContainer container, BarSnapshot current)
        {
            double rtl = ProjectOrdinary(container, current.Index);
            OrdinaryOuterAnchor outer = container.OuterAnchors[container.OuterAnchors.Count - 1];
            double outerPrice = outer.Price
                + OrdinarySlope(container) * (current.Index - outer.Bar);
            double lower = Math.Min(rtl, outerPrice) - TickSize * 0.5;
            double upper = Math.Max(rtl, outerPrice) + TickSize * 0.5;
            return current.Low >= lower && current.High <= upper;
        }

        private OrdinaryContainer FindOrdinaryContainer(int id)
        {
            if (id == 0)
                return null;
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                if (ordinaryContainers[i].Id == id)
                    return ordinaryContainers[i];
            }
            return null;
        }

        private AmbiguousFormation FindFormation(int id)
        {
            if (id == 0)
                return null;
            for (int i = 0; i < formations.Count; i++)
            {
                if (formations[i].Id == id)
                    return formations[i];
            }
            return null;
        }

        private BarSnapshot FindOuterAnchor(BarSnapshot origin, BarSnapshot end,
            CandidateDirection direction)
        {
            BarSnapshot selected = origin;
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot candidate = snapshots[i];
                if (candidate.Index <= origin.Index || candidate.Index > end.Index)
                    continue;
                bool moreExtreme = direction == CandidateDirection.Up
                    ? candidate.High >= selected.High
                    : candidate.Low <= selected.Low;
                if (moreExtreme)
                    selected = candidate;
            }
            return selected;
        }

        private bool HasActiveOrdinaryContainer(CandidateDirection direction)
        {
            for (int i = ordinaryContainers.Count - 1; i >= 0; i--)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                if (container.Direction == direction && container.Status == OrdinaryStatus.Active)
                    return true;
            }
            return false;
        }

        private void AdvanceOrdinaryContainers(BarSnapshot current)
        {
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                if (container.Status != OrdinaryStatus.Active || current.Index <= container.EndBar)
                    continue;

                double projected = ProjectOrdinary(container, current.Index);
                double bodyLow = Math.Min(current.Open, current.Close);
                double bodyHigh = Math.Max(current.Open, current.Close);
                bool broken = container.Direction == CandidateDirection.Up
                    ? bodyHigh < projected
                    : bodyLow > projected;
                if (broken)
                {
                    container.Status = OrdinaryStatus.Broken;
                    container.BreakBar = current.Index;
                    container.EndBar = current.Index;
                    MarkOrdinaryTerminated(container.Direction, container.Id);
                    Print("[APVA V4] ordinary broken id=" + container.Id
                        + " direction=" + container.Direction + " bar=" + current.Index
                        + " projectedRtl=" + FormatPrice(projected) + " body="
                        + FormatPrice(bodyLow) + "-" + FormatPrice(bodyHigh));
                    continue;
                }

                double support = container.Direction == CandidateDirection.Up ? current.Low : current.High;
                bool wickCrossed = container.Direction == CandidateDirection.Up
                    ? support < projected
                    : support > projected;
                bool closeInside = container.Direction == CandidateDirection.Up
                    ? current.Close >= projected
                    : current.Close <= projected;
                bool directional = container.Direction == CandidateDirection.Up
                    ? support > container.RtlOriginPrice
                    : support < container.RtlOriginPrice;
                if (wickCrossed && closeInside && directional)
                {
                    container.SupportBar = current.Index;
                    container.SupportPrice = support;
                    Print("[APVA V4] ordinary adjusted id=" + container.Id
                        + " direction=" + container.Direction + " bar=" + current.Index
                        + " support=" + FormatPrice(support));
                }
                else if (wickCrossed)
                {
                    container.Status = OrdinaryStatus.Invalidated;
                    container.BreakBar = current.Index;
                    MarkOrdinaryTerminated(container.Direction, container.Id);
                    Print("[APVA V4] ordinary invalidated id=" + container.Id
                        + " direction=" + container.Direction + " invalidationBar=" + current.Index
                        + " frozenEnd=" + container.EndBar + " projectedRtl=" + FormatPrice(projected)
                        + " wick=" + FormatPrice(support) + " close=" + FormatPrice(current.Close)
                        + " closeInside=" + closeInside + " directional=" + directional
                        + " reason=RTL traversal could not be adjusted");
                    continue;
                }

                UpdateOrdinaryP2(container, current);
                UpdateOrdinaryOuterExpansion(container, current, false);
                container.EndBar = current.Index;
                Print("[APVA V4] ordinary advanced id=" + container.Id
                    + " direction=" + container.Direction + " end=" + current.Index
                    + " support=" + container.SupportBar + "@" + FormatPrice(container.SupportPrice));
            }
        }

        private double ProjectOrdinary(OrdinaryContainer container, int bar)
        {
            int span = container.SupportBar - container.OriginBar;
            if (span <= 0)
                return container.RtlOriginPrice;
            double slope = (container.SupportPrice - container.RtlOriginPrice) / span;
            return container.RtlOriginPrice + slope * (bar - container.OriginBar);
        }

        private void MarkOrdinaryTerminated(CandidateDirection direction, int containerId)
        {
            if (direction == CandidateDirection.Up)
            {
                ordinaryUpTerminatedThisBar = true;
                ordinaryUpTerminatedContainerIdThisBar = containerId;
            }
            else
            {
                ordinaryDownTerminatedThisBar = true;
                ordinaryDownTerminatedContainerIdThisBar = containerId;
            }
        }

        private double OrdinarySlope(OrdinaryContainer container)
        {
            int span = container.SupportBar - container.OriginBar;
            if (span <= 0)
                return 0.0;
            return (container.SupportPrice - container.RtlOriginPrice) / span;
        }

        private void UpdateOrdinaryP2(OrdinaryContainer container, BarSnapshot current)
        {
            bool moreExtreme = container.Direction == CandidateDirection.Up
                ? current.High > container.P2Price
                : current.Low < container.P2Price;
            if (!moreExtreme)
                return;
            container.P2Bar = current.Index;
            container.P2Price = container.Direction == CandidateDirection.Up
                ? current.High
                : current.Low;
            Print("[APVA V4] ordinary P2 updated id=" + container.Id
                + " direction=" + container.Direction + " p2=" + container.P2Bar
                + "@" + FormatPrice(container.P2Price));
        }

        private void UpdateOrdinaryOuterExpansion(OrdinaryContainer container,
            BarSnapshot current, bool initial)
        {
            if (container.OuterAnchors.Count == 0)
                return;
            OrdinaryOuterAnchor activeOuter = container.OuterAnchors[container.OuterAnchors.Count - 1];
            double projected = activeOuter.Price
                + OrdinarySlope(container) * (current.Index - activeOuter.Bar);
            double extreme = container.Direction == CandidateDirection.Up ? current.High : current.Low;
            bool traversed = container.Direction == CandidateDirection.Up
                ? extreme > projected + TickSize * 0.5
                : extreme < projected - TickSize * 0.5;
            if (!traversed)
                return;
            if (activeOuter.Bar == current.Index)
                return;

            container.OuterAnchors.Add(new OrdinaryOuterAnchor(current.Index, extreme, true));
            Print("[APVA V4] ordinary VE created id=" + container.Id
                + " direction=" + container.Direction + " bar=" + current.Index
                + " anchor=" + FormatPrice(extreme)
                + " frozen=" + (activeOuter.IsVe ? "VE" : "LTL")
                + " reason=" + (initial ? "initial outer traversal" : "outer traversal"));
        }

        private bool IsOutside(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.OutsideBar
                || relation == xPvaBarRelation.OutsideBullish
                || relation == xPvaBarRelation.OutsideBearish;
        }

        private StrokeStyle StrokeForLevel(int level)
        {
            if (level <= 1)
                return null;
            if (level == 2)
                return dashStrokeStyle;
            return dotStrokeStyle;
        }

        private float WidthForLevel(int level)
        {
            if (level <= 1)
                return 2.0f;
            if (level == 2)
                return 1.8f;
            return 1.7f;
        }

        private void AdvanceConfirmedLaterals(BarSnapshot current)
        {
            for (int i = 0; i < formations.Count; i++)
            {
                AmbiguousFormation formation = formations[i];
                if (!formation.LateralConfirmed || formation.LateralClosed
                    || current.Index <= formation.LateralEndBar)
                    continue;

                double bodyLow = Math.Min(current.Open, current.Close);
                double bodyHigh = Math.Max(current.Open, current.Close);
                bool bodyAbove = bodyLow > formation.BoundaryHigh;
                bool bodyBelow = bodyHigh < formation.BoundaryLow;
                formation.LateralEndBar = current.Index;
                if (bodyAbove || bodyBelow)
                {
                    formation.LateralClosed = true;
                    formation.LateralBreakBar = current.Index;
                    Print("[APVA V4] lateral closed formation=" + formation.Id
                        + " origin=" + formation.OriginBar + " bar=" + current.Index
                        + " direction=" + (bodyAbove ? "Above" : "Below")
                        + " bounds=" + FormatPrice(formation.BoundaryLow) + "-"
                        + FormatPrice(formation.BoundaryHigh));
                }
                else
                {
                    Print("[APVA V4] lateral advanced formation=" + formation.Id
                        + " origin=" + formation.OriginBar + " end=" + current.Index);
                }
            }
        }

        private bool IsFormationClosed(AmbiguousFormation formation)
        {
            if (formation.Status == FormationStatus.Pending)
                return false;
            if (formation.Status == FormationStatus.Failed)
                return true;
            ProvisionalCandidate survivor = CommittedCandidate(formation);
            return survivor != null && survivor.Status == CandidateStatus.Broken;
        }

        private ProvisionalCandidate CommittedCandidate(AmbiguousFormation formation)
        {
            if (formation.Status == FormationStatus.ResolvedUp)
                return formation.Up;
            if (formation.Status == FormationStatus.ResolvedDown)
                return formation.Down;
            return null;
        }

        private bool CanSeedAmbiguity(BarSnapshot origin, BarSnapshot current)
        {
            if (!IsInsideLike(current.Relation))
                return false;
            if (current.High > origin.High || current.Low < origin.Low)
                return false;
            return current.Low > origin.Low && current.High < origin.High;
        }

        private AmbiguousFormation CreateFormation(BarSnapshot origin, BarSnapshot current)
        {
            var up = new ProvisionalCandidate(nextCandidateId++, CandidateDirection.Up,
                origin.Index, origin.Low, current.Index, current.Low);
            var down = new ProvisionalCandidate(nextCandidateId++, CandidateDirection.Down,
                origin.Index, origin.High, current.Index, current.High);
            var formation = new AmbiguousFormation(nextFormationId++, origin, current, up, down);
            formations.Add(formation);

            Print("[APVA V4] ambiguity created formation=" + formation.Id
                + " origin=" + formation.OriginBar + " firstContained=" + current.Index
                + " bounds=" + FormatPrice(formation.BoundaryLow) + "-" + FormatPrice(formation.BoundaryHigh)
                + " upCandidate=" + up.Id + " downCandidate=" + down.Id);
            return formation;
        }

        private void ProcessFormation(AmbiguousFormation formation, BarSnapshot current)
        {
            if (current.Index <= formation.EndBar)
                return;

            bool contained = current.High <= formation.BoundaryHigh && current.Low >= formation.BoundaryLow;
            if (contained)
            {
                formation.ContainedBarCount++;
                if (!formation.LateralConfirmed && formation.ContainedBarCount >= 2)
                {
                    formation.LateralConfirmed = true;
                    formation.LateralEndBar = current.Index;
                    Print("[APVA V4] lateral confirmed formation=" + formation.Id
                        + " origin=" + formation.OriginBar + " bar=" + current.Index);
                }
            }

            RefitEnvelopeCandidate(formation.Up, current);
            RefitEnvelopeCandidate(formation.Down, current);
            formation.EndBar = current.Index;

            if (formation.LateralConfirmed && current.Relation == xPvaBarRelation.HHHL)
            {
                Resolve(formation, formation.Up, formation.Down,
                    FormationStatus.ResolvedUp, current.Index);
                return;
            }

            if (formation.LateralConfirmed && current.Relation == xPvaBarRelation.LLLH)
            {
                Resolve(formation, formation.Down, formation.Up,
                    FormationStatus.ResolvedDown, current.Index);
                return;
            }

            bool upBroken = UpdateCandidate(formation.Up, current);
            bool downBroken = UpdateCandidate(formation.Down, current);

            if (upBroken && downBroken)
            {
                formation.Status = FormationStatus.Failed;
                formation.Up.Status = CandidateStatus.Discarded;
                formation.Down.Status = CandidateStatus.Discarded;
                Print("[APVA V4] ambiguity failed formation=" + formation.Id
                    + " bar=" + current.Index + " reason=both provisional RTLs broken");
                return;
            }

            if (upBroken)
            {
                Resolve(formation, formation.Down, formation.Up, FormationStatus.ResolvedDown, current.Index);
                return;
            }

            if (downBroken)
            {
                Resolve(formation, formation.Up, formation.Down, FormationStatus.ResolvedUp, current.Index);
                return;
            }

            Print("[APVA V4] ambiguity continued formation=" + formation.Id
                + " origin=" + formation.OriginBar + " end=" + current.Index
                + " state=" + (contained ? "contained" : "uncontained")
                + " upSupport=" + formation.Up.SupportBar + "@" + FormatPrice(formation.Up.SupportPrice)
                + " downSupport=" + formation.Down.SupportBar + "@" + FormatPrice(formation.Down.SupportPrice));
        }

        private void RefitEnvelopeCandidate(ProvisionalCandidate candidate, BarSnapshot current)
        {
            double projected = Project(candidate, current.Index);
            double support = candidate.Direction == CandidateDirection.Up ? current.Low : current.High;
            bool remainsDirectional = candidate.Direction == CandidateDirection.Up
                ? support > candidate.OriginPrice
                : support < candidate.OriginPrice;
            bool rtlWouldTraverse = candidate.Direction == CandidateDirection.Up
                ? support < projected
                : support > projected;
            if (remainsDirectional && rtlWouldTraverse)
            {
                candidate.SupportBar = current.Index;
                candidate.SupportPrice = support;
                Print("[APVA V4] provisional envelope adjusted candidate=" + candidate.Id
                    + " direction=" + candidate.Direction + " bar=" + current.Index
                    + " support=" + FormatPrice(support));
            }
            candidate.EndBar = current.Index;
        }

        private void ProcessCommittedFormation(AmbiguousFormation formation, BarSnapshot current)
        {
            ProvisionalCandidate candidate = CommittedCandidate(formation);
            if (candidate == null || candidate.Status != CandidateStatus.Committed
                || current.Index <= candidate.EndBar)
                return;

            double projected = Project(candidate, current.Index);
            double bodyLow = Math.Min(current.Open, current.Close);
            double bodyHigh = Math.Max(current.Open, current.Close);
            bool broken = candidate.Direction == CandidateDirection.Up
                ? bodyHigh < projected
                : bodyLow > projected;
            if (broken)
            {
                candidate.Status = CandidateStatus.Broken;
                candidate.BreakBar = current.Index;
                candidate.EndBar = current.Index;
                formation.EndBar = current.Index;
                Print("[APVA V4] committed broken candidate=" + candidate.Id
                    + " direction=" + candidate.Direction + " origin=" + candidate.OriginBar
                    + " bar=" + current.Index + " projectedRtl=" + FormatPrice(projected)
                    + " body=" + FormatPrice(bodyLow) + "-" + FormatPrice(bodyHigh));
                return;
            }

            double support = candidate.Direction == CandidateDirection.Up ? current.Low : current.High;
            bool wickCrossed = candidate.Direction == CandidateDirection.Up
                ? support < projected
                : support > projected;
            bool closeInside = candidate.Direction == CandidateDirection.Up
                ? current.Close >= projected
                : current.Close <= projected;
            bool remainsDirectional = candidate.Direction == CandidateDirection.Up
                ? support > candidate.OriginPrice
                : support < candidate.OriginPrice;
            if (wickCrossed && closeInside && remainsDirectional)
            {
                candidate.SupportBar = current.Index;
                candidate.SupportPrice = support;
                Print("[APVA V4] committed adjusted candidate=" + candidate.Id
                    + " direction=" + candidate.Direction + " bar=" + current.Index
                    + " support=" + FormatPrice(support));
            }

            candidate.EndBar = current.Index;
            formation.EndBar = current.Index;
            Print("[APVA V4] committed advanced candidate=" + candidate.Id
                + " direction=" + candidate.Direction + " origin=" + candidate.OriginBar
                + " end=" + current.Index + " support=" + candidate.SupportBar
                + "@" + FormatPrice(candidate.SupportPrice));
        }

        private bool UpdateCandidate(ProvisionalCandidate candidate, BarSnapshot current)
        {
            double projected = Project(candidate, current.Index);
            double bodyLow = Math.Min(current.Open, current.Close);
            double bodyHigh = Math.Max(current.Open, current.Close);
            bool broken = candidate.Direction == CandidateDirection.Up
                ? bodyHigh < projected
                : bodyLow > projected;
            if (broken)
            {
                Print("[APVA V4] provisional broken candidate=" + candidate.Id
                    + " direction=" + candidate.Direction + " bar=" + current.Index
                    + " projectedRtl=" + FormatPrice(projected)
                    + " body=" + FormatPrice(bodyLow) + "-" + FormatPrice(bodyHigh));
                return true;
            }
            candidate.EndBar = current.Index;
            return false;
        }

        private void Resolve(AmbiguousFormation formation, ProvisionalCandidate survivor,
            ProvisionalCandidate discarded, FormationStatus status, int bar)
        {
            formation.Status = status;
            survivor.Status = CandidateStatus.Committed;
            survivor.EndBar = bar;
            discarded.Status = CandidateStatus.Discarded;
            Print("[APVA V4] ambiguity resolved formation=" + formation.Id
                + " direction=" + survivor.Direction + " origin=" + formation.OriginBar
                + " bar=" + bar + " committedCandidate=" + survivor.Id
                + " discardedCandidate=" + discarded.Id);
        }

        private double Project(ProvisionalCandidate candidate, int bar)
        {
            int span = candidate.SupportBar - candidate.OriginBar;
            if (span <= 0)
                return candidate.OriginPrice;
            double slope = (candidate.SupportPrice - candidate.OriginPrice) / span;
            return candidate.OriginPrice + slope * (bar - candidate.OriginBar);
        }

        private bool IsInsideLike(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.InsideBar
                || relation == xPvaBarRelation.SameHighSameLow
                || relation == xPvaBarRelation.FTP
                || relation == xPvaBarRelation.FBP;
        }

        private BarSnapshot CreateSnapshot(int index)
        {
            xPvaBarFacts current = Facts(index);
            xPvaBarRelation relation = xPvaBarRelation.Unknown;
            if (index > 0)
                relation = eventEngine.ClassifyRelation(current, Facts(index - 1));
            return new BarSnapshot(index, current.Time, current.Open, current.High, current.Low,
                current.Close, current.Volume, relation);
        }

        private xPvaBarFacts Facts(int index)
        {
            return new xPvaBarFacts(index, Time.GetValueAt(index), Open.GetValueAt(index),
                High.GetValueAt(index), Low.GetValueAt(index), Close.GetValueAt(index),
                Volume.GetValueAt(index), TickSize);
        }

        private string FormatSnapshot(BarSnapshot snapshot)
        {
            return "[APVA V4] bar=" + snapshot.Index.ToString(CultureInfo.InvariantCulture)
                + " time=" + snapshot.Time.ToString("O", CultureInfo.InvariantCulture)
                + " open=" + FormatPrice(snapshot.Open)
                + " high=" + FormatPrice(snapshot.High)
                + " low=" + FormatPrice(snapshot.Low)
                + " close=" + FormatPrice(snapshot.Close)
                + " volume=" + snapshot.Volume.ToString(CultureInfo.InvariantCulture)
                + " relation=" + snapshot.Relation;
        }

        private string FormatPrice(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private void RunInvariantAudit()
        {
            auditFindings.Clear();
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer container = ordinaryContainers[i];
                bool directional = container.Direction == CandidateDirection.Up
                    ? container.SupportPrice > container.RtlOriginPrice + TickSize * 0.5
                    : container.SupportPrice < container.RtlOriginPrice - TickSize * 0.5;
                if (!directional)
                    AddAudit("Error", "OrdinaryDirectionalGeometry", "ordinary:" + container.Id,
                        "P1/P3 geometry is not strictly directional");

                int parentCount = (container.ParentFormationId != 0 ? 1 : 0)
                    + (container.ParentOrdinaryContainerId != 0 ? 1 : 0)
                    + (container.JoinedParentId != 0 ? 1 : 0);
                if (parentCount > 1)
                    AddAudit("Error", "MultipleVisualParents", "ordinary:" + container.Id,
                        "container has more than one derived visual parent");
                if (container.Level < 1)
                    AddAudit("Error", "InvalidLevel", "ordinary:" + container.Id,
                        "level must be positive");
                if (container.JoinedParentId != 0)
                {
                    JoinedContainer parent = FindJoinedContainer(container.JoinedParentId);
                    if (parent == null)
                        AddAudit("Error", "MissingJoinedParent", "ordinary:" + container.Id,
                            "joined parent does not exist");
                    else if (container.Level != parent.Level + 1)
                        AddAudit("Error", "JoinedChildLevel", "ordinary:" + container.Id,
                            "joined child level is not parent level plus one");
                }

                int auditEnd = container.Status == OrdinaryStatus.Broken
                    ? container.EndBar - 1
                    : container.EndBar;
                AuditRtlTraversal("ordinary:" + container.Id, container.Direction,
                    container.OriginBar, auditEnd, container.RtlOriginPrice,
                    OrdinarySlope(container));
                AuditOuterAnchorOrder("ordinary:" + container.Id, container.OuterAnchors);
            }

            for (int i = 0; i < joinedContainers.Count; i++)
            {
                JoinedContainer joined = joinedContainers[i];
                bool directional = joined.Direction == CandidateDirection.Up
                    ? joined.P3Price > joined.P1Price + TickSize * 0.5
                    : joined.P3Price < joined.P1Price - TickSize * 0.5;
                if (!directional)
                    AddAudit("Error", "JoinedDirectionalGeometry", "joined:" + joined.Id,
                        "joined P1/P3 geometry is not strictly directional");
                AuditRtlTraversal("joined:" + joined.Id, joined.Direction, joined.StartBar,
                    joined.EndBar, joined.P1Price, joined.Slope);
                AuditOuterAnchorOrder("joined:" + joined.Id, joined.OuterAnchors);
            }

            for (int i = 0; i < formations.Count; i++)
            {
                AmbiguousFormation formation = formations[i];
                if (formation.LateralConfirmed && formation.LateralEndBar < formation.OriginBar + 2)
                    AddAudit("Error", "LateralTooShort", "formation:" + formation.Id,
                        "confirmed lateral contains fewer than three bars");
                ProvisionalCandidate committed = CommittedCandidate(formation);
                if (committed != null && committed.Status != CandidateStatus.Committed
                    && committed.Status != CandidateStatus.Broken)
                    AddAudit("Error", "ResolvedCandidateStatus", "formation:" + formation.Id,
                        "resolved formation has no committed or broken survivor");
            }
        }

        private void AuditRtlTraversal(string subject, CandidateDirection direction,
            int startBar, int endBar, double p1Price, double slope)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= startBar || bar.Index > endBar)
                    continue;
                double projected = p1Price + slope * (bar.Index - startBar);
                bool traversed = direction == CandidateDirection.Up
                    ? bar.Low < projected - TickSize * 0.5
                    : bar.High > projected + TickSize * 0.5;
                if (!traversed)
                    continue;
                AddAudit("Error", "RtlTraversesBar", subject,
                    "RTL traverses bar " + bar.Index + " projected=" + FormatPrice(projected));
                return;
            }
        }

        private void AuditOuterAnchorOrder(string subject, List<OrdinaryOuterAnchor> anchors)
        {
            for (int i = 1; i < anchors.Count; i++)
            {
                if (anchors[i].Bar > anchors[i - 1].Bar)
                    continue;
                AddAudit("Error", "OuterAnchorOrder", subject,
                    "outer anchors are not strictly chronological");
                return;
            }
        }

        private void AddAudit(string severity, string code, string subject, string message)
        {
            auditFindings.Add(new AuditFinding
            {
                Severity = severity,
                Code = code,
                Subject = subject,
                Message = message
            });
            Print("[APVA V4] audit " + severity.ToLowerInvariant() + " code=" + code
                + " subject=" + subject + " message=" + message);
        }

        private int CountAuditSeverity(string severity)
        {
            int count = 0;
            for (int i = 0; i < auditFindings.Count; i++)
            {
                if (auditFindings[i].Severity == severity)
                    count++;
            }
            return count;
        }

        private JoinedContainer FindJoinedContainer(int id)
        {
            for (int i = 0; i < joinedContainers.Count; i++)
            {
                if (joinedContainers[i].Id == id)
                    return joinedContainers[i];
            }
            return null;
        }

        private string ComputeStateFingerprint()
        {
            var canonical = new StringBuilder();
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                canonical.Append("B|").Append(bar.Index).Append('|').Append(FormatPrice(bar.Open))
                    .Append('|').Append(FormatPrice(bar.High)).Append('|').Append(FormatPrice(bar.Low))
                    .Append('|').Append(FormatPrice(bar.Close)).Append('|').Append(bar.Relation).Append('\n');
            }
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer c = ordinaryContainers[i];
                canonical.Append("O|").Append(c.Id).Append('|').Append(c.Direction).Append('|')
                    .Append(c.OriginBar).Append('|').Append(c.EndBar).Append('|')
                    .Append(FormatPrice(c.RtlOriginPrice)).Append('|').Append(c.SupportBar).Append('|')
                    .Append(FormatPrice(c.SupportPrice)).Append('|').Append(c.Level).Append('|')
                    .Append(c.JoinedParentId).Append('|').Append(c.Status).Append('\n');
            }
            for (int i = 0; i < formations.Count; i++)
            {
                AmbiguousFormation f = formations[i];
                canonical.Append("F|").Append(f.Id).Append('|').Append(f.OriginBar).Append('|')
                    .Append(f.EndBar).Append('|').Append(f.Status).Append('|')
                    .Append(f.LateralConfirmed).Append('|').Append(f.LateralEndBar).Append('|')
                    .Append(f.Up.Status).Append('|').Append(f.Down.Status).Append('\n');
            }
            for (int i = 0; i < joinedContainers.Count; i++)
            {
                JoinedContainer c = joinedContainers[i];
                canonical.Append("J|").Append(c.Id).Append('|').Append(c.Kind).Append('|')
                    .Append(c.Direction).Append('|').Append(c.StartBar).Append('|').Append(c.EndBar)
                    .Append('|').Append(FormatPrice(c.P1Price)).Append('|').Append(c.P3Bar)
                    .Append('|').Append(FormatPrice(c.P3Price)).Append('|').Append(c.Level).Append('\n');
            }

            ulong hash = 1469598103934665603UL;
            string value = canonical.ToString();
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                unchecked { hash *= 1099511628211UL; }
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private void ExportJson(int start, int end, string fingerprint)
        {
            try
            {
                string folder = string.IsNullOrWhiteSpace(ExportFolder)
                    ? Path.Combine(Core.Globals.UserDataDir, "APVAExports")
                    : ExportFolder;
                Directory.CreateDirectory(folder);
                string fileName = string.IsNullOrWhiteSpace(ExportFileName)
                    ? "xPvaAutomatedContainerV4_" + start + "_" + end + ".json"
                    : ExportFileName;
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";
                foreach (char invalid in Path.GetInvalidFileNameChars())
                    fileName = fileName.Replace(invalid, '_');
                string path = Path.Combine(folder, fileName);
                File.WriteAllText(path, BuildJson(start, end, fingerprint), Encoding.UTF8);
                Print("[APVA V4] export complete path=" + path);
            }
            catch (Exception ex)
            {
                Print("[APVA V4] export failed: " + ex.Message);
            }
        }

        private string BuildJson(int start, int end, string fingerprint)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"metadata\": {");
            AppendJsonProperty(sb, "indicatorName", "xPvaAutomatedContainerV4", true);
            AppendJsonProperty(sb, "build", "stage14-audit-export-v1", true);
            AppendJsonProperty(sb, "instrument", Instrument == null ? "" : Instrument.FullName, true);
            AppendJsonProperty(sb, "startBar", start, true);
            AppendJsonProperty(sb, "endBar", end, true);
            AppendJsonProperty(sb, "fingerprint", fingerprint, false);
            sb.Append("\n  },\n  \"bars\": [");
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    {");
                AppendJsonProperty(sb, "index", bar.Index, true);
                AppendJsonProperty(sb, "time", bar.Time.ToString("O", CultureInfo.InvariantCulture), true);
                AppendJsonProperty(sb, "open", bar.Open, true);
                AppendJsonProperty(sb, "high", bar.High, true);
                AppendJsonProperty(sb, "low", bar.Low, true);
                AppendJsonProperty(sb, "close", bar.Close, true);
                AppendJsonProperty(sb, "volume", bar.Volume, true);
                AppendJsonProperty(sb, "relation", bar.Relation.ToString(), false);
                sb.Append("}");
            }
            sb.Append("\n  ],\n  \"formations\": [");
            for (int i = 0; i < formations.Count; i++)
            {
                AmbiguousFormation f = formations[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    {");
                AppendJsonProperty(sb, "id", f.Id, true);
                AppendJsonProperty(sb, "originBar", f.OriginBar, true);
                AppendJsonProperty(sb, "endBar", f.EndBar, true);
                AppendJsonProperty(sb, "status", f.Status.ToString(), true);
                AppendJsonProperty(sb, "boundaryHigh", f.BoundaryHigh, true);
                AppendJsonProperty(sb, "boundaryLow", f.BoundaryLow, true);
                AppendJsonProperty(sb, "lateralConfirmed", f.LateralConfirmed ? 1 : 0, true);
                AppendJsonProperty(sb, "lateralEndBar", f.LateralEndBar, true);
                AppendJsonProperty(sb, "lateralBreakBar", f.LateralBreakBar, true);
                AppendJsonProperty(sb, "upStatus", f.Up.Status.ToString(), true);
                AppendJsonProperty(sb, "downStatus", f.Down.Status.ToString(), false);
                sb.Append("}");
            }
            sb.Append("\n  ],\n  \"ordinaryContainers\": [");
            for (int i = 0; i < ordinaryContainers.Count; i++)
            {
                OrdinaryContainer c = ordinaryContainers[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    {");
                AppendJsonProperty(sb, "id", c.Id, true);
                AppendJsonProperty(sb, "direction", c.Direction.ToString(), true);
                AppendJsonProperty(sb, "status", c.Status.ToString(), true);
                AppendJsonProperty(sb, "level", c.Level, true);
                AppendJsonProperty(sb, "startBar", c.OriginBar, true);
                AppendJsonProperty(sb, "endBar", c.EndBar, true);
                AppendJsonProperty(sb, "p1Price", c.RtlOriginPrice, true);
                AppendJsonProperty(sb, "p2Bar", c.P2Bar, true);
                AppendJsonProperty(sb, "p2Price", c.P2Price, true);
                AppendJsonProperty(sb, "p3Bar", c.SupportBar, true);
                AppendJsonProperty(sb, "p3Price", c.SupportPrice, true);
                AppendJsonProperty(sb, "parentFormationId", c.ParentFormationId, true);
                AppendJsonProperty(sb, "parentOrdinaryId", c.ParentOrdinaryContainerId, true);
                AppendJsonProperty(sb, "joinedParentId", c.JoinedParentId, false);
                sb.Append("}");
            }
            sb.Append("\n  ],\n  \"joinedContainers\": [");
            for (int i = 0; i < joinedContainers.Count; i++)
            {
                JoinedContainer c = joinedContainers[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    {");
                AppendJsonProperty(sb, "id", c.Id, true);
                AppendJsonProperty(sb, "kind", c.Kind ?? "", true);
                AppendJsonProperty(sb, "direction", c.Direction.ToString(), true);
                AppendJsonProperty(sb, "level", c.Level, true);
                AppendJsonProperty(sb, "startBar", c.StartBar, true);
                AppendJsonProperty(sb, "endBar", c.EndBar, true);
                AppendJsonProperty(sb, "p1Price", c.P1Price, true);
                AppendJsonProperty(sb, "p2Bar", c.P2Bar, true);
                AppendJsonProperty(sb, "p2Price", c.P2Price, true);
                AppendJsonProperty(sb, "p3Bar", c.P3Bar, true);
                AppendJsonProperty(sb, "p3Price", c.P3Price, false);
                sb.Append("}");
            }
            sb.Append("\n  ],\n  \"warnings\": [");
            for (int i = 0; i < auditFindings.Count; i++)
            {
                AuditFinding finding = auditFindings[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    {");
                AppendJsonProperty(sb, "severity", finding.Severity, true);
                AppendJsonProperty(sb, "code", finding.Code, true);
                AppendJsonProperty(sb, "subject", finding.Subject, true);
                AppendJsonProperty(sb, "message", finding.Message, false);
                sb.Append("}");
            }
            sb.Append("\n  ],\n  \"barMembership\": [],\n  \"events\": []\n}\n");
            return sb.ToString();
        }

        private void AppendJsonProperty(StringBuilder sb, string name, string value, bool comma)
        {
            sb.Append("\n      \"").Append(JsonEscape(name)).Append("\": \"")
                .Append(JsonEscape(value ?? string.Empty)).Append('"');
            if (comma) sb.Append(',');
        }

        private void AppendJsonProperty(StringBuilder sb, string name, int value, bool comma)
        {
            sb.Append("\n      \"").Append(JsonEscape(name)).Append("\": ")
                .Append(value.ToString(CultureInfo.InvariantCulture));
            if (comma) sb.Append(',');
        }

        private void AppendJsonProperty(StringBuilder sb, string name, double value, bool comma)
        {
            sb.Append("\n      \"").Append(JsonEscape(name)).Append("\": ")
                .Append(value.ToString("R", CultureInfo.InvariantCulture));
            if (comma) sb.Append(',');
        }

        private string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private void DisposeRenderResources()
        {
            if (upBrushDx != null)
            {
                upBrushDx.Dispose();
                upBrushDx = null;
            }
            if (downBrushDx != null)
            {
                downBrushDx.Dispose();
                downBrushDx = null;
            }
            if (lateralOutlineBrushDx != null)
            {
                lateralOutlineBrushDx.Dispose();
                lateralOutlineBrushDx = null;
            }
            if (dashStrokeStyle != null)
            {
                dashStrokeStyle.Dispose();
                dashStrokeStyle = null;
            }
            if (dotStrokeStyle != null)
            {
                dotStrokeStyle.Dispose();
                dotStrokeStyle = null;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaAutomatedContainerV4[] cachexPvaAutomatedContainerV4;
		public xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			return xPvaAutomatedContainerV4(Input, debug, startBar, endBar, enableJsonExport, exportFolder, exportFileName);
		}

		public xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(ISeries<double> input, bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			if (cachexPvaAutomatedContainerV4 != null)
				for (int idx = 0; idx < cachexPvaAutomatedContainerV4.Length; idx++)
					if (cachexPvaAutomatedContainerV4[idx] != null && cachexPvaAutomatedContainerV4[idx].Debug == debug && cachexPvaAutomatedContainerV4[idx].StartBar == startBar && cachexPvaAutomatedContainerV4[idx].EndBar == endBar && cachexPvaAutomatedContainerV4[idx].EnableJsonExport == enableJsonExport && cachexPvaAutomatedContainerV4[idx].ExportFolder == exportFolder && cachexPvaAutomatedContainerV4[idx].ExportFileName == exportFileName && cachexPvaAutomatedContainerV4[idx].EqualsInput(input))
						return cachexPvaAutomatedContainerV4[idx];
			return CacheIndicator<xPvaAutomatedContainerV4>(new xPvaAutomatedContainerV4(){ Debug = debug, StartBar = startBar, EndBar = endBar, EnableJsonExport = enableJsonExport, ExportFolder = exportFolder, ExportFileName = exportFileName }, input, ref cachexPvaAutomatedContainerV4);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			return indicator.xPvaAutomatedContainerV4(Input, debug, startBar, endBar, enableJsonExport, exportFolder, exportFileName);
		}

		public Indicators.xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(ISeries<double> input , bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			return indicator.xPvaAutomatedContainerV4(input, debug, startBar, endBar, enableJsonExport, exportFolder, exportFileName);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			return indicator.xPvaAutomatedContainerV4(Input, debug, startBar, endBar, enableJsonExport, exportFolder, exportFileName);
		}

		public Indicators.xPvaAutomatedContainerV4 xPvaAutomatedContainerV4(ISeries<double> input , bool debug, int startBar, int endBar, bool enableJsonExport, string exportFolder, string exportFileName)
		{
			return indicator.xPvaAutomatedContainerV4(input, debug, startBar, endBar, enableJsonExport, exportFolder, exportFileName);
		}
	}
}

#endregion
