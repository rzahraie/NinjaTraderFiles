#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow
{
	public enum xTradeSide
	{
		Unknown,
		Buy,
		Sell
	}

	public enum xTradePriceRelation
	{
		UnknownNoQuote,
		AtAsk,
		AboveAsk,
		AtBid,
		BelowBid,
		BetweenBidAsk,
		LockedMarket,
		CrossedMarket
	}

	public enum xPendingEventDisposition
	{
		Assigned,
		WaitingForBar,
		NoMatchingPrimaryBar,
		DroppedDueToCapacity
	}

	public enum xTimestampRelationToCurrentBar
	{
		BeforeOpen,
		Inside,
		AtClose,
		AfterClose,
		NoCurrentBar
	}

	public sealed class xPriceLevelOrderFlow
	{
		public long PriceTicks { get; set; }
		public double Price { get; set; }
		public long TotalVolume { get; set; }
		public long BuyVolume { get; set; }
		public long SellVolume { get; set; }
		public long UnknownVolume { get; set; }
		public long Delta { get { return BuyVolume - SellVolume; } }
		public int TradeCount { get; set; }
		public int BuyTradeCount { get; set; }
		public int SellTradeCount { get; set; }
		public int UnknownTradeCount { get; set; }

		public xPriceLevelOrderFlow Clone()
		{
			return new xPriceLevelOrderFlow
			{
				PriceTicks = PriceTicks,
				Price = Price,
				TotalVolume = TotalVolume,
				BuyVolume = BuyVolume,
				SellVolume = SellVolume,
				UnknownVolume = UnknownVolume,
				TradeCount = TradeCount,
				BuyTradeCount = BuyTradeCount,
				SellTradeCount = SellTradeCount,
				UnknownTradeCount = UnknownTradeCount
			};
		}
	}

	public sealed class xOrderFlowBar
	{
		public int BarIndex { get; set; }
		public DateTime NinjaBarTime { get; set; }
		public DateTime BarOpenTime { get; set; }
		public DateTime BarCloseTime { get; set; }
		public double Open { get; set; }
		public double High { get; set; }
		public double Low { get; set; }
		public double Close { get; set; }
		public long NinjaTraderVolume { get; set; }
		public long CapturedVolume { get; set; }
		public long BuyVolume { get; set; }
		public long SellVolume { get; set; }
		public long UnknownVolume { get; set; }
		public long Delta { get { return BuyVolume - SellVolume; } }
		public long AbsoluteDelta { get { return Math.Abs(Delta); } }
		public int LastEventCount { get; set; }
		public int BidEventCount { get; set; }
		public int AskEventCount { get; set; }
		public int AtAskCount { get; set; }
		public int AboveAskCount { get; set; }
		public int AtBidCount { get; set; }
		public int BelowBidCount { get; set; }
		public int BetweenBidAskCount { get; set; }
		public int UnknownNoQuoteCount { get; set; }
		public int LockedMarketCount { get; set; }
		public int CrossedMarketCount { get; set; }
		public long AtAskVolume { get; set; }
		public long AboveAskVolume { get; set; }
		public long AtBidVolume { get; set; }
		public long BelowBidVolume { get; set; }
		public long BetweenBidAskVolume { get; set; }
		public long UnknownNoQuoteVolume { get; set; }
		public double PocPrice { get; set; }
		public long PocVolume { get; set; }
		public double MaxBuyVolumePrice { get; set; }
		public long MaxBuyVolume { get; set; }
		public double MaxSellVolumePrice { get; set; }
		public long MaxSellVolume { get; set; }
		public double MaxPositiveDeltaPrice { get; set; }
		public long MaxPositiveDelta { get; set; }
		public double MaxNegativeDeltaPrice { get; set; }
		public long MaxNegativeDelta { get; set; }
		public double CaptureCompletenessRatio { get; set; }
		public DateTime FirstMarketDataEventTime { get; set; }
		public DateTime LastMarketDataEventTime { get; set; }
		public bool IsPartialBar { get; set; }
		public bool IsRealtimeCaptured { get; set; }
		public long RunningDeltaOpen { get; set; }
		public long RunningDeltaHigh { get; set; }
		public long RunningDeltaLow { get; set; }
		public long RunningDeltaClose { get; set; }
		public long VolumeDifference { get; set; }
		public double VolumeDifferencePercent { get; set; }
		public bool CaptureCompletenessPassed { get; set; }
		public bool HadPendingEvents { get; set; }
		public int PendingEventsAssigned { get; set; }
		public int UnmatchedEventsObserved { get; set; }
		public bool ValidationPassed { get; set; }
		public string ValidationMessage { get; set; }
		public IReadOnlyDictionary<long, xPriceLevelOrderFlow> PriceLevels { get; set; }
	}

	public sealed class xLiveOrderFlowSnapshot
	{
		public int BarIndex { get; set; }
		public DateTime EventTime { get; set; }
		public long CapturedVolume { get; set; }
		public long BuyVolume { get; set; }
		public long SellVolume { get; set; }
		public long UnknownVolume { get; set; }
		public long RunningDeltaOpen { get; set; }
		public long RunningDeltaHigh { get; set; }
		public long RunningDeltaLow { get; set; }
		public long RunningDeltaClose { get; set; }
		public bool IsPartialBar { get; set; }
		public int PendingEventsCount { get; set; }
		public DateTime CurrentBarOpenTime { get; set; }
		public DateTime CurrentBarCloseTime { get; set; }
		public DateTime LastAssignedEventTime { get; set; }
		public int UnmatchedEventsObserved { get; set; }
		public bool CaptureIntegrityCompromised { get; set; }
	}

	internal sealed class xPendingMarketDataEvent
	{
		public long ArrivalSequence { get; set; }
		public DateTime Timestamp { get; set; }
		public MarketDataType EventType { get; set; }
		public double Price { get; set; }
		public long Volume { get; set; }
		public double BidAtEvent { get; set; }
		public double AskAtEvent { get; set; }
		public bool HasBidAtEvent { get; set; }
		public bool HasAskAtEvent { get; set; }
		public xTradeSide ClassifiedSide { get; set; }
		public xTradePriceRelation PriceRelation { get; set; }
	}

	internal sealed class xMutableOrderFlowBar
	{
		private readonly double tickSize;
		private readonly Dictionary<long, xPriceLevelOrderFlow> priceLevels;

		public int BarIndex;
		public DateTime NinjaBarTime;
		public DateTime BarOpenTime;
		public DateTime BarCloseTime;
		public bool IsPartialBar;
		public bool IsRealtimeCaptured;
		public long CapturedVolume;
		public long BuyVolume;
		public long SellVolume;
		public long UnknownVolume;
		public int LastEventCount;
		public int BidEventCount;
		public int AskEventCount;
		public int AtAskCount;
		public int AboveAskCount;
		public int AtBidCount;
		public int BelowBidCount;
		public int BetweenBidAskCount;
		public int UnknownNoQuoteCount;
		public int LockedMarketCount;
		public int CrossedMarketCount;
		public long AtAskVolume;
		public long AboveAskVolume;
		public long AtBidVolume;
		public long BelowBidVolume;
		public long BetweenBidAskVolume;
		public long UnknownNoQuoteVolume;
		public DateTime FirstMarketDataEventTime;
		public DateTime LastMarketDataEventTime;
		public long RunningDeltaOpen;
		public long RunningDeltaHigh;
		public long RunningDeltaLow;
		public long RunningDeltaClose;
		public bool HadPendingEvents;
		public int PendingEventsAssigned;
		public int UnmatchedEventsObserved;

		public Dictionary<long, xPriceLevelOrderFlow> PriceLevels { get { return priceLevels; } }

		public xMutableOrderFlowBar(double tickSize)
		{
			this.tickSize = tickSize;
			priceLevels = new Dictionary<long, xPriceLevelOrderFlow>();
		}

		public void Reset(int barIndex, DateTime ninjaBarTime, DateTime openTime, DateTime closeTime, bool isPartial, bool isRealtimeCaptured)
		{
			BarIndex = barIndex;
			NinjaBarTime = ninjaBarTime;
			BarOpenTime = openTime;
			BarCloseTime = closeTime;
			IsPartialBar = isPartial;
			IsRealtimeCaptured = isRealtimeCaptured;
			CapturedVolume = 0;
			BuyVolume = 0;
			SellVolume = 0;
			UnknownVolume = 0;
			LastEventCount = 0;
			BidEventCount = 0;
			AskEventCount = 0;
			AtAskCount = 0;
			AboveAskCount = 0;
			AtBidCount = 0;
			BelowBidCount = 0;
			BetweenBidAskCount = 0;
			UnknownNoQuoteCount = 0;
			LockedMarketCount = 0;
			CrossedMarketCount = 0;
			AtAskVolume = 0;
			AboveAskVolume = 0;
			AtBidVolume = 0;
			BelowBidVolume = 0;
			BetweenBidAskVolume = 0;
			UnknownNoQuoteVolume = 0;
			FirstMarketDataEventTime = DateTime.MinValue;
			LastMarketDataEventTime = DateTime.MinValue;
			RunningDeltaOpen = 0;
			RunningDeltaHigh = 0;
			RunningDeltaLow = 0;
			RunningDeltaClose = 0;
			HadPendingEvents = false;
			PendingEventsAssigned = 0;
			UnmatchedEventsObserved = 0;
			priceLevels.Clear();
		}

		public void ProcessBid(DateTime timestamp)
		{
			BidEventCount++;
			ObserveEventTime(timestamp);
		}

		public void ProcessAsk(DateTime timestamp)
		{
			AskEventCount++;
			ObserveEventTime(timestamp);
		}

		public void ProcessLast(double price, long priceTicks, long volume, xTradeSide side, xTradePriceRelation relation, DateTime timestamp)
		{
			LastEventCount++;
			CapturedVolume += volume;
			ObserveEventTime(timestamp);
			CountRelation(relation, volume);

			xPriceLevelOrderFlow level;
			if (!priceLevels.TryGetValue(priceTicks, out level))
			{
				level = new xPriceLevelOrderFlow { PriceTicks = priceTicks, Price = priceTicks * tickSize };
				priceLevels[priceTicks] = level;
			}

			level.TotalVolume += volume;
			level.TradeCount++;

			if (side == xTradeSide.Buy)
			{
				BuyVolume += volume;
				level.BuyVolume += volume;
				level.BuyTradeCount++;
				UpdateRunningDelta(volume);
			}
			else if (side == xTradeSide.Sell)
			{
				SellVolume += volume;
				level.SellVolume += volume;
				level.SellTradeCount++;
				UpdateRunningDelta(-volume);
			}
			else
			{
				UnknownVolume += volume;
				level.UnknownVolume += volume;
				level.UnknownTradeCount++;
			}
		}

		public xLiveOrderFlowSnapshot CreateLiveSnapshot(DateTime eventTime, int pendingEventsCount, int unmatchedEventsObserved, bool captureIntegrityCompromised)
		{
			return new xLiveOrderFlowSnapshot
			{
				BarIndex = BarIndex,
				EventTime = eventTime,
				CapturedVolume = CapturedVolume,
				BuyVolume = BuyVolume,
				SellVolume = SellVolume,
				UnknownVolume = UnknownVolume,
				RunningDeltaOpen = RunningDeltaOpen,
				RunningDeltaHigh = RunningDeltaHigh,
				RunningDeltaLow = RunningDeltaLow,
				RunningDeltaClose = RunningDeltaClose,
				IsPartialBar = IsPartialBar,
				PendingEventsCount = pendingEventsCount,
				CurrentBarOpenTime = BarOpenTime,
				CurrentBarCloseTime = BarCloseTime,
				LastAssignedEventTime = LastMarketDataEventTime,
				UnmatchedEventsObserved = unmatchedEventsObserved,
				CaptureIntegrityCompromised = captureIntegrityCompromised
			};
		}

		private void UpdateRunningDelta(long signedVolume)
		{
			RunningDeltaClose += signedVolume;
			if (RunningDeltaClose > RunningDeltaHigh)
				RunningDeltaHigh = RunningDeltaClose;
			if (RunningDeltaClose < RunningDeltaLow)
				RunningDeltaLow = RunningDeltaClose;
		}

		public xOrderFlowBar FinalizeBar(double open, double high, double low, double close, long ninjaTraderVolume, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent)
		{
			xOrderFlowBar bar = new xOrderFlowBar();
			bar.BarIndex = BarIndex;
			bar.NinjaBarTime = NinjaBarTime;
			bar.BarOpenTime = BarOpenTime;
			bar.BarCloseTime = BarCloseTime;
			bar.Open = open;
			bar.High = high;
			bar.Low = low;
			bar.Close = close;
			bar.NinjaTraderVolume = ninjaTraderVolume;
			bar.CapturedVolume = CapturedVolume;
			bar.BuyVolume = BuyVolume;
			bar.SellVolume = SellVolume;
			bar.UnknownVolume = UnknownVolume;
			bar.LastEventCount = LastEventCount;
			bar.BidEventCount = BidEventCount;
			bar.AskEventCount = AskEventCount;
			bar.AtAskCount = AtAskCount;
			bar.AboveAskCount = AboveAskCount;
			bar.AtBidCount = AtBidCount;
			bar.BelowBidCount = BelowBidCount;
			bar.BetweenBidAskCount = BetweenBidAskCount;
			bar.UnknownNoQuoteCount = UnknownNoQuoteCount;
			bar.LockedMarketCount = LockedMarketCount;
			bar.CrossedMarketCount = CrossedMarketCount;
			bar.AtAskVolume = AtAskVolume;
			bar.AboveAskVolume = AboveAskVolume;
			bar.AtBidVolume = AtBidVolume;
			bar.BelowBidVolume = BelowBidVolume;
			bar.BetweenBidAskVolume = BetweenBidAskVolume;
			bar.UnknownNoQuoteVolume = UnknownNoQuoteVolume;
			bar.CaptureCompletenessRatio = ninjaTraderVolume > 0 ? (double)CapturedVolume / ninjaTraderVolume : 0;
			bar.FirstMarketDataEventTime = FirstMarketDataEventTime;
			bar.LastMarketDataEventTime = LastMarketDataEventTime;
			bar.IsPartialBar = IsPartialBar;
			bar.IsRealtimeCaptured = IsRealtimeCaptured;
			bar.RunningDeltaOpen = RunningDeltaOpen;
			bar.RunningDeltaHigh = RunningDeltaHigh;
			bar.RunningDeltaLow = RunningDeltaLow;
			bar.RunningDeltaClose = RunningDeltaClose;
			bar.VolumeDifference = CapturedVolume - ninjaTraderVolume;
			if (ninjaTraderVolume == 0)
				bar.VolumeDifferencePercent = CapturedVolume == 0 ? 0 : 999999999;
			else
				bar.VolumeDifferencePercent = (double)bar.VolumeDifference / ninjaTraderVolume;
			bar.HadPendingEvents = HadPendingEvents;
			bar.PendingEventsAssigned = PendingEventsAssigned;
			bar.UnmatchedEventsObserved = UnmatchedEventsObserved;

			Dictionary<long, xPriceLevelOrderFlow> copy = new Dictionary<long, xPriceLevelOrderFlow>();
			foreach (KeyValuePair<long, xPriceLevelOrderFlow> pair in priceLevels)
				copy[pair.Key] = pair.Value.Clone();
			bar.PriceLevels = copy;

			CalculateDominantPrices(bar, copy);
			Validate(bar, copy, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent);
			return bar;
		}

		private void ObserveEventTime(DateTime timestamp)
		{
			if (timestamp == DateTime.MinValue)
				return;

			if (FirstMarketDataEventTime == DateTime.MinValue)
				FirstMarketDataEventTime = timestamp;
			LastMarketDataEventTime = timestamp;
		}

		private void CountRelation(xTradePriceRelation relation, long volume)
		{
			if (relation == xTradePriceRelation.AtAsk)
			{
				AtAskCount++;
				AtAskVolume += volume;
			}
			else if (relation == xTradePriceRelation.AboveAsk)
			{
				AboveAskCount++;
				AboveAskVolume += volume;
			}
			else if (relation == xTradePriceRelation.AtBid)
			{
				AtBidCount++;
				AtBidVolume += volume;
			}
			else if (relation == xTradePriceRelation.BelowBid)
			{
				BelowBidCount++;
				BelowBidVolume += volume;
			}
			else if (relation == xTradePriceRelation.BetweenBidAsk)
			{
				BetweenBidAskCount++;
				BetweenBidAskVolume += volume;
			}
			else if (relation == xTradePriceRelation.UnknownNoQuote)
			{
				UnknownNoQuoteCount++;
				UnknownNoQuoteVolume += volume;
			}
			else if (relation == xTradePriceRelation.LockedMarket)
			{
				LockedMarketCount++;
			}
			else if (relation == xTradePriceRelation.CrossedMarket)
			{
				CrossedMarketCount++;
			}
		}

		private void CalculateDominantPrices(xOrderFlowBar bar, Dictionary<long, xPriceLevelOrderFlow> levels)
		{
			long poc = long.MinValue;
			long maxBuy = long.MinValue;
			long maxSell = long.MinValue;
			long maxPosDelta = long.MinValue;
			long maxNegDelta = long.MaxValue;

			foreach (KeyValuePair<long, xPriceLevelOrderFlow> pair in levels)
			{
				xPriceLevelOrderFlow level = pair.Value;
				if (level.TotalVolume > poc)
				{
					poc = level.TotalVolume;
					bar.PocPrice = level.Price;
					bar.PocVolume = level.TotalVolume;
				}
				if (level.BuyVolume > maxBuy)
				{
					maxBuy = level.BuyVolume;
					bar.MaxBuyVolumePrice = level.Price;
					bar.MaxBuyVolume = level.BuyVolume;
				}
				if (level.SellVolume > maxSell)
				{
					maxSell = level.SellVolume;
					bar.MaxSellVolumePrice = level.Price;
					bar.MaxSellVolume = level.SellVolume;
				}
				if (level.Delta > maxPosDelta)
				{
					maxPosDelta = level.Delta;
					bar.MaxPositiveDeltaPrice = level.Price;
					bar.MaxPositiveDelta = level.Delta;
				}
				if (level.Delta < maxNegDelta)
				{
					maxNegDelta = level.Delta;
					bar.MaxNegativeDeltaPrice = level.Price;
					bar.MaxNegativeDelta = level.Delta;
				}
			}
		}

		private void Validate(xOrderFlowBar bar, Dictionary<long, xPriceLevelOrderFlow> levels, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent)
		{
			long total = 0;
			long buy = 0;
			long sell = 0;
			long unknown = 0;
			long delta = 0;

			foreach (KeyValuePair<long, xPriceLevelOrderFlow> pair in levels)
			{
				total += pair.Value.TotalVolume;
				buy += pair.Value.BuyVolume;
				sell += pair.Value.SellVolume;
				unknown += pair.Value.UnknownVolume;
				delta += pair.Value.Delta;
			}

			StringBuilder message = new StringBuilder();
			if (bar.BuyVolume + bar.SellVolume + bar.UnknownVolume != bar.CapturedVolume)
				message.Append("side volume sum mismatch; ");
			if (total != bar.CapturedVolume)
				message.Append("price total mismatch; ");
			if (buy != bar.BuyVolume)
				message.Append("price buy mismatch; ");
			if (sell != bar.SellVolume)
				message.Append("price sell mismatch; ");
			if (unknown != bar.UnknownVolume)
				message.Append("price unknown mismatch; ");
			if (delta != bar.Delta)
				message.Append("price delta mismatch; ");

			long absoluteDifference = Math.Abs(bar.VolumeDifference);
			double absoluteDifferencePercent = Math.Abs(bar.VolumeDifferencePercent);
			bool zeroVolumeMaintenance = bar.NinjaTraderVolume == 0 && bar.CapturedVolume == 0;
			bar.CaptureCompletenessPassed = zeroVolumeMaintenance
				|| bar.IsPartialBar
				|| absoluteDifference <= allowedVolumeDifferenceContracts
				|| absoluteDifferencePercent <= allowedVolumeDifferencePercent;
			if (!bar.CaptureCompletenessPassed)
			{
				message.Append(string.Format(CultureInfo.InvariantCulture,
					"volume mismatch: captured volume={0}; NinjaTrader volume={1}; difference={2}; difference percent={3:0.######}; pending-event count={4}; bar index={5}; bar interval={6}-{7}; ",
					bar.CapturedVolume,
					bar.NinjaTraderVolume,
					bar.VolumeDifference,
					bar.VolumeDifferencePercent,
					bar.PendingEventsAssigned,
					bar.BarIndex,
					bar.BarOpenTime,
					bar.BarCloseTime));
			}

			bar.ValidationPassed = message.Length == 0;
			bar.ValidationMessage = bar.ValidationPassed ? "OK" : message.ToString();
		}
	}

	public class xPvaOrderFlowCore : Indicator
	{
		private xMutableOrderFlowBar currentBar;
		private readonly Dictionary<int, xOrderFlowBar> finalizedBarsByIndex = new Dictionary<int, xOrderFlowBar>();
		private readonly Queue<int> finalizedOrder = new Queue<int>();
		private StreamWriter barWriter;
		private StreamWriter levelsWriter;
		private StreamWriter rawWriter;
		private readonly object writerLock = new object();
		private string exportFolder;
		private string fileInstrument;
		private string fileStamp;
		private int rowsSinceFlush;
		private int activeBarIndex;
		private bool hasActiveBar;
		private bool realtimeStarted;
		private bool terminated;
		private DateTime realtimeStartTime;
		private double currentBid;
		private double currentAsk;
		private long currentBidVolume;
		private long currentAskVolume;
		private bool hasBid;
		private bool hasAsk;
		private double lastPrice;
		private xTradeSide lastSide;
		private xTradePriceRelation lastRelation;
		private int mostRecentFinalizedBarIndex;
		private double mostRecentCaptureRatio;
		private DateTime lastPanelRefresh;
		private bool exportSnapshotNow;
		private bool snapshotFlushRequested;
		private Button snapshotButton;
		private Grid snapshotButtonHost;
		private xOrderFlowRegistryKey registryKey;
		private Guid registryPublisherId;
		private bool registryRegistered;
		private readonly List<xPendingMarketDataEvent> pendingEvents = new List<xPendingMarketDataEvent>();
		private long arrivalSequence;
		private int unmatchedEventsObserved;
		private int droppedEventsDueToCapacity;
		private bool captureIntegrityCompromised;
		private DateTime lastAssignedEventTime;
		private xTimestampRelationToCurrentBar lastTimestampRelation;
		private xPendingEventDisposition lastPendingDisposition;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaOrderFlowCore";
				Description = "Feed-neutral real-time order-flow capture engine for APVA research.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				UsePreviousSideForBetweenBidAsk = false;
				ExportBarSummaries = true;
				ExportPriceLevels = true;
				ExportRawEvents = false;
				ExportPartialOnTerminate = true;
				FlushOnBarFinalization = true;
				ExportSnapshotNow = false;
				FlushEveryNRows = 1000;
				MaxBarsRetained = 2000;
				MaxPendingEvents = 100000;
				AllowedVolumeDifferenceContracts = 2;
				AllowedVolumeDifferencePercent = 0.001;
				ShowDiagnosticsPanel = true;
				RegistryInstanceKey = "APVA";
			}
			else if (State == State.DataLoaded)
			{
				currentBar = new xMutableOrderFlowBar(TickSize);
				activeBarIndex = -1;
				mostRecentFinalizedBarIndex = -1;
				mostRecentCaptureRatio = 0;
				arrivalSequence = 0;
				unmatchedEventsObserved = 0;
				droppedEventsDueToCapacity = 0;
				captureIntegrityCompromised = false;
				lastAssignedEventTime = DateTime.MinValue;
				lastTimestampRelation = xTimestampRelationToCurrentBar.NoCurrentBar;
				lastPendingDisposition = xPendingEventDisposition.WaitingForBar;
				pendingEvents.Clear();
				lastSide = xTradeSide.Unknown;
				lastRelation = xTradePriceRelation.UnknownNoQuote;
				EnsureExportFolder();
				registryPublisherId = Guid.NewGuid();
				registryKey = BuildRegistryKey();
				RegisterRegistryPublisher();
			}
			else if (State == State.Historical)
			{
				Draw.TextFixed(this, "xPvaOrderFlowCoreHistorical",
					"Historical bars do not contain reconstructed bid/ask order flow.\nReal-time capture begins when the indicator enters State.Realtime.",
					TextPosition.BottomLeft);
				AddSnapshotButton();
			}
			else if (State == State.Realtime)
			{
				realtimeStarted = true;
				realtimeStartTime = DateTime.MinValue;
				OpenWriters();
			}
			else if (State == State.Terminated)
			{
				if (!terminated && ExportPartialOnTerminate && hasActiveBar)
				{
					currentBar.IsPartialBar = true;
					xOrderFlowBar bar = FinalizeActiveBar(true);
					if (bar != null)
						StoreAndExportFinalizedBar(bar);
				}
				terminated = true;
				if (registryRegistered)
					xPvaOrderFlowRegistry.UnregisterPublisher(registryKey, registryPublisherId);
				RemoveSnapshotButton();
				CloseWriters();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			if (State == State.Realtime)
			{
				SyncActiveBar(CurrentBar, DateTime.MinValue);
				DrainPendingEventsForCurrentBar();
			}

			ProcessSnapshotRequest();
			UpdateDiagnosticsPanel(false);
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null || State != State.Realtime)
				return;

			ProcessSnapshotRequest();

			if (realtimeStartTime == DateTime.MinValue)
				realtimeStartTime = e.Time;

			SyncActiveBar(CurrentBar, e.Time);

			if (e.MarketDataType == MarketDataType.Bid)
			{
				currentBid = e.Price;
				currentBidVolume = ToLongVolume(e.Volume);
				hasBid = currentBid > 0;
				xPendingMarketDataEvent evt = CreatePendingEvent(e, xTradeSide.Unknown, xTradePriceRelation.UnknownNoQuote);
				RouteMarketDataEvent(evt);
				UpdateDiagnosticsPanel(false);
			}
			else if (e.MarketDataType == MarketDataType.Ask)
			{
				currentAsk = e.Price;
				currentAskVolume = ToLongVolume(e.Volume);
				hasAsk = currentAsk > 0;
				xPendingMarketDataEvent evt = CreatePendingEvent(e, xTradeSide.Unknown, xTradePriceRelation.UnknownNoQuote);
				RouteMarketDataEvent(evt);
				UpdateDiagnosticsPanel(false);
			}
			else if (e.MarketDataType == MarketDataType.Last)
			{
				long volume = ToLongVolume(e.Volume);
				long priceTicks = PriceToTicks(e.Price);
				xTradePriceRelation relation;
				xTradeSide side = ClassifyLast(priceTicks, out relation);
				lastPrice = e.Price;
				lastRelation = relation;
				if (side == xTradeSide.Buy || side == xTradeSide.Sell)
					lastSide = side;

				xPendingMarketDataEvent evt = CreatePendingEvent(e, side, relation);
				RouteMarketDataEvent(evt);
				UpdateDiagnosticsPanel(true);
			}
		}

		public bool TryGetOrderFlowBar(int absoluteBarIndex, out xOrderFlowBar bar)
		{
			return finalizedBarsByIndex.TryGetValue(absoluteBarIndex, out bar);
		}

		public xOrderFlowBar GetMostRecentFinalizedBar()
		{
			xOrderFlowBar bar;
			if (mostRecentFinalizedBarIndex >= 0 && finalizedBarsByIndex.TryGetValue(mostRecentFinalizedBarIndex, out bar))
				return bar;
			return null;
		}

		private void SyncActiveBar(int targetBarIndex, DateTime eventTime)
		{
			if (targetBarIndex < 0)
				targetBarIndex = CurrentBar;
			if (targetBarIndex < 0)
				return;

			if (!hasActiveBar)
			{
				StartBar(targetBarIndex, eventTime, IsFirstRealtimeBarPartial(targetBarIndex, eventTime));
				DrainPendingEventsForCurrentBar();
				return;
			}

			while (targetBarIndex > activeBarIndex)
			{
				xOrderFlowBar finalized = FinalizeActiveBar(false);
				if (finalized != null)
					StoreAndExportFinalizedBar(finalized);
				StartBar(activeBarIndex + 1, eventTime, false);
				DrainPendingEventsForCurrentBar();
			}
		}

		private void StartBar(int barIndex, DateTime eventTime, bool isPartial)
		{
			DateTime ninjaBarTime;
			DateTime openTime;
			DateTime closeTime;
			ResolveBarTimes(barIndex, eventTime, out ninjaBarTime, out openTime, out closeTime);
			activeBarIndex = barIndex;
			hasActiveBar = true;
			currentBar.Reset(barIndex, ninjaBarTime, openTime, closeTime, isPartial, true);
		}

		private xOrderFlowBar FinalizeActiveBar(bool forcePartial)
		{
			if (!hasActiveBar)
				return null;

			int barsAgo = CurrentBar - activeBarIndex;
			double open = GetSeriesValue(Opens[0], barsAgo);
			double high = GetSeriesValue(Highs[0], barsAgo);
			double low = GetSeriesValue(Lows[0], barsAgo);
			double close = GetSeriesValue(Closes[0], barsAgo);
			long ntVolume = ToLongVolume(GetSeriesValue(Volumes[0], barsAgo));
			if (forcePartial)
				currentBar.IsPartialBar = true;

			currentBar.UnmatchedEventsObserved = unmatchedEventsObserved;
			xOrderFlowBar bar = currentBar.FinalizeBar(open, high, low, close, ntVolume, AllowedVolumeDifferenceContracts, AllowedVolumeDifferencePercent);
			if (!bar.ValidationPassed)
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore validation failed: BarIndex={0}, Time={1}, {2}", bar.BarIndex, FormatTime(bar.NinjaBarTime), bar.ValidationMessage));
			return bar;
		}

		private void StoreAndExportFinalizedBar(xOrderFlowBar bar)
		{
			lock (writerLock)
			{
				WritePriceLevels(bar);
				WriteBarSummary(bar);

				if (FlushOnBarFinalization)
					FlushCompletedBarWriters();
			}

			finalizedBarsByIndex[bar.BarIndex] = bar;
			finalizedOrder.Enqueue(bar.BarIndex);
			mostRecentFinalizedBarIndex = bar.BarIndex;
			mostRecentCaptureRatio = bar.CaptureCompletenessRatio;

			while (finalizedOrder.Count > MaxBarsRetained)
			{
				int removeIndex = finalizedOrder.Dequeue();
				if (removeIndex != bar.BarIndex)
					finalizedBarsByIndex.Remove(removeIndex);
			}

			if (bar.ValidationPassed && bar.CaptureCompletenessPassed && registryRegistered)
				xPvaOrderFlowRegistry.Publish(registryKey, registryPublisherId, bar);
		}

		private xPendingMarketDataEvent CreatePendingEvent(MarketDataEventArgs e, xTradeSide side, xTradePriceRelation relation)
		{
			return new xPendingMarketDataEvent
			{
				ArrivalSequence = ++arrivalSequence,
				Timestamp = e.Time,
				EventType = e.MarketDataType,
				Price = e.Price,
				Volume = ToLongVolume(e.Volume),
				BidAtEvent = currentBid,
				AskAtEvent = currentAsk,
				HasBidAtEvent = hasBid,
				HasAskAtEvent = hasAsk,
				ClassifiedSide = side,
				PriceRelation = relation
			};
		}

		private void RouteMarketDataEvent(xPendingMarketDataEvent evt)
		{
			xTimestampRelationToCurrentBar relation = GetTimestampRelationToCurrentBar(evt.Timestamp);
			lastTimestampRelation = relation;

			if (TryAssignEventToCurrentBar(evt))
			{
				lastPendingDisposition = xPendingEventDisposition.Assigned;
				WriteRawEvent(evt, false, pendingEvents.Count, activeBarIndex);
				return;
			}

			if (relation == xTimestampRelationToCurrentBar.AtClose || relation == xTimestampRelationToCurrentBar.AfterClose || relation == xTimestampRelationToCurrentBar.NoCurrentBar)
			{
				EnqueuePendingEvent(evt);
				lastPendingDisposition = xPendingEventDisposition.WaitingForBar;
				WriteRawEvent(evt, true, pendingEvents.Count, -1);
				return;
			}

			unmatchedEventsObserved++;
			captureIntegrityCompromised = true;
			lastPendingDisposition = xPendingEventDisposition.NoMatchingPrimaryBar;
			Print(string.Format(CultureInfo.InvariantCulture,
				"xPvaOrderFlowCore WARNING: market-data event did not match current primary bar interval. EventTime={0}, Relation={1}, CurrentBar={2}, Interval={3}-{4}",
				FormatTime(evt.Timestamp),
				relation,
				activeBarIndex,
				hasActiveBar ? FormatTime(currentBar.BarOpenTime) : string.Empty,
				hasActiveBar ? FormatTime(currentBar.BarCloseTime) : string.Empty));
			WriteRawEvent(evt, false, pendingEvents.Count, -1);
		}

		private bool TryAssignEventToCurrentBar(xPendingMarketDataEvent evt)
		{
			if (GetTimestampRelationToCurrentBar(evt.Timestamp) != xTimestampRelationToCurrentBar.Inside)
				return false;

			ApplyEventToCurrentBar(evt, true);
			return true;
		}

		private void ApplyEventToCurrentBar(xPendingMarketDataEvent evt, bool publishSnapshot)
		{
			if (!hasActiveBar)
				return;

			if (evt.EventType == MarketDataType.Bid)
				currentBar.ProcessBid(evt.Timestamp);
			else if (evt.EventType == MarketDataType.Ask)
				currentBar.ProcessAsk(evt.Timestamp);
			else if (evt.EventType == MarketDataType.Last)
			{
				currentBar.ProcessLast(evt.Price, PriceToTicks(evt.Price), evt.Volume, evt.ClassifiedSide, evt.PriceRelation, evt.Timestamp);
				if (publishSnapshot && registryRegistered)
					xPvaOrderFlowRegistry.PublishLiveSnapshot(registryKey, registryPublisherId, currentBar.CreateLiveSnapshot(evt.Timestamp, pendingEvents.Count, unmatchedEventsObserved, captureIntegrityCompromised));
			}

			lastAssignedEventTime = evt.Timestamp;
		}

		private void EnqueuePendingEvent(xPendingMarketDataEvent evt)
		{
			if (pendingEvents.Count >= MaxPendingEvents)
			{
				droppedEventsDueToCapacity++;
				captureIntegrityCompromised = true;
				lastPendingDisposition = xPendingEventDisposition.DroppedDueToCapacity;
				Print(string.Format(CultureInfo.InvariantCulture,
					"xPvaOrderFlowCore WARNING: pending-event queue exceeded MaxPendingEvents={0}. Data integrity is compromised. EventTime={1}, DroppedCount={2}",
					MaxPendingEvents,
					FormatTime(evt.Timestamp),
					droppedEventsDueToCapacity));
				return;
			}

			int insertAt = pendingEvents.Count;
			while (insertAt > 0 && pendingEvents[insertAt - 1].Timestamp > evt.Timestamp)
				insertAt--;
			pendingEvents.Insert(insertAt, evt);
		}

		private void DrainPendingEventsForCurrentBar()
		{
			if (!hasActiveBar || pendingEvents.Count == 0)
				return;

			int assigned = 0;
			while (pendingEvents.Count > 0)
			{
				xPendingMarketDataEvent evt = pendingEvents[0];
				xTimestampRelationToCurrentBar relation = GetTimestampRelationToCurrentBar(evt.Timestamp);
				if (relation == xTimestampRelationToCurrentBar.Inside)
				{
					pendingEvents.RemoveAt(0);
					currentBar.HadPendingEvents = true;
					currentBar.PendingEventsAssigned++;
					ApplyEventToCurrentBar(evt, false);
					assigned++;
					lastPendingDisposition = xPendingEventDisposition.Assigned;
					continue;
				}

				if (relation == xTimestampRelationToCurrentBar.BeforeOpen)
				{
					pendingEvents.RemoveAt(0);
					unmatchedEventsObserved++;
					captureIntegrityCompromised = true;
					lastPendingDisposition = xPendingEventDisposition.NoMatchingPrimaryBar;
					Print(string.Format(CultureInfo.InvariantCulture,
						"xPvaOrderFlowCore WARNING: pending event was older than the current primary bar and was not assigned. EventTime={0}, CurrentBar={1}, Interval={2}-{3}",
						FormatTime(evt.Timestamp),
						activeBarIndex,
						FormatTime(currentBar.BarOpenTime),
						FormatTime(currentBar.BarCloseTime)));
					continue;
				}

				break;
			}

			if (assigned > 0 && registryRegistered)
				xPvaOrderFlowRegistry.PublishLiveSnapshot(registryKey, registryPublisherId, currentBar.CreateLiveSnapshot(lastAssignedEventTime, pendingEvents.Count, unmatchedEventsObserved, captureIntegrityCompromised));
		}

		private xTimestampRelationToCurrentBar GetTimestampRelationToCurrentBar(DateTime timestamp)
		{
			if (!hasActiveBar || timestamp == DateTime.MinValue || currentBar.BarOpenTime == DateTime.MinValue || currentBar.BarCloseTime == DateTime.MinValue)
				return xTimestampRelationToCurrentBar.NoCurrentBar;
			if (timestamp < currentBar.BarOpenTime)
				return xTimestampRelationToCurrentBar.BeforeOpen;
			if (timestamp < currentBar.BarCloseTime)
				return xTimestampRelationToCurrentBar.Inside;
			if (timestamp == currentBar.BarCloseTime)
				return xTimestampRelationToCurrentBar.AtClose;
			return xTimestampRelationToCurrentBar.AfterClose;
		}

		private bool IsFirstRealtimeBarPartial(int barIndex, DateTime eventTime)
		{
			DateTime ninjaTime;
			DateTime openTime;
			DateTime closeTime;
			ResolveBarTimes(barIndex, eventTime, out ninjaTime, out openTime, out closeTime);
			return eventTime == DateTime.MinValue || openTime == DateTime.MinValue || eventTime > openTime;
		}

		private void ResolveBarTimes(int barIndex, DateTime eventTime, out DateTime ninjaBarTime, out DateTime openTime, out DateTime closeTime)
		{
			int barsAgo = CurrentBar - barIndex;
			if (barsAgo >= 0 && barsAgo <= CurrentBar)
				ninjaBarTime = Times[0][barsAgo];
			else
				ninjaBarTime = eventTime;

			closeTime = ninjaBarTime;
			if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value > 0 && closeTime != DateTime.MinValue)
				openTime = closeTime.AddMinutes(-BarsPeriod.Value);
			else
				openTime = barIndex > 0 && barsAgo + 1 <= CurrentBar ? Times[0][barsAgo + 1] : DateTime.MinValue;
		}

		private xTradeSide ClassifyLast(long lastTicks, out xTradePriceRelation relation)
		{
			if (!hasBid || !hasAsk)
			{
				relation = xTradePriceRelation.UnknownNoQuote;
				return xTradeSide.Unknown;
			}

			long bidTicks = PriceToTicks(currentBid);
			long askTicks = PriceToTicks(currentAsk);

			if (bidTicks > askTicks)
			{
				relation = xTradePriceRelation.CrossedMarket;
				return xTradeSide.Unknown;
			}

			if (bidTicks == askTicks)
			{
				relation = xTradePriceRelation.LockedMarket;
				if (lastTicks >= askTicks)
					return xTradeSide.Buy;
				if (lastTicks <= bidTicks)
					return xTradeSide.Sell;
				return xTradeSide.Unknown;
			}

			if (lastTicks == askTicks)
			{
				relation = xTradePriceRelation.AtAsk;
				return xTradeSide.Buy;
			}
			if (lastTicks > askTicks)
			{
				relation = xTradePriceRelation.AboveAsk;
				return xTradeSide.Buy;
			}
			if (lastTicks == bidTicks)
			{
				relation = xTradePriceRelation.AtBid;
				return xTradeSide.Sell;
			}
			if (lastTicks < bidTicks)
			{
				relation = xTradePriceRelation.BelowBid;
				return xTradeSide.Sell;
			}

			relation = xTradePriceRelation.BetweenBidAsk;
			if (UsePreviousSideForBetweenBidAsk && (lastSide == xTradeSide.Buy || lastSide == xTradeSide.Sell))
				return lastSide;
			return xTradeSide.Unknown;
		}

		private long PriceToTicks(double price)
		{
			if (TickSize <= 0)
				return 0;
			return (long)Math.Round(price / TickSize, MidpointRounding.AwayFromZero);
		}

		private long ToLongVolume(double volume)
		{
			if (volume <= 0)
				return 0;
			return (long)Math.Round(volume, MidpointRounding.AwayFromZero);
		}

		private double GetSeriesValue(ISeries<double> series, int barsAgo)
		{
			if (barsAgo >= 0 && barsAgo <= CurrentBar)
				return series[barsAgo];
			return 0;
		}

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xPvaOrderFlowCore");
			Directory.CreateDirectory(exportFolder);
			fileInstrument = SanitizeFileName(Instrument != null ? Instrument.FullName : "UnknownInstrument");
			fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
		}

		private xOrderFlowRegistryKey BuildRegistryKey()
		{
			string instrumentFullName = Instrument != null ? Instrument.FullName : string.Empty;
			string masterInstrumentName = Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : string.Empty;
			string tradingHoursName = string.Empty;
			try
			{
				if (Bars != null && Bars.TradingHours != null)
					tradingHoursName = Bars.TradingHours.Name;
			}
			catch
			{
				tradingHoursName = string.Empty;
			}

			int value2 = 0;
			try
			{
				value2 = BarsPeriod != null ? BarsPeriod.Value2 : 0;
			}
			catch
			{
				value2 = 0;
			}

			return new xOrderFlowRegistryKey(
				instrumentFullName,
				masterInstrumentName,
				BarsPeriod != null ? BarsPeriod.BarsPeriodType : BarsPeriodType.Minute,
				BarsPeriod != null ? BarsPeriod.Value : 0,
				value2,
				tradingHoursName,
				string.Empty,
				RegistryInstanceKey);
		}

		private void RegisterRegistryPublisher()
		{
			xOrderFlowPublisherRegistrationResult result = xPvaOrderFlowRegistry.RegisterPublisher(registryKey, registryPublisherId);
			registryRegistered = result == xOrderFlowPublisherRegistrationResult.Registered || result == xOrderFlowPublisherRegistrationResult.Refreshed;

			if (registryRegistered)
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore registered publisher {0} for registry key {1}", registryPublisherId, registryKey));
			else
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore WARNING: duplicate/ambiguous publisher for registry key {0}. This core will not publish mixed data.", registryKey));
		}

		private void OpenWriters()
		{
			try
			{
				if (ExportBarSummaries)
				{
					barWriter = new StreamWriter(Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore_BARS_{0}_{1}.csv", fileInstrument, fileStamp)), false, Encoding.UTF8);
					barWriter.WriteLine("BarIndex,NinjaBarTime,BarOpenTime,BarCloseTime,Open,High,Low,Close,NinjaTraderVolume,CapturedVolume,CaptureCompletenessRatio,BuyVolume,SellVolume,UnknownVolume,Delta,AbsoluteDelta,LastEventCount,BidEventCount,AskEventCount,AtAskCount,AboveAskCount,AtBidCount,BelowBidCount,BetweenBidAskCount,UnknownNoQuoteCount,LockedMarketCount,CrossedMarketCount,AtAskVolume,AboveAskVolume,AtBidVolume,BelowBidVolume,BetweenBidAskVolume,UnknownNoQuoteVolume,POCPrice,POCVolume,MaxBuyVolumePrice,MaxBuyVolume,MaxSellVolumePrice,MaxSellVolume,MaxPositiveDeltaPrice,MaxPositiveDelta,MaxNegativeDeltaPrice,MaxNegativeDelta,FirstMarketDataEventTime,LastMarketDataEventTime,IsPartialBar,IsRealtimeCaptured,ValidationPassed,ValidationMessage,RunningDeltaOpen,RunningDeltaHigh,RunningDeltaLow,RunningDeltaClose,VolumeDifference,VolumeDifferencePercent,CaptureCompletenessPassed,HadPendingEvents,PendingEventsAssigned,UnmatchedEventsObserved");
				}
				if (ExportPriceLevels)
				{
					levelsWriter = new StreamWriter(Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore_LEVELS_{0}_{1}.csv", fileInstrument, fileStamp)), false, Encoding.UTF8);
					levelsWriter.WriteLine("BarIndex,NinjaBarTime,BarOpenTime,BarCloseTime,PriceTicks,Price,TotalVolume,BuyVolume,SellVolume,UnknownVolume,Delta,TradeCount,BuyTradeCount,SellTradeCount,UnknownTradeCount");
				}
				if (ExportRawEvents)
				{
					rawWriter = new StreamWriter(Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowCore_RAW_{0}_{1}.csv", fileInstrument, fileStamp)), false, Encoding.UTF8);
					rawWriter.WriteLine("Timestamp,EventType,Price,PriceTicks,Volume,CurrentBid,CurrentBidTicks,CurrentAsk,CurrentAskTicks,SpreadTicks,TradeSide,TradePriceRelation,PrimaryBarIndex,Instrument,EventTimestamp,ArrivalSequence,CurrentPrimaryBarIndex,CurrentBarOpenTime,CurrentBarCloseTime,TimestampRelationToCurrentBar,WasBuffered,PendingQueueSizeAfterEvent,AssignedBarIndex,AssignedBarOpenTime,AssignedBarCloseTime");
				}

				Print("xPvaOrderFlowCore CSV note: Files copied while NinjaTrader is actively writing may contain a partial final row unless the writers are flushed first. Use ExportSnapshotNow or FlushOnBarFinalization for safer live inspection.");
			}
			catch (Exception ex)
			{
				Print("xPvaOrderFlowCore export open failed: " + ex.Message);
				CloseWriters();
			}
		}

		private void CloseWriters()
		{
			lock (writerLock)
			{
				try
				{
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
					if (rawWriter != null)
					{
						rawWriter.Flush();
						rawWriter.Close();
						rawWriter = null;
					}
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore export close failed: " + ex.Message);
				}
			}
		}

		private void WriteBarSummary(xOrderFlowBar bar)
		{
			if (barWriter == null)
				return;

			lock (writerLock)
			{
				try
				{
					string[] columns =
					{
						bar.BarIndex.ToString(CultureInfo.InvariantCulture),
						FormatTime(bar.NinjaBarTime),
						FormatTime(bar.BarOpenTime),
						FormatTime(bar.BarCloseTime),
						FormatDouble(bar.Open),
						FormatDouble(bar.High),
						FormatDouble(bar.Low),
						FormatDouble(bar.Close),
						bar.NinjaTraderVolume.ToString(CultureInfo.InvariantCulture),
						bar.CapturedVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.CaptureCompletenessRatio),
						bar.BuyVolume.ToString(CultureInfo.InvariantCulture),
						bar.SellVolume.ToString(CultureInfo.InvariantCulture),
						bar.UnknownVolume.ToString(CultureInfo.InvariantCulture),
						bar.Delta.ToString(CultureInfo.InvariantCulture),
						bar.AbsoluteDelta.ToString(CultureInfo.InvariantCulture),
						bar.LastEventCount.ToString(CultureInfo.InvariantCulture),
						bar.BidEventCount.ToString(CultureInfo.InvariantCulture),
						bar.AskEventCount.ToString(CultureInfo.InvariantCulture),
						bar.AtAskCount.ToString(CultureInfo.InvariantCulture),
						bar.AboveAskCount.ToString(CultureInfo.InvariantCulture),
						bar.AtBidCount.ToString(CultureInfo.InvariantCulture),
						bar.BelowBidCount.ToString(CultureInfo.InvariantCulture),
						bar.BetweenBidAskCount.ToString(CultureInfo.InvariantCulture),
						bar.UnknownNoQuoteCount.ToString(CultureInfo.InvariantCulture),
						bar.LockedMarketCount.ToString(CultureInfo.InvariantCulture),
						bar.CrossedMarketCount.ToString(CultureInfo.InvariantCulture),
						bar.AtAskVolume.ToString(CultureInfo.InvariantCulture),
						bar.AboveAskVolume.ToString(CultureInfo.InvariantCulture),
						bar.AtBidVolume.ToString(CultureInfo.InvariantCulture),
						bar.BelowBidVolume.ToString(CultureInfo.InvariantCulture),
						bar.BetweenBidAskVolume.ToString(CultureInfo.InvariantCulture),
						bar.UnknownNoQuoteVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.PocPrice),
						bar.PocVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.MaxBuyVolumePrice),
						bar.MaxBuyVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.MaxSellVolumePrice),
						bar.MaxSellVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.MaxPositiveDeltaPrice),
						bar.MaxPositiveDelta.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.MaxNegativeDeltaPrice),
						bar.MaxNegativeDelta.ToString(CultureInfo.InvariantCulture),
						FormatTime(bar.FirstMarketDataEventTime),
						FormatTime(bar.LastMarketDataEventTime),
						bar.IsPartialBar.ToString(),
						bar.IsRealtimeCaptured.ToString(),
						bar.ValidationPassed.ToString(),
						bar.ValidationMessage,
						bar.RunningDeltaOpen.ToString(CultureInfo.InvariantCulture),
						bar.RunningDeltaHigh.ToString(CultureInfo.InvariantCulture),
						bar.RunningDeltaLow.ToString(CultureInfo.InvariantCulture),
						bar.RunningDeltaClose.ToString(CultureInfo.InvariantCulture),
						bar.VolumeDifference.ToString(CultureInfo.InvariantCulture),
						FormatDouble(bar.VolumeDifferencePercent),
						bar.CaptureCompletenessPassed.ToString(),
						bar.HadPendingEvents.ToString(),
						bar.PendingEventsAssigned.ToString(CultureInfo.InvariantCulture),
						bar.UnmatchedEventsObserved.ToString(CultureInfo.InvariantCulture)
					};
					barWriter.WriteLine(ToCsv(columns));
					FlushPeriodically();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore bar export failed: " + ex.Message);
				}
			}
		}

		private void WritePriceLevels(xOrderFlowBar bar)
		{
			if (levelsWriter == null || bar.PriceLevels == null)
				return;

			lock (writerLock)
			{
				try
				{
					List<long> keys = new List<long>(bar.PriceLevels.Keys);
					keys.Sort();
					foreach (long key in keys)
					{
						xPriceLevelOrderFlow level = bar.PriceLevels[key];
						string[] columns =
						{
							bar.BarIndex.ToString(CultureInfo.InvariantCulture),
							FormatTime(bar.NinjaBarTime),
							FormatTime(bar.BarOpenTime),
							FormatTime(bar.BarCloseTime),
							level.PriceTicks.ToString(CultureInfo.InvariantCulture),
							FormatDouble(level.Price),
							level.TotalVolume.ToString(CultureInfo.InvariantCulture),
							level.BuyVolume.ToString(CultureInfo.InvariantCulture),
							level.SellVolume.ToString(CultureInfo.InvariantCulture),
							level.UnknownVolume.ToString(CultureInfo.InvariantCulture),
							level.Delta.ToString(CultureInfo.InvariantCulture),
							level.TradeCount.ToString(CultureInfo.InvariantCulture),
							level.BuyTradeCount.ToString(CultureInfo.InvariantCulture),
							level.SellTradeCount.ToString(CultureInfo.InvariantCulture),
							level.UnknownTradeCount.ToString(CultureInfo.InvariantCulture)
						};
						levelsWriter.WriteLine(ToCsv(columns));
					}
					FlushPeriodically();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore level export failed: " + ex.Message);
				}
			}
		}

		private void WriteRawEvent(xPendingMarketDataEvent e, bool wasBuffered, int pendingQueueSizeAfterEvent, int assignedBarIndex)
		{
			if (rawWriter == null)
				return;

			lock (writerLock)
			{
				try
				{
					long priceTicks = PriceToTicks(e.Price);
					long bidTicks = e.HasBidAtEvent ? PriceToTicks(e.BidAtEvent) : 0;
					long askTicks = e.HasAskAtEvent ? PriceToTicks(e.AskAtEvent) : 0;
					long spreadTicks = e.HasBidAtEvent && e.HasAskAtEvent ? askTicks - bidTicks : 0;
					string[] columns =
					{
						FormatTime(e.Timestamp),
						e.EventType.ToString(),
						FormatDouble(e.Price),
						priceTicks.ToString(CultureInfo.InvariantCulture),
						e.Volume.ToString(CultureInfo.InvariantCulture),
						e.HasBidAtEvent ? FormatDouble(e.BidAtEvent) : string.Empty,
						e.HasBidAtEvent ? bidTicks.ToString(CultureInfo.InvariantCulture) : string.Empty,
						e.HasAskAtEvent ? FormatDouble(e.AskAtEvent) : string.Empty,
						e.HasAskAtEvent ? askTicks.ToString(CultureInfo.InvariantCulture) : string.Empty,
						spreadTicks.ToString(CultureInfo.InvariantCulture),
						e.ClassifiedSide.ToString(),
						e.PriceRelation.ToString(),
						activeBarIndex.ToString(CultureInfo.InvariantCulture),
						Instrument != null ? Instrument.FullName : string.Empty,
						FormatTime(e.Timestamp),
						e.ArrivalSequence.ToString(CultureInfo.InvariantCulture),
						activeBarIndex.ToString(CultureInfo.InvariantCulture),
						hasActiveBar ? FormatTime(currentBar.BarOpenTime) : string.Empty,
						hasActiveBar ? FormatTime(currentBar.BarCloseTime) : string.Empty,
						lastTimestampRelation.ToString(),
						wasBuffered.ToString(),
						pendingQueueSizeAfterEvent.ToString(CultureInfo.InvariantCulture),
						assignedBarIndex >= 0 ? assignedBarIndex.ToString(CultureInfo.InvariantCulture) : string.Empty,
						assignedBarIndex >= 0 && hasActiveBar ? FormatTime(currentBar.BarOpenTime) : string.Empty,
						assignedBarIndex >= 0 && hasActiveBar ? FormatTime(currentBar.BarCloseTime) : string.Empty
					};
					rawWriter.WriteLine(ToCsv(columns));
					FlushPeriodically();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore raw export failed: " + ex.Message);
				}
			}
		}

		private void FlushPeriodically()
		{
			rowsSinceFlush++;
			if (FlushEveryNRows <= 1 || rowsSinceFlush >= FlushEveryNRows)
			{
				if (barWriter != null)
					barWriter.Flush();
				if (levelsWriter != null)
					levelsWriter.Flush();
				if (rawWriter != null)
					rawWriter.Flush();
				rowsSinceFlush = 0;
			}
		}

		private void FlushCompletedBarWriters()
		{
			lock (writerLock)
			{
				try
				{
					if (levelsWriter != null)
						levelsWriter.Flush();
					if (barWriter != null)
						barWriter.Flush();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore completed-bar flush failed: " + ex.Message);
				}
			}
		}

		private void FlushAllWriters(string reason)
		{
			lock (writerLock)
			{
				try
				{
					if (levelsWriter != null)
						levelsWriter.Flush();
					if (barWriter != null)
						barWriter.Flush();
					if (rawWriter != null)
						rawWriter.Flush();

					Print(string.Format(CultureInfo.InvariantCulture,
						"xPvaOrderFlowCore snapshot flush complete ({0}). MostRecentFinalizedBarIndex={1}, ExportFolder={2}",
						reason,
						mostRecentFinalizedBarIndex,
						exportFolder));
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowCore snapshot flush failed: " + ex.Message);
				}
			}
		}

		private void ProcessSnapshotRequest()
		{
			if (!snapshotFlushRequested)
				return;

			snapshotFlushRequested = false;
			exportSnapshotNow = false;
			FlushAllWriters("user request");
		}

		private void AddSnapshotButton()
		{
			if (ChartControl == null || snapshotButtonHost != null)
				return;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				if (State >= State.Terminated || snapshotButtonHost != null)
					return;

				snapshotButtonHost = new Grid
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(6, 26, 0, 0),
					Width = 82,
					Height = 26
				};
				snapshotButton = new Button
				{
					Content = "OF Flush",
					ToolTip = "Flush xPvaOrderFlowCore CSV writers without finalizing the active bar.",
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Padding = new Thickness(4, 1, 4, 1)
				};
				snapshotButton.Click += SnapshotButtonClick;
				snapshotButtonHost.Children.Add(snapshotButton);
				UserControlCollection.Add(snapshotButtonHost);
			});
		}

		private void RemoveSnapshotButton()
		{
			if (ChartControl == null || snapshotButtonHost == null)
				return;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				if (snapshotButtonHost == null)
					return;

				if (snapshotButton != null)
					snapshotButton.Click -= SnapshotButtonClick;
				UserControlCollection.Remove(snapshotButtonHost);
				snapshotButton = null;
				snapshotButtonHost = null;
			});
		}

		private void SnapshotButtonClick(object sender, RoutedEventArgs e)
		{
			snapshotFlushRequested = false;
			exportSnapshotNow = false;
			FlushAllWriters("chart button");
		}

		private void UpdateDiagnosticsPanel(bool force)
		{
			if (!ShowDiagnosticsPanel)
				return;

			DateTime now = DateTime.Now;
			if (!force && lastPanelRefresh != DateTime.MinValue && (now - lastPanelRefresh).TotalMilliseconds < 500)
				return;
			lastPanelRefresh = now;

			long ntVolume = CurrentBar >= 0 ? ToLongVolume(Volume[0]) : 0;
			DateTime oldestPending = pendingEvents.Count > 0 ? pendingEvents[0].Timestamp : DateTime.MinValue;
			DateTime newestPending = pendingEvents.Count > 0 ? pendingEvents[pendingEvents.Count - 1].Timestamp : DateTime.MinValue;
			string text = string.Format(CultureInfo.InvariantCulture,
				"Feed-neutral order-flow capture\nInstrument: {0}\nCurrent Bid: {1}\nCurrent Ask: {2}\nLast Price: {3}\nLast Side: {4}\nLast Price Relation: {5}\nCurrent Bar Captured Volume: {6}\nCurrent Bar NT Volume: {7}\nCurrent Bar Buy Volume: {8}\nCurrent Bar Sell Volume: {9}\nCurrent Bar Unknown Volume: {10}\nCurrent Bar Delta: {11}\nMost Recent Finalized Bar Index: {12}\nMost Recent Capture Ratio: {13:0.###}\nPending events: {14}\nOldest pending event time: {15}\nNewest pending event time: {16}\nCurrent bar interval: {17} - {18}\nLast assigned event time: {19}\nUnmatched-event count: {20}\nCapture-integrity status: {21}\nLast timestamp relation: {22}\nLast pending disposition: {23}\nExport Folder: {24}",
				Instrument != null ? Instrument.FullName : string.Empty,
				hasBid ? FormatDouble(currentBid) : string.Empty,
				hasAsk ? FormatDouble(currentAsk) : string.Empty,
				lastPrice > 0 ? FormatDouble(lastPrice) : string.Empty,
				lastSide,
				lastRelation,
				hasActiveBar ? currentBar.CapturedVolume.ToString(CultureInfo.InvariantCulture) : "0",
				ntVolume,
				hasActiveBar ? currentBar.BuyVolume.ToString(CultureInfo.InvariantCulture) : "0",
				hasActiveBar ? currentBar.SellVolume.ToString(CultureInfo.InvariantCulture) : "0",
				hasActiveBar ? currentBar.UnknownVolume.ToString(CultureInfo.InvariantCulture) : "0",
				hasActiveBar ? (currentBar.BuyVolume - currentBar.SellVolume).ToString(CultureInfo.InvariantCulture) : "0",
				mostRecentFinalizedBarIndex,
				mostRecentCaptureRatio,
				pendingEvents.Count,
				FormatTime(oldestPending),
				FormatTime(newestPending),
				hasActiveBar ? FormatTime(currentBar.BarOpenTime) : string.Empty,
				hasActiveBar ? FormatTime(currentBar.BarCloseTime) : string.Empty,
				FormatTime(lastAssignedEventTime),
				unmatchedEventsObserved,
				captureIntegrityCompromised ? "Compromised" : "OK",
				lastTimestampRelation,
				lastPendingDisposition,
				exportFolder);
			Draw.TextFixed(this, "xPvaOrderFlowCoreDiagnostics", text, TextPosition.TopLeft);
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

		private string FormatTime(DateTime time)
		{
			return time == DateTime.MinValue ? string.Empty : time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		}

		private string FormatDouble(double value)
		{
			return value.ToString("0.########", CultureInfo.InvariantCulture);
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

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Use previous side for between bid/ask", GroupName = "Classification", Order = 1)]
		public bool UsePreviousSideForBetweenBidAsk { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export bar summaries", GroupName = "Export", Order = 2)]
		public bool ExportBarSummaries { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export price levels", GroupName = "Export", Order = 3)]
		public bool ExportPriceLevels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export raw events", GroupName = "Export", Order = 4)]
		public bool ExportRawEvents { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export partial on terminate", GroupName = "Export", Order = 5)]
		public bool ExportPartialOnTerminate { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flush on bar finalization", GroupName = "Export", Order = 6)]
		public bool FlushOnBarFinalization { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export snapshot now", Description = "Set true to flush open CSV writers without finalizing the active bar. The indicator resets this to false after the request is handled.", GroupName = "Export", Order = 7)]
		public bool ExportSnapshotNow
		{
			get { return exportSnapshotNow; }
			set
			{
				exportSnapshotNow = value;
				if (value)
					snapshotFlushRequested = true;
			}
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Flush every N rows", GroupName = "Export", Order = 8)]
		public int FlushEveryNRows { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max bars retained", GroupName = "Memory", Order = 9)]
		public int MaxBarsRetained { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max pending events", GroupName = "Integrity", Order = 10)]
		public int MaxPendingEvents { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Allowed volume difference contracts", GroupName = "Integrity", Order = 11)]
		public long AllowedVolumeDifferenceContracts { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "Allowed volume difference percent", GroupName = "Integrity", Order = 12)]
		public double AllowedVolumeDifferencePercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 13)]
		public bool ShowDiagnosticsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Registry instance key", GroupName = "Registry", Order = 14)]
		public string RegistryInstanceKey { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public string ExportFolder { get { return exportFolder; } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaOrderFlow.xPvaOrderFlowCore[] cachexPvaOrderFlowCore;
		public xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			return xPvaOrderFlowCore(Input, usePreviousSideForBetweenBidAsk, exportBarSummaries, exportPriceLevels, exportRawEvents, exportPartialOnTerminate, flushOnBarFinalization, exportSnapshotNow, flushEveryNRows, maxBarsRetained, maxPendingEvents, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent, showDiagnosticsPanel, registryInstanceKey);
		}

		public xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(ISeries<double> input, bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			if (cachexPvaOrderFlowCore != null)
				for (int idx = 0; idx < cachexPvaOrderFlowCore.Length; idx++)
					if (cachexPvaOrderFlowCore[idx] != null && cachexPvaOrderFlowCore[idx].UsePreviousSideForBetweenBidAsk == usePreviousSideForBetweenBidAsk && cachexPvaOrderFlowCore[idx].ExportBarSummaries == exportBarSummaries && cachexPvaOrderFlowCore[idx].ExportPriceLevels == exportPriceLevels && cachexPvaOrderFlowCore[idx].ExportRawEvents == exportRawEvents && cachexPvaOrderFlowCore[idx].ExportPartialOnTerminate == exportPartialOnTerminate && cachexPvaOrderFlowCore[idx].FlushOnBarFinalization == flushOnBarFinalization && cachexPvaOrderFlowCore[idx].ExportSnapshotNow == exportSnapshotNow && cachexPvaOrderFlowCore[idx].FlushEveryNRows == flushEveryNRows && cachexPvaOrderFlowCore[idx].MaxBarsRetained == maxBarsRetained && cachexPvaOrderFlowCore[idx].MaxPendingEvents == maxPendingEvents && cachexPvaOrderFlowCore[idx].AllowedVolumeDifferenceContracts == allowedVolumeDifferenceContracts && cachexPvaOrderFlowCore[idx].AllowedVolumeDifferencePercent == allowedVolumeDifferencePercent && cachexPvaOrderFlowCore[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexPvaOrderFlowCore[idx].RegistryInstanceKey == registryInstanceKey && cachexPvaOrderFlowCore[idx].EqualsInput(input))
						return cachexPvaOrderFlowCore[idx];
			return CacheIndicator<xPvaOrderFlow.xPvaOrderFlowCore>(new xPvaOrderFlow.xPvaOrderFlowCore(){ UsePreviousSideForBetweenBidAsk = usePreviousSideForBetweenBidAsk, ExportBarSummaries = exportBarSummaries, ExportPriceLevels = exportPriceLevels, ExportRawEvents = exportRawEvents, ExportPartialOnTerminate = exportPartialOnTerminate, FlushOnBarFinalization = flushOnBarFinalization, ExportSnapshotNow = exportSnapshotNow, FlushEveryNRows = flushEveryNRows, MaxBarsRetained = maxBarsRetained, MaxPendingEvents = maxPendingEvents, AllowedVolumeDifferenceContracts = allowedVolumeDifferenceContracts, AllowedVolumeDifferencePercent = allowedVolumeDifferencePercent, ShowDiagnosticsPanel = showDiagnosticsPanel, RegistryInstanceKey = registryInstanceKey }, input, ref cachexPvaOrderFlowCore);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowCore(Input, usePreviousSideForBetweenBidAsk, exportBarSummaries, exportPriceLevels, exportRawEvents, exportPartialOnTerminate, flushOnBarFinalization, exportSnapshotNow, flushEveryNRows, maxBarsRetained, maxPendingEvents, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent, showDiagnosticsPanel, registryInstanceKey);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(ISeries<double> input , bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowCore(input, usePreviousSideForBetweenBidAsk, exportBarSummaries, exportPriceLevels, exportRawEvents, exportPartialOnTerminate, flushOnBarFinalization, exportSnapshotNow, flushEveryNRows, maxBarsRetained, maxPendingEvents, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent, showDiagnosticsPanel, registryInstanceKey);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowCore(Input, usePreviousSideForBetweenBidAsk, exportBarSummaries, exportPriceLevels, exportRawEvents, exportPartialOnTerminate, flushOnBarFinalization, exportSnapshotNow, flushEveryNRows, maxBarsRetained, maxPendingEvents, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent, showDiagnosticsPanel, registryInstanceKey);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowCore xPvaOrderFlowCore(ISeries<double> input , bool usePreviousSideForBetweenBidAsk, bool exportBarSummaries, bool exportPriceLevels, bool exportRawEvents, bool exportPartialOnTerminate, bool flushOnBarFinalization, bool exportSnapshotNow, int flushEveryNRows, int maxBarsRetained, int maxPendingEvents, long allowedVolumeDifferenceContracts, double allowedVolumeDifferencePercent, bool showDiagnosticsPanel, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowCore(input, usePreviousSideForBetweenBidAsk, exportBarSummaries, exportPriceLevels, exportRawEvents, exportPartialOnTerminate, flushOnBarFinalization, exportSnapshotNow, flushEveryNRows, maxBarsRetained, maxPendingEvents, allowedVolumeDifferenceContracts, allowedVolumeDifferencePercent, showDiagnosticsPanel, registryInstanceKey);
		}
	}
}

#endregion
