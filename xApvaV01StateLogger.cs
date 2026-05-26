#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
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
		private xApvaV01CsvReportWriter reportWriter;

		[NinjaScriptProperty]
		[Display(Name = "OutputRoot", Order = 100, GroupName = "Export")]
		public string OutputRoot { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "ExportTag", Order = 101, GroupName = "Export")]
		public string ExportTag { get; set; }
		
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
				OutputRoot = Path.Combine(
				    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				    "ApvaAnalysis",
				    "data",
				    "Validation");
				ExportTag = "";
            }
            else if (State == State.Configure)
            {
                analyzer = new ApvaV01Analyzer();
            }
            else if (State == State.DataLoaded)
            {
                string instrumentName = Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "UnknownInstrument";

				string safeInstrumentName = MakeSafeFileName(instrumentName);

				string tag = string.IsNullOrWhiteSpace(ExportTag)
				    ? ""
				    : ExportTag.Trim();

				string rawInstrumentDir = Path.Combine(OutputRoot, safeInstrumentName);
				string reportsDir = Path.Combine(OutputRoot, "Reports");
				string reportSuffix = "_" + safeInstrumentName + tag + ".csv";

				Directory.CreateDirectory(rawInstrumentDir);
				Directory.CreateDirectory(reportsDir);
				
				outputPath = Path.Combine(
				    rawInstrumentDir,
				    "xApvaV01StateLog_" + safeInstrumentName + tag + ".csv");

				Print("xApvaV01StateLogger outputPath: " + outputPath);

                headerWritten = false;
				
				sessionStats = new xApvaV01SessionStats();
				
				reportWriter = new xApvaV01CsvReportWriter();

				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01SessionStats" + reportSuffix),
				    xApvaV01SessionStats.CsvHeader(),
				    () => sessionStats.ToCsvSummary(instrumentName, GetSessionContext())
				        + Environment.NewLine);
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01Transitions" + reportSuffix),
				    xApvaV01SessionStats.TransitionCsvHeader(),
				    () => sessionStats.ToTransitionCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01TransitionProbabilities" + reportSuffix),
				    xApvaV01SessionStats.TransitionProbabilityCsvHeader(),
				    () => sessionStats.ToTransitionProbabilityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01RunLengths" + reportSuffix),
				    xApvaV01SessionStats.RunLengthCsvHeader(),
				    () => sessionStats.ToRunLengthCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01EntryStats" + reportSuffix),
				    xApvaV01SessionStats.EntryStatsCsvHeader(),
				    () => sessionStats.ToEntryStatsCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01DurationBucketTransitions" + reportSuffix),
				    xApvaV01SessionStats.DurationBucketTransitionCsvHeader(),
				    () => sessionStats.ToDurationBucketTransitionCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01DurationBucketProbabilities" + reportSuffix),
				    xApvaV01SessionStats.DurationBucketProbabilityCsvHeader(),
				    () => sessionStats.ToDurationBucketProbabilityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01PrecursorStats" + reportSuffix),
				    xApvaV01SessionStats.PrecursorStatsCsvHeader(),
				    () => sessionStats.ToPrecursorStatsCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01HazardRates" + reportSuffix),
				    xApvaV01SessionStats.HazardCsvHeader(),
				    () => sessionStats.ToHazardCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01SponsorStates" + reportSuffix),
				    xApvaV01SessionStats.SponsorStateCsvHeader(),
				    () => sessionStats.ToSponsorStateCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01SponsorTransitions" + reportSuffix),
				    xApvaV01SessionStats.SponsorTransitionCsvHeader(),
				    () => sessionStats.ToSponsorTransitionCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(reportsDir, "xApvaV01StateTriplets" + reportSuffix),
				    xApvaV01SessionStats.StateTripletCsvHeader(),
				    () => sessionStats.ToStateTripletCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01StateTripletProbabilities" + reportSuffix),
				    xApvaV01SessionStats
				        .StateTripletProbabilityCsvHeader(),
				    () => sessionStats.ToStateTripletProbabilityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01PersistenceStats" + reportSuffix),
				    xApvaV01SessionStats.PersistenceCsvHeader(),
				    () => sessionStats.ToPersistenceCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01BirthQuality" + reportSuffix),
				    xApvaV01SessionStats.BirthQualityCsvHeader(),
				    () => sessionStats.ToBirthQualityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01IncubationQualityBuckets" + reportSuffix),
				    xApvaV01SessionStats.IncubationQualityBucketCsvHeader(),
				    () => sessionStats.ToIncubationQualityBucketCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01IncubationQualityBucketProbabilities" + reportSuffix),
				    xApvaV01SessionStats.IncubationQualityBucketProbabilityCsvHeader(),
				    () => sessionStats.ToIncubationQualityBucketProbabilityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01IncubationQualityConditionalProbabilities" + reportSuffix),
				    xApvaV01SessionStats
				        .IncubationQualityConditionalProbabilityCsvHeader(),
				    () => sessionStats
				        .ToIncubationQualityConditionalProbabilityCsv(
				            instrumentName,
				            GetSessionContext(),
				            sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01TransitionExpectancy" + reportSuffix),
				    xApvaV01SessionStats.TransitionExpectancyCsvHeader(),
				    () => sessionStats.ToTransitionExpectancyCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01StateQuadruplets" + reportSuffix),
				    xApvaV01SessionStats.StateQuadrupletCsvHeader(),
				    () => sessionStats.ToStateQuadrupletCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.AddReport(
				    Path.Combine(
				        reportsDir,
				        "xApvaV01StateQuadrupletProbabilities" + reportSuffix),
				    xApvaV01SessionStats.StateQuadruletProbabilityCsvHeader(),
				    () => sessionStats.ToStateQuadrupletProbabilityCsv(
				        instrumentName,
				        GetSessionContext(),
				        sessionStats.TotalBars));
				
				reportWriter.DeleteExistingFiles();

                try
				{
					if (File.Exists(outputPath))
    					File.Delete(outputPath);
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
		    //return Bars != null && Bars.IsFirstBarOfSession
		      //  ? "RTH"
		     //   : "ETH";
			 return "RTH";
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

		        if (CurrentBar % 50 == 0 && reportWriter != null)
    				reportWriter.WriteAll();
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
		public xApvaV01StateLogger xApvaV01StateLogger(string outputRoot, string exportTag)
		{
			return xApvaV01StateLogger(Input, outputRoot, exportTag);
		}

		public xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input, string outputRoot, string exportTag)
		{
			if (cachexApvaV01StateLogger != null)
				for (int idx = 0; idx < cachexApvaV01StateLogger.Length; idx++)
					if (cachexApvaV01StateLogger[idx] != null && cachexApvaV01StateLogger[idx].OutputRoot == outputRoot && cachexApvaV01StateLogger[idx].ExportTag == exportTag && cachexApvaV01StateLogger[idx].EqualsInput(input))
						return cachexApvaV01StateLogger[idx];
			return CacheIndicator<xApvaV01StateLogger>(new xApvaV01StateLogger(){ OutputRoot = outputRoot, ExportTag = exportTag }, input, ref cachexApvaV01StateLogger);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(string outputRoot, string exportTag)
		{
			return indicator.xApvaV01StateLogger(Input, outputRoot, exportTag);
		}

		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input , string outputRoot, string exportTag)
		{
			return indicator.xApvaV01StateLogger(input, outputRoot, exportTag);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(string outputRoot, string exportTag)
		{
			return indicator.xApvaV01StateLogger(Input, outputRoot, exportTag);
		}

		public Indicators.xApvaV01StateLogger xApvaV01StateLogger(ISeries<double> input , string outputRoot, string exportTag)
		{
			return indicator.xApvaV01StateLogger(input, outputRoot, exportTag);
		}
	}
}

#endregion
