#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// 
    /// </summary>
    [Description("")]
    public class xLateralEx : Indicator
    {
        #region Variables
        // Wizard generated variables
        // User defined variables (add any user defined variables below)
		double m_LatHigh = 0;
		double m_LatLow = 0;
		
		xLateralStateEnums m_LateralState = xLateralStateEnums.NO_STATE;
		xLateralPiercedStateEnums m_LateralPiercedState = xLateralPiercedStateEnums.NO_STATE;
		
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void OnStateChange()
        {
			if (State == State.SetDefaults) 
   			{ 
            	IsOverlay							= false;
				BarsRequiredToPlot  = 2;
			}
        }
		
		private void CheckInitLateral()
		{
			if (((High[2] >= High[1]) && (High[2] >= High[0])) && ((Low[2] <= Low[1]) && (Low[2] <= Low[0])))
			{
				m_LatHigh = High[2];
				m_LatLow = Low[2];
				
				m_LateralState = xLateralStateEnums.INTACT;
			}
			else m_LateralState = xLateralStateEnums.NO_STATE;
				
		}

		private void CheckForLateral()
		{
			if ((High[2] >= Close[0]) && (Low[2] <= Close[0])) 
			{	
				m_LatHigh = High[2];
				m_LatLow = Low[2];
				
				m_LateralState = xLateralStateEnums.INTACT;
			}
			else m_LateralState = xLateralStateEnums.NO_STATE;
		}
		
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
			
			if (CurrentBar < 2) return;
			
			switch(m_LateralState)
			{
				case xLateralStateEnums.NO_STATE:
					CheckInitLateral();
				break;
					
				case xLateralStateEnums.INTACT:
					if (m_LatHigh <= Low[0]) m_LateralState = xLateralStateEnums.BROKEN_ABOVE;
					else if (m_LatLow >= High[0]) m_LateralState = xLateralStateEnums.BROKEN_BELOW;
				break;
					
				case xLateralStateEnums.BROKEN_BELOW:
				case xLateralStateEnums.BROKEN_ABOVE:
					CheckInitLateral();
				break;
					
				default:
					
				break;
			}
			
			if (m_LateralState == xLateralStateEnums.INTACT)
			{
				if (m_LatHigh < High[0]) m_LateralPiercedState = xLateralPiercedStateEnums.PIERCED_ABOVE;
				else if (m_LatLow > Low[0]) m_LateralPiercedState = xLateralPiercedStateEnums.PIERCED_BELOW;
				else m_LateralPiercedState = xLateralPiercedStateEnums.NO_STATE;
			}
			else m_LateralPiercedState = xLateralPiercedStateEnums.NO_STATE;
        }

        #region Properties
		public xLateralStateEnums this[int Bar]
		{
			get
			{
				Update();
				
				return m_LateralState;
				
			}
		}
		
		public xLateralPiercedStateEnums LateralPiercedState
		{
			get 
			{
				Update();
				
				return m_LateralPiercedState; 
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
		private xLateralEx[] cachexLateralEx;
		public xLateralEx xLateralEx()
		{
			return xLateralEx(Input);
		}

		public xLateralEx xLateralEx(ISeries<double> input)
		{
			if (cachexLateralEx != null)
				for (int idx = 0; idx < cachexLateralEx.Length; idx++)
					if (cachexLateralEx[idx] != null &&  cachexLateralEx[idx].EqualsInput(input))
						return cachexLateralEx[idx];
			return CacheIndicator<xLateralEx>(new xLateralEx(), input, ref cachexLateralEx);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xLateralEx xLateralEx()
		{
			return indicator.xLateralEx(Input);
		}

		public Indicators.xLateralEx xLateralEx(ISeries<double> input )
		{
			return indicator.xLateralEx(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xLateralEx xLateralEx()
		{
			return indicator.xLateralEx(Input);
		}

		public Indicators.xLateralEx xLateralEx(ISeries<double> input )
		{
			return indicator.xLateralEx(input);
		}
	}
}

#endregion
