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
                IsOverlay = false;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                EnableExport = true;
                OutputFolder = @"D:\APVA\Exports";
                ExportTag = "apva_bar_evidence_v01";
                PrintSummaryOnTerminate = true;
            }
            else if (State == State.DataLoaded)
            {
                featureEngine = new ApvaEvidenceV01FeatureEngine();
                analyzer = new ApvaEvidenceV01Analyzer();

                if (EnableExport)
                    logger = CreateLogger();
            }
            else if (State == State.Terminated)
            {
                if (logger != null)
                {
                    logger.Flush();
                    logger.Dispose();
                    logger = null;
                }

                if (PrintSummaryOnTerminate && analyzer != null)
                    Print(analyzer.BuildSummary().ToDiagnosticString());
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || featureEngine == null || analyzer == null)
                return;

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

        private ApvaEvidenceV01Logger CreateLogger()
        {
            if (string.IsNullOrWhiteSpace(OutputFolder))
                throw new InvalidOperationException(
                    "OutputFolder is required when export is enabled.");

            EnsureOutsideNinjaTraderSourceFolder(OutputFolder);
            Directory.CreateDirectory(OutputFolder);

            string instrumentName =
                Instrument != null && Instrument.MasterInstrument != null
                    ? Instrument.MasterInstrument.Name
                    : "UnknownInstrument";
            string barsPeriod = BarsPeriod != null
                ? BarsPeriod.ToString()
                : "UnknownBarsPeriod";
            string exportTag = string.IsNullOrWhiteSpace(ExportTag)
                ? "apva_bar_evidence_v01"
                : ExportTag.Trim();

            string fileName =
                MakeSafeFileName(instrumentName) + "_" +
                MakeSafeFileName(barsPeriod) + "_" +
                MakeSafeFileName(exportTag) + ".csv";
            string filePath = Path.Combine(OutputFolder, fileName);

            return new ApvaEvidenceV01Logger(filePath);
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

            return value;
        }
    }
}

