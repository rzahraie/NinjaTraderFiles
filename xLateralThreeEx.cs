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
	public class xLateralThreeEx : Indicator
	{
		private bool m_Init = true;
        Rectangle m_Rect;
		double m_RectHigh = 0;
		double m_RectLow = 0;
		int m_RectStartBar = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "xLateralThreeEx";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= false;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				BarsBack									= 3;
			}
			else if (State == State.Configure)
			{
			}
		}

		private void DrawLateral()
		{
			bool u1 = High[2] >= High[1];
			bool u12 = High[2] >= High[0];
			bool l1 = Low[2] <= Low[1];
			bool l12 = Low[2] <= Low[0];
			
			if (u1 && u12 && l1 && l12)
			{
				m_Init = false;
				
				Print(CurrentBar + " Draw Lateral " + m_Init);
				
				string tag = System.Convert.ToString(High[0]) + 
				System.Convert.ToString(Low[0]);

				m_RectHigh = High[2];
				m_RectLow = Low[2];
				m_RectStartBar = CurrentBar-2;
				
                m_Rect = Draw.Rectangle(this, tag, false, 2,
					High[2], 0, Low[2], Brushes.Black, 
										Brushes.Gray, 1);
				
				m_Rect.OutlineStroke.Pen = new Pen(Brushes.Black, 1);
			}
		}

        private bool LateralBroken()
        {
			if (m_Rect == null) return false;
			
            int startbar = (CurrentBar - m_Rect.StartAnchor.BarsAgo);
            int endbar = (CurrentBar - m_Rect.EndAnchor.BarsAgo);

            double high = High[CurrentBar - startbar];
            double low = Low[CurrentBar - startbar];

            if ((m_RectHigh <= Low[0]) || (m_RectLow > High[0])) 
			{
				Print(CurrentBar + " Broken Lateral " + m_RectHigh + " " + Low[0] +
				 " " + m_RectLow + " " + High[0]);
				m_Rect = null;
				m_Init = true;
				return true;
			}
            else 
			{
				
				int startBarsago = CurrentBar - m_RectStartBar;
				m_Rect = Draw.Rectangle(this, m_Rect.Tag, false, startBarsago,
					m_RectHigh, 0, m_RectLow, Brushes.Black, 
										Brushes.Gray, 1);
				m_Rect.OutlineStroke.Pen = new Pen(Brushes.Black, 1);
				Print(CurrentBar + " Extend Lateral " + " start bar (ago) " + startBarsago);
			}

            return false;
        }

		protected override void OnBarUpdate()
		{
			try
			{
				if (CurrentBar < 3) return;
				
				if (m_Init) DrawLateral();
				else if (LateralBroken()) DrawLateral();
				
			}
			catch(System.Exception e)
			{
				Print(e.ToString());	
				Print(e.Data.ToString());
				
			}
			
		}

		#region Properties
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="BarsBack", Order=1, GroupName="Parameters")]
		public int BarsBack
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xLateralThreeEx[] cachexLateralThreeEx;
		public xLateralThreeEx xLateralThreeEx(int barsBack)
		{
			return xLateralThreeEx(Input, barsBack);
		}

		public xLateralThreeEx xLateralThreeEx(ISeries<double> input, int barsBack)
		{
			if (cachexLateralThreeEx != null)
				for (int idx = 0; idx < cachexLateralThreeEx.Length; idx++)
					if (cachexLateralThreeEx[idx] != null && cachexLateralThreeEx[idx].BarsBack == barsBack && cachexLateralThreeEx[idx].EqualsInput(input))
						return cachexLateralThreeEx[idx];
			return CacheIndicator<xLateralThreeEx>(new xLateralThreeEx(){ BarsBack = barsBack }, input, ref cachexLateralThreeEx);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xLateralThreeEx xLateralThreeEx(int barsBack)
		{
			return indicator.xLateralThreeEx(Input, barsBack);
		}

		public Indicators.xLateralThreeEx xLateralThreeEx(ISeries<double> input , int barsBack)
		{
			return indicator.xLateralThreeEx(input, barsBack);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xLateralThreeEx xLateralThreeEx(int barsBack)
		{
			return indicator.xLateralThreeEx(Input, barsBack);
		}

		public Indicators.xLateralThreeEx xLateralThreeEx(ISeries<double> input , int barsBack)
		{
			return indicator.xLateralThreeEx(input, barsBack);
		}
	}
}

#endregion
