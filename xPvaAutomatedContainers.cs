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
    public class xPvaAutomatedContainers : Indicator
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
            Broken,
            Joined,
            Adjusted
        }

        private enum RenderLineKind
        {
            Rtl,
            Ltl,
            Ve
        }

        private enum StructuralRole
        {
            Unknown,
            Origin,
            Component
        }

        private sealed class AnalyzedBar
        {
            public int Bar;
            public DateTime Time;
            public double Open;
            public double High;
            public double Low;
            public double Close;
            public double Volume;
            public xPvaBarRelation Relation;
            public bool IsTransitional;
            public bool IsTranslational;
            public bool IsReversal;
            public bool IsSimplePeak;
            public bool IsAcceleratedPeak;
            public bool IsPeakEligible;
            public double Range;
            public double Body;
            public double BodyToRange;
            public bool IsCompressedBody;
            public bool IsDominantCandidate;
            public bool IsDominant;
            public string DominanceReason;
        }

        private sealed class PriceLine
        {
            public int StartBar;
            public double StartPrice;
            public double Slope;

            public double ValueAt(int bar)
            {
                return StartPrice + Slope * (bar - StartBar);
            }
        }

        private sealed class RenderSegment
        {
            public int ContainerId;
            public int Level;
            public ContainerDirection Direction;
            public RenderLineKind Kind;
            public int StartBar;
            public int EndBar;
            public double StartPrice;
            public double EndPrice;
        }

        private sealed class VolumePoint
        {
            public int Bar;
            public double Volume;
            public bool SimplePeak;
            public bool AcceleratedPeak;
            public bool PeakEligible;
        }

        private sealed class PriceContainer
        {
            public int Id;
            public int ParentId;
            public readonly List<int> ChildIds = new List<int>();
            public int Level;
            public ContainerDirection Direction;
            public ContainerStatus Status;
            public int StartBar;
            public int EndBar;
            public int P1Bar;
            public double P1Price;
            public int P2Bar;
            public double P2Price;
            public int P3Bar;
            public double P3Price;
            public PriceLine Rtl;
            public PriceLine Ltl;
            public PriceLine ActiveVe;
            public readonly List<RenderSegment> FrozenSegments = new List<RenderSegment>();
            public readonly List<VolumePoint> VolumePoints = new List<VolumePoint>();
            public string Reason;
            public int LineageId;
            public int StructuralParentId;
            public int OriginFttContainerId;
            public int OriginFttBar;
            public double OriginFttPrice;
            public int StructuralLineageJoinId;
            public string StructuralLineageContextRole;
            public int StructuralLineageContextSourceId;
            public int InheritedParentStructuralLineageJoinId;
            public int InheritedParentStructuralSourceId;
            public string InheritedParentStructuralRole;
            public int FttCandidateBar;
            public double FttCandidatePrice;
            public bool FttConfirmed;
            public StructuralRole Role;
        }

        private sealed class StructuralLineageJoin
        {
            public int Id;
            public int LineageId;
            public int LeftId;
            public int MiddleId;
            public int RightId;
            public readonly List<int> SupersededRightIds = new List<int>();
            public int OriginFttContainerId;
            public int OriginFttBar;
            public double OriginFttPrice;
            public ContainerDirection Direction;
            public int StartBar;
            public int EndBar;
            public int P1Bar;
            public double P1Price;
            public int P2Bar;
            public double P2Price;
            public int P3Bar;
            public double P3Price;
            public bool IsActiveContext;
            public int ContextActivatedBar;
            public int ContextContainerId;
            public int ContextBrokenParentId;
        }

        private sealed class PendingTape
        {
            public int StartBar = -1;
            public readonly List<int> SkippedBars = new List<int>();
        }

        private sealed class LateralFormation
        {
            public int StartBar = -1;
            public double Upper;
            public double Lower;
            public int BarsInside;
            public bool IsValid;
        }

        private sealed class ContainerExportEvent
        {
            public int EventId;
            public int Bar;
            public string EventType;
            public int ContainerId;
            public readonly List<int> RelatedContainerIds = new List<int>();
            public int Level;
            public string Reason;
            public string Details;
        }

        private sealed class ExportWarning
        {
            public int WarningId;
            public int Bar;
            public int ContainerId;
            public string WarningType;
            public string Severity;
            public string Message;
        }

        private xPvaDiscreteEventEngine eventEngine;
        private readonly List<AnalyzedBar> analyzedBars = new List<AnalyzedBar>();
        private readonly List<PriceContainer> containers = new List<PriceContainer>();
        private readonly List<StructuralLineageJoin> structuralLineageJoins = new List<StructuralLineageJoin>();
        private readonly List<RenderSegment> renderSegments = new List<RenderSegment>();
        private readonly List<string> debugEvents = new List<string>();
        private readonly List<ContainerExportEvent> exportEvents = new List<ContainerExportEvent>();
        private readonly List<PriceContainer> containersFrozenThisBar = new List<PriceContainer>();
        private PendingTape pendingTape;
        private LateralFormation lateral;
        private int nextContainerId;
        private int nextLineageId;
        private int nextStructuralLineageJoinId;
        private int nextExportEventId;
        private int resolvedLateralBar;
        private int resolvedLateralOrigin;
        private int lastBuiltStart = -1;
        private int lastBuiltEnd = -1;
        private bool lastDebugMode;
        private bool lastEnableLineageJoins;
        private string lastJsonExportSignature;
        private SharpDX.Direct2D1.Brush upBrushDx;
        private SharpDX.Direct2D1.Brush downBrushDx;
        private StrokeStyle dashStrokeStyle;
        private StrokeStyle dotStrokeStyle;
        private StrokeStyle deepLevelStrokeStyle;

        [NinjaScriptProperty]
        [Display(Name = "Debug", GroupName = "Debug", Order = 1)]
        public bool Debug { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start Bar", GroupName = "Debug", Order = 2)]
        public int StartBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End Bar", GroupName = "Debug", Order = 3)]
        public int EndBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Lineage Joins", GroupName = "Debug", Order = 4)]
        public bool EnableLineageJoins { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Export JSON", GroupName = "Export", Order = 1)]
        public bool ExportJson { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "JSON Export Folder", GroupName = "Export", Order = 2)]
        public string JsonExportFolder { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "JSON File Name", GroupName = "Export", Order = 3)]
        public string JsonFileName { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Export On Every Bar", GroupName = "Export", Order = 4)]
        public bool ExportOnEveryBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Debug Export", GroupName = "Export", Order = 5)]
        public bool DebugExport { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaAutomatedContainers";
                Description = "Automated PVA price container construction rendered with NT8 SharpDX.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                PrintTo = PrintTo.OutputTab2;
                Debug = false;
                EnableLineageJoins = false;
                StartBar = 0;
                EndBar = 0;
                ExportJson = false;
                JsonExportFolder = @"C:\Users\rz0\Documents\ApvaAnalysis\ContainerJSON";
                JsonFileName = string.Empty;
                ExportOnEveryBar = false;
                DebugExport = false;
            }
            else if (State == State.DataLoaded)
            {
                eventEngine = new xPvaDiscreteEventEngine(TickSize);
                ResetModel();
            }
            else if (State == State.Terminated)
            {
                DisposeRenderResources();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 || eventEngine == null)
                return;

            int effectiveStart;
            int effectiveEnd;
            if (!TryGetReplayBounds(out effectiveStart, out effectiveEnd))
                return;

            if (effectiveEnd > CurrentBar)
                return;

            if (effectiveStart == lastBuiltStart && effectiveEnd == lastBuiltEnd && Debug == lastDebugMode && EnableLineageJoins == lastEnableLineageJoins)
                return;

            BuildModel(effectiveStart, effectiveEnd);
            lastBuiltStart = effectiveStart;
            lastBuiltEnd = effectiveEnd;
            lastDebugMode = Debug;
            lastEnableLineageJoins = EnableLineageJoins;
            ForceRefresh();
        }

        public override void OnRenderTargetChanged()
        {
            DisposeRenderResources();

            if (RenderTarget == null)
                return;

            upBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 0, 255, 255));
            downBrushDx = new SolidColorBrush(RenderTarget, SharpDX.Color.Red);
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
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (chartControl == null || chartScale == null || ChartBars == null || RenderTarget == null)
                return;

            if (upBrushDx == null || downBrushDx == null)
                OnRenderTargetChanged();

            if (upBrushDx == null || downBrushDx == null)
                return;

            AntialiasMode oldMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            foreach (RenderSegment segment in renderSegments)
            {
                if (segment.EndBar < ChartBars.FromIndex || segment.StartBar > ChartBars.ToIndex)
                    continue;

                int startBar = Math.Max(segment.StartBar, ChartBars.FromIndex);
                int endBar = Math.Min(segment.EndBar, ChartBars.ToIndex);
                if (endBar < startBar)
                    continue;

                double slope = (segment.EndBar == segment.StartBar)
                    ? 0.0
                    : (segment.EndPrice - segment.StartPrice) / (segment.EndBar - segment.StartBar);

                double startPrice = segment.StartPrice + slope * (startBar - segment.StartBar);
                double endPrice = segment.StartPrice + slope * (endBar - segment.StartBar);

                float x1 = chartControl.GetXByBarIndex(ChartBars, startBar);
                float x2 = chartControl.GetXByBarIndex(ChartBars, endBar);
                float y1 = chartScale.GetYByValue(startPrice);
                float y2 = chartScale.GetYByValue(endPrice);

                SharpDX.Direct2D1.Brush brush = segment.Direction == ContainerDirection.Up ? upBrushDx : downBrushDx;
                StrokeStyle style = StrokeForLevel(segment.Level);
                float width = WidthForLevel(segment.Level);

                RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), brush, width, style);
            }

            RenderTarget.AntialiasMode = oldMode;
        }

        private void BuildModel(int start, int end)
        {
            ResetModel();
            if (start < 0 || end < start || end > CurrentBar)
                return;

            for (int bar = start; bar <= end; bar++)
            {
                AnalyzedBar analyzed = AnalyzeBar(bar);
                analyzedBars.Add(analyzed);

                if (bar == start)
                {
                    SeedReplayStart(analyzed);
                    continue;
                }

                UpdateLateral(analyzed);
                UpdateContainers(analyzed);
            }

            RebuildRenderSegments();
            ValidateHierarchy();
            MaybeExportJson(start, end);

            if (Debug)
            {
                foreach (string debugEvent in debugEvents)
                    Print(debugEvent);
            }
        }

        private void ResetModel()
        {
            analyzedBars.Clear();
            containers.Clear();
            structuralLineageJoins.Clear();
            renderSegments.Clear();
            debugEvents.Clear();
            exportEvents.Clear();
            pendingTape = new PendingTape();
            lateral = new LateralFormation();
            nextContainerId = 1;
            nextLineageId = 1;
            nextStructuralLineageJoinId = 1;
            nextExportEventId = 1;
            resolvedLateralBar = -1;
            resolvedLateralOrigin = -1;
        }

        private void SeedReplayStart(AnalyzedBar bar)
        {
            pendingTape.StartBar = bar.Bar;
            lateral.StartBar = bar.Bar;
            lateral.Upper = bar.High;
            lateral.Lower = bar.Low;
            lateral.BarsInside = 1;
            lateral.IsValid = true;
            LogDebug(bar.Bar, "replay seeded from Start Bar; prior bars are not used as container origin");
        }

        private bool TryGetReplayBounds(out int effectiveStart, out int effectiveEnd)
        {
            effectiveStart = 0;
            effectiveEnd = CurrentBar;

            if (!Debug)
                return true;

            if (StartBar < 0 || StartBar > CurrentBar)
            {
                LogDebug(CurrentBar, "bounds rejected: Start Bar is outside loaded data");
                return false;
            }

            effectiveStart = StartBar;
            effectiveEnd = EndBar <= 0 ? CurrentBar : EndBar;

            if (effectiveEnd < effectiveStart || effectiveEnd > CurrentBar)
            {
                LogDebug(CurrentBar, "bounds rejected: End Bar must be >= Start Bar and <= CurrentBar");
                return false;
            }

            return true;
        }

        private AnalyzedBar AnalyzeBar(int bar)
        {
            xPvaBarFacts cur = Facts(bar);
            xPvaBarFacts prev = bar > 0 ? Facts(bar - 1) : null;
            xPvaBarFacts prev2 = bar > 1 ? Facts(bar - 2) : null;

            xPvaBarRelation relation = prev == null ? xPvaBarRelation.Unknown : eventEngine.ClassifyRelation(cur, prev);
            bool isReversal = IsBodyReversal(cur, relation);
            bool simplePeak = prev != null && cur.Volume > prev.Volume;
            bool acceleratedPeak = prev != null && prev2 != null && cur.Volume > prev.Volume && prev.Volume > prev2.Volume;
            bool transitional = IsTransitional(relation);
            bool translational = IsTranslational(relation);
            double range = Math.Max(0.0, cur.High - cur.Low);
            double body = Math.Abs(cur.Close - cur.Open);
            double bodyRatio = range > 0.0 ? body / range : 0.0;
            bool compressedBody = bodyRatio <= 0.25;
            bool dominantCandidate = transitional && !isReversal && simplePeak;
            string dominanceReason;
            bool dominant = IsDominantBar(cur, prev, relation, isReversal, compressedBody, out dominanceReason);

            return new AnalyzedBar
            {
                Bar = bar,
                Time = cur.Time,
                Open = cur.Open,
                High = cur.High,
                Low = cur.Low,
                Close = cur.Close,
                Volume = cur.Volume,
                Relation = relation,
                IsTransitional = transitional,
                IsTranslational = translational,
                IsReversal = isReversal,
                IsSimplePeak = simplePeak,
                IsAcceleratedPeak = acceleratedPeak,
                IsPeakEligible = transitional && !isReversal && simplePeak,
                Range = range,
                Body = body,
                BodyToRange = bodyRatio,
                IsCompressedBody = compressedBody,
                IsDominantCandidate = dominantCandidate,
                IsDominant = dominant,
                DominanceReason = dominanceReason
            };
        }

        private xPvaBarFacts Facts(int absoluteBar)
        {
            return new xPvaBarFacts(
                absoluteBar,
                Time.GetValueAt(absoluteBar),
                Open.GetValueAt(absoluteBar),
                High.GetValueAt(absoluteBar),
                Low.GetValueAt(absoluteBar),
                Close.GetValueAt(absoluteBar),
                Volume.GetValueAt(absoluteBar),
                TickSize);
        }

        private void UpdateLateral(AnalyzedBar bar)
        {
            if (bar.Bar <= 0)
            {
                lateral.StartBar = bar.Bar;
                lateral.Upper = bar.High;
                lateral.Lower = bar.Low;
                lateral.BarsInside = 1;
                lateral.IsValid = true;
                return;
            }

            if (!lateral.IsValid || lateral.StartBar < 0)
            {
                lateral.StartBar = bar.Bar - 1;
                lateral.Upper = High.GetValueAt(lateral.StartBar);
                lateral.Lower = Low.GetValueAt(lateral.StartBar);
                lateral.BarsInside = 1;
                lateral.IsValid = true;
            }

            bool fullyInside = bar.High <= lateral.Upper + TickSize * 0.5 && bar.Low >= lateral.Lower - TickSize * 0.5;
            bool closeInside = bar.Close <= lateral.Upper + TickSize * 0.5 && bar.Close >= lateral.Lower - TickSize * 0.5;
            bool bodyOutside = bar.High < lateral.Lower - TickSize * 0.5 || bar.Low > lateral.Upper + TickSize * 0.5;

            if (lateral.BarsInside < 3)
            {
                if (fullyInside)
                {
                    lateral.BarsInside++;
                    LogDebug(bar.Bar, "lateral building: bar remains inside origin boundary " + lateral.StartBar);
                    return;
                }

                lateral.StartBar = bar.Bar;
                lateral.Upper = bar.High;
                lateral.Lower = bar.Low;
                lateral.BarsInside = 1;
                lateral.IsValid = true;
                return;
            }

            if (bodyOutside)
            {
                LogDebug(bar.Bar, "lateral invalidated: full bar range outside origin boundary " + lateral.StartBar);
                lateral.IsValid = false;
                return;
            }

            if (closeInside)
            {
                lateral.BarsInside++;
                LogDebug(bar.Bar, "lateral compressed as synthetic one-bar context from " + lateral.StartBar);
            }
        }

        private void UpdateContainers(AnalyzedBar bar)
        {
            containersFrozenThisBar.Clear();
            UpdateExistingContainers(bar);

            if (bar.Bar == 0)
            {
                pendingTape.StartBar = bar.Bar;
                return;
            }

            if (pendingTape.StartBar < 0)
                pendingTape.StartBar = bar.Bar - 1;

            if (TryPromoteChildAfterParentBreak(bar))
            {
                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            if (bar.IsTranslational)
            {
                pendingTape.SkippedBars.Add(bar.Bar);
                LogDebug(bar.Bar, "skipped translational bar " + bar.Relation + "; pending start remains " + pendingTape.StartBar);
                return;
            }

            if (!bar.IsTransitional)
                return;

            ContainerDirection direction = DirectionFromRelation(bar.Relation);
            if (direction == ContainerDirection.Unknown)
                return;

            direction = ResolvePendingConstructionDirection(bar, direction);

            LogV2ShadowDecision(bar, direction);

            int resolvedOrigin = ResolveOriginBar(bar);
            PriceContainer frozenChild = FindFrozenOppositeChildForResponse(direction, bar.Bar, resolvedOrigin);
            if (frozenChild != null)
            {
                PriceContainer parent = FindContainer(frozenChild.ParentId);
                int responseOrigin = Math.Max(resolvedOrigin, frozenChild.StartBar);
                if (parent != null && responseOrigin < bar.Bar)
                {
                    PriceContainer response = CreateContainer(responseOrigin, bar.Bar, direction, frozenChild.Level, "same-direction response after child break " + frozenChild.Id + " from " + bar.Relation);
                    if (response != null)
                    {
                        response.ParentId = parent.Id;
                        if (!parent.ChildIds.Contains(response.Id))
                            parent.ChildIds.Add(response.Id);
                        AssignStructuralLineageFromSource(response, frozenChild, parent, bar, "same-direction response after child break");
                        AssignStructuralLineageContextInheritance(response, bar, "same-direction response after child break");
                        AssignInheritedParentStructuralContext(response, parent, bar, "create same-direction child-break response");
                        LogActiveStructuralLineageContext(response, bar, "create child-break response");
                        LogDebug(bar.Bar, "same-direction child response created: id=" + response.Id + " parent=" + parent.Id + " level=" + response.Level + " after broken child=" + frozenChild.Id);
                        TryJoinTriads(response);
                    }

                    pendingTape = new PendingTape { StartBar = bar.Bar };
                    return;
                }
            }

            if (ShouldSuppressImmediateChildAfterResponseBreak(bar, direction))
            {
                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            if (TryCreateV2ChildContainer(bar, direction))
            {
                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            if (TryContinueActiveSameDirectionContainer(bar, direction))
            {
                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            if (IsOutsideRelation(bar.Relation) && TryAbsorbContextualOutsideBar(bar, direction))
            {
                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            PriceContainer broken = FindBrokenContainer(bar, direction);

            PriceContainer containing = broken == null ? FindContainingActiveContainer(bar) : null;
            if (containing != null)
            {
                if (containing.Direction == direction)
                {
                    LogDebug(bar.Bar, "container creation suppressed: same-direction transitional bar remains inside active container id=" + containing.Id);
                    pendingTape = new PendingTape { StartBar = bar.Bar };
                    return;
                }

                int childOrigin = Math.Max(ResolveOriginBar(bar), containing.StartBar);
                if (childOrigin >= bar.Bar)
                {
                    pendingTape = new PendingTape { StartBar = bar.Bar };
                    return;
                }

                PriceContainer child = CreateContainer(childOrigin, bar.Bar, direction, containing.Level + 1, "opposite child inside active container " + containing.Id + " from " + bar.Relation);
                if (child != null)
                {
                    child.ParentId = containing.Id;
                    if (!containing.ChildIds.Contains(child.Id))
                        containing.ChildIds.Add(child.Id);
                    AssignStructuralLineage(child, containing, bar, "opposite child inside active container");
                    AssignStructuralLineageContextInheritance(child, bar, "opposite child inside active container");
                    AssignInheritedParentStructuralContext(child, containing, bar, "create opposite child");
                    LogActiveStructuralLineageContext(child, bar, "create opposite child");
                    LogDebug(bar.Bar, "opposite child container created: id=" + child.Id + " parent=" + containing.Id + " level=" + child.Level);
                    TryJoinTriads(child);
                }

                pendingTape = new PendingTape { StartBar = bar.Bar };
                return;
            }

            int level = broken != null ? broken.Level : 1;
            int origin = ResolveOriginBar(bar);

            if (origin >= bar.Bar)
                return;

            if (broken != null)
            {
                broken.Status = ContainerStatus.Broken;
                ConfirmFttCandidate(broken, bar, "container break");
                LogDebug(bar.Bar, "container break: id=" + broken.Id + " level=" + broken.Level + " by " + bar.Relation);
            }

            PriceContainer created = CreateContainer(origin, bar.Bar, direction, level, "created from " + bar.Relation);
            InheritBrokenChildParentScope(created, broken, bar);
            AttachContainedChildren(created);
            PriceContainer rolloverParent = TryJoinFrozenParentTerminalTriad(created, bar);
            TryJoinTriads(rolloverParent ?? created);

            pendingTape = new PendingTape { StartBar = bar.Bar };
        }

        private ContainerDirection ResolvePendingConstructionDirection(AnalyzedBar bar, ContainerDirection fallback)
        {
            if (bar == null || pendingTape.StartBar < 0 || pendingTape.StartBar >= bar.Bar - 1)
                return fallback;

            AnalyzedBar start = GetAnalyzed(pendingTape.StartBar);
            if (start == null)
                return fallback;

            bool upValid = IsValidTwoPointConstruction(start, bar, ContainerDirection.Up);
            bool downValid = IsValidTwoPointConstruction(start, bar, ContainerDirection.Down);
            if (upValid == downValid)
                return fallback;

            ContainerDirection resolved = upValid ? ContainerDirection.Up : ContainerDirection.Down;
            if (resolved != fallback)
            {
                LogDebug(bar.Bar,
                    "pending construction direction resolved from origin " + pendingTape.StartBar
                    + ": local=" + fallback
                    + " aggregate=" + resolved);
            }
            return resolved;
        }

        private PriceContainer TryJoinFrozenParentTerminalTriad(PriceContainer created, AnalyzedBar bar)
        {
            if (!EnableLineageJoins || created == null || created.ParentId != 0)
                return null;

            PriceContainer bestLeft = null;
            PriceContainer bestMiddle = null;
            PriceContainer bestBrokenParent = null;
            string bestReject = "no terminal joined pair";

            foreach (PriceContainer frozenParent in containersFrozenThisBar)
            {
                if (frozenParent == null || frozenParent.Status != ContainerStatus.Broken)
                    continue;
                if (frozenParent.ParentId != created.ParentId || frozenParent.Direction == created.Direction)
                    continue;
                if (string.IsNullOrEmpty(frozenParent.Reason)
                    || !frozenParent.Reason.StartsWith("joined triad ", StringComparison.Ordinal))
                    continue;

                var joinedChildren = new List<PriceContainer>();
                foreach (int childId in frozenParent.ChildIds)
                {
                    PriceContainer child = FindContainer(childId);
                    if (child == null || child.ParentId != frozenParent.Id || child.Status == ContainerStatus.Broken)
                        continue;
                    joinedChildren.Add(child);
                }

                joinedChildren.Sort((a, b) =>
                {
                    int startCompare = a.StartBar.CompareTo(b.StartBar);
                    return startCompare != 0 ? startCompare : a.Id.CompareTo(b.Id);
                });

                for (int i = 0; i < joinedChildren.Count - 1; i++)
                {
                    PriceContainer left = joinedChildren[i];
                    PriceContainer middle = joinedChildren[i + 1];
                    if (left.Direction != created.Direction || middle.Direction == created.Direction)
                        continue;
                    if (left.Status != ContainerStatus.Joined)
                        continue;
                    if (middle.Status != ContainerStatus.Joined
                        && middle.Status != ContainerStatus.Active
                        && middle.Status != ContainerStatus.Adjusted)
                        continue;
                    if (!(left.StartBar < middle.StartBar && middle.StartBar < created.StartBar))
                        continue;

                    string rejectReason;
                    if (!CanJoinFrozenParentTerminalContainers(left, middle, created, out rejectReason))
                    {
                        bestReject = rejectReason;
                        continue;
                    }

                    if (bestLeft == null || middle.StartBar > bestMiddle.StartBar)
                    {
                        bestLeft = left;
                        bestMiddle = middle;
                        bestBrokenParent = frozenParent;
                    }
                }
            }

            if (bestLeft == null || bestMiddle == null)
            {
                if (containersFrozenThisBar.Count > 0)
                    LogDebug(bar.Bar, "frozen-parent terminal join not applied: new=" + created.Id + " reason=" + bestReject);
                return null;
            }

            int parentLevel = Math.Min(created.Level, Math.Min(bestLeft.Level, bestMiddle.Level));
            PriceContainer joined = CreateJoinedParent(bestLeft, bestMiddle, created, false, parentLevel, created.ParentId);
            if (joined != null)
            {
                LogDebug(bar.Bar,
                    "frozen-parent terminal join accepted: parent=" + joined.Id
                    + " children=" + bestLeft.Id + "," + bestMiddle.Id + "," + created.Id
                    + " brokenParent=" + bestBrokenParent.Id);
            }

            return joined;
        }

        private bool CanJoinFrozenParentTerminalContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            if (!IsAlternatingTriad(left, middle, right))
            {
                rejectReason = "not alternating";
                return false;
            }
            if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
            {
                rejectReason = "not start ordered";
                return false;
            }

            string p1Reject;
            if (!HasJoinP1Geometry(left, middle, right, out p1Reject))
            {
                rejectReason = p1Reject;
                return false;
            }
            if (!HasMiddleFailureToTraverse(left, middle))
            {
                rejectReason = "middle FTT failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            if (!IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price))
            {
                rejectReason = "joined P1/P3 geometry failed";
                return false;
            }

            return true;
        }

        private bool TryPromoteChildAfterParentBreak(AnalyzedBar bar)
        {
            PriceContainer selectedParent = null;
            PriceContainer selectedChild = null;

            foreach (PriceContainer frozen in containersFrozenThisBar)
            {
                if (frozen.Direction == ContainerDirection.Unknown)
                    continue;

                ContainerDirection direction = OppositeDirection(frozen.Direction);
                PriceContainer child = FindPromotableChild(frozen, direction);
                if (child == null)
                    continue;

                if (selectedChild == null
                    || child.Level > selectedChild.Level
                    || (child.Level == selectedChild.Level && child.StartBar > selectedChild.StartBar))
                {
                    selectedParent = frozen;
                    selectedChild = child;
                }
            }

            if (selectedParent == null || selectedChild == null)
                return false;

            PromoteChildFromBrokenParent(selectedParent, selectedChild, bar);
            LogDebug(bar.Bar, "child promoted after parent break: child=" + selectedChild.Id + " inheritedParent=" + selectedChild.ParentId + " level=" + selectedChild.Level + " brokenParent=" + selectedParent.Id);
            return true;
        }

        private void PromoteChildFromBrokenParent(PriceContainer selectedParent, PriceContainer selectedChild, AnalyzedBar bar)
        {
            int inheritedParentId = selectedParent.ParentId;
            DetachFromExistingParent(selectedChild);
            DeactivateBrokenParentDescendants(selectedParent, selectedChild, bar);
            selectedChild.ParentId = inheritedParentId;
            selectedChild.Level = selectedParent.Level;
            DemoteRecursively(selectedChild, selectedChild.Level);

            PriceContainer inheritedParent = FindContainer(inheritedParentId);
            if (inheritedParent != null && !inheritedParent.ChildIds.Contains(selectedChild.Id))
                inheritedParent.ChildIds.Add(selectedChild.Id);

            selectedChild.EndBar = Math.Max(selectedChild.EndBar, bar.Bar);
            AddVolumePoint(selectedChild, bar);
            UpdateP2P3(selectedChild, bar);
            LogStructuralLineage(selectedChild, bar, "child promoted after parent break");

            if (ShouldAdjustContainerForRtlWick(selectedChild, bar))
                AdjustContainerToWickViolation(selectedChild, bar);

            UpdateVe(selectedChild, bar);
            TryJoinTriads(selectedChild);
            LogStructuralLineagePromotionContext(selectedParent, selectedChild, bar);
            AssignStructuralLineageContextInheritance(selectedChild, bar, "child promoted after parent break");
            if (Debug && selectedChild.StructuralLineageJoinId != 0)
                LogDebug(bar.Bar,
                    "promotion inherited structural context: child=" + selectedChild.Id
                    + StructuralInheritanceSummary(selectedChild)
                    + " brokenParent=" + selectedParent.Id
                    + " inheritedParent=" + selectedChild.ParentId
                    + " level=" + selectedChild.Level);
            LogActiveStructuralLineageContext(selectedChild, bar, "promote child after parent break");
        }

        private bool TryCreateV2ChildContainer(AnalyzedBar bar, ContainerDirection direction)
        {
            xPvaContainerEngineV2.Decision decision = GetV2Decision(bar, direction);
            if (decision.Kind != xPvaContainerEngineV2.DecisionKind.CreateChild)
                return false;

            PriceContainer parent = FindContainer(decision.ParentId);
            if (parent == null)
                return false;
            if (parent.Status != ContainerStatus.Active && parent.Status != ContainerStatus.Adjusted)
                return false;
            if (!HasLiveParentScope(parent))
                return false;
            if (parent.Direction == direction)
            {
                LogDebug(bar.Bar, "V2 child creation suppressed: same-direction local parent id=" + parent.Id + " dir=" + direction);
                return false;
            }

            PriceContainer brokenByClose = FindBrokenContainer(bar, direction);
            if (brokenByClose != null && brokenByClose.Id == parent.Id)
            {
                LogDebug(bar.Bar,
                    "V2 child creation deferred: proposed parent id=" + parent.Id
                    + " is broken by current " + direction + " close");
                return false;
            }

            int childOrigin = Math.Max(decision.StartBar, parent.StartBar);
            if (childOrigin >= bar.Bar)
                return false;

            PriceContainer child = CreateContainer(childOrigin, bar.Bar, direction, parent.Level + 1, "V2 local child inside active container " + parent.Id + " from " + bar.Relation);
            if (child == null)
                return false;

            child.ParentId = parent.Id;
            if (!parent.ChildIds.Contains(child.Id))
                parent.ChildIds.Add(child.Id);
            AssignStructuralLineage(child, parent, bar, "V2 local child");
            AssignStructuralLineageContextInheritance(child, bar, "V2 local child");
            AssignInheritedParentStructuralContext(child, parent, bar, "create V2 child");
            LogActiveStructuralLineageContext(child, bar, "create V2 child");

            LogDebug(bar.Bar, "V2 child container created: id=" + child.Id + " parent=" + parent.Id + " level=" + child.Level + " reason=" + decision.Reason);
            TryJoinTriads(child);

            return true;
        }

        private void LogV2ShadowDecision(AnalyzedBar bar, ContainerDirection direction)
        {
            if (!Debug || bar == null || direction == ContainerDirection.Unknown)
                return;

            xPvaContainerEngineV2.Decision decision = GetV2Decision(bar, direction);
            if (decision.Kind == xPvaContainerEngineV2.DecisionKind.CreateChild
                || decision.Kind == xPvaContainerEngineV2.DecisionKind.PromoteChild)
            {
                LogDebug(bar.Bar,
                    "V2 shadow decision: " + decision.Kind
                    + " id=" + decision.ContainerId
                    + " parent=" + decision.ParentId
                    + " dir=" + decision.Direction
                    + " level=" + decision.Level
                    + " start=" + decision.StartBar
                    + " end=" + decision.EndBar
                    + " reason=" + decision.Reason);
            }
        }

        private xPvaContainerEngineV2.Decision GetV2Decision(AnalyzedBar bar, ContainerDirection direction)
        {
            var engine = new xPvaContainerEngineV2();
            foreach (PriceContainer container in containers)
            {
                engine.SeedContainer(
                    container.Id,
                    container.ParentId,
                    ToV2Direction(container.Direction),
                    container.Level,
                    ToV2Status(container.Status),
                    container.StartBar,
                    container.EndBar,
                    container.Reason);
            }

            var context = new xPvaContainerEngineV2.BarContext
            {
                Bar = bar.Bar,
                OriginBar = pendingTape.StartBar >= 0 ? pendingTape.StartBar : Math.Max(0, bar.Bar - 1),
                TapeDirection = ToV2Direction(direction),
                IsTransitional = bar.IsTransitional,
                IsTranslational = bar.IsTranslational,
                LocalParentId = FindDeepestLiveContextContainer(bar)
            };

            PriceContainer broken = FindBrokenContainer(bar, direction);
            if (broken != null)
                context.OppositeChildBreakoutId = broken.Id;

            foreach (PriceContainer frozen in containersFrozenThisBar)
                context.FrozenParentIds.Add(frozen.Id);

            return engine.Process(context);
        }

        private int FindDeepestLiveContextContainer(AnalyzedBar bar)
        {
            PriceContainer selected = null;
            foreach (PriceContainer container in containers)
            {
                if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;
                if (container.StartBar > bar.Bar || container.EndBar < Math.Max(0, bar.Bar - 1))
                    continue;

                if (selected == null
                    || container.Level > selected.Level
                    || (container.Level == selected.Level && container.StartBar > selected.StartBar))
                    selected = container;
            }

            return selected != null ? selected.Id : 0;
        }

        private xPvaContainerEngineV2.Direction ToV2Direction(ContainerDirection direction)
        {
            if (direction == ContainerDirection.Up)
                return xPvaContainerEngineV2.Direction.Up;
            if (direction == ContainerDirection.Down)
                return xPvaContainerEngineV2.Direction.Down;
            return xPvaContainerEngineV2.Direction.Unknown;
        }

        private xPvaContainerEngineV2.Status ToV2Status(ContainerStatus status)
        {
            if (status == ContainerStatus.Active || status == ContainerStatus.Adjusted)
                return xPvaContainerEngineV2.Status.Active;
            if (status == ContainerStatus.Joined)
                return xPvaContainerEngineV2.Status.Joined;
            return xPvaContainerEngineV2.Status.Frozen;
        }

        private ContainerDirection OppositeDirection(ContainerDirection direction)
        {
            if (direction == ContainerDirection.Up)
                return ContainerDirection.Down;
            if (direction == ContainerDirection.Down)
                return ContainerDirection.Up;
            return ContainerDirection.Unknown;
        }

        private PriceContainer FindPromotableChild(PriceContainer parent, ContainerDirection direction)
        {
            if (parent == null)
                return null;

            PriceContainer selected = FindPromotableDirectChild(parent, direction);
            if (selected != null)
                return selected;

            foreach (PriceContainer child in containers)
            {
                if (child == null)
                    continue;
                if (child.Direction != direction)
                    continue;
                if (child.Status != ContainerStatus.Active && child.Status != ContainerStatus.Adjusted)
                    continue;
                if (!IsDescendantOf(child, parent.Id))
                    continue;

                if (selected == null
                    || child.StartBar < selected.StartBar
                    || (child.StartBar == selected.StartBar && child.Level < selected.Level))
                    selected = child;
            }

            return selected;
        }

        private PriceContainer FindPromotableDirectChild(PriceContainer parent, ContainerDirection direction)
        {
            PriceContainer selected = null;
            if (parent == null)
                return null;

            foreach (int childId in parent.ChildIds)
            {
                PriceContainer child = FindContainer(childId);
                if (child == null)
                    continue;
                if (child.ParentId != parent.Id)
                    continue;
                if (child.Direction != direction)
                    continue;
                if (child.Status != ContainerStatus.Active && child.Status != ContainerStatus.Adjusted)
                    continue;

                if (selected == null
                    || child.Level > selected.Level
                    || (child.Level == selected.Level && child.StartBar > selected.StartBar))
                    selected = child;
            }

            return selected;
        }

        private void DeactivateBrokenParentDescendants(PriceContainer brokenParent, PriceContainer promotedChild, AnalyzedBar bar)
        {
            if (brokenParent == null)
                return;

            int deactivated = 0;
            foreach (PriceContainer container in containers)
            {
                if (container.Id == brokenParent.Id || container.Id == promotedChild.Id)
                    continue;
                if (promotedChild != null && IsDescendantOf(container, promotedChild.Id))
                    continue;
                if (!IsDescendantOf(container, brokenParent.Id))
                    continue;
                if (container.Status == ContainerStatus.Broken)
                    continue;

                container.Status = ContainerStatus.Broken;
                deactivated++;
            }

            if (deactivated > 0)
                LogDebug(bar.Bar, "broken parent descendants deactivated: parent=" + brokenParent.Id + " count=" + deactivated);
        }

        private void LogStructuralLineagePromotionContext(PriceContainer brokenParent, PriceContainer promotedChild, AnalyzedBar bar)
        {
            if (promotedChild == null || structuralLineageJoins.Count == 0)
                return;

            bool found = false;
            foreach (StructuralLineageJoin join in structuralLineageJoins)
            {
                string role = StructuralLineageContextRole(join, promotedChild);
                if (role == null)
                    continue;

                found = true;
                bool activated = ActivateStructuralLineageContext(join, role, brokenParent, promotedChild, bar);

                if (Debug)
                    LogDebug(bar.Bar,
                        "structural promotion context: promoted=" + promotedChild.Id
                        + " role=" + role
                        + " structuralJoin=" + join.Id
                        + " lineage=" + join.LineageId
                        + " triad=" + join.LeftId + "," + join.MiddleId + "," + join.RightId
                        + " brokenParent=" + (brokenParent == null ? 0 : brokenParent.Id)
                        + " inheritedParent=" + promotedChild.ParentId
                        + " level=" + promotedChild.Level
                        + " originFtt=" + join.OriginFttContainerId + "@" + join.OriginFttBar
                        + " span=" + join.StartBar + "-" + join.EndBar
                        + " p1=" + join.P1Bar + "@" + FormatPrice(join.P1Price)
                        + " p2=" + join.P2Bar + "@" + FormatPrice(join.P2Price)
                        + " p3=" + join.P3Bar + "@" + FormatPrice(join.P3Price)
                        + " activeContext=" + join.IsActiveContext
                        + " action=" + (activated ? "activated" : "observed"));
            }

            if (!found && Debug)
            {
                LogDebug(bar.Bar,
                    "structural promotion context: promoted=" + promotedChild.Id
                    + " brokenParent=" + (brokenParent == null ? 0 : brokenParent.Id)
                    + " structuralJoin=none action=observed");
            }
        }

        private bool ActivateStructuralLineageContext(StructuralLineageJoin join, string role, PriceContainer brokenParent, PriceContainer promotedChild, AnalyzedBar bar)
        {
            if (join == null || promotedChild == null)
                return false;
            if (role != "right" && role != "supersededRight" && role != "descendant" && role != "structuralDescendant")
                return false;
            if ((role == "descendant" || role == "structuralDescendant") && !join.IsActiveContext)
                return false;

            bool changed = !join.IsActiveContext || join.ContextContainerId != promotedChild.Id;
            join.IsActiveContext = true;
            join.ContextActivatedBar = bar.Bar;
            join.ContextContainerId = promotedChild.Id;
            join.ContextBrokenParentId = brokenParent == null ? 0 : brokenParent.Id;
            return changed;
        }

        private string StructuralJoinRole(StructuralLineageJoin join, int containerId)
        {
            if (join == null)
                return null;
            if (join.LeftId == containerId)
                return "left";
            if (join.MiddleId == containerId)
                return "middle";
            if (join.RightId == containerId)
                return "right";
            if (join.SupersededRightIds.Contains(containerId))
                return "supersededRight";
            return null;
        }

        private string StructuralLineageContextRole(StructuralLineageJoin join, PriceContainer container)
        {
            if (join == null || container == null)
                return null;
            if (container.LineageId != join.LineageId)
                return null;

            string role = StructuralJoinRole(join, container.Id);
            if (role != null)
                return role;
            if (!join.IsActiveContext)
                return null;
            if (IsDescendantOfActiveStructuralContext(container, join))
                return "descendant";
            if (IsStructuralDescendantOfActiveStructuralContext(container, join))
                return "structuralDescendant";
            return null;
        }

        private StructuralLineageJoin FindActiveStructuralLineageContext(PriceContainer container)
        {
            if (container == null || container.LineageId == 0)
                return null;

            StructuralLineageJoin selected = null;
            foreach (StructuralLineageJoin join in structuralLineageJoins)
            {
                if (!join.IsActiveContext || join.LineageId != container.LineageId)
                    continue;
                if (join.ContextContainerId != container.Id
                    && StructuralJoinRole(join, container.Id) == null
                    && !IsDescendantOfActiveStructuralContext(container, join)
                    && !IsStructuralDescendantOfActiveStructuralContext(container, join))
                    continue;
                if (selected == null || join.ContextActivatedBar > selected.ContextActivatedBar)
                    selected = join;
            }

            return selected;
        }

        private void LogActiveStructuralLineageContext(PriceContainer container, AnalyzedBar bar, string action)
        {
            if (!Debug || container == null || bar == null)
                return;

            StructuralLineageJoin join = FindActiveStructuralLineageContext(container);
            if (join == null)
                return;

            string role = StructuralLineageContextRole(join, container);
            if (role == null)
                role = "context";

            LogDebug(bar.Bar,
                "active structural context: action=" + action
                + " container=" + container.Id
                + " role=" + role
                + " structuralJoin=" + join.Id
                + " lineage=" + join.LineageId
                + " triad=" + join.LeftId + "," + join.MiddleId + "," + join.RightId
                + " contextContainer=" + join.ContextContainerId
                + " activatedBar=" + join.ContextActivatedBar
                + " span=" + join.StartBar + "-" + join.EndBar);
        }

        private void AssignStructuralLineageContextInheritance(PriceContainer container, AnalyzedBar bar, string reason)
        {
            if (container == null || bar == null)
                return;

            StructuralLineageJoin join = FindActiveStructuralLineageContext(container);
            if (join == null)
                return;

            string role = StructuralLineageContextRole(join, container);
            if (role == null)
                return;

            bool changed = container.StructuralLineageJoinId != join.Id
                || container.StructuralLineageContextRole != role
                || container.StructuralLineageContextSourceId != join.ContextContainerId;

            container.StructuralLineageJoinId = join.Id;
            container.StructuralLineageContextRole = role;
            container.StructuralLineageContextSourceId = join.ContextContainerId;

            if (Debug)
                LogDebug(bar.Bar,
                    "structural context inherited: container=" + container.Id
                    + " structuralJoin=" + join.Id
                    + " role=" + role
                    + " source=" + join.ContextContainerId
                    + " lineage=" + join.LineageId
                    + " triad=" + join.LeftId + "," + join.MiddleId + "," + join.RightId
                    + " reason=" + reason
                    + " action=" + (changed ? "assigned" : "observed"));
        }

        private string StructuralInheritanceSummary(PriceContainer container)
        {
            if (container == null)
                return "";

            string summary = "";
            if (container.StructuralLineageJoinId != 0)
            {
                summary += ":sj" + container.StructuralLineageJoinId
                    + ":" + container.StructuralLineageContextRole
                    + ":src" + container.StructuralLineageContextSourceId;
            }

            if (container.InheritedParentStructuralLineageJoinId != 0)
            {
                summary += ":psj" + container.InheritedParentStructuralLineageJoinId
                    + ":" + container.InheritedParentStructuralRole
                    + ":psrc" + container.InheritedParentStructuralSourceId;
            }

            return summary;
        }

        private void AssignInheritedParentStructuralContext(PriceContainer child, PriceContainer parent, AnalyzedBar bar, string action)
        {
            if (child == null || parent == null || bar == null)
                return;

            int inheritedJoinId = parent.StructuralLineageJoinId;
            int inheritedSourceId = parent.Id;
            string inheritedRole = parent.StructuralLineageContextRole;
            if (inheritedJoinId == 0)
            {
                inheritedJoinId = parent.InheritedParentStructuralLineageJoinId;
                inheritedSourceId = parent.InheritedParentStructuralSourceId;
                inheritedRole = parent.InheritedParentStructuralRole;
            }

            if (inheritedJoinId == 0)
                return;

            bool changed = child.InheritedParentStructuralLineageJoinId != inheritedJoinId
                || child.InheritedParentStructuralSourceId != inheritedSourceId
                || child.InheritedParentStructuralRole != inheritedRole;

            child.InheritedParentStructuralLineageJoinId = inheritedJoinId;
            child.InheritedParentStructuralSourceId = inheritedSourceId;
            child.InheritedParentStructuralRole = inheritedRole;

            LogDebug(bar.Bar,
                "structural inherited parent context: action=" + action
                + " child=" + child.Id
                + " parent=" + parent.Id
                + StructuralInheritanceSummary(parent)
                + " childLineage=" + child.LineageId
                + " parentLineage=" + parent.LineageId
                + " inheritanceAction=" + (changed ? "assigned" : "observed"));
        }

        private void CloseStructuralLineageContextsForBrokenContainer(PriceContainer broken, AnalyzedBar bar, string reason)
        {
            if (broken == null || bar == null)
                return;

            foreach (StructuralLineageJoin join in structuralLineageJoins)
            {
                if (!join.IsActiveContext)
                    continue;
                if (join.ContextContainerId != broken.Id)
                    continue;

                join.IsActiveContext = false;
                LogDebug(bar.Bar,
                    "structural context closed: structuralJoin=" + join.Id
                    + " lineage=" + join.LineageId
                    + " contextContainer=" + broken.Id
                    + " reason=" + reason);
            }
        }

        private bool IsDescendantOf(PriceContainer container, int ancestorId)
        {
            if (container == null || ancestorId == 0)
                return false;

            int parentId = container.ParentId;
            while (parentId != 0)
            {
                if (parentId == ancestorId)
                    return true;

                PriceContainer parent = FindContainer(parentId);
                if (parent == null)
                    return false;

                parentId = parent.ParentId;
            }

            return false;
        }

        private bool IsDescendantOfActiveStructuralContext(PriceContainer container, StructuralLineageJoin join)
        {
            if (container == null || join == null)
                return false;

            if (IsDescendantOf(container, join.ContextContainerId))
                return true;
            if (IsDescendantOf(container, join.RightId))
                return true;

            foreach (int supersededRightId in join.SupersededRightIds)
                if (IsDescendantOf(container, supersededRightId))
                    return true;

            return false;
        }

        private bool IsStructuralDescendantOf(PriceContainer container, int ancestorId)
        {
            if (container == null || ancestorId == 0)
                return false;

            int parentId = container.StructuralParentId;
            while (parentId != 0)
            {
                if (parentId == ancestorId)
                    return true;

                PriceContainer parent = FindContainer(parentId);
                if (parent == null)
                    return false;

                parentId = parent.StructuralParentId;
            }

            return false;
        }

        private bool IsStructuralDescendantOfActiveStructuralContext(PriceContainer container, StructuralLineageJoin join)
        {
            if (container == null || join == null)
                return false;

            if (IsStructuralDescendantOf(container, join.ContextContainerId))
                return true;
            if (IsStructuralDescendantOf(container, join.RightId))
                return true;

            foreach (int supersededRightId in join.SupersededRightIds)
                if (IsStructuralDescendantOf(container, supersededRightId))
                    return true;

            return false;
        }

        private void InheritBrokenChildParentScope(PriceContainer created, PriceContainer broken, AnalyzedBar bar)
        {
            if (created == null || broken == null || broken.ParentId == 0)
                return;
            if (created.Level != broken.Level)
                return;

            PriceContainer parent = FindContainer(broken.ParentId);
            if (parent == null)
                return;
            if ((parent.Status != ContainerStatus.Active && parent.Status != ContainerStatus.Adjusted) || !HasLiveParentScope(parent))
            {
                LogDebug(bar.Bar, "created response scope not inherited: parent inactive id=" + parent.Id + " broken=" + broken.Id);
                return;
            }

            created.ParentId = parent.Id;
            if (!parent.ChildIds.Contains(created.Id))
                parent.ChildIds.Add(created.Id);
            AssignStructuralLineageFromSource(created, broken, parent, bar, "created response inherited parent scope");

            LogDebug(bar.Bar, "created response inherited parent scope: id=" + created.Id + " parent=" + parent.Id + " broken=" + broken.Id + " level=" + created.Level);
        }

        private void UpdateExistingContainers(AnalyzedBar bar)
        {
            foreach (PriceContainer container in containers)
            {
                if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;

                if (bar.Bar <= container.StartBar)
                    continue;

                container.EndBar = Math.Max(container.EndBar, bar.Bar);
                AddVolumePoint(container, bar);
                UpdateP2P3(container, bar);

                if (ShouldAdjustContainerForRtlWick(container, bar))
                    AdjustContainerToWickViolation(container, bar);

                UpdateVe(container, bar);
                ExtendTerminalJoinedChild(container, bar);

                if (IsBarCompletelyOutsideBreakSide(container, bar))
                {
                    container.Status = ContainerStatus.Broken;
                    ConfirmFttCandidate(container, bar, "RTL break side freeze");
                    containersFrozenThisBar.Add(container);
                    CloseStructuralLineageContextsForBrokenContainer(container, bar, "RTL break side freeze");
                    LogDebug(bar.Bar, "container frozen: id=" + container.Id + " level=" + container.Level + " " + container.Direction + " bar fully outside RTL break side");
                }
            }
        }

        private void ExtendTerminalJoinedChild(PriceContainer parent, AnalyzedBar bar)
        {
            if (parent == null || bar == null)
                return;
            if (string.IsNullOrEmpty(parent.Reason) || !parent.Reason.StartsWith("joined triad", StringComparison.Ordinal))
                return;

            PriceContainer terminalChild = FindTerminalJoinedChild(parent);
            if (terminalChild == null || bar.Bar <= terminalChild.EndBar || bar.Bar <= terminalChild.StartBar)
                return;

            terminalChild.EndBar = bar.Bar;
            AddVolumePoint(terminalChild, bar);
            UpdateP2P3(terminalChild, bar);

            if (ShouldAdjustContainerForRtlWick(terminalChild, bar))
                AdjustContainerToWickViolation(terminalChild, bar);

            UpdateVe(terminalChild, bar);

            if (IsBarCompletelyOutsideBreakSide(terminalChild, bar))
            {
                terminalChild.Status = ContainerStatus.Broken;
                containersFrozenThisBar.Add(terminalChild);
                CloseStructuralLineageContextsForBrokenContainer(terminalChild, bar, "terminal joined child freeze");
                LogDebug(bar.Bar, "terminal joined child frozen: child=" + terminalChild.Id + " parent=" + parent.Id + " bar fully outside RTL break side");
                return;
            }

            LogDebug(bar.Bar, "terminal joined child extended with parent: child=" + terminalChild.Id + " parent=" + parent.Id);
        }

        private PriceContainer FindTerminalJoinedChild(PriceContainer parent)
        {
            PriceContainer selected = null;
            foreach (int childId in parent.ChildIds)
            {
                PriceContainer child = FindContainer(childId);
                if (child == null)
                    continue;
                if (child.ParentId != parent.Id)
                    continue;
                if (child.Direction != parent.Direction)
                    continue;
                if (child.Status != ContainerStatus.Joined)
                    continue;

                if (selected == null ||
                    child.StartBar > selected.StartBar ||
                    (child.StartBar == selected.StartBar && child.EndBar > selected.EndBar))
                    selected = child;
            }

            return selected;
        }

        private bool HasLiveParentScope(PriceContainer container)
        {
            if (container == null)
                return false;

            int parentId = container.ParentId;
            int guard = 0;
            while (parentId != 0 && guard++ < containers.Count)
            {
                PriceContainer parent = FindContainer(parentId);
                if (parent == null || parent.Status == ContainerStatus.Broken)
                    return false;

                parentId = parent.ParentId;
            }

            return true;
        }

        private PriceContainer CreateContainer(int startBar, int endBar, ContainerDirection direction, int level, string reason)
        {
            AnalyzedBar start = GetAnalyzed(startBar);
            AnalyzedBar end = GetAnalyzed(endBar);
            if (start == null || end == null)
                return null;
            if (endBar == startBar + 1 && IsOutsideRelation(end.Relation))
            {
                LogDebug(endBar, "container construction suppressed: second bar is outside relation " + end.Relation + " start=" + startBar + " end=" + endBar);
                return null;
            }
            if (!IsValidTwoPointConstruction(start, end, direction))
            {
                LogDebug(endBar, "container construction suppressed: invalid " + direction + " two-point construction start=" + startBar + " end=" + endBar + " startLow=" + FormatPrice(start.Low) + " endLow=" + FormatPrice(end.Low) + " startHigh=" + FormatPrice(start.High) + " endHigh=" + FormatPrice(end.High));
                return null;
            }

            double p1Price = direction == ContainerDirection.Up ? start.Low : start.High;
            double rtlEnd = direction == ContainerDirection.Up ? end.Low : end.High;
            double slope = (rtlEnd - p1Price) / Math.Max(1, endBar - startBar);
            slope = FitInitialRtlToInterveningBars(startBar, endBar, direction, p1Price, slope);
            double ltlStart = direction == ContainerDirection.Up ? start.High : start.Low;
            double p2Price = direction == ContainerDirection.Up ? Math.Max(start.High, end.High) : Math.Min(start.Low, end.Low);
            int p2Bar = direction == ContainerDirection.Up
                ? (end.High >= start.High ? endBar : startBar)
                : (end.Low <= start.Low ? endBar : startBar);

            var container = new PriceContainer
            {
                Id = nextContainerId++,
                ParentId = 0,
                Level = Math.Max(1, level),
                Direction = direction,
                Status = ContainerStatus.Active,
                StartBar = startBar,
                EndBar = endBar,
                P1Bar = startBar,
                P1Price = p1Price,
                P2Bar = p2Bar,
                P2Price = p2Price,
                P3Bar = 0,
                P3Price = 0.0,
                Rtl = new PriceLine { StartBar = startBar, StartPrice = p1Price, Slope = slope },
                Ltl = new PriceLine { StartBar = startBar, StartPrice = ltlStart, Slope = slope },
                Reason = reason,
                LineageId = nextLineageId++,
                Role = StructuralRole.Origin
            };

            InitializeFttCandidate(container);
            InitializeOuterExpansion(container, start, end);
            AddVolumePoint(container, start);
            AddVolumePoint(container, end);
            containers.Add(container);
            LogDebug(endBar, "container created: id=" + container.Id + " " + direction + " L" + container.Level + " start=" + startBar + " end=" + endBar + " lineage=" + container.LineageId + " reason=" + reason);
            return container;
        }

        private double FitInitialRtlToInterveningBars(int startBar, int endBar, ContainerDirection direction, double p1Price, double initialSlope)
        {
            double selectedSlope = initialSlope;
            int supportBar = endBar;

            for (int bar = startBar + 1; bar < endBar; bar++)
            {
                double candidatePrice = direction == ContainerDirection.Up
                    ? Low.GetValueAt(bar)
                    : High.GetValueAt(bar);
                double projected = p1Price + selectedSlope * (bar - startBar);
                bool violates = direction == ContainerDirection.Up
                    ? candidatePrice < projected - TickSize * 0.5
                    : candidatePrice > projected + TickSize * 0.5;
                if (!violates)
                    continue;

                double candidateSlope = (candidatePrice - p1Price) / Math.Max(1, bar - startBar);
                if ((direction == ContainerDirection.Up && candidateSlope < selectedSlope)
                    || (direction == ContainerDirection.Down && candidateSlope > selectedSlope))
                {
                    selectedSlope = candidateSlope;
                    supportBar = bar;
                }
            }

            if (supportBar != endBar)
                LogDebug(endBar, "initial RTL fitted to intervening bar " + supportBar + " for " + direction + " container start=" + startBar + " end=" + endBar);

            return selectedSlope;
        }

        private void InitializeFttCandidate(PriceContainer container)
        {
            if (container == null)
                return;

            container.FttCandidateBar = container.P2Bar;
            container.FttCandidatePrice = container.P2Price;
        }

        private void UpdateFttCandidate(PriceContainer container, AnalyzedBar bar)
        {
            if (container == null || bar == null)
                return;

            bool changed = false;
            if (container.Direction == ContainerDirection.Up && bar.High > container.FttCandidatePrice)
            {
                container.FttCandidatePrice = bar.High;
                container.FttCandidateBar = bar.Bar;
                changed = true;
            }
            else if (container.Direction == ContainerDirection.Down && bar.Low < container.FttCandidatePrice)
            {
                container.FttCandidatePrice = bar.Low;
                container.FttCandidateBar = bar.Bar;
                changed = true;
            }

            if (changed)
                LogDebug(bar.Bar, "FTT candidate updated: id=" + container.Id + " lineage=" + container.LineageId + " candidateBar=" + container.FttCandidateBar + " price=" + FormatPrice(container.FttCandidatePrice));
        }

        private void ConfirmFttCandidate(PriceContainer container, AnalyzedBar bar, string reason)
        {
            if (container == null || bar == null || container.FttConfirmed || container.FttCandidateBar <= 0)
                return;

            container.FttConfirmed = true;
            LogDebug(bar.Bar, "FTT confirmed: id=" + container.Id + " lineage=" + container.LineageId + " fttBar=" + container.FttCandidateBar + " price=" + FormatPrice(container.FttCandidatePrice) + " reason=" + reason);
        }

        private void AssignStructuralLineage(PriceContainer child, PriceContainer parent, AnalyzedBar bar, string reason)
        {
            if (child == null || parent == null)
                return;

            child.StructuralParentId = parent.Id;
            child.Role = StructuralRole.Component;

            if (child.Direction != parent.Direction && ShouldStartOppositeCampaignFromParent(parent))
            {
                PriceContainer existing = FindLineageByOrigin(parent.Id, parent.FttCandidateBar);
                child.LineageId = existing != null ? existing.LineageId : nextLineageId++;
                child.OriginFttContainerId = parent.Id;
                child.OriginFttBar = parent.FttCandidateBar;
                child.OriginFttPrice = parent.FttCandidatePrice;
            }
            else if (parent.OriginFttContainerId != 0)
            {
                child.LineageId = parent.LineageId;
                child.OriginFttContainerId = parent.OriginFttContainerId;
                child.OriginFttBar = parent.OriginFttBar;
                child.OriginFttPrice = parent.OriginFttPrice;
            }
            else if (child.Direction != parent.Direction)
            {
                PriceContainer existing = FindLineageByOrigin(parent.Id, parent.FttCandidateBar);
                child.LineageId = existing != null ? existing.LineageId : nextLineageId++;
                child.OriginFttContainerId = parent.Id;
                child.OriginFttBar = parent.FttCandidateBar;
                child.OriginFttPrice = parent.FttCandidatePrice;
            }
            else
            {
                child.LineageId = parent.LineageId;
                child.OriginFttContainerId = parent.OriginFttContainerId;
                child.OriginFttBar = parent.OriginFttBar;
                child.OriginFttPrice = parent.OriginFttPrice;
            }

            LogStructuralLineage(child, bar, reason);
        }

        private void AssignStructuralLineageFromSource(PriceContainer child, PriceContainer source, PriceContainer structuralParent, AnalyzedBar bar, string reason)
        {
            if (child == null || source == null)
                return;

            child.StructuralParentId = structuralParent != null ? structuralParent.Id : source.StructuralParentId;
            child.Role = StructuralRole.Component;
            child.LineageId = source.LineageId;
            child.OriginFttContainerId = source.OriginFttContainerId;
            child.OriginFttBar = source.OriginFttBar;
            child.OriginFttPrice = source.OriginFttPrice;

            LogStructuralLineage(child, bar, reason);
        }

        private bool ShouldStartOppositeCampaignFromParent(PriceContainer parent)
        {
            if (parent == null)
                return false;

            return parent.ParentId == 0 && parent.Level == 1;
        }

        private PriceContainer FindLineageByOrigin(int originContainerId, int originFttBar)
        {
            if (originContainerId == 0 || originFttBar == 0)
                return null;

            PriceContainer selected = null;
            foreach (PriceContainer candidate in containers)
            {
                if (candidate.OriginFttContainerId != originContainerId || candidate.OriginFttBar != originFttBar)
                    continue;
                if (candidate.LineageId == 0)
                    continue;
                if (selected == null || candidate.StartBar < selected.StartBar)
                    selected = candidate;
            }

            return selected;
        }

        private void LogStructuralLineage(PriceContainer container, AnalyzedBar bar, string reason)
        {
            if (container == null || bar == null)
                return;

            LogDebug(bar.Bar, "lineage assigned: id=" + container.Id + " lineage=" + container.LineageId + " structuralParent=" + container.StructuralParentId + " originFttContainer=" + container.OriginFttContainerId + " originFttBar=" + container.OriginFttBar + " role=" + container.Role + " reason=" + reason);
        }

        private void InitializeOuterExpansion(PriceContainer container, AnalyzedBar start, AnalyzedBar end)
        {
            if (container == null || start == null || end == null || end.Bar <= start.Bar)
                return;

            double projected = container.Ltl.ValueAt(end.Bar);
            if (container.Direction == ContainerDirection.Up && end.High > projected)
            {
                FreezeActiveOuterLine(container, end.Bar);
                container.ActiveVe = new PriceLine { StartBar = end.Bar, StartPrice = end.High, Slope = container.Rtl.Slope };
                LogDebug(end.Bar, "initial VE anchor: up container id=" + container.Id + " second bar high exceeded parallel LTL");
            }
            else if (container.Direction == ContainerDirection.Down && end.Low < projected)
            {
                FreezeActiveOuterLine(container, end.Bar);
                container.ActiveVe = new PriceLine { StartBar = end.Bar, StartPrice = end.Low, Slope = container.Rtl.Slope };
                LogDebug(end.Bar, "initial VE anchor: down container id=" + container.Id + " second bar low exceeded parallel LTL");
            }
        }

        private void AddVolumePoint(PriceContainer container, AnalyzedBar bar)
        {
            if (container == null || bar == null)
                return;

            for (int i = 0; i < container.VolumePoints.Count; i++)
            {
                if (container.VolumePoints[i].Bar == bar.Bar)
                    return;
            }

            container.VolumePoints.Add(new VolumePoint
            {
                Bar = bar.Bar,
                Volume = bar.Volume,
                SimplePeak = bar.IsSimplePeak,
                AcceleratedPeak = bar.IsAcceleratedPeak,
                PeakEligible = bar.IsPeakEligible
            });
        }

        private void UpdateP2P3(PriceContainer container, AnalyzedBar bar)
        {
            if (container.Direction == ContainerDirection.Up)
            {
                if (bar.High > container.P2Price)
                {
                    container.P2Price = bar.High;
                    container.P2Bar = bar.Bar;
                }

                if (container.P2Bar > container.P1Bar)
                {
                    double rtl = container.Rtl.ValueAt(bar.Bar);
                    if (Math.Abs(bar.Low - rtl) <= TickSize && IsValidP1P3Geometry(container.Direction, container.P1Price, bar.Low))
                    {
                        container.P3Bar = bar.Bar;
                        container.P3Price = bar.Low;
                    }
                }
            }
            else if (container.Direction == ContainerDirection.Down)
            {
                if (bar.Low < container.P2Price)
                {
                    container.P2Price = bar.Low;
                    container.P2Bar = bar.Bar;
                }

                if (container.P2Bar > container.P1Bar)
                {
                    double rtl = container.Rtl.ValueAt(bar.Bar);
                    if (Math.Abs(bar.High - rtl) <= TickSize && IsValidP1P3Geometry(container.Direction, container.P1Price, bar.High))
                    {
                        container.P3Bar = bar.Bar;
                        container.P3Price = bar.High;
                    }
                }
            }

            UpdateFttCandidate(container, bar);
        }

        private bool TryAbsorbContextualOutsideBar(AnalyzedBar bar, ContainerDirection direction)
        {
            PriceContainer container = null;
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                PriceContainer candidate = containers[i];
                if (candidate.Direction != direction)
                    continue;
                if (candidate.Status != ContainerStatus.Active && candidate.Status != ContainerStatus.Adjusted)
                    continue;
                if (bar.Bar <= candidate.StartBar)
                    continue;

                container = candidate;
                break;
            }

            if (container == null)
                return false;

            container.Status = ContainerStatus.Adjusted;
            LogDebug(bar.Bar, "OB absorbed into active " + direction + " container id=" + container.Id + " instead of creating reversal");
            return true;
        }

        private bool TryContinueActiveSameDirectionContainer(AnalyzedBar bar, ContainerDirection direction)
        {
            PriceContainer container = FindActiveContainer(direction);
            if (container == null)
                return false;

            LogSameDirectionSuppressionDiagnostics(bar, direction, container);
            PriceContainer brokenByClose = FindBrokenContainer(bar, direction);
            if (brokenByClose != null && IsDescendantOf(container, brokenByClose.Id))
            {
                PriceContainer promotionChild = FindPromotableChild(brokenByClose, direction);
                if (promotionChild == null)
                    promotionChild = container;

                brokenByClose.Status = ContainerStatus.Broken;
                if (!containersFrozenThisBar.Contains(brokenByClose))
                    containersFrozenThisBar.Add(brokenByClose);

                PromoteChildFromBrokenParent(brokenByClose, promotionChild, bar);
                LogDebug(bar.Bar,
                    "same-direction breakout promoted active child: child=" + promotionChild.Id
                    + " sourceActive=" + container.Id
                    + " inheritedParent=" + promotionChild.ParentId
                    + " level=" + promotionChild.Level
                    + " brokenParent=" + brokenByClose.Id);
                return true;
            }

            bool activeIsJoinedParent = !string.IsNullOrEmpty(container.Reason)
                && container.Reason.StartsWith("joined triad", StringComparison.Ordinal);
            if (brokenByClose != null
                && brokenByClose.ParentId == container.Id
                && brokenByClose.Direction != container.Direction
                && !activeIsJoinedParent)
            {
                brokenByClose.Status = ContainerStatus.Broken;
                ConfirmFttCandidate(brokenByClose, bar, "direct opposite child break");
                if (!containersFrozenThisBar.Contains(brokenByClose))
                    containersFrozenThisBar.Add(brokenByClose);
                CloseStructuralLineageContextsForBrokenContainer(brokenByClose, bar, "direct opposite child break");
                LogDebug(bar.Bar,
                    "direct opposite child terminated: parent=" + container.Id
                    + " child=" + brokenByClose.Id
                    + " direction=" + direction);

                int responseOrigin = -1;
                if (lateral.IsValid && lateral.BarsInside >= 3)
                {
                    int lateralOrigin = Math.Max(lateral.StartBar, brokenByClose.StartBar);
                    AnalyzedBar lateralStart = GetAnalyzed(lateralOrigin);
                    if (lateralStart != null && IsValidTwoPointConstruction(lateralStart, bar, direction))
                    {
                        responseOrigin = lateralOrigin;
                        resolvedLateralBar = bar.Bar;
                        resolvedLateralOrigin = lateralOrigin;
                        lateral.IsValid = false;
                        LogDebug(bar.Bar,
                            "direct child break consumed lateral origin " + lateralOrigin
                            + " for " + direction + " response");
                    }
                }
                if (responseOrigin < 0)
                    responseOrigin = Math.Max(ResolveOriginBar(bar), brokenByClose.StartBar);
                PriceContainer response = responseOrigin < bar.Bar
                    ? CreateContainer(responseOrigin, bar.Bar, direction, brokenByClose.Level,
                        "same-direction response after direct child break " + brokenByClose.Id + " from " + bar.Relation)
                    : null;
                if (response != null)
                {
                    response.ParentId = container.Id;
                    if (!container.ChildIds.Contains(response.Id))
                        container.ChildIds.Add(response.Id);
                    AssignStructuralLineageFromSource(response, brokenByClose, container, bar, "same-direction response after direct child break");
                    AssignStructuralLineageContextInheritance(response, bar, "same-direction response after direct child break");
                    AssignInheritedParentStructuralContext(response, container, bar, "create direct child-break response");
                    LogDebug(bar.Bar,
                        "direct child-break response created: id=" + response.Id
                        + " parent=" + container.Id
                        + " level=" + response.Level
                        + " origin=" + responseOrigin);
                    TryJoinTriads(response);
                }
                else
                {
                    TryJoinTriads(container);
                }
                return true;
            }

            PriceContainer joinedBreakoutChild;
            if (ShouldDeferSameDirectionSuppressionForJoinedChildBreakout(bar, direction, container, out joinedBreakoutChild))
            {
                if (TryResolveJoinedChildBreakoutWithTerminalChild(container, joinedBreakoutChild, bar, direction))
                    return true;
                return false;
            }

            LogDebug(bar.Bar, "same-direction " + bar.Relation + " extended active " + direction + " container id=" + container.Id + "; nested container suppressed");
            TryJoinTriads(container);
            return true;
        }

        private bool ShouldDeferSameDirectionSuppressionForJoinedChildBreakout(AnalyzedBar bar, ContainerDirection direction, PriceContainer active, out PriceContainer brokenIfReached)
        {
            brokenIfReached = null;
            if (bar == null || active == null)
                return false;
            if (string.IsNullOrEmpty(active.Reason) || !active.Reason.StartsWith("joined triad", StringComparison.Ordinal))
                return false;

            brokenIfReached = FindBrokenContainer(bar, direction);
            if (brokenIfReached == null)
                return false;
            if (brokenIfReached.ParentId != active.Id)
                return false;
            if (brokenIfReached.Direction == active.Direction || brokenIfReached.Direction == ContainerDirection.Unknown)
                return false;

            LogDebug(bar.Bar, "same-direction suppression deferred: joined parent id=" + active.Id + " direct opposite child break id=" + brokenIfReached.Id);
            return true;
        }

        private bool TryResolveJoinedChildBreakoutWithTerminalChild(PriceContainer active, PriceContainer brokenChild, AnalyzedBar bar, ContainerDirection direction)
        {
            if (active == null || brokenChild == null || bar == null)
                return false;

            PriceContainer terminalChild = FindTerminalJoinedChild(active);
            if (terminalChild == null || terminalChild.Direction != direction)
                return false;
            if (terminalChild.EndBar < bar.Bar)
                return false;

            brokenChild.Status = ContainerStatus.Broken;
            if (!containersFrozenThisBar.Contains(brokenChild))
                containersFrozenThisBar.Add(brokenChild);

            LogDebug(bar.Bar, "same-direction joined child breakout absorbed by terminal child: parent=" + active.Id + " child=" + terminalChild.Id + " broken=" + brokenChild.Id);
            TryJoinTriads(terminalChild);
            return true;
        }

        private void LogSameDirectionSuppressionDiagnostics(AnalyzedBar bar, ContainerDirection direction, PriceContainer active)
        {
            if (!Debug || bar == null || active == null)
                return;

            PriceContainer brokenIfReached = FindBrokenContainer(bar, direction);
            LogDebug(bar.Bar,
                "same-direction suppression diagnostic: active id=" + active.Id
                + " dir=" + active.Direction
                + " level=" + active.Level
                + " status=" + active.Status
                + " start=" + active.StartBar
                + " end=" + active.EndBar
                + " reason=" + active.Reason
                + " pendingStart=" + pendingTape.StartBar
                + " brokenIfReached=" + DescribeContainerRef(brokenIfReached));

            foreach (int childId in active.ChildIds)
            {
                PriceContainer child = FindContainer(childId);
                if (child == null)
                {
                    LogDebug(bar.Bar, "same-direction suppression child diagnostic: parent=" + active.Id + " child=" + childId + " missing");
                    continue;
                }

                if (child.Direction == active.Direction || child.Direction == ContainerDirection.Unknown)
                    continue;

                double rtl = child.Rtl != null ? child.Rtl.ValueAt(bar.Bar) : double.NaN;
                bool closeBreaks = false;
                if (child.Rtl != null && (child.Status == ContainerStatus.Active || child.Status == ContainerStatus.Adjusted) && bar.Bar > child.StartBar)
                {
                    closeBreaks = child.Direction == ContainerDirection.Up
                        ? bar.Close < rtl - TickSize * 0.5
                        : bar.Close > rtl + TickSize * 0.5;
                }

                LogDebug(bar.Bar,
                    "same-direction suppression child diagnostic: parent=" + active.Id
                    + " child=" + child.Id
                    + " dir=" + child.Direction
                    + " level=" + child.Level
                    + " status=" + child.Status
                    + " start=" + child.StartBar
                    + " end=" + child.EndBar
                    + " reason=" + child.Reason
                    + " close=" + FormatPrice(bar.Close)
                    + " rtl=" + FormatPrice(rtl)
                    + " closeBreaks=" + closeBreaks);
            }
        }

        private string DescribeContainerRef(PriceContainer container)
        {
            if (container == null)
                return "none";

            return "id=" + container.Id
                + " dir=" + container.Direction
                + " level=" + container.Level
                + " status=" + container.Status
                + " start=" + container.StartBar
                + " end=" + container.EndBar
                + " reason=" + container.Reason
                + " parent=" + container.ParentId;
        }

        private string FormatPrice(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);

            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private PriceContainer FindFrozenOppositeChildForResponse(ContainerDirection responseDirection, int currentBar, int responseOrigin)
        {
            PriceContainer selected = null;
            foreach (PriceContainer frozen in containersFrozenThisBar)
            {
                if (frozen.Direction == responseDirection)
                    continue;
                if (frozen.ParentId == 0)
                    continue;
                bool isJoinedChild = !string.IsNullOrEmpty(frozen.Reason)
                    && frozen.Reason.StartsWith("joined triad", StringComparison.Ordinal);
                bool hasLocalResponseOrigin = responseOrigin > frozen.StartBar && responseOrigin < currentBar;
                if (!isJoinedChild && frozen.EndBar - frozen.StartBar > 2 && !hasLocalResponseOrigin)
                    continue;

                PriceContainer parent = FindContainer(frozen.ParentId);
                if (parent == null || parent.Direction != responseDirection)
                    continue;
                if (parent.Status != ContainerStatus.Active && parent.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(parent))
                    continue;

                if (selected == null || frozen.Level > selected.Level || frozen.StartBar > selected.StartBar)
                    selected = frozen;
            }

            return selected;
        }

        private bool IsValidTwoPointConstruction(AnalyzedBar start, AnalyzedBar end, ContainerDirection direction)
        {
            if (start == null || end == null || direction == ContainerDirection.Unknown)
                return false;
            if (direction == ContainerDirection.Up)
                return IsValidP1P3Geometry(direction, start.Low, end.Low);
            if (direction == ContainerDirection.Down)
                return IsValidP1P3Geometry(direction, start.High, end.High);
            return false;
        }

        private bool IsValidP1P3Geometry(ContainerDirection direction, double p1Price, double p3Price)
        {
            if (direction == ContainerDirection.Up)
                return p3Price > p1Price;
            if (direction == ContainerDirection.Down)
                return p3Price < p1Price;
            return false;
        }

        private bool ShouldSuppressImmediateChildAfterResponseBreak(AnalyzedBar bar, ContainerDirection direction)
        {
            if (bar == null || direction == ContainerDirection.Unknown)
                return false;

            foreach (PriceContainer frozen in containersFrozenThisBar)
            {
                if (frozen == null || frozen.Direction == direction)
                    continue;
                if (frozen.EndBar != bar.Bar)
                    continue;
                if (string.IsNullOrEmpty(frozen.Reason)
                    || !frozen.Reason.StartsWith("same-direction response after child break", StringComparison.Ordinal))
                    continue;

                PriceContainer parent = FindContainer(frozen.ParentId);
                if (parent == null)
                    continue;
                if (parent.Direction != frozen.Direction)
                    continue;
                if (parent.Status != ContainerStatus.Active && parent.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(parent))
                    continue;
                if (parent.Level <= 1)
                    continue;

                LogDebug(bar.Bar, "V2 child creation suppressed: nested response child just broke id=" + frozen.Id + " parent=" + parent.Id + " dir=" + direction);
                return true;
            }

            return false;
        }

        private PriceContainer FindActiveContainer(ContainerDirection direction)
        {
            PriceContainer selected = null;
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                PriceContainer container = containers[i];
                if (container.Direction != direction)
                    continue;
                if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;

                if (selected == null || container.Level >= selected.Level)
                    selected = container;
            }

            return selected;
        }

        private PriceContainer FindContainingActiveContainer(AnalyzedBar bar)
        {
            PriceContainer selected = null;
            foreach (PriceContainer container in containers)
            {
                if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;

                double rtl = container.Rtl.ValueAt(bar.Bar);
                double outer = (container.ActiveVe ?? container.Ltl).ValueAt(bar.Bar);
                double lower = Math.Min(rtl, outer) - TickSize * 0.5;
                double upper = Math.Max(rtl, outer) + TickSize * 0.5;

                if (bar.High <= upper && bar.Low >= lower)
                {
                    if (selected == null || container.Level >= selected.Level)
                        selected = container;
                }
            }

            return selected;
        }

        private void UpdateVe(PriceContainer container, AnalyzedBar bar)
        {
            if (container.Direction == ContainerDirection.Up)
            {
                double ltl = container.ActiveVe != null ? container.ActiveVe.ValueAt(bar.Bar) : container.Ltl.ValueAt(bar.Bar);
                if (bar.High > ltl)
                {
                    FreezeActiveOuterLine(container, bar.Bar);
                    container.ActiveVe = new PriceLine { StartBar = bar.Bar, StartPrice = bar.High, Slope = container.Rtl.Slope };
                    LogDebug(bar.Bar, "VE created: up container id=" + container.Id + " high pierced LTL/VE");
                }
            }
            else if (container.Direction == ContainerDirection.Down)
            {
                double ltl = container.ActiveVe != null ? container.ActiveVe.ValueAt(bar.Bar) : container.Ltl.ValueAt(bar.Bar);
                if (bar.Low < ltl)
                {
                    FreezeActiveOuterLine(container, bar.Bar);
                    container.ActiveVe = new PriceLine { StartBar = bar.Bar, StartPrice = bar.Low, Slope = container.Rtl.Slope };
                    LogDebug(bar.Bar, "VE created: down container id=" + container.Id + " low pierced LTL/VE");
                }
            }
        }

        private void FreezeActiveOuterLine(PriceContainer container, int endBar)
        {
            PriceLine line = container.ActiveVe ?? container.Ltl;
            if (line == null || endBar < line.StartBar)
                return;

            container.FrozenSegments.Add(new RenderSegment
            {
                ContainerId = container.Id,
                Level = container.Level,
                Direction = container.Direction,
                Kind = container.ActiveVe == null ? RenderLineKind.Ltl : RenderLineKind.Ve,
                StartBar = line.StartBar,
                EndBar = endBar,
                StartPrice = line.StartPrice,
                EndPrice = line.ValueAt(endBar)
            });
        }

        private bool IsRtlPenetratedByWick(PriceContainer container, AnalyzedBar bar)
        {
            double rtl = container.Rtl.ValueAt(bar.Bar);
            if (container.Direction == ContainerDirection.Up)
                return bar.Low < rtl - TickSize * 0.5;
            if (container.Direction == ContainerDirection.Down)
                return bar.High > rtl + TickSize * 0.5;
            return false;
        }

        private bool IsCloseInsideContainer(PriceContainer container, AnalyzedBar bar)
        {
            double rtl = container.Rtl.ValueAt(bar.Bar);
            if (container.Direction == ContainerDirection.Up)
                return bar.Close >= rtl - TickSize * 0.5;
            if (container.Direction == ContainerDirection.Down)
                return bar.Close <= rtl + TickSize * 0.5;
            return false;
        }

        private bool ShouldAdjustContainerForRtlWick(PriceContainer container, AnalyzedBar bar)
        {
            if (container == null || bar == null)
                return false;
            if (!IsRtlPenetratedByWick(container, bar) || !IsCloseInsideContainer(container, bar))
                return false;

            ContainerDirection barDirection = DirectionFromRelation(bar.Relation);
            if (barDirection == OppositeDirection(container.Direction))
            {
                xPvaContainerEngineV2.Decision decision = GetV2Decision(bar, barDirection);
                if (decision.Kind == xPvaContainerEngineV2.DecisionKind.CreateChild && decision.ParentId == container.Id)
                {
                    LogDebug(bar.Bar, "RTL wick adjustment deferred: id=" + container.Id + " opposite child creation takes precedence");
                    return false;
                }
            }

            double adjustedPrice = container.Direction == ContainerDirection.Up ? bar.Low : bar.High;
            if (!IsValidP1P3Geometry(container.Direction, container.P1Price, adjustedPrice))
            {
                LogDebug(bar.Bar,
                    "RTL wick adjustment rejected: id=" + container.Id
                    + " would violate " + container.Direction + " P1/P3 geometry"
                    + " p1=" + FormatPrice(container.P1Price)
                    + " candidateP3=" + FormatPrice(adjustedPrice));
                return false;
            }

            return true;
        }

        private bool IsBarCompletelyOutsideBreakSide(PriceContainer container, AnalyzedBar bar)
        {
            double tolerance = TickSize * 0.5;
            double rtl = container.Rtl.ValueAt(bar.Bar);

            // A full-range move through the RTL side freezes the current container.
            // A full-range move through the opposite side is expansion/VE territory,
            // not a break, so this check is intentionally direction-aware.
            if (container.Direction == ContainerDirection.Up)
                return bar.High < rtl - tolerance;
            if (container.Direction == ContainerDirection.Down)
                return bar.Low > rtl + tolerance;

            return false;
        }

        private void AdjustContainerToWickViolation(PriceContainer container, AnalyzedBar bar)
        {
            bool remainsJoined = container.Status == ContainerStatus.Joined;
            double adjustedPrice = container.Direction == ContainerDirection.Up ? bar.Low : bar.High;
            container.Rtl.Slope = (adjustedPrice - container.P1Price) / Math.Max(1, bar.Bar - container.P1Bar);
            container.Ltl.Slope = container.Rtl.Slope;
            RebuildOuterExpansion(container, bar.Bar);
            if (!remainsJoined)
                container.Status = ContainerStatus.Adjusted;
            LogDebug(bar.Bar,
                "RTL wick adjustment: id=" + container.Id
                + " close remained inside container"
                + (remainsJoined ? "; joined status preserved" : ""));
        }

        private void RebuildOuterExpansion(PriceContainer container, int throughBar)
        {
            if (container == null || container.Ltl == null || container.Rtl == null)
                return;

            container.FrozenSegments.Clear();
            container.ActiveVe = null;
            container.Ltl.Slope = container.Rtl.Slope;

            PriceLine active = container.Ltl;
            bool activeIsVe = false;

            int firstOuterBar = Math.Max(container.StartBar + 1, container.Ltl.StartBar);
            for (int bar = firstOuterBar; bar <= throughBar; bar++)
            {
                AnalyzedBar analyzed = GetAnalyzed(bar);
                if (analyzed == null)
                    continue;

                double projected = active.ValueAt(bar);
                bool pierced = container.Direction == ContainerDirection.Up
                    ? analyzed.High > projected
                    : analyzed.Low < projected;

                if (!pierced)
                    continue;

                container.FrozenSegments.Add(new RenderSegment
                {
                    ContainerId = container.Id,
                    Level = container.Level,
                    Direction = container.Direction,
                    Kind = activeIsVe ? RenderLineKind.Ve : RenderLineKind.Ltl,
                    StartBar = active.StartBar,
                    EndBar = bar,
                    StartPrice = active.StartPrice,
                    EndPrice = active.ValueAt(bar)
                });

                active = new PriceLine
                {
                    StartBar = bar,
                    StartPrice = container.Direction == ContainerDirection.Up ? analyzed.High : analyzed.Low,
                    Slope = container.Rtl.Slope
                };
                activeIsVe = true;
                LogDebug(bar,
                    "VE rebuilt: " + container.Direction.ToString().ToLowerInvariant()
                    + " container id=" + container.Id
                    + " price pierced recalculated LTL/VE");
            }

            if (activeIsVe)
                container.ActiveVe = active;
        }

        private PriceContainer FindBrokenContainer(AnalyzedBar bar, ContainerDirection newDirection)
        {
            PriceContainer best = null;
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                PriceContainer container = containers[i];
                if (container.Direction == newDirection)
                    continue;
                if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;
                if (bar.Bar <= container.StartBar)
                    continue;

                double rtl = container.Rtl.ValueAt(bar.Bar);
                bool closeBreaks = container.Direction == ContainerDirection.Up
                    ? bar.Close < rtl - TickSize * 0.5
                    : bar.Close > rtl + TickSize * 0.5;

                if (!closeBreaks)
                    continue;

                if (best == null || container.Level > best.Level || container.StartBar > best.StartBar)
                    best = container;
            }

            return best;
        }

        private int ResolveOriginBar(AnalyzedBar bar)
        {
            if (resolvedLateralBar == bar.Bar && resolvedLateralOrigin >= 0)
                return resolvedLateralOrigin;

            if (lateral.IsValid && lateral.BarsInside >= 3)
            {
                if (BarBreaksLateral(bar, DirectionFromRelation(bar.Relation)))
                {
                    resolvedLateralBar = bar.Bar;
                    resolvedLateralOrigin = lateral.StartBar;
                    lateral.IsValid = false;
                    LogDebug(bar.Bar, "lateral resolved into container from origin " + lateral.StartBar);
                    return resolvedLateralOrigin;
                }
            }

            return pendingTape.StartBar >= 0 ? pendingTape.StartBar : Math.Max(0, bar.Bar - 1);
        }

        private void AttachContainedChildren(PriceContainer parent)
        {
            if (parent == null)
                return;

            foreach (PriceContainer child in containers)
            {
                if (child.Id == parent.Id || child.ParentId != 0)
                    continue;
                if (child.StartBar >= parent.StartBar && child.EndBar <= parent.EndBar && child.Level <= parent.Level)
                {
                    child.ParentId = parent.Id;
                    parent.ChildIds.Add(child.Id);
                    DemoteRecursively(child, parent.Level + 1);
                }
            }
        }

        private void TryJoinTriads(PriceContainer newest)
        {
            if (newest == null)
                return;
            if (!HasLiveParentScope(newest))
                return;

            bool joined;
            do
            {
                joined = false;
                List<PriceContainer> eligible = ActiveSameParentContainers(newest.ParentId);
                eligible.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
                LogJoinEligibility(newest, eligible);
                AnalyzedBar newestEnd = GetAnalyzed(newest.EndBar);
                if (newestEnd != null)
                    LogActiveStructuralLineageContext(newest, newestEnd, "join scan");
                LogLineageJoinDiagnostics(newest);
                LogStructuralContextTriadDiagnostics(newest);

                for (int i = 0; i <= eligible.Count - 3; i++)
                {
                    PriceContainer left = eligible[i];
                    PriceContainer middle = eligible[i + 1];
                    PriceContainer right = eligible[i + 2];
                    string rejectReason;

                    if (!CanJoinContainers(left, middle, right, out rejectReason))
                    {
                        LogDebug(newest.EndBar, "join rejected: " + left.Id + "," + middle.Id + "," + right.Id + " reason=" + rejectReason);
                        continue;
                    }

                    PriceContainer parent = CreateJoinedParent(left, middle, right, false);
                    if (parent == null)
                        continue;

                    newest = parent;
                    joined = true;
                    break;
                }

                PriceContainer chainedParent;
                if (!joined && TryJoinStructuralContextTriad(newest, out chainedParent))
                {
                    newest = chainedParent;
                    joined = true;
                }

                if (!joined && TryJoinRecentSameLevelTriad(newest, out chainedParent))
                {
                    newest = chainedParent;
                    joined = true;
                }

                if (!joined && TryJoinChainedTriad(newest, out chainedParent))
                {
                    newest = chainedParent;
                    joined = true;
                }

                if (!joined && TryJoinLineageTriad(newest, out chainedParent))
                {
                    newest = chainedParent;
                    joined = true;
                }
            }
            while (joined);
        }

        private void LogJoinEligibility(PriceContainer newest, List<PriceContainer> eligible)
        {
            if (!Debug || newest == null)
                return;
            if (newest.Level < 2 && newest.StructuralLineageJoinId == 0 && newest.InheritedParentStructuralLineageJoinId == 0)
                return;

            string summary = "";
            foreach (PriceContainer candidate in eligible)
            {
                if (summary.Length > 0)
                    summary += ";";
                summary += candidate.Id + ":" + candidate.Direction + ":L" + candidate.Level + ":" + candidate.Status + "@" + candidate.StartBar + "-" + candidate.EndBar + ":p" + candidate.ParentId + StructuralInheritanceSummary(candidate);
            }

            LogDebug(newest.EndBar, "join eligibility: newest=" + newest.Id + StructuralInheritanceSummary(newest) + " parent=" + newest.ParentId + " level=" + newest.Level + " candidates=" + summary);
        }

        private bool TryJoinRecentSameLevelTriad(PriceContainer newest, out PriceContainer parent)
        {
            parent = null;
            if (newest == null || newest.Direction == ContainerDirection.Unknown)
                return false;
            if (newest.Status != ContainerStatus.Active && newest.Status != ContainerStatus.Adjusted)
                return false;
            if (!HasLiveParentScope(newest))
                return false;

            var eligible = new List<PriceContainer>();
            foreach (PriceContainer container in containers)
            {
                if (container.Id == newest.Id)
                    continue;
                if (container.ParentId != newest.ParentId || container.Level != newest.Level)
                    continue;
                if (container.Direction == ContainerDirection.Unknown || container.Status == ContainerStatus.Joined)
                    continue;
                if (!HasLiveParentScope(container))
                    continue;
                if (container.StartBar >= newest.StartBar)
                    continue;

                eligible.Add(container);
            }

            eligible.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            if (eligible.Count < 2)
                return false;

            for (int i = eligible.Count - 2; i >= 0; i--)
            {
                PriceContainer left = eligible[i];
                for (int j = eligible.Count - 1; j > i; j--)
                {
                    PriceContainer middle = eligible[j];
                    string rejectReason;

                    if (!CanJoinRecentContainers(left, middle, newest, out rejectReason))
                    {
                        LogDebug(newest.EndBar, "recent join rejected: " + left.Id + "," + middle.Id + "," + newest.Id + " reason=" + rejectReason);
                        LogLineageTriadGeometryIfRelated(newest.EndBar, left, middle, newest);
                        continue;
                    }

                    parent = CreateJoinedParent(left, middle, newest, false);
                    return parent != null;
                }
            }

            return false;
        }

        private bool TryJoinChainedTriad(PriceContainer newest, out PriceContainer parent)
        {
            parent = null;
            if (newest == null || newest.Level != 1 || newest.ParentId != 0)
                return false;
            if (newest.Direction == ContainerDirection.Unknown)
                return false;
            if (newest.Status != ContainerStatus.Active && newest.Status != ContainerStatus.Adjusted)
                return false;
            if (!HasLiveParentScope(newest))
                return false;

            PriceContainer bestLeft = null;
            PriceContainer bestMiddle = null;

            foreach (PriceContainer left in containers)
            {
                if (left.Id == newest.Id || left.Level != 1 || left.ParentId != 0)
                    continue;
                if (left.Direction != newest.Direction)
                    continue;
                if (left.StartBar >= newest.StartBar)
                    continue;
                if (left.Status == ContainerStatus.Joined)
                    continue;
                if (!HasLiveParentScope(left))
                    continue;

                foreach (int childId in left.ChildIds)
                {
                    PriceContainer middle = FindContainer(childId);
                    if (middle == null)
                        continue;
                    if (middle.Direction == newest.Direction || middle.Direction == ContainerDirection.Unknown)
                        continue;
                    if (middle.StartBar < left.StartBar || middle.EndBar > newest.StartBar)
                        continue;
                    if (middle.Status != ContainerStatus.Active && middle.Status != ContainerStatus.Adjusted && middle.Status != ContainerStatus.Broken)
                        continue;
                    if (!HasLiveParentScope(middle))
                        continue;

                    string rejectReason;
                    if (!CanJoinChainedContainers(left, middle, newest, out rejectReason))
                    {
                        LogDebug(newest.EndBar, "chained join rejected: " + left.Id + "," + middle.Id + "," + newest.Id + " reason=" + rejectReason);
                        continue;
                    }

                    if (bestLeft == null || left.StartBar > bestLeft.StartBar || middle.StartBar > bestMiddle.StartBar)
                    {
                        bestLeft = left;
                        bestMiddle = middle;
                    }
                }
            }

            if (bestLeft == null || bestMiddle == null)
                return false;

            parent = CreateJoinedParent(bestLeft, bestMiddle, newest, true);
            return parent != null;
        }

        private bool TryJoinLineageTriad(PriceContainer newest, out PriceContainer parent)
        {
            parent = null;
            if (!EnableLineageJoins)
                return false;
            if (newest == null || newest.LineageId == 0 || newest.Direction == ContainerDirection.Unknown)
                return false;
            if (newest.Status != ContainerStatus.Active && newest.Status != ContainerStatus.Adjusted)
                return false;
            if (!HasLiveParentScope(newest))
                return false;

            var related = new List<PriceContainer>();
            foreach (PriceContainer candidate in containers)
            {
                if (candidate.LineageId != newest.LineageId)
                    continue;
                if (candidate.Id == newest.Id)
                    continue;
                if (candidate.Direction == ContainerDirection.Unknown || candidate.Status == ContainerStatus.Joined)
                    continue;
                if (candidate.Status != ContainerStatus.Active && candidate.Status != ContainerStatus.Adjusted && candidate.Status != ContainerStatus.Broken)
                    continue;
                if (!HasLiveParentScope(candidate))
                    continue;
                if (candidate.StartBar >= newest.StartBar)
                    continue;

                related.Add(candidate);
            }

            related.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            if (related.Count < 2)
                return false;

            PriceContainer bestLeft = null;
            PriceContainer bestMiddle = null;

            for (int i = related.Count - 2; i >= 0; i--)
            {
                PriceContainer left = related[i];
                for (int j = related.Count - 1; j > i; j--)
                {
                    PriceContainer middle = related[j];
                    if (!CanJoinLineageContainers(left, middle, newest))
                        continue;

                    if (bestLeft == null
                        || left.StartBar > bestLeft.StartBar
                        || (left.StartBar == bestLeft.StartBar && middle.StartBar > bestMiddle.StartBar))
                    {
                        bestLeft = left;
                        bestMiddle = middle;
                    }
                }
            }

            if (bestLeft == null || bestMiddle == null)
                return false;

            parent = CreateJoinedParent(bestLeft, bestMiddle, newest, false);
            if (parent != null)
                LogDebug(parent.EndBar, "lineage join accepted: parent=" + parent.Id + " children=" + bestLeft.Id + "," + bestMiddle.Id + "," + newest.Id + " lineage=" + parent.LineageId + " originFtt=" + parent.OriginFttContainerId + "@" + parent.OriginFttBar);

            return parent != null;
        }

        private List<PriceContainer> ActiveSameParentContainers(int parentId)
        {
            var result = new List<PriceContainer>();
            foreach (PriceContainer container in containers)
            {
                if (container.ParentId == parentId
                    && (container.Status == ContainerStatus.Active || container.Status == ContainerStatus.Adjusted)
                    && HasLiveParentScope(container)
                    && container.Direction != ContainerDirection.Unknown)
                    result.Add(container);
            }
            return result;
        }

        private bool CanJoinContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";

            if (left.Level != middle.Level || middle.Level != right.Level)
            {
                rejectReason = "levels differ";
                return false;
            }

            if (left.ParentId != middle.ParentId || middle.ParentId != right.ParentId)
            {
                rejectReason = "parent scope differs";
                return false;
            }

            bool upDownUp = left.Direction == ContainerDirection.Up && middle.Direction == ContainerDirection.Down && right.Direction == ContainerDirection.Up;
            bool downUpDown = left.Direction == ContainerDirection.Down && middle.Direction == ContainerDirection.Up && right.Direction == ContainerDirection.Down;
            if (!upDownUp && !downUpDown)
            {
                rejectReason = "not an alternating triad";
                return false;
            }

            if (left.EndBar > middle.StartBar || middle.EndBar > right.StartBar)
            {
                rejectReason = "containers overlap out of order";
                return false;
            }

            if (upDownUp && !(left.P1Price < middle.P1Price && middle.P1Price > right.P1Price))
            {
                rejectReason = "up-down-up P1 geometry failed";
                return false;
            }

            if (downUpDown && !(left.P1Price > middle.P1Price && middle.P1Price < right.P1Price))
            {
                rejectReason = "down-up-down P1 geometry failed";
                return false;
            }

            return true;
        }

        private bool CanJoinRecentContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";

            if (left.Level != middle.Level || middle.Level != right.Level)
            {
                rejectReason = "levels differ";
                return false;
            }

            if (left.ParentId != middle.ParentId || middle.ParentId != right.ParentId)
            {
                rejectReason = "parent scope differs";
                return false;
            }

            bool upDownUp = left.Direction == ContainerDirection.Up && middle.Direction == ContainerDirection.Down && right.Direction == ContainerDirection.Up;
            bool downUpDown = left.Direction == ContainerDirection.Down && middle.Direction == ContainerDirection.Up && right.Direction == ContainerDirection.Down;
            if (!upDownUp && !downUpDown)
            {
                rejectReason = "not an alternating triad";
                return false;
            }

            if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
            {
                rejectReason = "containers are not start-ordered";
                return false;
            }

            if (upDownUp && !(left.P1Price < middle.P1Price && middle.P1Price > right.P1Price))
            {
                rejectReason = "up-down-up P1 geometry failed";
                return false;
            }

            if (downUpDown && !(left.P1Price > middle.P1Price && middle.P1Price < right.P1Price))
            {
                rejectReason = "down-up-down P1 geometry failed";
                return false;
            }

            bool oppositeBreakException = left.Status != ContainerStatus.Broken
                && middle.Status == ContainerStatus.Broken
                && right.Status != ContainerStatus.Broken;
            if (oppositeBreakException)
                return true;

            if (IsRootLevelOneDownUpDownJoin(left, middle, right))
                return true;

            return HasResumedBeyondFirstP2(left, right, out rejectReason);
        }

        private bool HasResumedBeyondFirstP2(PriceContainer left, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            if (left.Direction == ContainerDirection.Up)
            {
                if (right.P2Price > left.P2Price + TickSize * 0.5)
                    return true;

                rejectReason = "right up container has not exceeded left up P2";
                return false;
            }

            if (left.Direction == ContainerDirection.Down)
            {
                if (right.P2Price < left.P2Price - TickSize * 0.5)
                    return true;

                rejectReason = "right down container has not exceeded left down P2";
                return false;
            }

            rejectReason = "unknown direction";
            return false;
        }

        private bool CanJoinChainedContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";

            bool upDownUp = left.Direction == ContainerDirection.Up && middle.Direction == ContainerDirection.Down && right.Direction == ContainerDirection.Up;
            bool downUpDown = left.Direction == ContainerDirection.Down && middle.Direction == ContainerDirection.Up && right.Direction == ContainerDirection.Down;
            if (!upDownUp && !downUpDown)
            {
                rejectReason = "not an alternating chained triad";
                return false;
            }

            if (left.Level != right.Level || middle.Level <= left.Level)
            {
                rejectReason = "chain levels are not parent-child-parent";
                return false;
            }

            if (middle.ParentId != left.Id)
            {
                rejectReason = "middle is not a child of the left container";
                return false;
            }

            if (left.StartBar >= middle.StartBar || middle.EndBar > right.StartBar)
            {
                rejectReason = "chained containers are not ordered";
                return false;
            }

            if (upDownUp && !(left.P1Price < middle.P1Price && middle.P1Price > right.P1Price))
            {
                rejectReason = "up-down-up P1 geometry failed";
                return false;
            }

            if (downUpDown && !(left.P1Price > middle.P1Price && middle.P1Price < right.P1Price))
            {
                rejectReason = "down-up-down P1 geometry failed";
                return false;
            }

            if (IsRootLevelOneDownUpDownJoin(left, middle, right))
                return true;

            return HasResumedBeyondFirstP2(left, right, out rejectReason);
        }

        private bool IsRootLevelOneDownUpDownJoin(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            return left != null
                && middle != null
                && right != null
                && left.Level == 1
                && middle.Level == 1
                && right.Level == 1
                && left.ParentId == 0
                && middle.ParentId == 0
                && right.ParentId == 0
                && left.Direction == ContainerDirection.Down
                && middle.Direction == ContainerDirection.Up
                && right.Direction == ContainerDirection.Down;
        }

        private void LogLineageJoinDiagnostics(PriceContainer newest)
        {
            if (!Debug || newest == null || newest.LineageId == 0)
                return;

            var related = new List<PriceContainer>();
            foreach (PriceContainer candidate in containers)
            {
                if (candidate.LineageId != newest.LineageId)
                    continue;
                if (candidate.Direction == ContainerDirection.Unknown)
                    continue;
                if (candidate.Status == ContainerStatus.Broken)
                    continue;

                related.Add(candidate);
            }

            if (related.Count < 3)
                return;

            related.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            for (int i = 0; i <= related.Count - 3; i++)
            {
                PriceContainer left = related[i];
                PriceContainer middle = related[i + 1];
                PriceContainer right = related[i + 2];
                if (left.Id != newest.Id && middle.Id != newest.Id && right.Id != newest.Id)
                    continue;
                if (!IsAlternatingTriad(left, middle, right))
                    continue;
                if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
                    continue;
                if (left.Level == middle.Level && middle.Level == right.Level && left.ParentId == middle.ParentId && middle.ParentId == right.ParentId)
                    continue;

                LogDebug(newest.EndBar, "lineage join candidate: lineage=" + newest.LineageId + " triad=" + left.Id + "," + middle.Id + "," + right.Id + " levels=" + left.Level + "," + middle.Level + "," + right.Level + " parents=" + left.ParentId + "," + middle.ParentId + "," + right.ParentId + " originFtt=" + newest.OriginFttContainerId + "@" + newest.OriginFttBar);
                LogLineageTriadGeometry(newest.EndBar, left, middle, right);
                TryRecordStructuralLineageJoin(newest.EndBar, left, middle, right);
            }
        }

        private void LogLineageTriadGeometry(int bar, PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (!Debug || !IsAlternatingTriad(left, middle, right))
                return;

            string currentReject;
            bool currentResume = HasResumedBeyondFirstP2(left, right, out currentReject);
            string p1Reject;
            bool p1Geometry = HasJoinP1Geometry(left, middle, right, out p1Reject);
            bool middleFtt = HasMiddleFailureToTraverse(left, middle);
            bool oldExtreme = HasTChannelStyleSameDirectionExtreme(left, right);

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            bool joinedP1P3 = IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price);
            bool oldWouldAccept = p1Geometry && middleFtt && oldExtreme && joinedP1P3;
            string activeReject;
            bool activeEligible = CanJoinLineageContainers(left, middle, right, out activeReject);

            LogDebug(bar,
                "lineage triad geometry: triad=" + left.Id + "," + middle.Id + "," + right.Id
                + " pattern=" + DescribeTriadPattern(left, middle, right)
                + " originFtt=" + right.OriginFttContainerId + "@" + right.OriginFttBar
                + " leftP2=" + FormatPrice(left.P2Price)
                + " middleP2=" + FormatPrice(middle.P2Price)
                + " rightP2=" + FormatPrice(right.P2Price)
                + " currentResume=" + FormatPassFail(currentResume)
                + " currentReject=" + (currentResume ? "none" : currentReject)
                + " p1Geometry=" + FormatPassFail(p1Geometry)
                + " p1Reject=" + (p1Geometry ? "none" : p1Reject)
                + " middleFTT=" + FormatPassFail(middleFtt)
                + " oldExtreme=" + FormatPassFail(oldExtreme)
                + " joinedP3=" + p3Bar + "@" + FormatPrice(p3Price)
                + " joinedP1P3=" + FormatPassFail(joinedP1P3)
                + " oldStyleWouldAccept=" + oldWouldAccept
                + " activeLineageEligible=" + FormatPassFail(activeEligible)
                + " activeReject=" + (activeEligible ? "none" : activeReject));
        }

        private void LogLineageTriadGeometryIfRelated(int bar, PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (!Debug || left == null || middle == null || right == null)
                return;
            if (left.LineageId == 0 || left.LineageId != middle.LineageId || middle.LineageId != right.LineageId)
                return;

            LogLineageTriadGeometry(bar, left, middle, right);
        }

        private void LogStructuralContextTriadDiagnostics(PriceContainer newest)
        {
            if (!Debug || newest == null)
                return;

            string contextKey = StructuralContextKey(newest);
            if (string.IsNullOrEmpty(contextKey))
                return;
            int contextSourceId = StructuralContextSourceId(newest);
            if (!IsStructuralContextFrontierMember(newest, contextSourceId))
                return;

            var related = new List<PriceContainer>();
            foreach (PriceContainer candidate in containers)
            {
                if (candidate == null || candidate.Id == newest.Id)
                    continue;
                if (candidate.Direction == ContainerDirection.Unknown || candidate.Status == ContainerStatus.Joined)
                    continue;
                if (candidate.Status != ContainerStatus.Active && candidate.Status != ContainerStatus.Adjusted && candidate.Status != ContainerStatus.Broken)
                    continue;
                if (candidate.StartBar >= newest.StartBar)
                    continue;
                if (StructuralContextKey(candidate) != contextKey)
                    continue;
                if (!IsStructuralContextFrontierMember(candidate, contextSourceId))
                    continue;

                related.Add(candidate);
            }

            related.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            if (related.Count < 2)
                return;

            for (int i = related.Count - 2; i >= 0; i--)
            {
                PriceContainer left = related[i];
                for (int j = related.Count - 1; j > i; j--)
                {
                    PriceContainer middle = related[j];
                    if (!IsAlternatingTriad(left, middle, newest))
                        continue;
                    if (!(left.StartBar < middle.StartBar && middle.StartBar < newest.StartBar))
                        continue;
                    if (left.LineageId == middle.LineageId && middle.LineageId == newest.LineageId)
                        continue;

                    LogStructuralContextTriadGeometry(newest.EndBar, contextKey, left, middle, newest);
                }
            }
        }

        private bool TryJoinStructuralContextTriad(PriceContainer newest, out PriceContainer parent)
        {
            parent = null;
            if (!EnableLineageJoins)
                return false;
            if (newest == null)
                return false;

            string contextKey = StructuralContextKey(newest);
            if (string.IsNullOrEmpty(contextKey))
                return false;
            int contextSourceId = StructuralContextSourceId(newest);
            PriceContainer right = ResolveStructuralContextFrontierContainer(newest, contextSourceId);
            if (right == null || right.Direction == ContainerDirection.Unknown)
                return false;
            if (right.Status != ContainerStatus.Active && right.Status != ContainerStatus.Adjusted)
                return false;
            contextKey = StructuralContextKey(right);
            contextSourceId = StructuralContextSourceId(right);

            var related = new List<PriceContainer>();
            foreach (PriceContainer candidate in containers)
            {
                if (candidate == null || candidate.Id == right.Id)
                    continue;
                if (candidate.Direction == ContainerDirection.Unknown || candidate.Status == ContainerStatus.Joined)
                    continue;
                if (candidate.Status != ContainerStatus.Active && candidate.Status != ContainerStatus.Adjusted && candidate.Status != ContainerStatus.Broken)
                    continue;
                if (candidate.StartBar >= right.StartBar)
                    continue;
                if (StructuralContextKey(candidate) != contextKey)
                    continue;
                if (!IsStructuralContextFrontierMember(candidate, contextSourceId))
                    continue;

                related.Add(candidate);
            }

            related.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            if (related.Count < 2)
                return false;

            PriceContainer bestLeft = null;
            PriceContainer bestMiddle = null;
            string bestReject = "";
            bool bestIsOrdinary = false;

            for (int i = related.Count - 2; i >= 0; i--)
            {
                PriceContainer left = related[i];
                for (int j = related.Count - 1; j > i; j--)
                {
                    PriceContainer middle = related[j];
                    string rejectReason;
                    if (!CanJoinStructuralContextContainers(left, middle, right, out rejectReason))
                    {
                        bestReject = rejectReason;
                        continue;
                    }

                    bool candidateIsOrdinary = IsOrdinarySameLevelParentScopeTriad(left, middle, right);
                    if (bestLeft == null
                        || (bestIsOrdinary && !candidateIsOrdinary)
                        || (bestIsOrdinary == candidateIsOrdinary
                            && (left.StartBar < bestLeft.StartBar
                                || (left.StartBar == bestLeft.StartBar && middle.StartBar < bestMiddle.StartBar))))
                    {
                        bestLeft = left;
                        bestMiddle = middle;
                        bestIsOrdinary = candidateIsOrdinary;
                    }
                }
            }

            if (bestLeft == null || bestMiddle == null)
            {
                if (Debug && !string.IsNullOrEmpty(bestReject))
                    LogDebug(newest.EndBar, "structural context join rejected: newest=" + newest.Id + " frontier=" + right.Id + " reason=" + bestReject);
                return false;
            }

            int parentLevel = Math.Min(bestLeft.Level, Math.Min(bestMiddle.Level, right.Level));
            int parentId = ResolveStructuralContextJoinedParentId(bestLeft, bestMiddle, right, parentLevel);
            List<PriceContainer> absorbedContextContainers = FindInterveningStructuralContextContainers(bestLeft, bestMiddle, right, contextKey, contextSourceId);
            PriceContainer originOverride = ResolveStructuralContextJoinedOrigin(bestLeft, right, contextSourceId, parentId);
            parent = CreateJoinedParent(bestLeft, bestMiddle, right, false, parentLevel, parentId, absorbedContextContainers, originOverride);
            if (parent != null)
                LogDebug(parent.EndBar,
                    "structural context join accepted: parent=" + parent.Id
                    + " children=" + bestLeft.Id + "," + bestMiddle.Id + "," + right.Id
                    + " absorbed=" + JoinContainerIds(absorbedContextContainers)
                    + " trigger=" + newest.Id
                    + " context=" + contextKey
                    + " originOverride=" + (originOverride == null ? "none" : originOverride.Id + "@" + originOverride.StartBar)
                    + " lineage=" + parent.LineageId
                    + " level=" + parent.Level
                    + " parent=" + parent.ParentId);

            return parent != null;
        }

        private PriceContainer ResolveStructuralContextJoinedOrigin(PriceContainer left, PriceContainer right, int contextSourceId, int joinedParentId)
        {
            if (left == null || right == null || contextSourceId == 0)
                return null;

            PriceContainer source = FindContainer(contextSourceId);
            if (source == null)
                return null;
            if (source.Direction != left.Direction)
                return null;
            if (source.P1Bar >= left.P1Bar)
                return null;
            if (joinedParentId != 0 && source.ParentId != joinedParentId && source.Id != joinedParentId)
                return null;
            if (!IsStructuralContextFrontierMember(left, contextSourceId) || !IsStructuralContextFrontierMember(right, contextSourceId))
                return null;

            return source;
        }

        private PriceContainer ResolveStructuralContextFrontierContainer(PriceContainer container, int contextSourceId)
        {
            PriceContainer current = container;
            while (current != null)
            {
                if (IsStructuralContextFrontierMember(current, contextSourceId))
                    return current;
                if (current.ParentId == 0)
                    return null;
                current = FindContainer(current.ParentId);
            }

            return null;
        }

        private bool IsOrdinarySameLevelParentScopeTriad(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            return left != null
                && middle != null
                && right != null
                && left.Level == middle.Level
                && middle.Level == right.Level
                && left.ParentId == middle.ParentId
                && middle.ParentId == right.ParentId;
        }

        private bool CanJoinStructuralContextContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            if (left == null || middle == null || right == null)
            {
                rejectReason = "missing triad member";
                return false;
            }
            if (!IsAlternatingTriad(left, middle, right))
            {
                rejectReason = "not alternating";
                return false;
            }
            if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
            {
                rejectReason = "not start ordered";
                return false;
            }

            string p1Reject;
            if (!HasJoinP1Geometry(left, middle, right, out p1Reject))
            {
                rejectReason = p1Reject;
                return false;
            }
            if (!HasMiddleFailureToTraverse(left, middle))
            {
                rejectReason = "middle FTT failed";
                return false;
            }
            if (!HasTChannelStyleSameDirectionExtreme(left, right))
            {
                rejectReason = "old-style same-direction extreme failed";
                return false;
            }
            if (!HasResumedBeyondFirstP2(left, right, out rejectReason))
                return false;

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            if (!IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price))
            {
                rejectReason = "joined P1/P3 geometry failed";
                return false;
            }

            return true;
        }

        private int ResolveStructuralContextJoinedParentId(PriceContainer left, PriceContainer middle, PriceContainer right, int parentLevel)
        {
            if (left.Level == parentLevel && middle.Level == parentLevel && right.Level == parentLevel
                && left.ParentId == middle.ParentId && middle.ParentId == right.ParentId)
                return left.ParentId;

            if (left.Level == parentLevel && left.ParentId == 0)
                return 0;
            if (middle.Level == parentLevel && middle.ParentId == 0)
                return 0;
            if (right.Level == parentLevel && right.ParentId == 0)
                return 0;

            if (left.Level == parentLevel)
                return left.ParentId;
            if (middle.Level == parentLevel)
                return middle.ParentId;
            return right.ParentId;
        }

        private void LogStructuralContextTriadGeometry(int bar, string contextKey, PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            string currentReject;
            bool currentResume = HasResumedBeyondFirstP2(left, right, out currentReject);
            string p1Reject;
            bool p1Geometry = HasJoinP1Geometry(left, middle, right, out p1Reject);
            bool middleFtt = HasMiddleFailureToTraverse(left, middle);
            bool oldExtreme = HasTChannelStyleSameDirectionExtreme(left, right);

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            bool joinedP1P3 = IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price);
            bool contextReady = currentResume && p1Geometry && middleFtt && oldExtreme && joinedP1P3;

            LogDebug(bar,
                "structural context triad: context=" + contextKey
                + " triad=" + JoinIds(left, middle, right)
                + " pattern=" + DescribeTriadPattern(left, middle, right)
                + " lineages=" + left.LineageId + "," + middle.LineageId + "," + right.LineageId
                + " levels=" + left.Level + "," + middle.Level + "," + right.Level
                + " parents=" + left.ParentId + "," + middle.ParentId + "," + right.ParentId
                + " statuses=" + left.Status + "," + middle.Status + "," + right.Status
                + " psrc=" + left.InheritedParentStructuralSourceId + "," + middle.InheritedParentStructuralSourceId + "," + right.InheritedParentStructuralSourceId
                + " currentResume=" + FormatPassFail(currentResume)
                + " currentReject=" + (currentResume ? "none" : currentReject)
                + " p1Geometry=" + FormatPassFail(p1Geometry)
                + " p1Reject=" + (p1Geometry ? "none" : p1Reject)
                + " middleFTT=" + FormatPassFail(middleFtt)
                + " oldExtreme=" + FormatPassFail(oldExtreme)
                + " joinedP3=" + p3Bar + "@" + FormatPrice(p3Price)
                + " joinedP1P3=" + FormatPassFail(joinedP1P3)
                + " contextReady=" + FormatPassFail(contextReady));
        }

        private string StructuralContextKey(PriceContainer container)
        {
            if (container == null)
                return "";
            if (container.InheritedParentStructuralLineageJoinId != 0 && container.InheritedParentStructuralSourceId != 0)
                return container.InheritedParentStructuralLineageJoinId.ToString(CultureInfo.InvariantCulture)
                    + "@" + container.InheritedParentStructuralSourceId.ToString(CultureInfo.InvariantCulture);
            if (container.StructuralLineageJoinId != 0)
                return container.StructuralLineageJoinId.ToString(CultureInfo.InvariantCulture)
                    + "@" + container.Id.ToString(CultureInfo.InvariantCulture);
            return "";
        }

        private int StructuralContextSourceId(PriceContainer container)
        {
            if (container == null)
                return 0;
            if (container.InheritedParentStructuralLineageJoinId != 0 && container.InheritedParentStructuralSourceId != 0)
                return container.InheritedParentStructuralSourceId;
            if (container.StructuralLineageJoinId != 0)
                return container.Id;
            return 0;
        }

        private bool IsStructuralContextFrontierMember(PriceContainer container, int contextSourceId)
        {
            if (container == null)
                return false;
            if (contextSourceId == 0)
                return false;
            if (container.ParentId == 0)
                return true;
            if (container.ParentId == contextSourceId)
                return true;
            return false;
        }

        private bool CanJoinLineageContainers(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            string rejectReason;
            return CanJoinLineageContainers(left, middle, right, out rejectReason);
        }

        private bool CanJoinLineageContainers(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            if (left == null || middle == null || right == null)
            {
                rejectReason = "missing triad member";
                return false;
            }
            if (left.LineageId == 0 || left.LineageId != middle.LineageId || middle.LineageId != right.LineageId)
            {
                rejectReason = "lineage differs";
                return false;
            }
            if (!IsAlternatingTriad(left, middle, right))
            {
                rejectReason = "not alternating";
                return false;
            }
            if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
            {
                rejectReason = "not start ordered";
                return false;
            }
            if (left.Level == middle.Level && middle.Level == right.Level && left.ParentId == middle.ParentId && middle.ParentId == right.ParentId)
            {
                rejectReason = "ordinary same-level parent-scope join";
                return false;
            }

            string p1Reject;
            if (!HasJoinP1Geometry(left, middle, right, out p1Reject))
            {
                rejectReason = p1Reject;
                return false;
            }
            if (!HasMiddleFailureToTraverse(left, middle))
            {
                rejectReason = "middle FTT failed";
                return false;
            }
            if (!HasTChannelStyleSameDirectionExtreme(left, right))
            {
                rejectReason = "old-style same-direction extreme failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            if (!IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price))
            {
                rejectReason = "joined P1/P3 geometry failed";
                return false;
            }

            if (!IsActiveLineageJoinMember(left, out rejectReason)
                || !IsActiveLineageJoinMember(middle, out rejectReason)
                || !IsActiveLineageJoinMember(right, out rejectReason))
                return false;

            return true;
        }

        private bool TryRecordStructuralLineageJoin(int bar, PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            string rejectReason;
            if (!CanRecordStructuralLineageJoin(left, middle, right, out rejectReason))
            {
                LogDebug(bar, "structural lineage join rejected: triad=" + JoinIds(left, middle, right) + " reason=" + rejectReason);
                return false;
            }

            StructuralLineageJoin existing = FindStructuralLineageJoin(left.Id, middle.Id, right.Id);
            if (existing != null)
                return false;

            StructuralLineageJoin superseded = FindStructuralLineageJoinForLeftMiddle(left.LineageId, left.Id, middle.Id, left.Direction);

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);

            if (superseded != null)
            {
                int previousRightId = superseded.RightId;
                if (!superseded.SupersededRightIds.Contains(previousRightId))
                    superseded.SupersededRightIds.Add(previousRightId);
                superseded.RightId = right.Id;
                superseded.EndBar = right.EndBar;
                superseded.P3Bar = p3Bar;
                superseded.P3Price = p3Price;
                superseded.OriginFttContainerId = right.OriginFttContainerId;
                superseded.OriginFttBar = right.OriginFttBar;
                superseded.OriginFttPrice = right.OriginFttPrice;

                LogDebug(bar,
                    "structural lineage join updated: id=" + superseded.Id
                    + " lineage=" + superseded.LineageId
                    + " previousRight=" + previousRightId
                    + " triad=" + superseded.LeftId + "," + superseded.MiddleId + "," + superseded.RightId
                    + " statuses=" + left.Status + "," + middle.Status + "," + right.Status
                    + " p3=" + superseded.P3Bar + "@" + FormatPrice(superseded.P3Price)
                    + " visualMutation=False");

                return true;
            }

            var join = new StructuralLineageJoin
            {
                Id = nextStructuralLineageJoinId++,
                LineageId = left.LineageId,
                LeftId = left.Id,
                MiddleId = middle.Id,
                RightId = right.Id,
                OriginFttContainerId = right.OriginFttContainerId,
                OriginFttBar = right.OriginFttBar,
                OriginFttPrice = right.OriginFttPrice,
                Direction = left.Direction,
                StartBar = left.P1Bar,
                EndBar = right.EndBar,
                P1Bar = left.P1Bar,
                P1Price = left.P1Price,
                P2Bar = left.P2Bar,
                P2Price = left.P2Price,
                P3Bar = p3Bar,
                P3Price = p3Price
            };

            structuralLineageJoins.Add(join);
            LogDebug(bar,
                "structural lineage join recorded: id=" + join.Id
                + " lineage=" + join.LineageId
                + " triad=" + join.LeftId + "," + join.MiddleId + "," + join.RightId
                + " statuses=" + left.Status + "," + middle.Status + "," + right.Status
                + " levels=" + left.Level + "," + middle.Level + "," + right.Level
                + " parents=" + left.ParentId + "," + middle.ParentId + "," + right.ParentId
                + " originFtt=" + join.OriginFttContainerId + "@" + join.OriginFttBar
                + " p1=" + join.P1Bar + "@" + FormatPrice(join.P1Price)
                + " p2=" + join.P2Bar + "@" + FormatPrice(join.P2Price)
                + " p3=" + join.P3Bar + "@" + FormatPrice(join.P3Price)
                + " visualMutation=False");

            return true;
        }

        private bool CanRecordStructuralLineageJoin(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            if (left == null || middle == null || right == null)
            {
                rejectReason = "missing triad member";
                return false;
            }
            if (left.LineageId == 0 || left.LineageId != middle.LineageId || middle.LineageId != right.LineageId)
            {
                rejectReason = "lineage differs";
                return false;
            }
            if (!IsAlternatingTriad(left, middle, right))
            {
                rejectReason = "not alternating";
                return false;
            }
            if (!(left.StartBar < middle.StartBar && middle.StartBar < right.StartBar))
            {
                rejectReason = "not start ordered";
                return false;
            }
            if (left.Level == middle.Level && middle.Level == right.Level && left.ParentId == middle.ParentId && middle.ParentId == right.ParentId)
            {
                rejectReason = "ordinary same-level parent-scope join";
                return false;
            }

            string visualReject;
            if (CanJoinLineageContainers(left, middle, right, out visualReject))
            {
                rejectReason = "visual lineage join already eligible";
                return false;
            }
            if (!IsJoinedComponentVisualReject(visualReject))
            {
                rejectReason = "visual reject is not structural-only: " + visualReject;
                return false;
            }

            if (!IsStructuralLineageJoinMember(left, out rejectReason)
                || !IsStructuralLineageJoinMember(middle, out rejectReason)
                || !IsStructuralLineageJoinMember(right, out rejectReason))
                return false;

            string p1Reject;
            if (!HasJoinP1Geometry(left, middle, right, out p1Reject))
            {
                rejectReason = p1Reject;
                return false;
            }
            if (!HasMiddleFailureToTraverse(left, middle))
            {
                rejectReason = "middle FTT failed";
                return false;
            }
            if (!HasTChannelStyleSameDirectionExtreme(left, right))
            {
                rejectReason = "old-style same-direction extreme failed";
                return false;
            }

            int p3Bar;
            double p3Price;
            FindJoinedParentP3(left.Direction, left, middle, right, new List<PriceContainer>(), out p3Bar, out p3Price);
            if (!IsValidP1P3Geometry(left.Direction, left.P1Price, p3Price))
            {
                rejectReason = "joined P1/P3 geometry failed";
                return false;
            }

            return true;
        }

        private bool IsJoinedComponentVisualReject(string rejectReason)
        {
            return rejectReason != null
                && rejectReason.IndexOf("status=Joined", StringComparison.Ordinal) >= 0;
        }

        private bool IsStructuralLineageJoinMember(PriceContainer container, out string rejectReason)
        {
            rejectReason = "";
            if (container == null)
            {
                rejectReason = "missing structural member";
                return false;
            }
            if (container.Direction == ContainerDirection.Unknown)
            {
                rejectReason = "structural member direction unknown id=" + container.Id;
                return false;
            }
            if (container.Status != ContainerStatus.Active
                && container.Status != ContainerStatus.Adjusted
                && container.Status != ContainerStatus.Broken
                && container.Status != ContainerStatus.Joined)
            {
                rejectReason = "structural member inactive id=" + container.Id + " status=" + container.Status;
                return false;
            }

            return true;
        }

        private StructuralLineageJoin FindStructuralLineageJoin(int leftId, int middleId, int rightId)
        {
            foreach (StructuralLineageJoin join in structuralLineageJoins)
            {
                if (join.LeftId == leftId && join.MiddleId == middleId && join.RightId == rightId)
                    return join;
            }

            return null;
        }

        private StructuralLineageJoin FindStructuralLineageJoinForLeftMiddle(int lineageId, int leftId, int middleId, ContainerDirection direction)
        {
            foreach (StructuralLineageJoin join in structuralLineageJoins)
            {
                if (join.LineageId == lineageId && join.LeftId == leftId && join.MiddleId == middleId && join.Direction == direction)
                    return join;
            }

            return null;
        }

        private string JoinIds(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            return (left == null ? "null" : left.Id.ToString(CultureInfo.InvariantCulture))
                + "," + (middle == null ? "null" : middle.Id.ToString(CultureInfo.InvariantCulture))
                + "," + (right == null ? "null" : right.Id.ToString(CultureInfo.InvariantCulture));
        }

        private bool IsActiveLineageJoinMember(PriceContainer container, out string rejectReason)
        {
            rejectReason = "";
            if (container == null)
            {
                rejectReason = "missing active member";
                return false;
            }
            if (container.Direction == ContainerDirection.Unknown || container.Status == ContainerStatus.Joined)
            {
                rejectReason = "member not joinable id=" + container.Id + " status=" + container.Status;
                return false;
            }
            if (container.Status != ContainerStatus.Active && container.Status != ContainerStatus.Adjusted && container.Status != ContainerStatus.Broken)
            {
                rejectReason = "member inactive id=" + container.Id + " status=" + container.Status;
                return false;
            }
            if (!HasLiveParentScope(container))
            {
                rejectReason = "member has inactive parent scope id=" + container.Id + " parent=" + container.ParentId;
                return false;
            }

            return true;
        }

        private bool HasJoinP1Geometry(PriceContainer left, PriceContainer middle, PriceContainer right, out string rejectReason)
        {
            rejectReason = "";
            bool upDownUp = left.Direction == ContainerDirection.Up && middle.Direction == ContainerDirection.Down && right.Direction == ContainerDirection.Up;
            bool downUpDown = left.Direction == ContainerDirection.Down && middle.Direction == ContainerDirection.Up && right.Direction == ContainerDirection.Down;
            if (!upDownUp && !downUpDown)
            {
                rejectReason = "not alternating";
                return false;
            }

            if (upDownUp && !(left.P1Price < middle.P1Price && middle.P1Price > right.P1Price))
            {
                rejectReason = "up-down-up P1 geometry failed";
                return false;
            }

            if (downUpDown && !(left.P1Price > middle.P1Price && middle.P1Price < right.P1Price))
            {
                rejectReason = "down-up-down P1 geometry failed";
                return false;
            }

            return true;
        }

        private bool HasMiddleFailureToTraverse(PriceContainer left, PriceContainer middle)
        {
            if (left.Direction == ContainerDirection.Down && middle.Direction == ContainerDirection.Up)
                return middle.P2Price < left.P1Price - TickSize * 0.5;
            if (left.Direction == ContainerDirection.Up && middle.Direction == ContainerDirection.Down)
                return middle.P2Price > left.P1Price + TickSize * 0.5;

            return false;
        }

        private bool HasTChannelStyleSameDirectionExtreme(PriceContainer left, PriceContainer right)
        {
            if (left.Direction == ContainerDirection.Down)
                return right.P2Price - left.P2Price <= TickSize * 0.5;
            if (left.Direction == ContainerDirection.Up)
                return left.P2Price - right.P2Price <= TickSize * 0.5;

            return false;
        }

        private string DescribeTriadPattern(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            return left.Direction + "-" + middle.Direction + "-" + right.Direction;
        }

        private string FormatPassFail(bool value)
        {
            return value ? "pass" : "fail";
        }

        private bool IsAlternatingTriad(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left == null || middle == null || right == null)
                return false;

            bool upDownUp = left.Direction == ContainerDirection.Up
                && middle.Direction == ContainerDirection.Down
                && right.Direction == ContainerDirection.Up;
            bool downUpDown = left.Direction == ContainerDirection.Down
                && middle.Direction == ContainerDirection.Up
                && right.Direction == ContainerDirection.Down;

            return upDownUp || downUpDown;
        }

        private int ResolveJoinedLineageId(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left.LineageId != 0 && left.LineageId == middle.LineageId && middle.LineageId == right.LineageId)
                return left.LineageId;
            if (left.LineageId != 0 && left.LineageId == right.LineageId)
                return left.LineageId;
            if (middle.LineageId != 0)
                return middle.LineageId;
            if (left.LineageId != 0)
                return left.LineageId;
            if (right.LineageId != 0)
                return right.LineageId;

            return nextLineageId++;
        }

        private int ResolveJoinedStructuralParentId(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left.StructuralParentId != 0)
                return left.StructuralParentId;
            if (middle.StructuralParentId != 0)
                return middle.StructuralParentId;
            return right.StructuralParentId;
        }

        private int ResolveJoinedOriginFttContainerId(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left.OriginFttContainerId != 0)
                return left.OriginFttContainerId;
            if (middle.OriginFttContainerId != 0)
                return middle.OriginFttContainerId;
            return right.OriginFttContainerId;
        }

        private int ResolveJoinedOriginFttBar(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left.OriginFttBar != 0)
                return left.OriginFttBar;
            if (middle.OriginFttBar != 0)
                return middle.OriginFttBar;
            return right.OriginFttBar;
        }

        private double ResolveJoinedOriginFttPrice(PriceContainer left, PriceContainer middle, PriceContainer right)
        {
            if (left.OriginFttPrice != 0.0)
                return left.OriginFttPrice;
            if (middle.OriginFttPrice != 0.0)
                return middle.OriginFttPrice;
            return right.OriginFttPrice;
        }

        private void PropagateStructuralLineageContextToJoinedParent(PriceContainer parent, List<PriceContainer> directChildren, int bar)
        {
            if (parent == null || directChildren == null)
                return;

            PriceContainer source = null;
            PriceContainer parentSource = null;
            foreach (PriceContainer child in directChildren)
            {
                if (child == null)
                    continue;
                if (child.StructuralLineageJoinId != 0 && (source == null || child.StartBar < source.StartBar))
                    source = child;
                if (IsBetterInheritedParentStructuralSource(parent, child, parentSource))
                    parentSource = child;
            }

            if (source != null)
            {
                parent.StructuralLineageJoinId = source.StructuralLineageJoinId;
                parent.StructuralLineageContextRole = "joinedParent";
                parent.StructuralLineageContextSourceId = source.Id;

                LogDebug(bar,
                    "structural context propagated to joined parent: parent=" + parent.Id
                    + " structuralJoin=" + parent.StructuralLineageJoinId
                    + " sourceChild=" + source.Id
                    + " sourceRole=" + source.StructuralLineageContextRole
                    + " lineage=" + parent.LineageId);
            }

            if (parentSource != null && source == null)
            {
                parent.InheritedParentStructuralLineageJoinId = parentSource.InheritedParentStructuralLineageJoinId;
                parent.InheritedParentStructuralSourceId = parentSource.InheritedParentStructuralSourceId;
                parent.InheritedParentStructuralRole = parentSource.InheritedParentStructuralRole;

                LogDebug(bar,
                    "inherited parent context propagated to joined parent: parent=" + parent.Id
                    + " parentStructuralJoin=" + parent.InheritedParentStructuralLineageJoinId
                    + " sourceChild=" + parentSource.Id
                    + " sourceParent=" + parent.InheritedParentStructuralSourceId
                    + " sourceRole=" + parent.InheritedParentStructuralRole
                    + " lineage=" + parent.LineageId);
            }
        }

        private bool IsBetterInheritedParentStructuralSource(PriceContainer joinedParent, PriceContainer candidate, PriceContainer selected)
        {
            if (candidate == null || candidate.InheritedParentStructuralLineageJoinId == 0)
                return false;
            if (selected == null)
                return true;

            int visualParentId = joinedParent != null ? joinedParent.ParentId : 0;
            if (visualParentId != 0)
            {
                bool candidateMatchesVisualParent = candidate.InheritedParentStructuralSourceId == visualParentId;
                bool selectedMatchesVisualParent = selected.InheritedParentStructuralSourceId == visualParentId;
                if (candidateMatchesVisualParent != selectedMatchesVisualParent)
                    return candidateMatchesVisualParent;

                bool candidateHasVisualStructuralParent = candidate.StructuralParentId == visualParentId;
                bool selectedHasVisualStructuralParent = selected.StructuralParentId == visualParentId;
                if (candidateHasVisualStructuralParent != selectedHasVisualStructuralParent)
                    return candidateHasVisualStructuralParent;
            }

            if (candidate.StartBar != selected.StartBar)
                return candidate.StartBar < selected.StartBar;
            return candidate.Id < selected.Id;
        }

        private PriceContainer CreateJoinedParent(PriceContainer left, PriceContainer middle, PriceContainer right, bool preserveMiddleAsNestedChild)
        {
            return CreateJoinedParent(left, middle, right, preserveMiddleAsNestedChild, left.Level, left.ParentId);
        }

        private PriceContainer CreateJoinedParent(PriceContainer left, PriceContainer middle, PriceContainer right, bool preserveMiddleAsNestedChild, int parentLevel, int parentId)
        {
            return CreateJoinedParent(left, middle, right, preserveMiddleAsNestedChild, parentLevel, parentId, null);
        }

        private PriceContainer CreateJoinedParent(PriceContainer left, PriceContainer middle, PriceContainer right, bool preserveMiddleAsNestedChild, int parentLevel, int parentId, List<PriceContainer> additionalDirectChildren)
        {
            return CreateJoinedParent(left, middle, right, preserveMiddleAsNestedChild, parentLevel, parentId, additionalDirectChildren, null);
        }

        private PriceContainer CreateJoinedParent(PriceContainer left, PriceContainer middle, PriceContainer right, bool preserveMiddleAsNestedChild, int parentLevel, int parentId, List<PriceContainer> additionalDirectChildren, PriceContainer originOverride)
        {
            ContainerDirection direction = left.Direction;
            PriceContainer owningParent = FindContainer(parentId);
            bool oppositeBreakException = left.Status != ContainerStatus.Broken
                && middle.Status == ContainerStatus.Broken
                && right.Status != ContainerStatus.Broken;
            if (owningParent != null && owningParent.Direction == direction && !oppositeBreakException)
            {
                LogDebug(right.EndBar,
                    "join rejected: same-direction joined child would be created under parent=" + owningParent.Id
                    + " direction=" + direction
                    + " children=" + left.Id + "," + middle.Id + "," + right.Id);
                return null;
            }

            int childLevel = parentLevel + 1;
            PriceContainer origin = originOverride != null ? originOverride : left;
            int startBar = origin.P1Bar;
            int endBar = right.EndBar;
            double p1Price = origin.P1Price;
            int p3Bar;
            double p3Price;
            int p2Bar;
            double p2Price;

            List<PriceContainer> interveningRootContainers = FindInterveningRootContainers(left, right);
            AddUniqueContainers(interveningRootContainers, additionalDirectChildren);
            FindJoinedParentP3(direction, startBar, p1Price, left, middle, right, interveningRootContainers, out p3Bar, out p3Price);
            p2Bar = left.P2Bar;
            p2Price = left.P2Price;

            if (!IsValidP1P3Geometry(direction, p1Price, p3Price))
            {
                LogDebug(endBar, "join rejected: invalid " + direction + " P1/P3 geometry children=" + left.Id + "," + middle.Id + "," + right.Id + " p1=" + FormatPrice(p1Price) + " p3=" + FormatPrice(p3Price));
                return null;
            }

            double slope = (p3Price - p1Price) / Math.Max(1, p3Bar - startBar);

            var parent = new PriceContainer
            {
                Id = nextContainerId++,
                ParentId = parentId,
                Level = parentLevel,
                Direction = direction,
                Status = ContainerStatus.Active,
                StartBar = startBar,
                EndBar = endBar,
                P1Bar = startBar,
                P1Price = p1Price,
                P2Bar = p2Bar,
                P2Price = p2Price,
                P3Bar = p3Bar,
                P3Price = p3Price,
                Rtl = new PriceLine { StartBar = startBar, StartPrice = p1Price, Slope = slope },
                // For joined containers, the LTL begins at point 2 of the first same-direction component.
                // PVA does not draw the joined LTL from P1.
                Ltl = new PriceLine { StartBar = p2Bar, StartPrice = p2Price, Slope = slope },
                Reason = "joined triad " + left.Id + "," + middle.Id + "," + right.Id,
                LineageId = ResolveJoinedLineageId(left, middle, right),
                StructuralParentId = ResolveJoinedStructuralParentId(left, middle, right),
                OriginFttContainerId = ResolveJoinedOriginFttContainerId(left, middle, right),
                OriginFttBar = ResolveJoinedOriginFttBar(left, middle, right),
                OriginFttPrice = ResolveJoinedOriginFttPrice(left, middle, right),
                FttCandidateBar = p2Bar,
                FttCandidatePrice = p2Price,
                Role = StructuralRole.Component
            };

            var directChildren = new List<PriceContainer> { left, right };
            if (!preserveMiddleAsNestedChild)
                directChildren.Insert(1, middle);
            if (originOverride != null && !directChildren.Contains(originOverride))
            {
                for (int i = directChildren.Count - 1; i >= 0; i--)
                {
                    PriceContainer child = directChildren[i];
                    if (child != null && IsDescendantOf(child, originOverride.Id))
                        directChildren.RemoveAt(i);
                }
                directChildren.Add(originOverride);
            }

            foreach (PriceContainer child in interveningRootContainers)
            {
                if (originOverride != null && child != null && IsDescendantOf(child, originOverride.Id))
                    continue;
                if (!directChildren.Contains(child))
                    directChildren.Add(child);
            }
            directChildren.Sort((a, b) =>
            {
                int startCompare = a.StartBar.CompareTo(b.StartBar);
                if (startCompare != 0)
                    return startCompare;
                return a.Id.CompareTo(b.Id);
            });

            containers.Add(parent);
            RebuildOuterExpansion(parent, endBar);
            AttachJoinedParentToExistingParent(parent);
            foreach (PriceContainer child in directChildren)
            {
                bool remainsBroken = child.Status == ContainerStatus.Broken;
                DetachFromExistingParent(child);
                child.ParentId = parent.Id;
                if (!remainsBroken)
                    child.Status = ContainerStatus.Joined;
                if (!parent.ChildIds.Contains(child.Id))
                    parent.ChildIds.Add(child.Id);
                DemoteRecursively(child, childLevel);
            }

            PropagateStructuralLineageContextToJoinedParent(parent, directChildren, endBar);
            LogDebug(endBar, "join accepted: parent=" + parent.Id + " children=" + left.Id + "," + middle.Id + "," + right.Id + " lineage=" + parent.LineageId);
            return parent;
        }

        private void AttachJoinedParentToExistingParent(PriceContainer parent)
        {
            if (parent == null || parent.ParentId == 0)
                return;

            PriceContainer owner = FindContainer(parent.ParentId);
            if (owner == null)
                return;
            if (owner.Status == ContainerStatus.Broken || !HasLiveParentScope(owner))
                return;

            if (!owner.ChildIds.Contains(parent.Id))
                owner.ChildIds.Add(parent.Id);
        }

        private List<PriceContainer> FindInterveningRootContainers(PriceContainer left, PriceContainer right)
        {
            var result = new List<PriceContainer>();
            foreach (PriceContainer candidate in containers)
            {
                if (candidate.Id == left.Id || candidate.Id == right.Id)
                    continue;
                if (candidate.ParentId != left.ParentId)
                    continue;
                if (candidate.Level != left.Level)
                    continue;
                if (candidate.StartBar < left.StartBar || candidate.EndBar > right.EndBar)
                    continue;
                if (candidate.Status == ContainerStatus.Joined)
                    continue;

                result.Add(candidate);
            }

            result.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
            return result;
        }

        private List<PriceContainer> FindInterveningStructuralContextContainers(PriceContainer left, PriceContainer middle, PriceContainer right, string contextKey, int contextSourceId)
        {
            var result = new List<PriceContainer>();
            if (left == null || middle == null || right == null || string.IsNullOrEmpty(contextKey) || contextSourceId == 0)
                return result;

            foreach (PriceContainer candidate in containers)
            {
                if (candidate == null)
                    continue;
                if (candidate.Id == left.Id || candidate.Id == middle.Id || candidate.Id == right.Id)
                    continue;
                if (candidate.Direction == ContainerDirection.Unknown || candidate.Status == ContainerStatus.Joined)
                    continue;
                if (candidate.Status != ContainerStatus.Active && candidate.Status != ContainerStatus.Adjusted && candidate.Status != ContainerStatus.Broken)
                    continue;
                if (candidate.StartBar < left.StartBar || candidate.EndBar > right.EndBar)
                    continue;
                if (StructuralContextKey(candidate) != contextKey)
                    continue;
                if (!IsStructuralContextFrontierMember(candidate, contextSourceId))
                    continue;

                result.Add(candidate);
            }

            result.Sort((a, b) =>
            {
                int startCompare = a.StartBar.CompareTo(b.StartBar);
                if (startCompare != 0)
                    return startCompare;
                return a.Id.CompareTo(b.Id);
            });
            return result;
        }

        private void AddUniqueContainers(List<PriceContainer> target, List<PriceContainer> source)
        {
            if (target == null || source == null)
                return;

            foreach (PriceContainer container in source)
            {
                if (container == null || target.Contains(container))
                    continue;
                AddUniqueContainer(target, container);
            }
        }

        private void AddUniqueContainer(List<PriceContainer> target, PriceContainer container)
        {
            if (target == null || container == null || target.Contains(container))
                return;
            target.Add(container);
        }

        private string JoinContainerIds(List<PriceContainer> source)
        {
            if (source == null || source.Count == 0)
                return "none";

            var ids = new List<string>();
            foreach (PriceContainer container in source)
            {
                if (container != null)
                    ids.Add(container.Id.ToString(CultureInfo.InvariantCulture));
            }

            return ids.Count == 0 ? "none" : string.Join(",", ids.ToArray());
        }

        private void DetachFromExistingParent(PriceContainer child)
        {
            if (child == null || child.ParentId == 0)
                return;

            PriceContainer oldParent = FindContainer(child.ParentId);
            if (oldParent != null)
                oldParent.ChildIds.Remove(child.Id);
        }

        private void FindJoinedParentP3(ContainerDirection direction, PriceContainer left, PriceContainer middle, PriceContainer right, List<PriceContainer> interveningRootContainers, out int p3Bar, out double p3Price)
        {
            FindJoinedParentP3(direction, left.P1Bar, left.P1Price, left, middle, right, interveningRootContainers, out p3Bar, out p3Price);
        }

        private void FindJoinedParentP3(ContainerDirection direction, int p1Bar, double p1Price, PriceContainer left, PriceContainer middle, PriceContainer right, List<PriceContainer> interveningRootContainers, out int p3Bar, out double p3Price)
        {
            p3Bar = middle.P2Bar;
            p3Price = middle.P2Price;
            bool foundOppositeExtreme = false;

            FindOppositeExtremeForP3(direction, middle, ref p3Bar, ref p3Price, ref foundOppositeExtreme);
            foreach (PriceContainer candidate in interveningRootContainers)
            {
                if (candidate.Direction != direction && candidate.Direction != ContainerDirection.Unknown)
                    FindOppositeExtremeForP3(direction, candidate, ref p3Bar, ref p3Price, ref foundOppositeExtreme);
            }

            if (!foundOppositeExtreme)
            {
                p3Bar = right.P1Bar;
                p3Price = right.P1Price;
            }

            AdjustJoinedParentP3ForRtlViolations(direction, p1Bar, p1Price, p3Bar, p3Price, right.EndBar, out p3Bar, out p3Price);
        }

        private void FindOppositeExtremeForP3(ContainerDirection parentDirection, PriceContainer opposite, ref int p3Bar, ref double p3Price, ref bool found)
        {
            for (int bar = opposite.StartBar; bar <= opposite.EndBar; bar++)
            {
                double candidate = parentDirection == ContainerDirection.Up ? Low.GetValueAt(bar) : High.GetValueAt(bar);
                if (!found
                    || (parentDirection == ContainerDirection.Up && candidate < p3Price)
                    || (parentDirection == ContainerDirection.Down && candidate > p3Price))
                {
                    p3Bar = bar;
                    p3Price = candidate;
                    found = true;
                }
            }
        }

        private void AdjustJoinedParentP3ForRtlViolations(ContainerDirection direction, int p1Bar, double p1Price, int candidateP3Bar, double candidateP3Price, int endBar, out int p3Bar, out double p3Price)
        {
            p3Bar = candidateP3Bar;
            p3Price = candidateP3Price;
            double selectedSlope = (p3Price - p1Price) / Math.Max(1, p3Bar - p1Bar);

            for (int bar = p3Bar + 1; bar <= endBar; bar++)
            {
                double candidatePrice = direction == ContainerDirection.Up ? Low.GetValueAt(bar) : High.GetValueAt(bar);
                double projected = p1Price + selectedSlope * (bar - p1Bar);
                bool violates = direction == ContainerDirection.Up
                    ? candidatePrice < projected - TickSize * 0.5
                    : candidatePrice > projected + TickSize * 0.5;

                if (!violates)
                    continue;

                double candidateSlope = (candidatePrice - p1Price) / Math.Max(1, bar - p1Bar);
                if ((direction == ContainerDirection.Up && candidateSlope <= selectedSlope)
                    || (direction == ContainerDirection.Down && candidateSlope >= selectedSlope))
                {
                    p3Bar = bar;
                    p3Price = candidatePrice;
                    selectedSlope = candidateSlope;
                }
            }
        }

        private void DemoteRecursively(PriceContainer container, int level)
        {
            if (container == null)
                return;

            if (container.Level != level)
            {
                container.Level = level;
                LogDebug(container.EndBar, "demotion/style recalculation: id=" + container.Id + " level=" + level);
            }

            foreach (int childId in container.ChildIds)
            {
                PriceContainer child = FindContainer(childId);
                if (child != null)
                    DemoteRecursively(child, level + 1);
            }
        }

        private void RebuildRenderSegments()
        {
            renderSegments.Clear();
            foreach (PriceContainer container in containers)
            {
                if (container.Rtl == null || container.Ltl == null)
                    continue;

                foreach (RenderSegment frozen in container.FrozenSegments)
                {
                    renderSegments.Add(new RenderSegment
                    {
                        ContainerId = frozen.ContainerId,
                        Level = container.Level,
                        Direction = frozen.Direction,
                        Kind = frozen.Kind,
                        StartBar = frozen.StartBar,
                        EndBar = frozen.EndBar,
                        StartPrice = frozen.StartPrice,
                        EndPrice = frozen.EndPrice
                    });
                }

                renderSegments.Add(new RenderSegment
                {
                    ContainerId = container.Id,
                    Level = container.Level,
                    Direction = container.Direction,
                    Kind = RenderLineKind.Rtl,
                    StartBar = container.Rtl.StartBar,
                    EndBar = container.EndBar,
                    StartPrice = container.Rtl.StartPrice,
                    EndPrice = container.Rtl.ValueAt(container.EndBar)
                });

                if (container.ActiveVe == null && container.Ltl.StartBar <= container.EndBar)
                {
                    renderSegments.Add(new RenderSegment
                    {
                        ContainerId = container.Id,
                        Level = container.Level,
                        Direction = container.Direction,
                        Kind = RenderLineKind.Ltl,
                        StartBar = container.Ltl.StartBar,
                        EndBar = container.EndBar,
                        StartPrice = container.Ltl.StartPrice,
                        EndPrice = container.Ltl.ValueAt(container.EndBar)
                    });
                }

                if (container.ActiveVe != null && container.ActiveVe.StartBar <= container.EndBar)
                {
                    renderSegments.Add(new RenderSegment
                    {
                        ContainerId = container.Id,
                        Level = container.Level,
                        Direction = container.Direction,
                        Kind = RenderLineKind.Ve,
                        StartBar = container.ActiveVe.StartBar,
                        EndBar = container.EndBar,
                        StartPrice = container.ActiveVe.StartPrice,
                        EndPrice = container.ActiveVe.ValueAt(container.EndBar)
                    });
                }
            }
        }

        private void ValidateHierarchy()
        {
            foreach (PriceContainer container in containers)
            {
                if (container.ParentId != 0 && FindContainer(container.ParentId) == null)
                    LogDebug(container.EndBar, "hierarchy warning: parent missing for id=" + container.Id);

                foreach (int childId in container.ChildIds)
                {
                    PriceContainer child = FindContainer(childId);
                    if (child == null)
                    {
                        LogDebug(container.EndBar, "hierarchy warning: child missing id=" + childId + " parent=" + container.Id);
                        continue;
                    }

                    if (child.ParentId != container.Id)
                        LogDebug(container.EndBar, "hierarchy warning: child reciprocal parent mismatch child=" + child.Id + " parent=" + container.Id);

                    if (child.Level <= container.Level)
                        LogDebug(container.EndBar, "hierarchy warning: child level not demoted child=" + child.Id + " parent=" + container.Id);
                }
            }
        }

        private void MaybeExportJson(int start, int end)
        {
            if (!ExportJson)
                return;

            if (!ExportOnEveryBar)
            {
                bool debugBoundReached = Debug && EndBar > 0 && CurrentBar >= end;
                bool historicalComplete = State != State.Historical || CurrentBar >= Count - 1;
                if (!debugBoundReached && !historicalComplete)
                    return;
            }

            string signature = start.ToString(CultureInfo.InvariantCulture)
                + ":" + end.ToString(CultureInfo.InvariantCulture)
                + ":" + containers.Count.ToString(CultureInfo.InvariantCulture)
                + ":" + analyzedBars.Count.ToString(CultureInfo.InvariantCulture)
                + ":" + ExportOnEveryBar.ToString(CultureInfo.InvariantCulture);
            if (!ExportOnEveryBar && signature == lastJsonExportSignature)
                return;

            try
            {
                List<ExportWarning> warnings = BuildExportWarnings();
                string folder = string.IsNullOrWhiteSpace(JsonExportFolder)
                    ? @"C:\Users\rz0\Documents\ApvaAnalysis\ContainerJSON"
                    : JsonExportFolder;
                Directory.CreateDirectory(folder);

                string fileName = string.IsNullOrWhiteSpace(JsonFileName)
                    ? BuildDefaultJsonFileName(start, end)
                    : JsonFileName;
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";

                string path = Path.Combine(folder, SanitizeFileName(fileName));
                File.WriteAllText(path, BuildContainerJson(start, end, warnings), Encoding.UTF8);
                lastJsonExportSignature = signature;

                if (DebugExport)
                {
                    Print("[APVA Export] path=" + path);
                    Print("[APVA Export] bars=" + analyzedBars.Count
                        + " containers=" + containers.Count
                        + " memberships=" + analyzedBars.Count
                        + " events=" + exportEvents.Count
                        + " warnings=" + warnings.Count);
                }
            }
            catch (Exception ex)
            {
                if (DebugExport)
                    Print("[APVA Export] failed: " + ex.Message);
            }
        }

        private string BuildContainerJson(int start, int end, List<ExportWarning> warnings)
        {
            var sb = new StringBuilder(1024 * 128);
            sb.AppendLine("{");
            AppendMetadataJson(sb, start, end);
            sb.AppendLine(",");
            AppendBarsJson(sb);
            sb.AppendLine(",");
            AppendContainersJson(sb);
            sb.AppendLine(",");
            AppendBarMembershipJson(sb);
            sb.AppendLine(",");
            AppendContainerEventsJson(sb);
            sb.AppendLine(",");
            AppendValidationWarningsJson(sb, warnings);
            sb.AppendLine();
            sb.Append("}");
            return sb.ToString();
        }

        private void AppendMetadataJson(StringBuilder sb, int start, int end)
        {
            AnalyzedBar first = analyzedBars.Count > 0 ? analyzedBars[0] : null;
            AnalyzedBar last = analyzedBars.Count > 0 ? analyzedBars[analyzedBars.Count - 1] : null;

            sb.AppendLine("  \"metadata\": {");
            AppendJsonProperty(sb, 4, "instrument", SafeInstrumentName(), true);
            AppendJsonProperty(sb, 4, "barsPeriod", SafeBarsPeriodName(), true);
            AppendJsonProperty(sb, 4, "sessionTemplate", SafeSessionTemplateName(), true);
            AppendJsonProperty(sb, 4, "timezone", TimeZoneInfo.Local.Id, true);
            AppendJsonProperty(sb, 4, "startBar", start, true);
            AppendJsonProperty(sb, 4, "endBar", end, true);
            AppendJsonProperty(sb, 4, "firstProcessedTime", first == null ? null : first.Time.ToString("O", CultureInfo.InvariantCulture), true);
            AppendJsonProperty(sb, 4, "lastProcessedTime", last == null ? null : last.Time.ToString("O", CultureInfo.InvariantCulture), true);
            AppendJsonProperty(sb, 4, "generatedAt", DateTime.Now.ToString("O", CultureInfo.InvariantCulture), true);
            AppendJsonProperty(sb, 4, "indicatorName", "xPvaAutomatedContainers", true);
            AppendJsonProperty(sb, 4, "indicatorVersion", "0.1", true);
            AppendJsonProperty(sb, 4, "chartBarsCount", Count, true);
            AppendJsonProperty(sb, 4, "processedBarCount", analyzedBars.Count, false);
            sb.Append("  }");
        }

        private void AppendBarsJson(StringBuilder sb)
        {
            sb.AppendLine("  \"bars\": [");
            for (int i = 0; i < analyzedBars.Count; i++)
            {
                AnalyzedBar bar = analyzedBars[i];
                sb.AppendLine("    {");
                AppendJsonProperty(sb, 6, "barIndex", bar.Bar, true);
                AppendJsonProperty(sb, 6, "time", bar.Time.ToString("O", CultureInfo.InvariantCulture), true);
                AppendJsonProperty(sb, 6, "open", bar.Open, true);
                AppendJsonProperty(sb, 6, "high", bar.High, true);
                AppendJsonProperty(sb, 6, "low", bar.Low, true);
                AppendJsonProperty(sb, 6, "close", bar.Close, true);
                AppendJsonProperty(sb, 6, "volume", bar.Volume, true);
                AppendJsonProperty(sb, 6, "priceType", bar.Relation.ToString(), true);
                AppendJsonProperty(sb, 6, "volumeColor", "Unknown", true);
                AppendJsonProperty(sb, 6, "eventText", bar.Relation.ToString(), true);
                AppendJsonProperty(sb, 6, "isLateralSynthetic", false, true);
                AppendJsonNullProperty(sb, 6, "lateralId", true);
                AppendIndent(sb, 6);
                sb.Append("\"rawBarIndexes\": [").Append(bar.Bar.ToString(CultureInfo.InvariantCulture)).AppendLine("]");
                sb.Append("    }");
                if (i < analyzedBars.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("  ]");
        }

        private void AppendContainersJson(StringBuilder sb)
        {
            sb.AppendLine("  \"containers\": [");
            for (int i = 0; i < containers.Count; i++)
            {
                PriceContainer container = containers[i];
                sb.AppendLine("    {");
                AppendJsonProperty(sb, 6, "containerId", container.Id, true);
                AppendJsonProperty(sb, 6, "level", container.Level, true);
                AppendJsonProperty(sb, 6, "direction", container.Direction.ToString(), true);
                AppendJsonProperty(sb, 6, "status", container.Status.ToString(), true);
                AppendJsonProperty(sb, 6, "startBar", container.StartBar, true);
                AppendJsonProperty(sb, 6, "endBar", container.EndBar, true);
                AppendNullableBarProperty(sb, 6, "p1Bar", container.P1Bar, true);
                AppendNullableBarProperty(sb, 6, "p2Bar", container.P2Bar, true);
                AppendNullableBarProperty(sb, 6, "p3Bar", container.P3Bar, true);
                AppendNullableBarProperty(sb, 6, "fttBar", container.FttConfirmed ? container.FttCandidateBar : 0, true);
                AppendLineAnchorJson(sb, container, true);
                AppendNullableBarProperty(sb, 6, "veBar", container.ActiveVe == null ? 0 : container.ActiveVe.StartBar, true);
                AppendNullablePriceProperty(sb, 6, "vePrice", container.ActiveVe == null ? double.NaN : container.ActiveVe.StartPrice, true);
                AppendNullableBarProperty(sb, 6, "breakBar", container.Status == ContainerStatus.Broken ? container.EndBar : 0, true);
                AppendJsonProperty(sb, 6, "breakType", container.Status == ContainerStatus.Broken ? "BreakConfirmed" : null, true);
                AppendNullableBarProperty(sb, 6, "parentContainerId", container.ParentId, true);
                AppendIntArrayProperty(sb, 6, "childContainerIds", container.ChildIds, true);
                AppendJsonProperty(sb, 6, "creationReason", container.Reason, true);
                AppendJsonProperty(sb, 6, "completionReason", container.FttConfirmed ? "FTT confirmed" : null, true);
                AppendJsonProperty(sb, 6, "joinReason", container.Status == ContainerStatus.Joined || IsJoinedReason(container) ? container.Reason : null, true);
                AppendJsonProperty(sb, 6, "adjustmentReason", container.Status == ContainerStatus.Adjusted ? "Adjusted" : null, true);
                AppendJsonProperty(sb, 6, "confidence", "High", true);
                AppendStringArrayProperty(sb, 6, "warnings", ContainerWarningMessages(container), true);
                AppendJsonProperty(sb, 6, "lineStyle", "Level" + container.Level.ToString(CultureInfo.InvariantCulture), true);
                AppendJsonProperty(sb, 6, "lineColor", container.Direction == ContainerDirection.Up ? "Blue" : container.Direction == ContainerDirection.Down ? "Red" : "Unknown", true);
                AppendJsonProperty(sb, 6, "lineWidth", (double)WidthForLevel(container.Level), true);
                AppendJsonProperty(sb, 6, "dashStyle", DashStyleName(container.Level), false);
                sb.Append("    }");
                if (i < containers.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("  ]");
        }

        private void AppendLineAnchorJson(StringBuilder sb, PriceContainer container, bool commaAfter)
        {
            AppendNullableBarProperty(sb, 6, "rtlStartBar", container.Rtl == null ? 0 : container.Rtl.StartBar, true);
            AppendNullablePriceProperty(sb, 6, "rtlStartPrice", container.Rtl == null ? double.NaN : container.Rtl.StartPrice, true);
            AppendNullableBarProperty(sb, 6, "rtlEndBar", container.Rtl == null ? 0 : container.EndBar, true);
            AppendNullablePriceProperty(sb, 6, "rtlEndPrice", container.Rtl == null ? double.NaN : container.Rtl.ValueAt(container.EndBar), true);
            AppendNullableBarProperty(sb, 6, "ltlStartBar", container.Ltl == null ? 0 : container.Ltl.StartBar, true);
            AppendNullablePriceProperty(sb, 6, "ltlStartPrice", container.Ltl == null ? double.NaN : container.Ltl.StartPrice, true);
            AppendNullableBarProperty(sb, 6, "ltlEndBar", container.Ltl == null ? 0 : container.EndBar, true);
            AppendNullablePriceProperty(sb, 6, "ltlEndPrice", container.Ltl == null ? double.NaN : container.Ltl.ValueAt(container.EndBar), commaAfter);
        }

        private void AppendBarMembershipJson(StringBuilder sb)
        {
            sb.AppendLine("  \"barMembership\": [");
            for (int i = 0; i < analyzedBars.Count; i++)
            {
                AnalyzedBar bar = analyzedBars[i];
                List<PriceContainer> members = ContainersForBar(bar.Bar);
                PriceContainer primary = PrimaryContainer(members);
                sb.AppendLine("    {");
                AppendJsonProperty(sb, 6, "barIndex", bar.Bar, true);
                AppendContainerIdArrayProperty(sb, 6, "containerIds", members, true);
                AppendNullableBarProperty(sb, 6, "primaryContainerId", primary == null ? 0 : primary.Id, true);
                sb.AppendLine("      \"roles\": [");
                bool wroteRole = false;
                for (int m = 0; m < members.Count; m++)
                    wroteRole = AppendRolesForContainer(sb, members[m], bar.Bar, wroteRole);
                sb.AppendLine();
                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < analyzedBars.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("  ]");
        }

        private bool AppendRolesForContainer(StringBuilder sb, PriceContainer container, int bar, bool wroteRole)
        {
            string[] roles = RolesForContainerBar(container, bar);
            for (int i = 0; i < roles.Length; i++)
            {
                if (wroteRole)
                    sb.AppendLine(",");
                sb.AppendLine("        {");
                AppendJsonProperty(sb, 10, "containerId", container.Id, true);
                AppendJsonProperty(sb, 10, "role", roles[i], false);
                sb.Append("        }");
                wroteRole = true;
            }
            return wroteRole;
        }

        private void AppendContainerEventsJson(StringBuilder sb)
        {
            sb.AppendLine("  \"containerEvents\": [");
            for (int i = 0; i < exportEvents.Count; i++)
            {
                ContainerExportEvent ev = exportEvents[i];
                sb.AppendLine("    {");
                AppendJsonProperty(sb, 6, "eventId", ev.EventId, true);
                AppendJsonProperty(sb, 6, "barIndex", ev.Bar, true);
                AppendJsonProperty(sb, 6, "eventType", ev.EventType, true);
                AppendNullableBarProperty(sb, 6, "containerId", ev.ContainerId, true);
                AppendIntArrayProperty(sb, 6, "relatedContainerIds", ev.RelatedContainerIds, true);
                AppendNullableBarProperty(sb, 6, "level", ev.Level, true);
                AppendJsonProperty(sb, 6, "reason", ev.Reason, true);
                AppendIndent(sb, 6);
                sb.Append("\"details\": { \"message\": ").Append(JsonValue(ev.Details)).AppendLine(" }");
                sb.Append("    }");
                if (i < exportEvents.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("  ]");
        }

        private void AppendValidationWarningsJson(StringBuilder sb, List<ExportWarning> warnings)
        {
            sb.AppendLine("  \"validationWarnings\": [");
            for (int i = 0; i < warnings.Count; i++)
            {
                ExportWarning warning = warnings[i];
                sb.AppendLine("    {");
                AppendJsonProperty(sb, 6, "warningId", warning.WarningId, true);
                AppendJsonProperty(sb, 6, "barIndex", warning.Bar, true);
                AppendNullableBarProperty(sb, 6, "containerId", warning.ContainerId, true);
                AppendJsonProperty(sb, 6, "warningType", warning.WarningType, true);
                AppendJsonProperty(sb, 6, "severity", warning.Severity, true);
                AppendJsonProperty(sb, 6, "message", warning.Message, false);
                sb.Append("    }");
                if (i < warnings.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("  ]");
        }

        private List<ExportWarning> BuildExportWarnings()
        {
            var warnings = new List<ExportWarning>();
            foreach (PriceContainer container in containers)
            {
                if (container.StartBar > container.EndBar)
                    AddExportWarning(warnings, container.EndBar, container.Id, "InvalidRange", "High", "Container startBar is greater than endBar.");
                if (container.P1Bar == 0 && container.StartBar != 0)
                    AddExportWarning(warnings, container.StartBar, container.Id, "MissingP1", "Medium", "Container P1 anchor is missing.");
                if (container.P2Bar == 0 && container.StartBar != 0)
                    AddExportWarning(warnings, container.StartBar, container.Id, "MissingP2", "Medium", "Container P2 anchor is missing.");
                if (container.P3Bar == 0 && container.StartBar != 0)
                    AddExportWarning(warnings, container.StartBar, container.Id, "MissingP3", "Medium", "Container P3 anchor is missing.");
                if (container.P1Bar != 0 && container.P2Bar != 0 && container.P2Bar < container.P1Bar)
                    AddExportWarning(warnings, container.P2Bar, container.Id, "InvalidAnchorOrdering", "High", "P2 occurs before P1.");
                if (container.P1Bar != 0 && container.P3Bar != 0 && container.P3Bar < container.P1Bar)
                    AddExportWarning(warnings, container.P3Bar, container.Id, "InvalidAnchorOrdering", "High", "P3 occurs before P1.");
                if (container.ParentId != 0)
                {
                    PriceContainer parent = FindContainer(container.ParentId);
                    if (parent == null)
                        AddExportWarning(warnings, container.EndBar, container.Id, "BrokenParentChildReference", "High", "Parent container reference is missing.");
                    else
                    {
                        if (!parent.ChildIds.Contains(container.Id))
                            AddExportWarning(warnings, container.EndBar, container.Id, "BrokenParentChildReference", "High", "Parent does not include child reciprocal reference.");
                        if (container.Level <= parent.Level)
                            AddExportWarning(warnings, container.EndBar, container.Id, "UnexpectedLevelJump", "Medium", "Child level is not greater than parent level.");
                        if (container.StartBar < parent.StartBar || container.EndBar > parent.EndBar)
                            AddExportWarning(warnings, container.EndBar, container.Id, "ChildOutsideParent", "Medium", "Child container extends outside parent range.");
                        if (HasParentCycle(container))
                            AddExportWarning(warnings, container.EndBar, container.Id, "BrokenParentChildReference", "High", "Parent-child cycle detected.");
                    }
                }
                foreach (int childId in container.ChildIds)
                    if (FindContainer(childId) == null)
                        AddExportWarning(warnings, container.EndBar, container.Id, "BrokenParentChildReference", "High", "Child container reference is missing: " + childId.ToString(CultureInfo.InvariantCulture));
                if (!ContainerAppearsInMembership(container, container.StartBar) || !ContainerAppearsInMembership(container, container.EndBar))
                    AddExportWarning(warnings, container.EndBar, container.Id, "MissingMembership", "Medium", "Container is missing from membership at start or end bar.");
            }
            return warnings;
        }

        private void AddExportWarning(List<ExportWarning> warnings, int bar, int containerId, string warningType, string severity, string message)
        {
            warnings.Add(new ExportWarning
            {
                WarningId = warnings.Count + 1,
                Bar = bar,
                ContainerId = containerId,
                WarningType = warningType,
                Severity = severity,
                Message = message
            });
        }

        private PriceContainer FindContainer(int id)
        {
            foreach (PriceContainer container in containers)
                if (container.Id == id)
                    return container;
            return null;
        }

        private string BuildDefaultJsonFileName(int start, int end)
        {
            return "APVA_Containers_"
                + SanitizeFileNamePart(SafeInstrumentName())
                + "_"
                + SanitizeFileNamePart(SafeBarsPeriodName())
                + "_"
                + start.ToString(CultureInfo.InvariantCulture)
                + "_"
                + end.ToString(CultureInfo.InvariantCulture)
                + "_"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                + ".json";
        }

        private string SafeInstrumentName()
        {
            try
            {
                return Instrument == null ? "Unknown" : Instrument.FullName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string SafeBarsPeriodName()
        {
            try
            {
                return BarsPeriod == null ? "Unknown" : BarsPeriod.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        private string SafeSessionTemplateName()
        {
            try
            {
                return Bars == null || Bars.TradingHours == null ? "Unknown" : Bars.TradingHours.Name;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string SanitizeFileName(string fileName)
        {
            string result = string.IsNullOrWhiteSpace(fileName) ? "APVA_Containers.json" : fileName;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result;
        }

        private string SanitizeFileNamePart(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            result = result.Replace(' ', '_');
            return result;
        }

        private List<PriceContainer> ContainersForBar(int bar)
        {
            var result = new List<PriceContainer>();
            foreach (PriceContainer container in containers)
            {
                if (container.StartBar <= bar && container.EndBar >= bar)
                    result.Add(container);
            }
            result.Sort((a, b) =>
            {
                int level = a.Level.CompareTo(b.Level);
                if (level != 0)
                    return level;
                return a.Id.CompareTo(b.Id);
            });
            return result;
        }

        private PriceContainer PrimaryContainer(List<PriceContainer> members)
        {
            if (members == null || members.Count == 0)
                return null;
            PriceContainer selected = members[0];
            foreach (PriceContainer candidate in members)
            {
                if (candidate.Status == ContainerStatus.Active && selected.Status != ContainerStatus.Active)
                    selected = candidate;
                else if (candidate.Level < selected.Level)
                    selected = candidate;
            }
            return selected;
        }

        private bool ContainerAppearsInMembership(PriceContainer container, int bar)
        {
            if (container == null)
                return false;
            List<PriceContainer> members = ContainersForBar(bar);
            foreach (PriceContainer member in members)
                if (member.Id == container.Id)
                    return true;
            return false;
        }

        private string[] RolesForContainerBar(PriceContainer container, int bar)
        {
            var roles = new List<string>();
            if (bar == container.StartBar)
                roles.Add("Start");
            if (bar == container.EndBar)
                roles.Add("End");
            if (bar == container.P1Bar)
                roles.Add("P1");
            if (bar == container.P2Bar)
                roles.Add("P2");
            if (bar == container.P3Bar)
                roles.Add("P3");
            if (container.FttConfirmed && bar == container.FttCandidateBar)
                roles.Add("FTT");
            if (container.ActiveVe != null && bar == container.ActiveVe.StartBar)
                roles.Add("VE");
            if (container.Status == ContainerStatus.Broken && bar == container.EndBar)
                roles.Add("Break");
            if (roles.Count == 0)
                roles.Add("Member");
            return roles.ToArray();
        }

        private bool HasParentCycle(PriceContainer container)
        {
            int parentId = container.ParentId;
            int guard = 0;
            while (parentId != 0 && guard++ < containers.Count + 1)
            {
                if (parentId == container.Id)
                    return true;
                PriceContainer parent = FindContainer(parentId);
                if (parent == null)
                    return false;
                parentId = parent.ParentId;
            }
            return guard > containers.Count;
        }

        private bool IsJoinedReason(PriceContainer container)
        {
            return container != null
                && !string.IsNullOrEmpty(container.Reason)
                && container.Reason.StartsWith("joined", StringComparison.OrdinalIgnoreCase);
        }

        private string[] ContainerWarningMessages(PriceContainer container)
        {
            var messages = new List<string>();
            if (container.StartBar > container.EndBar)
                messages.Add("Invalid range");
            if (container.ParentId != 0 && FindContainer(container.ParentId) == null)
                messages.Add("Missing parent");
            if (container.ParentId != 0)
            {
                PriceContainer parent = FindContainer(container.ParentId);
                if (parent != null && container.Level <= parent.Level)
                    messages.Add("Child level is not greater than parent level");
            }
            return messages.ToArray();
        }

        private string DashStyleName(int level)
        {
            if (level <= 1)
                return "Solid";
            if (level == 2)
                return "Dash";
            if (level == 3)
                return "Dot";
            return "DashDotDot";
        }

        private void RecordExportEvent(int bar, string message)
        {
            if (!ExportJson && !DebugExport)
                return;

            int containerId = ExtractIntAfter(message, "id=");
            if (containerId == 0)
                containerId = ExtractIntAfter(message, "parent=");
            PriceContainer container = containerId == 0 ? null : FindContainer(containerId);

            var ev = new ContainerExportEvent
            {
                EventId = nextExportEventId++,
                Bar = bar,
                EventType = ClassifyExportEvent(message),
                ContainerId = containerId,
                Level = container == null ? ExtractIntAfter(message, "level=") : container.Level,
                Reason = ExportEventReason(message),
                Details = message
            };

            AddRelatedContainerId(ev.RelatedContainerIds, ExtractIntAfter(message, "child="));
            AddRelatedContainerId(ev.RelatedContainerIds, ExtractIntAfter(message, "parent="));
            AddRelatedContainerId(ev.RelatedContainerIds, ExtractIntAfter(message, "broken="));
            AddRelatedContainerId(ev.RelatedContainerIds, ExtractIntAfter(message, "left="));
            AddRelatedContainerId(ev.RelatedContainerIds, ExtractIntAfter(message, "right="));
            exportEvents.Add(ev);
        }

        private void AddRelatedContainerId(List<int> related, int id)
        {
            if (id != 0 && !related.Contains(id))
                related.Add(id);
        }

        private string ClassifyExportEvent(string message)
        {
            string lower = message == null ? string.Empty : message.ToLowerInvariant();
            if (lower.Contains("created"))
                return "ContainerCreated";
            if (lower.Contains("extended"))
                return "ContainerExtended";
            if (lower.Contains("join rejected"))
                return "ContainerJoinRejected";
            if (lower.Contains("join accepted") || lower.Contains("joined"))
                return "ContainerJoined";
            if (lower.Contains("demotion"))
                return "ContainerDemoted";
            if (lower.Contains("adjust"))
                return "ContainerAdjusted";
            if (lower.Contains("wick violation"))
                return "RtlWickViolationAdjusted";
            if (lower.Contains("break"))
                return "BreakConfirmed";
            if (lower.Contains("lateral compressed"))
                return "LateralCompressed";
            if (lower.Contains("skipped"))
                return "SkippedBar";
            if (lower.Contains("warning"))
                return "ValidationWarning";
            if (lower.Contains("frozen"))
                return "ContainerCompleted";
            if (lower.Contains("line stopped"))
                return "LineStoppedExtending";
            return "ContainerAdjusted";
        }

        private string ExportEventReason(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Unknown";
            int separator = message.IndexOf(':');
            if (separator > 0)
                return message.Substring(0, separator);
            return message.Length > 80 ? message.Substring(0, 80) : message;
        }

        private int ExtractIntAfter(string value, string token)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(token))
                return 0;
            int index = value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return 0;
            index += token.Length;
            while (index < value.Length && !char.IsDigit(value[index]) && value[index] != '-')
                index++;
            int start = index;
            while (index < value.Length && (char.IsDigit(value[index]) || value[index] == '-'))
                index++;
            if (index <= start)
                return 0;
            int parsed;
            return int.TryParse(value.Substring(start, index - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private bool BarBreaksLateral(AnalyzedBar bar, ContainerDirection direction)
        {
            if (bar == null || !lateral.IsValid || lateral.StartBar < 0)
                return false;

            if (direction == ContainerDirection.Up)
                return bar.High >= lateral.Upper - TickSize * 0.5;

            if (direction == ContainerDirection.Down)
                return bar.Low <= lateral.Lower + TickSize * 0.5;

            return false;
        }

        private AnalyzedBar GetAnalyzed(int bar)
        {
            foreach (AnalyzedBar analyzed in analyzedBars)
                if (analyzed.Bar == bar)
                    return analyzed;
            return null;
        }

        private bool IsDominantBar(xPvaBarFacts cur, xPvaBarFacts prev, xPvaBarRelation relation, bool isReversal, bool compressedBody, out string reason)
        {
            if (prev == null)
            {
                reason = "missing prior bar";
                return false;
            }

            if (!IsTransitional(relation))
            {
                reason = "not transitional";
                return false;
            }

            if (isReversal)
            {
                reason = "reversal bars are not dominant";
                return false;
            }

            if (compressedBody)
            {
                reason = "compressed open-close body";
                return false;
            }

            double priorBodyHigh = Math.Max(prev.Open, prev.Close);
            double priorBodyLow = Math.Min(prev.Open, prev.Close);
            if (cur.Close <= priorBodyHigh && cur.Close >= priorBodyLow)
            {
                reason = "close remains inside prior body";
                return false;
            }

            if ((relation == xPvaBarRelation.HHHL || relation == xPvaBarRelation.OutsideBullish || relation == xPvaBarRelation.OutsideBar)
                && cur.Close > prev.High)
            {
                reason = "close above prior high";
                return true;
            }

            if ((relation == xPvaBarRelation.LLLH || relation == xPvaBarRelation.OutsideBearish || relation == xPvaBarRelation.OutsideBar)
                && cur.Close < prev.Low)
            {
                reason = "close below prior low";
                return true;
            }

            reason = "close did not clear prior high/low";
            return false;
        }

        private bool IsTransitional(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.HHHL
                || relation == xPvaBarRelation.LLLH
                || relation == xPvaBarRelation.StitchLong
                || relation == xPvaBarRelation.StitchShort
                || relation == xPvaBarRelation.OutsideBar
                || relation == xPvaBarRelation.OutsideBullish
                || relation == xPvaBarRelation.OutsideBearish
                || relation == xPvaBarRelation.HighReversal
                || relation == xPvaBarRelation.LowReversal;
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

        private bool IsReversalRelation(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.HighReversal || relation == xPvaBarRelation.LowReversal;
        }

        private bool IsBodyReversal(xPvaBarFacts cur, xPvaBarRelation relation)
        {
            if (relation == xPvaBarRelation.HighReversal || relation == xPvaBarRelation.LowReversal)
                return true;

            if (relation == xPvaBarRelation.HHHL && cur.Close < cur.Open)
                return true;

            if (relation == xPvaBarRelation.LLLH && cur.Close > cur.Open)
                return true;

            return false;
        }

        private bool IsOutsideRelation(xPvaBarRelation relation)
        {
            return relation == xPvaBarRelation.OutsideBar
                || relation == xPvaBarRelation.OutsideBullish
                || relation == xPvaBarRelation.OutsideBearish;
        }

        private ContainerDirection DirectionFromRelation(xPvaBarRelation relation)
        {
            if (relation == xPvaBarRelation.HHHL || relation == xPvaBarRelation.HighReversal || relation == xPvaBarRelation.OutsideBullish)
                return ContainerDirection.Up;
            if (relation == xPvaBarRelation.LLLH || relation == xPvaBarRelation.LowReversal || relation == xPvaBarRelation.OutsideBearish)
                return ContainerDirection.Down;
            return ContainerDirection.Unknown;
        }

        private StrokeStyle StrokeForLevel(int level)
        {
            if (level <= 1)
                return null;
            if (level == 2)
                return dashStrokeStyle;
            if (level == 3)
                return dotStrokeStyle;
            if (deepLevelStrokeStyle != null)
                return deepLevelStrokeStyle;
            return dotStrokeStyle;
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

        private void AppendJsonProperty(StringBuilder sb, int indent, string name, string value, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": ").Append(JsonValue(value));
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendJsonProperty(StringBuilder sb, int indent, string name, int value, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": ").Append(value.ToString(CultureInfo.InvariantCulture));
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendJsonProperty(StringBuilder sb, int indent, string name, double value, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": ").Append(JsonNumber(value));
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendJsonProperty(StringBuilder sb, int indent, string name, bool value, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": ").Append(value ? "true" : "false");
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendJsonNullProperty(StringBuilder sb, int indent, string name, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": null");
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendNullableBarProperty(StringBuilder sb, int indent, string name, int value, bool comma)
        {
            if (value == 0)
                AppendJsonNullProperty(sb, indent, name, comma);
            else
                AppendJsonProperty(sb, indent, name, value, comma);
        }

        private void AppendNullablePriceProperty(StringBuilder sb, int indent, string name, double value, bool comma)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                AppendJsonNullProperty(sb, indent, name, comma);
            else
                AppendJsonProperty(sb, indent, name, value, comma);
        }

        private void AppendIntArrayProperty(StringBuilder sb, int indent, string name, List<int> values, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": [");
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append("]");
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendContainerIdArrayProperty(StringBuilder sb, int indent, string name, List<PriceContainer> values, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": [");
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(values[i].Id.ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append("]");
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendStringArrayProperty(StringBuilder sb, int indent, string name, string[] values, bool comma)
        {
            AppendIndent(sb, indent);
            sb.Append(JsonValue(name)).Append(": [");
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(JsonValue(values[i]));
                }
            }
            sb.Append("]");
            if (comma)
                sb.Append(",");
            sb.AppendLine();
        }

        private void AppendIndent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++)
                sb.Append(' ');
        }

        private string JsonValue(string value)
        {
            if (value == null)
                return "null";

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string JsonNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "null";
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private void LogDebug(int bar, string message)
        {
            RecordExportEvent(bar, message);

            if (!Debug)
                return;

            debugEvents.Add(string.Format(CultureInfo.InvariantCulture, "[APVA] bar={0} {1}", bar, message));
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
        }
    }

}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaAutomatedContainers[] cachexPvaAutomatedContainers;
		public xPvaAutomatedContainers xPvaAutomatedContainers(bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			return xPvaAutomatedContainers(Input, debug, startBar, endBar, enableLineageJoins, exportJson, jsonExportFolder, jsonFileName, exportOnEveryBar, debugExport);
		}

		public xPvaAutomatedContainers xPvaAutomatedContainers(ISeries<double> input, bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			if (cachexPvaAutomatedContainers != null)
				for (int idx = 0; idx < cachexPvaAutomatedContainers.Length; idx++)
					if (cachexPvaAutomatedContainers[idx] != null && cachexPvaAutomatedContainers[idx].Debug == debug && cachexPvaAutomatedContainers[idx].StartBar == startBar && cachexPvaAutomatedContainers[idx].EndBar == endBar && cachexPvaAutomatedContainers[idx].EnableLineageJoins == enableLineageJoins && cachexPvaAutomatedContainers[idx].ExportJson == exportJson && cachexPvaAutomatedContainers[idx].JsonExportFolder == jsonExportFolder && cachexPvaAutomatedContainers[idx].JsonFileName == jsonFileName && cachexPvaAutomatedContainers[idx].ExportOnEveryBar == exportOnEveryBar && cachexPvaAutomatedContainers[idx].DebugExport == debugExport && cachexPvaAutomatedContainers[idx].EqualsInput(input))
						return cachexPvaAutomatedContainers[idx];
			return CacheIndicator<xPvaAutomatedContainers>(new xPvaAutomatedContainers(){ Debug = debug, StartBar = startBar, EndBar = endBar, EnableLineageJoins = enableLineageJoins, ExportJson = exportJson, JsonExportFolder = jsonExportFolder, JsonFileName = jsonFileName, ExportOnEveryBar = exportOnEveryBar, DebugExport = debugExport }, input, ref cachexPvaAutomatedContainers);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaAutomatedContainers xPvaAutomatedContainers(bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			return indicator.xPvaAutomatedContainers(Input, debug, startBar, endBar, enableLineageJoins, exportJson, jsonExportFolder, jsonFileName, exportOnEveryBar, debugExport);
		}

		public Indicators.xPvaAutomatedContainers xPvaAutomatedContainers(ISeries<double> input , bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			return indicator.xPvaAutomatedContainers(input, debug, startBar, endBar, enableLineageJoins, exportJson, jsonExportFolder, jsonFileName, exportOnEveryBar, debugExport);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaAutomatedContainers xPvaAutomatedContainers(bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			return indicator.xPvaAutomatedContainers(Input, debug, startBar, endBar, enableLineageJoins, exportJson, jsonExportFolder, jsonFileName, exportOnEveryBar, debugExport);
		}

		public Indicators.xPvaAutomatedContainers xPvaAutomatedContainers(ISeries<double> input , bool debug, int startBar, int endBar, bool enableLineageJoins, bool exportJson, string jsonExportFolder, string jsonFileName, bool exportOnEveryBar, bool debugExport)
		{
			return indicator.xPvaAutomatedContainers(input, debug, startBar, endBar, enableLineageJoins, exportJson, jsonExportFolder, jsonFileName, exportOnEveryBar, debugExport);
		}
	}
}

#endregion
