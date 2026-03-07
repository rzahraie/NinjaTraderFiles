#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
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
	public class IndicadorCandlestick : Indicator
	{
		private int										barPaintWidth;
		private Dictionary<string, DXMediaMap>			dxmBrushes;
		private SharpDX.RectangleF						reuseRect;
		private SharpDX.Vector2							reuseVector1, reuseVector2;
		private double									tmpMax, tmpMin, tmpPlotVal;
		private int										x, y1, y2, y3, y4;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "IndicadorCandlestick";
				Calculate									= Calculate.OnBarClose;
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
				ClearOutputWindow();
				
				dxmBrushes	= new Dictionary<string, DXMediaMap>();
				foreach (string brushName in new string[] { "barColorDown", "barColorUp", "shadowColor" })
					dxmBrushes.Add(brushName, new DXMediaMap());
				
				BarColorDown								= Brushes.Red;
				BarColorUp									= Brushes.Lime;
				ShadowColor									= (Application.Current.TryFindResource("ChartControl.AxisPen") as Pen).Brush;
				ShadowWidth									= 1;
				
				AddPlot(Brushes.Gray, "IndClose");
				AddPlot(Brushes.Gray, "IndHigh");
				AddPlot(Brushes.Gray, "IndLow");
				AddPlot(Brushes.Gray, "IndOpen");
			}
		}

		protected override void OnBarUpdate()
		{
			if( CurrentBar < 10) return;
			
			IndOpen[0] = Open[0];
			IndHigh[0] = High[0];
			IndLow[0] = Low[0];
			IndClose[0] = Close[0];
			
			for (int i = 1; i < PlotBrushes.Count(); i++)
				PlotBrushes[i][0] = Brushes.Transparent;
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			barPaintWidth = Math.Max(3, 1 + 2 * ((int)ChartBars.Properties.ChartStyle.BarWidth - 1) + 2 * ShadowWidth);

            for (int idx = ChartBars.FromIndex; idx <= ChartBars.ToIndex; idx++)
            {
                if (idx - Displacement < 0 || idx - Displacement >= BarsArray[0].Count || (idx - Displacement < BarsRequiredToPlot))
                    continue;

                x					= ChartControl.GetXByBarIndex(ChartBars, idx);
                y1					= chartScale.GetYByValue(IndOpen.GetValueAt(idx));
                y2					= chartScale.GetYByValue(IndHigh.GetValueAt(idx));
                y3					= chartScale.GetYByValue(IndLow.GetValueAt(idx));
                y4					= chartScale.GetYByValue(IndClose.GetValueAt(idx));

				reuseVector1.X		= x;
				reuseVector1.Y		= y2;
				reuseVector2.X		= x;
				reuseVector2.Y		= y3;

				RenderTarget.DrawLine(reuseVector1, reuseVector2, dxmBrushes["shadowColor"].DxBrush);

				if (y4 == y1)
				{
					reuseVector1.X	= (x - barPaintWidth / 2);
					reuseVector1.Y	= y1;
					reuseVector2.X	= (x + barPaintWidth / 2);
					reuseVector2.Y	= y1;

					RenderTarget.DrawLine(reuseVector1, reuseVector2, dxmBrushes["shadowColor"].DxBrush);
				}
				else
				{
					if (y4 > y1)
					{
						UpdateRect(ref reuseRect, (x - barPaintWidth / 2), y1, barPaintWidth, (y4 - y1));
						RenderTarget.FillRectangle(reuseRect, dxmBrushes["barColorDown"].DxBrush);
					}
					else
					{
						UpdateRect(ref reuseRect, (x - barPaintWidth / 2), y4, barPaintWidth, (y1 - y4));
						RenderTarget.FillRectangle(reuseRect, dxmBrushes["barColorUp"].DxBrush);
					}

					UpdateRect(ref reuseRect, ((x - barPaintWidth / 2) + (ShadowWidth / 2)), Math.Min(y4, y1), (barPaintWidth - ShadowWidth + 2), Math.Abs(y4 - y1));
					RenderTarget.DrawRectangle(reuseRect, dxmBrushes["shadowColor"].DxBrush);
				}
            }
		}

		public override void OnRenderTargetChanged()
		{		
			try
			{
				foreach (KeyValuePair<string, DXMediaMap> item in dxmBrushes)
				{
					if (item.Value.DxBrush != null)
						item.Value.DxBrush.Dispose();

					if (RenderTarget != null)
						item.Value.DxBrush = item.Value.MediaBrush.ToDxBrush(RenderTarget);					
				}
			}
			catch (Exception exception)
			{
				Log(string.Format("SpreadCandlesticks exception: ", exception.ToString()), LogLevel.Error);
			}
		}

		private void UpdateRect(ref SharpDX.RectangleF updateRectangle, float x, float y, float width, float height)
		{
			updateRectangle.X		= x;
			updateRectangle.Y		= y;
			updateRectangle.Width	= width;
			updateRectangle.Height	= height;
		}

		private void UpdateRect(ref SharpDX.RectangleF rectangle, int x, int y, int width, int height)
		{
			UpdateRect(ref rectangle, (float)x, (float)y, (float)width, (float)height);
		}

		#region Properties
		[Browsable(false)]
		public class DXMediaMap
		{
			public SharpDX.Direct2D1.Brush		DxBrush;
			public System.Windows.Media.Brush	MediaBrush;
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndClose
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndHigh
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndLow
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndOpen
		{
			get { return Values[3]; }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="BarColorDown", Order=1, GroupName= "Optics")]
		public Brush BarColorDown
		{
			get { return dxmBrushes["barColorDown"].MediaBrush; }
			set { dxmBrushes["barColorDown"].MediaBrush = value; }
		}

		[Browsable(false)]
		public string BarColorDownSerializable
		{
			get { return Serialize.BrushToString(BarColorDown); }
			set { BarColorDown = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="BarColorUp", Order=2, GroupName= "Optics")]
		public Brush BarColorUp
		{
			get { return dxmBrushes["barColorUp"].MediaBrush; }
			set { dxmBrushes["barColorUp"].MediaBrush = value; }
		}

		[Browsable(false)]
		public string BarColorUpSerializable
		{
			get { return Serialize.BrushToString(BarColorUp); }
			set { BarColorUp = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ShadowColor", Order=3, GroupName="Optics")]
		public Brush ShadowColor
		{
			get { return dxmBrushes["shadowColor"].MediaBrush; }
			set { dxmBrushes["shadowColor"].MediaBrush = value; }
		}

		[Browsable(false)]
		public string ShadowColorSerializable
		{
			get { return Serialize.BrushToString(ShadowColor); }
			set { ShadowColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ShadowWidth", Order=4, GroupName= "Optics")]
		public int ShadowWidth
		{ get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private IndicadorCandlestick[] cacheIndicadorCandlestick;
		public IndicadorCandlestick IndicadorCandlestick(Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			return IndicadorCandlestick(Input, barColorDown, barColorUp, shadowColor, shadowWidth);
		}

		public IndicadorCandlestick IndicadorCandlestick(ISeries<double> input, Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			if (cacheIndicadorCandlestick != null)
				for (int idx = 0; idx < cacheIndicadorCandlestick.Length; idx++)
					if (cacheIndicadorCandlestick[idx] != null && cacheIndicadorCandlestick[idx].BarColorDown == barColorDown && cacheIndicadorCandlestick[idx].BarColorUp == barColorUp && cacheIndicadorCandlestick[idx].ShadowColor == shadowColor && cacheIndicadorCandlestick[idx].ShadowWidth == shadowWidth && cacheIndicadorCandlestick[idx].EqualsInput(input))
						return cacheIndicadorCandlestick[idx];
			return CacheIndicator<IndicadorCandlestick>(new IndicadorCandlestick(){ BarColorDown = barColorDown, BarColorUp = barColorUp, ShadowColor = shadowColor, ShadowWidth = shadowWidth }, input, ref cacheIndicadorCandlestick);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.IndicadorCandlestick IndicadorCandlestick(Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			return indicator.IndicadorCandlestick(Input, barColorDown, barColorUp, shadowColor, shadowWidth);
		}

		public Indicators.IndicadorCandlestick IndicadorCandlestick(ISeries<double> input , Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			return indicator.IndicadorCandlestick(input, barColorDown, barColorUp, shadowColor, shadowWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.IndicadorCandlestick IndicadorCandlestick(Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			return indicator.IndicadorCandlestick(Input, barColorDown, barColorUp, shadowColor, shadowWidth);
		}

		public Indicators.IndicadorCandlestick IndicadorCandlestick(ISeries<double> input , Brush barColorDown, Brush barColorUp, Brush shadowColor, int shadowWidth)
		{
			return indicator.IndicadorCandlestick(input, barColorDown, barColorUp, shadowColor, shadowWidth);
		}
	}
}

#endregion
