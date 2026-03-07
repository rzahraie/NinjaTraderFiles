#region Using declarations
using System;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.xPva;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaSignals : Indicator
    {
        private xPvaDataset cached;
        private DateTime cachedTradingDay = Core.Globals.MinDate;
        private string cachedPath;

        private ATR atr;
        private xPvaSignalEngine engine;

        [NinjaScriptProperty]
        [Display(Name="LabelFolder", Order=1, GroupName="xPva")]
        public string LabelFolder { get; set; } = @"C:\temp\xPvaLabels";

        [NinjaScriptProperty]
        [Display(Name="MinScore", Order=2, GroupName="xPva")]
        public int MinScore { get; set; } = 60;
		
		private DateTime lastLoadedWriteUtc = Core.Globals.MinDate;
		private string lastLoadedPath = null;
		private string lastSignaledLabelId = null;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaSignals";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
                atr = ATR(14);
                engine = new xPvaSignalEngine { MinScoreToSignal = MinScore };
            }
        }

        private void EnsureDatasetLoaded()
		{
		    string master = Instrument != null ? Instrument.MasterInstrument.Name : "Unknown";
		    string barsType = BarsPeriod.BarsPeriodType.ToString();
		    int barsValue = BarsPeriod.Value;
		
		    string path = xPvaLabelFileReader.FindLatestDatasetFile(LabelFolder, master, barsType, barsValue);
			
		    if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
		    {
		        cached = null;
		        lastLoadedPath = null;
		        lastLoadedWriteUtc = Core.Globals.MinDate;
		        return;
		    }
		
			DateTime writeUtc = System.IO.File.GetLastWriteTimeUtc(path);
			
			Print($"xPvaSignals: master={master} barsType={barsType} barsValue={barsValue}");
			Print($"xPvaSignals: latestPath={(path ?? "(null)")}");
		    Print($"xPvaSignals loaded labels: {path} (writeUtc={writeUtc:O})");
			
		    // Reload if path changed or file changed
		    if (cached != null && path == lastLoadedPath && writeUtc == lastLoadedWriteUtc)
		        return;
		
		    try
		    {
		        cached = xPvaLabelFileReader.ReadDataset(path);
				Print($"xPvaSignals: labelsLoaded={(cached?.Labels?.Count ?? 0)}");
		        lastLoadedPath = path;
		        lastLoadedWriteUtc = writeUtc;
		
				Print($"xPvaSignals: folder={LabelFolder}");
		    }
		    catch (Exception ex)
		    {
		        Print("xPvaSignals load error: " + ex);
		        cached = null;
		        lastLoadedPath = null;
		        lastLoadedWriteUtc = Core.Globals.MinDate;
		    }
		}

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 50)
                return;

            EnsureDatasetLoaded();

            // no labels loaded => no signal
            if (cached == null)
                return;

            // Update engine threshold (in case changed in UI)
            engine.MinScoreToSignal = MinScore;

            // Evaluate signal
            xPvaSignal sig = engine.Evaluate(
			    CurrentBar,
			    barsAgo => Close[barsAgo],
			    barsAgo => High[barsAgo],
			    barsAgo => Low[barsAgo],
			    barsAgo => Volume[barsAgo],
			    () => atr[0],
			    dt => Bars.GetBar(dt),
			    cached);

            if (sig.Type == xPvaSignalType.None)
                return;

			// Only fire once per new label
			var latestBbt = cached.Labels
			    .Where(l => l.Type == xPvaLabelType.BBT)
			    .OrderByDescending(l => l.FinalizedAt?.TimeUtc ?? l.CreatedAt?.TimeUtc ?? DateTime.MinValue)
			    .FirstOrDefault();
			
			if (latestBbt == null || latestBbt.Id == lastSignaledLabelId)
			    return;
			
			lastSignaledLabelId = latestBbt.Id;

            // Draw
            string tagBase = $"xPvaSig_{Time[0]:yyyyMMdd_HHmmss}_{sig.Type}";
            string reason = string.Join(",", sig.Reasons);

            if (sig.Type == xPvaSignalType.LongEntry)
            {
                Draw.ArrowUp(this, tagBase + "_A", true, 0, Low[0] - (atr[0] * 0.2), Brushes.LimeGreen);
            }
            else if (sig.Type == xPvaSignalType.ShortEntry)
            {
                Draw.ArrowDown(this, tagBase + "_A", true, 0, High[0] + (atr[0] * 0.2), Brushes.OrangeRed);
            }

            Draw.Text(this, tagBase + "_T", $"score={sig.Score} {reason}", 0, Close[0]);

            Draw.HorizontalLine(this, tagBase + "_STOP", sig.StopPrice, Brushes.Gray);
            Draw.HorizontalLine(this, tagBase + "_TGT", sig.TargetPrice, Brushes.Gray);

            Alert(tagBase, Priority.Medium, $"{sig} (labels={cachedPath})", "Alert1.wav", 0, Brushes.Black, Brushes.Yellow);
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaSignals[] cachexPvaSignals;
		public xPvaSignals xPvaSignals(string labelFolder, int minScore)
		{
			return xPvaSignals(Input, labelFolder, minScore);
		}

		public xPvaSignals xPvaSignals(ISeries<double> input, string labelFolder, int minScore)
		{
			if (cachexPvaSignals != null)
				for (int idx = 0; idx < cachexPvaSignals.Length; idx++)
					if (cachexPvaSignals[idx] != null && cachexPvaSignals[idx].LabelFolder == labelFolder && cachexPvaSignals[idx].MinScore == minScore && cachexPvaSignals[idx].EqualsInput(input))
						return cachexPvaSignals[idx];
			return CacheIndicator<xPvaSignals>(new xPvaSignals(){ LabelFolder = labelFolder, MinScore = minScore }, input, ref cachexPvaSignals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaSignals xPvaSignals(string labelFolder, int minScore)
		{
			return indicator.xPvaSignals(Input, labelFolder, minScore);
		}

		public Indicators.xPvaSignals xPvaSignals(ISeries<double> input , string labelFolder, int minScore)
		{
			return indicator.xPvaSignals(input, labelFolder, minScore);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaSignals xPvaSignals(string labelFolder, int minScore)
		{
			return indicator.xPvaSignals(Input, labelFolder, minScore);
		}

		public Indicators.xPvaSignals xPvaSignals(ISeries<double> input , string labelFolder, int minScore)
		{
			return indicator.xPvaSignals(input, labelFolder, minScore);
		}
	}
}

#endregion
