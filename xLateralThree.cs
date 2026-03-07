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
	public class xLateralThree : Indicator
	{
		private bool m_Init = true;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "xLateralThree";
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
				string tag = System.Convert.ToString(High[0]) + 
				System.Convert.ToString(Low[0]);
				
				Draw.Rectangle(this, tag, false, 2,
					High[2], 0, Low[2], Brushes.Black, 
										Brushes.Gray, 1);
				
				Print("Draw Rectangle " + CurrentBar + " tag " + tag);
			}
		}

		private void ContinueLateralEx()
		{
			bool lateral = false;
			bool cont = false;
			
			double high = 0;
			double low = 0;
			DrawingTools.Rectangle rect = null;
			
			foreach (DrawingTool draw in DrawObjects)
 			{
				if (draw is DrawingTools.Rectangle)
				{
					rect = draw as DrawingTools.Rectangle;
					
					int startbar = (CurrentBar - rect.StartAnchor.BarsAgo);
					int endbar = (CurrentBar - rect.EndAnchor.BarsAgo);
					
					high = High[CurrentBar - startbar];
					low = Low[CurrentBar - startbar];
					
					Print("CurrentBar " + CurrentBar + " Start " + startbar + " High " + high + 
					" End " + endbar + " low " + low + " tag " + rect.Tag);
					
				}
			}
		}
		
		private bool ContinueLateral()
		{
			bool lateral = false;
			bool cont = false;
			
			double high = 0;
			double low = 0;
			DrawingTools.Rectangle rect = null;
			
			foreach (DrawingTool draw in DrawObjects)
 			{
				if (draw is DrawingTools.Rectangle)
				{
					rect = draw as DrawingTools.Rectangle;
					
					if (rect.Tag.Contains("BROKEN")) 
					{
						Print(CurrentBar + " " + rect.Tag);
						continue;
					}
					
					int startbar = (CurrentBar - rect.StartAnchor.BarsAgo);
					int endbar = (CurrentBar - rect.EndAnchor.BarsAgo);
					
					high = rect.StartAnchor.Price;
					low = rect.EndAnchor.Price;
					
					if ((high <= Low[0]) || (low >= High[0]))
					{
						Print(CurrentBar + "****" + rect.Tag + " high " + 
						rect.StartAnchor.Price +
						" low " + rect.EndAnchor.Price);
						rect.Tag = rect.Tag + "BROKEN";
						return false;
					}
					else
					{
						rect.EndAnchor.BarsAgo = 0;
						Print("CurrentBar " + CurrentBar + " Start bars ago " + rect.StartAnchor.BarsAgo + 
						" End bars ago " + rect.EndAnchor.BarsAgo);
						
						int start = rect.StartAnchor.BarsAgo;
						
						Draw.Rectangle(this, rect.Tag, false, 
							start++,
							high, rect.EndAnchor.BarsAgo, low, Brushes.Black, 
									Brushes.Gray, 1);
						return true;
					}
					
					
//					Print(CurrentBar + " " + startbar + " " + endbar + " " + high +
//					 " " + low + " " + rect.EndAnchor.BarsAgo + " " + rect.Tag);
					
//					if (endbar == CurrentBar)
//					{
//						lateral = true;
//						break;
//					}
				}
			}
			
//			if (lateral)
//			{
////				Print("Bar " + CurrentBar + " lateral " + " high" + 
////				high + " low " + low);
				
//				if (high <= Low[0]) cont = false;
//				else if (low >= High[0]) cont = false;
//				else
//				{
////					Print("Bar " + CurrentBar + " ELSE ");
//					rect.EndAnchor.BarsAgo = 0;
//					cont = true;
//				}
//			}
			
//			return cont;
			return false;
		}
		
		protected override void OnBarUpdate()
		{
			try
			{
				if (!ContinueLateral()) DrawLateral();
				
			}
			catch(System.Exception e)
			{
				Print(e.ToString());	
			}
			
			//Add your custom indicator logic here.
			
			/*bool h1 = High[0] >= High[1];
			bool h2 = High[0] >= High[2];
			
			bool l1 = Low[0] <= Low[1];
			bool l2 = Low[0] <= Low[2];
			
			bool ch1 = High[0] >= Close[1];
			bool ch2 = High[0] >= Close[2];
			
			bool cl1 = Low[0] <= Close[1];
			bool cl2 = Low[0] <= Close[2];
			
			if (m_Init)
			{
				if (h1 && h2 && l1 && l2 && ch1 && ch2 && cl1 && cl2)
				{
					
					Draw.Rectangle(this, "rect", CurrentBar - 3, High[CurrentBar-3],CurrentBar, High[CurrentBar],false, "");
					m_Init = false;
				}
			}*/
			
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
		private xLateralThree[] cachexLateralThree;
		public xLateralThree xLateralThree(int barsBack)
		{
			return xLateralThree(Input, barsBack);
		}

		public xLateralThree xLateralThree(ISeries<double> input, int barsBack)
		{
			if (cachexLateralThree != null)
				for (int idx = 0; idx < cachexLateralThree.Length; idx++)
					if (cachexLateralThree[idx] != null && cachexLateralThree[idx].BarsBack == barsBack && cachexLateralThree[idx].EqualsInput(input))
						return cachexLateralThree[idx];
			return CacheIndicator<xLateralThree>(new xLateralThree(){ BarsBack = barsBack }, input, ref cachexLateralThree);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xLateralThree xLateralThree(int barsBack)
		{
			return indicator.xLateralThree(Input, barsBack);
		}

		public Indicators.xLateralThree xLateralThree(ISeries<double> input , int barsBack)
		{
			return indicator.xLateralThree(input, barsBack);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xLateralThree xLateralThree(int barsBack)
		{
			return indicator.xLateralThree(Input, barsBack);
		}

		public Indicators.xLateralThree xLateralThree(ISeries<double> input , int barsBack)
		{
			return indicator.xLateralThree(input, barsBack);
		}
	}
}

#endregion
