#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
#endregion 

namespace NinjaTrader.NinjaScript.Indicators
{
    [Description("Hover Feedback Indicator")]
    public class HoverFeedbackIndicator : Indicator
    {
        private System.Windows.Point? hoverPoint;
        private ChartControl chartControl;    
		protected override void OnStateChange()
	    {
	        if (State == State.SetDefaults)
	        {
	            Description = "Displays a rectangle at mouse hover position.";
	            Name = "HoverFeedbackIndicator";
	            IsOverlay = true;
	        }
	        else if (State == State.DataLoaded)
	        {
	            chartControl = ChartControl;
	            if (ChartPanel != null)
	            {
	                ChartPanel.MouseMove += OnChartPanelMouseMove;
	            }
	        }
	        else if (State == State.Terminated)
	        {
	            if (ChartPanel != null)
	            {
	                ChartPanel.MouseMove -= OnChartPanelMouseMove;
	            }
	        }
	    }
	
	    private void OnChartPanelMouseMove(object sender, MouseEventArgs e)
	    {
	        // Convert screen point to chart coordinates
	        hoverPoint = e.GetPosition(chartControl);
	        ForceRefresh(); // Force redraw
	    }
	
	    protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	    {
	        if (hoverPoint.HasValue && chartControl != null)
	        {
	            using (SolidColorBrush dxBrush = new SolidColorBrush(RenderTarget, Color.Blue))
	            {
	                // Draw a blue rectangle at the hover point
	                RectangleF rect = new RectangleF(
	                    (float)(hoverPoint.Value.X - RectangleSize / 2),
	                    (float)(hoverPoint.Value.Y - RectangleSize / 2),
	                    RectangleSize,
	                    RectangleSize);
	                RenderTarget.FillRectangle(rect, dxBrush);
	            }
	        }
	    }
	
	    #region Properties
	    [NinjaScriptProperty]
	    [Display(Name = "Rectangle Size", Order = 1, GroupName = "Visuals")]
	    public int RectangleSize { get; set; } = 24;
	    #endregion
		}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private HoverFeedbackIndicator[] cacheHoverFeedbackIndicator;
		public HoverFeedbackIndicator HoverFeedbackIndicator(int rectangleSize)
		{
			return HoverFeedbackIndicator(Input, rectangleSize);
		}

		public HoverFeedbackIndicator HoverFeedbackIndicator(ISeries<double> input, int rectangleSize)
		{
			if (cacheHoverFeedbackIndicator != null)
				for (int idx = 0; idx < cacheHoverFeedbackIndicator.Length; idx++)
					if (cacheHoverFeedbackIndicator[idx] != null && cacheHoverFeedbackIndicator[idx].RectangleSize == rectangleSize && cacheHoverFeedbackIndicator[idx].EqualsInput(input))
						return cacheHoverFeedbackIndicator[idx];
			return CacheIndicator<HoverFeedbackIndicator>(new HoverFeedbackIndicator(){ RectangleSize = rectangleSize }, input, ref cacheHoverFeedbackIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HoverFeedbackIndicator HoverFeedbackIndicator(int rectangleSize)
		{
			return indicator.HoverFeedbackIndicator(Input, rectangleSize);
		}

		public Indicators.HoverFeedbackIndicator HoverFeedbackIndicator(ISeries<double> input , int rectangleSize)
		{
			return indicator.HoverFeedbackIndicator(input, rectangleSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HoverFeedbackIndicator HoverFeedbackIndicator(int rectangleSize)
		{
			return indicator.HoverFeedbackIndicator(Input, rectangleSize);
		}

		public Indicators.HoverFeedbackIndicator HoverFeedbackIndicator(ISeries<double> input , int rectangleSize)
		{
			return indicator.HoverFeedbackIndicator(input, rectangleSize);
		}
	}
}

#endregion
