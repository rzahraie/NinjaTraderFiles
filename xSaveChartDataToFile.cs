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
using System.IO;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class xSaveChartDataToFile : Indicator
	{
		private string path;
		private StreamWriter sw; // a variable for the StreamWriter that will be used 
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Save the chart data to a file.";
				Name										= "xSaveChartDataToFile";
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
				
				
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			path 			= NinjaTrader.Core.Globals.UserDataDir;
			path += Instrument.FullName + Instrument + BarsPeriod.Value.ToString() +  BarsPeriod.BarsPeriodType.ToString() + ".csv";
			//Add your custom indicator logic here.
			sw = File.AppendText(path);  // Open the path for writing
			sw.WriteLine(Time[0] + "," + Open[0] + "," + High[0] + "," + Low[0] + "," + Close[0] + "," + Volume[0]); // Append a new line to the file
			sw.Close(); // Close the file to allow future calls to access the file again.
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSaveChartDataToFile[] cachexSaveChartDataToFile;
		public xSaveChartDataToFile xSaveChartDataToFile()
		{
			return xSaveChartDataToFile(Input);
		}

		public xSaveChartDataToFile xSaveChartDataToFile(ISeries<double> input)
		{
			if (cachexSaveChartDataToFile != null)
				for (int idx = 0; idx < cachexSaveChartDataToFile.Length; idx++)
					if (cachexSaveChartDataToFile[idx] != null &&  cachexSaveChartDataToFile[idx].EqualsInput(input))
						return cachexSaveChartDataToFile[idx];
			return CacheIndicator<xSaveChartDataToFile>(new xSaveChartDataToFile(), input, ref cachexSaveChartDataToFile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSaveChartDataToFile xSaveChartDataToFile()
		{
			return indicator.xSaveChartDataToFile(Input);
		}

		public Indicators.xSaveChartDataToFile xSaveChartDataToFile(ISeries<double> input )
		{
			return indicator.xSaveChartDataToFile(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSaveChartDataToFile xSaveChartDataToFile()
		{
			return indicator.xSaveChartDataToFile(Input);
		}

		public Indicators.xSaveChartDataToFile xSaveChartDataToFile(ISeries<double> input )
		{
			return indicator.xSaveChartDataToFile(input);
		}
	}
}

#endregion
