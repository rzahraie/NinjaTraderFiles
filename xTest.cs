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
	public class xTest : Indicator
	{
		enum CHANNEL_STATES  { NONE, POINT_1, POINT_3 };
		CHANNEL_STATES channelStates = CHANNEL_STATES.NONE;
		string _msgID = "msgWindow";
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "xTest";
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
				if (ChartPanel != null)
                {
                    ChartPanel.KeyDown += OnKeyDown;
					ChartPanel.MouseDown += OnMouseDown;
                    ChartPanel.MouseMove += OnMouseMove;
                }
			}
			else if (State == State.Terminated)
            {
                if (ChartPanel != null)
                {
                    ChartPanel.KeyDown -= OnKeyDown;
					ChartPanel.MouseDown -= OnMouseDown;
					ChartPanel.MouseMove -= OnMouseMove;
                }
            }
		}

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            FSM();
        }

        private void FSM()
        {
            switch (channelStates)
            {
                case CHANNEL_STATES.NONE:
                    break;

                case CHANNEL_STATES.POINT_1:
                    TransitionToPoint1();
                    break;

                case CHANNEL_STATES.POINT_3:
                    //TransitionToPoint3();
                    break;

                default:
                    break;
            }
        }

        protected override void OnBarUpdate()
		{
			
		}
		private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (channelStates == CHANNEL_STATES.POINT_1) channelStates = CHANNEL_STATES.POINT_3;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
           
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key ==Key.Insert)       
            {
				channelStates = CHANNEL_STATES.POINT_1;
				TransitionToPoint1();
			}
			else if (e.Key == Key.Delete)       //  Abort selection or new channel creation
            {
				channelStates = CHANNEL_STATES.NONE;
                RemoveDrawObject(_msgID);
			}
		}
		
		private void TransitionToPoint1()
		{
			DrawMessage("New Channel\r\nClick Point 1:");
		}
		
		private void TransitionToPoint3()
		{
			DrawMessage("New Channel\r\nClick Point 3:");	
		}
		
		public void DrawMessage(string msg)
        {
            Draw.TextFixed(this, _msgID, msg, TextPosition.TopLeft, Brushes.BlueViolet, 
                new SimpleFont("Arial",10) , Brushes.Transparent, Brushes.Transparent, 255);
        }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xTest[] cachexTest;
		public xTest xTest()
		{
			return xTest(Input);
		}

		public xTest xTest(ISeries<double> input)
		{
			if (cachexTest != null)
				for (int idx = 0; idx < cachexTest.Length; idx++)
					if (cachexTest[idx] != null &&  cachexTest[idx].EqualsInput(input))
						return cachexTest[idx];
			return CacheIndicator<xTest>(new xTest(), input, ref cachexTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xTest xTest()
		{
			return indicator.xTest(Input);
		}

		public Indicators.xTest xTest(ISeries<double> input )
		{
			return indicator.xTest(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xTest xTest()
		{
			return indicator.xTest(Input);
		}

		public Indicators.xTest xTest(ISeries<double> input )
		{
			return indicator.xTest(input);
		}
	}
}

#endregion
