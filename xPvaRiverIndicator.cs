#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{		
    public class xPvaRiverIndicator : Indicator
    {
        private NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine engine;

        private readonly Dictionary<int, RiverBarState> riverStates = new Dictionary<int, RiverBarState>();
		private NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot? manualGeometrySnapshot;
		private int lastManualSnapshotVersion = -1;
		private NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerRuntime.State manualRuntimeState =
    								new NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerRuntime.State();
		private NinjaTrader.NinjaScript.xPva.Engine.ManualContainerSnapshot? latestManualSnapshot = null;
		private NinjaTrader.NinjaScript.xPva.Engine.xPvaAutoContainerMvp.State autoMvpState =
		    new NinjaTrader.NinjaScript.xPva.Engine.xPvaAutoContainerMvp.State();
		
		private NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot? autoMvpSnapshot = null;
		
		private NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeEvent[] manualVolumeEvents =
    			new NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeEvent[0];
		
		private int? manualHistoricalCandidateBar = null;
		private int? manualHistoricalConfirmedBar = null;
		private string manualHistoricalActionToken = null;
		private string manualHistoricalStructureToken = null;
		private bool manualHistoricalScanPending = false;
		private int lastManualInterpretedContainerId = -1;
		private int lastManualInterpretedP3BarIndex = -1;
		private string manualHistoricalDecisionCompact = null;
		private int lastManualLiveCandidateBar = -1;
		private int lastManualLiveConfirmedBar = -1;
		
		private string manualStatusText = null;
		private int manualStatusBarIndex = -1;
		
		private string manualComparisonText = null;
		private int manualComparisonBarIndex = -1;
		
		private NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot? autoGeometrySnapshot = null;
		private bool autoContainerActive = false;
		private int autoP1 = -1;
		private int autoP2 = -1;
		private int autoP3 = -1;
		private bool autoIsUp = true;
		private int autoContainerId = 0;
		

        [NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "VolPivotWindow", Order = 1, GroupName = "Parameters")]
		public int VolPivotWindow { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Print Events", Order = 2, GroupName = "Parameters")]
		public bool PrintEvents { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw FTT", Order = 3, GroupName = "Parameters")]
		public bool DrawFtt { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Action Boxes", Order = 4, GroupName = "Parameters")]
		public bool DrawActionBoxes { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Turn/Trend Lane", Order = 5, GroupName = "Parameters")]
		public bool DrawTurnTrendLane { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw OOE Lane", Order = 6, GroupName = "Parameters")]
		public bool DrawOoeLane { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Container IDs", Order = 7, GroupName = "Parameters")]
		public bool DrawContainerIds { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Provisional Geometry", Order = 8, GroupName = "Parameters")]
		public bool DrawGeometry { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "Display Mode (0=Research, 1=Trading)", Order = 9, GroupName = "Parameters")]
		public int DisplayMode { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Volume Lane", Order = 10, GroupName = "Parameters")]
		public bool DrawVolumeLane { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Middle Lane", Order = 11, GroupName = "Parameters")]
		public bool DrawMiddleLane { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Container Lane", Order = 12, GroupName = "Parameters")]
		public bool DrawContainerLane { get; set; }
		
		[NinjaScriptProperty]
		[Range(8, 18)]
		[Display(Name = "Font Size", Order = 13, GroupName = "Parameters")]
		public int FontSize { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Geometry Points", Order = 14, GroupName = "Parameters")]
		public bool DrawGeometryPoints { get; set; }

		
		
		private sealed class RiverBarState
		{
		    public string ActionToken;
		    public string TurnTrendToken;
		    public string VolumeToken;
		    public string ContainerToken;
			public string TradeIntentToken;
			public string ManualDecisionToken;
			public bool ManualHasFtt;
		
		    public bool HasFtt;
		    public NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection? FttPriorDirection;
		
		    public NinjaTrader.NinjaScript.xPva.Engine.ActionType? ActionType;
		}
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaRiverIndicator";
                Description = "Lane-based river chart view for xPvaEngine.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                VolPivotWindow = 1;
                PrintEvents = false;
                DrawFtt = true;
                DrawActionBoxes = true;
                DrawTurnTrendLane = true;
                DrawOoeLane = true;
                DrawContainerIds = true;
                DrawGeometry = false;
                FontSize = 10;
				
				DisplayMode = 0;

				VolPivotWindow = 1;
				PrintEvents = false;
				DrawFtt = true;
				DrawActionBoxes = true;
				DrawTurnTrendLane = true;
				DrawOoeLane = true;
				DrawContainerIds = true;
				DrawGeometry = false;
				
				DrawVolumeLane = true;
				DrawMiddleLane = true;
				DrawContainerLane = true;
				DrawGeometryPoints = true;
				
				FontSize = 10;
            }
            else if (State == State.DataLoaded)
            {
                engine = new NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine(VolPivotWindow);
            }
        }

		private void DrawRtl(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g)
		{
		    if (!g.P1.HasValue || !g.P3.HasValue)
		        return;
		
		    int i1 = g.P1.Value.BarIndex;
		    int i3 = g.P3.Value.BarIndex;
		
		    double p1 = g.P1.Value.Price;
		    double p3 = g.P3.Value.Price;
		
		    if (i3 <= i1)
		        return;
		
		    double slope = (p3 - p1) / (double)(i3 - i1);
		    double rtlNow = p1 + slope * (CurrentBar - i1);
		
		    int barsAgo1 = BarsAgoFromIndex(i1);
		    if (barsAgo1 < 0 || barsAgo1 > CurrentBar)
		        return;
		
		    Brush brush =
		        g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		            ? Brushes.Blue
		            : Brushes.Red;
		
		    Draw.Line(
		        this,
		        $"RTL_{g.ContainerId}",
		        false,
		        barsAgo1,
		        p1,
		        0,
		        rtlNow,
		        brush,
		        NinjaTrader.Gui.DashStyleHelper.Solid,
		        2);
		}
		
		private void DrawLtl(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g)
		{
		    if (!g.P1.HasValue || !g.P2.HasValue || !g.P3.HasValue)
		        return;
		
		    int i1 = g.P1.Value.BarIndex;
		    int i2 = g.P2.Value.BarIndex;
		    int i3 = g.P3.Value.BarIndex;
		
		    double p1 = g.P1.Value.Price;
		    double p2 = g.P2.Value.Price;
		    double p3 = g.P3.Value.Price;
		
		    double slope = (p3 - p1) / (double)(i3 - i1);
		
		    double intercept = p2 - slope * i2;
		
		    int barsAgo2 = BarsAgoFromIndex(i2);
		
		    double yNow = slope * CurrentBar + intercept;
		
		    Brush brush =
		        g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? Brushes.Blue
		        : Brushes.Red;
		
		    Draw.Line(
		        this,
		        $"LTL_{g.ContainerId}",
		        false,
		        barsAgo2,
		        p2,
		        0,
		        yNow,
		        brush,
		        DashStyleHelper.Solid,
		        2);
		}
		
		
		
		private void DrawVe1(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g)
		{
		    if (!g.P1.HasValue || !g.P2.HasValue || !g.P3.HasValue)
		        return;
		
		    int i1 = g.P1.Value.BarIndex;
		    int i2 = g.P2.Value.BarIndex;
		    int i3 = g.P3.Value.BarIndex;
		
		    double p1 = g.P1.Value.Price;
		    double p2 = g.P2.Value.Price;
		    double p3 = g.P3.Value.Price;
		
		    if (i3 <= i1)
		        return;
		
		    double slope = (p3 - p1) / (double)(i3 - i1);
		
		    // RTL evaluated at P2
		    double rtlAtP2 = p1 + slope * (i2 - i1);
		
		    // Channel width measured between RTL and LTL at P2
		    double width = System.Math.Abs(p2 - rtlAtP2);
		    if (width <= 0.0)
		        return;
		
		    // LTL projected to current bar
		    double ltlNow = p2 + slope * (CurrentBar - i2);
		
		    // VE1 is one channel width outside LTL
		    double veStart;
		    double veNow;
		
		    if (g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
		    {
		        veStart = p2 + width;
		        veNow = ltlNow + width;
		    }
		    else
		    {
		        veStart = p2 - width;
		        veNow = ltlNow - width;
		    }
		
		    int barsAgo2 = BarsAgoFromIndex(i2);
		    if (barsAgo2 < 0 || barsAgo2 > CurrentBar)
		        return;
		
		    Brush brush =
		        g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		            ? Brushes.Blue
		            : Brushes.Red;
		
		    Draw.Line(
		        this,
		        $"VE1_{g.ContainerId}",
		        false,
		        barsAgo2,
		        veStart,
		        0,
		        veNow,
		        brush,
		        NinjaTrader.Gui.DashStyleHelper.Dot,
		        1);
		}
		
		private void DrawGeometryStateLabel(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g)
		{
		    if (!g.P3.HasValue)
		        return;
		
		    int barsAgo = BarsAgoFromIndex(g.P3.Value.BarIndex);
		
		    Brush brush = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? Brushes.Blue
		        : Brushes.Red;
		
		    Draw.Text(
		        this,
		        $"GeoState_{g.ContainerId}",
		        false,
		        $"C#{g.ContainerId} {g.State}",
		        barsAgo,
		        g.P3.Value.Price + 2 * TickSize,
		        0,
		        brush,
		        new SimpleFont("Arial", FontSize - 1),
		        TextAlignment.Center,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void ProcessRiverEvent(
		    NinjaTrader.NinjaScript.xPva.Engine.EngineEvent e,
		    RiverBarState currentState,
		    ref NinjaTrader.NinjaScript.xPva.Engine.TurnType? latestTurnType,
		    ref NinjaTrader.NinjaScript.xPva.Engine.TrendType? latestTrendType)
		{
		    if (PrintEvents)
		        Print(FormatEventLine(e));
		
		    switch (e.Kind)
		    {
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.FttCandidate:
		            if (e.FttCandidate.HasValue)
		            {
		                manualHistoricalCandidateBar = e.BarIndex;
		                Print($"[ManualRuntime-Historical] FTT Candidate C#{e.FttCandidate.Value.ContainerId} at bar {e.BarIndex}");
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.FttConfirmed:
		            if (e.FttConfirmed.HasValue)
		            {
		                currentState.HasFtt = true;
		                currentState.FttPriorDirection = e.FttConfirmed.Value.PriorDirection;
		
		                manualHistoricalConfirmedBar = e.BarIndex;
		
		                var manualState = GetRiverState(e.BarIndex);
		                manualState.ManualHasFtt = true;
						
						manualRuntimeState.HasActiveManualContainer = false;
						
						manualStatusText = $"MC C#{e.FttConfirmed.Value.ContainerId} RESOLVED";
						manualStatusBarIndex = e.BarIndex;
		
		                Print($"[ManualRuntime-Historical] FTT Confirmed C#{e.FttConfirmed.Value.ContainerId} at bar {e.BarIndex}");
		
		                if (DrawFtt)
		                    DrawFttConfirmed(e.FttConfirmed.Value);
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Structure:
		            if (e.Structure.HasValue)
		            {
		                manualHistoricalStructureToken = e.Structure.Value.State.ToString();
		                Print($"[ManualRuntime-Historical] Structure C#{e.Structure.Value.ContainerId} {e.Structure.Value.State} dir={e.Structure.Value.Direction}");
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Action:
		            if (e.Action.HasValue)
		            {
		                currentState.ActionType = e.Action.Value.Action;
		                currentState.ActionToken = ActionToken(e.Action.Value.Action);
		
		                manualHistoricalActionToken = e.Action.Value.Action.ToString();
		                Print($"[ManualRuntime-Historical] Action C#{e.Action.Value.ContainerId} {e.Action.Value.Action}");
		
		                if (DisplayMode == 0 && DrawActionBoxes)
		                    DrawActionBox(e.Action.Value);
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.TradeIntent:
		            if (e.TradeIntent.HasValue)
		            {
		                currentState.TradeIntentToken = TradeIntentToken(e.TradeIntent.Value.Intent);
		
		                Print($"[ManualRuntime-Historical] TradeIntent C#{e.TradeIntent.Value.ContainerId} {e.TradeIntent.Value.Intent}");
		
		                string actionShort = e.TradeIntent.Value.Intent.ToString();
		                if (actionShort == "Sideline")
		                    actionShort = "SIDE";
		                else if (actionShort == "Enter")
		                    actionShort = "ENT";
		                else if (actionShort == "Reverse")
		                    actionShort = "REV";
		                else if (actionShort == "HoldThru")
		                    actionShort = "HT";
		                else if (actionShort == "EarlyExit")
		                    actionShort = "EE";
		                else if (actionShort == "ReEntry")
		                    actionShort = "RE";
		
		                string structureShort = manualHistoricalStructureToken;
		                if (structureShort == "Broken")
		                    structureShort = "BRK";
		                else if (structureShort == "Transition")
		                    structureShort = "TRN";
		
		                manualHistoricalDecisionCompact = actionShort;
		                if (!string.IsNullOrEmpty(structureShort))
		                    manualHistoricalDecisionCompact += " " + structureShort;
		
		                var manualState = GetRiverState(e.BarIndex);
		                manualState.ManualDecisionToken = manualHistoricalDecisionCompact;
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerReport:
		            if (e.ContainerReport.HasValue)
		            {
		                currentState.ContainerToken = "C#" + e.ContainerReport.Value.ContainerId;
		
		                if (DisplayMode == 0 && DrawContainerIds)
		                    DrawContainerId(e.ContainerReport.Value);
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Turn:
		            if (e.Turn.HasValue)
		                latestTurnType = e.Turn.Value.Type;
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.TrendType:
		            if (e.TrendType.HasValue)
		                latestTrendType = e.TrendType.Value.Type;
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.VolOoe:
		            if (e.VolOoe.HasValue)
		                currentState.VolumeToken = MergeToken(currentState.VolumeToken, e.VolOoe.Value.Name.ToString());
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.VolPivot:
		            if (e.VolPivot.HasValue)
		            {
		                string pv = e.VolPivot.Value.Kind == NinjaTrader.NinjaScript.xPva.Engine.VolPivotKind.Peak ? "Pk" : "Tr";
		                currentState.VolumeToken = MergeToken(currentState.VolumeToken, pv);
		            }
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerGeometry:
		            if (DisplayMode == 0 && DrawGeometry && e.ContainerGeometry.HasValue)
		                DrawContainerGeometryEvent(e.ContainerGeometry.Value);
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerGeometrySnapshot:
		            if (DrawGeometryPoints && e.ContainerGeometrySnapshot.HasValue)
		            {
						Print($"[AutoGeom] C#{e.ContainerGeometrySnapshot.Value.ContainerId} state={e.ContainerGeometrySnapshot.Value.State}");
						
		                var g = e.ContainerGeometrySnapshot.Value;
						
						if (latestManualSnapshot.HasValue)
						{
						    var cmp =
						        NinjaTrader.NinjaScript.xPva.Engine.xPvaContainerComparer.Compare(
						            latestManualSnapshot.Value,
						            g);
						
						    manualComparisonText =
						        $"CMP P1:{cmp.P1BarError} P2:{cmp.P2BarError} P3:{cmp.P3BarError} S:{cmp.RtlSlopeError:0.########}";
						    manualComparisonBarIndex = CurrentBar;
							
							Print($"[Compare] manual C#{latestManualSnapshot.Value.ContainerId} vs auto C#{g.ContainerId} " +
                                     $"P1:{cmp.P1BarError} P2:{cmp.P2BarError} P3:{cmp.P3BarError} S:{cmp.RtlSlopeError:0.########}");
						}
						
		                DrawGeometryPointsEvent(g);
		
		                if (g.State == NinjaTrader.NinjaScript.xPva.Engine.GeometryState.Active)
		                {
		                    DrawRtl(g);
		                    DrawLtl(g);
		                    DrawVe1(g);
		                }
		
		                DrawGeometryStateLabel(g);
		            }
		            break;
		    }
		}
		
		private void UpdateAutoContainerMvp()
		{
		    if (CurrentBar < 1)
		        return;
		
		    bool isHHHL =
		        High[0] > High[1] &&
		        Low[0] > Low[1];
		
		    bool isLHLL =
		        High[0] < High[1] &&
		        Low[0] < Low[1];
		
		    if (!autoContainerActive)
		    {
		        if (isHHHL)
		        {
		            autoContainerActive = true;
		            autoIsUp = true;
		
		            autoP1 = CurrentBar - 1;
		            autoP2 = CurrentBar;
		            autoP3 = CurrentBar;
		            autoContainerId++;
		        }
		        else if (isLHLL)
		        {
		            autoContainerActive = true;
		            autoIsUp = false;
		
		            autoP1 = CurrentBar - 1;
		            autoP2 = CurrentBar;
		            autoP3 = CurrentBar;
		            autoContainerId++;
		        }
		    }
		    else
		    {
		        autoP3 = CurrentBar;
		
		        bool broken =
		            autoIsUp
		                ? Low[0] < Low[CurrentBar - autoP1]
		                : High[0] > High[CurrentBar - autoP1];
		
		        if (broken)
		        {
		            autoContainerActive = false;
		            return;
		        }
		    }
		
		    if (!autoContainerActive)
		        return;
		
		    if (autoP1 < 0 || autoP2 < 0 || autoP3 < 0)
		        return;
		
		    double p1Price = autoIsUp ? Low[CurrentBar - autoP1] : High[CurrentBar - autoP1];
		    double p2Price = autoIsUp ? High[CurrentBar - autoP2] : Low[CurrentBar - autoP2];
		    double p3Price = autoIsUp ? Low[CurrentBar - autoP3] : High[CurrentBar - autoP3];
		
		    var p1 = new NinjaTrader.NinjaScript.xPva.Engine.GeometryPoint(autoP1, p1Price);
		    var p2 = new NinjaTrader.NinjaScript.xPva.Engine.GeometryPoint(autoP2, p2Price);
		    var p3 = new NinjaTrader.NinjaScript.xPva.Engine.GeometryPoint(autoP3, p3Price);
		
		    if (autoP3 <= autoP1)
		        return;
		
		    var rtl = new NinjaTrader.NinjaScript.xPva.Engine.LineDef(p1, p3);
		
		    var ltlB = new NinjaTrader.NinjaScript.xPva.Engine.GeometryPoint(
		        p2.BarIndex + 1,
		        p2.Price + rtl.Slope);
		
		    var ltl = new NinjaTrader.NinjaScript.xPva.Engine.LineDef(p2, ltlB);
		
		    double rtlAtP2 = rtl.ValueAt(p2.BarIndex);
		    double width = System.Math.Abs(p2.Price - rtlAtP2);
		
		    double ltlNow = ltl.ValueAt(CurrentBar);
		    double ve1 = autoIsUp ? ltlNow + width : ltlNow - width;
		    double ve2 = autoIsUp ? ltlNow + 2.0 * width : ltlNow - 2.0 * width;
		
		    autoGeometrySnapshot = new NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot(
		        autoContainerId,
		        autoIsUp
		            ? NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		            : NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Down,
		        NinjaTrader.NinjaScript.xPva.Engine.GeometryState.Active,
		        p1,
		        p2,
		        p3,
		        rtl,
		        ltl,
		        ve1,
		        ve2,
		        CurrentBar);
		}

		private void RefreshManualGeometrySnapshot()
		{
		    NinjaTrader.NinjaScript.xPva.Engine.ManualContainerSnapshot manualSnapshot;
		    int version;
		
		    if (!NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerBridge.TryGetLatest(out manualSnapshot, out version))
		        return;
		
		    if (version == lastManualSnapshotVersion)
		        return;
		
		    latestManualSnapshot = manualSnapshot;
		
		    manualGeometrySnapshot =
		        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerAdapter.FromManual(
		            manualSnapshot,
		            CurrentBar);
		
		    NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerRuntime.LoadManualContainer(
		        manualRuntimeState,
		        manualGeometrySnapshot.Value);
			
			lastManualLiveCandidateBar = -1;
			lastManualLiveConfirmedBar = -1;
		
		    bool manualContainerChanged =
		        manualSnapshot.ContainerId != lastManualInterpretedContainerId ||
		        manualSnapshot.P3.BarIndex != lastManualInterpretedP3BarIndex;
		
		    if (manualContainerChanged)
		    {
		        manualHistoricalCandidateBar = null;
		        manualHistoricalConfirmedBar = null;
		        manualHistoricalActionToken = null;
		        manualHistoricalStructureToken = null;
		        manualHistoricalScanPending = true;
				manualHistoricalDecisionCompact = null;
				manualStatusText = null;
				manualStatusBarIndex = -1;
				
				lastManualLiveCandidateBar = -1;
				lastManualLiveConfirmedBar = -1;
				
				manualRuntimeState.HasActiveManualContainer = false;
				
				manualComparisonText = null;
				manualComparisonBarIndex = -1;
				
				manualVolumeEvents = new NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeEvent[0];
		    }
		    else
		    {
		        manualHistoricalScanPending = false;
		    }
		
		    lastManualSnapshotVersion = version;
		
		    var g = manualGeometrySnapshot.Value;
		    Print($"[River] manual C#{g.ContainerId} state={g.State} P1={g.P1.HasValue} P2={g.P2.HasValue} P3={g.P3.HasValue}");
			
			if (g.P1.HasValue && g.P3.HasValue)
			{
			    manualVolumeEvents =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualVolumeAnalyzer.Analyze(
			            g.P1.Value.BarIndex,
			            g.P3.Value.BarIndex,
			            idx => (long)Bars.GetVolume(idx));
			
				Print($"[ManualVolume] count={manualVolumeEvents.Length} P1Bar={g.P1.Value.BarIndex} P3Bar={g.P3.Value.BarIndex}");
				
			    for (int i = 0; i < manualVolumeEvents.Length; i++)
			    {
			        var ve = manualVolumeEvents[i];
			        Print($"[ManualVolume] C#{g.ContainerId} {ve.Label} at bar {ve.BarIndex} vol={ve.Volume}");
			    }
			}
			
			manualStatusText =
		    $"MC C#{g.ContainerId} " +
		    $"{(g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up ? "UP" : "DN")} " +
		    $"{latestManualSnapshot.Value.BreakMode}";
			manualStatusBarIndex = CurrentBar;
		}
		
		private void DrawManualHistoricalCandidate(
		    NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g,
		    int barIndex)
		{
		    int barsAgo = BarsAgoFromIndex(barIndex);
		
		    if (barsAgo < 0 || barsAgo > CurrentBar)
		        return;
		
		    string tag = $"ManualCand_{g.ContainerId}_{barIndex}";
		
		    if (g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
		    {
		        Draw.ArrowDown(
		            this,
		            tag,
		            false,
		            barsAgo,
		            High[barsAgo] + 1.5 * TickSize,
		            Brushes.Gold);
		    }
		    else
		    {
		        Draw.ArrowUp(
		            this,
		            tag,
		            false,
		            barsAgo,
		            Low[barsAgo] - 1.5 * TickSize,
		            Brushes.Gold);
		    }
		}
		
		private void DrawManualComparisonLabel()
		{
		    if (string.IsNullOrEmpty(manualComparisonText))
		        return;
		
		    int barIndex = manualComparisonBarIndex >= 0 ? manualComparisonBarIndex : CurrentBar;
		    int barsAgo = BarsAgoFromIndex(barIndex);
		
		    if (barsAgo < 0 || barsAgo > CurrentBar)
		        return;
		
		    Draw.Text(
		        this,
		        "xPvaManualComparison",
		        false,
		        manualComparisonText,
		        barsAgo,
		        High[barsAgo] + 12 * TickSize,
		        0,
		        Brushes.DarkViolet,
		        new SimpleFont("Arial", FontSize - 1),
		        TextAlignment.Left,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void DrawManualHistoricalConfirmed(
		    NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g,
		    int barIndex)
		{
		    int barsAgo = BarsAgoFromIndex(barIndex);
		
		    if (barsAgo < 0 || barsAgo > CurrentBar)
		        return;
		
		    string tag = $"ManualConf_{g.ContainerId}_{barIndex}";
		
		    double y = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? High[barsAgo] + 3 * TickSize
		        : Low[barsAgo] - 3 * TickSize;
		
		    Draw.Text(
		        this,
		        tag,
		        false,
		        "FTT",
		        barsAgo,
		        y,
		        0,
		        Brushes.Gold,
		        new SimpleFont("Arial", FontSize - 1),
		        TextAlignment.Center,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void DrawManualHistoricalDecision(
	    NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g,
	    int barIndex,
	    string compactToken)
		{
		    int barsAgo = BarsAgoFromIndex(barIndex);
		
		    if (barsAgo < 0 || barsAgo > CurrentBar)
		        return;
		
		    string tag = $"ManualDecision_{g.ContainerId}_{barIndex}";
		
		    Brush brush = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? Brushes.Blue
		        : Brushes.Red;
		
		    double y = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? High[barsAgo] + 6 * TickSize
		        : Low[barsAgo] - 6 * TickSize;
		
		    Draw.Text(
		        this,
		        tag,
		        false,
		        compactToken,
		        barsAgo,
		        y,
		        0,
		        brush,
		        new SimpleFont("Arial", FontSize - 1),
		        TextAlignment.Center,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void PublishAutoContainerSnapshot()
		{
		    if (autoP1 < 0 || autoP2 < 0 || autoP3 < 0)
		        return;
		
		    var p1 = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(Time[CurrentBar - autoP1], Open[CurrentBar - autoP1], 
				High[CurrentBar - autoP1], Low[CurrentBar - autoP1], Close[CurrentBar - autoP1], (long)Volume[CurrentBar - autoP1], autoP1);
		    var p2 = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(Time[CurrentBar - autoP2], Open[CurrentBar - autoP2], 
				High[CurrentBar - autoP2], Low[CurrentBar - autoP2], Close[CurrentBar - autoP2], (long)Volume[CurrentBar - autoP2], autoP2);
		    var p3 = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(Time[CurrentBar - autoP3], Open[CurrentBar - autoP3], 
				High[CurrentBar - autoP3], Low[CurrentBar - autoP3], Close[CurrentBar - autoP3], (long)Volume[CurrentBar - autoP3], autoP3);
		
		    double slope = (p2.H - p1.H) / (autoP2 - autoP1);
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
		    RefreshManualGeometrySnapshot();
		    DrawManualGeometrySnapshot();
		    base.OnRender(chartControl, chartScale);
		}
		
        protected override void OnBarUpdate()
		{
		    if (CurrentBar < 1 || engine == null)
		        return;
		
			bool isHHHL = High[0] > High[1] && Low[0] > Low[1];
			bool isLHLL = High[0] < High[1] && Low[0] < Low[1];
			
			if (!autoContainerActive)
			{
			    if (isHHHL)
			    {
			        autoContainerActive = true;
			        autoIsUp = true;
			
			        autoP1 = CurrentBar - 1;
			        autoP2 = CurrentBar;
			        autoP3 = CurrentBar;
			
			        autoContainerId++;
			    }
			    else if (isLHLL)
			    {
			        autoContainerActive = true;
			        autoIsUp = false;
			
			        autoP1 = CurrentBar - 1;
			        autoP2 = CurrentBar;
			        autoP3 = CurrentBar;
			
			        autoContainerId++;
			    }
			}
			
			if (autoContainerActive)
			{
				PublishAutoContainerSnapshot();
				
			    // extend P3 forward
			    autoP3 = CurrentBar;
			
			    // optional: break condition
			    bool broken =
			        autoIsUp
			            ? Low[0] < Low[autoP1 - CurrentBar]   // rough condition
			            : High[0] > High[autoP1 - CurrentBar];
			
			    if (broken)
			    {
			        autoContainerActive = false;
			    }
			}
			
		    RefreshManualGeometrySnapshot();
			
			if (CurrentBar >= 1)
			{
			    var prevSnap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
			        Time[1],
			        Open[1],
			        High[1],
			        Low[1],
			        Close[1],
			        (long)Volume[1],
			        CurrentBar - 1);
			
			    var curSnap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
			        Time[0],
			        Open[0],
			        High[0],
			        Low[0],
			        Close[0],
			        (long)Volume[0],
			        CurrentBar);
			
			    autoMvpSnapshot =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaAutoContainerMvp.Step(
			            autoMvpState,
			            prevSnap,
			            curSnap);
			}
			
			UpdateAutoContainerMvp();
			
			if (latestManualSnapshot.HasValue && autoGeometrySnapshot.HasValue)
			{
			    var cmp =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaContainerComparer.Compare(
			            latestManualSnapshot.Value,
			            autoGeometrySnapshot.Value);
			
			    manualComparisonText =
			        $"CMP P1:{cmp.P1BarError} P2:{cmp.P2BarError} P3:{cmp.P3BarError} S:{cmp.RtlSlopeError:0.########}";
			    manualComparisonBarIndex = CurrentBar;
			}
			
			if (latestManualSnapshot.HasValue && autoGeometrySnapshot.HasValue)
			{
			    var cmp =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaContainerComparer.Compare(
			            latestManualSnapshot.Value,
			            autoGeometrySnapshot.Value);
			
			    manualComparisonText =
			        $"CMP P1:{cmp.P1BarError} P2:{cmp.P2BarError} P3:{cmp.P3BarError} S:{cmp.RtlSlopeError:0.########}";
			    manualComparisonBarIndex = CurrentBar;
			}
			
			NinjaTrader.NinjaScript.xPva.Engine.TurnType? latestTurnType = null;
			NinjaTrader.NinjaScript.xPva.Engine.TrendType? latestTrendType = null;
			
			if (manualHistoricalScanPending && manualGeometrySnapshot.HasValue && latestManualSnapshot.HasValue)
			{
			    var g = manualGeometrySnapshot.Value;
			    var manualSnapshot = latestManualSnapshot.Value;
				
			    var manualEvents =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualEventSource.BuildHistoricalEvents(
			            g,
			            manualSnapshot,
			            CurrentBar,
			            idx => Bars.GetHigh(idx),
			            idx => Bars.GetLow(idx),
			            idx => Bars.GetClose(idx),
			            TickSize);

				foreach (var me in manualEvents)
				{
				    var stateForEventBar = GetRiverState(me.BarIndex);
				    ProcessRiverEvent(me, stateForEventBar, ref latestTurnType, ref latestTrendType);
				}
			    
			    manualHistoricalScanPending = false;
			    lastManualInterpretedContainerId = manualSnapshot.ContainerId;
			    lastManualInterpretedP3BarIndex = manualSnapshot.P3.BarIndex;
			}
		
		    var snap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
		        Time[0],
		        Open[0],
		        High[0],
		        Low[0],
		        Close[0],
		        (long)Volume[0],
		        CurrentBar);
		
		    var evs = engine.Step(snap);
		    if (evs == null || evs.Events == null || evs.Events.Length == 0)
		        return;
			
			if (manualRuntimeState.HasActiveManualContainer)
			{
			    var liveCandidate =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerRuntime.CheckFttCandidate(
			            manualRuntimeState,
			            snap);
			
			    if (liveCandidate.HasValue && liveCandidate.Value.BarIndex != lastManualLiveCandidateBar)
			    {
			        lastManualLiveCandidateBar = liveCandidate.Value.BarIndex;
			
			        var candidateEvent = NinjaTrader.NinjaScript.xPva.Engine.EngineEvent.From(liveCandidate.Value);
			        var candidateState = GetRiverState(candidateEvent.BarIndex);
			        ProcessRiverEvent(candidateEvent, candidateState, ref latestTurnType, ref latestTrendType);
			    }
			
			    var liveConfirmed =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerRuntime.CheckFttConfirmed(
			            manualRuntimeState,
			            snap);
			
			    if (liveConfirmed.HasValue && liveConfirmed.Value.BarIndex != lastManualLiveConfirmedBar)
			    {
			        lastManualLiveConfirmedBar = liveConfirmed.Value.BarIndex;
			
			        var confirmedEvent = NinjaTrader.NinjaScript.xPva.Engine.EngineEvent.From(liveConfirmed.Value);
			        var confirmedState = GetRiverState(confirmedEvent.BarIndex);
			        ProcessRiverEvent(confirmedEvent, confirmedState, ref latestTurnType, ref latestTrendType);
			
			        var g = manualRuntimeState.ActiveContainer;
			
			        var structure =
			            NinjaTrader.NinjaScript.xPva.Engine.xPvaManualStructureResolver.Resolve(
			                g,
			                liveConfirmed.Value.BarIndex);
			
			        var structureEvent = NinjaTrader.NinjaScript.xPva.Engine.EngineEvent.From(structure);
			        ProcessRiverEvent(structureEvent, GetRiverState(structureEvent.BarIndex), ref latestTurnType, ref latestTrendType);
			
			        var action =
			            NinjaTrader.NinjaScript.xPva.Engine.xPvaManualActionResolver.Resolve(
			                g,
			                structure,
			                liveConfirmed.Value.BarIndex);
			
			        var actionEvent = NinjaTrader.NinjaScript.xPva.Engine.EngineEvent.From(action);
			        ProcessRiverEvent(actionEvent, GetRiverState(actionEvent.BarIndex), ref latestTurnType, ref latestTrendType);
			
			        var tradeIntent =
			            NinjaTrader.NinjaScript.xPva.Engine.xPvaManualTradeIntentResolver.Resolve(
			                g,
			                structure,
			                action,
			                liveConfirmed.Value.BarIndex);
			
			        var tradeIntentEvent = NinjaTrader.NinjaScript.xPva.Engine.EngineEvent.From(tradeIntent);
			        ProcessRiverEvent(tradeIntentEvent, GetRiverState(tradeIntentEvent.BarIndex), ref latestTurnType, ref latestTrendType);
					manualRuntimeState.HasActiveManualContainer = false;

			    }
			}
			
			if (autoMvpSnapshot.HasValue)
			{
			    var ag = autoMvpSnapshot.Value;
			    DrawGeometryPointsEvent(ag);
			    DrawRtl(ag);
			    DrawLtl(ag);
			    DrawVe1(ag);
			}
		
		    var currentState = GetRiverState(CurrentBar);
		
			foreach (var e in evs.Events)
			{
			    ProcessRiverEvent(e, currentState, ref latestTurnType, ref latestTrendType);
			}	
		    
		    string turnTrend = BuildTurnTrendToken(latestTurnType, latestTrendType);
		    if (!string.IsNullOrEmpty(turnTrend))
		        currentState.TurnTrendToken = MergeToken(currentState.TurnTrendToken, turnTrend);
		
		    DrawRiverBar(CurrentBar, currentState);
		}
		
		private void DrawManualVolumeLabels(
    NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot g)
		{
		    if (manualVolumeEvents == null || manualVolumeEvents.Length == 0)
		        return;
		
		    for (int i = 0; i < manualVolumeEvents.Length; i++)
		    {
		        var ve = manualVolumeEvents[i];
		        int barsAgo = BarsAgoFromIndex(ve.BarIndex);
		
		        if (barsAgo < 0 || barsAgo > CurrentBar)
		            continue;
		
		        string text = ve.Label.ToString();
		        Brush brush = Brushes.DarkOrange;
		
		        if (ve.Label == NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeLabel.P1)
		            brush = Brushes.Gold;
		        else if (ve.Label == NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeLabel.PP1)
		            brush = Brushes.OrangeRed;
		        else if (ve.Label == NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeLabel.Peak)
		            brush = Brushes.DodgerBlue;
		        else if (ve.Label == NinjaTrader.NinjaScript.xPva.Engine.ManualVolumeLabel.Trough)
		            brush = Brushes.LimeGreen;
		
		        double y = High[barsAgo] + 10 * TickSize;
		
		        Draw.Text(
		            this,
		            $"ManualVol_{g.ContainerId}_{ve.BarIndex}_{ve.Label}",
		            false,
		            text,
		            barsAgo,
		            y,
		            0,
		            brush,
		            new SimpleFont("Arial", FontSize),
		            TextAlignment.Center,
		            Brushes.Transparent,
		            Brushes.Transparent,
		            0);
		    }
		}
		
		private void DrawManualGeometrySnapshot()
		{
		    if (!manualGeometrySnapshot.HasValue)
		        return;
		
		    var g = manualGeometrySnapshot.Value;
		
		    DrawGeometryPointsEvent(g);
		    DrawRtl(g);
		    DrawLtl(g);
		    DrawVe1(g);
		    DrawGeometryStateLabel(g);
		
		    if (manualHistoricalCandidateBar.HasValue)
		        DrawManualHistoricalCandidate(g, manualHistoricalCandidateBar.Value);
		
		    if (manualHistoricalConfirmedBar.HasValue)
		        DrawManualHistoricalConfirmed(g, manualHistoricalConfirmedBar.Value);
		
		    DrawManualStatusLabel();
		    DrawManualComparisonLabel();
			DrawManualVolumeLabels(g);
		}
		
		private void DrawManualStatusLabel()
		{
		    if (string.IsNullOrEmpty(manualStatusText))
		        return;
		
		    int barIndex = manualStatusBarIndex >= 0 ? manualStatusBarIndex : CurrentBar;
		    int barsAgo = BarsAgoFromIndex(barIndex);
		
		    if (barsAgo < 0 || barsAgo > CurrentBar)
		        return;
		
		    Draw.Text(
		        this,
		        "xPvaManualStatus",
		        false,
		        manualStatusText,
		        barsAgo,
		        High[barsAgo] + 9 * TickSize,
		        0,
		        Brushes.Goldenrod,
		        new SimpleFont("Arial", FontSize - 1),
		        TextAlignment.Left,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void DrawGeometryPointsEvent(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometrySnapshot e)
		{
		    Brush brush = e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? Brushes.DodgerBlue
		        : Brushes.IndianRed;
		
		    if (e.P1.HasValue)
		    {
		        int barsAgo1 = BarsAgoFromIndex(e.P1.Value.BarIndex);
		        Draw.Text(
		            this,
		            string.Format("xPvaGeoP1_{0}_{1}", e.ContainerId, e.P1.Value.BarIndex),
		            false,
		            "1",
		            barsAgo1,
		            e.P1.Value.Price,
		            0,
		            brush,
		            new SimpleFont("Arial", FontSize),
		            TextAlignment.Center,
		            Brushes.Transparent,
		            Brushes.Transparent,
		            0);
		    }
		
		    if (e.P2.HasValue)
		    {
		        int barsAgo2 = BarsAgoFromIndex(e.P2.Value.BarIndex);
		        Draw.Text(
		            this,
		            string.Format("xPvaGeoP2_{0}_{1}", e.ContainerId, e.P2.Value.BarIndex),
		            false,
		            "2",
		            barsAgo2,
		            e.P2.Value.Price,
		            0,
		            brush,
		            new SimpleFont("Arial", FontSize),
		            TextAlignment.Center,
		            Brushes.Transparent,
		            Brushes.Transparent,
		            0);
		    }
		
		    if (e.P3.HasValue)
		    {
		        int barsAgo3 = BarsAgoFromIndex(e.P3.Value.BarIndex);
		        Draw.Text(
		            this,
		            string.Format("xPvaGeoP3_{0}_{1}", e.ContainerId, e.P3.Value.BarIndex),
		            false,
		            "3",
		            barsAgo3,
		            e.P3.Value.Price,
		            0,
		            brush,
		            new SimpleFont("Arial", FontSize),
		            TextAlignment.Center,
		            Brushes.Transparent,
		            Brushes.Transparent,
		            0);
		    }
		}

		private string TradeIntentToken(NinjaTrader.NinjaScript.xPva.Engine.TradeIntent intent)
		{
		    switch (intent)
		    {
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.Enter:
		            return "ENT";
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.Reverse:
		            return "REV";
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.Sideline:
		            return "SIDE";
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.HoldThru:
		            return "HT";
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.EarlyExit:
		            return "EE";
		        case NinjaTrader.NinjaScript.xPva.Engine.TradeIntent.ReEntry:
		            return "RE";
		        default:
		            return "";
		    }
		}
		
		private RiverBarState GetRiverState(int barIndex)
		{
		    if (!riverStates.TryGetValue(barIndex, out RiverBarState state))
		    {
		        state = new RiverBarState();
		        riverStates[barIndex] = state;
		    }
		
		    return state;
		}
		
		private string MergeToken(string existing, string next)
		{
		    if (string.IsNullOrEmpty(existing))
		        return next;
		
		    if (string.IsNullOrEmpty(next))
		        return existing;
		
		    if (existing.Contains(next))
		        return existing;
		
		    return existing + " " + next;
		}
		
		private string BuildTurnTrendToken(
		    NinjaTrader.NinjaScript.xPva.Engine.TurnType? turnType,
		    NinjaTrader.NinjaScript.xPva.Engine.TrendType? trendType)
		{
		    string t = turnType.HasValue ? turnType.Value.ToString() : "";
		    string tt = trendType.HasValue ? trendType.Value.ToString() : "";
		
		    if (string.IsNullOrEmpty(t) && string.IsNullOrEmpty(tt))
		        return "";
		
		    if (string.IsNullOrEmpty(t))
		        return tt;
		
		    if (string.IsNullOrEmpty(tt))
		        return t;
		
		    return tt + t;
		}

        private void DrawFttConfirmed(NinjaTrader.NinjaScript.xPva.Engine.FttConfirmedEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaRiverFTT_{0}_{1}", e.ContainerId, e.BarIndex);

            if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
            {
                Draw.ArrowDown(this, tag, false, barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Gold);
            }
            else if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Down)
            {
                Draw.ArrowUp(this, tag, false, barsAgo, Low[barsAgo] - 2 * TickSize, Brushes.Gold);
            }
        }

		private void DrawRiverBar(int barIndex, RiverBarState state)
		{
		    int barsAgo = BarsAgoFromIndex(barIndex);
			
			double actionY  = High[barsAgo] + 8 * TickSize;
			double midY     = Low[barsAgo]  - 10 * TickSize;
			double volumeY  = Low[barsAgo]  - 18 * TickSize;
			double contY    = High[barsAgo] + 4 * TickSize;
		
		    bool tradingMode = DisplayMode == 1;
			
			string upperToken = null;
			
			if (!string.IsNullOrEmpty(state.ManualDecisionToken))
			    upperToken = state.ManualDecisionToken;
			else if (!string.IsNullOrEmpty(state.TradeIntentToken))
			    upperToken = state.TradeIntentToken;
			else if (!string.IsNullOrEmpty(state.ActionToken))
			    upperToken = state.ActionToken;
			
		    // Upper lane: action
		    if (!string.IsNullOrEmpty(upperToken))
		    {
		        if (!tradingMode || !string.IsNullOrEmpty(state.ManualDecisionToken) || IsTradeRelevant(state.ActionType))
			    {
			        Draw.Text(
			            this,
			            string.Format("xPvaRiverActionLane_{0}", barIndex),
			            false,
			            upperToken,
			            barsAgo,
			            High[barsAgo] + 5 * TickSize,
			            0,
			            !string.IsNullOrEmpty(state.ManualDecisionToken)
			                ? Brushes.Goldenrod
			                : ActionBrush(state.ActionType ?? NinjaTrader.NinjaScript.xPva.Engine.ActionType.Unknown),
			            new SimpleFont("Arial", FontSize),
			            TextAlignment.Left,
			            Brushes.Transparent,
			            Brushes.Transparent,
			            0);
			    }
		    }
		
		    // Middle lane: trend/turn token
		    if (DrawMiddleLane && !string.IsNullOrEmpty(state.TurnTrendToken))
		    {
		        if (!tradingMode || !string.IsNullOrEmpty(state.ActionToken))
		        {
		            Draw.Text(
		                this,
		                string.Format("xPvaRiverMiddle_{0}", barIndex),
		                false,
		                state.TurnTrendToken,
		                barsAgo,
		                midY,
		                0,
		                Brushes.MediumPurple,
		                new SimpleFont("Arial", FontSize),
		                TextAlignment.Left,
		                Brushes.Transparent,
		                Brushes.Transparent,
		                0);
		        }
		    }
		
		    // Lower lane: volume
		    if (DrawVolumeLane && !string.IsNullOrEmpty(state.VolumeToken))
		    {
		        if (!tradingMode || !string.IsNullOrEmpty(state.ActionToken))
		        {
		            Draw.Text(
		                this,
		                string.Format("xPvaRiverLower_{0}", barIndex),
		                false,
		                state.VolumeToken,
		                barsAgo,
		                volumeY,
		                0,
		                Brushes.DeepSkyBlue,
		                new SimpleFont("Arial", FontSize),
		                TextAlignment.Left,
		                Brushes.Transparent,
		                Brushes.Transparent,
		                0);
		        }
		    }
		
		    // Container lane
		    if (DrawContainerLane && !string.IsNullOrEmpty(state.ContainerToken))
		    {
		        if (!tradingMode || !string.IsNullOrEmpty(state.ActionToken))
		        {
		            Draw.Text(
		                this,
		                string.Format("xPvaRiverContainer_{0}", barIndex),
		                false,
		                state.ContainerToken,
		                barsAgo,
		                contY,
		                0,
		                Brushes.LightGray,
		                new SimpleFont("Arial", FontSize - 1),
		                TextAlignment.Left,
		                Brushes.Transparent,
		                Brushes.Transparent,
		                0);
		        }
		    }
			
			if (state.ManualHasFtt)
			{
			    Draw.Text(
			        this,
			        string.Format("xPvaRiverManualFtt_{0}", barIndex),
			        false,
			        "FTT",
			        barsAgo,
			        High[barsAgo] + 3 * TickSize,
			        0,
			        Brushes.Gold,
			        new SimpleFont("Arial", FontSize - 1),
			        TextAlignment.Center,
			        Brushes.Transparent,
			        Brushes.Transparent,
			        0);
			}
		}
		
		private bool IsTradeRelevant(NinjaTrader.NinjaScript.xPva.Engine.ActionType? action)
		{
		    if (!action.HasValue)
		        return false;
		
		    switch (action.Value)
		    {
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
		            return true;
		
		        default:
		            return false;
		    }
		}
		
        private void DrawActionBox(NinjaTrader.NinjaScript.xPva.Engine.ActionEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaRiverAction_{0}_{1}", e.ContainerId, e.BarIndex);

            string text = ActionToken(e.Action);
            Brush brush = ActionBrush(e.Action);

            double y = High[barsAgo] + 4 * TickSize;

            Draw.Text(
                this,
                tag,
                false,
                text,
                barsAgo,
                y,
                0,
                brush,
                new SimpleFont("Arial", FontSize),
                TextAlignment.Left,
                Brushes.Transparent,
                Brushes.Black,
                0);
        }

        private void DrawContainerId(NinjaTrader.NinjaScript.xPva.Engine.ContainerReportEvent e)
        {
            int anchorBarIndex = e.FttConfirmedBarIndex ?? e.StartBarIndex;
            int barsAgo = BarsAgoFromIndex(anchorBarIndex);
            string tag = string.Format("xPvaRiverCID_{0}_{1}", e.ContainerId, anchorBarIndex);

            Draw.Text(
                this,
                tag,
                false,
                "C#" + e.ContainerId,
                barsAgo,
                High[barsAgo] + 1 * TickSize,
                0,
                Brushes.White,
                new SimpleFont("Arial", FontSize),
                TextAlignment.Left,
                Brushes.Transparent,
                Brushes.Transparent,
                0);
        }

        private void DrawContainerGeometryEvent(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometryEvent e)
        {
            int startBarsAgo = BarsAgoFromIndex(e.StartBarIndex);
            int extremeBarsAgo = BarsAgoFromIndex(e.ExtremeBarIndex);
            int confirmBarsAgo = BarsAgoFromIndex(e.ConfirmBarIndex);

            string rtlTag = string.Format("xPvaRiverGeoRTL_{0}", e.ContainerId);
            string diagTag = string.Format("xPvaRiverGeoDiag_{0}", e.ContainerId);

            Brush brush = e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
                ? Brushes.DodgerBlue
                : Brushes.IndianRed;

            Draw.Line(
                this,
                rtlTag,
                false,
                startBarsAgo,
                e.StartPrice,
                confirmBarsAgo,
                e.ConfirmPrice,
                brush,
                NinjaTrader.Gui.DashStyleHelper.Solid,
                2);

            Draw.Line(
                this,
                diagTag,
                false,
                extremeBarsAgo,
                e.ExtremePrice,
                confirmBarsAgo,
                e.ConfirmPrice,
                brush,
                NinjaTrader.Gui.DashStyleHelper.Dot,
                1);
        }

        private string ActionToken(NinjaTrader.NinjaScript.xPva.Engine.ActionType action)
        {
            switch (action)
            {
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
                    return "ENT";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
                    return "REV";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
                    return "SIDE";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
                    return "HOLD";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
                    return "IN";
                default:
                    return "?";
            }
        }

        private Brush ActionBrush(NinjaTrader.NinjaScript.xPva.Engine.ActionType action)
        {
            switch (action)
            {
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
                    return Brushes.LimeGreen;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
                    return Brushes.OrangeRed;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
                    return Brushes.Gray;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
                    return Brushes.DeepSkyBlue;
                default:
                    return Brushes.White;
            }
        }

        private string FormatEventLine(NinjaTrader.NinjaScript.xPva.Engine.EngineEvent e)
        {
            return string.Format("{0} B{1}: {2} {3}", Instrument.FullName, e.BarIndex, e.Kind, e.Text);
        }

        private int BarsAgoFromIndex(int eventBarIndex)
        {
            int barsAgo = CurrentBar - eventBarIndex;
            if (barsAgo < 0)
                barsAgo = 0;
            return barsAgo;
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaRiverIndicator[] cachexPvaRiverIndicator;
		public xPvaRiverIndicator xPvaRiverIndicator(int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			return xPvaRiverIndicator(Input, volPivotWindow, printEvents, drawFtt, drawActionBoxes, drawTurnTrendLane, drawOoeLane, drawContainerIds, drawGeometry, displayMode, drawVolumeLane, drawMiddleLane, drawContainerLane, fontSize, drawGeometryPoints);
		}

		public xPvaRiverIndicator xPvaRiverIndicator(ISeries<double> input, int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			if (cachexPvaRiverIndicator != null)
				for (int idx = 0; idx < cachexPvaRiverIndicator.Length; idx++)
					if (cachexPvaRiverIndicator[idx] != null && cachexPvaRiverIndicator[idx].VolPivotWindow == volPivotWindow && cachexPvaRiverIndicator[idx].PrintEvents == printEvents && cachexPvaRiverIndicator[idx].DrawFtt == drawFtt && cachexPvaRiverIndicator[idx].DrawActionBoxes == drawActionBoxes && cachexPvaRiverIndicator[idx].DrawTurnTrendLane == drawTurnTrendLane && cachexPvaRiverIndicator[idx].DrawOoeLane == drawOoeLane && cachexPvaRiverIndicator[idx].DrawContainerIds == drawContainerIds && cachexPvaRiverIndicator[idx].DrawGeometry == drawGeometry && cachexPvaRiverIndicator[idx].DisplayMode == displayMode && cachexPvaRiverIndicator[idx].DrawVolumeLane == drawVolumeLane && cachexPvaRiverIndicator[idx].DrawMiddleLane == drawMiddleLane && cachexPvaRiverIndicator[idx].DrawContainerLane == drawContainerLane && cachexPvaRiverIndicator[idx].FontSize == fontSize && cachexPvaRiverIndicator[idx].DrawGeometryPoints == drawGeometryPoints && cachexPvaRiverIndicator[idx].EqualsInput(input))
						return cachexPvaRiverIndicator[idx];
			return CacheIndicator<xPvaRiverIndicator>(new xPvaRiverIndicator(){ VolPivotWindow = volPivotWindow, PrintEvents = printEvents, DrawFtt = drawFtt, DrawActionBoxes = drawActionBoxes, DrawTurnTrendLane = drawTurnTrendLane, DrawOoeLane = drawOoeLane, DrawContainerIds = drawContainerIds, DrawGeometry = drawGeometry, DisplayMode = displayMode, DrawVolumeLane = drawVolumeLane, DrawMiddleLane = drawMiddleLane, DrawContainerLane = drawContainerLane, FontSize = fontSize, DrawGeometryPoints = drawGeometryPoints }, input, ref cachexPvaRiverIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaRiverIndicator xPvaRiverIndicator(int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			return indicator.xPvaRiverIndicator(Input, volPivotWindow, printEvents, drawFtt, drawActionBoxes, drawTurnTrendLane, drawOoeLane, drawContainerIds, drawGeometry, displayMode, drawVolumeLane, drawMiddleLane, drawContainerLane, fontSize, drawGeometryPoints);
		}

		public Indicators.xPvaRiverIndicator xPvaRiverIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			return indicator.xPvaRiverIndicator(input, volPivotWindow, printEvents, drawFtt, drawActionBoxes, drawTurnTrendLane, drawOoeLane, drawContainerIds, drawGeometry, displayMode, drawVolumeLane, drawMiddleLane, drawContainerLane, fontSize, drawGeometryPoints);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaRiverIndicator xPvaRiverIndicator(int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			return indicator.xPvaRiverIndicator(Input, volPivotWindow, printEvents, drawFtt, drawActionBoxes, drawTurnTrendLane, drawOoeLane, drawContainerIds, drawGeometry, displayMode, drawVolumeLane, drawMiddleLane, drawContainerLane, fontSize, drawGeometryPoints);
		}

		public Indicators.xPvaRiverIndicator xPvaRiverIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFtt, bool drawActionBoxes, bool drawTurnTrendLane, bool drawOoeLane, bool drawContainerIds, bool drawGeometry, int displayMode, bool drawVolumeLane, bool drawMiddleLane, bool drawContainerLane, int fontSize, bool drawGeometryPoints)
		{
			return indicator.xPvaRiverIndicator(input, volPivotWindow, printEvents, drawFtt, drawActionBoxes, drawTurnTrendLane, drawOoeLane, drawContainerIds, drawGeometry, displayMode, drawVolumeLane, drawMiddleLane, drawContainerLane, fontSize, drawGeometryPoints);
		}
	}
}

#endregion
