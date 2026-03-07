#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// Lateral
    /// </summary>
    [Description("Lateral")]
    public class xLateralMovementSimpleClose : Indicator
    {
        #region Variables
        // Wizard generated variables
        // User defined variables (add any user defined variables below)
		public const int NO_STATE = -1;
		public const int BROKEN_ABOVE = 0;
		public const int BROKEN_BELOW = 1;
		public const int INTACT = 2;
		
		
		
		public int LAT = 5;
		public int NO_LAT = 6;
		
		public int m_CurrentBar;
		
		public bool m_Draw;
		
		private xLateralMovementSimpleCloseEnums m_LateralState;
		
		private double m_High = 0;
		private double m_Low = 0;
		
		private int m_LatBar = -1;
		
		private Lat m_Lat;
		private Lat m_StateLat;
		
		private int index = 0;
		
		private Series<int> m_LateralIndicator;
		
		
		private bool m_Pierced = false;
		
		List<Lat> m_LatList = new List<Lat>();
        #endregion
		
		#region CommonFuncs
		public double GetHigh(int Bar)
		{
			return (High[m_CurrentBar-Bar]);
		}
		
		public double GetLow(int Bar)
		{
			return (Low[m_CurrentBar-Bar]);	
		}
		
		public double GetClose(int Bar)
		{
			return (Close[m_CurrentBar-Bar]);
		}
		
		public double GetOpen(int Bar)
		{
			return (Open[m_CurrentBar-Bar]);
		}
		#endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        
		
		
		public class Lat
		{
			public string m_Tag;
			public int m_StartBar;
			public int m_EndBar;
			public double m_High;
			public double m_Low;
			public Brush m_Color = Brushes.Gray;
		};
		
		private void DrawLatRectangle(Lat lat)
		{
			if (!m_Draw) return;
			
			RemoveDrawObject(lat.m_Tag);
			
			Print(CurrentBar +  "\t" + lat.m_Color.ToString());
			
			Rectangle mRect = Draw.Rectangle(this, lat.m_Tag, false, CurrentBar - lat.m_StartBar, 
				lat.m_High, CurrentBar - lat.m_EndBar, lat.m_Low, Brushes.Black, 
										lat.m_Color, 10);
			
			mRect.OutlineStroke.Pen = new Pen(Brushes.Black, 1);
			
		}
		
		private void CreateLat(int Bar)
		{
			m_Lat = new Lat();
				
			m_Lat.m_StartBar = m_LatBar;
			m_Lat.m_EndBar = Bar;
			
			m_Lat.m_High = High[CurrentBar - m_LatBar];
			m_Lat.m_Low = Low[CurrentBar - m_LatBar];
			
			m_Lat.m_Tag = System.Convert.ToString(m_Lat.m_High) + System.Convert.ToString(m_Lat.m_Low);
			
			if (((m_Lat.m_High < High[CurrentBar- Bar]) || (m_Lat.m_Low > Low[CurrentBar-Bar])) && (m_Lat.m_High >= Close[CurrentBar-Bar])
				&& (m_Lat.m_Low <= Close[CurrentBar-Bar])) 
			{
				m_Pierced = true;
			}
			
			if (m_Pierced) 
			{
				m_Lat.m_Color = Brushes.Orange;
				Print(CurrentBar +  "\t" + Time[0] + "\t" + "Orange" + "\t" + m_Lat.m_Color.ToString());
				
			}
			
			DrawLatRectangle(m_Lat);	
			
			m_LatList.Add(m_Lat);
			
		}
		public string GetLatealStateString()
		{
			Update();
			
			switch(m_LateralState)
			{
				case xLateralMovementSimpleCloseEnums.NO_STATE:
					return "NO_STATE";
				break;
					
				case xLateralMovementSimpleCloseEnums.BROKEN_ABOVE:
					return "BROKEN_ABOVE";
				break;
					
				case xLateralMovementSimpleCloseEnums.BROKEN_BELOW:
					return "BROKEN_BELOW";
				break;
					
				case xLateralMovementSimpleCloseEnums.INTACT:
					return "INTACT";
				break;
					
				default:
					return "NO_STATE";
				break;
			}
		}
		
		private void SimpleLateral(int Bar)
		{
			double pricehigh = Close[CurrentBar-Bar];
			double pricelow = Close[CurrentBar-Bar];

			
			if ((m_High >= pricehigh && (m_Low <= pricelow))) CreateLat(Bar);
			else
			{
				if (m_Lat != null) CreateLat(Bar);
				
				
				m_LatBar = Bar;
				m_High = High[CurrentBar-Bar];
				m_Low = Low[CurrentBar-Bar];
				
				m_Lat = null;
			}
		}
		
		protected override void OnStateChange()
        {
			if (State == State.SetDefaults) 
   			{ 
	            IsOverlay							= true;
				Calculate							= Calculate.OnBarClose;
	            //PriceTypeSupported	= false;
				
				BarsRequiredToPlot  = 3; 
				
				m_Draw = true;
				
				m_LateralIndicator = new Series<int>(this);
				
				m_LateralState = xLateralMovementSimpleCloseEnums.NO_STATE;
			}
			
        }

		public bool IsBarLateral(int Bar)
		{
			if (m_Lat == null) return false;
			else if ((m_Lat.m_StartBar <= Bar) && ( m_Lat.m_EndBar >= Bar)) return true;
			else return false;
		}
		
		public xLateralMovementSimpleCloseEnums GetLateralState(int Bar)
		{
			bool lateral = IsBarLateral(Bar);
			
			if (m_Lat != null) Print(Bar + "\t" + m_Lat.m_High + "\t" + High[CurrentBar-Bar] + "\t" + m_Lat.m_Low);
			
			if (lateral) return xLateralMovementSimpleCloseEnums.INTACT;
			else
			{
				if (m_Lat == null) return xLateralMovementSimpleCloseEnums.NO_STATE;
				else if (m_Lat.m_High < Low[CurrentBar-Bar]) return xLateralMovementSimpleCloseEnums.BROKEN_ABOVE;
				else if (m_Lat.m_Low > High[CurrentBar-Bar]) return xLateralMovementSimpleCloseEnums.BROKEN_BELOW;
				else return xLateralMovementSimpleCloseEnums.NO_STATE;
			}
		}
		
		private void SetLateralState(int Bar)
		{
			bool lateral = false;
			double high = 0;
			double low = 0;
			
			foreach (IDrawingTool draw in DrawObjects)
 			{
				if (draw is DrawingTools.Rectangle)
				{
					DrawingTools.Rectangle rect = (DrawingTools.Rectangle) draw;
					
					int startbar = (CurrentBar - rect.StartAnchor.BarsAgo);
					int endbar = (CurrentBar - rect.EndAnchor.BarsAgo);
					
					high = rect.StartAnchor.Price;
					low = rect.EndAnchor.Price;
					
					if ((startbar <= Bar) && ( endbar >= Bar)) 
					{
						lateral = true;
						break;
					}
				}
			}
			
			if (lateral)
			{
				if ((high >= Close[CurrentBar-Bar]) && (low <= Close[CurrentBar-Bar]))
				{
					m_LateralState = xLateralMovementSimpleCloseEnums.INTACT;
				}
				else if (high < Low[CurrentBar-Bar]) 
				{
					m_LateralState = xLateralMovementSimpleCloseEnums.BROKEN_ABOVE;
					m_Pierced = false;
				}
				else if (low > High[CurrentBar-Bar]) 
				{
					m_LateralState = xLateralMovementSimpleCloseEnums.BROKEN_BELOW;
					m_Pierced = false;
				}
			}
			else 
			{
				m_LateralState = xLateralMovementSimpleCloseEnums.NO_STATE;
				m_Pierced = false;
			}
			
			
		}
		
		public int GetLatStartBar(int Bar)
		{
			if (!IsBarLateral(Bar)) return -1;
			
			return m_Lat.m_StartBar;
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
			m_CurrentBar = CurrentBar;
			
			try
			{
				SimpleLateral(m_CurrentBar);
				
				SetLateralState(m_CurrentBar);
				
				m_LateralIndicator[0] = (int)m_LateralState;
				
				//Print(CurrentBar + "\t" + GetLatealStateString() + "\t" + m_Pierced);
			}
			catch(System.Exception e)
			{
				Print(e.ToString());	
			}

        }

        #region Properties
		public xLateralMovementSimpleCloseEnums this[int Bar]
		{
			get
			{
				Update();
				
				//Print("xLateralMovementSimpleCloseEnums : " + m_LateralIndicator[CurrentBar - Bar]);
				if (Bar < 0) return xLateralMovementSimpleCloseEnums.NO_STATE;
				//else return (xLateralMovementSimpleCloseEnums)m_LateralIndicator[CurrentBar - Bar];
				else return GetLateralState(Bar);
			}
		}
		
		
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xLateralMovementSimpleClose[] cachexLateralMovementSimpleClose;
		public xLateralMovementSimpleClose xLateralMovementSimpleClose()
		{
			return xLateralMovementSimpleClose(Input);
		}

		public xLateralMovementSimpleClose xLateralMovementSimpleClose(ISeries<double> input)
		{
			if (cachexLateralMovementSimpleClose != null)
				for (int idx = 0; idx < cachexLateralMovementSimpleClose.Length; idx++)
					if (cachexLateralMovementSimpleClose[idx] != null &&  cachexLateralMovementSimpleClose[idx].EqualsInput(input))
						return cachexLateralMovementSimpleClose[idx];
			return CacheIndicator<xLateralMovementSimpleClose>(new xLateralMovementSimpleClose(), input, ref cachexLateralMovementSimpleClose);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xLateralMovementSimpleClose xLateralMovementSimpleClose()
		{
			return indicator.xLateralMovementSimpleClose(Input);
		}

		public Indicators.xLateralMovementSimpleClose xLateralMovementSimpleClose(ISeries<double> input )
		{
			return indicator.xLateralMovementSimpleClose(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xLateralMovementSimpleClose xLateralMovementSimpleClose()
		{
			return indicator.xLateralMovementSimpleClose(Input);
		}

		public Indicators.xLateralMovementSimpleClose xLateralMovementSimpleClose(ISeries<double> input )
		{
			return indicator.xLateralMovementSimpleClose(input);
		}
	}
}

#endregion
