#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.xPva.Engine2;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class xPvaEngineHost : Indicator
	{
		 private xPvaEngine2 _engine;
		 private xPvaEngineParameters _parameters;

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "VolPivotWindow", Order = 1, GroupName = "Parameters")]
        public int VolPivotWindow { get; set; } = 1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
            {
                Name = "xPvaEngineHost";
                Calculate = Calculate.OnBarClose;   // start deterministic; later you can do intrabar carefully
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
				_parameters = new xPvaEngineParameters();
                _engine = new xPvaEngine2(_parameters);
            }
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			if (CurrentBar < 1)
                return;

            // Convert NT bar data into engine snapshot.
            // Time[0] is exchange time; for now convert to UTC via DateTime.SpecifyKind local assumption.
            // Better: use your session/timezone normalization later.
            DateTime time = Time[0];
            DateTime timeUtc = DateTime.SpecifyKind(time, DateTimeKind.Local).ToUniversalTime();

            var snap = new xPva.Engine2.BarSnapshot(
                timeUtc,
                Open[0], High[0], Low[0], Close[0],
                (long)Volume[0],
                CurrentBar
            );

            if (!_engine.Step(snap, Instrument.MasterInstrument.TickSize))
			    return;
			
			var st = _engine.State;
			var f = st.LastBarFeatures;
			
			if (f.HasValue)
			{
			    Print(
			        $"{Instrument.FullName} B{f.Value.BarIndex} " +
			        $"PC={f.Value.PriceCase} " +
			        $"POL={f.Value.Polarity} " +
			        $"DIR={st.LastDirection.Context}:{st.LastDirection.Score:F2} " +
			        $"DOM={st.LastDominance.State} " +
			        $"FLIP={st.LastSequenceStats.FlipCount} " +
			        $"IMB={st.LastImbalance.Imbalance:F2} " +
			        $"LAT={st.LastLateral.State}/{st.LastLateral.Bias} " +
			        $"SIG={st.LastSignal.Phase}:{st.LastSignal.Score:F2} " +
					$"EXE={st.LastExecution.Intent} " +
					$"POS={st.CurrentPosition} " +
					$"DEG={st.DegradingSignalBars} " +
					$"STB={st.StableSignalBars}");
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaEngineHost[] cachexPvaEngineHost;
		public xPvaEngineHost xPvaEngineHost(int volPivotWindow)
		{
			return xPvaEngineHost(Input, volPivotWindow);
		}

		public xPvaEngineHost xPvaEngineHost(ISeries<double> input, int volPivotWindow)
		{
			if (cachexPvaEngineHost != null)
				for (int idx = 0; idx < cachexPvaEngineHost.Length; idx++)
					if (cachexPvaEngineHost[idx] != null && cachexPvaEngineHost[idx].VolPivotWindow == volPivotWindow && cachexPvaEngineHost[idx].EqualsInput(input))
						return cachexPvaEngineHost[idx];
			return CacheIndicator<xPvaEngineHost>(new xPvaEngineHost(){ VolPivotWindow = volPivotWindow }, input, ref cachexPvaEngineHost);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaEngineHost xPvaEngineHost(int volPivotWindow)
		{
			return indicator.xPvaEngineHost(Input, volPivotWindow);
		}

		public Indicators.xPvaEngineHost xPvaEngineHost(ISeries<double> input , int volPivotWindow)
		{
			return indicator.xPvaEngineHost(input, volPivotWindow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaEngineHost xPvaEngineHost(int volPivotWindow)
		{
			return indicator.xPvaEngineHost(Input, volPivotWindow);
		}

		public Indicators.xPvaEngineHost xPvaEngineHost(ISeries<double> input , int volPivotWindow)
		{
			return indicator.xPvaEngineHost(input, volPivotWindow);
		}
	}
}

#endregion
