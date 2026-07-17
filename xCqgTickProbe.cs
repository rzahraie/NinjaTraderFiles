#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDiagnostics
{
	public class xCqgTickProbe : Indicator
	{
		private enum TradeSide
		{
			Unknown,
			Buy,
			Sell
		}

		private sealed class PriceLevelStats
		{
			public double TotalVolume;
			public double BuyVolume;
			public double SellVolume;
			public double UnknownVolume;
		}

		private StreamWriter rawWriter;
		private StreamWriter barWriter;
		private StreamWriter levelsWriter;
		private string exportFolder;
		private string instrumentFileName;
		private string instanceStamp;
		private int rawFilePart;
		private int rawEventsInCurrentFile;
		private int eventsSinceFlush;
		private long rawEventsCaptured;
		private int activeBarIndex;
		private bool currentBarInitialized;
		private bool terminationFinalized;
		private int historicalTickBars;

		private double currentBid;
		private double currentAsk;
		private double lastPrice;
		private TradeSide lastSide;
		private TradeSide previousSide;

		private double capturedLastVolume;
		private double buyVolume;
		private double sellVolume;
		private double unknownVolume;
		private int numberOfLastEvents;
		private int numberOfBidEvents;
		private int numberOfAskEvents;
		private int numberOfUnknownLastEvents;
		private DateTime firstEventTime;
		private DateTime lastEventTime;
		private double minObservedBid;
		private double maxObservedBid;
		private double minObservedAsk;
		private double maxObservedAsk;
		private readonly Dictionary<double, PriceLevelStats> priceLevels = new Dictionary<double, PriceLevelStats>();

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xCqgTickProbe";
				Description = "Diagnostic CQG tick probe for real-time Last/Bid/Ask capture and 5-minute bar summaries.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				ExportRawEvents = true;
				ExportBarSummaries = true;
				ExportPriceLevels = true;
				UsePreviousSideForBetweenBidAsk = false;
				EnableOneTickSeries = false;
				FlushEveryNEvents = 1000;
				MaxRawEventsPerFile = 500000;
				ShowDiagnosticsPanel = true;
			}
			else if (State == State.Configure)
			{
				if (EnableOneTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				activeBarIndex = -1;
				rawFilePart = 1;
				currentBid = 0;
				currentAsk = 0;
				lastPrice = 0;
				lastSide = TradeSide.Unknown;
				previousSide = TradeSide.Unknown;
				ResetCurrentBarStats();
				EnsureExportFolder();
				OpenWriters();
			}
			else if (State == State.Historical)
			{
				Draw.TextFixed(this, "xCqgTickProbeHistorical",
					"Historical OnMarketData replay may require Tick Replay or provider support. This probe primarily validates real-time CQG events.",
					TextPosition.BottomLeft);
			}
			else if (State == State.Terminated)
			{
				if (!terminationFinalized && currentBarInitialized)
				{
					FinalizeCurrentBar();
					terminationFinalized = true;
				}
				CloseWriters();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				if (State == State.Historical)
					historicalTickBars++;
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			SyncCurrentBar();
			UpdateDiagnosticsPanel();
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;

			if (State != State.Realtime)
				return;

			SyncCurrentBar();

			if (e.MarketDataType == MarketDataType.Bid)
			{
				currentBid = e.Price;
				numberOfBidEvents++;
				ObserveBid(e.Price, e.Time);
				WriteRawEvent(e, string.Empty);
			}
			else if (e.MarketDataType == MarketDataType.Ask)
			{
				currentAsk = e.Price;
				numberOfAskEvents++;
				ObserveAsk(e.Price, e.Time);
				WriteRawEvent(e, string.Empty);
			}
			else if (e.MarketDataType == MarketDataType.Last)
			{
				TradeSide side = ClassifyLastTrade(e.Price);
				previousSide = lastSide;
				lastSide = side;
				lastPrice = e.Price;

				double volume = e.Volume;
				double price = NormalizePrice(e.Price);
				PriceLevelStats level;
				if (!priceLevels.TryGetValue(price, out level))
				{
					level = new PriceLevelStats();
					priceLevels[price] = level;
				}

				capturedLastVolume += volume;
				numberOfLastEvents++;
				level.TotalVolume += volume;

				if (side == TradeSide.Buy)
				{
					buyVolume += volume;
					level.BuyVolume += volume;
				}
				else if (side == TradeSide.Sell)
				{
					sellVolume += volume;
					level.SellVolume += volume;
				}
				else
				{
					unknownVolume += volume;
					numberOfUnknownLastEvents++;
					level.UnknownVolume += volume;
				}

				ObserveEventTime(e.Time);
				WriteRawEvent(e, side.ToString());
			}

			UpdateDiagnosticsPanel();
		}

		private void SyncCurrentBar()
		{
			if (CurrentBar < 0)
				return;

			if (!currentBarInitialized)
			{
				activeBarIndex = CurrentBar;
				currentBarInitialized = true;
				ResetCurrentBarStats();
				return;
			}

			if (CurrentBar != activeBarIndex)
			{
				FinalizeCurrentBar();
				activeBarIndex = CurrentBar;
				ResetCurrentBarStats();
			}
		}

		private TradeSide ClassifyLastTrade(double price)
		{
			if (currentAsk > 0 && price >= currentAsk)
				return TradeSide.Buy;

			if (currentBid > 0 && price <= currentBid)
				return TradeSide.Sell;

			if (UsePreviousSideForBetweenBidAsk && (lastSide == TradeSide.Buy || lastSide == TradeSide.Sell))
				return lastSide;

			return TradeSide.Unknown;
		}

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xCqgTickProbe");
			Directory.CreateDirectory(exportFolder);
			instrumentFileName = SanitizeFileName(Instrument != null ? Instrument.FullName : "UnknownInstrument");
			instanceStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
		}

		private void OpenWriters()
		{
			try
			{
				if (ExportRawEvents)
					OpenRawWriter();

				if (ExportBarSummaries)
				{
					string path = Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xCqgTickProbe_BARS_{0}_{1}.csv", instrumentFileName, instanceStamp));
					barWriter = new StreamWriter(path, false, Encoding.UTF8);
					barWriter.WriteLine("BarStartTime,BarEndTime,Open,High,Low,Close,NinjaTraderVolume,CapturedLastVolume,BuyVolume,SellVolume,UnknownVolume,Delta,NumberOfLastEvents,NumberOfBidEvents,NumberOfAskEvents,NumberOfUnknownLastEvents,PriceLevelsCount,POCPrice,MaxBuyVolumePrice,MaxSellVolumePrice,FirstEventTime,LastEventTime,MinObservedBid,MaxObservedBid,MinObservedAsk,MaxObservedAsk,CaptureCompletenessRatio");
				}

				if (ExportPriceLevels)
				{
					string path = Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xCqgTickProbe_LEVELS_{0}_{1}.csv", instrumentFileName, instanceStamp));
					levelsWriter = new StreamWriter(path, false, Encoding.UTF8);
					levelsWriter.WriteLine("BarStartTime,Price,TotalVolume,BuyVolume,SellVolume,UnknownVolume,Delta");
				}
			}
			catch (Exception ex)
			{
				Print("xCqgTickProbe failed to open export writers: " + ex.Message);
				CloseWriters();
			}
		}

		private void OpenRawWriter()
		{
			string suffix = rawFilePart <= 1 ? string.Empty : string.Format(CultureInfo.InvariantCulture, "_part{0}", rawFilePart);
			string path = Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xCqgTickProbe_RAW_{0}_{1}{2}.csv", instrumentFileName, instanceStamp, suffix));
			rawWriter = new StreamWriter(path, false, Encoding.UTF8);
			rawWriter.WriteLine("Timestamp,BarsInProgress,EventType,Price,Volume,CurrentBid,CurrentAsk,Side,PreviousSide,BidAskSpreadTicks,Instrument,ConnectionStatusOptional");
			rawEventsInCurrentFile = 0;
		}

		private void CloseWriters()
		{
			try
			{
				if (rawWriter != null)
				{
					rawWriter.Flush();
					rawWriter.Close();
					rawWriter = null;
				}

				if (barWriter != null)
				{
					barWriter.Flush();
					barWriter.Close();
					barWriter = null;
				}

				if (levelsWriter != null)
				{
					levelsWriter.Flush();
					levelsWriter.Close();
					levelsWriter = null;
				}
			}
			catch (Exception ex)
			{
				Print("xCqgTickProbe failed to close export writers: " + ex.Message);
			}
		}

		private void WriteRawEvent(MarketDataEventArgs e, string side)
		{
			if (!ExportRawEvents || rawWriter == null)
				return;

			try
			{
				double spreadTicks = TickSize > 0 && currentAsk > 0 && currentBid > 0 ? (currentAsk - currentBid) / TickSize : 0;
				string[] columns =
				{
					FormatTime(e.Time),
					BarsInProgress.ToString(CultureInfo.InvariantCulture),
					e.MarketDataType.ToString(),
					FormatDouble(e.Price),
					FormatDouble(e.Volume),
					FormatDouble(currentBid),
					FormatDouble(currentAsk),
					side,
					previousSide == TradeSide.Unknown ? string.Empty : previousSide.ToString(),
					FormatDouble(spreadTicks),
					Instrument != null ? Instrument.FullName : string.Empty,
					string.Empty
				};

				rawWriter.WriteLine(ToCsv(columns));
				rawEventsCaptured++;
				rawEventsInCurrentFile++;
				FlushPeriodically();

				if (MaxRawEventsPerFile > 0 && rawEventsInCurrentFile >= MaxRawEventsPerFile)
				{
					rawWriter.Flush();
					rawWriter.Close();
					rawFilePart++;
					OpenRawWriter();
				}
			}
			catch (Exception ex)
			{
				Print("xCqgTickProbe raw export error: " + ex.Message);
			}
		}

		private void WriteBarSummary()
		{
			if (!ExportBarSummaries || barWriter == null || activeBarIndex < 0 || CurrentBars[0] < activeBarIndex)
				return;

			try
			{
				double ntVolume = Volume[0];
				if (CurrentBar != activeBarIndex && CurrentBars[0] > activeBarIndex)
					ntVolume = Volumes[0][1];

				double ratio = ntVolume > 0 ? capturedLastVolume / ntVolume : 0;
				double pocPrice;
				double maxBuyPrice;
				double maxSellPrice;
				GetDominantPrices(out pocPrice, out maxBuyPrice, out maxSellPrice);

				string[] columns =
				{
					FormatTime(TimeForBar(activeBarIndex)),
					FormatTime(EndTimeForBar(activeBarIndex)),
					FormatDouble(OpenForBar(activeBarIndex)),
					FormatDouble(HighForBar(activeBarIndex)),
					FormatDouble(LowForBar(activeBarIndex)),
					FormatDouble(CloseForBar(activeBarIndex)),
					FormatDouble(ntVolume),
					FormatDouble(capturedLastVolume),
					FormatDouble(buyVolume),
					FormatDouble(sellVolume),
					FormatDouble(unknownVolume),
					FormatDouble(buyVolume - sellVolume),
					numberOfLastEvents.ToString(CultureInfo.InvariantCulture),
					numberOfBidEvents.ToString(CultureInfo.InvariantCulture),
					numberOfAskEvents.ToString(CultureInfo.InvariantCulture),
					numberOfUnknownLastEvents.ToString(CultureInfo.InvariantCulture),
					priceLevels.Count.ToString(CultureInfo.InvariantCulture),
					FormatOptionalPrice(pocPrice),
					FormatOptionalPrice(maxBuyPrice),
					FormatOptionalPrice(maxSellPrice),
					firstEventTime == DateTime.MinValue ? string.Empty : FormatTime(firstEventTime),
					lastEventTime == DateTime.MinValue ? string.Empty : FormatTime(lastEventTime),
					FormatOptionalPrice(minObservedBid),
					FormatOptionalPrice(maxObservedBid),
					FormatOptionalPrice(minObservedAsk),
					FormatOptionalPrice(maxObservedAsk),
					FormatDouble(ratio)
				};

				barWriter.WriteLine(ToCsv(columns));
				FlushPeriodically();
			}
			catch (Exception ex)
			{
				Print("xCqgTickProbe bar export error: " + ex.Message);
			}
		}

		private void WritePriceLevels()
		{
			if (!ExportPriceLevels || levelsWriter == null)
				return;

			try
			{
				string barStart = FormatTime(TimeForBar(activeBarIndex));
				List<double> prices = new List<double>(priceLevels.Keys);
				prices.Sort();

				foreach (double price in prices)
				{
					PriceLevelStats level = priceLevels[price];
					string[] columns =
					{
						barStart,
						FormatDouble(price),
						FormatDouble(level.TotalVolume),
						FormatDouble(level.BuyVolume),
						FormatDouble(level.SellVolume),
						FormatDouble(level.UnknownVolume),
						FormatDouble(level.BuyVolume - level.SellVolume)
					};
					levelsWriter.WriteLine(ToCsv(columns));
				}

				FlushPeriodically();
			}
			catch (Exception ex)
			{
				Print("xCqgTickProbe levels export error: " + ex.Message);
			}
		}

		private void FinalizeCurrentBar()
		{
			if (!currentBarInitialized)
				return;

			WriteBarSummary();
			WritePriceLevels();
		}

		private void ResetCurrentBarStats()
		{
			capturedLastVolume = 0;
			buyVolume = 0;
			sellVolume = 0;
			unknownVolume = 0;
			numberOfLastEvents = 0;
			numberOfBidEvents = 0;
			numberOfAskEvents = 0;
			numberOfUnknownLastEvents = 0;
			firstEventTime = DateTime.MinValue;
			lastEventTime = DateTime.MinValue;
			minObservedBid = double.MaxValue;
			maxObservedBid = double.MinValue;
			minObservedAsk = double.MaxValue;
			maxObservedAsk = double.MinValue;
			priceLevels.Clear();
		}

		private double NormalizePrice(double price)
		{
			if (TickSize <= 0)
				return price;

			return Math.Round(price / TickSize, MidpointRounding.AwayFromZero) * TickSize;
		}

		private string SanitizeFileName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "UnknownInstrument";

			char[] invalid = Path.GetInvalidFileNameChars();
			StringBuilder builder = new StringBuilder(value.Length);
			foreach (char ch in value)
			{
				if (Array.IndexOf(invalid, ch) >= 0 || char.IsWhiteSpace(ch) || ch == '/' || ch == '\\' || ch == ':' || ch == '|')
					builder.Append('_');
				else
					builder.Append(ch);
			}
			return builder.ToString();
		}

		private void ObserveBid(double price, DateTime time)
		{
			if (price <= 0)
				return;

			if (price < minObservedBid)
				minObservedBid = price;
			if (price > maxObservedBid)
				maxObservedBid = price;
			ObserveEventTime(time);
		}

		private void ObserveAsk(double price, DateTime time)
		{
			if (price <= 0)
				return;

			if (price < minObservedAsk)
				minObservedAsk = price;
			if (price > maxObservedAsk)
				maxObservedAsk = price;
			ObserveEventTime(time);
		}

		private void ObserveEventTime(DateTime time)
		{
			if (time == DateTime.MinValue)
				time = DateTime.Now;

			if (firstEventTime == DateTime.MinValue)
				firstEventTime = time;
			lastEventTime = time;
		}

		private void GetDominantPrices(out double pocPrice, out double maxBuyPrice, out double maxSellPrice)
		{
			pocPrice = double.NaN;
			maxBuyPrice = double.NaN;
			maxSellPrice = double.NaN;
			double maxTotal = double.MinValue;
			double maxBuy = double.MinValue;
			double maxSell = double.MinValue;

			foreach (KeyValuePair<double, PriceLevelStats> pair in priceLevels)
			{
				if (pair.Value.TotalVolume > maxTotal)
				{
					maxTotal = pair.Value.TotalVolume;
					pocPrice = pair.Key;
				}
				if (pair.Value.BuyVolume > maxBuy)
				{
					maxBuy = pair.Value.BuyVolume;
					maxBuyPrice = pair.Key;
				}
				if (pair.Value.SellVolume > maxSell)
				{
					maxSell = pair.Value.SellVolume;
					maxSellPrice = pair.Key;
				}
			}
		}

		private DateTime TimeForBar(int barIndex)
		{
			int barsAgo = CurrentBar - barIndex;
			if (barsAgo >= 0 && barsAgo <= CurrentBar)
				return Times[0][barsAgo];
			return DateTime.MinValue;
		}

		private DateTime EndTimeForBar(int barIndex)
		{
			DateTime start = TimeForBar(barIndex);
			if (start == DateTime.MinValue)
				return DateTime.MinValue;

			if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value > 0)
				return start.AddMinutes(BarsPeriod.Value);

			return start;
		}

		private double OpenForBar(int barIndex)
		{
			int barsAgo = CurrentBar - barIndex;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Opens[0][barsAgo] : 0;
		}

		private double HighForBar(int barIndex)
		{
			int barsAgo = CurrentBar - barIndex;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Highs[0][barsAgo] : 0;
		}

		private double LowForBar(int barIndex)
		{
			int barsAgo = CurrentBar - barIndex;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Lows[0][barsAgo] : 0;
		}

		private double CloseForBar(int barIndex)
		{
			int barsAgo = CurrentBar - barIndex;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Closes[0][barsAgo] : 0;
		}

		private void FlushPeriodically()
		{
			eventsSinceFlush++;
			if (FlushEveryNEvents <= 1 || eventsSinceFlush >= FlushEveryNEvents)
			{
				if (rawWriter != null)
					rawWriter.Flush();
				if (barWriter != null)
					barWriter.Flush();
				if (levelsWriter != null)
					levelsWriter.Flush();
				eventsSinceFlush = 0;
			}
		}

		private string FormatTime(DateTime time)
		{
			return time == DateTime.MinValue ? string.Empty : time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		}

		private string FormatDouble(double value)
		{
			return value.ToString("0.########", CultureInfo.InvariantCulture);
		}

		private string FormatOptionalPrice(double value)
		{
			if (double.IsNaN(value) || value == double.MaxValue || value == double.MinValue)
				return string.Empty;
			return FormatDouble(value);
		}

		private string ToCsv(IEnumerable<string> columns)
		{
			StringBuilder builder = new StringBuilder();
			bool first = true;
			foreach (string column in columns)
			{
				if (!first)
					builder.Append(',');
				first = false;
				builder.Append(EscapeCsv(column));
			}
			return builder.ToString();
		}

		private string EscapeCsv(string value)
		{
			if (value == null)
				return string.Empty;

			if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
				return value;

			return "\"" + value.Replace("\"", "\"\"") + "\"";
		}

		private void UpdateDiagnosticsPanel()
		{
			if (!ShowDiagnosticsPanel)
				return;

			double ntVolume = CurrentBar >= 0 ? Volume[0] : 0;
			double ratio = ntVolume > 0 ? capturedLastVolume / ntVolume : 0;
			string text = string.Format(CultureInfo.InvariantCulture,
				"xCqgTickProbe\nBid: {0}\nAsk: {1}\nLast: {2} {3}\nRaw events: {4}\nBar Last Vol: {5}\nBar NT Vol: {6}\nBuy: {7}  Sell: {8}  Unknown: {9}\nDelta: {10}\nCapture Ratio: {11:0.###}\nHistorical 1-tick bars: {12}\nExport: {13}",
				FormatOptionalPrice(currentBid),
				FormatOptionalPrice(currentAsk),
				FormatOptionalPrice(lastPrice),
				lastSide,
				rawEventsCaptured,
				FormatDouble(capturedLastVolume),
				FormatDouble(ntVolume),
				FormatDouble(buyVolume),
				FormatDouble(sellVolume),
				FormatDouble(unknownVolume),
				FormatDouble(buyVolume - sellVolume),
				ratio,
				historicalTickBars,
				exportFolder);

			Draw.TextFixed(this, "xCqgTickProbeDiagnostics", text, TextPosition.TopLeft);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Export raw events", GroupName = "Export", Order = 1)]
		public bool ExportRawEvents { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export bar summaries", GroupName = "Export", Order = 2)]
		public bool ExportBarSummaries { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export price levels", GroupName = "Export", Order = 3)]
		public bool ExportPriceLevels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use previous side for between bid/ask", GroupName = "Classification", Order = 4)]
		public bool UsePreviousSideForBetweenBidAsk { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable one tick series", GroupName = "Historical", Order = 5)]
		public bool EnableOneTickSeries { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Flush every N events", GroupName = "Export", Order = 6)]
		public int FlushEveryNEvents { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max raw events per file", GroupName = "Export", Order = 7)]
		public int MaxRawEventsPerFile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 8)]
		public bool ShowDiagnosticsPanel { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public string ExportFolder
		{
			get { return exportFolder; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaDiagnostics.xCqgTickProbe[] cachexCqgTickProbe;
		public xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			return xCqgTickProbe(Input, exportRawEvents, exportBarSummaries, exportPriceLevels, usePreviousSideForBetweenBidAsk, enableOneTickSeries, flushEveryNEvents, maxRawEventsPerFile, showDiagnosticsPanel);
		}

		public xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(ISeries<double> input, bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			if (cachexCqgTickProbe != null)
				for (int idx = 0; idx < cachexCqgTickProbe.Length; idx++)
					if (cachexCqgTickProbe[idx] != null && cachexCqgTickProbe[idx].ExportRawEvents == exportRawEvents && cachexCqgTickProbe[idx].ExportBarSummaries == exportBarSummaries && cachexCqgTickProbe[idx].ExportPriceLevels == exportPriceLevels && cachexCqgTickProbe[idx].UsePreviousSideForBetweenBidAsk == usePreviousSideForBetweenBidAsk && cachexCqgTickProbe[idx].EnableOneTickSeries == enableOneTickSeries && cachexCqgTickProbe[idx].FlushEveryNEvents == flushEveryNEvents && cachexCqgTickProbe[idx].MaxRawEventsPerFile == maxRawEventsPerFile && cachexCqgTickProbe[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexCqgTickProbe[idx].EqualsInput(input))
						return cachexCqgTickProbe[idx];
			return CacheIndicator<xPvaDiagnostics.xCqgTickProbe>(new xPvaDiagnostics.xCqgTickProbe(){ ExportRawEvents = exportRawEvents, ExportBarSummaries = exportBarSummaries, ExportPriceLevels = exportPriceLevels, UsePreviousSideForBetweenBidAsk = usePreviousSideForBetweenBidAsk, EnableOneTickSeries = enableOneTickSeries, FlushEveryNEvents = flushEveryNEvents, MaxRawEventsPerFile = maxRawEventsPerFile, ShowDiagnosticsPanel = showDiagnosticsPanel }, input, ref cachexCqgTickProbe);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			return indicator.xCqgTickProbe(Input, exportRawEvents, exportBarSummaries, exportPriceLevels, usePreviousSideForBetweenBidAsk, enableOneTickSeries, flushEveryNEvents, maxRawEventsPerFile, showDiagnosticsPanel);
		}

		public Indicators.xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(ISeries<double> input , bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			return indicator.xCqgTickProbe(input, exportRawEvents, exportBarSummaries, exportPriceLevels, usePreviousSideForBetweenBidAsk, enableOneTickSeries, flushEveryNEvents, maxRawEventsPerFile, showDiagnosticsPanel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			return indicator.xCqgTickProbe(Input, exportRawEvents, exportBarSummaries, exportPriceLevels, usePreviousSideForBetweenBidAsk, enableOneTickSeries, flushEveryNEvents, maxRawEventsPerFile, showDiagnosticsPanel);
		}

		public Indicators.xPvaDiagnostics.xCqgTickProbe xCqgTickProbe(ISeries<double> input , bool exportRawEvents, bool exportBarSummaries, bool exportPriceLevels, bool usePreviousSideForBetweenBidAsk, bool enableOneTickSeries, int flushEveryNEvents, int maxRawEventsPerFile, bool showDiagnosticsPanel)
		{
			return indicator.xCqgTickProbe(input, exportRawEvents, exportBarSummaries, exportPriceLevels, usePreviousSideForBetweenBidAsk, enableOneTickSeries, flushEveryNEvents, maxRawEventsPerFile, showDiagnosticsPanel);
		}
	}
}

#endregion
