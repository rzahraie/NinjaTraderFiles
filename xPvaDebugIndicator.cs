#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaDebugIndicator : Indicator
    {
        private NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine engine;

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "VolPivotWindow", Order = 1, GroupName = "Parameters")]
        public int VolPivotWindow { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print Events", Order = 2, GroupName = "Parameters")]
        public bool PrintEvents { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw FTT Markers", Order = 3, GroupName = "Parameters")]
        public bool DrawFttMarkers { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw EndEffect Markers", Order = 4, GroupName = "Parameters")]
        public bool DrawEndEffectMarkers { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Turn Markers", Order = 5, GroupName = "Parameters")]
        public bool DrawTurnMarkers { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw TrendType Markers", Order = 6, GroupName = "Parameters")]
        public bool DrawTrendTypeMarkers { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Action Markers", Order = 7, GroupName = "Parameters")]
        public bool DrawActionMarkers { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Structure Markers", Order = 8, GroupName = "Parameters")]
		public bool DrawStructureMarkers { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Print Container Reports", Order = 9, GroupName = "Parameters")]
		public bool PrintContainerReports { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Container Reports", Order = 10, GroupName = "Parameters")]
		public bool DrawContainerReports { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Persistent Containers", Order = 11, GroupName = "Parameters")]
		public bool DrawPersistentContainers { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Draw Container Geometry", Order = 12, GroupName = "Parameters")]
		public bool DrawContainerGeometry { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaDebugIndicator";
                Description = "Debug/inspection indicator for xPvaEngine event stream.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                VolPivotWindow = 1;
                PrintEvents = true;
                DrawFttMarkers = true;
                DrawEndEffectMarkers = false;
                DrawTurnMarkers = false;
                DrawTrendTypeMarkers = false;
                DrawActionMarkers = true;
				DrawStructureMarkers = true;
				PrintContainerReports = true;
				DrawContainerReports = true;
				DrawPersistentContainers = false;
				DrawContainerGeometry = true;
            }
            else if (State == State.DataLoaded)
            {
                engine = new NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine(VolPivotWindow);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || engine == null)
                return;

            DateTime timeUtc = DateTime.SpecifyKind(Time[0], DateTimeKind.Local).ToUniversalTime();

            var snap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
                timeUtc,
                Open[0],
                High[0],
                Low[0],
                Close[0],
                (long)Volume[0],
                CurrentBar);

            NinjaTrader.NinjaScript.xPva.Engine.EngineEvents evs = engine.Step(snap);

            if (evs == null || evs.Events == null || evs.Events.Length == 0)
                return;

            foreach (var e in evs.Events)
            {
                if (PrintEvents)
                    Print(FormatEventLine(e));

                switch (e.Kind)
                {
                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.FttConfirmed:
					    if (DrawFttMarkers && e.FttConfirmed.HasValue)
					        DrawFttConfirmed(e.FttConfirmed.Value);
					    break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.EndEffect:
                        if (DrawEndEffectMarkers && e.EndEffect.HasValue)
                            DrawEndEffect(e.EndEffect.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Turn:
                        if (DrawTurnMarkers && e.Turn.HasValue)
                            DrawTurn(e.Turn.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.TrendType:
                        if (DrawTrendTypeMarkers && e.TrendType.HasValue)
                            DrawTrendType(e.TrendType.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Action:
                        if (DrawActionMarkers && e.Action.HasValue)
                            DrawAction(e.Action.Value);
                        break;
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Structure:
					    if (DrawStructureMarkers && e.Structure.HasValue)
					        DrawStructure(e.Structure.Value);
					    break;
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerReport:
					    if (DrawContainerReports && e.ContainerReport.HasValue)
					        DrawContainerReport(e.ContainerReport.Value);
					    break;
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.PersistentContainer:
					    if (DrawPersistentContainers && e.PersistentContainer.HasValue)
					        DrawPersistentContainer(e.PersistentContainer.Value);
					    break;
						
					case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerGeometry:
					    if (DrawContainerGeometry && e.ContainerGeometry.HasValue)
					        DrawContainerGeometryEvent(e.ContainerGeometry.Value);
					    break;
                }
            }
        }
		
		private void DrawContainerGeometryEvent(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometryEvent e)
		{
		    int startBarsAgo = BarsAgoFromIndex(e.StartBarIndex);
		    int extremeBarsAgo = BarsAgoFromIndex(e.ExtremeBarIndex);
		    int confirmBarsAgo = BarsAgoFromIndex(e.ConfirmBarIndex);
		
		    string rtlTag = string.Format("xPvaGeoRTL_{0}", e.ContainerId);
		    string diagTag = string.Format("xPvaGeoDiag_{0}", e.ContainerId);
		    string txtTag = string.Format("xPvaGeoTxt_{0}", e.ContainerId);
		
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
		        DashStyleHelper.Solid,
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
		        DashStyleHelper.Dot,
		        1);
		
		    Draw.Text(
		        this,
		        txtTag,
		        false,
		        string.Format("CG#{0}", e.ContainerId),
		        confirmBarsAgo,
		        e.ConfirmPrice + 3 * TickSize,
		        0,
		        brush,
		        new SimpleFont("Arial", 10),
		        System.Windows.TextAlignment.Left,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}

		private void DrawPersistentContainer(NinjaTrader.NinjaScript.xPva.Engine.PersistentContainerEvent e)
		{
		    int barsAgo = BarsAgoFromIndex(e.LastBarIndex);
		    string tag = string.Format("xPvaPC_{0}_{1}_{2}", e.ContainerId, e.LifecycleState, e.LastBarIndex);
		
		    string text = string.Format(
		        "PC#{0}\n{1}\nX:{2}",
		        e.ContainerId,
		        e.LifecycleState,
		        e.ExtremeBarIndex);
		
		    double y = e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
		        ? High[barsAgo] + 6 * TickSize
		        : Low[barsAgo] - 6 * TickSize;
		
		    Draw.Text(
		        this,
		        tag,
		        false,
		        text,
		        barsAgo,
		        y,
		        0,
		        Brushes.Goldenrod,
		        new SimpleFont("Arial", 9),
		        System.Windows.TextAlignment.Left,
		        Brushes.Transparent,
		        Brushes.Transparent,
		        0);
		}
		
		private void DrawContainerReport(NinjaTrader.NinjaScript.xPva.Engine.ContainerReportEvent e)
		{
		    int anchorBarIndex = e.FttConfirmedBarIndex ?? e.StartBarIndex;
		    int barsAgo = BarsAgoFromIndex(anchorBarIndex);
		
		    string tag = string.Format("xPvaReport_{0}_{1}", e.ContainerId, anchorBarIndex);
		
		    string text = string.Format(
		        "C#{0}\n{1}\n{2}",
		        e.ContainerId,
		        e.StructureState,
		        e.ActionType);
		
		    double y = High[barsAgo] + 4 * TickSize;
		
		    Brush brush = Brushes.White;
		
		    switch (e.ActionType)
		    {
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
		            brush = Brushes.LimeGreen;
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
		            brush = Brushes.OrangeRed;
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
		            brush = Brushes.Gray;
		            break;
		
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
		        case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
		            brush = Brushes.DeepSkyBlue;
		            break;
		    }
		
		    Draw.Text(
		        this,
		        tag,
		        false,
		        text,
		        barsAgo,
		        y,
		        0,
		        brush,
		        new SimpleFont("Arial", 11),
		        System.Windows.TextAlignment.Left,
		        Brushes.Transparent,
		        Brushes.Black,
		        0);
		}

		private void DrawStructure(NinjaTrader.NinjaScript.xPva.Engine.StructureEvent e)
		{
		    int barsAgo = BarsAgoFromIndex(e.BarIndex);
		    string tag = string.Format("xPvaStruct_{0}_{1}", e.BarIndex, e.State);
		    string text = string.Format("S:{0}", e.State);
		
		    Draw.Text(this, tag, false, text, barsAgo, Close[barsAgo], 0,
		        Brushes.White, new SimpleFont("Arial", 10),
		        System.Windows.TextAlignment.Left,
		        Brushes.Transparent, Brushes.Black, 0);
		}
		
        private string FormatEventLine(NinjaTrader.NinjaScript.xPva.Engine.EngineEvent e)
        {
            return string.Format(
                "{0} B{1}: {2} {3}",
                Instrument.FullName,
                e.BarIndex,
                e.Kind,
                e.Text);
        }

        private int BarsAgoFromIndex(int eventBarIndex)
        {
            int barsAgo = CurrentBar - eventBarIndex;
            if (barsAgo < 0)
                barsAgo = 0;
            return barsAgo;
        }

        private void DrawFttConfirmed(NinjaTrader.NinjaScript.xPva.Engine.FttConfirmedEvent e)
		{
		    int barsAgo = BarsAgoFromIndex(e.BarIndex);
		    string tag = string.Format("xPvaFTTCONF_{0}_{1}", e.BarIndex, e.PriorDirection);
		
		    if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
		    {
		        Draw.ArrowDown(this, tag, false, barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Gold);
		    }
		    else if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Down)
		    {
		        Draw.ArrowUp(this, tag, false, barsAgo, Low[barsAgo] - 2 * TickSize, Brushes.Gold);
		    }
		}

        private void DrawEndEffect(NinjaTrader.NinjaScript.xPva.Engine.EndEffectEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaEE_{0}_{1}_{2}", e.BarIndex, e.Kind, e.Source);
            string text = string.Format("EE:{0}", e.Kind);

            Draw.Text(this, tag, false, text, barsAgo, High[barsAgo] + 3 * TickSize, 0, Brushes.DeepSkyBlue, new SimpleFont("Arial", 10), TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawTurn(NinjaTrader.NinjaScript.xPva.Engine.TurnEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaTurn_{0}_{1}", e.BarIndex, e.Type);
            string text = string.Format("T{0}", e.Type);

            Draw.Text(this, tag, false, text, barsAgo, Low[barsAgo] - 3 * TickSize, 0, Brushes.Orange, new SimpleFont("Arial", 10), TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawTrendType(NinjaTrader.NinjaScript.xPva.Engine.TrendTypeEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaTrend_{0}_{1}", e.BarIndex, e.Type);
            string text = string.Format("TT:{0}", e.Type);

            Draw.Text(this, tag, false, text, barsAgo, High[barsAgo] + 5 * TickSize, 0, Brushes.MediumPurple, new SimpleFont("Arial", 10), TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void DrawAction(NinjaTrader.NinjaScript.xPva.Engine.ActionEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaAction_{0}_{1}", e.BarIndex, e.Action);

            switch (e.Action)
            {
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
                    Draw.Dot(this, tag, false, barsAgo, Low[barsAgo] - 2 * TickSize, Brushes.LimeGreen);
                    break;

                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
                    Draw.Dot(this, tag, false, barsAgo, Close[barsAgo], Brushes.DodgerBlue);
                    break;

                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
                    Draw.Diamond(this, tag, false, barsAgo, Close[barsAgo], Brushes.Red);
                    break;

                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
                    Draw.Square(this, tag, false, barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Gray);
                    break;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaDebugIndicator[] cachexPvaDebugIndicator;
		public xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			return xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers, drawStructureMarkers, printContainerReports, drawContainerReports, drawPersistentContainers, drawContainerGeometry);
		}

		public xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input, int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			if (cachexPvaDebugIndicator != null)
				for (int idx = 0; idx < cachexPvaDebugIndicator.Length; idx++)
					if (cachexPvaDebugIndicator[idx] != null && cachexPvaDebugIndicator[idx].VolPivotWindow == volPivotWindow && cachexPvaDebugIndicator[idx].PrintEvents == printEvents && cachexPvaDebugIndicator[idx].DrawFttMarkers == drawFttMarkers && cachexPvaDebugIndicator[idx].DrawEndEffectMarkers == drawEndEffectMarkers && cachexPvaDebugIndicator[idx].DrawTurnMarkers == drawTurnMarkers && cachexPvaDebugIndicator[idx].DrawTrendTypeMarkers == drawTrendTypeMarkers && cachexPvaDebugIndicator[idx].DrawActionMarkers == drawActionMarkers && cachexPvaDebugIndicator[idx].DrawStructureMarkers == drawStructureMarkers && cachexPvaDebugIndicator[idx].PrintContainerReports == printContainerReports && cachexPvaDebugIndicator[idx].DrawContainerReports == drawContainerReports && cachexPvaDebugIndicator[idx].DrawPersistentContainers == drawPersistentContainers && cachexPvaDebugIndicator[idx].DrawContainerGeometry == drawContainerGeometry && cachexPvaDebugIndicator[idx].EqualsInput(input))
						return cachexPvaDebugIndicator[idx];
			return CacheIndicator<xPvaDebugIndicator>(new xPvaDebugIndicator(){ VolPivotWindow = volPivotWindow, PrintEvents = printEvents, DrawFttMarkers = drawFttMarkers, DrawEndEffectMarkers = drawEndEffectMarkers, DrawTurnMarkers = drawTurnMarkers, DrawTrendTypeMarkers = drawTrendTypeMarkers, DrawActionMarkers = drawActionMarkers, DrawStructureMarkers = drawStructureMarkers, PrintContainerReports = printContainerReports, DrawContainerReports = drawContainerReports, DrawPersistentContainers = drawPersistentContainers, DrawContainerGeometry = drawContainerGeometry }, input, ref cachexPvaDebugIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			return indicator.xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers, drawStructureMarkers, printContainerReports, drawContainerReports, drawPersistentContainers, drawContainerGeometry);
		}

		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			return indicator.xPvaDebugIndicator(input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers, drawStructureMarkers, printContainerReports, drawContainerReports, drawPersistentContainers, drawContainerGeometry);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			return indicator.xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers, drawStructureMarkers, printContainerReports, drawContainerReports, drawPersistentContainers, drawContainerGeometry);
		}

		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers, bool drawStructureMarkers, bool printContainerReports, bool drawContainerReports, bool drawPersistentContainers, bool drawContainerGeometry)
		{
			return indicator.xPvaDebugIndicator(input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers, drawStructureMarkers, printContainerReports, drawContainerReports, drawPersistentContainers, drawContainerGeometry);
		}
	}
}

#endregion
