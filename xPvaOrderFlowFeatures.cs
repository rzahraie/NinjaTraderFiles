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
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow
{
	public enum xFeatureExclusionReason
	{
		None,
		PartialSourceBar,
		InvalidSourceBar,
		HistoricalWithoutOrderFlow,
		ZeroVolumeNoTrades,
		IncompleteCaptureBar
	}

	public sealed class xOrderFlowBarFeatures
	{
		public int BarIndex { get; set; }
		public DateTime NinjaBarTime { get; set; }
		public DateTime BarOpenTime { get; set; }
		public DateTime BarCloseTime { get; set; }
		public bool SourceBarAvailable { get; set; }
		public bool SourceBarValidationPassed { get; set; }
		public bool SourceBarIsPartial { get; set; }
		public bool SourceBarIsRealtimeCaptured { get; set; }
		public bool SourceCaptureCompletenessPassed { get; set; }
		public xFeatureExclusionReason ExclusionReason { get; set; }
		public double Open { get; set; }
		public double High { get; set; }
		public double Low { get; set; }
		public double Close { get; set; }
		public long Volume { get; set; }
		public long BuyVolume { get; set; }
		public long SellVolume { get; set; }
		public long UnknownVolume { get; set; }
		public long Delta { get; set; }
		public int RangeTicks { get; set; }
		public int BodyTicks { get; set; }
		public int UpperWickTicks { get; set; }
		public int LowerWickTicks { get; set; }
		public double CloseLocation { get; set; }
		public double BodyToRangeRatio { get; set; }
		public double BuyVolumePercent { get; set; }
		public double SellVolumePercent { get; set; }
		public double DeltaPercent { get; set; }
		public double AbsoluteDeltaPercent { get; set; }
		public double VolumePerRangeTick { get; set; }
		public double AbsoluteDeltaPerRangeTick { get; set; }
		public double NetPriceChangeTicks { get; set; }
		public double PriceChangePerThousandVolume { get; set; }
		public double PriceChangePerThousandAbsDelta { get; set; }
		public double PocPrice { get; set; }
		public double PocLocation { get; set; }
		public long PocVolume { get; set; }
		public double PocVolumePercent { get; set; }
		public int CloseToPocTicks { get; set; }
		public double MaxBuyVolumePrice { get; set; }
		public double MaxSellVolumePrice { get; set; }
		public double MaxPositiveDeltaPrice { get; set; }
		public double MaxNegativeDeltaPrice { get; set; }
		public int PriceLevelsTraded { get; set; }
		public int ActivePriceLevels { get; set; }
		public double VolumeConcentrationTop1Percent { get; set; }
		public double VolumeConcentrationTop3Percent { get; set; }
		public double VolumeConcentrationTop5Percent { get; set; }
		public int BuyImbalanceCount { get; set; }
		public int SellImbalanceCount { get; set; }
		public int MaxStackedBuyImbalances { get; set; }
		public int MaxStackedSellImbalances { get; set; }
		public long BuyImbalanceVolume { get; set; }
		public long SellImbalanceVolume { get; set; }
		public double BuyImbalanceVolumePercent { get; set; }
		public double SellImbalanceVolumePercent { get; set; }
		public long HighPriceTotalVolume { get; set; }
		public long HighPriceBuyVolume { get; set; }
		public long HighPriceSellVolume { get; set; }
		public long LowPriceTotalVolume { get; set; }
		public long LowPriceBuyVolume { get; set; }
		public long LowPriceSellVolume { get; set; }
		public bool ZeroSellVolumeAtHigh { get; set; }
		public bool ZeroBuyVolumeAtLow { get; set; }
		public double VolumeZScore { get; set; }
		public double RangeZScore { get; set; }
		public double AbsoluteDeltaZScore { get; set; }
		public double VolumePerRangeTickZScore { get; set; }
		public bool RollingStatisticsReady { get; set; }
		public bool ValidationPassed { get; set; }
		public string ValidationMessage { get; set; }
	}

	public class xPvaOrderFlowFeatures : Indicator
	{
		private sealed class RollingSample
		{
			public double Volume;
			public double RangeTicks;
			public double AbsoluteDelta;
			public double VolumePerRangeTick;
		}

		private readonly Dictionary<int, xOrderFlowBarFeatures> featuresByBarIndex = new Dictionary<int, xOrderFlowBarFeatures>();
		private readonly Queue<int> featureOrder = new Queue<int>();
		private readonly Queue<RollingSample> rollingWindow = new Queue<RollingSample>();
		private readonly object writerLock = new object();
		private StreamWriter featureWriter;
		private string exportFolder;
		private string fileInstrument;
		private string fileStamp;
		private int lastProcessedBarIndex;
		private int mostRecentFeatureBarIndex;
		private xOrderFlowBarFeatures mostRecentFeatures;
		private DateTime lastPanelRefresh;
		private xOrderFlowRegistryKey registryKey;
		private int nextIndexToProcess;
		private bool registryInitialized;
		private bool lastActivePublisherState;
		private bool lastAmbiguousState;
		private bool printedNoPublisher;
		private bool printedPublisherFound;
		private int oldestPublishedIndex = -1;
		private int newestPublishedIndex = -1;
		private int barsRetrieved;
		private int barsProcessed;
		private int registryMisses;
		private int lastRegistryMissIndex = -1;
		private xOrderFlowFeatureRegistryKey featureRegistryKey;
		private Guid featureRegistryPublisherId;
		private bool featureRegistryRegistered;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaOrderFlowFeatures";
				Description = "Objective feature extraction layer for finalized xPvaOrderFlowCore bars.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				ImbalanceRatio = 3.0;
				MinimumImbalanceVolume = 20;
				MinimumOpposingVolume = 1;
				UseZeroOpposingVolumeRule = true;
				StackedImbalanceMinimumLevels = 3;
				RollingLookback = 50;
				ExportFeaturesCsv = true;
				FlushOnBarFinalization = true;
				MaxBarsRetained = 2000;
				ShowDiagnosticsPanel = true;
				IncludePartialBars = false;
				IncludeInvalidCoreBars = false;
				IncludeZeroVolumeBars = false;
				IncludeIncompleteCaptureBars = false;
				RegistryInstanceKey = "APVA";
			}
			else if (State == State.DataLoaded)
			{
				lastProcessedBarIndex = -1;
				mostRecentFeatureBarIndex = -1;
				nextIndexToProcess = -1;
				featureRegistryPublisherId = Guid.NewGuid();
				registryKey = BuildRegistryKey();
				featureRegistryKey = BuildFeatureRegistryKey();
				RegisterFeatureRegistryPublisher();
				EnsureExportFolder();
			}
			else if (State == State.Realtime)
			{
				OpenWriter();
			}
			else if (State == State.Terminated)
			{
				if (featureRegistryRegistered)
					xPvaOrderFlowFeatureRegistry.UnregisterPublisher(featureRegistryKey, featureRegistryPublisherId);
				CloseWriter();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 1)
				return;

			ProcessAvailableFinalizedBars();
			UpdateDiagnosticsPanel(false);
		}

		public bool TryGetFeatures(int absoluteBarIndex, out xOrderFlowBarFeatures features)
		{
			return featuresByBarIndex.TryGetValue(absoluteBarIndex, out features);
		}

		public xOrderFlowBarFeatures GetMostRecentFeatures()
		{
			return mostRecentFeatures;
		}

		private void ProcessAvailableFinalizedBars()
		{
			bool activePublisher = xPvaOrderFlowRegistry.HasActivePublisher(registryKey);
			bool ambiguous = xPvaOrderFlowRegistry.IsAmbiguous(registryKey);
			lastActivePublisherState = activePublisher;
			lastAmbiguousState = ambiguous;

			if (ambiguous)
			{
				if (!printedNoPublisher)
				{
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures WARNING: publisher ambiguity detected for registry key {0}. Feature rows will not be consumed until the key is unambiguous.", registryKey));
					printedNoPublisher = true;
				}
				return;
			}

			if (!activePublisher)
			{
				if (!printedNoPublisher)
				{
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures: No active xPvaOrderFlowCore publisher found for registry key {0}", registryKey));
					printedNoPublisher = true;
					printedPublisherFound = false;
				}
				return;
			}

			if (!printedPublisherFound)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures: Active core publisher found for registry key {0}", registryKey));
				printedPublisherFound = true;
				printedNoPublisher = false;
			}

			if (!xPvaOrderFlowRegistry.TryGetPublishedIndexRange(registryKey, out oldestPublishedIndex, out newestPublishedIndex))
				return;

			if (!registryInitialized)
			{
				nextIndexToProcess = oldestPublishedIndex;
				registryInitialized = true;
			}
			else if (nextIndexToProcess < oldestPublishedIndex)
			{
				nextIndexToProcess = oldestPublishedIndex;
			}

			int latestEligibleIndex = CurrentBar - 1;
			int latestPublishedIndex = xPvaOrderFlowRegistry.GetMostRecentPublishedIndex(registryKey);
			int upper = Math.Min(latestEligibleIndex, latestPublishedIndex);

			for (int index = nextIndexToProcess; index <= upper; index++)
			{
				xOrderFlowBar source;
				if (!xPvaOrderFlowRegistry.TryGetBar(registryKey, index, out source))
				{
					registryMisses++;
					lastRegistryMissIndex = index;
					break;
				}

				barsRetrieved++;
				if (!ShouldProcessSource(source))
				{
					lastProcessedBarIndex = index;
					nextIndexToProcess = index + 1;
					continue;
				}

				ProcessSourceBar(source);
				nextIndexToProcess = index + 1;
			}
		}

		private void ProcessSourceBar(xOrderFlowBar source)
		{
			if (IsZeroVolumeNoTradeBar(source) && !IncludeZeroVolumeBars)
			{
				lastProcessedBarIndex = source.BarIndex;
				return;
			}

			xOrderFlowBarFeatures features = CalculateFeatures(source);
			if (!features.ValidationPassed)
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures validation failed: BarIndex={0}, Time={1}, {2}", features.BarIndex, FormatTime(features.NinjaBarTime), features.ValidationMessage));

			StoreFeatures(features);
			if (features.ValidationPassed && features.ExclusionReason == xFeatureExclusionReason.None && featureRegistryRegistered)
				xPvaOrderFlowFeatureRegistry.Publish(featureRegistryKey, featureRegistryPublisherId, features);
			WriteFeatures(features);
			if (FlushOnBarFinalization)
				FlushWriter();

			barsProcessed++;
			lastProcessedBarIndex = source.BarIndex;
		}

		private bool ShouldProcessSource(xOrderFlowBar source)
		{
			if (source == null)
				return false;

			if (!source.IsRealtimeCaptured)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures skipped BarIndex={0}: source bar was not realtime captured.", source.BarIndex));
				return false;
			}

			if (source.IsPartialBar && !IncludePartialBars)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures skipped BarIndex={0}: partial source bar.", source.BarIndex));
				return false;
			}

			if (!source.ValidationPassed && !IncludeInvalidCoreBars)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures skipped BarIndex={0}: source validation failed: {1}", source.BarIndex, source.ValidationMessage));
				return false;
			}

			if (!source.CaptureCompletenessPassed && !IncludeIncompleteCaptureBars)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures skipped BarIndex={0}: incomplete source capture. Captured={1}, NinjaTraderVolume={2}, Difference={3}, Percent={4:0.######}",
					source.BarIndex,
					source.CapturedVolume,
					source.NinjaTraderVolume,
					source.VolumeDifference,
					source.VolumeDifferencePercent));
				return false;
			}

			return true;
		}

		private xOrderFlowBarFeatures CalculateFeatures(xOrderFlowBar source)
		{
			xOrderFlowBarFeatures f = new xOrderFlowBarFeatures();
			f.BarIndex = source.BarIndex;
			f.NinjaBarTime = source.NinjaBarTime;
			f.BarOpenTime = source.BarOpenTime;
			f.BarCloseTime = source.BarCloseTime;
			f.SourceBarAvailable = true;
			f.SourceBarValidationPassed = source.ValidationPassed;
			f.SourceBarIsPartial = source.IsPartialBar;
			f.SourceBarIsRealtimeCaptured = source.IsRealtimeCaptured;
			f.SourceCaptureCompletenessPassed = source.CaptureCompletenessPassed;
			f.ExclusionReason = xFeatureExclusionReason.None;
			f.Open = source.Open;
			f.High = source.High;
			f.Low = source.Low;
			f.Close = source.Close;
			f.Volume = source.CapturedVolume;
			f.BuyVolume = source.BuyVolume;
			f.SellVolume = source.SellVolume;
			f.UnknownVolume = source.UnknownVolume;
			f.Delta = source.Delta;
			f.PocPrice = source.PocPrice;
			f.PocVolume = source.PocVolume;
			f.MaxBuyVolumePrice = source.MaxBuyVolumePrice;
			f.MaxSellVolumePrice = source.MaxSellVolumePrice;
			f.MaxPositiveDeltaPrice = source.MaxPositiveDeltaPrice;
			f.MaxNegativeDeltaPrice = source.MaxNegativeDeltaPrice;

			if (IsZeroVolumeNoTradeBar(source))
			{
				CalculateZeroVolumeNoTradeFeatures(f);
				SanitizeFeatureValues(f);
				return f;
			}

			if (!source.CaptureCompletenessPassed)
				f.ExclusionReason = xFeatureExclusionReason.IncompleteCaptureBar;

			CalculatePriceFeatures(f);
			CalculateOrderFlowNormalization(f);
			CalculatePocFeatures(f);
			CalculatePriceLevelFeatures(source, f);
			ValidateFeatures(f, source);
			SanitizeFeatureValues(f);
			if (f.ValidationPassed && f.ExclusionReason == xFeatureExclusionReason.None)
				ApplyRollingStatistics(f);
			return f;
		}

		private void CalculateZeroVolumeNoTradeFeatures(xOrderFlowBarFeatures f)
		{
			f.ExclusionReason = xFeatureExclusionReason.ZeroVolumeNoTrades;
			CalculatePriceFeatures(f);
			f.BuyVolumePercent = 0;
			f.SellVolumePercent = 0;
			f.DeltaPercent = 0;
			f.AbsoluteDeltaPercent = 0;
			f.VolumePerRangeTick = 0;
			f.AbsoluteDeltaPerRangeTick = 0;
			f.PriceChangePerThousandVolume = 0;
			f.PriceChangePerThousandAbsDelta = 0;
			f.PocPrice = 0;
			f.PocLocation = 0;
			f.PocVolume = 0;
			f.PocVolumePercent = 0;
			f.CloseToPocTicks = 0;
			f.MaxBuyVolumePrice = 0;
			f.MaxSellVolumePrice = 0;
			f.MaxPositiveDeltaPrice = 0;
			f.MaxNegativeDeltaPrice = 0;
			f.PriceLevelsTraded = 0;
			f.ActivePriceLevels = 0;
			f.RollingStatisticsReady = false;
			f.ValidationPassed = true;
			f.ValidationMessage = "Zero-volume no-trade bar";
		}

		private void CalculatePriceFeatures(xOrderFlowBarFeatures f)
		{
			f.RangeTicks = Math.Max(0, PriceDifferenceToTicks(f.High - f.Low));
			f.BodyTicks = Math.Max(0, PriceDifferenceToTicks(Math.Abs(f.Close - f.Open)));
			f.UpperWickTicks = Math.Max(0, PriceDifferenceToTicks(f.High - Math.Max(f.Open, f.Close)));
			f.LowerWickTicks = Math.Max(0, PriceDifferenceToTicks(Math.Min(f.Open, f.Close) - f.Low));
			f.CloseLocation = f.High > f.Low ? Clamp01((f.Close - f.Low) / (f.High - f.Low)) : 0.5;
			f.BodyToRangeRatio = f.RangeTicks > 0 ? SafeDivide(f.BodyTicks, f.RangeTicks) : 0;
			f.NetPriceChangeTicks = TickSize > 0 ? (f.Close - f.Open) / TickSize : 0;
		}

		private void CalculateOrderFlowNormalization(xOrderFlowBarFeatures f)
		{
			long absDelta = Math.Abs(f.Delta);
			int rangeDenominator = Math.Max(1, f.RangeTicks);
			f.BuyVolumePercent = SafeDivide(f.BuyVolume, f.Volume);
			f.SellVolumePercent = SafeDivide(f.SellVolume, f.Volume);
			f.DeltaPercent = SafeDivide(f.Delta, f.Volume);
			f.AbsoluteDeltaPercent = SafeDivide(absDelta, f.Volume);
			f.VolumePerRangeTick = SafeDivide(f.Volume, rangeDenominator);
			f.AbsoluteDeltaPerRangeTick = SafeDivide(absDelta, rangeDenominator);
			f.PriceChangePerThousandVolume = f.Volume != 0 ? f.NetPriceChangeTicks * 1000.0 / f.Volume : 0;
			f.PriceChangePerThousandAbsDelta = absDelta != 0 ? f.NetPriceChangeTicks * 1000.0 / absDelta : 0;
		}

		private void CalculatePocFeatures(xOrderFlowBarFeatures f)
		{
			f.PocLocation = f.High > f.Low ? Clamp01((f.PocPrice - f.Low) / (f.High - f.Low)) : 0.5;
			f.PocVolumePercent = SafeDivide(f.PocVolume, f.Volume);
			f.CloseToPocTicks = PriceDifferenceToTicks(f.Close - f.PocPrice);
		}

		private void CalculatePriceLevelFeatures(xOrderFlowBar source, xOrderFlowBarFeatures f)
		{
			if (source.PriceLevels == null)
				return;

			f.PriceLevelsTraded = source.PriceLevels.Count;
			List<xPriceLevelOrderFlow> active = new List<xPriceLevelOrderFlow>();
			foreach (KeyValuePair<long, xPriceLevelOrderFlow> pair in source.PriceLevels)
				if (pair.Value != null && pair.Value.TotalVolume > 0)
					active.Add(pair.Value);

			f.ActivePriceLevels = active.Count;
			active.Sort((a, b) => b.TotalVolume.CompareTo(a.TotalVolume));
			f.VolumeConcentrationTop1Percent = Concentration(active, f.Volume, 1);
			f.VolumeConcentrationTop3Percent = Concentration(active, f.Volume, 3);
			f.VolumeConcentrationTop5Percent = Concentration(active, f.Volume, 5);

			CalculateImbalances(source, f);
			CalculateExtremePriceStats(source, f);
		}

		private void CalculateImbalances(xOrderFlowBar source, xOrderFlowBarFeatures f)
		{
			if (source.PriceLevels == null || source.PriceLevels.Count == 0)
				return;

			List<long> buyImbalanceTicks = new List<long>();
			List<long> sellImbalanceTicks = new List<long>();
			List<long> keys = new List<long>(source.PriceLevels.Keys);
			keys.Sort();

			foreach (long key in keys)
			{
				xPriceLevelOrderFlow level = source.PriceLevels[key];
				xPriceLevelOrderFlow opposingBelow;
				long opposingSell = source.PriceLevels.TryGetValue(key - 1, out opposingBelow) && opposingBelow != null ? opposingBelow.SellVolume : 0;
				if (IsImbalance(level.BuyVolume, opposingSell))
				{
					f.BuyImbalanceCount++;
					f.BuyImbalanceVolume += level.BuyVolume;
					buyImbalanceTicks.Add(key);
				}

				xPriceLevelOrderFlow opposingAbove;
				long opposingBuy = source.PriceLevels.TryGetValue(key + 1, out opposingAbove) && opposingAbove != null ? opposingAbove.BuyVolume : 0;
				if (IsImbalance(level.SellVolume, opposingBuy))
				{
					f.SellImbalanceCount++;
					f.SellImbalanceVolume += level.SellVolume;
					sellImbalanceTicks.Add(key);
				}
			}

			f.BuyImbalanceVolumePercent = SafeDivide(f.BuyImbalanceVolume, f.Volume);
			f.SellImbalanceVolumePercent = SafeDivide(f.SellImbalanceVolume, f.Volume);
			f.MaxStackedBuyImbalances = MaxConsecutiveRun(buyImbalanceTicks);
			f.MaxStackedSellImbalances = MaxConsecutiveRun(sellImbalanceTicks);
		}

		private bool IsImbalance(long sameSideVolume, long opposingVolume)
		{
			if (sameSideVolume < MinimumImbalanceVolume)
				return false;

			if (opposingVolume == 0)
				return UseZeroOpposingVolumeRule;

			if (opposingVolume < MinimumOpposingVolume)
				return false;

			return (double)sameSideVolume / opposingVolume >= ImbalanceRatio;
		}

		private void CalculateExtremePriceStats(xOrderFlowBar source, xOrderFlowBarFeatures f)
		{
			long highTicks = PriceToTicks(source.High);
			long lowTicks = PriceToTicks(source.Low);
			xPriceLevelOrderFlow highLevel;
			if (source.PriceLevels != null && source.PriceLevels.TryGetValue(highTicks, out highLevel) && highLevel != null)
			{
				f.HighPriceTotalVolume = highLevel.TotalVolume;
				f.HighPriceBuyVolume = highLevel.BuyVolume;
				f.HighPriceSellVolume = highLevel.SellVolume;
			}
			xPriceLevelOrderFlow lowLevel;
			if (source.PriceLevels != null && source.PriceLevels.TryGetValue(lowTicks, out lowLevel) && lowLevel != null)
			{
				f.LowPriceTotalVolume = lowLevel.TotalVolume;
				f.LowPriceBuyVolume = lowLevel.BuyVolume;
				f.LowPriceSellVolume = lowLevel.SellVolume;
			}
			f.ZeroSellVolumeAtHigh = f.HighPriceSellVolume == 0;
			f.ZeroBuyVolumeAtLow = f.LowPriceBuyVolume == 0;
		}

		private void ApplyRollingStatistics(xOrderFlowBarFeatures f)
		{
			if (rollingWindow.Count >= RollingLookback)
			{
				f.RollingStatisticsReady = true;
				f.VolumeZScore = ZScore(f.Volume, RollingMetric.Volume);
				f.RangeZScore = ZScore(f.RangeTicks, RollingMetric.RangeTicks);
				f.AbsoluteDeltaZScore = ZScore(Math.Abs(f.Delta), RollingMetric.AbsoluteDelta);
				f.VolumePerRangeTickZScore = ZScore(f.VolumePerRangeTick, RollingMetric.VolumePerRangeTick);
			}

			rollingWindow.Enqueue(new RollingSample
			{
				Volume = f.Volume,
				RangeTicks = f.RangeTicks,
				AbsoluteDelta = Math.Abs(f.Delta),
				VolumePerRangeTick = f.VolumePerRangeTick
			});
			while (rollingWindow.Count > RollingLookback)
				rollingWindow.Dequeue();
		}

		private enum RollingMetric
		{
			Volume,
			RangeTicks,
			AbsoluteDelta,
			VolumePerRangeTick
		}

		private double ZScore(double value, RollingMetric metric)
		{
			if (rollingWindow.Count == 0)
				return 0;

			double sum = 0;
			foreach (RollingSample sample in rollingWindow)
				sum += RollingValue(sample, metric);
			double mean = sum / rollingWindow.Count;

			double variance = 0;
			foreach (RollingSample sample in rollingWindow)
			{
				double diff = RollingValue(sample, metric) - mean;
				variance += diff * diff;
			}
			variance /= rollingWindow.Count; // Population standard deviation over the prior valid-bar baseline.
			double stdDev = Math.Sqrt(variance);
			return stdDev > 0 ? (value - mean) / stdDev : 0;
		}

		private double RollingValue(RollingSample sample, RollingMetric metric)
		{
			if (metric == RollingMetric.Volume)
				return sample.Volume;
			if (metric == RollingMetric.RangeTicks)
				return sample.RangeTicks;
			if (metric == RollingMetric.AbsoluteDelta)
				return sample.AbsoluteDelta;
			return sample.VolumePerRangeTick;
		}

		private void ValidateFeatures(xOrderFlowBarFeatures f, xOrderFlowBar source)
		{
			if (f.ExclusionReason == xFeatureExclusionReason.ZeroVolumeNoTrades)
			{
				f.RollingStatisticsReady = false;
				f.ValidationPassed = true;
				f.ValidationMessage = "Zero-volume no-trade bar";
				return;
			}

			StringBuilder message = new StringBuilder();
			if (f.BuyVolume + f.SellVolume + f.UnknownVolume != f.Volume)
				message.Append("side volume sum mismatch; ");
			if (!Between(f.CloseLocation, 0, 1))
				message.Append("close location out of range; ");
			if (!Between(f.PocLocation, 0, 1))
				message.Append("poc location out of range; ");
			if (f.BodyToRangeRatio < -0.000001 || f.BodyToRangeRatio > 1.000001)
				message.Append("body/range ratio out of range; ");
			if (f.VolumeConcentrationTop1Percent - f.VolumeConcentrationTop3Percent > 0.000001 || f.VolumeConcentrationTop3Percent - f.VolumeConcentrationTop5Percent > 0.000001 || f.VolumeConcentrationTop5Percent > 1.000001)
				message.Append("volume concentration ordering invalid; ");
			if (f.BuyImbalanceCount > f.ActivePriceLevels || f.SellImbalanceCount > f.ActivePriceLevels)
				message.Append("imbalance count exceeds active levels; ");
			if (source.PriceLevels != null)
			{
				if (!source.PriceLevels.ContainsKey(PriceToTicks(source.High)))
					message.Append("high price level missing; ");
				if (!source.PriceLevels.ContainsKey(PriceToTicks(source.Low)))
					message.Append("low price level missing; ");
			}
			if (!AllFinite(f))
				message.Append("non-finite numeric value; ");

			f.ValidationPassed = message.Length == 0;
			f.ValidationMessage = f.ValidationPassed ? "OK" : message.ToString();
		}

		private bool IsZeroVolumeNoTradeBar(xOrderFlowBar source)
		{
			return source != null
				&& source.NinjaTraderVolume == 0
				&& source.CapturedVolume == 0
				&& (source.PriceLevels == null || source.PriceLevels.Count == 0);
		}

		private void StoreFeatures(xOrderFlowBarFeatures features)
		{
			featuresByBarIndex[features.BarIndex] = features;
			featureOrder.Enqueue(features.BarIndex);
			mostRecentFeatureBarIndex = features.BarIndex;
			mostRecentFeatures = features;
			while (featureOrder.Count > MaxBarsRetained)
			{
				int removeIndex = featureOrder.Dequeue();
				if (removeIndex != features.BarIndex)
					featuresByBarIndex.Remove(removeIndex);
			}
		}

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xPvaOrderFlowFeatures");
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
				Instrument != null ? Instrument.FullName : string.Empty,
				masterInstrumentName,
				BarsPeriod != null ? BarsPeriod.BarsPeriodType : BarsPeriodType.Minute,
				BarsPeriod != null ? BarsPeriod.Value : 0,
				value2,
				tradingHoursName,
				string.Empty,
				RegistryInstanceKey);
		}

		private xOrderFlowFeatureRegistryKey BuildFeatureRegistryKey()
		{
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

			return new xOrderFlowFeatureRegistryKey(
				Instrument != null ? Instrument.FullName : string.Empty,
				BarsPeriod != null ? BarsPeriod.BarsPeriodType : BarsPeriodType.Minute,
				BarsPeriod != null ? BarsPeriod.Value : 0,
				value2,
				tradingHoursName,
				RegistryInstanceKey);
		}

		private void RegisterFeatureRegistryPublisher()
		{
			xOrderFlowFeaturePublisherRegistrationResult result = xPvaOrderFlowFeatureRegistry.RegisterPublisher(featureRegistryKey, featureRegistryPublisherId);
			featureRegistryRegistered = result == xOrderFlowFeaturePublisherRegistrationResult.Registered || result == xOrderFlowFeaturePublisherRegistrationResult.Refreshed;

			if (featureRegistryRegistered)
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures registered feature publisher {0} for registry key {1}", featureRegistryPublisherId, featureRegistryKey));
			else
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures WARNING: duplicate/ambiguous feature publisher for registry key {0}. This feature instance will not publish mixed data.", featureRegistryKey));
		}

		private void OpenWriter()
		{
			if (!ExportFeaturesCsv)
				return;
			try
			{
				featureWriter = new StreamWriter(Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowFeatures_BARS_{0}_{1}.csv", fileInstrument, fileStamp)), false, Encoding.UTF8);
				featureWriter.WriteLine("BarIndex,NinjaBarTime,BarOpenTime,BarCloseTime,SourceBarAvailable,SourceBarValidationPassed,SourceBarIsPartial,SourceBarIsRealtimeCaptured,ExclusionReason,Open,High,Low,Close,Volume,BuyVolume,SellVolume,UnknownVolume,Delta,RangeTicks,BodyTicks,UpperWickTicks,LowerWickTicks,CloseLocation,BodyToRangeRatio,BuyVolumePercent,SellVolumePercent,DeltaPercent,AbsoluteDeltaPercent,VolumePerRangeTick,AbsoluteDeltaPerRangeTick,NetPriceChangeTicks,PriceChangePerThousandVolume,PriceChangePerThousandAbsDelta,PocPrice,PocLocation,PocVolume,PocVolumePercent,CloseToPocTicks,MaxBuyVolumePrice,MaxSellVolumePrice,MaxPositiveDeltaPrice,MaxNegativeDeltaPrice,PriceLevelsTraded,ActivePriceLevels,VolumeConcentrationTop1Percent,VolumeConcentrationTop3Percent,VolumeConcentrationTop5Percent,BuyImbalanceCount,SellImbalanceCount,MaxStackedBuyImbalances,MaxStackedSellImbalances,BuyImbalanceVolume,SellImbalanceVolume,BuyImbalanceVolumePercent,SellImbalanceVolumePercent,HighPriceTotalVolume,HighPriceBuyVolume,HighPriceSellVolume,LowPriceTotalVolume,LowPriceBuyVolume,LowPriceSellVolume,ZeroSellVolumeAtHigh,ZeroBuyVolumeAtLow,VolumeZScore,RangeZScore,AbsoluteDeltaZScore,VolumePerRangeTickZScore,RollingStatisticsReady,ValidationPassed,ValidationMessage,SourceCaptureCompletenessPassed,SourceExclusionReason");
				Print("xPvaOrderFlowFeatures uses population standard deviation for rolling z-score baselines, calculated from prior valid complete realtime bars only.");
			}
			catch (Exception ex)
			{
				Print("xPvaOrderFlowFeatures export open failed: " + ex.Message);
				CloseWriter();
			}
		}

		private void CloseWriter()
		{
			lock (writerLock)
			{
				try
				{
					if (featureWriter != null)
					{
						featureWriter.Flush();
						featureWriter.Close();
						featureWriter = null;
					}
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowFeatures export close failed: " + ex.Message);
				}
			}
		}

		private void WriteFeatures(xOrderFlowBarFeatures f)
		{
			if (featureWriter == null)
				return;

			lock (writerLock)
			{
				try
				{
					string[] columns =
					{
						f.BarIndex.ToString(CultureInfo.InvariantCulture), FormatTime(f.NinjaBarTime), FormatTime(f.BarOpenTime), FormatTime(f.BarCloseTime),
						f.SourceBarAvailable.ToString(), f.SourceBarValidationPassed.ToString(), f.SourceBarIsPartial.ToString(), f.SourceBarIsRealtimeCaptured.ToString(), f.ExclusionReason.ToString(),
						FormatDouble(f.Open), FormatDouble(f.High), FormatDouble(f.Low), FormatDouble(f.Close),
						f.Volume.ToString(CultureInfo.InvariantCulture), f.BuyVolume.ToString(CultureInfo.InvariantCulture), f.SellVolume.ToString(CultureInfo.InvariantCulture), f.UnknownVolume.ToString(CultureInfo.InvariantCulture), f.Delta.ToString(CultureInfo.InvariantCulture),
						f.RangeTicks.ToString(CultureInfo.InvariantCulture), f.BodyTicks.ToString(CultureInfo.InvariantCulture), f.UpperWickTicks.ToString(CultureInfo.InvariantCulture), f.LowerWickTicks.ToString(CultureInfo.InvariantCulture),
						FormatDouble(f.CloseLocation), FormatDouble(f.BodyToRangeRatio), FormatDouble(f.BuyVolumePercent), FormatDouble(f.SellVolumePercent), FormatDouble(f.DeltaPercent), FormatDouble(f.AbsoluteDeltaPercent),
						FormatDouble(f.VolumePerRangeTick), FormatDouble(f.AbsoluteDeltaPerRangeTick), FormatDouble(f.NetPriceChangeTicks), FormatDouble(f.PriceChangePerThousandVolume), FormatDouble(f.PriceChangePerThousandAbsDelta),
						FormatDouble(f.PocPrice), FormatDouble(f.PocLocation), f.PocVolume.ToString(CultureInfo.InvariantCulture), FormatDouble(f.PocVolumePercent), f.CloseToPocTicks.ToString(CultureInfo.InvariantCulture),
						FormatDouble(f.MaxBuyVolumePrice), FormatDouble(f.MaxSellVolumePrice), FormatDouble(f.MaxPositiveDeltaPrice), FormatDouble(f.MaxNegativeDeltaPrice),
						f.PriceLevelsTraded.ToString(CultureInfo.InvariantCulture), f.ActivePriceLevels.ToString(CultureInfo.InvariantCulture),
						FormatDouble(f.VolumeConcentrationTop1Percent), FormatDouble(f.VolumeConcentrationTop3Percent), FormatDouble(f.VolumeConcentrationTop5Percent),
						f.BuyImbalanceCount.ToString(CultureInfo.InvariantCulture), f.SellImbalanceCount.ToString(CultureInfo.InvariantCulture), f.MaxStackedBuyImbalances.ToString(CultureInfo.InvariantCulture), f.MaxStackedSellImbalances.ToString(CultureInfo.InvariantCulture),
						f.BuyImbalanceVolume.ToString(CultureInfo.InvariantCulture), f.SellImbalanceVolume.ToString(CultureInfo.InvariantCulture), FormatDouble(f.BuyImbalanceVolumePercent), FormatDouble(f.SellImbalanceVolumePercent),
						f.HighPriceTotalVolume.ToString(CultureInfo.InvariantCulture), f.HighPriceBuyVolume.ToString(CultureInfo.InvariantCulture), f.HighPriceSellVolume.ToString(CultureInfo.InvariantCulture),
						f.LowPriceTotalVolume.ToString(CultureInfo.InvariantCulture), f.LowPriceBuyVolume.ToString(CultureInfo.InvariantCulture), f.LowPriceSellVolume.ToString(CultureInfo.InvariantCulture),
						f.ZeroSellVolumeAtHigh.ToString(), f.ZeroBuyVolumeAtLow.ToString(),
						FormatDouble(f.VolumeZScore), FormatDouble(f.RangeZScore), FormatDouble(f.AbsoluteDeltaZScore), FormatDouble(f.VolumePerRangeTickZScore),
						f.RollingStatisticsReady.ToString(), f.ValidationPassed.ToString(), f.ValidationMessage,
						f.SourceCaptureCompletenessPassed.ToString(), f.ExclusionReason.ToString()
					};
					featureWriter.WriteLine(ToCsv(columns));
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowFeatures export failed: " + ex.Message);
				}
			}
		}

		private void FlushWriter()
		{
			lock (writerLock)
			{
				try
				{
					if (featureWriter != null)
						featureWriter.Flush();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowFeatures flush failed: " + ex.Message);
				}
			}
		}

		private void UpdateDiagnosticsPanel(bool force)
		{
			if (!ShowDiagnosticsPanel)
				return;

			DateTime now = DateTime.Now;
			if (!force && lastPanelRefresh != DateTime.MinValue && (now - lastPanelRefresh).TotalMilliseconds < 500)
				return;
			lastPanelRefresh = now;

			xOrderFlowBarFeatures f = mostRecentFeatures;
			string text = string.Format(CultureInfo.InvariantCulture,
				"xPvaOrderFlowFeatures\nInstrument: {0}\nRegistry key: {1}\nActive publisher found: {2}\nPublisher ambiguity detected: {3}\nOldest published index: {4}\nNewest published index: {5}\nNext feature index: {6}\nBars retrieved: {7}\nBars processed: {8}\nRegistry misses: {9}\nLast registry-miss index: {10}\nLast successfully processed index: {11}\nSource captured volume: {12}\nDelta: {13}\nDelta percent: {14:0.###}\nRange ticks: {15}\nClose location: {16:0.###}\nPOC location: {17:0.###}\nVolume/range tick: {18:0.###}\nVolume z-score: {19:0.###}\nRange z-score: {20:0.###}\nAbsolute delta z-score: {21:0.###}\nBuy imbalance count: {22}\nSell imbalance count: {23}\nMax stacked buy imbalances: {24}\nMax stacked sell imbalances: {25}\nFeature validation status: {26}\nRolling statistics ready: {27}",
				Instrument != null ? Instrument.FullName : string.Empty,
				registryKey.ToString(),
				lastActivePublisherState,
				lastAmbiguousState,
				oldestPublishedIndex,
				newestPublishedIndex,
				nextIndexToProcess,
				barsRetrieved,
				barsProcessed,
				registryMisses,
				lastRegistryMissIndex,
				lastProcessedBarIndex,
				f != null ? f.Volume.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.Delta.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.DeltaPercent : 0,
				f != null ? f.RangeTicks.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.CloseLocation : 0,
				f != null ? f.PocLocation : 0,
				f != null ? f.VolumePerRangeTick : 0,
				f != null ? f.VolumeZScore : 0,
				f != null ? f.RangeZScore : 0,
				f != null ? f.AbsoluteDeltaZScore : 0,
				f != null ? f.BuyImbalanceCount.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.SellImbalanceCount.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.MaxStackedBuyImbalances.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.MaxStackedSellImbalances.ToString(CultureInfo.InvariantCulture) : "",
				f != null ? f.ValidationPassed.ToString() : "",
				f != null ? f.RollingStatisticsReady.ToString() : "");
			Draw.TextFixed(this, "xPvaOrderFlowFeaturesDiagnostics", text, TextPosition.TopRight);
		}

		private double Concentration(List<xPriceLevelOrderFlow> active, long volume, int count)
		{
			if (volume <= 0 || active.Count == 0)
				return 0;
			long sum = 0;
			int limit = Math.Min(count, active.Count);
			for (int i = 0; i < limit; i++)
				sum += active[i].TotalVolume;
			return SafeDivide(sum, volume);
		}

		private int MaxConsecutiveRun(List<long> sortedTicks)
		{
			if (sortedTicks.Count == 0)
				return 0;
			sortedTicks.Sort();
			int best = 1;
			int current = 1;
			for (int i = 1; i < sortedTicks.Count; i++)
			{
				if (sortedTicks[i] == sortedTicks[i - 1] + 1)
					current++;
				else if (sortedTicks[i] != sortedTicks[i - 1])
					current = 1;
				if (current > best)
					best = current;
			}
			return best;
		}

		private int PriceDifferenceToTicks(double priceDifference)
		{
			return TickSize > 0 ? (int)Math.Round(priceDifference / TickSize, MidpointRounding.AwayFromZero) : 0;
		}

		private long PriceToTicks(double price)
		{
			return TickSize > 0 ? (long)Math.Round(price / TickSize, MidpointRounding.AwayFromZero) : 0;
		}

		private double SafeDivide(double numerator, double denominator)
		{
			return denominator != 0 ? numerator / denominator : 0;
		}

		private double Clamp01(double value)
		{
			if (value < 0)
				return 0;
			if (value > 1)
				return 1;
			return value;
		}

		private bool Between(double value, double min, double max)
		{
			return value >= min - 0.000001 && value <= max + 0.000001;
		}

		private bool AllFinite(xOrderFlowBarFeatures f)
		{
			return IsFinite(f.Open) && IsFinite(f.High) && IsFinite(f.Low) && IsFinite(f.Close)
				&& IsFinite(f.CloseLocation) && IsFinite(f.BodyToRangeRatio)
				&& IsFinite(f.BuyVolumePercent) && IsFinite(f.SellVolumePercent) && IsFinite(f.DeltaPercent) && IsFinite(f.AbsoluteDeltaPercent)
				&& IsFinite(f.VolumePerRangeTick) && IsFinite(f.AbsoluteDeltaPerRangeTick) && IsFinite(f.NetPriceChangeTicks)
				&& IsFinite(f.PriceChangePerThousandVolume) && IsFinite(f.PriceChangePerThousandAbsDelta)
				&& IsFinite(f.PocPrice) && IsFinite(f.PocLocation) && IsFinite(f.PocVolumePercent)
				&& IsFinite(f.VolumeConcentrationTop1Percent) && IsFinite(f.VolumeConcentrationTop3Percent) && IsFinite(f.VolumeConcentrationTop5Percent)
				&& IsFinite(f.BuyImbalanceVolumePercent) && IsFinite(f.SellImbalanceVolumePercent)
				&& IsFinite(f.VolumeZScore) && IsFinite(f.RangeZScore) && IsFinite(f.AbsoluteDeltaZScore) && IsFinite(f.VolumePerRangeTickZScore);
		}

		private bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private void SanitizeFeatureValues(xOrderFlowBarFeatures f)
		{
			f.CloseLocation = Clean(f.CloseLocation);
			f.BodyToRangeRatio = Clean(f.BodyToRangeRatio);
			f.BuyVolumePercent = Clean(f.BuyVolumePercent);
			f.SellVolumePercent = Clean(f.SellVolumePercent);
			f.DeltaPercent = Clean(f.DeltaPercent);
			f.AbsoluteDeltaPercent = Clean(f.AbsoluteDeltaPercent);
			f.VolumePerRangeTick = Clean(f.VolumePerRangeTick);
			f.AbsoluteDeltaPerRangeTick = Clean(f.AbsoluteDeltaPerRangeTick);
			f.NetPriceChangeTicks = Clean(f.NetPriceChangeTicks);
			f.PriceChangePerThousandVolume = Clean(f.PriceChangePerThousandVolume);
			f.PriceChangePerThousandAbsDelta = Clean(f.PriceChangePerThousandAbsDelta);
			f.PocLocation = Clean(f.PocLocation);
			f.PocVolumePercent = Clean(f.PocVolumePercent);
			f.VolumeConcentrationTop1Percent = Clean(f.VolumeConcentrationTop1Percent);
			f.VolumeConcentrationTop3Percent = Clean(f.VolumeConcentrationTop3Percent);
			f.VolumeConcentrationTop5Percent = Clean(f.VolumeConcentrationTop5Percent);
			f.BuyImbalanceVolumePercent = Clean(f.BuyImbalanceVolumePercent);
			f.SellImbalanceVolumePercent = Clean(f.SellImbalanceVolumePercent);
			f.VolumeZScore = Clean(f.VolumeZScore);
			f.RangeZScore = Clean(f.RangeZScore);
			f.AbsoluteDeltaZScore = Clean(f.AbsoluteDeltaZScore);
			f.VolumePerRangeTickZScore = Clean(f.VolumePerRangeTickZScore);
		}

		private double Clean(double value)
		{
			return IsFinite(value) ? value : 0;
		}

		private string SanitizeFileName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "UnknownInstrument";
			char[] invalid = Path.GetInvalidFileNameChars();
			StringBuilder builder = new StringBuilder(value.Length);
			foreach (char ch in value)
				builder.Append(Array.IndexOf(invalid, ch) >= 0 || char.IsWhiteSpace(ch) || ch == '/' || ch == '\\' || ch == ':' || ch == '|' ? '_' : ch);
			return builder.ToString();
		}

		private string FormatTime(DateTime time)
		{
			return time == DateTime.MinValue ? string.Empty : time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		}

		private string FormatDouble(double value)
		{
			return Clean(value).ToString("0.########", CultureInfo.InvariantCulture);
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
		[Range(1.0, double.MaxValue)]
		[Display(Name = "Imbalance ratio", GroupName = "Imbalance", Order = 1)]
		public double ImbalanceRatio { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum imbalance volume", GroupName = "Imbalance", Order = 2)]
		public long MinimumImbalanceVolume { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum opposing volume", GroupName = "Imbalance", Order = 3)]
		public long MinimumOpposingVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use zero opposing volume rule", GroupName = "Imbalance", Order = 4)]
		public bool UseZeroOpposingVolumeRule { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Stacked imbalance minimum levels", GroupName = "Imbalance", Order = 5)]
		public int StackedImbalanceMinimumLevels { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Rolling lookback", GroupName = "Rolling", Order = 6)]
		public int RollingLookback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export features CSV", GroupName = "Export", Order = 7)]
		public bool ExportFeaturesCsv { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flush on bar finalization", GroupName = "Export", Order = 8)]
		public bool FlushOnBarFinalization { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max bars retained", GroupName = "Memory", Order = 9)]
		public int MaxBarsRetained { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 10)]
		public bool ShowDiagnosticsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Include partial bars", GroupName = "Source Filters", Order = 11)]
		public bool IncludePartialBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Include invalid core bars", GroupName = "Source Filters", Order = 12)]
		public bool IncludeInvalidCoreBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Include zero-volume bars", GroupName = "Source Filters", Order = 13)]
		public bool IncludeZeroVolumeBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Include incomplete capture bars", GroupName = "Source Filters", Order = 14)]
		public bool IncludeIncompleteCaptureBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Registry instance key", GroupName = "Registry", Order = 15)]
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
		private xPvaOrderFlow.xPvaOrderFlowFeatures[] cachexPvaOrderFlowFeatures;
		public xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			return xPvaOrderFlowFeatures(Input, imbalanceRatio, minimumImbalanceVolume, minimumOpposingVolume, useZeroOpposingVolumeRule, stackedImbalanceMinimumLevels, rollingLookback, exportFeaturesCsv, flushOnBarFinalization, maxBarsRetained, showDiagnosticsPanel, includePartialBars, includeInvalidCoreBars, includeZeroVolumeBars, registryInstanceKey);
		}

		public xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(ISeries<double> input, double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			if (cachexPvaOrderFlowFeatures != null)
				for (int idx = 0; idx < cachexPvaOrderFlowFeatures.Length; idx++)
					if (cachexPvaOrderFlowFeatures[idx] != null && cachexPvaOrderFlowFeatures[idx].ImbalanceRatio == imbalanceRatio && cachexPvaOrderFlowFeatures[idx].MinimumImbalanceVolume == minimumImbalanceVolume && cachexPvaOrderFlowFeatures[idx].MinimumOpposingVolume == minimumOpposingVolume && cachexPvaOrderFlowFeatures[idx].UseZeroOpposingVolumeRule == useZeroOpposingVolumeRule && cachexPvaOrderFlowFeatures[idx].StackedImbalanceMinimumLevels == stackedImbalanceMinimumLevels && cachexPvaOrderFlowFeatures[idx].RollingLookback == rollingLookback && cachexPvaOrderFlowFeatures[idx].ExportFeaturesCsv == exportFeaturesCsv && cachexPvaOrderFlowFeatures[idx].FlushOnBarFinalization == flushOnBarFinalization && cachexPvaOrderFlowFeatures[idx].MaxBarsRetained == maxBarsRetained && cachexPvaOrderFlowFeatures[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexPvaOrderFlowFeatures[idx].IncludePartialBars == includePartialBars && cachexPvaOrderFlowFeatures[idx].IncludeInvalidCoreBars == includeInvalidCoreBars && cachexPvaOrderFlowFeatures[idx].IncludeZeroVolumeBars == includeZeroVolumeBars && cachexPvaOrderFlowFeatures[idx].RegistryInstanceKey == registryInstanceKey && cachexPvaOrderFlowFeatures[idx].EqualsInput(input))
						return cachexPvaOrderFlowFeatures[idx];
			return CacheIndicator<xPvaOrderFlow.xPvaOrderFlowFeatures>(new xPvaOrderFlow.xPvaOrderFlowFeatures(){ ImbalanceRatio = imbalanceRatio, MinimumImbalanceVolume = minimumImbalanceVolume, MinimumOpposingVolume = minimumOpposingVolume, UseZeroOpposingVolumeRule = useZeroOpposingVolumeRule, StackedImbalanceMinimumLevels = stackedImbalanceMinimumLevels, RollingLookback = rollingLookback, ExportFeaturesCsv = exportFeaturesCsv, FlushOnBarFinalization = flushOnBarFinalization, MaxBarsRetained = maxBarsRetained, ShowDiagnosticsPanel = showDiagnosticsPanel, IncludePartialBars = includePartialBars, IncludeInvalidCoreBars = includeInvalidCoreBars, IncludeZeroVolumeBars = includeZeroVolumeBars, RegistryInstanceKey = registryInstanceKey }, input, ref cachexPvaOrderFlowFeatures);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowFeatures(Input, imbalanceRatio, minimumImbalanceVolume, minimumOpposingVolume, useZeroOpposingVolumeRule, stackedImbalanceMinimumLevels, rollingLookback, exportFeaturesCsv, flushOnBarFinalization, maxBarsRetained, showDiagnosticsPanel, includePartialBars, includeInvalidCoreBars, includeZeroVolumeBars, registryInstanceKey);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(ISeries<double> input , double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowFeatures(input, imbalanceRatio, minimumImbalanceVolume, minimumOpposingVolume, useZeroOpposingVolumeRule, stackedImbalanceMinimumLevels, rollingLookback, exportFeaturesCsv, flushOnBarFinalization, maxBarsRetained, showDiagnosticsPanel, includePartialBars, includeInvalidCoreBars, includeZeroVolumeBars, registryInstanceKey);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowFeatures(Input, imbalanceRatio, minimumImbalanceVolume, minimumOpposingVolume, useZeroOpposingVolumeRule, stackedImbalanceMinimumLevels, rollingLookback, exportFeaturesCsv, flushOnBarFinalization, maxBarsRetained, showDiagnosticsPanel, includePartialBars, includeInvalidCoreBars, includeZeroVolumeBars, registryInstanceKey);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowFeatures xPvaOrderFlowFeatures(ISeries<double> input , double imbalanceRatio, long minimumImbalanceVolume, long minimumOpposingVolume, bool useZeroOpposingVolumeRule, int stackedImbalanceMinimumLevels, int rollingLookback, bool exportFeaturesCsv, bool flushOnBarFinalization, int maxBarsRetained, bool showDiagnosticsPanel, bool includePartialBars, bool includeInvalidCoreBars, bool includeZeroVolumeBars, string registryInstanceKey)
		{
			return indicator.xPvaOrderFlowFeatures(input, imbalanceRatio, minimumImbalanceVolume, minimumOpposingVolume, useZeroOpposingVolumeRule, stackedImbalanceMinimumLevels, rollingLookback, exportFeaturesCsv, flushOnBarFinalization, maxBarsRetained, showDiagnosticsPanel, includePartialBars, includeInvalidCoreBars, includeZeroVolumeBars, registryInstanceKey);
		}
	}
}

#endregion
