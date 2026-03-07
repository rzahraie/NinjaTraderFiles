#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
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
                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Ftt:
                        if (DrawFttMarkers && e.Ftt.HasValue)
                            DrawFtt(e.Ftt.Value);
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
                }
            }
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

        private void DrawFtt(NinjaTrader.NinjaScript.xPva.Engine.FttEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaFTT_{0}_{1}", e.BarIndex, e.Direction);

            if (e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
            {
                Draw.ArrowDown(this, tag, false, barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Gold);
            }
            else if (e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Down)
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
		public xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			return xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers);
		}

		public xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input, int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			if (cachexPvaDebugIndicator != null)
				for (int idx = 0; idx < cachexPvaDebugIndicator.Length; idx++)
					if (cachexPvaDebugIndicator[idx] != null && cachexPvaDebugIndicator[idx].VolPivotWindow == volPivotWindow && cachexPvaDebugIndicator[idx].PrintEvents == printEvents && cachexPvaDebugIndicator[idx].DrawFttMarkers == drawFttMarkers && cachexPvaDebugIndicator[idx].DrawEndEffectMarkers == drawEndEffectMarkers && cachexPvaDebugIndicator[idx].DrawTurnMarkers == drawTurnMarkers && cachexPvaDebugIndicator[idx].DrawTrendTypeMarkers == drawTrendTypeMarkers && cachexPvaDebugIndicator[idx].DrawActionMarkers == drawActionMarkers && cachexPvaDebugIndicator[idx].EqualsInput(input))
						return cachexPvaDebugIndicator[idx];
			return CacheIndicator<xPvaDebugIndicator>(new xPvaDebugIndicator(){ VolPivotWindow = volPivotWindow, PrintEvents = printEvents, DrawFttMarkers = drawFttMarkers, DrawEndEffectMarkers = drawEndEffectMarkers, DrawTurnMarkers = drawTurnMarkers, DrawTrendTypeMarkers = drawTrendTypeMarkers, DrawActionMarkers = drawActionMarkers }, input, ref cachexPvaDebugIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			return indicator.xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers);
		}

		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			return indicator.xPvaDebugIndicator(input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			return indicator.xPvaDebugIndicator(Input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers);
		}

		public Indicators.xPvaDebugIndicator xPvaDebugIndicator(ISeries<double> input , int volPivotWindow, bool printEvents, bool drawFttMarkers, bool drawEndEffectMarkers, bool drawTurnMarkers, bool drawTrendTypeMarkers, bool drawActionMarkers)
		{
			return indicator.xPvaDebugIndicator(input, volPivotWindow, printEvents, drawFttMarkers, drawEndEffectMarkers, drawTurnMarkers, drawTrendTypeMarkers, drawActionMarkers);
		}
	}
}

#endregion
