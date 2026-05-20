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
        private string outputPath;
        private bool headerWritten;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xApvaV01StateLogger";
                Description = "APVA v0.1 state logger. No trade signals.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
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
                outputPath = Path.Combine(
                    NinjaTrader.Core.Globals.UserDataDir,
                    "xApvaV01StateLog.csv");

                headerWritten = false;

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

        private void WriteSnapshot(ApvaStateSnapshot snapshot)
        {
            try
            {
                if (!headerWritten)
                {
                    File.AppendAllText(outputPath, ApvaV01SnapshotFormatter.CsvHeader() + Environment.NewLine);
                    headerWritten = true;
                }

                File.AppendAllText(outputPath, ApvaV01SnapshotFormatter.ToCsv(snapshot) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Print("xApvaV01StateLogger: Write failed: " + ex.Message);
            }
        }
    }
}