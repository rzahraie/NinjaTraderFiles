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
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class xTestAPVA : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Test the PVA harness";
				Name										= "xTestAPVA";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				var results = APVA.Core.TestRunner.RunAll();
				foreach (var line in results)
    				Print(line);   // prints to NinjaScript Output window
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xTestAPVA[] cachexTestAPVA;
		public xTestAPVA xTestAPVA()
		{
			return xTestAPVA(Input);
		}

		public xTestAPVA xTestAPVA(ISeries<double> input)
		{
			if (cachexTestAPVA != null)
				for (int idx = 0; idx < cachexTestAPVA.Length; idx++)
					if (cachexTestAPVA[idx] != null &&  cachexTestAPVA[idx].EqualsInput(input))
						return cachexTestAPVA[idx];
			return CacheIndicator<xTestAPVA>(new xTestAPVA(), input, ref cachexTestAPVA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xTestAPVA xTestAPVA()
		{
			return indicator.xTestAPVA(Input);
		}

		public Indicators.xTestAPVA xTestAPVA(ISeries<double> input )
		{
			return indicator.xTestAPVA(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xTestAPVA xTestAPVA()
		{
			return indicator.xTestAPVA(Input);
		}

		public Indicators.xTestAPVA xTestAPVA(ISeries<double> input )
		{
			return indicator.xTestAPVA(input);
		}
	}
}

#endregion
