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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class xQMFI : Indicator
	{
		Series<double> qmfi;
		private xSeriesInputType	m_SeriesInputType = xSeriesInputType.TypPriceXVol;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"QCharts MFI";
				Name								= "xQMFI";
				Calculate							= Calculate.OnEachTick;
				IsOverlay							= false;
				DisplayInDataBox					= true;
				DrawOnPricePanel					= false;
				DrawHorizontalGridLines				= true;
				DrawVerticalGridLines				= true;
				PaintPriceMarkers					= true;
				ScaleJustification					= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive			= true;
				Period					= 14;
				AddPlot(Brushes.Blue, "QMFI");
				
				AddLine(new Stroke(Brushes.Blue, DashStyleHelper.DashDotDot,1),45, "Zero");
				AddLine(new Stroke(Brushes.Red, DashStyleHelper.Solid,1),50,"First");
				AddLine(new Stroke(Brushes.Blue, DashStyleHelper.Solid, 1), 55, "Second");
				AddLine(new Stroke(Brushes.Yellow, DashStyleHelper.Solid, 2), 60, "Three");
				AddLine(new Stroke(Brushes.Red, DashStyleHelper.Solid, 2), 70, "Fourth");
				
				qmfi = new Series<double>(this);
				
				BarsRequiredToPlot = 20;
			}
			else if (State == State.Configure)
			{
			}
		}
		
		public string GetCurrentPeakLevel()
		{
			Update();
			
			if (QMFI[0] < QMFI[1]) return "";
			
			double value = QMFI[0];
			
			if ((value >= 46) && (value < 47)) return "VLW_PEAK";
			else if ((value >= 47) && (value < 50)) return "LOW_PEAK";
			else if ((value >= 50) && (value < 55)) return "MINOR_PEAK";
			else if ((value >=55) && (value < 60)) return "MID_PEAK";
			else if ((value >=60) && (value < 70)) return "HIGH_PEAK";
			else if (value >=70) return "XTRM_PEAK";
			else return "";	
		}
		
		private double GetProdValue(int Bar)
		{
			double val = 0;
			
			switch(m_SeriesInputType)
			{
				case xSeriesInputType.TypPriceXVol:
					val = Typical[0]*Volume[0];
				break;
					
				case xSeriesInputType.Volume:
					val = Volume[0];
				break;
					
				case xSeriesInputType.BarVolatility:
					val = High[0] - Low[0];
				break;
					
				case xSeriesInputType.BarVolatilityCloseToClose:
					val = Math.Abs(Close[0] - Close[1]);
				break;
					
				default:
					val = 0;
				break;
			}
			
			return val;
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			double prod = 0;
			double avgp = 0;
			
			prod = GetProdValue(CurrentBar);
			
			qmfi[0] = prod;
			
			QMFI[0] = RSI(qmfi, Period, 1)[0];
		}

		#region Properties
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Period", Order=1, GroupName="Parameters")]
		public int Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Input Type", Order=2, GroupName="Parameters")]
		 public xSeriesInputType InputType
        {
            get { return m_SeriesInputType; }
            set { m_SeriesInputType = value; }
        }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> QMFI
		{
			get { return Values[0]; }
		}

		



		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xQMFI[] cachexQMFI;
		public xQMFI xQMFI(int period, xSeriesInputType inputType)
		{
			return xQMFI(Input, period, inputType);
		}

		public xQMFI xQMFI(ISeries<double> input, int period, xSeriesInputType inputType)
		{
			if (cachexQMFI != null)
				for (int idx = 0; idx < cachexQMFI.Length; idx++)
					if (cachexQMFI[idx] != null && cachexQMFI[idx].Period == period && cachexQMFI[idx].InputType == inputType && cachexQMFI[idx].EqualsInput(input))
						return cachexQMFI[idx];
			return CacheIndicator<xQMFI>(new xQMFI(){ Period = period, InputType = inputType }, input, ref cachexQMFI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xQMFI xQMFI(int period, xSeriesInputType inputType)
		{
			return indicator.xQMFI(Input, period, inputType);
		}

		public Indicators.xQMFI xQMFI(ISeries<double> input , int period, xSeriesInputType inputType)
		{
			return indicator.xQMFI(input, period, inputType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xQMFI xQMFI(int period, xSeriesInputType inputType)
		{
			return indicator.xQMFI(Input, period, inputType);
		}

		public Indicators.xQMFI xQMFI(ISeries<double> input , int period, xSeriesInputType inputType)
		{
			return indicator.xQMFI(input, period, inputType);
		}
	}
}

#endregion
