#region Using declarations
using System;
using System.IO;
using NinjaTrader.NinjaScript.APVA.V01;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xApvaV01StateLogger : Indicator
    {
        private ApvaV01Analyzer analyzer;
        private xApvaV01SessionStats sessionStats;
		private string outputPath;
        private bool headerWritten;
		private bool summaryPrinted;
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xApvaV01StateLogger";
                Description = "APVA v0.1 state logger. No trade signals.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
            }
            else if (State == State.Configure)
            {
                analyzer = new ApvaV01Analyzer();
            }
            else if (State == State.DataLoaded)
            {
                string instrumentName = Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "UnknownInstrument";

				string safeInstrumentName = MakeSafeFileName(instrumentName);
				
				outputPath = Path.Combine(
				    NinjaTrader.Core.Globals.UserDataDir,
				    "xApvaV01StateLog_" + safeInstrumentName + ".csv");

                headerWritten = false;
				
				sessionStats = new xApvaV01SessionStats();
				
				Print("APVA session stats initialized");

                try
                {
                    if (File.Exists(outputPath))
                        File.Delete(outputPath);
                }
                catch (Exception ex)
                {
                    Print("xApvaV01StateLogger: Could not delete prior log: " + ex.Message);
                }
			}
        }

		private void PrintSessionStats()
		{
		    if (sessionStats == null)
		        return;
		
		    string instrumentName =
		        Instrument != null && Instrument.MasterInstrument != null
		            ? Instrument.MasterInstrument.Name
		            : "Unknown";
		
		    string sessionContext =
		        GetSessionContext();
		
		    Print(sessionStats.ToSummaryString(instrumentName, sessionContext));
		}

		private static string MakeSafeFileName(string value)
		{
		    if (string.IsNullOrEmpty(value))
		        return "UnknownInstrument";
		
		    foreach (char c in Path.GetInvalidFileNameChars())
		        value = value.Replace(c, '_');
		
		    return value;
		}

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

            var snapshot = analyzer.Update(
                CurrentBar,
                Time[0],
                Open[0],
                High[0],
                Low[0],
                Close[0],
                Volume[0]);

            WriteSnapshot(snapshot);
        }

		private string GetSessionContext()
		{
		    return Bars != null && Bars.IsFirstBarOfSession
		        ? "RTH"
		        : "ETH";
		}

        private void WriteSnapshot(ApvaStateSnapshot snapshot)
		{
		    try
		    {
		        if (!headerWritten)
		        {
		            File.AppendAllText(
		                outputPath,
		                ApvaV01SnapshotFormatter.CsvHeader() + Environment.NewLine);
		
		            headerWritten = true;
		        }
		
		        string instrumentName =
		            Instrument != null && Instrument.MasterInstrument != null
		                ? Instrument.MasterInstrument.Name
		                : "UnknownInstrument";
		
		        string sessionContext = GetSessionContext();
		
		        File.AppendAllText(
		            outputPath,
		            ApvaV01SnapshotFormatter.ToCsv(snapshot, instrumentName, sessionContext)
		                + Environment.NewLine);
		
		        if (sessionStats != null)
		            sessionStats.Accumulate(snapshot);
				
				string summaryPath = Path.Combine(
				    NinjaTrader.Core.Globals.UserDataDir,
				    "xApvaV01SessionStats.csv");
		
		        if (!summaryPrinted &&
				    CurrentBar == Count - 1)
				{
				    File.AppendAllText(
				        summaryPath,
				        sessionStats.ToCsvSummary(
				            instrumentName,
				            sessionContext) + Environment.NewLine);
				
				    summaryPrinted = true;
				}
		    }
		    catch (Exception ex)
		    {
		        Print("xApvaV01StateLogger: Write failed: " + ex.Message);
		    }
		}
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xApvaV01StateLogger[] cachexApvaV01StateLogger;
		public xApvaV01StateLogger xApvaV01StateLogger()
		{
			return xApvaV01StateLogger(Input);
		}

		public xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input)
		{
			if (cachexApvaV01StateLogger != null)
				for (int idx = 0; idx < cachexApvaV01StateLogger.Length; idx++)
					if (cachexApvaV01StateLogger[idx] != null &&  cachexApvaV01StateLogger[idx].EqualsInput(input))
						return cachexApvaV01StateLogger[idx];
			return CacheIndicator<xApvaV01StateLogger>(new xApvaV01StateLogger(), input, ref cachexApvaV01StateLogger);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xApvaV01StateLogger xApvaV01StateLogger()
		{
			return indicator.xApvaV01StateLogger(Input);
		}

		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input )
		{
			return indicator.xApvaV01StateLogger(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xApvaV01StateLogger xApvaV01StateLogger()
		{
			return indicator.xApvaV01StateLogger(Input);
		}

		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input )
		{
			return indicator.xApvaV01StateLogger(input);
		}
	}
}

#endregion
