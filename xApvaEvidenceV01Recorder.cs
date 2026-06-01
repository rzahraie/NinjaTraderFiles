#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using NinjaTrader.NinjaScript.APVA.V01;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    // Recorder-only indicator for APVA Evidence v0.1 research exports.
    public class xApvaEvidenceV01Recorder : Indicator
    {
        private ApvaEvidenceV01FeatureEngine featureEngine;
        private ApvaEvidenceV01Analyzer analyzer;
        private ApvaEvidenceV01Logger logger;
        private DateTime? firstProcessedBarTime;
        private DateTime? lastProcessedBarTime;
        private string openFilePath;

        [NinjaScriptProperty]
        [Display(Name = "EnableExport", Order = 100, GroupName = "Export")]
        public bool EnableExport { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "OutputFolder", Order = 101, GroupName = "Export")]
        public string OutputFolder { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ExportTag", Order = 102, GroupName = "Export")]
        public string ExportTag { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PrintSummaryOnTerminate", Order = 103, GroupName = "Export")]
        public bool PrintSummaryOnTerminate { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xApvaEvidenceV01Recorder";
                Description = "APVA Evidence v0.1 recorder. No trade signals.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                EnableExport = true;
                OutputFolder = @"C:\Users\rz0\Documents\ApvaAnalysis\Evidence";
                ExportTag = "apva_bar_evidence_v01";
                PrintSummaryOnTerminate = true;
            }
            else if (State == State.DataLoaded)
            {
                featureEngine = new ApvaEvidenceV01FeatureEngine();
                analyzer = new ApvaEvidenceV01Analyzer();
            }
            else if (State == State.Terminated)
            {
                CloseLoggerAndFinalize();

                if (PrintSummaryOnTerminate && analyzer != null)
                    Print(analyzer.BuildSummary().ToDiagnosticString());
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || featureEngine == null || analyzer == null)
                return;

            if (!firstProcessedBarTime.HasValue)
                firstProcessedBarTime = Time[0];

            lastProcessedBarTime = Time[0];

            if (EnableExport && logger == null)
                logger = CreateLogger("OPEN");

            List<double> recentVolumes = BuildRecentVolumes();
            List<double> recentRanges = BuildRecentRanges();
            List<double> recentBodies = BuildRecentBodies();

            ApvaEvidenceRow row = featureEngine.Build(
                CurrentBar,
                Time[0],
                Open[0],
                High[0],
                Low[0],
                Close[0],
                Volume[0],
                Open[1],
                High[1],
                Low[1],
                Close[1],
                Volume[1],
                recentVolumes,
                recentRanges,
                recentBodies);

            analyzer.Add(row);

            if (EnableExport && logger != null)
                logger.Append(row);
        }

        private ApvaEvidenceV01Logger CreateLogger(string endDateToken)
        {
            if (string.IsNullOrWhiteSpace(OutputFolder))
                throw new InvalidOperationException(
                    "OutputFolder is required when export is enabled.");

            EnsureOutsideNinjaTraderSourceFolder(OutputFolder);
            Directory.CreateDirectory(OutputFolder);

            string masterInstrumentName =
                Instrument != null && Instrument.MasterInstrument != null
                    ? Instrument.MasterInstrument.Name
                    : "UnknownInstrument";
            string instrumentFullName =
                Instrument != null && !string.IsNullOrWhiteSpace(Instrument.FullName)
                    ? Instrument.FullName
                    : masterInstrumentName;
            string barsPeriod = BarsPeriod != null
                ? BarsPeriod.ToString()
                : "UnknownBarsPeriod";
            string tradingHours =
                Bars != null &&
                Bars.TradingHours != null &&
                !string.IsNullOrWhiteSpace(Bars.TradingHours.Name)
                    ? Bars.TradingHours.Name
                    : "UnknownTradingHours";
            string exportTag = string.IsNullOrWhiteSpace(ExportTag)
                ? "apva_bar_evidence_v01"
                : ExportTag.Trim();
            string startDate = firstProcessedBarTime.HasValue
                ? firstProcessedBarTime.Value.ToString("yyyyMMdd")
                : "UnknownStartDate";
            string instrumentFolder = Path.Combine(
                OutputFolder,
                MakeSafeFileName(masterInstrumentName));

            EnsureOutsideNinjaTraderSourceFolder(instrumentFolder);
            Directory.CreateDirectory(instrumentFolder);

            string fileName =
                MakeSafeFileName(instrumentFullName) + "_" +
                MakeSafeFileName(barsPeriod) + "_" +
                MakeSafeFileName(tradingHours) + "_" +
                startDate + "_" +
                MakeSafeFileName(endDateToken) + "_" +
                MakeSafeFileName(exportTag) + ".csv";
            string filePath = Path.Combine(instrumentFolder, fileName);

            openFilePath = filePath;
            return new ApvaEvidenceV01Logger(filePath);
        }

        private void CloseLoggerAndFinalize()
        {
            if (logger == null)
                return;

            try
            {
                logger.Flush();
            }
            catch (Exception ex)
            {
                Print("xApvaEvidenceV01Recorder: Flush failed: " + ex.Message);
            }

            try
            {
                logger.Dispose();
            }
            catch (Exception ex)
            {
                Print("xApvaEvidenceV01Recorder: Dispose failed: " + ex.Message);
            }
            finally
            {
                logger = null;
            }

            try
            {
                FinalizeOpenExportFile();
            }
            catch (Exception ex)
            {
                Print("xApvaEvidenceV01Recorder: Final rename failed: " + ex.Message);
            }
        }

        private void FinalizeOpenExportFile()
        {
            if (string.IsNullOrWhiteSpace(openFilePath) ||
                !firstProcessedBarTime.HasValue ||
                !lastProcessedBarTime.HasValue ||
                !File.Exists(openFilePath))
            {
                return;
            }

            string finalFilePath = CreateFinalFilePath();

            if (File.Exists(finalFilePath))
                finalFilePath = AddTimestampSuffix(finalFilePath);

            File.Move(openFilePath, finalFilePath);
            openFilePath = null;
        }

        private static string AddTimestampSuffix(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string candidate = Path.Combine(
                directory,
                fileName + "_" + timestamp + extension);
            int suffix = 1;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(
                    directory,
                    fileName + "_" + timestamp + "_" + suffix + extension);
                suffix++;
            }

            return candidate;
        }

        private string CreateFinalFilePath()
        {
            string masterInstrumentName =
                Instrument != null && Instrument.MasterInstrument != null
                    ? Instrument.MasterInstrument.Name
                    : "UnknownInstrument";
            string instrumentFullName =
                Instrument != null && !string.IsNullOrWhiteSpace(Instrument.FullName)
                    ? Instrument.FullName
                    : masterInstrumentName;
            string barsPeriod = BarsPeriod != null
                ? BarsPeriod.ToString()
                : "UnknownBarsPeriod";
            string tradingHours =
                Bars != null &&
                Bars.TradingHours != null &&
                !string.IsNullOrWhiteSpace(Bars.TradingHours.Name)
                    ? Bars.TradingHours.Name
                    : "UnknownTradingHours";
            string exportTag = string.IsNullOrWhiteSpace(ExportTag)
                ? "apva_bar_evidence_v01"
                : ExportTag.Trim();
            string instrumentFolder = Path.Combine(
                OutputFolder,
                MakeSafeFileName(masterInstrumentName));

            EnsureOutsideNinjaTraderSourceFolder(instrumentFolder);

            string fileName =
                MakeSafeFileName(instrumentFullName) + "_" +
                MakeSafeFileName(barsPeriod) + "_" +
                MakeSafeFileName(tradingHours) + "_" +
                firstProcessedBarTime.Value.ToString("yyyyMMdd") + "_" +
                lastProcessedBarTime.Value.ToString("yyyyMMdd") + "_" +
                MakeSafeFileName(exportTag) + ".csv";

            return Path.Combine(instrumentFolder, fileName);
        }

        private static void EnsureOutsideNinjaTraderSourceFolder(
            string outputFolder)
        {
            string fullOutputFolder = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string customFolder = Path.GetFullPath(Path.Combine(
                    NinjaTrader.Core.Globals.UserDataDir,
                    "bin",
                    "Custom"))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (fullOutputFolder.Equals(
                    customFolder,
                    StringComparison.OrdinalIgnoreCase) ||
                fullOutputFolder.StartsWith(
                    customFolder + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "OutputFolder must be outside NinjaTrader source folders.");
            }
        }

        private List<double> BuildRecentVolumes()
        {
            int count = Math.Min(CurrentBar + 1, 50);
            var values = new List<double>(count);

            for (int barsAgo = count - 1; barsAgo >= 0; barsAgo--)
                values.Add(Volume[barsAgo]);

            return values;
        }

        private List<double> BuildRecentRanges()
        {
            int count = Math.Min(CurrentBar + 1, 20);
            var values = new List<double>(count);

            for (int barsAgo = count - 1; barsAgo >= 0; barsAgo--)
                values.Add(High[barsAgo] - Low[barsAgo]);

            return values;
        }

        private List<double> BuildRecentBodies()
        {
            int count = Math.Min(CurrentBar + 1, 20);
            var values = new List<double>(count);

            for (int barsAgo = count - 1; barsAgo >= 0; barsAgo--)
                values.Add(Math.Abs(Close[barsAgo] - Open[barsAgo]));

            return values;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidCharacter, '_');

            return value.Replace(' ', '_');
        }
    }
}



#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xApvaEvidenceV01Recorder[] cachexApvaEvidenceV01Recorder;
		public xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			return xApvaEvidenceV01Recorder(Input, enableExport, outputFolder, exportTag, printSummaryOnTerminate);
		}

		public xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(ISeries<double> input, bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			if (cachexApvaEvidenceV01Recorder != null)
				for (int idx = 0; idx < cachexApvaEvidenceV01Recorder.Length; idx++)
					if (cachexApvaEvidenceV01Recorder[idx] != null && cachexApvaEvidenceV01Recorder[idx].EnableExport == enableExport && cachexApvaEvidenceV01Recorder[idx].OutputFolder == outputFolder && cachexApvaEvidenceV01Recorder[idx].ExportTag == exportTag && cachexApvaEvidenceV01Recorder[idx].PrintSummaryOnTerminate == printSummaryOnTerminate && cachexApvaEvidenceV01Recorder[idx].EqualsInput(input))
						return cachexApvaEvidenceV01Recorder[idx];
			return CacheIndicator<xApvaEvidenceV01Recorder>(new xApvaEvidenceV01Recorder(){ EnableExport = enableExport, OutputFolder = outputFolder, ExportTag = exportTag, PrintSummaryOnTerminate = printSummaryOnTerminate }, input, ref cachexApvaEvidenceV01Recorder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			return indicator.xApvaEvidenceV01Recorder(Input, enableExport, outputFolder, exportTag, printSummaryOnTerminate);
		}

		public Indicators.xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(ISeries<double> input , bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			return indicator.xApvaEvidenceV01Recorder(input, enableExport, outputFolder, exportTag, printSummaryOnTerminate);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			return indicator.xApvaEvidenceV01Recorder(Input, enableExport, outputFolder, exportTag, printSummaryOnTerminate);
		}

		public Indicators.xApvaEvidenceV01Recorder xApvaEvidenceV01Recorder(ISeries<double> input , bool enableExport, string outputFolder, string exportTag, bool printSummaryOnTerminate)
		{
			return indicator.xApvaEvidenceV01Recorder(input, enableExport, outputFolder, exportTag, printSummaryOnTerminate);
		}
	}
}

#endregion
