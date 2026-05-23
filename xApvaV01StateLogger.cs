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
		private string summaryPath;
		private bool summaryHeaderWritten;
		private string transitionPath;
		private bool transitionHeaderWritten;
		private string transitionProbabilityPath;
		private bool transitionProbabilityHeaderWritten;
		private string runLengthPath;
		private bool runLengthHeaderWritten;
		
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
				
				string indicatorDir = Path.Combine(
				    NinjaTrader.Core.Globals.UserDataDir,
				    "bin",
				    "Custom",
				    "Indicators");
				
				outputPath = Path.Combine(
				    indicatorDir,
				    "xApvaV01StateLog_" + safeInstrumentName + ".csv");
				
				summaryPath = Path.Combine(
				    indicatorDir,
				    "xApvaV01SessionStats.csv");

                headerWritten = false;
				
				sessionStats = new xApvaV01SessionStats();
				
				summaryHeaderWritten = false;
				
				Print("APVA session stats initialized");
				
				transitionPath = Path.Combine(
				    indicatorDir,
				    "xApvaV01Transitions.csv");
				
				transitionProbabilityPath = Path.Combine(
				    indicatorDir,
				    "xApvaV01TransitionProbabilities.csv");
				
				transitionProbabilityHeaderWritten = false;
				
				transitionHeaderWritten = false;
				
				runLengthPath = Path.Combine(
				    indicatorDir,
				    "xApvaV01RunLengths.csv");
				
				runLengthHeaderWritten = false;

                try
				{
				    if (File.Exists(outputPath))
				        File.Delete(outputPath);
				
				    if (File.Exists(summaryPath))
				        File.Delete(summaryPath);
					
					if (File.Exists(transitionPath))
    					File.Delete(transitionPath);
					
					if (File.Exists(transitionProbabilityPath))
    					File.Delete(transitionProbabilityPath);
				}
				catch (Exception ex)
				{
				    Print("xApvaV01StateLogger: Could not delete prior log/stat files: " + ex.Message);
				}
			}
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
				
				if (!summaryHeaderWritten)
				{
				    File.AppendAllText(
				        summaryPath,
				        xApvaV01SessionStats.CsvHeader() + Environment.NewLine);
				
				    summaryHeaderWritten = true;
				}

		        if (CurrentBar % 50 == 0)
				{
				    File.AppendAllText(
				        summaryPath,
				        sessionStats.ToCsvSummary(
				            instrumentName,
				            sessionContext) + Environment.NewLine);
					
					if (!transitionHeaderWritten)
					{
					    File.AppendAllText(
					        transitionPath,
					        xApvaV01SessionStats.TransitionCsvHeader() + Environment.NewLine);
					
					    transitionHeaderWritten = true;
					}
					
					string transitionCsv =
					    sessionStats.ToTransitionCsv(
					        instrumentName,
					        sessionContext,
					        sessionStats.TotalBars);
					
					if (!string.IsNullOrEmpty(transitionCsv))
					{
					    File.AppendAllText(
					        transitionPath,
					        transitionCsv);
					}
					
					if (!transitionProbabilityHeaderWritten)
					{
					    File.AppendAllText(
					        transitionProbabilityPath,
					        xApvaV01SessionStats.TransitionProbabilityCsvHeader()
					            + Environment.NewLine);
					
					    transitionProbabilityHeaderWritten = true;
					}
					
					string probabilityCsv =
					    sessionStats.ToTransitionProbabilityCsv(
					        instrumentName,
					        sessionContext,
					        sessionStats.TotalBars);
					
					if (!string.IsNullOrEmpty(probabilityCsv))
					{
					    File.AppendAllText(
					        transitionProbabilityPath,
					        probabilityCsv);
					}
					
					if (!runLengthHeaderWritten)
					{
					    File.AppendAllText(
					        runLengthPath,
					        xApvaV01SessionStats.RunLengthCsvHeader()
					            + Environment.NewLine);
					
					    runLengthHeaderWritten = true;
					}
					
					string runLengthCsv =
					    sessionStats.ToRunLengthCsv(
					        instrumentName,
					        sessionContext,
					        sessionStats.TotalBars);
					
					if (!string.IsNullOrEmpty(runLengthCsv))
					{
					    File.AppendAllText(
					        runLengthPath,
					        runLengthCsv);
					}
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
