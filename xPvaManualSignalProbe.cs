#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.xPva.Engine;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaManualSignalProbe : Indicator
    {
        private ManualSignalState _signalState;
        private ManualPositionState _positionState;

        private int _lastBridgeVersion = -1;
        private int _lastPrintedBar = -1;

        [NinjaScriptProperty]
        [Display(Name = "Print Every Bar", Order = 1, GroupName = "Parameters")]
        public bool PrintEveryBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print Only On Change", Order = 2, GroupName = "Parameters")]
        public bool PrintOnlyOnChange { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Fixed Banner", Order = 3, GroupName = "Parameters")]
        public bool ShowFixedBanner { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaManualSignalProbe";
                Description = "Direct probe for manual analysis, signal, and execution states.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                PrintEveryBar = true;
                PrintOnlyOnChange = false;
                ShowFixedBanner = true;
            }
            else if (State == State.DataLoaded)
            {
                _signalState = new ManualSignalState();
                _positionState = new ManualPositionState();
                Print("[Probe] DataLoaded");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

			Print($"[Probe] alive B{CurrentBar}");
			
            if (ShowFixedBanner)
            {
                Draw.TextFixed(
                    this,
                    "xPvaManualSignalProbeBanner",
                    "MANUAL SIGNAL PROBE ACTIVE",
                    TextPosition.TopLeft,
                    Brushes.Gold,
                    new SimpleFont("Arial", 16),
                    Brushes.Transparent,
                    Brushes.Transparent,
                    0);
            }

            ManualContainerAnalysis analysis;
            int version;

            if (!xPvaManualContainerBridge.TryGetLatest(out analysis, out version))
            {
                if (PrintEveryBar && _lastPrintedBar != CurrentBar)
                {
                    Print($"[Probe] B{CurrentBar} no manual analysis available");
                    _lastPrintedBar = CurrentBar;
                }
                return;
            }

            // Evaluate signal directly from the latest manual analysis
            ManualSignalDecision sig =
                xPvaManualSignalEngine.Evaluate(analysis, _signalState);

            // Build a simple trade plan from current container geometry
            double? entryPrice = null;
            double? stopPrice = null;
            double? targetPrice = null;

            TryBuildSimpleTradePlan(analysis, out entryPrice, out stopPrice, out targetPrice);

            // Evaluate execution directly from the signal decision
            ManualExecutionDecision exec =
                xPvaManualExecutionEngine.Evaluate(
                    sig,
                    analysis,
                    _positionState,
                    entryPrice,
                    stopPrice,
                    targetPrice);

            bool structureBroken =
                analysis.StructureState.HasValue &&
                analysis.StructureState.Value == StructureState.Broken;

            bool printThisBar = false;

            if (PrintEveryBar)
                printThisBar = true;

            if (version != _lastBridgeVersion)
                printThisBar = true;

            if (PrintOnlyOnChange && version == _lastBridgeVersion)
                printThisBar = false;

            if (printThisBar && _lastPrintedBar != CurrentBar)
            {
                string structureText = analysis.StructureState.HasValue
                    ? analysis.StructureState.Value.ToString()
                    : "null";

                string actionText = analysis.ActionType.HasValue
                    ? analysis.ActionType.Value.ToString()
                    : "null";

                string intentText = analysis.TradeIntent.HasValue
                    ? analysis.TradeIntent.Value.ToString()
                    : "null";

                string dirText = analysis.Snapshot.IsUpContainer ? "UP" : "DN";

                string candText = analysis.FttCandidateBar.HasValue
                    ? analysis.FttCandidateBar.Value.ToString()
                    : "null";

                string confText = analysis.FttConfirmedBar.HasValue
                    ? analysis.FttConfirmedBar.Value.ToString()
                    : "null";

                string entryText = entryPrice.HasValue ? entryPrice.Value.ToString("F2") : "null";
                string stopText = stopPrice.HasValue ? stopPrice.Value.ToString("F2") : "null";
                string targetText = targetPrice.HasValue ? targetPrice.Value.ToString("F2") : "null";

                Print(
                    $"[Probe] " +
                    $"Ver={version} " +
                    $"B{CurrentBar} " +
                    $"C#{analysis.Snapshot.ContainerId} " +
                    $"Dir={dirText} " +
                    $"Struct={structureText} " +
                    $"Action={actionText} " +
                    $"Intent={intentText} " +
                    $"VolState={analysis.VolumeState} " +
                    $"Cand={candText} " +
                    $"Conf={confText} " +
                    $"Signal={sig.Signal} " +
                    $"Transition={sig.Transition} " +
                    $"Phase={sig.Phase} " +
                    $"Exec={exec.Action} " +
                    $"ExecReason={exec.Reason} " +
                    $"Broken={structureBroken} " +
                    $"Entry={entryText} " +
                    $"Stop={stopText} " +
                    $"Target={targetText}");
            }

            _lastBridgeVersion = version;
            _lastPrintedBar = CurrentBar;
        }

        private void TryBuildSimpleTradePlan(
		    ManualContainerAnalysis analysis,
		    out double? entryPrice,
		    out double? stopPrice,
		    out double? targetPrice)
		{
		    entryPrice = null;
		    stopPrice = null;
		    targetPrice = null;
		
		    if (!analysis.FttConfirmedBar.HasValue)
		        return;
		
		    var snap = analysis.Snapshot;
		
		    // Use P3 as entry reference and P2 as stop reference.
		    entryPrice = snap.P3.Price;
		    stopPrice = snap.P2.Price;
		
		    double risk = Math.Abs(entryPrice.Value - stopPrice.Value);
		    if (risk <= TickSize)
		    {
		        entryPrice = null;
		        stopPrice = null;
		        targetPrice = null;
		        return;
		    }
		
		    if (snap.IsUpContainer)
		        targetPrice = entryPrice.Value + 2.0 * risk;
		    else
		        targetPrice = entryPrice.Value - 2.0 * risk;
		}
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaManualSignalProbe[] cachexPvaManualSignalProbe;
		public xPvaManualSignalProbe xPvaManualSignalProbe(bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			return xPvaManualSignalProbe(Input, printEveryBar, printOnlyOnChange, showFixedBanner);
		}

		public xPvaManualSignalProbe xPvaManualSignalProbe(ISeries<double> input, bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			if (cachexPvaManualSignalProbe != null)
				for (int idx = 0; idx < cachexPvaManualSignalProbe.Length; idx++)
					if (cachexPvaManualSignalProbe[idx] != null && cachexPvaManualSignalProbe[idx].PrintEveryBar == printEveryBar && cachexPvaManualSignalProbe[idx].PrintOnlyOnChange == printOnlyOnChange && cachexPvaManualSignalProbe[idx].ShowFixedBanner == showFixedBanner && cachexPvaManualSignalProbe[idx].EqualsInput(input))
						return cachexPvaManualSignalProbe[idx];
			return CacheIndicator<xPvaManualSignalProbe>(new xPvaManualSignalProbe(){ PrintEveryBar = printEveryBar, PrintOnlyOnChange = printOnlyOnChange, ShowFixedBanner = showFixedBanner }, input, ref cachexPvaManualSignalProbe);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaManualSignalProbe xPvaManualSignalProbe(bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			return indicator.xPvaManualSignalProbe(Input, printEveryBar, printOnlyOnChange, showFixedBanner);
		}

		public Indicators.xPvaManualSignalProbe xPvaManualSignalProbe(ISeries<double> input , bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			return indicator.xPvaManualSignalProbe(input, printEveryBar, printOnlyOnChange, showFixedBanner);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaManualSignalProbe xPvaManualSignalProbe(bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			return indicator.xPvaManualSignalProbe(Input, printEveryBar, printOnlyOnChange, showFixedBanner);
		}

		public Indicators.xPvaManualSignalProbe xPvaManualSignalProbe(ISeries<double> input , bool printEveryBar, bool printOnlyOnChange, bool showFixedBanner)
		{
			return indicator.xPvaManualSignalProbe(input, printEveryBar, printOnlyOnChange, showFixedBanner);
		}
	}
}

#endregion
