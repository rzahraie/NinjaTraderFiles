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
		
		    int barsAgo1 = BarsAgoFromIndex(g.P1.Value.BarIndex);
		
		    Brush brush = g.Direction ==
		        NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? Brushes.Blue
		        : Brushes.Red;
		
		    Draw.Line(
		        this,
		        $"RTL_{g.ContainerId}",     // stable tag
		        false,
		        barsAgo1,
		        g.P1.Value.Price,
		        0,
		        g.P3.Value.Price,
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
		
		    if (i3 == i1)
		        return;
		
		    double slope = (p3 - p1) / (double)(i3 - i1);
		
		    double rtlAtP2 = p1 + slope * (i2 - i1);
			double ltlAtP2 = p2;   // because LTL passes through P2
			double width = Math.Abs(ltlAtP2 - rtlAtP2);
		
		    if (width <= 0)
		        return;
		
		    double ltlNow = p2 + slope * (CurrentBar - i2);

			double veStart = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
			    ? p2 + width
			    : p2 - width;
			
			double veNow = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
			    ? ltlNow + width
			    : ltlNow - width;
		
		    int barsAgo2 = BarsAgoFromIndex(i2);
		
		    Brush brush = g.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
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
		
		private void RefreshManualGeometrySnapshot()
		{
		    NinjaTrader.NinjaScript.xPva.Engine.ManualContainerSnapshot manualSnapshot;
		    int version;
		
		    if (NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerBridge.TryGetLatest(out manualSnapshot, out version))
		    {
		        if (version != lastManualSnapshotVersion)
		        {
		            manualGeometrySnapshot =
		                NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerAdapter.FromManual(
		                    manualSnapshot,
		                    CurrentBar);
		
		            lastManualSnapshotVersion = version;
		
		            var g = manualGeometrySnapshot.Value;
		            Print($"[River] manual C#{g.ContainerId} state={g.State} P1={g.P1.HasValue} P2={g.P2.HasValue} P3={g.P3.HasValue}");
		        }
		    }
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
		    RefreshManualGeometrySnapshot();
		    base.OnRender(chartControl, chartScale);
		}
		
        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || engine == null)
                return;
			
			Print($"[River] OnBarUpdate CurrentBar={CurrentBar}");
			
			RefreshManualGeometrySnapshot();
			
			var currentState = GetRiverState(CurrentBar);

			NinjaTrader.NinjaScript.xPva.Engine.TurnType? latestTurnType = null;
			NinjaTrader.NinjaScript.xPva.Engine.TrendType? latestTrendType = null;

            DateTime timeUtc = DateTime.SpecifyKind(Time[0], DateTimeKind.Local).ToUniversalTime();
			
			NinjaTrader.NinjaScript.xPva.Engine.ManualContainerSnapshot manualSnapshot;
			if (NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerBridge.TryGetLatest(out manualSnapshot, out lastManualSnapshotVersion))
			{
			    manualGeometrySnapshot =
			        NinjaTrader.NinjaScript.xPva.Engine.xPvaManualContainerAdapter.FromManual(
			            manualSnapshot,
			            CurrentBar);
			
			    var g = manualGeometrySnapshot.Value;
			    Print($"[River] manual C#{g.ContainerId} state={g.State} P1={g.P1.HasValue} P2={g.P2.HasValue} P3={g.P3.HasValue}");
			}
			
			if (manualGeometrySnapshot.HasValue)
			{
			    var g = manualGeometrySnapshot.Value;
			    Print($"[River] manual C#{g.ContainerId} state={g.State} P1={g.P1.HasValue} P2={g.P2.HasValue} P3={g.P3.HasValue}");
			}

            var snap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
                timeUtc,
                Open[0],
                High[0],
                Low[0],
                Close[0],
                (long)Volume[0],
                CurrentBar);

            var evs = engine.Step(snap);
            if (evs == null || evs.Events == null || evs.Events.Length == 0)
                return;

            foreach (var e in evs.Events)
			{
			    if (PrintEvents)
			        Print(FormatEventLine(e));
			
			    switch (e.Kind)
			    {
			        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.FttConfirmed:
			            if (e.FttConfirmed.HasValue)
			            {
			                currentState.HasFtt = true;
			                currentState.FttPriorDirection = e.FttConfirmed.Value.PriorDirection;
			
			                if (DrawFtt)
			                    DrawFttConfirmed(e.FttConfirmed.Value);
			            }
			            break;
			
			        case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Action:
			            if (e.Action.HasValue)
			            {
			                currentState.ActionType = e.Action.Value.Action;
			                currentState.ActionToken = ActionToken(e.Action.Value.Action);
			
			                if (DisplayMode == 0 && DrawActionBoxes)
			                    DrawActionBox(e.Action.Value);
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
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.TradeIntent:
					    if (e.TradeIntent.HasValue)
					        currentState.TradeIntentToken = TradeIntentToken(e.TradeIntent.Value.Intent);
					    break;
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerGeometrySnapshot:
					    if (DrawGeometryPoints && e.ContainerGeometrySnapshot.HasValue)
					    {
					        var g = e.ContainerGeometrySnapshot.Value;
					
					        DrawGeometryPointsEvent(g);
					
					        if (g.State == NinjaTrader.NinjaScript.xPva.Engine.GeometryState.Active)
					        {
								DrawGeometryPointsEvent(g);
					            DrawRtl(g);
					            DrawLtl(g);
					            DrawVe1(g);
								DrawGeometryStateLabel(g);
					        }
					
					        DrawGeometryStateLabel(g);
					    }
					    break;
			    }
			}
			
			string turnTrend = BuildTurnTrendToken(latestTurnType, latestTrendType);
			if (!string.IsNullOrEmpty(turnTrend))
			    currentState.TurnTrendToken = MergeToken(currentState.TurnTrendToken, turnTrend);

            DrawRiverBar(CurrentBar, currentState);
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
		
		    // Upper lane: action
		    if (!string.IsNullOrEmpty(state.ActionToken))
		    {
		        if (!tradingMode || IsTradeRelevant(state.ActionType))
		        {
		            Draw.Text(
		                this,
		                string.Format("xPvaRiverActionLane_{0}", barIndex),
		                false,
		                state.ActionToken,
		                barsAgo,
		                actionY,
		                0,
		                ActionBrush(state.ActionType ?? NinjaTrader.NinjaScript.xPva.Engine.ActionType.Unknown),
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
