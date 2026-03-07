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
	public class xDrawHersheyContainer : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Draw Hershey containers such as tapes, traverses and channels.";
				Name										= "xDrawHersheyContainer";
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
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
			}
			
			
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xDrawHersheyContainer[] cachexDrawHersheyContainer;
		public xDrawHersheyContainer xDrawHersheyContainer()
		{
			return xDrawHersheyContainer(Input);
		}

		public xDrawHersheyContainer xDrawHersheyContainer(ISeries<double> input)
		{
			if (cachexDrawHersheyContainer != null)
				for (int idx = 0; idx < cachexDrawHersheyContainer.Length; idx++)
					if (cachexDrawHersheyContainer[idx] != null &&  cachexDrawHersheyContainer[idx].EqualsInput(input))
						return cachexDrawHersheyContainer[idx];
			return CacheIndicator<xDrawHersheyContainer>(new xDrawHersheyContainer(), input, ref cachexDrawHersheyContainer);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xDrawHersheyContainer xDrawHersheyContainer()
		{
			return indicator.xDrawHersheyContainer(Input);
		}

		public Indicators.xDrawHersheyContainer xDrawHersheyContainer(ISeries<double> input )
		{
			return indicator.xDrawHersheyContainer(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xDrawHersheyContainer xDrawHersheyContainer()
		{
			return indicator.xDrawHersheyContainer(Input);
		}

		public Indicators.xDrawHersheyContainer xDrawHersheyContainer(ISeries<double> input )
		{
			return indicator.xDrawHersheyContainer(input);
		}
	}
}

#endregion
