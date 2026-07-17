#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.xPva.Engine;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaAutomatedContainerV3 : Indicator
    {
        private enum ContainerDirection
        {
            Unknown,
            Up,
            Down
        }

        private enum ContainerStatus
        {
            Active,
            Joined,
            Inactive,
            Broken
        }

        private sealed class BarSnapshot
        {
            public int Index;
            public DateTime Time;
            public double Open;
            public double High;
            public double Low;
            public double Close;
            public double Volume;
            public xPvaBarRelation Relation;
        }

        private sealed class TwoBarContainer
        {
            public int Id;
            public ContainerDirection Direction;
            public ContainerStatus Status;
            public int ParentId;
            public int Level;
            public bool SameDirectionBreakException;
            public int OriginBrokenContainerId;
            public bool LatentDirectionException;
            public int OriginLatentBar;
            public int PromotionSourceParentId;
            public int PromotedFromBrokenAncestorId;
            public int LineageId;
            public int OriginFttContainerId;
            public int OriginFttBar;
            public double OriginFttPrice;
            public bool IsJoinedParent;
            public int JoinLeftId;
            public int JoinMiddleId;
            public int JoinRightId;
            public int StartBar;
            public int EndBar;
            public int BreakBar;
            public double RtlStartPrice;
            public double RtlEndPrice;
            public int LtlStartBar;
            public double LtlStartPrice;
            public double LtlEndPrice;
            public int RtlSupportBar;
            public double RtlSupportPrice;
            public readonly List<OuterLineSegment> FrozenOuterSegments = new List<OuterLineSegment>();
            public int ActiveOuterStartBar;
            public double ActiveOuterStartPrice;
            public bool ActiveOuterIsVe;
            public string Reason;
        }

        private sealed class OuterLineSegment
        {
            public int StartBar;
            public int EndBar;
            public double StartPrice;
            public double EndPrice;
            public bool IsVe;
        }

        private sealed class LineageJoinRecord
        {
            public int Id;
            public int LineageId;
            public int LeftId;
            public int MiddleId;
            public int RightId;
            public ContainerDirection Direction;
            public int StartBar;
            public int EndBar;
            public int P3Bar;
            public double P3Price;
            public int OriginFttContainerId;
            public int OriginFttBar;
            public double OriginFttPrice;
        }

        private sealed class LateralFormationState
        {
            public int Id;
            public int OriginSnapshotIndex;
            public int OriginBar;
            public int EndBar;
            public double High;
            public double Low;
            public ContainerDirection Direction;
            public int ResolvedBar;
            public int ContainerId;
            public bool Closed;
            public int TerminationBar;
            public bool LifecycleTerminated;
            public xPvaBarRelation OpeningRelation;
            public int TranslationalBarCount;
        }

        private readonly List<BarSnapshot> snapshots = new List<BarSnapshot>();
        private readonly List<TwoBarContainer> containers = new List<TwoBarContainer>();
        private readonly List<TwoBarContainer> containersBrokenThisBar = new List<TwoBarContainer>();
        private readonly List<LineageJoinRecord> lineageJoins = new List<LineageJoinRecord>();
        private readonly List<LateralFormationState> lateralFormations = new List<LateralFormationState>();
        private xPvaDiscreteEventEngine eventEngine;
        private int nextLineageId;
        private string lastBuildSignature;
        private SolidColorBrush upBrushDx;
        private SolidColorBrush downBrushDx;
        private SolidColorBrush upBrokenBrushDx;
        private SolidColorBrush downBrokenBrushDx;
        private SolidColorBrush upInactiveBrushDx;
        private SolidColorBrush downInactiveBrushDx;
        private StrokeStyle dashStrokeStyle;
        private StrokeStyle dotStrokeStyle;
        private StrokeStyle deepLevelStrokeStyle;
        private SolidColorBrush lateralFillBrushDx;
        private SolidColorBrush lateralOutlineBrushDx;

        [NinjaScriptProperty]
        [Display(Name = "Debug", GroupName = "Diagnostics", Order = 1)]
        public bool Debug { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start Bar", GroupName = "Diagnostics", Order = 2)]
        public int StartBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End Bar", GroupName = "Diagnostics", Order = 3)]
        public int EndBar { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaAutomatedContainerV3";
                Description = "APVA V3 diagnostic foundation; no container behavior is implemented.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                PrintTo = PrintTo.OutputTab2;
                Debug = true;
                StartBar = 0;
                EndBar = 0;
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
                    Print("[APVA V3] bounds rejected: " + rejection);
                return;
            }

            if (CurrentBar < end)
                return;

            string signature = start.ToString(CultureInfo.InvariantCulture)
                + ":" + end.ToString(CultureInfo.InvariantCulture)
                + ":" + Debug.ToString();
            if (signature == lastBuildSignature)
                return;

            BuildDiagnosticSnapshot(start, end);
            lastBuildSignature = signature;
            ForceRefresh();
        }

        public override void OnRenderTargetChanged()
        {
            DisposeRenderResources();
            if (RenderTarget == null)
                return;

            upBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 102, 255, 255));
            downBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(220, 38, 38, 255));
            upBrokenBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 102, 255, 150));
            downBrokenBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(220, 38, 38, 150));
            upInactiveBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 102, 255, 85));
            downInactiveBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(220, 38, 38, 85));
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
            deepLevelStrokeStyle = new StrokeStyle(Core.Globals.D2DFactory,
                new StrokeStyleProperties
                {
                    DashStyle = SharpDX.Direct2D1.DashStyle.DashDotDot,
                    DashCap = CapStyle.Round,
                    StartCap = CapStyle.Round,
                    EndCap = CapStyle.Round
                });
            lateralFillBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(70, 70, 70, 20));
            lateralOutlineBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(70, 70, 70, 130));
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (chartControl == null || chartScale == null || ChartBars == null || RenderTarget == null)
                return;
            if (upBrushDx == null || downBrushDx == null || dashStrokeStyle == null || lateralFillBrushDx == null)
                OnRenderTargetChanged();
            if (upBrushDx == null || downBrushDx == null || dashStrokeStyle == null
                || lateralFillBrushDx == null || lateralOutlineBrushDx == null)
                return;

            AntialiasMode priorMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            for (int i = 0; i < lateralFormations.Count; i++)
            {
                LateralFormationState lateral = lateralFormations[i];
                int renderedEndBar = lateral.LifecycleTerminated
                    ? lateral.TerminationBar
                    : lateral.EndBar;
                if (renderedEndBar < ChartBars.FromIndex || lateral.OriginBar > ChartBars.ToIndex)
                    continue;

                float lateralX1 = chartControl.GetXByBarIndex(ChartBars, lateral.OriginBar);
                float lateralX2 = chartControl.GetXByBarIndex(ChartBars, renderedEndBar);
                float lateralY1 = chartScale.GetYByValue(lateral.High);
                float lateralY2 = chartScale.GetYByValue(lateral.Low);
                var rectangle = new RectangleF(
                    Math.Min(lateralX1, lateralX2),
                    Math.Min(lateralY1, lateralY2),
                    Math.Abs(lateralX2 - lateralX1),
                    Math.Abs(lateralY2 - lateralY1));
                RenderTarget.DrawRectangle(rectangle, lateralOutlineBrushDx, 1.0f);
            }

            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer container = containers[i];
                if (container.EndBar < ChartBars.FromIndex || container.StartBar > ChartBars.ToIndex)
                    continue;

                float x1 = chartControl.GetXByBarIndex(ChartBars, container.StartBar);
                float x2 = chartControl.GetXByBarIndex(ChartBars, container.EndBar);
                SolidColorBrush brush = BrushForContainer(container);
                StrokeStyle strokeStyle = StrokeForLevel(container.Level);
                float lineWidth = WidthForLevel(container.Level);
                RenderTarget.DrawLine(
                    new Vector2(x1, chartScale.GetYByValue(container.RtlStartPrice)),
                    new Vector2(x2, chartScale.GetYByValue(container.RtlEndPrice)),
                    brush,
                    lineWidth,
                    strokeStyle);

                for (int segmentIndex = 0; segmentIndex < container.FrozenOuterSegments.Count; segmentIndex++)
                {
                    OuterLineSegment segment = container.FrozenOuterSegments[segmentIndex];
                    float segmentX1 = chartControl.GetXByBarIndex(ChartBars, segment.StartBar);
                    float segmentX2 = chartControl.GetXByBarIndex(ChartBars, segment.EndBar);
                    RenderTarget.DrawLine(
                        new Vector2(segmentX1, chartScale.GetYByValue(segment.StartPrice)),
                        new Vector2(segmentX2, chartScale.GetYByValue(segment.EndPrice)),
                        brush,
                        lineWidth,
                        strokeStyle);
                }

                int span = container.EndBar - container.StartBar;
                double slope = span <= 0
                    ? 0.0
                    : (container.RtlEndPrice - container.RtlStartPrice) / span;
                double activeOuterEndPrice = container.ActiveOuterStartPrice
                    + slope * (container.EndBar - container.ActiveOuterStartBar);
                float activeX1 = chartControl.GetXByBarIndex(ChartBars, container.ActiveOuterStartBar);
                RenderTarget.DrawLine(
                    new Vector2(activeX1, chartScale.GetYByValue(container.ActiveOuterStartPrice)),
                    new Vector2(x2, chartScale.GetYByValue(activeOuterEndPrice)),
                    brush,
                    lineWidth,
                    strokeStyle);
            }
            RenderTarget.AntialiasMode = priorMode;
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
            {
                rejection = "End Bar exceeds CurrentBar";
                return false;
            }
            return true;
        }

        private void BuildDiagnosticSnapshot(int start, int end)
        {
            snapshots.Clear();
            containers.Clear();
            containersBrokenThisBar.Clear();
            lineageJoins.Clear();
            lateralFormations.Clear();
            for (int index = start; index <= end; index++)
                snapshots.Add(CreateSnapshot(index));

            BuildTwoBarContainers();

            if (!Debug)
                return;

            Print("[APVA V3] snapshot begin build=step14-nested-lateral-v35 start=" + start
                + " end=" + end
                + " count=" + snapshots.Count
                + " currentBar=" + CurrentBar);
            for (int i = 0; i < snapshots.Count; i++)
                Print(FormatSnapshot(snapshots[i]));
            Print("[APVA V3] snapshot end start=" + start
                + " end=" + end
                + " count=" + snapshots.Count
                + " containers=" + containers.Count
                + " lineageJoins=" + lineageJoins.Count
                + " laterals=" + lateralFormations.Count);
        }

        private void BuildTwoBarContainers()
        {
            int nextId = 1;
            nextLineageId = 1;
            int pendingIndex = 0;
            int preSyntheticIndex = -1;
            int nextLateralId = 1;
            LateralFormationState collectingLateral = null;
            LateralFormationState pendingLateralIntent = null;
            ContainerDirection latentDirection = ContainerDirection.Unknown;
            int latentOriginIndex = -1;
            int latentParentId = 0;
            int deferredBrokenContainerId = 0;
            ContainerDirection deferredResponseDirection = ContainerDirection.Unknown;
            for (int i = 1; i < snapshots.Count; i++)
            {
                containersBrokenThisBar.Clear();
                BarSnapshot end = snapshots[i];
                bool lateralResolvedThisBar = false;
                if (pendingLateralIntent != null
                    && pendingLateralIntent.ContainerId == 0
                    && end.Index > pendingLateralIntent.ResolvedBar)
                {
                    if (Debug)
                        Print("[APVA V3] lateral intent expired id=" + pendingLateralIntent.Id
                            + " origin=" + pendingLateralIntent.OriginBar
                            + " resolvedBar=" + pendingLateralIntent.ResolvedBar
                            + " bar=" + end.Index
                            + " reason=no container materialized on resolution bar");
                    pendingLateralIntent = null;
                }
                if (collectingLateral == null
                    && pendingLateralIntent == null
                    && i >= 2)
                {
                    BarSnapshot origin = snapshots[i - 2];
                    BarSnapshot second = snapshots[i - 1];
                    if (IsContainedByLateralOrigin(origin, second)
                        && IsContainedByLateralOrigin(origin, end))
                    {
                        collectingLateral = new LateralFormationState
                        {
                            Id = nextLateralId++,
                            OriginSnapshotIndex = i - 2,
                            OriginBar = origin.Index,
                            EndBar = end.Index,
                            High = origin.High,
                            Low = origin.Low,
                            Direction = ContainerDirection.Unknown,
                            ResolvedBar = -1,
                            ContainerId = 0,
                            Closed = false,
                            TerminationBar = -1,
                            LifecycleTerminated = false,
                            OpeningRelation = second.Relation,
                            TranslationalBarCount = 3
                        };
                        lateralFormations.Add(collectingLateral);
                        if (Debug)
                            Print("[APVA V3] lateral qualified id=" + collectingLateral.Id
                                + " origin=" + collectingLateral.OriginBar
                                + " bar2=" + second.Index
                                + " bar3=" + end.Index
                                + " range=" + FormatPrice(collectingLateral.Low)
                                + "-" + FormatPrice(collectingLateral.High));
                    }
                }
                if (collectingLateral != null)
                {
                    ContainerDirection resolvedDirection = end.Index <= collectingLateral.EndBar
                        ? ContainerDirection.Unknown
                        : LateralBodyBreakDirection(collectingLateral, end);
                    if (resolvedDirection == ContainerDirection.Unknown)
                    {
                        if (end.Index > collectingLateral.EndBar)
                            collectingLateral.EndBar = end.Index;
                    }
                    else
                    {
                        collectingLateral.Direction = resolvedDirection;
                        collectingLateral.ResolvedBar = end.Index;
                        collectingLateral.Closed = true;
                        pendingLateralIntent = collectingLateral;
                        pendingIndex = collectingLateral.OriginSnapshotIndex;
                        lateralResolvedThisBar = true;
                        if (Debug)
                            Print("[APVA V3] lateral resolved id=" + collectingLateral.Id
                                + " origin=" + collectingLateral.OriginBar
                                + " bar=" + end.Index
                                + " direction=" + collectingLateral.Direction
                                + " reason=entire candle body beyond lateral boundary");
                        collectingLateral = null;
                    }
                }

                ContainerDirection eventDirection = DirectionFromRelation(end.Relation);
                bool insideLikeBar = IsInsideLikeRelation(end.Relation);
                int responseLatentOriginIndex = latentOriginIndex;
                int responseLatentParentId = latentParentId;
                ContainerDirection responseLatentDirection = latentDirection;
                int responseDeferredBrokenContainerId = deferredBrokenContainerId;
                bool latentResponseThisBar = latentOriginIndex >= 0
                    && eventDirection != ContainerDirection.Unknown;
                bool deferredResponseThisBar = deferredBrokenContainerId != 0
                    && eventDirection == deferredResponseDirection
                    && !IsOutsideRelation(end.Relation);
                if (deferredResponseThisBar)
                {
                    deferredBrokenContainerId = 0;
                    deferredResponseDirection = ContainerDirection.Unknown;
                }
                if (pendingLateralIntent != null
                    && eventDirection == pendingLateralIntent.Direction
                    && !IsOutsideRelation(end.Relation))
                    pendingIndex = pendingLateralIntent.OriginSnapshotIndex;
                if (latentResponseThisBar)
                {
                    pendingIndex = latentOriginIndex;
                    latentDirection = ContainerDirection.Unknown;
                    latentOriginIndex = -1;
                    latentParentId = 0;
                }
                UpdateActiveAncestors(i);
                if (!latentResponseThisBar
                    && !deferredResponseThisBar
                    && !lateralResolvedThisBar
                    && TryExtendLastContainer(i))
                {
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }
                bool leafBrokenThisBar = !insideLikeBar && TryBreakLastContainer(i);
                if (leafBrokenThisBar
                    && IsOutsideRelation(end.Relation)
                    && containersBrokenThisBar.Count > 0)
                {
                    TwoBarContainer brokenLeaf = FindOutermostBrokenThisBar();
                    ContainerDirection breakResponseDirection = brokenLeaf.Direction == ContainerDirection.Up
                        ? ContainerDirection.Down
                        : ContainerDirection.Up;
                    int breakFttBar;
                    double breakFttPrice;
                    FindFttExtreme(brokenLeaf, breakResponseDirection, out breakFttBar, out breakFttPrice);
                    deferredBrokenContainerId = brokenLeaf.Id;
                    deferredResponseDirection = breakResponseDirection;
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    if (Debug)
                        Print("[APVA V3] break response deferred container=" + brokenLeaf.Id
                            + " origin=" + breakFttBar
                            + " bar=" + end.Index
                            + " direction=" + breakResponseDirection
                            + " reason=Outside Bar break direction follows broken container");
                    continue;
                }
                if (leafBrokenThisBar
                    && eventDirection == ContainerDirection.Unknown
                    && containersBrokenThisBar.Count > 0)
                {
                    TwoBarContainer brokenLeaf = FindOutermostBrokenThisBar();
                    latentDirection = brokenLeaf.Direction == ContainerDirection.Up
                        ? ContainerDirection.Down
                        : ContainerDirection.Up;
                    latentOriginIndex = i;
                    TwoBarContainer latentScope = FindContainerById(brokenLeaf.ParentId);
                    latentParentId = latentScope == null ? 0 : latentScope.Id;
                    pendingIndex = i;
                    if (Debug)
                        Print("[APVA V3] latent break response recorded bar=" + end.Index
                            + " direction=" + latentDirection
                            + " brokenContainer=" + brokenLeaf.Id
                            + " parent=" + (latentParentId == 0
                                ? "none"
                                : latentParentId.ToString(CultureInfo.InvariantCulture))
                            + " reason=break occurred on a translational bar");
                }
                if (!latentResponseThisBar
                    && !deferredResponseThisBar
                    && !lateralResolvedThisBar
                    && TryExtendLastContainer(i))
                {
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }
                if (!latentResponseThisBar
                    && !deferredResponseThisBar
                    && !lateralResolvedThisBar
                    && TryAdjustLastContainerToRtlWick(i))
                {
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }
                bool activeContainerAdvanced = !latentResponseThisBar
                    && !deferredResponseThisBar
                    && !lateralResolvedThisBar
                    && AdvanceNewestActiveContainerProjection(i);
                if (!latentResponseThisBar
                    && !deferredResponseThisBar
                    && !lateralResolvedThisBar
                    && TryAdvanceLastContainerThroughTranslationalBar(i))
                {
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }
                if (activeContainerAdvanced
                    && IsTranslational(end.Relation)
                    && !IsStitchRelation(end.Relation))
                {
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }

                bool stitchFromSyntheticOrigin = IsStitchRelation(end.Relation)
                    && IsSyntheticLateralBar(snapshots[pendingIndex].Relation);
                if (IsTranslational(end.Relation)
                    && !stitchFromSyntheticOrigin
                    && !lateralResolvedThisBar)
                {
                    bool resetsSyntheticOrigin = IsSyntheticLateralBar(end.Relation);
                    if (IsStitchRelation(end.Relation))
                    {
                        latentDirection = DirectionFromRelation(end.Relation);
                        latentOriginIndex = i;
                        TwoBarContainer latentParent = FindNewestActiveContainer();
                        latentParentId = latentParent == null ? 0 : latentParent.Id;
                        pendingIndex = i;
                        if (Debug)
                            Print("[APVA V3] latent direction recorded bar=" + end.Index
                                + " direction=" + latentDirection
                                + " parent=" + (latentParentId == 0 ? "none" : latentParentId.ToString(CultureInfo.InvariantCulture))
                                + " reason=stitch geometry is not drawable");
                    }
                    if (Debug)
                        Print("[APVA V3] construction skipped bar=" + end.Index
                            + " relation=" + end.Relation
                            + " pendingOrigin=" + snapshots[pendingIndex].Index
                            + " nextOrigin=" + snapshots[pendingIndex].Index);
                    if (resetsSyntheticOrigin)
                    {
                        preSyntheticIndex = pendingIndex;
                    }
                    continue;
                }

                BarSnapshot start = snapshots[pendingIndex];
                ContainerDirection direction = DirectionFromRelation(end.Relation);
                if (direction == ContainerDirection.Unknown)
                {
                    LogConstructionRejected(end.Index, start.Index, "relation has no construction direction");
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }

                bool resumesPreSyntheticOrigin = preSyntheticIndex >= 0
                    && DirectionFromRelation(snapshots[preSyntheticIndex].Relation) == direction;
                if (resumesPreSyntheticOrigin)
                {
                    pendingIndex = preSyntheticIndex;
                    start = snapshots[pendingIndex];
                }

                bool materializingLateral = lateralResolvedThisBar
                    && pendingLateralIntent != null
                    && pendingLateralIntent.ContainerId == 0
                    && start.Index == pendingLateralIntent.OriginBar
                    && direction == pendingLateralIntent.Direction;
                TwoBarContainer parent = materializingLateral ? null : FindNewestActiveContainer();
                TwoBarContainer directionOwner = parent == null
                    ? null
                    : FindLiveDirectionOwner(parent, direction);
                TwoBarContainer brokenOppositeChild = directionOwner == null
                    ? null
                    : FindBrokenOppositeChildThisBar(directionOwner, direction);
                if (brokenOppositeChild == null)
                    brokenOppositeChild = FindBrokenOppositeChildInScopeThisBar(parent, direction);
                if (brokenOppositeChild == null && deferredResponseThisBar)
                    brokenOppositeChild = FindContainerById(responseDeferredBrokenContainerId);
                bool latentBreakException = latentResponseThisBar;
                bool latentScopeMatches = latentBreakException
                    && directionOwner != null
                    && (responseLatentParentId == 0 || directionOwner.Id == responseLatentParentId);
                int originFttBar = 0;
                double originFttPrice = 0.0;
                if (brokenOppositeChild != null)
                {
                    FindFttExtreme(brokenOppositeChild, direction, out originFttBar, out originFttPrice);
                    for (int originIndex = 0; originIndex < snapshots.Count; originIndex++)
                    {
                        if (snapshots[originIndex].Index != originFttBar)
                            continue;
                        pendingIndex = originIndex;
                        start = snapshots[originIndex];
                        break;
                    }
                }
                if (IsOutsideRelation(end.Relation))
                {
                    if (brokenOppositeChild != null)
                    {
                        deferredBrokenContainerId = brokenOppositeChild.Id;
                        deferredResponseDirection = direction;
                        if (Debug)
                            Print("[APVA V3] break response deferred container="
                                + brokenOppositeChild.Id
                                + " origin=" + start.Index
                                + " bar=" + end.Index
                                + " direction=" + direction
                                + " reason=Outside Bar may only be the first response bar");
                    }
                    LogConstructionRejected(end.Index, start.Index, "second bar is Outside Bar");
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }

                bool geometryValid = direction == ContainerDirection.Up
                    ? end.Low > start.Low
                    : end.High < start.High;
                if (!geometryValid)
                {
                    string rule = direction == ContainerDirection.Up
                        ? "Up requires second low above first low"
                        : "Down requires second high below first high";
                    LogConstructionRejected(end.Index, start.Index, rule);
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }

                double rtlStart = direction == ContainerDirection.Up ? start.Low : start.High;
                double endpointPrice = direction == ContainerDirection.Up ? end.Low : end.High;
                double slope = (endpointPrice - rtlStart) / Math.Max(1, end.Index - start.Index);
                int supportBar = end.Index;
                double supportPrice = endpointPrice;
                for (int j = pendingIndex + 1; j < i; j++)
                {
                    BarSnapshot intervening = snapshots[j];
                    double candidatePrice = direction == ContainerDirection.Up ? intervening.Low : intervening.High;
                    double projected = rtlStart + slope * (intervening.Index - start.Index);
                    bool violates = direction == ContainerDirection.Up
                        ? candidatePrice < projected - TickSize * 0.5
                        : candidatePrice > projected + TickSize * 0.5;
                    if (!violates)
                        continue;

                    double candidateSlope = (candidatePrice - rtlStart) / Math.Max(1, intervening.Index - start.Index);
                    if ((direction == ContainerDirection.Up && candidateSlope < slope)
                        || (direction == ContainerDirection.Down && candidateSlope > slope))
                    {
                        slope = candidateSlope;
                        supportBar = intervening.Index;
                        supportPrice = candidatePrice;
                    }
                }

                double rtlEnd = rtlStart + slope * (end.Index - start.Index);
                double ltlStart = direction == ContainerDirection.Up ? start.High : start.Low;
                double ltlEnd = ltlStart + slope * (end.Index - start.Index);
                bool directionalSlopeValid = direction == ContainerDirection.Up ? slope > 0.0 : slope < 0.0;
                if (!directionalSlopeValid)
                {
                    LogConstructionRejected(end.Index, start.Index,
                        direction + " fitted RTL must retain a non-horizontal directional slope");
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }

                if (directionOwner != null
                    && brokenOppositeChild == null
                    && !latentScopeMatches)
                {
                    LogConstructionRejected(end.Index, start.Index,
                        "same-direction child blocked by live container id="
                        + directionOwner.Id);
                    pendingIndex = i;
                    preSyntheticIndex = -1;
                    continue;
                }
                if (brokenOppositeChild != null)
                    parent = directionOwner ?? FindContainerById(brokenOppositeChild.ParentId);
                else if (latentScopeMatches)
                    parent = directionOwner;

                var container = new TwoBarContainer
                {
                    Id = nextId++,
                    Direction = direction,
                    Status = ContainerStatus.Active,
                    ParentId = parent == null ? 0 : parent.Id,
                    Level = parent == null ? 1 : parent.Level + 1,
                    SameDirectionBreakException = brokenOppositeChild != null,
                    OriginBrokenContainerId = brokenOppositeChild == null ? 0 : brokenOppositeChild.Id,
                    LatentDirectionException = latentBreakException,
                    OriginLatentBar = latentBreakException ? snapshots[responseLatentOriginIndex].Index : 0,
                    LineageId = parent == null ? nextLineageId++ : parent.LineageId,
                    OriginFttContainerId = brokenOppositeChild == null ? 0 : brokenOppositeChild.Id,
                    OriginFttBar = originFttBar,
                    OriginFttPrice = originFttPrice,
                    StartBar = start.Index,
                    EndBar = end.Index,
                    BreakBar = -1,
                    RtlStartPrice = rtlStart,
                    RtlEndPrice = rtlEnd,
                    LtlStartBar = start.Index,
                    LtlStartPrice = ltlStart,
                    LtlEndPrice = ltlEnd,
                    RtlSupportBar = supportBar,
                    RtlSupportPrice = supportPrice,
                    Reason = latentBreakException
                        ? (direction == responseLatentDirection
                            ? "materialized latent direction from " + snapshots[responseLatentOriginIndex].Index
                            : "same-direction response after latent opposite state at " + snapshots[responseLatentOriginIndex].Index)
                        : (brokenOppositeChild != null
                        ? "same-direction response after opposite child break " + brokenOppositeChild.Id
                        : (resumesPreSyntheticOrigin
                            ? "same-direction synthetic continuation"
                            : (i == pendingIndex + 1 ? "adjacent two-bar construction" : "skipped-bar construction")))
                };
                containers.Add(container);
                if (pendingLateralIntent != null
                    && pendingLateralIntent.ContainerId == 0
                    && container.StartBar == pendingLateralIntent.OriginBar
                    && container.Direction == pendingLateralIntent.Direction)
                {
                    pendingLateralIntent.ContainerId = container.Id;
                    container.Reason = "resolved lateral formation " + pendingLateralIntent.Id;
                    AbsorbLateralHistory(container, pendingLateralIntent);
                    pendingLateralIntent = null;
                }
                if (parent != null)
                    AdvanceContainerProjection(parent, i, "child creation");
                RebuildOuterExpansion(container);

                pendingIndex = i;
                preSyntheticIndex = -1;
            }

            CreatePromotionTriadJoins();
            CreateTerminalLineageTriadJoins();
            CreateOrdinaryScopeTriadJoins();
            CreateMixedScopeJoinedTriads();
            RecordMixedLevelLineageJoins();
            EnforceHierarchyInvariants();
            LogFinalContainers();
            LogFinalLaterals();
        }

        private void AbsorbLateralHistory(
            TwoBarContainer lateralParent,
            LateralFormationState lateral)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer candidate = containers[i];
                if (candidate.Id == lateralParent.Id
                    || candidate.ParentId != 0
                    || candidate.StartBar < lateral.OriginBar
                    || candidate.StartBar > lateral.ResolvedBar)
                    continue;

                candidate.ParentId = lateralParent.Id;
                candidate.Level = lateralParent.Level + 1;
                AssignLineageRecursively(candidate, lateralParent.LineageId);
                RecalculateDescendantLevels(candidate);
                if (Debug)
                    Print("[APVA V3] lateral history absorbed container=" + candidate.Id
                        + " lateralParent=" + lateralParent.Id
                        + " level=" + candidate.Level
                        + " span=" + candidate.StartBar + "-" + candidate.EndBar);
            }
        }

        private void LogFinalLaterals()
        {
            if (!Debug)
                return;
            for (int i = 0; i < lateralFormations.Count; i++)
            {
                LateralFormationState lateral = lateralFormations[i];
                Print("[APVA V3] lateral final id=" + lateral.Id
                    + " origin=" + lateral.OriginBar
                    + " end=" + lateral.EndBar
                    + " renderEnd=" + (lateral.LifecycleTerminated ? lateral.TerminationBar : lateral.EndBar)
                    + " range=" + FormatPrice(lateral.Low) + "-" + FormatPrice(lateral.High)
                    + " direction=" + lateral.Direction
                    + " resolvedBar=" + lateral.ResolvedBar
                    + " container=" + lateral.ContainerId
                    + " status=" + (lateral.Closed ? "Resolved" : "Collecting")
                    + (lateral.LifecycleTerminated
                        ? " lifecycleTermination=" + lateral.TerminationBar
                        : ""));
            }
        }

        private bool TryBreakLastContainer(int endIndex)
        {
            if (containers.Count == 0)
                return false;

            TwoBarContainer container = FindNewestActiveContainer();
            if (container == null)
                return false;
            return TryBreakContainer(container, endIndex);
        }

        private void UpdateActiveAncestors(int endIndex)
        {
            TwoBarContainer leaf = FindNewestActiveContainer();
            int parentId = leaf == null ? 0 : leaf.ParentId;
            while (parentId != 0)
            {
                TwoBarContainer parent = FindContainerById(parentId);
                if (parent == null)
                    break;

                int nextParentId = parent.ParentId;
                bool insideLikeBar = IsInsideLikeRelation(snapshots[endIndex].Relation);
                bool handled = insideLikeBar
                    ? TryExtendContainer(parent, endIndex)
                        || TryAdjustContainerToRtlWick(parent, endIndex)
                    : TryBreakContainer(parent, endIndex)
                        || TryExtendContainer(parent, endIndex)
                        || TryAdjustContainerToRtlWick(parent, endIndex);
                if (!handled)
                    AdvanceContainerProjection(parent, endIndex, "live ancestor continuation");
                parentId = nextParentId;
            }
        }

        private bool AdvanceNewestActiveContainerProjection(int endIndex)
        {
            TwoBarContainer container = FindNewestActiveContainer();
            return AdvanceContainerProjection(container, endIndex, "active container continuation");
        }

        private bool AdvanceContainerProjection(TwoBarContainer container, int endIndex, string reason)
        {
            if (container == null || container.Status != ContainerStatus.Active)
                return false;

            BarSnapshot end = snapshots[endIndex];
            int span = container.EndBar - container.StartBar;
            if (span <= 0 || end.Index <= container.EndBar)
                return false;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            container.EndBar = end.Index;
            container.RtlEndPrice = container.RtlStartPrice
                + slope * (end.Index - container.StartBar);
            container.LtlEndPrice = container.LtlStartPrice
                + slope * (end.Index - container.LtlStartBar);
            container.Reason = reason;
            RebuildOuterExpansion(container);

            if (Debug)
                Print("[APVA V3] container continued id=" + container.Id
                    + " direction=" + container.Direction
                    + " start=" + container.StartBar
                    + " end=" + container.EndBar
                    + " reason=" + reason);
            return true;
        }

        private TwoBarContainer FindNewestActiveContainer()
        {
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                if (containers[i].Status == ContainerStatus.Active)
                    return containers[i];
            }
            return null;
        }

        private TwoBarContainer FindContainerById(int id)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i].Id == id)
                    return containers[i];
            }
            return null;
        }

        private bool HasLiveDirectionInAncestry(TwoBarContainer container, ContainerDirection direction)
        {
            return FindLiveDirectionOwner(container, direction) != null;
        }

        private TwoBarContainer FindBrokenOppositeChildThisBar(
            TwoBarContainer directionOwner,
            ContainerDirection responseDirection)
        {
            for (int i = containersBrokenThisBar.Count - 1; i >= 0; i--)
            {
                TwoBarContainer broken = containersBrokenThisBar[i];
                if (broken.ParentId == directionOwner.Id
                    && broken.Direction != responseDirection)
                    return broken;
            }
            return null;
        }

        private TwoBarContainer FindOutermostBrokenThisBar()
        {
            TwoBarContainer outermost = null;
            for (int i = 0; i < containersBrokenThisBar.Count; i++)
            {
                TwoBarContainer candidate = containersBrokenThisBar[i];
                if (outermost == null
                    || candidate.Level < outermost.Level
                    || (candidate.Level == outermost.Level
                        && candidate.StartBar < outermost.StartBar))
                    outermost = candidate;
            }
            return outermost;
        }

        private TwoBarContainer FindBrokenOppositeChildInScopeThisBar(
            TwoBarContainer activeScope,
            ContainerDirection responseDirection)
        {
            int scopeId = activeScope == null ? 0 : activeScope.Id;
            for (int i = containersBrokenThisBar.Count - 1; i >= 0; i--)
            {
                TwoBarContainer broken = containersBrokenThisBar[i];
                if (broken.Direction != responseDirection
                    && broken.ParentId == scopeId)
                    return broken;
            }
            return null;
        }

        private TwoBarContainer FindLiveDirectionOwner(TwoBarContainer container, ContainerDirection direction)
        {
            TwoBarContainer current = container;
            while (current != null)
            {
                if (current.Status == ContainerStatus.Active && current.Direction == direction)
                    return current;
                current = current.ParentId == 0 ? null : FindContainerById(current.ParentId);
            }
            return null;
        }

        private bool IsEntireBarOutsideLateral(LateralFormationState lateral, BarSnapshot bar)
        {
            return LateralBodyBreakDirection(lateral, bar) == lateral.Direction;
        }

        private bool IsContainedByLateralOrigin(BarSnapshot origin, BarSnapshot candidate)
        {
            double tolerance = TickSize * 0.5;
            return candidate.High <= origin.High + tolerance
                && candidate.Low >= origin.Low - tolerance;
        }

        private ContainerDirection LateralBodyBreakDirection(
            LateralFormationState lateral,
            BarSnapshot bar)
        {
            double tolerance = TickSize * 0.5;
            double bodyLow = Math.Min(bar.Open, bar.Close);
            double bodyHigh = Math.Max(bar.Open, bar.Close);
            if (bodyLow > lateral.High + tolerance)
                return ContainerDirection.Up;
            if (bodyHigh < lateral.Low - tolerance)
                return ContainerDirection.Down;
            return ContainerDirection.Unknown;
        }

        private void ForceTerminateLateralContainer(LateralFormationState lateral, int endIndex)
        {
            TwoBarContainer container = FindContainerById(lateral.ContainerId);
            if (container == null || container.Status != ContainerStatus.Active)
                return;

            BarSnapshot end = snapshots[endIndex];
            int span = container.EndBar - container.StartBar;
            if (span <= 0 || end.Index <= container.EndBar)
                return;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            container.Status = ContainerStatus.Broken;
            container.BreakBar = end.Index;
            container.EndBar = end.Index;
            container.RtlEndPrice = container.RtlStartPrice
                + slope * (end.Index - container.StartBar);
            container.LtlEndPrice = container.LtlStartPrice
                + slope * (end.Index - container.LtlStartBar);
            container.Reason = "lateral envelope termination";
            RebuildOuterExpansion(container);
            containersBrokenThisBar.Add(container);
            PromoteSurvivingChild(container, end.Index);

            if (Debug)
                Print("[APVA V3] lateral container terminated id=" + container.Id
                    + " lateral=" + lateral.Id
                    + " origin=" + container.StartBar
                    + " bar=" + end.Index
                    + " reason=entire bar outside lateral envelope");
        }

        private bool TryBreakContainer(TwoBarContainer container, int endIndex)
        {
            BarSnapshot end = snapshots[endIndex];
            int span = container.EndBar - container.StartBar;
            if (container.Status != ContainerStatus.Active
                || span <= 0
                || end.Index <= container.EndBar)
                return false;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            double projectedRtl = container.RtlStartPrice
                + slope * (end.Index - container.StartBar);
            double tolerance = TickSize * 0.5;
            bool fullBarBeyondRtl = container.Direction == ContainerDirection.Up
                ? end.High < projectedRtl - tolerance
                : end.Low > projectedRtl + tolerance;
            bool closeBeyondRtl = container.Direction == ContainerDirection.Up
                ? end.Close < projectedRtl - tolerance
                : end.Close > projectedRtl + tolerance;
            double candidateP3 = container.Direction == ContainerDirection.Up ? end.Low : end.High;
            bool rtlPenetrated = container.Direction == ContainerDirection.Up
                ? end.Low < projectedRtl - tolerance
                : end.High > projectedRtl + tolerance;
            bool candidateGeometryValid = container.Direction == ContainerDirection.Up
                ? candidateP3 > container.RtlStartPrice
                : candidateP3 < container.RtlStartPrice;
            bool outsideBarInvalidatesGeometry = IsOutsideRelation(end.Relation)
                && rtlPenetrated
                && !candidateGeometryValid;
            if (!fullBarBeyondRtl && !closeBeyondRtl && !outsideBarInvalidatesGeometry)
                return false;

            container.Status = ContainerStatus.Broken;
            container.BreakBar = end.Index;
            container.EndBar = end.Index;
            container.RtlEndPrice = projectedRtl;
            container.LtlEndPrice = container.LtlStartPrice
                + slope * (end.Index - container.LtlStartBar);
            container.Reason = outsideBarInvalidatesGeometry
                ? "Outside Bar invalidated RTL geometry"
                : (fullBarBeyondRtl ? "full-bar RTL break" : "close-through RTL break");
            RebuildOuterExpansion(container);
            containersBrokenThisBar.Add(container);

            if (Debug)
                Print("[APVA V3] container broken id=" + container.Id
                    + " direction=" + container.Direction
                    + " start=" + container.StartBar
                    + " breakBar=" + container.BreakBar
                    + " rtl=" + FormatPrice(projectedRtl)
                    + " range=" + FormatPrice(end.Low) + "-" + FormatPrice(end.High)
                    + " reason=" + (outsideBarInvalidatesGeometry
                        ? "Outside Bar penetrated RTL and invalidated P1/P3 geometry"
                        : (fullBarBeyondRtl ? "entire bar beyond RTL" : "close crossed RTL")));
            PromoteSurvivingChild(container, end.Index);
            return true;
        }

        private bool TryExtendLastContainer(int endIndex)
        {
            if (containers.Count == 0)
                return false;

            return TryExtendContainer(FindNewestActiveContainer(), endIndex);
        }

        private void PromoteSurvivingChild(TwoBarContainer brokenParent, int breakBar)
        {
            TwoBarContainer survivor = null;
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer candidate = containers[i];
                if (candidate.Status != ContainerStatus.Active
                    || candidate.Direction == brokenParent.Direction
                    || !IsDescendantOf(candidate, brokenParent.Id))
                    continue;

                if (survivor == null
                    || candidate.Level > survivor.Level
                    || (candidate.Level == survivor.Level && candidate.StartBar > survivor.StartBar))
                    survivor = candidate;
            }
            if (survivor == null)
                return;

            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer displaced = containers[i];
                if (displaced.Id == survivor.Id
                    || displaced.Status != ContainerStatus.Active
                    || !IsDescendantOf(displaced, brokenParent.Id)
                    || IsDescendantOf(displaced, survivor.Id))
                    continue;

                displaced.Status = ContainerStatus.Inactive;
                displaced.Reason = "ancestor branch displaced by promotion";
                if (Debug)
                    Print("[APVA V3] container inactive id=" + displaced.Id
                        + " bar=" + breakBar
                        + " reason=ancestor branch displaced by promotion");
            }

            int priorParentId = survivor.ParentId;
            int priorLevel = survivor.Level;
            if (survivor.OriginFttContainerId == 0)
            {
                TwoBarContainer priorParent = FindContainerById(priorParentId);
                if (priorParent != null)
                {
                    int fttBar;
                    double fttPrice;
                    FindFttExtreme(priorParent, survivor.Direction, out fttBar, out fttPrice);
                    survivor.OriginFttContainerId = priorParent.Id;
                    survivor.OriginFttBar = fttBar;
                    survivor.OriginFttPrice = fttPrice;
                }
            }
            survivor.ParentId = brokenParent.ParentId;
            survivor.Level = brokenParent.Level;
            survivor.PromotionSourceParentId = priorParentId;
            survivor.PromotedFromBrokenAncestorId = brokenParent.Id;
            survivor.Reason = "promoted after parent break";
            RecalculateDescendantLevels(survivor);

            if (Debug)
                Print("[APVA V3] container promoted id=" + survivor.Id
                    + " brokenParent=" + brokenParent.Id
                    + " parent=" + (survivor.ParentId == 0
                        ? "none"
                        : survivor.ParentId.ToString(CultureInfo.InvariantCulture))
                    + " level=" + priorLevel + "->" + survivor.Level
                    + " bar=" + breakBar
                    + " priorParent=" + priorParentId);
        }

        private bool IsDescendantOf(TwoBarContainer container, int ancestorId)
        {
            if (container == null || ancestorId == 0)
                return false;

            var visited = new HashSet<int>();
            TwoBarContainer current = container;
            while (current.ParentId != 0 && visited.Add(current.Id))
            {
                if (current.ParentId == ancestorId)
                    return true;
                current = FindContainerById(current.ParentId);
                if (current == null)
                    return false;
            }
            return false;
        }

        private void RecalculateDescendantLevels(TwoBarContainer parent)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer child = containers[i];
                if (child.ParentId != parent.Id)
                    continue;

                int priorLevel = child.Level;
                child.Level = parent.Level + 1;
                if (Debug && priorLevel != child.Level)
                    Print("[APVA V3] container level recalculated id=" + child.Id
                        + " parent=" + parent.Id
                        + " level=" + priorLevel + "->" + child.Level);
                RecalculateDescendantLevels(child);
            }
        }

        private void DeactivateBranch(TwoBarContainer container, int bar, string reason)
        {
            if (container.Status == ContainerStatus.Active)
            {
                container.Status = ContainerStatus.Inactive;
                container.Reason = reason;
                if (Debug)
                    Print("[APVA V3] container inactive id=" + container.Id
                        + " bar=" + bar
                        + " reason=" + reason);
            }

            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i].ParentId == container.Id)
                    DeactivateBranch(containers[i], bar, "ancestor branch deactivated");
            }
        }

        private void CreatePromotionTriadJoins()
        {
            int originalCount = containers.Count;
            for (int i = 0; i < originalCount; i++)
            {
                TwoBarContainer right = containers[i];
                if (right.PromotionSourceParentId == 0
                    || right.PromotedFromBrokenAncestorId == 0
                    || right.Status != ContainerStatus.Active)
                    continue;

                TwoBarContainer middle = FindContainerById(right.PromotionSourceParentId);
                TwoBarContainer left = middle == null
                    ? null
                    : FindContainerById(middle.OriginBrokenContainerId);
                if (left == null
                    || middle == null
                    || middle.OriginBrokenContainerId != left.Id
                    || left.Direction != right.Direction
                    || middle.Direction == right.Direction
                    || !(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar)
                    || left.EndBar > middle.StartBar + 1
                    || middle.StartBar > left.EndBar + 1)
                    continue;

                TryCreateJoinedParent(left, middle, right);
            }
        }

        private void CreateOrdinaryScopeTriadJoins()
        {
            var eligible = new List<TwoBarContainer>();
            bool createdAny = false;
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer container = containers[i];
                if (container.Status != ContainerStatus.Joined
                    && !HasJoinedParent(container))
                    eligible.Add(container);
            }
            eligible.Sort((a, b) =>
            {
                int parentCompare = a.ParentId.CompareTo(b.ParentId);
                if (parentCompare != 0)
                    return parentCompare;
                int levelCompare = a.Level.CompareTo(b.Level);
                if (levelCompare != 0)
                    return levelCompare;
                int startCompare = a.StartBar.CompareTo(b.StartBar);
                return startCompare != 0 ? startCompare : a.Id.CompareTo(b.Id);
            });

            for (int leftIndex = 0; leftIndex + 2 < eligible.Count; leftIndex++)
            {
                TwoBarContainer left = eligible[leftIndex];
                if (HasJoinedParent(left))
                    continue;
                bool joined = false;
                for (int middleIndex = leftIndex + 1; middleIndex + 1 < eligible.Count && !joined; middleIndex++)
                {
                    TwoBarContainer middle = eligible[middleIndex];
                    if (HasJoinedParent(middle)
                        || left.ParentId != middle.ParentId
                        || left.Level != middle.Level
                        || left.Direction == middle.Direction
                        || (!left.IsJoinedParent
                            && left.EndBar > middle.StartBar + 1)
                        || !IsBreakLinkedToLeft(left, middle)
                        || HasCompleteInterveningSibling(eligible, left, middle))
                        continue;

                    for (int rightIndex = middleIndex + 1; rightIndex < eligible.Count; rightIndex++)
                    {
                        TwoBarContainer right = eligible[rightIndex];
                        if (HasJoinedParent(right)
                            || middle.ParentId != right.ParentId
                            || middle.Level != right.Level
                            || left.Direction != right.Direction
                            || (left.IsJoinedParent && right.EndBar <= left.EndBar)
                            || (middle.Level > 1 && middle.EndBar > right.StartBar))
                            continue;

                        bool p1GeometryValid = left.Direction == ContainerDirection.Up
                            ? left.RtlStartPrice < middle.RtlStartPrice
                                && middle.RtlStartPrice > right.RtlStartPrice
                            : left.RtlStartPrice > middle.RtlStartPrice
                                && middle.RtlStartPrice < right.RtlStartPrice;
                        if (!p1GeometryValid)
                            continue;

                        if (TryCreateJoinedParent(left, middle, right))
                        {
                            createdAny = true;
                            joined = true;
                            break;
                        }
                    }
                }
            }

            if (createdAny)
                CreateOrdinaryScopeTriadJoins();
        }

        private void CreateTerminalLineageTriadJoins()
        {
            var rights = new List<TwoBarContainer>();
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer candidate = containers[i];
                TwoBarContainer middle = FindContainerById(candidate.OriginBrokenContainerId);
                TwoBarContainer commonParent = FindContainerById(candidate.ParentId);
                if (middle != null
                    && commonParent != null
                    && commonParent.Status == ContainerStatus.Broken
                    && commonParent.Direction != candidate.Direction
                    && middle.Direction != candidate.Direction
                    && middle.ParentId == candidate.ParentId
                    && middle.Level == candidate.Level
                    && middle.LineageId == candidate.LineageId)
                    rights.Add(candidate);
            }
            rights.Sort((a, b) =>
            {
                int endCompare = b.EndBar.CompareTo(a.EndBar);
                return endCompare != 0 ? endCompare : b.Id.CompareTo(a.Id);
            });

            for (int i = 0; i < rights.Count; i++)
            {
                TwoBarContainer right = rights[i];
                if (HasJoinedParent(right))
                    continue;
                TwoBarContainer middle = FindContainerById(right.OriginBrokenContainerId);
                TwoBarContainer cursor = middle == null
                    ? null
                    : FindContainerById(middle.OriginBrokenContainerId != 0
                        ? middle.OriginBrokenContainerId
                        : middle.OriginFttContainerId);
                TwoBarContainer left = null;
                int chainLength = 0;
                while (cursor != null && chainLength++ < containers.Count)
                {
                    if (cursor.ParentId != right.ParentId
                        || cursor.Level != right.Level
                        || cursor.LineageId != right.LineageId)
                        break;
                    if (cursor.Direction == right.Direction)
                        left = cursor;
                    int predecessorId = cursor.OriginBrokenContainerId != 0
                        ? cursor.OriginBrokenContainerId
                        : cursor.OriginFttContainerId;
                    cursor = FindContainerById(predecessorId);
                }

                if (left == null
                    || left.Id == FindImmediateLineagePredecessorId(middle)
                    || HasJoinedParent(left)
                    || left.StartBar >= middle.StartBar)
                    continue;

                if (TryCreateJoinedParent(left, middle, right) && Debug)
                    Print("[APVA V3] terminal lineage triad materialized children="
                        + left.Id + "," + middle.Id + "," + right.Id);
            }
        }

        private int FindImmediateLineagePredecessorId(TwoBarContainer container)
        {
            if (container == null)
                return 0;
            return container.OriginBrokenContainerId != 0
                ? container.OriginBrokenContainerId
                : container.OriginFttContainerId;
        }

        private bool IsBreakLinkedToLeft(
            TwoBarContainer left,
            TwoBarContainer middle)
        {
            if (middle.OriginBrokenContainerId == 0)
                return true;
            if (middle.OriginBrokenContainerId == left.Id)
                return true;

            TwoBarContainer brokenOrigin = FindContainerById(middle.OriginBrokenContainerId);
            return brokenOrigin != null && IsDescendantOf(brokenOrigin, left.Id);
        }

        private bool HasCompleteInterveningSibling(
            List<TwoBarContainer> eligible,
            TwoBarContainer left,
            TwoBarContainer middle)
        {
            for (int i = 0; i < eligible.Count; i++)
            {
                TwoBarContainer candidate = eligible[i];
                if (candidate.Id == left.Id
                    || candidate.Id == middle.Id
                    || candidate.ParentId != left.ParentId
                    || candidate.Level != left.Level
                    || candidate.StartBar < left.EndBar
                    || candidate.EndBar > middle.StartBar)
                    continue;
                return true;
            }
            return false;
        }

        private bool HasJoinedParent(TwoBarContainer container)
        {
            if (container == null || container.ParentId == 0)
                return false;
            TwoBarContainer parent = FindContainerById(container.ParentId);
            return parent != null && parent.IsJoinedParent;
        }

        private void CreateMixedScopeJoinedTriads()
        {
            int originalCount = containers.Count;
            var rights = new List<TwoBarContainer>();
            for (int i = 0; i < originalCount; i++)
            {
                if (containers[i].IsJoinedParent)
                    rights.Add(containers[i]);
            }
            rights.Sort((a, b) =>
            {
                int endCompare = b.EndBar.CompareTo(a.EndBar);
                return endCompare != 0 ? endCompare : b.Id.CompareTo(a.Id);
            });

            for (int i = 0; i < rights.Count; i++)
            {
                TwoBarContainer right = rights[i];
                if (HasJoinedParent(right))
                    continue;

                TwoBarContainer middle = FindMixedScopeMiddle(right);
                if (middle == null
                    || HasJoinedParent(middle)
                    || middle.Direction == right.Direction
                    || middle.StartBar >= right.StartBar)
                    continue;

                TwoBarContainer left = null;
                for (int j = 0; j < originalCount; j++)
                {
                    TwoBarContainer candidate = containers[j];
                    if (!candidate.IsJoinedParent
                        || HasJoinedParent(candidate)
                        || candidate.Direction != right.Direction
                        || candidate.LineageId != middle.LineageId
                        || candidate.StartBar >= middle.StartBar
                        || (candidate.EndBar > middle.StartBar
                            && !IsDescendantOf(FindContainerById(middle.OriginFttContainerId), candidate.Id)))
                        continue;

                    if (left == null
                        || candidate.EndBar > left.EndBar
                        || (candidate.EndBar == left.EndBar && candidate.Id > left.Id))
                        left = candidate;
                }
                if (left == null)
                    continue;

                TwoBarContainer originScope = FindContainerById(left.ParentId);
                if (TryCreateJoinedParent(left, middle, right, originScope) && Debug)
                    Print("[APVA V3] mixed-scope joined triad materialized children="
                        + left.Id + "," + middle.Id + "," + right.Id
                        + " start=" + left.StartBar
                        + " end=" + right.EndBar);
            }
        }

        private TwoBarContainer FindMixedScopeMiddle(TwoBarContainer right)
        {
            TwoBarContainer middle = null;
            TwoBarContainer branch = FindContainerById(right.JoinLeftId);
            var visited = new HashSet<int>();
            while (branch != null && visited.Add(branch.Id))
            {
                TwoBarContainer candidate = FindContainerById(branch.OriginFttContainerId);
                if (candidate != null
                    && candidate.Direction != right.Direction
                    && candidate.StartBar < right.StartBar
                    && (middle == null || candidate.StartBar < middle.StartBar))
                    middle = candidate;
                if (!branch.IsJoinedParent)
                    break;
                branch = FindContainerById(branch.JoinLeftId);
            }
            return middle;
        }

        private void RecordMixedLevelLineageJoins()
        {
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer right = containers[i];
                if (right.LineageId == 0
                    || right.OriginFttContainerId == 0
                    || right.Status != ContainerStatus.Active)
                    continue;

                TwoBarContainer middle = FindContainerById(right.OriginFttContainerId);
                if (middle == null || middle.LineageId != right.LineageId || middle.Direction == right.Direction)
                    continue;

                TwoBarContainer left = null;
                for (int j = 0; j < containers.Count; j++)
                {
                    TwoBarContainer candidate = containers[j];
                    if (candidate.Id == right.Id
                        || candidate.LineageId != right.LineageId
                        || candidate.Direction != right.Direction
                        || candidate.StartBar >= middle.StartBar)
                        continue;

                    bool structurallyAdjacent = middle.ParentId == candidate.Id
                        || (middle.ParentId == candidate.ParentId && middle.Level == candidate.Level);
                    if (!structurallyAdjacent)
                        continue;
                    if (left == null || candidate.StartBar > left.StartBar)
                        left = candidate;
                }
                if (left == null || !(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
                    continue;

                bool isMixedContext = left.Level != middle.Level
                    || middle.Level != right.Level
                    || left.ParentId != middle.ParentId
                    || middle.ParentId != right.ParentId;
                if (!isMixedContext)
                    continue;

                bool p1GeometryValid = right.Direction == ContainerDirection.Up
                    ? left.RtlStartPrice < middle.RtlStartPrice
                        && middle.RtlStartPrice > right.RtlStartPrice
                    : left.RtlStartPrice > middle.RtlStartPrice
                        && middle.RtlStartPrice < right.RtlStartPrice;
                if (!p1GeometryValid)
                    continue;

                int p3Bar;
                double p3Price;
                if (!TryFindLineageJoinP3(left, right, out p3Bar, out p3Price))
                    continue;

                var record = new LineageJoinRecord
                {
                    Id = lineageJoins.Count + 1,
                    LineageId = right.LineageId,
                    LeftId = left.Id,
                    MiddleId = middle.Id,
                    RightId = right.Id,
                    Direction = right.Direction,
                    StartBar = left.StartBar,
                    EndBar = right.EndBar,
                    P3Bar = p3Bar,
                    P3Price = p3Price,
                    OriginFttContainerId = right.OriginFttContainerId,
                    OriginFttBar = right.OriginFttBar,
                    OriginFttPrice = right.OriginFttPrice
                };
                lineageJoins.Add(record);

                if (Debug)
                    Print("[APVA V3] mixed-level lineage join recorded id=" + record.Id
                        + " lineage=" + record.LineageId
                        + " triad=" + record.LeftId + "," + record.MiddleId + "," + record.RightId
                        + " levels=" + left.Level + "," + middle.Level + "," + right.Level
                        + " parents=" + left.ParentId + "," + middle.ParentId + "," + right.ParentId
                        + " originFtt=" + record.OriginFttContainerId + "@" + record.OriginFttBar
                        + " p3=" + record.P3Bar + "@" + FormatPrice(record.P3Price)
                        + " visualMutation=False");
            }
        }

        private bool TryFindLineageJoinP3(
            TwoBarContainer left,
            TwoBarContainer right,
            out int p3Bar,
            out double p3Price)
        {
            p3Bar = 0;
            p3Price = 0.0;
            bool found = false;
            double selectedSlope = 0.0;
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= left.StartBar || bar.Index > right.EndBar)
                    continue;

                double candidatePrice = left.Direction == ContainerDirection.Up ? bar.Low : bar.High;
                bool geometryValid = left.Direction == ContainerDirection.Up
                    ? candidatePrice > left.RtlStartPrice
                    : candidatePrice < left.RtlStartPrice;
                if (!geometryValid)
                    return false;

                double candidateSlope = (candidatePrice - left.RtlStartPrice)
                    / (bar.Index - left.StartBar);
                bool tighter = !found
                    || (left.Direction == ContainerDirection.Up && candidateSlope < selectedSlope)
                    || (left.Direction == ContainerDirection.Down && candidateSlope > selectedSlope);
                if (!tighter)
                    continue;

                found = true;
                selectedSlope = candidateSlope;
                p3Bar = bar.Index;
                p3Price = candidatePrice;
            }
            return found && (left.Direction == ContainerDirection.Up ? selectedSlope > 0.0 : selectedSlope < 0.0);
        }

        private bool TryCreateJoinedParent(
            TwoBarContainer left,
            TwoBarContainer middle,
            TwoBarContainer right,
            TwoBarContainer originScope = null)
        {
            int startBar = left.StartBar;
            int endBar = right.EndBar;
            double p1Price = left.RtlStartPrice;
            bool foundSupport = false;
            int p3Bar = 0;
            double p3Price = 0.0;
            double selectedSlope = 0.0;

            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= startBar || bar.Index > endBar)
                    continue;

                double candidatePrice = left.Direction == ContainerDirection.Up ? bar.Low : bar.High;
                bool geometryValid = left.Direction == ContainerDirection.Up
                    ? candidatePrice > p1Price
                    : candidatePrice < p1Price;
                if (!geometryValid)
                {
                    if (Debug)
                        Print("[APVA V3] triad join rejected children="
                            + left.Id + "," + middle.Id + "," + right.Id
                            + " bar=" + bar.Index
                            + " reason=strict joined P1/P3 geometry failed");
                    return false;
                }

                double candidateSlope = (candidatePrice - p1Price) / (bar.Index - startBar);
                bool tighter = !foundSupport
                    || (left.Direction == ContainerDirection.Up && candidateSlope < selectedSlope)
                    || (left.Direction == ContainerDirection.Down && candidateSlope > selectedSlope);
                if (!tighter)
                    continue;

                foundSupport = true;
                p3Bar = bar.Index;
                p3Price = candidatePrice;
                selectedSlope = candidateSlope;
            }

            if (!foundSupport || (left.Direction == ContainerDirection.Up ? selectedSlope <= 0.0 : selectedSlope >= 0.0))
                return false;

            int nextId = 1;
            for (int i = 0; i < containers.Count; i++)
                nextId = Math.Max(nextId, containers[i].Id + 1);

            TwoBarContainer brokenAncestor = FindContainerById(right.PromotedFromBrokenAncestorId);
            int parentId = originScope != null
                ? originScope.ParentId
                : brokenAncestor == null ? right.ParentId : brokenAncestor.ParentId;
            int level = originScope != null
                ? originScope.Level
                : brokenAncestor == null ? right.Level : brokenAncestor.Level;
            BarSnapshot ltlAnchor = FindSnapshotByBar(middle.StartBar);
            if (ltlAnchor == null)
                return false;
            double ltlStartPrice = left.Direction == ContainerDirection.Up
                ? ltlAnchor.High
                : ltlAnchor.Low;
            var joined = new TwoBarContainer
            {
                Id = nextId,
                Direction = left.Direction,
                Status = ContainerStatus.Active,
                ParentId = parentId,
                Level = level,
                StartBar = startBar,
                EndBar = endBar,
                BreakBar = -1,
                RtlStartPrice = p1Price,
                RtlEndPrice = p1Price + selectedSlope * (endBar - startBar),
                LtlStartBar = middle.StartBar,
                LtlStartPrice = ltlStartPrice,
                LtlEndPrice = ltlStartPrice + selectedSlope * (endBar - middle.StartBar),
                RtlSupportBar = p3Bar,
                RtlSupportPrice = p3Price,
                LineageId = left.LineageId == 0 ? nextLineageId++ : left.LineageId,
                OriginFttContainerId = right.OriginFttContainerId,
                OriginFttBar = right.OriginFttBar,
                OriginFttPrice = right.OriginFttPrice,
                IsJoinedParent = true,
                JoinLeftId = left.Id,
                JoinMiddleId = middle.Id,
                JoinRightId = right.Id,
                Reason = "joined promotion triad " + left.Id + "," + middle.Id + "," + right.Id
            };

            left.ParentId = joined.Id;
            middle.ParentId = joined.Id;
            right.ParentId = joined.Id;
            if (middle.Status != ContainerStatus.Broken)
                middle.Status = ContainerStatus.Joined;
            if (right.Status != ContainerStatus.Broken)
                right.Status = ContainerStatus.Joined;
            left.Reason = "joined component of " + joined.Id;
            middle.Reason = "joined component of " + joined.Id;
            right.Reason = "joined component of " + joined.Id;

            containers.Add(joined);
            AbsorbEnclosedSiblings(joined, left, middle, right);
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer latentResponse = containers[i];
                if (latentResponse.Id == joined.Id
                    || !latentResponse.LatentDirectionException
                    || latentResponse.Direction != joined.Direction
                    || latentResponse.StartBar < right.StartBar
                    || latentResponse.StartBar > joined.EndBar
                    || latentResponse.ParentId != joined.ParentId)
                    continue;

                latentResponse.ParentId = joined.Id;
                latentResponse.Level = joined.Level + 1;
                latentResponse.LineageId = joined.LineageId;
                if (Debug)
                    Print("[APVA V3] latent response absorbed container=" + latentResponse.Id
                        + " joinedParent=" + joined.Id
                        + " level=" + latentResponse.Level
                        + " origin=" + latentResponse.OriginLatentBar);
            }
            AssignLineageRecursively(joined, joined.LineageId);
            ReplayJoinedLifecycle(joined, endBar);
            AbsorbEnclosedSiblings(joined, left, middle, right);
            AssignLineageRecursively(joined, joined.LineageId);
            RebuildOuterExpansion(joined);
            RecalculateDescendantLevels(joined);

            if (Debug)
                Print("[APVA V3] triad join accepted parent=" + joined.Id
                    + " direction=" + joined.Direction
                    + " children=" + left.Id + "," + middle.Id + "," + right.Id
                    + " start=" + joined.StartBar
                    + " end=" + joined.EndBar
                    + " p3=" + joined.RtlSupportBar + "@" + FormatPrice(joined.RtlSupportPrice)
                    + " lineage=" + joined.LineageId
                    + " originFtt=" + joined.OriginFttContainerId + "@" + joined.OriginFttBar
                    + " level=" + joined.Level);
            return true;
        }

        private void AbsorbEnclosedSiblings(TwoBarContainer joined, TwoBarContainer left,
            TwoBarContainer middle, TwoBarContainer right)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer enclosedSibling = containers[i];
                if (enclosedSibling.Id == joined.Id
                    || enclosedSibling.Id == left.Id
                    || enclosedSibling.Id == middle.Id
                    || enclosedSibling.Id == right.Id
                    || enclosedSibling.ParentId == joined.Id
                    || enclosedSibling.StartBar <= joined.StartBar
                    || enclosedSibling.EndBar > joined.EndBar)
                    continue;

                bool sameScope = enclosedSibling.ParentId == joined.ParentId
                    && enclosedSibling.Level == joined.Level;
                TwoBarContainer brokenOrigin = FindContainerById(enclosedSibling.OriginBrokenContainerId);
                bool breakLinkedInside = brokenOrigin != null
                    && (brokenOrigin.Id == joined.Id || IsDescendantOf(brokenOrigin, joined.Id));
                if (!sameScope && !breakLinkedInside)
                    continue;

                enclosedSibling.ParentId = joined.Id;
                enclosedSibling.Level = joined.Level + 1;
                enclosedSibling.LineageId = joined.LineageId;
                if (Debug)
                    Print("[APVA V3] enclosed sibling absorbed container=" + enclosedSibling.Id
                        + " joinedParent=" + joined.Id
                        + " level=" + enclosedSibling.Level
                        + " span=" + enclosedSibling.StartBar + "-" + enclosedSibling.EndBar);
            }
        }

        private void AssignLineageRecursively(TwoBarContainer container, int lineageId)
        {
            if (container == null)
                return;

            container.LineageId = lineageId;
            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i].ParentId == container.Id)
                    AssignLineageRecursively(containers[i], lineageId);
            }
        }

        private void ReplayJoinedLifecycle(TwoBarContainer joined, int initialEndBar)
        {
            int initialSpan = initialEndBar - joined.StartBar;
            if (initialSpan <= 0)
                return;

            double slope = (joined.RtlEndPrice - joined.RtlStartPrice) / initialSpan;
            double lineEpsilon = Math.Max(1e-12, TickSize * 1e-9);
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= initialEndBar)
                    continue;

                double projectedRtl = joined.RtlStartPrice
                    + slope * (bar.Index - joined.StartBar);
                joined.EndBar = bar.Index;
                joined.RtlEndPrice = projectedRtl;
                joined.LtlEndPrice = joined.LtlStartPrice
                    + slope * (bar.Index - joined.LtlStartBar);

                bool rtlTraversed = joined.Direction == ContainerDirection.Up
                    ? bar.Low < projectedRtl - lineEpsilon
                    : bar.High > projectedRtl + lineEpsilon;
                if (!rtlTraversed)
                    continue;

                joined.Status = ContainerStatus.Broken;
                joined.BreakBar = bar.Index;
                joined.Reason = "joined RTL traversal";
                if (Debug)
                    Print("[APVA V3] joined container terminated id=" + joined.Id
                        + " direction=" + joined.Direction
                        + " bar=" + joined.BreakBar
                        + " rtl=" + FormatPrice(projectedRtl)
                        + " range=" + FormatPrice(bar.Low) + "-" + FormatPrice(bar.High)
                        + " reason=first post-join RTL traversal");
                break;
            }
        }

        private void EnforceHierarchyInvariants()
        {
            int validationBar = snapshots.Count == 0 ? 0 : snapshots[snapshots.Count - 1].Index;
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer container = containers[i];
                if (container.Status != ContainerStatus.Active || container.ParentId == 0)
                    continue;

                var visited = new HashSet<int>();
                TwoBarContainer current = container;
                string invalidReason = null;
                while (current.ParentId != 0)
                {
                    if (!visited.Add(current.Id))
                    {
                        invalidReason = "hierarchy cycle detected";
                        break;
                    }

                    TwoBarContainer parent = FindContainerById(current.ParentId);
                    if (parent == null)
                    {
                        invalidReason = "missing parent id=" + current.ParentId;
                        break;
                    }
                    if (parent.Status != ContainerStatus.Active)
                    {
                        invalidReason = "non-live ancestor id=" + parent.Id;
                        break;
                    }
                    bool validBreakException = current.SameDirectionBreakException
                        && current.OriginBrokenContainerId != 0
                        && IsValidSameDirectionBreakException(current, parent);
                    bool validLatentException = IsValidLatentDirectionException(current, parent);
                    if (parent.Direction == current.Direction
                        && !validBreakException
                        && !validLatentException)
                    {
                        invalidReason = "same-direction live parent id=" + parent.Id;
                        break;
                    }
                    current = parent;
                }

                if (invalidReason != null)
                    DeactivateBranch(container, validationBar, invalidReason);
            }

            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer root = containers[i];
                if (root.ParentId != 0)
                    continue;

                if (root.Level != 1)
                {
                    if (Debug)
                        Print("[APVA V3] root level corrected id=" + root.Id
                            + " level=" + root.Level + "->1");
                    root.Level = 1;
                }
                RecalculateDescendantLevels(root);
            }

            int activeCount = 0;
            int joinedCount = 0;
            int inactiveCount = 0;
            int brokenCount = 0;
            int violationCount = 0;
            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer container = containers[i];
                if (container.Status == ContainerStatus.Active)
                    activeCount++;
                else if (container.Status == ContainerStatus.Joined)
                    joinedCount++;
                else if (container.Status == ContainerStatus.Inactive)
                    inactiveCount++;
                else if (container.Status == ContainerStatus.Broken)
                    brokenCount++;

                if (container.ParentId == 0)
                {
                    if (container.Level != 1)
                        violationCount++;
                    continue;
                }

                TwoBarContainer parent = FindContainerById(container.ParentId);
                if (parent == null || container.Level != parent.Level + 1)
                    violationCount++;
                if (container.Status == ContainerStatus.Active
                    && (parent == null
                        || parent.Status != ContainerStatus.Active
                        || (parent.Direction == container.Direction
                            && !IsValidSameDirectionBreakException(container, parent)
                            && !IsValidLatentDirectionException(container, parent))))
                    violationCount++;
            }

            if (Debug)
                Print("[APVA V3] hierarchy validation active=" + activeCount
                    + " joined=" + joinedCount
                    + " inactive=" + inactiveCount
                    + " broken=" + brokenCount
                    + " violations=" + violationCount);
        }

        private bool IsValidSameDirectionBreakException(
            TwoBarContainer response,
            TwoBarContainer parent)
        {
            if (!response.SameDirectionBreakException
                || response.OriginBrokenContainerId == 0
                || parent == null
                || response.Direction != parent.Direction)
                return false;

            TwoBarContainer broken = FindContainerById(response.OriginBrokenContainerId);
            return broken != null
                && broken.Status == ContainerStatus.Broken
                && broken.ParentId == parent.Id
                && broken.Direction != response.Direction
                && broken.BreakBar >= response.StartBar
                && broken.BreakBar <= response.EndBar;
        }

        private bool IsValidLatentDirectionException(
            TwoBarContainer response,
            TwoBarContainer parent)
        {
            return response != null
                && parent != null
                && response.LatentDirectionException
                && response.OriginLatentBar > parent.StartBar
                && response.OriginLatentBar == response.StartBar
                && response.Direction == parent.Direction;
        }

        private bool TryExtendContainer(TwoBarContainer container, int endIndex)
        {
            if (container == null)
                return false;

            BarSnapshot end = snapshots[endIndex];
            ContainerDirection direction = DirectionFromRelation(end.Relation);
            if (container.Status != ContainerStatus.Active
                || direction == ContainerDirection.Unknown
                || direction != container.Direction
                || IsOutsideRelation(end.Relation)
                || end.Index <= container.EndBar)
                return false;

            int startIndex = -1;
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].Index == container.StartBar)
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex < 0)
                return false;

            BarSnapshot start = snapshots[startIndex];
            double rtlStart = direction == ContainerDirection.Up ? start.Low : start.High;
            double endpointPrice = direction == ContainerDirection.Up ? end.Low : end.High;
            bool endpointValid = direction == ContainerDirection.Up
                ? endpointPrice > rtlStart
                : endpointPrice < rtlStart;
            if (!endpointValid)
                return false;

            double slope = (endpointPrice - rtlStart) / (end.Index - start.Index);
            int supportBar = end.Index;
            double supportPrice = endpointPrice;
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                BarSnapshot candidate = snapshots[i];
                double candidatePrice = direction == ContainerDirection.Up ? candidate.Low : candidate.High;
                double candidateSlope = (candidatePrice - rtlStart) / (candidate.Index - start.Index);
                bool isTighterSupport = direction == ContainerDirection.Up
                    ? candidateSlope < slope
                    : candidateSlope > slope;
                if (!isTighterSupport)
                    continue;

                slope = candidateSlope;
                supportBar = candidate.Index;
                supportPrice = candidatePrice;
            }

            bool directionalSlopeValid = direction == ContainerDirection.Up ? slope > 0.0 : slope < 0.0;
            if (!directionalSlopeValid)
                return false;

            container.EndBar = end.Index;
            container.RtlEndPrice = rtlStart + slope * (end.Index - start.Index);
            container.LtlEndPrice = container.LtlStartPrice
                + slope * (end.Index - container.LtlStartBar);
            container.RtlSupportBar = supportBar;
            container.RtlSupportPrice = supportPrice;
            container.Reason = "same-direction extension";
            RebuildOuterExpansion(container);

            if (Debug)
                Print("[APVA V3] container extended id=" + container.Id
                    + " direction=" + container.Direction
                    + " start=" + container.StartBar
                    + " end=" + container.EndBar
                    + " support=" + container.RtlSupportBar + "@" + FormatPrice(container.RtlSupportPrice));
            return true;
        }

        private bool TryAdjustLastContainerToRtlWick(int endIndex)
        {
            if (containers.Count == 0)
                return false;

            return TryAdjustContainerToRtlWick(FindNewestActiveContainer(), endIndex);
        }

        private bool TryAdjustContainerToRtlWick(TwoBarContainer container, int endIndex)
        {
            if (container == null)
                return false;

            BarSnapshot end = snapshots[endIndex];
            if (container.Status != ContainerStatus.Active || end.Index <= container.EndBar)
                return false;

            ContainerDirection barDirection = DirectionFromRelation(end.Relation);
            if (barDirection != ContainerDirection.Unknown && barDirection != container.Direction)
                return false;

            int span = container.EndBar - container.StartBar;
            if (span <= 0)
                return false;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            double projectedRtl = container.RtlStartPrice + slope * (end.Index - container.StartBar);
            double tolerance = TickSize * 0.5;
            bool wickPenetrated = container.Direction == ContainerDirection.Up
                ? end.Low < projectedRtl - tolerance
                : end.High > projectedRtl + tolerance;
            bool closeRemainedInside = container.Direction == ContainerDirection.Up
                ? end.Close >= projectedRtl - tolerance
                : end.Close <= projectedRtl + tolerance;
            bool insideLikeAdjustment = IsInsideLikeRelation(end.Relation);
            if (!wickPenetrated || (!closeRemainedInside && !insideLikeAdjustment))
                return false;

            double adjustedPrice = container.Direction == ContainerDirection.Up ? end.Low : end.High;
            bool geometryValid = container.Direction == ContainerDirection.Up
                ? adjustedPrice > container.RtlStartPrice
                : adjustedPrice < container.RtlStartPrice;
            if (!geometryValid)
            {
                if (Debug)
                    Print("[APVA V3] RTL adjustment rejected id=" + container.Id
                        + " bar=" + end.Index
                        + " reason=" + container.Direction + " P3 would violate strict P1/P3 geometry");
                return false;
            }

            double adjustedSlope = (adjustedPrice - container.RtlStartPrice)
                / (end.Index - container.StartBar);
            container.EndBar = end.Index;
            container.RtlEndPrice = adjustedPrice;
            container.LtlEndPrice = container.LtlStartPrice
                + adjustedSlope * (end.Index - container.LtlStartBar);
            container.RtlSupportBar = end.Index;
            container.RtlSupportPrice = adjustedPrice;
            container.Reason = "RTL wick adjustment";
            RebuildOuterExpansion(container);

            if (Debug)
                Print("[APVA V3] container adjusted id=" + container.Id
                    + " direction=" + container.Direction
                    + " start=" + container.StartBar
                    + " end=" + container.EndBar
                    + " projectedRtl=" + FormatPrice(projectedRtl)
                    + " wick=" + FormatPrice(adjustedPrice)
                    + " close=" + FormatPrice(end.Close)
                    + " reason=" + (insideLikeAdjustment
                        ? "inside-like bar refitted RTL"
                        : "close remained inside RTL"));
            return true;
        }

        private bool TryAdvanceLastContainerThroughTranslationalBar(int endIndex)
        {
            if (containers.Count == 0)
                return false;

            BarSnapshot end = snapshots[endIndex];
            if (!IsTranslational(end.Relation) || IsStitchRelation(end.Relation))
                return false;

            TwoBarContainer container = FindNewestActiveContainer();
            if (container == null)
                return false;
            if (container.Status != ContainerStatus.Active)
                return false;
            int span = container.EndBar - container.StartBar;
            if (span <= 0 || end.Index <= container.EndBar)
                return false;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            double projectedRtl = container.RtlStartPrice
                + slope * (end.Index - container.StartBar);
            double tolerance = TickSize * 0.5;
            bool remainsInsideRtl = container.Direction == ContainerDirection.Up
                ? end.Low >= projectedRtl - tolerance
                : end.High <= projectedRtl + tolerance;
            if (!remainsInsideRtl)
                return false;

            container.EndBar = end.Index;
            container.RtlEndPrice = projectedRtl;
            container.LtlEndPrice = container.LtlStartPrice
                + slope * (end.Index - container.LtlStartBar);
            container.Reason = "translational-bar extension";
            RebuildOuterExpansion(container);

            if (Debug)
                Print("[APVA V3] container advanced id=" + container.Id
                    + " direction=" + container.Direction
                    + " start=" + container.StartBar
                    + " end=" + container.EndBar
                    + " relation=" + end.Relation
                    + " reason=translational bar remained inside RTL");
            return true;
        }

        private void RebuildOuterExpansion(TwoBarContainer container)
        {
            container.FrozenOuterSegments.Clear();
            container.ActiveOuterStartBar = container.LtlStartBar;
            container.ActiveOuterStartPrice = container.LtlStartPrice;
            container.ActiveOuterIsVe = false;

            int span = container.EndBar - container.StartBar;
            if (span <= 0)
                return;

            double slope = (container.RtlEndPrice - container.RtlStartPrice) / span;
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index <= container.LtlStartBar || bar.Index > container.EndBar)
                    continue;

                double projectedOuter = container.ActiveOuterStartPrice
                    + slope * (bar.Index - container.ActiveOuterStartBar);
                double lineEpsilon = Math.Max(1e-12, TickSize * 1e-9);
                bool traversed = container.Direction == ContainerDirection.Up
                    ? bar.High > projectedOuter + lineEpsilon
                    : bar.Low < projectedOuter - lineEpsilon;
                if (!traversed)
                    continue;

                container.FrozenOuterSegments.Add(new OuterLineSegment
                {
                    StartBar = container.ActiveOuterStartBar,
                    EndBar = bar.Index,
                    StartPrice = container.ActiveOuterStartPrice,
                    EndPrice = projectedOuter,
                    IsVe = container.ActiveOuterIsVe
                });
                container.ActiveOuterStartBar = bar.Index;
                container.ActiveOuterStartPrice = container.Direction == ContainerDirection.Up
                    ? bar.High
                    : bar.Low;
                container.ActiveOuterIsVe = true;
            }
        }

        private void LogFinalContainers()
        {
            if (!Debug)
                return;

            for (int i = 0; i < containers.Count; i++)
            {
                TwoBarContainer container = containers[i];
                container.Id = i + 1;
                Print("[APVA V3] container created id=" + container.Id
                    + " direction=" + container.Direction
                    + " level=" + container.Level
                    + " parent=" + (container.ParentId == 0 ? "none" : container.ParentId.ToString(CultureInfo.InvariantCulture))
                    + " lineage=" + container.LineageId
                    + (container.OriginFttContainerId == 0
                        ? ""
                        : " originFtt=" + container.OriginFttContainerId
                            + "@" + container.OriginFttBar
                            + ":" + FormatPrice(container.OriginFttPrice))
                    + (container.SameDirectionBreakException
                        ? " breakResponseTo=" + container.OriginBrokenContainerId
                        : "")
                    + (container.LatentDirectionException
                        ? " latentResponseTo=" + container.OriginLatentBar
                        : "")
                    + (container.IsJoinedParent
                        ? " joinedChildren=" + container.JoinLeftId + "," + container.JoinMiddleId + "," + container.JoinRightId
                        : "")
                    + " start=" + container.StartBar
                    + " end=" + container.EndBar
                    + " status=" + container.Status
                    + (container.BreakBar >= 0 ? " breakBar=" + container.BreakBar : "")
                    + " rtl=" + FormatPrice(container.RtlStartPrice) + "->" + FormatPrice(container.RtlEndPrice)
                    + " ltl=" + container.LtlStartBar + "@" + FormatPrice(container.LtlStartPrice)
                    + "->" + FormatPrice(container.LtlEndPrice)
                    + " support=" + container.RtlSupportBar + "@" + FormatPrice(container.RtlSupportPrice)
                    + " veCount=" + container.FrozenOuterSegments.Count
                    + " style=" + StyleNameForLevel(container.Level)
                    + " width=" + WidthForLevel(container.Level).ToString("0.##", CultureInfo.InvariantCulture)
                    + " opacity=" + OpacityForStatus(container.Status)
                    + " reason=" + container.Reason);
                for (int segmentIndex = 0; segmentIndex < container.FrozenOuterSegments.Count; segmentIndex++)
                {
                    OuterLineSegment frozen = container.FrozenOuterSegments[segmentIndex];
                    double veAnchorPrice = segmentIndex + 1 < container.FrozenOuterSegments.Count
                        ? container.FrozenOuterSegments[segmentIndex + 1].StartPrice
                        : container.ActiveOuterStartPrice;
                    Print("[APVA V3] VE created container=" + container.Id
                        + " sequence=" + (segmentIndex + 1)
                        + " bar=" + frozen.EndBar
                        + " anchor=" + FormatPrice(veAnchorPrice)
                        + " frozen=" + (frozen.IsVe ? "VE" : "LTL")
                        + " reason=outer line traversed");
                }
            }
        }

        private void LogConstructionRejected(int bar, int startBar, string reason)
        {
            if (Debug)
                Print("[APVA V3] container rejected bar=" + bar + " start=" + startBar + " reason=" + reason);
        }

        private ContainerDirection DirectionFromRelation(xPvaBarRelation relation)
        {
            if (relation == xPvaBarRelation.HHHL
                || relation == xPvaBarRelation.HighReversal
                || relation == xPvaBarRelation.OutsideBullish
                || relation == xPvaBarRelation.StitchLong)
                return ContainerDirection.Up;
            if (relation == xPvaBarRelation.LLLH
                || relation == xPvaBarRelation.LowReversal
                || relation == xPvaBarRelation.OutsideBearish
                || relation == xPvaBarRelation.StitchShort)
                return ContainerDirection.Down;
            return ContainerDirection.Unknown;
        }

        private bool IsOutsideRelation(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.OutsideBar
                || relation == xPvaBarRelation.OutsideBullish
                || relation == xPvaBarRelation.OutsideBearish;
        }

        private bool IsTranslational(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.InsideBar
                || relation == xPvaBarRelation.FTP
                || relation == xPvaBarRelation.FBP
                || relation == xPvaBarRelation.SameHighSameLow
                || relation == xPvaBarRelation.StitchLong
                || relation == xPvaBarRelation.StitchShort;
        }

        private bool IsInsideLikeRelation(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.InsideBar
                || relation == xPvaBarRelation.FTP
                || relation == xPvaBarRelation.FBP;
        }

        private bool IsSyntheticLateralBar(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.InsideBar
                || relation == xPvaBarRelation.SameHighSameLow;
        }

        private bool IsStitchRelation(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.StitchLong
                || relation == xPvaBarRelation.StitchShort;
        }

        private BarSnapshot CreateSnapshot(int index)
        {
            xPvaBarFacts current = Facts(index);
            xPvaBarRelation relation = xPvaBarRelation.Unknown;
            if (index > 0)
                relation = eventEngine.ClassifyRelation(current, Facts(index - 1));

            return new BarSnapshot
            {
                Index = index,
                Time = current.Time,
                Open = current.Open,
                High = current.High,
                Low = current.Low,
                Close = current.Close,
                Volume = current.Volume,
                Relation = relation
            };
        }

        private BarSnapshot FindSnapshotByBar(int barIndex)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].Index == barIndex)
                    return snapshots[i];
            }
            return null;
        }

        private void FindFttExtreme(
            TwoBarContainer opposite,
            ContainerDirection responseDirection,
            out int fttBar,
            out double fttPrice)
        {
            fttBar = 0;
            fttPrice = 0.0;
            bool found = false;
            for (int i = 0; i < snapshots.Count; i++)
            {
                BarSnapshot bar = snapshots[i];
                if (bar.Index < opposite.StartBar || bar.Index > opposite.EndBar)
                    continue;

                double candidate = responseDirection == ContainerDirection.Up ? bar.Low : bar.High;
                bool moreExtreme = !found
                    || (responseDirection == ContainerDirection.Up && candidate <= fttPrice)
                    || (responseDirection == ContainerDirection.Down && candidate >= fttPrice);
                if (!moreExtreme)
                    continue;

                found = true;
                fttBar = bar.Index;
                fttPrice = candidate;
            }
        }

        private xPvaBarFacts Facts(int index)
        {
            return new xPvaBarFacts(
                index,
                Time.GetValueAt(index),
                Open.GetValueAt(index),
                High.GetValueAt(index),
                Low.GetValueAt(index),
                Close.GetValueAt(index),
                Volume.GetValueAt(index),
                TickSize);
        }

        private string FormatSnapshot(BarSnapshot snapshot)
        {
            return "[APVA V3] bar=" + snapshot.Index.ToString(CultureInfo.InvariantCulture)
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

        private SolidColorBrush BrushForContainer(TwoBarContainer container)
        {
            if (container.Status == ContainerStatus.Inactive)
                return container.Direction == ContainerDirection.Up ? upInactiveBrushDx : downInactiveBrushDx;
            if (container.Status == ContainerStatus.Broken || container.Status == ContainerStatus.Joined)
                return container.Direction == ContainerDirection.Up ? upBrokenBrushDx : downBrokenBrushDx;
            return container.Direction == ContainerDirection.Up ? upBrushDx : downBrushDx;
        }

        private StrokeStyle StrokeForLevel(int level)
        {
            if (level <= 1)
                return null;
            if (level == 2)
                return dashStrokeStyle;
            if (level == 3)
                return dotStrokeStyle;
            return deepLevelStrokeStyle ?? dotStrokeStyle;
        }

        private float WidthForLevel(int level)
        {
            if (level <= 1)
                return 2.0f;
            if (level == 2)
                return 1.8f;
            if (level == 3)
                return 1.7f;
            return 1.35f;
        }

        private string StyleNameForLevel(int level)
        {
            if (level <= 1)
                return "Solid";
            if (level == 2)
                return "Dash";
            if (level == 3)
                return "Dot";
            return "DashDotDot";
        }

        private int OpacityForStatus(ContainerStatus status)
        {
            if (status == ContainerStatus.Inactive)
                return 85;
            if (status == ContainerStatus.Broken || status == ContainerStatus.Joined)
                return 150;
            return 255;
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
            if (upBrokenBrushDx != null)
            {
                upBrokenBrushDx.Dispose();
                upBrokenBrushDx = null;
            }
            if (downBrokenBrushDx != null)
            {
                downBrokenBrushDx.Dispose();
                downBrokenBrushDx = null;
            }
            if (upInactiveBrushDx != null)
            {
                upInactiveBrushDx.Dispose();
                upInactiveBrushDx = null;
            }
            if (downInactiveBrushDx != null)
            {
                downInactiveBrushDx.Dispose();
                downInactiveBrushDx = null;
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
            if (deepLevelStrokeStyle != null)
            {
                deepLevelStrokeStyle.Dispose();
                deepLevelStrokeStyle = null;
            }
            if (lateralFillBrushDx != null)
            {
                lateralFillBrushDx.Dispose();
                lateralFillBrushDx = null;
            }
            if (lateralOutlineBrushDx != null)
            {
                lateralOutlineBrushDx.Dispose();
                lateralOutlineBrushDx = null;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaAutomatedContainerV3[] cachexPvaAutomatedContainerV3;
		public xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(bool debug, int startBar, int endBar)
		{
			return xPvaAutomatedContainerV3(Input, debug, startBar, endBar);
		}

		public xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(ISeries<double> input, bool debug, int startBar, int endBar)
		{
			if (cachexPvaAutomatedContainerV3 != null)
				for (int idx = 0; idx < cachexPvaAutomatedContainerV3.Length; idx++)
					if (cachexPvaAutomatedContainerV3[idx] != null && cachexPvaAutomatedContainerV3[idx].Debug == debug && cachexPvaAutomatedContainerV3[idx].StartBar == startBar && cachexPvaAutomatedContainerV3[idx].EndBar == endBar && cachexPvaAutomatedContainerV3[idx].EqualsInput(input))
						return cachexPvaAutomatedContainerV3[idx];
			return CacheIndicator<xPvaAutomatedContainerV3>(new xPvaAutomatedContainerV3(){ Debug = debug, StartBar = startBar, EndBar = endBar }, input, ref cachexPvaAutomatedContainerV3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainerV3(Input, debug, startBar, endBar);
		}

		public Indicators.xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(ISeries<double> input , bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainerV3(input, debug, startBar, endBar);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainerV3(Input, debug, startBar, endBar);
		}

		public Indicators.xPvaAutomatedContainerV3 xPvaAutomatedContainerV3(ISeries<double> input , bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainerV3(input, debug, startBar, endBar);
		}
	}
}

#endregion
