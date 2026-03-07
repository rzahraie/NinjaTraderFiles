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
	public class xTestMousePoints : Indicator
	{
		ChartHelper chartHelper;
		ChannelLogic channelLogic;
		ChartCoord chartCoord;
		SharpDX.Vector2 canvasCoord;
		
		private Channel _currentChannel = null;
		SharpDX.Color _snapColor = SharpDX.Color.Blue;
		SharpDX.Color _defaultColor = SharpDX.Color.Blue;
		SharpDX.Vector2 _mouseCurrentPos;
		int _defaultWidth = 1;
		string _msgID = "msgWindow";
		enum CHANNEL_STATES  { NONE, POINT_1, POINT_3 };
		CHANNEL_STATES channelStates = CHANNEL_STATES.NONE;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "xTestMousePoints";
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
                }
            }
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
			if (e.Key ==Key.Insert)       // Create NEW Channel: first step initialize Channel data
            {
                _currentChannel = new Channel();
                _currentChannel.ChannelColor = _defaultColor;
                _currentChannel.ChannelWidth = _defaultWidth;
                //_currentChannel.Alarm = _initialAlarm;
                //_currentMode = ToolMode.Create;
                _mouseCurrentPos = SharpDX.Point.Zero;
				channelStates = CHANNEL_STATES.POINT_1;
                
            }
			else if (e.Key == Key.Escape)       //  Abort selection or new channel creation
            {
				channelStates = CHANNEL_STATES.NONE;
                RemoveDrawObject(_msgID);
			}
			else if (e.Key == Key.Delete)       //  Abort selection or new channel creation
            {
				channelStates = CHANNEL_STATES.NONE;
                RemoveDrawObject(_msgID);
			}
		}
        
		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			if (CurrentBar == 0)
            {
                chartHelper = new ChartHelper(ChartControl, Bars, TickSize, ChartBars);
                channelLogic = new ChannelLogic(chartHelper, false, ChartBars);
            }
			else
			{
				switch(channelStates)
				{
					case CHANNEL_STATES.NONE:
						break;
						
					case CHANNEL_STATES.POINT_1:
						TransitionToPoint1();
						break;
						
					case CHANNEL_STATES.POINT_3:
						TransitionToPoint3();
						break;
						
					default:
						break;
				}
			}
		}
		
		private void TransitionToPoint1()
		{
			DrawMessage("New Channel\r\nClick Point 1:");
		}
		
		private void TransitionToPoint3()
		{
			if (_currentChannel != null)
			{
				 ChartCoord coord = channelLogic.Snap(chartCoord, _snapColor, RenderTarget);
             	_currentChannel.SetPoint(1, coord, false); // Set Point 1
				DrawMessage("New Channel\r\nClick Point 3:");
			}
		}

		

        


        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);
			
			Point cursorPoint = chartControl.MouseDownPoint;
		
            // Print the x- and y-coordinates of the mouse cursor when clicked
			DateTime dt = chartControl.GetTimeByX((int)cursorPoint.X);
			double y = chartScale.GetValueByY((float)cursorPoint.Y);
			int id = Bars.GetBar(dt);
         
			chartCoord = new ChartCoord();
			chartCoord.X = dt;
			chartCoord.Y = y;
			
			canvasCoord = new SharpDX.Vector2(0, 0);
			canvasCoord.X = chartControl.GetXByBarIndex(ChartBars, id);
			canvasCoord.Y = chartScale.GetYByValue(y);
        }
		
		public void DrawMessage(string msg)
        {
            Draw.TextFixed(this, _msgID, msg, TextPosition.TopLeft, Brushes.BlueViolet, 
                new SimpleFont("Arial",10) , Brushes.Transparent, Brushes.Transparent, 255);
        }
		
		private void PrintInfo(ChartControl chartControl, ChartScale chartScale)
		{
			Point cursorPoint = chartControl.MouseDownPoint;
			DateTime dt = chartControl.GetTimeByX((int)cursorPoint.X);
			double y = chartScale.GetValueByY((float)cursorPoint.Y);
			int id = Bars.GetBar(dt);
            Print(String.Format("Mouse clicked at coordinates {0},{1}", cursorPoint.X, cursorPoint.Y));
			Print(String.Format("Mouse clicked at date,price {0},{1}", dt.ToString(), y));
			Print(String.Format("Mouse clicked at Bar id {0}", id));
			ChartCoord chartCoord = new ChartCoord();
			chartCoord.X = dt;
			chartCoord.Y = y;
			
			SharpDX.Vector2 canvasCoord = new SharpDX.Vector2(0, 0);
			canvasCoord.X = chartControl.GetXByBarIndex(ChartBars, id);
			canvasCoord.Y = chartScale.GetYByValue(y);
			
			Print(String.Format("Canvas X {0}, Canvas Y {1}", canvasCoord.X, canvasCoord.Y));
		}
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xTestMousePoints[] cachexTestMousePoints;
		public xTestMousePoints xTestMousePoints()
		{
			return xTestMousePoints(Input);
		}

		public xTestMousePoints xTestMousePoints(ISeries<double> input)
		{
			if (cachexTestMousePoints != null)
				for (int idx = 0; idx < cachexTestMousePoints.Length; idx++)
					if (cachexTestMousePoints[idx] != null &&  cachexTestMousePoints[idx].EqualsInput(input))
						return cachexTestMousePoints[idx];
			return CacheIndicator<xTestMousePoints>(new xTestMousePoints(), input, ref cachexTestMousePoints);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xTestMousePoints xTestMousePoints()
		{
			return indicator.xTestMousePoints(Input);
		}

		public Indicators.xTestMousePoints xTestMousePoints(ISeries<double> input )
		{
			return indicator.xTestMousePoints(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xTestMousePoints xTestMousePoints()
		{
			return indicator.xTestMousePoints(Input);
		}

		public Indicators.xTestMousePoints xTestMousePoints(ISeries<double> input )
		{
			return indicator.xTestMousePoints(input);
		}
	}
}

#endregion
