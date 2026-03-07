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
	public class xSplitVolumeOld : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Plot dominant and non-dominant volume for a single price, side by side.";
				Name										= "xSplitVolumeOld";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                IsAutoScale = true;
			}
			else if (State == State.Configure)
			{
			}
		}

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            DrawRectExEx(chartControl,221,3000,6000, SharpDX.Color.Red, SharpDX.Color.Black);
            DrawRectExEx(chartControl,222, 3000, 6000, SharpDX.Color.Black, SharpDX.Color.Red);
            base.OnRender(chartControl, chartScale);
        }

        private List<float> GetBarDistance(int indx)
        {
            ChartControlProperties props = ChartControl.Properties;

            float barSpace = props.BarDistance;
            float barWidth = (float)ChartControl.BarWidth;

            float barStartX = ChartControl.GetXByBarIndex(ChartBars, indx);
            barStartX = barStartX - barSpace / 2;
            float barEndx = ChartControl.GetXByBarIndex(ChartBars, indx) + barSpace / 2;

            return new List<float>() { barStartX, barEndx - (float)(0.1*barSpace) };

        }

        private void CreateBar(float value, float x1, float x2, SharpDX.Color clr)
        {
            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;
            float y0 = ChartPanel.Y + ChartPanel.H;
            float y1 = y0 - value;
            SharpDX.Vector2 startPoint = new SharpDX.Vector2(x1, y0);
            SharpDX.Vector2 endPoint = new SharpDX.Vector2(x2, y1);

            // calculate the desired width and heigh of the rectangle
            float width = endPoint.X - startPoint.X;
            float height = endPoint.Y - startPoint.Y;

            // construct the rectangleF struct to describe the with position and size the drawing
            SharpDX.RectangleF rect = new SharpDX.RectangleF(startPoint.X, startPoint.Y, width, height);

            // define the brush used in the rectangle
            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr);

            // execute the render target fill rectangle with desired values
            RenderTarget.FillRectangle(rect, customDXBrush);

            // always dispose of a brush when finished
            customDXBrush.Dispose();
        }

        private void DrawRectExEx(ChartControl chartControl, int indx, int value1, int value2, SharpDX.Color clr1, SharpDX.Color clr2)
        {
            int x = chartControl.GetXByBarIndex(ChartBars, indx);
            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;
            List<float> lf = GetBarDistance(indx);

            float z = (float)(2.5 * barWidth);
            float z1 = (lf[1] - lf[0])/2;

            //CreateBar(value1,lf[0], lf[0] + z, SharpDX.Color.Red);
            //CreateBar(value2, lf[0] + z, lf[1], SharpDX.Color.Black);

            CreateBar(value1, lf[0], lf[0] + z1, clr1);
            CreateBar(value2, lf[0] + z1, lf[1], clr2);

            
        }

        private void DrawRectEx(ChartControl chartControl)
        {
            // create two vectors to position the rectangle

            int x = chartControl.GetXByBarIndex(ChartBars, 536);
            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;
            List<float> lf = GetBarDistance(536);

            float y0 = ChartPanel.Y + ChartPanel.H;
            float y1 = y0 - 50;
            float z = (float)(2.5 * barWidth);
            SharpDX.Vector2 startPoint = new SharpDX.Vector2(lf[0], y0);
            SharpDX.Vector2 endPoint = new SharpDX.Vector2(lf[0] + z, y1);

            // calculate the desired width and heigh of the rectangle
            float width = endPoint.X - startPoint.X;
            float height = endPoint.Y - startPoint.Y;



            // define the brush used in the rectangle
            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.DodgerBlue);



            // construct the rectangleF struct to describe the with position and size the drawing
            SharpDX.RectangleF rect = new SharpDX.RectangleF(startPoint.X, startPoint.Y, width, height);


            // execute the render target fill rectangle with desired values
            RenderTarget.FillRectangle(rect, customDXBrush);



            // always dispose of a brush when finished
            customDXBrush.Dispose();
        }
        private void DrawRect(ChartControl chartControl)
        {
            int x = chartControl.GetXByBarIndex(ChartBars, 2);

            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Black);

            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;

            RenderTarget.DrawRectangle(new SharpDX.RectangleF(x, ChartPanel.Y, 1, ChartPanel.H), customDXBrush);

        }
        private void DrawLine(ChartControl chartControl)
        {
            int x = chartControl.GetXByBarIndex(ChartBars, 263);
            SharpDX.Vector2 startPoint = new SharpDX.Vector2(x, ChartPanel.Y + ChartPanel.H);
            SharpDX.Vector2 endPoint = new SharpDX.Vector2(x, (ChartPanel.Y + ChartPanel.H) - 50);

            // define the brush used in the line
            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.DodgerBlue);


            // execute the render target draw line with desired values
            RenderTarget.DrawLine(startPoint, endPoint, customDXBrush, 2);

            // always dispose of a brush when finished
            customDXBrush.Dispose();
        }

        private void DrawX(ChartControl chartControl)
        {
            // create two vectors to position the ellipse

            int x = chartControl.GetXByBarIndex(ChartBars, 263);

            SharpDX.Vector2 startPoint = new SharpDX.Vector2(x, ChartPanel.Y);
            SharpDX.Vector2 endPoint = new SharpDX.Vector2(x + ChartPanel.W, ChartPanel.Y + ChartPanel.H);


            // calculate the center point of the ellipse from start/end points

            SharpDX.Vector2 centerPoint = (startPoint + endPoint) / 2;



            // set the radius of the ellipse
            float radiusX = 50;
            float radiusY = 50;



            // construct the rectangleF struct to describe the position and size the drawing

            SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(centerPoint, radiusX, radiusY);



            // define the brush used in the rectangle

            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.DodgerBlue);


            // execute the render target fill ellipse with desired values
            RenderTarget.FillEllipse(ellipse, customDXBrush);



            // always dispose of a brush when finished
            customDXBrush.Dispose();
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
		private xSplitVolumeOld[] cachexSplitVolumeOld;
		public xSplitVolumeOld xSplitVolumeOld()
		{
			return xSplitVolumeOld(Input);
		}

		public xSplitVolumeOld xSplitVolumeOld(ISeries<double> input)
		{
			if (cachexSplitVolumeOld != null)
				for (int idx = 0; idx < cachexSplitVolumeOld.Length; idx++)
					if (cachexSplitVolumeOld[idx] != null &&  cachexSplitVolumeOld[idx].EqualsInput(input))
						return cachexSplitVolumeOld[idx];
			return CacheIndicator<xSplitVolumeOld>(new xSplitVolumeOld(), input, ref cachexSplitVolumeOld);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitVolumeOld xSplitVolumeOld()
		{
			return indicator.xSplitVolumeOld(Input);
		}

		public Indicators.xSplitVolumeOld xSplitVolumeOld(ISeries<double> input )
		{
			return indicator.xSplitVolumeOld(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitVolumeOld xSplitVolumeOld()
		{
			return indicator.xSplitVolumeOld(Input);
		}

		public Indicators.xSplitVolumeOld xSplitVolumeOld(ISeries<double> input )
		{
			return indicator.xSplitVolumeOld(input);
		}
	}
}

#endregion
