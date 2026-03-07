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
	public class xFRVPeakIndicator : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Determines the FRV or peak volume value for a given UV value, start and end timeslots.";
				Name										= "xFRVPeakIndicator";
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
				UV					= 0.1;
				BeginTimeSlot					= 		DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				EndTimeSlot						= 		DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
				VolumeType					= @"FRV";
				AddPlot(new Stroke(Brushes.LawnGreen, 2), PlotStyle.Bar, "FRVorPEAKValue");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.Realtime)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;
			//Add your custom indicator logic here.
			
			TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			System.DateTime nowTime = DateTime.Parse(System.DateTime.Now.ToShortTimeString());
			System.DateTime dtnowest = TimeZoneInfo.ConvertTime(nowTime, est);
			System.TimeSpan now = new System.TimeSpan(
                dtnowest.Hour, dtnowest.Minute, dtnowest.Second);
			
			System.TimeSpan beginTimeSlotSpan = new System.TimeSpan(
                BeginTimeSlot.Hour, BeginTimeSlot.Minute, BeginTimeSlot.Second);
			System.TimeSpan endTimeSlotSpan = new System.TimeSpan(
                EndTimeSlot.Hour, EndTimeSlot.Minute, EndTimeSlot.Second);
			
			double todayUV = Volume[0]/SMA(Volume,65)[0];
			
			PrintTo = PrintTo.OutputTab2;
			
			string tab = "\t\t\t\t";
			
			string symbol = Instrument.ToString().Split(' ')[0];
			string str = CurrentBar.ToString() + tab + BeginTimeSlot.ToString() + tab + 
				EndTimeSlot.ToString() + tab + nowTime.ToString() + tab + System.Convert.ToString(todayUV) + tab + 
				System.Convert.ToString(UV);
			
			Print(symbol + tab + str);
			if ((beginTimeSlotSpan <= now ) && (endTimeSlotSpan > now) && (todayUV >= UV))
			{
				FRVorPEAKValue[0] = todayUV;
				//Print(todayUV);
			}
			
			//PrintTo = PrintTo.OutputTab2;
			//Print(CurrentBar + "           " + Volume[0]  + "        " + SMA(Volume,65)[0] + "          " + todayUV + "     " + nowTime);
 		}

		#region Properties
		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="UV", Description="Unusual Volume", Order=1, GroupName="Parameters")]
		public double UV
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="BeginTimeSlot", Description="Begin Time Slot", Order=2, GroupName="Parameters")]
		public DateTime BeginTimeSlot
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="EndTimeSlot", Description="End time slot", Order=3, GroupName="Parameters")]
		public DateTime EndTimeSlot
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="VolumeType", Description="Type of volume - FRV or PEAK", Order=4, GroupName="Parameters")]
		public string VolumeType
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FRVorPEAKValue
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
		private xFRVPeakIndicator[] cachexFRVPeakIndicator;
		public xFRVPeakIndicator xFRVPeakIndicator(double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			return xFRVPeakIndicator(Input, uV, beginTimeSlot, endTimeSlot, volumeType);
		}

		public xFRVPeakIndicator xFRVPeakIndicator(ISeries<double> input, double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			if (cachexFRVPeakIndicator != null)
				for (int idx = 0; idx < cachexFRVPeakIndicator.Length; idx++)
					if (cachexFRVPeakIndicator[idx] != null && cachexFRVPeakIndicator[idx].UV == uV && cachexFRVPeakIndicator[idx].BeginTimeSlot == beginTimeSlot && cachexFRVPeakIndicator[idx].EndTimeSlot == endTimeSlot && cachexFRVPeakIndicator[idx].VolumeType == volumeType && cachexFRVPeakIndicator[idx].EqualsInput(input))
						return cachexFRVPeakIndicator[idx];
			return CacheIndicator<xFRVPeakIndicator>(new xFRVPeakIndicator(){ UV = uV, BeginTimeSlot = beginTimeSlot, EndTimeSlot = endTimeSlot, VolumeType = volumeType }, input, ref cachexFRVPeakIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xFRVPeakIndicator xFRVPeakIndicator(double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			return indicator.xFRVPeakIndicator(Input, uV, beginTimeSlot, endTimeSlot, volumeType);
		}

		public Indicators.xFRVPeakIndicator xFRVPeakIndicator(ISeries<double> input , double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			return indicator.xFRVPeakIndicator(input, uV, beginTimeSlot, endTimeSlot, volumeType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xFRVPeakIndicator xFRVPeakIndicator(double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			return indicator.xFRVPeakIndicator(Input, uV, beginTimeSlot, endTimeSlot, volumeType);
		}

		public Indicators.xFRVPeakIndicator xFRVPeakIndicator(ISeries<double> input , double uV, DateTime beginTimeSlot, DateTime endTimeSlot, string volumeType)
		{
			return indicator.xFRVPeakIndicator(input, uV, beginTimeSlot, endTimeSlot, volumeType);
		}
	}
}

#endregion
