#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Draws the previous session's high and low across the current intraday session.
	/// </summary>
	public class PreviousSessionHighLow : Indicator
	{
		private DateTime			currentTradingDay;
		private bool				hasCurrentSession;
		private bool				hasPriorSession;
		private double				currentSessionHigh;
		private double				currentSessionLow;
		private double				priorSessionHigh;
		private double				priorSessionLow;
		private int					sessionStartBar;
		private SessionIterator		sessionIterator;
		private Series<double>		priorHighSeries;
		private Series<double>		priorLowSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Draws horizontal lines for the previous session's high and low on the current intraday session.";
				Name						= "Previous Session High Low";
				IsAutoScale					= false;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= true;
				BarsRequiredToPlot			= 0;
			}
			else if (State == State.Configure)
			{
				currentTradingDay	= Core.Globals.MinDate;
				hasCurrentSession	= false;
				hasPriorSession		= false;
				currentSessionHigh	= double.MinValue;
				currentSessionLow	= double.MaxValue;
				priorSessionHigh	= double.NaN;
				priorSessionLow		= double.NaN;
				sessionStartBar		= 0;
				sessionIterator		= null;
			}
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);
				priorHighSeries = new Series<double>(this);
				priorLowSeries = new Series<double>(this);
			}
			else if (State == State.Historical && !Bars.BarsType.IsIntraday)
			{
				const string message = "Previous Session High Low requires an intraday chart.";
				Draw.TextFixed(this, "PreviousSessionHighLowInfo", message, TextPosition.BottomRight);
				Log(message, LogLevel.Error);
			}
		}

		protected override void OnBarUpdate()
		{
			if (!Bars.BarsType.IsIntraday)
				return;

			DateTime tradingDay = sessionIterator.GetTradingDay(Time[0]);

			if (!hasCurrentSession || tradingDay != currentTradingDay)
			{
				if (hasCurrentSession)
				{
					priorSessionHigh = currentSessionHigh;
					priorSessionLow = currentSessionLow;
					hasPriorSession = true;
				}

				currentTradingDay = tradingDay;
				currentSessionHigh = High[0];
				currentSessionLow = Low[0];
				sessionStartBar = CurrentBar;
				hasCurrentSession = true;
			}
			else
			{
				currentSessionHigh = Math.Max(currentSessionHigh, High[0]);
				currentSessionLow = Math.Min(currentSessionLow, Low[0]);
			}

			if (hasPriorSession)
			{
				priorHighSeries[0] = priorSessionHigh;
				priorLowSeries[0] = priorSessionLow;

				int startBarsAgo = CurrentBar - sessionStartBar;
				string dayTag = currentTradingDay.ToString("yyyyMMdd");

				Draw.Line(this, "PreviousSessionHigh_" + dayTag, false, startBarsAgo, priorSessionHigh, 0, priorSessionHigh, Brushes.DarkBlue, DashStyleHelper.Solid, 2);
				Draw.Line(this, "PreviousSessionLow_" + dayTag, false, startBarsAgo, priorSessionLow, 0, priorSessionLow, Brushes.Red, DashStyleHelper.Solid, 2);
			}
			else
			{
				priorHighSeries[0] = double.NaN;
				priorLowSeries[0] = double.NaN;
			}
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PriorHigh => priorHighSeries;

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PriorLow => priorLowSeries;
		#endregion

		public override string FormatPriceMarker(double price) => Instrument.MasterInstrument.FormatPrice(price);
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PreviousSessionHighLow[] cachePreviousSessionHighLow;
		public PreviousSessionHighLow PreviousSessionHighLow()
		{
			return PreviousSessionHighLow(Input);
		}

		public PreviousSessionHighLow PreviousSessionHighLow(ISeries<double> input)
		{
			if (cachePreviousSessionHighLow != null)
				for (int idx = 0; idx < cachePreviousSessionHighLow.Length; idx++)
					if (cachePreviousSessionHighLow[idx] != null && cachePreviousSessionHighLow[idx].EqualsInput(input))
						return cachePreviousSessionHighLow[idx];
			return CacheIndicator<PreviousSessionHighLow>(new PreviousSessionHighLow(), input, ref cachePreviousSessionHighLow);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PreviousSessionHighLow PreviousSessionHighLow()
		{
			return indicator.PreviousSessionHighLow(Input);
		}

		public Indicators.PreviousSessionHighLow PreviousSessionHighLow(ISeries<double> input)
		{
			return indicator.PreviousSessionHighLow(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PreviousSessionHighLow PreviousSessionHighLow()
		{
			return indicator.PreviousSessionHighLow(Input);
		}

		public Indicators.PreviousSessionHighLow PreviousSessionHighLow(ISeries<double> input)
		{
			return indicator.PreviousSessionHighLow(input);
		}
	}
}

#endregion
