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
	public class xPrintPriceData : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "xPrintPriceData";
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
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			Print(CurrentBar + "," + Time[0] + "," + 
			Open[0].ToString("0.0000") + "," + 
			High[0].ToString("0.0000") + "," + 
			Low[0].ToString("0.0000") + "," + 
			Close[0].ToString("0.0000") + "," + 
			Volume[0]);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPrintPriceData[] cachexPrintPriceData;
		public xPrintPriceData xPrintPriceData()
		{
			return xPrintPriceData(Input);
		}

		public xPrintPriceData xPrintPriceData(ISeries<double> input)
		{
			if (cachexPrintPriceData != null)
				for (int idx = 0; idx < cachexPrintPriceData.Length; idx++)
					if (cachexPrintPriceData[idx] != null &&  cachexPrintPriceData[idx].EqualsInput(input))
						return cachexPrintPriceData[idx];
			return CacheIndicator<xPrintPriceData>(new xPrintPriceData(), input, ref cachexPrintPriceData);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPrintPriceData xPrintPriceData()
		{
			return indicator.xPrintPriceData(Input);
		}

		public Indicators.xPrintPriceData xPrintPriceData(ISeries<double> input )
		{
			return indicator.xPrintPriceData(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPrintPriceData xPrintPriceData()
		{
			return indicator.xPrintPriceData(Input);
		}

		public Indicators.xPrintPriceData xPrintPriceData(ISeries<double> input )
		{
			return indicator.xPrintPriceData(input);
		}
	}
}

#endregion
