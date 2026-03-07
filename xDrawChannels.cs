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
	public class xDrawChannels : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Draw a Hershey channel given point 1, point 3 and last bar of the channel.";
				Name										= "xDrawChannels";
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
				UpChannelColor					= Brushes.Blue;
				DownChannelColor					= Brushes.Red;
				ContinueChannelUntilBroken					= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
            {
                if (ChartPanel != null)
                {
                    ChartPanel.KeyDown += OnKeyDown;
					ChartControl.MouseLeftButtonDown += MouseClicked;
                }
            }
            else if (State == State.Terminated)
            {
                if (ChartPanel != null)
                {
                    ChartPanel.KeyDown -= OnKeyDown;
					ChartControl.MouseLeftButtonDown -= MouseClicked;
                }
            }
		}
		
		public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                
            }
        }
		
		protected void MouseClicked(object sender, MouseButtonEventArgs e)
		{
			// convert e.GetPosition for different dpi settings
			int x = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel as IInputElement).X, 
							ChartControl.PresentationSource);
//			clickPoint.Y = ChartingExtensions.ConvertToVerticalPixels(e.GetPosition(ChartPanel as IInputElement).Y, 
//							ChartControl.PresentationSource);
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			double slotIndex = chartControl.GetSlotIndexByX(35);
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}

		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="UpChannelColor", Order=1, GroupName="Parameters")]
		public Brush UpChannelColor
		{ get; set; }

		[Browsable(false)]
		public string UpChannelColorSerializable
		{
			get { return Serialize.BrushToString(UpChannelColor); }
			set { UpChannelColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="DownChannelColor", Order=2, GroupName="Parameters")]
		public Brush DownChannelColor
		{ get; set; }

		[Browsable(false)]
		public string DownChannelColorSerializable
		{
			get { return Serialize.BrushToString(DownChannelColor); }
			set { DownChannelColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Display(Name="ContinueChannelUntilBroken", Order=3, GroupName="Parameters")]
		public bool ContinueChannelUntilBroken
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xDrawChannels[] cachexDrawChannels;
		public xDrawChannels xDrawChannels(Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			return xDrawChannels(Input, upChannelColor, downChannelColor, continueChannelUntilBroken);
		}

		public xDrawChannels xDrawChannels(ISeries<double> input, Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			if (cachexDrawChannels != null)
				for (int idx = 0; idx < cachexDrawChannels.Length; idx++)
					if (cachexDrawChannels[idx] != null && cachexDrawChannels[idx].UpChannelColor == upChannelColor && cachexDrawChannels[idx].DownChannelColor == downChannelColor && cachexDrawChannels[idx].ContinueChannelUntilBroken == continueChannelUntilBroken && cachexDrawChannels[idx].EqualsInput(input))
						return cachexDrawChannels[idx];
			return CacheIndicator<xDrawChannels>(new xDrawChannels(){ UpChannelColor = upChannelColor, DownChannelColor = downChannelColor, ContinueChannelUntilBroken = continueChannelUntilBroken }, input, ref cachexDrawChannels);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xDrawChannels xDrawChannels(Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			return indicator.xDrawChannels(Input, upChannelColor, downChannelColor, continueChannelUntilBroken);
		}

		public Indicators.xDrawChannels xDrawChannels(ISeries<double> input , Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			return indicator.xDrawChannels(input, upChannelColor, downChannelColor, continueChannelUntilBroken);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xDrawChannels xDrawChannels(Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			return indicator.xDrawChannels(Input, upChannelColor, downChannelColor, continueChannelUntilBroken);
		}

		public Indicators.xDrawChannels xDrawChannels(ISeries<double> input , Brush upChannelColor, Brush downChannelColor, bool continueChannelUntilBroken)
		{
			return indicator.xDrawChannels(input, upChannelColor, downChannelColor, continueChannelUntilBroken);
		}
	}
}

#endregion
