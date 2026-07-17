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
	public sealed class xOrderFlowEvidence
	{
		public int EndBarIndex { get; set; }
		public int StartBarIndex { get; set; }
		public int WindowBars { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool SourceRangeComplete { get; set; }
		public bool SourceValidationPassed { get; set; }
		public double StartOpen { get; set; }
		public double EndClose { get; set; }
		public double HighestHigh { get; set; }
		public double LowestLow { get; set; }
		public int GrossRangeTicks { get; set; }
		public double NetPriceChangeTicks { get; set; }
		public double AbsoluteNetPriceChangeTicks { get; set; }
		public int UpCloseBars { get; set; }
		public int DownCloseBars { get; set; }
		public int UnchangedCloseBars { get; set; }
		public long TotalVolume { get; set; }
		public long TotalBuyVolume { get; set; }
		public long TotalSellVolume { get; set; }
		public long TotalUnknownVolume { get; set; }
		public long TotalDelta { get; set; }
		public long TotalAbsoluteBarDelta { get; set; }
		public double AggregateDeltaPercent { get; set; }
		public double AverageBarDeltaPercent { get; set; }
		public double AverageAbsoluteDeltaPercent { get; set; }
		public double VolumePerNetPriceTick { get; set; }
		public double AbsoluteDeltaPerNetPriceTick { get; set; }
		public double TotalVolumePerGrossRangeTick { get; set; }
		public double PriceProgressEfficiency { get; set; }
		public double DirectionalCloseConsistency { get; set; }
		public int PriceDirectionSign { get; set; }
		public int DeltaDirectionSign { get; set; }
		public int PocMigrationSign { get; set; }
		public bool PriceDeltaDirectionAgreement { get; set; }
		public bool PriceDeltaDirectionDisagreement { get; set; }
		public double FirstPocPrice { get; set; }
		public double LastPocPrice { get; set; }
		public double PocMigrationTicks { get; set; }
		public double AveragePocLocation { get; set; }
		public double AverageCloseLocation { get; set; }
		public double AverageCloseToPocTicks { get; set; }
		public int PositivePocMigrationSteps { get; set; }
		public int NegativePocMigrationSteps { get; set; }
		public int FlatPocMigrationSteps { get; set; }
		public int TotalBuyImbalanceCount { get; set; }
		public int TotalSellImbalanceCount { get; set; }
		public int BarsWithBuyImbalances { get; set; }
		public int BarsWithSellImbalances { get; set; }
		public int BarsWithStackedBuyImbalances { get; set; }
		public int BarsWithStackedSellImbalances { get; set; }
		public int MaxObservedBuyStack { get; set; }
		public int MaxObservedSellStack { get; set; }
		public long TotalBuyImbalanceVolume { get; set; }
		public long TotalSellImbalanceVolume { get; set; }
		public double BuyImbalanceVolumePercent { get; set; }
		public double SellImbalanceVolumePercent { get; set; }
		public double AverageVolumeConcentrationTop1Percent { get; set; }
		public double AverageVolumeConcentrationTop3Percent { get; set; }
		public double AverageVolumeConcentrationTop5Percent { get; set; }
		public double VolumeConcentrationTrend { get; set; }
		public int BarsWithZeroSellVolumeAtHigh { get; set; }
		public int BarsWithZeroBuyVolumeAtLow { get; set; }
		public double AverageVolumeZScore { get; set; }
		public double MaximumVolumeZScore { get; set; }
		public double MinimumVolumeZScore { get; set; }
		public double AverageRangeZScore { get; set; }
		public double MaximumRangeZScore { get; set; }
		public double MinimumRangeZScore { get; set; }
		public double AverageAbsoluteDeltaZScore { get; set; }
		public double MaximumAbsoluteDeltaZScore { get; set; }
		public double AverageVolumePerRangeTickZScore { get; set; }
		public double SignedPriceDeltaDivergenceScore { get; set; }
		public double EffortResultScore { get; set; }
		public double PocAgreementScore { get; set; }
		public double ImbalanceAgreementScore { get; set; }
		public bool RollingInputsReady { get; set; }
		public bool ValidationPassed { get; set; }
		public string ValidationMessage { get; set; }
	}

	public readonly struct xEvidenceKey : IEquatable<xEvidenceKey>
	{
		public int EndBarIndex { get; }
		public int WindowBars { get; }

		public xEvidenceKey(int endBarIndex, int windowBars)
		{
			EndBarIndex = endBarIndex;
			WindowBars = windowBars;
		}

		public bool Equals(xEvidenceKey other)
		{
			return EndBarIndex == other.EndBarIndex && WindowBars == other.WindowBars;
		}

		public override bool Equals(object obj)
		{
			return obj is xEvidenceKey && Equals((xEvidenceKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (EndBarIndex * 397) ^ WindowBars;
			}
		}
	}

	public class xPvaOrderFlowEvidence : Indicator
	{
		private readonly Dictionary<int, xOrderFlowBarFeatures> sourceFeaturesByIndex = new Dictionary<int, xOrderFlowBarFeatures>();
		private readonly Dictionary<xEvidenceKey, xOrderFlowEvidence> evidenceByKey = new Dictionary<xEvidenceKey, xOrderFlowEvidence>();
		private readonly Dictionary<int, xOrderFlowEvidence> mostRecentEvidenceByWindow = new Dictionary<int, xOrderFlowEvidence>();
		private readonly Queue<xEvidenceKey> evidenceOrder = new Queue<xEvidenceKey>();
		private readonly object writerLock = new object();
		private StreamWriter evidenceWriter;
		private string exportFolder;
		private string fileInstrument;
		private string fileStamp;
		private xOrderFlowFeatureRegistryKey featureRegistryKey;
		private int nextFeatureIndexToProcess;
		private bool registryInitialized;
		private bool lastActivePublisherState;
		private bool lastAmbiguousState;
		private bool printedNoPublisher;
		private bool printedPublisherFound;
		private int oldestPublishedIndex = -1;
		private int newestPublishedIndex = -1;
		private int mostRecentFeatureBarIndex = -1;
		private int mostRecentEvidenceEndIndex = -1;
		private int registryMisses;
		private int lastRegistryMissIndex = -1;
		private DateTime lastPanelRefresh;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaOrderFlowEvidence";
				Description = "Objective multi-bar evidence layer for finalized xPvaOrderFlowFeatures objects.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				RegistryInstanceKey = "APVA";
				EnableWindow2 = true;
				EnableWindow3 = true;
				EnableWindow5 = true;
				EnableWindow8 = false;
				PriceDirectionToleranceTicks = 0.5;
				DeltaDirectionTolerance = 0;
				PocDirectionToleranceTicks = 0.5;
				StackedImbalanceMinimumLevels = 3;
				ExportEvidenceCsv = true;
				FlushOnEvidenceWrite = true;
				ShowDiagnosticsPanel = true;
				MaxEvidenceObjectsRetained = 5000;
			}
			else if (State == State.DataLoaded)
			{
				nextFeatureIndexToProcess = -1;
				featureRegistryKey = BuildFeatureRegistryKey();
				EnsureExportFolder();
			}
			else if (State == State.Realtime)
			{
				OpenWriter();
				Print("xPvaOrderFlowEvidence uses RegistryInstanceKey as the feature registry instance key.");
				Print("xPvaOrderFlowEvidence EffortResultScore formula: AverageVolumePerRangeTickZScore - abs(AverageRangeZScore), only when all rolling inputs are ready.");
			}
			else if (State == State.Terminated)
			{
				CloseWriter();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 1)
				return;

			ProcessAvailableFeatureBars();
			UpdateDiagnosticsPanel(false);
		}

		public bool TryGetEvidence(int endBarIndex, int windowBars, out xOrderFlowEvidence evidence)
		{
			return evidenceByKey.TryGetValue(new xEvidenceKey(endBarIndex, windowBars), out evidence);
		}

		public xOrderFlowEvidence GetMostRecentEvidence(int windowBars)
		{
			xOrderFlowEvidence evidence;
			return mostRecentEvidenceByWindow.TryGetValue(windowBars, out evidence) ? evidence : null;
		}

		private void ProcessAvailableFeatureBars()
		{
			bool activePublisher = xPvaOrderFlowFeatureRegistry.HasActivePublisher(featureRegistryKey);
			bool ambiguous = xPvaOrderFlowFeatureRegistry.IsAmbiguous(featureRegistryKey);
			lastActivePublisherState = activePublisher;
			lastAmbiguousState = ambiguous;

			if (ambiguous)
			{
				if (!printedNoPublisher)
				{
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowEvidence WARNING: feature publisher ambiguity detected for registry key {0}. Evidence rows will not be consumed until the key is unambiguous.", featureRegistryKey));
					printedNoPublisher = true;
				}
				return;
			}

			if (!activePublisher)
			{
				if (!printedNoPublisher)
				{
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowEvidence: No active xPvaOrderFlowFeatures publisher found for registry key {0}", featureRegistryKey));
					printedNoPublisher = true;
					printedPublisherFound = false;
				}
				return;
			}

			if (!printedPublisherFound)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowEvidence: Active feature publisher found for registry key {0}", featureRegistryKey));
				printedPublisherFound = true;
				printedNoPublisher = false;
			}

			if (!xPvaOrderFlowFeatureRegistry.TryGetPublishedIndexRange(featureRegistryKey, out oldestPublishedIndex, out newestPublishedIndex))
				return;

			if (!registryInitialized)
			{
				nextFeatureIndexToProcess = oldestPublishedIndex;
				registryInitialized = true;
			}
			else if (nextFeatureIndexToProcess < oldestPublishedIndex)
			{
				nextFeatureIndexToProcess = oldestPublishedIndex;
			}

			int latestEligibleIndex = CurrentBar - 1;
			int latestPublishedIndex = xPvaOrderFlowFeatureRegistry.GetMostRecentPublishedIndex(featureRegistryKey);
			int upper = Math.Min(latestEligibleIndex, latestPublishedIndex);

			for (int index = nextFeatureIndexToProcess; index <= upper; index++)
			{
				xOrderFlowBarFeatures features;
				if (!xPvaOrderFlowFeatureRegistry.TryGetFeatures(featureRegistryKey, index, out features))
				{
					registryMisses++;
					lastRegistryMissIndex = index;
					nextFeatureIndexToProcess = index + 1;
					continue;
				}

				StoreSourceFeatures(features);
				ProcessWindowsEndingAt(features.BarIndex);
				nextFeatureIndexToProcess = index + 1;
			}
		}

		private void StoreSourceFeatures(xOrderFlowBarFeatures features)
		{
			sourceFeaturesByIndex[features.BarIndex] = features;
			mostRecentFeatureBarIndex = features.BarIndex;
			TrimSourceFeatures(features.BarIndex);
		}

		private void ProcessWindowsEndingAt(int endBarIndex)
		{
			int[] windows = EnabledWindows();
			for (int i = 0; i < windows.Length; i++)
			{
				int window = windows[i];
				xEvidenceKey key = new xEvidenceKey(endBarIndex, window);
				if (evidenceByKey.ContainsKey(key))
					continue;

				if (!WindowSourcesReady(endBarIndex, window))
					continue;

				xOrderFlowEvidence evidence = CalculateEvidence(endBarIndex, window);
				ValidateEvidence(evidence);
				if (!evidence.ValidationPassed)
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowEvidence validation failed: EndBarIndex={0}, WindowBars={1}, {2}", evidence.EndBarIndex, evidence.WindowBars, evidence.ValidationMessage));

				StoreEvidence(key, evidence);
				WriteEvidence(evidence);
				if (FlushOnEvidenceWrite)
					FlushWriter();
			}
		}

		private bool WindowSourcesReady(int endBarIndex, int window)
		{
			int start = endBarIndex - window + 1;
			for (int index = start; index <= endBarIndex; index++)
			{
				xOrderFlowBarFeatures f;
				if (!sourceFeaturesByIndex.TryGetValue(index, out f) || !IsUsableSource(f))
					return false;
			}
			return true;
		}

		private bool IsUsableSource(xOrderFlowBarFeatures f)
		{
			return f != null
				&& f.SourceBarAvailable
				&& f.SourceBarValidationPassed
				&& f.SourceCaptureCompletenessPassed
				&& !f.SourceBarIsPartial
				&& f.SourceBarIsRealtimeCaptured
				&& f.ValidationPassed;
		}

		private xOrderFlowEvidence CalculateEvidence(int endBarIndex, int window)
		{
			int start = endBarIndex - window + 1;
			xOrderFlowBarFeatures first = sourceFeaturesByIndex[start];
			xOrderFlowBarFeatures last = sourceFeaturesByIndex[endBarIndex];
			xOrderFlowEvidence e = new xOrderFlowEvidence();
			e.StartBarIndex = start;
			e.EndBarIndex = endBarIndex;
			e.WindowBars = window;
			e.StartTime = first.BarOpenTime != DateTime.MinValue ? first.BarOpenTime : first.NinjaBarTime;
			e.EndTime = last.BarCloseTime != DateTime.MinValue ? last.BarCloseTime : last.NinjaBarTime;
			e.SourceRangeComplete = true;
			e.SourceValidationPassed = true;
			e.StartOpen = first.Open;
			e.EndClose = last.Close;
			e.HighestHigh = first.High;
			e.LowestLow = first.Low;
			e.FirstPocPrice = first.PocPrice;
			e.LastPocPrice = last.PocPrice;

			double sumDeltaPercent = 0;
			double sumAbsDeltaPercent = 0;
			double sumPocLocation = 0;
			double sumCloseLocation = 0;
			double sumCloseToPocTicks = 0;
			double sumTop1 = 0;
			double sumTop3 = 0;
			double sumTop5 = 0;
			double sumVolumeZ = 0;
			double sumRangeZ = 0;
			double sumAbsDeltaZ = 0;
			double sumVolumePerRangeZ = 0;
			double maxVolumeZ = double.MinValue;
			double minVolumeZ = double.MaxValue;
			double maxRangeZ = double.MinValue;
			double minRangeZ = double.MaxValue;
			double maxAbsDeltaZ = double.MinValue;
			bool rollingReady = true;

			for (int index = start; index <= endBarIndex; index++)
			{
				xOrderFlowBarFeatures f = sourceFeaturesByIndex[index];
				if (f.High > e.HighestHigh)
					e.HighestHigh = f.High;
				if (f.Low < e.LowestLow)
					e.LowestLow = f.Low;
				if (f.Close > f.Open)
					e.UpCloseBars++;
				else if (f.Close < f.Open)
					e.DownCloseBars++;
				else
					e.UnchangedCloseBars++;

				e.TotalVolume += f.Volume;
				e.TotalBuyVolume += f.BuyVolume;
				e.TotalSellVolume += f.SellVolume;
				e.TotalUnknownVolume += f.UnknownVolume;
				e.TotalDelta += f.Delta;
				e.TotalAbsoluteBarDelta += Math.Abs(f.Delta);
				sumDeltaPercent += f.DeltaPercent;
				sumAbsDeltaPercent += f.AbsoluteDeltaPercent;
				sumPocLocation += f.PocLocation;
				sumCloseLocation += f.CloseLocation;
				sumCloseToPocTicks += f.CloseToPocTicks;

				e.TotalBuyImbalanceCount += f.BuyImbalanceCount;
				e.TotalSellImbalanceCount += f.SellImbalanceCount;
				if (f.BuyImbalanceCount > 0)
					e.BarsWithBuyImbalances++;
				if (f.SellImbalanceCount > 0)
					e.BarsWithSellImbalances++;
				if (f.MaxStackedBuyImbalances >= StackedImbalanceMinimumLevels)
					e.BarsWithStackedBuyImbalances++;
				if (f.MaxStackedSellImbalances >= StackedImbalanceMinimumLevels)
					e.BarsWithStackedSellImbalances++;
				if (f.MaxStackedBuyImbalances > e.MaxObservedBuyStack)
					e.MaxObservedBuyStack = f.MaxStackedBuyImbalances;
				if (f.MaxStackedSellImbalances > e.MaxObservedSellStack)
					e.MaxObservedSellStack = f.MaxStackedSellImbalances;
				e.TotalBuyImbalanceVolume += f.BuyImbalanceVolume;
				e.TotalSellImbalanceVolume += f.SellImbalanceVolume;

				sumTop1 += f.VolumeConcentrationTop1Percent;
				sumTop3 += f.VolumeConcentrationTop3Percent;
				sumTop5 += f.VolumeConcentrationTop5Percent;
				if (f.ZeroSellVolumeAtHigh)
					e.BarsWithZeroSellVolumeAtHigh++;
				if (f.ZeroBuyVolumeAtLow)
					e.BarsWithZeroBuyVolumeAtLow++;

				rollingReady = rollingReady && f.RollingStatisticsReady;
				sumVolumeZ += f.VolumeZScore;
				sumRangeZ += f.RangeZScore;
				sumAbsDeltaZ += f.AbsoluteDeltaZScore;
				sumVolumePerRangeZ += f.VolumePerRangeTickZScore;
				if (f.VolumeZScore > maxVolumeZ)
					maxVolumeZ = f.VolumeZScore;
				if (f.VolumeZScore < minVolumeZ)
					minVolumeZ = f.VolumeZScore;
				if (f.RangeZScore > maxRangeZ)
					maxRangeZ = f.RangeZScore;
				if (f.RangeZScore < minRangeZ)
					minRangeZ = f.RangeZScore;
				if (f.AbsoluteDeltaZScore > maxAbsDeltaZ)
					maxAbsDeltaZ = f.AbsoluteDeltaZScore;

				if (index > start)
				{
					xOrderFlowBarFeatures prior = sourceFeaturesByIndex[index - 1];
					int step = SignWithTolerance(TickSize > 0 ? (f.PocPrice - prior.PocPrice) / TickSize : 0, PocDirectionToleranceTicks);
					if (step > 0)
						e.PositivePocMigrationSteps++;
					else if (step < 0)
						e.NegativePocMigrationSteps++;
					else
						e.FlatPocMigrationSteps++;
				}
			}

			e.GrossRangeTicks = Math.Max(0, PriceDifferenceToTicks(e.HighestHigh - e.LowestLow));
			e.NetPriceChangeTicks = TickSize > 0 ? (e.EndClose - e.StartOpen) / TickSize : 0;
			e.AbsoluteNetPriceChangeTicks = Math.Abs(e.NetPriceChangeTicks);
			e.AggregateDeltaPercent = SafeDivide(e.TotalDelta, e.TotalVolume);
			e.AverageBarDeltaPercent = SafeDivide(sumDeltaPercent, window);
			e.AverageAbsoluteDeltaPercent = SafeDivide(sumAbsDeltaPercent, window);
			e.VolumePerNetPriceTick = SafeDivide(e.TotalVolume, Math.Max(1.0, e.AbsoluteNetPriceChangeTicks));
			e.AbsoluteDeltaPerNetPriceTick = SafeDivide(e.TotalAbsoluteBarDelta, Math.Max(1.0, e.AbsoluteNetPriceChangeTicks));
			e.TotalVolumePerGrossRangeTick = SafeDivide(e.TotalVolume, Math.Max(1, e.GrossRangeTicks));
			e.PriceProgressEfficiency = Clamp01(SafeDivide(e.AbsoluteNetPriceChangeTicks, Math.Max(1, e.GrossRangeTicks)));
			e.DirectionalCloseConsistency = DirectionalCloseConsistency(e);
			e.PriceDirectionSign = SignWithTolerance(e.NetPriceChangeTicks, PriceDirectionToleranceTicks);
			e.DeltaDirectionSign = SignWithTolerance(e.TotalDelta, DeltaDirectionTolerance);
			e.PocMigrationTicks = TickSize > 0 ? (e.LastPocPrice - e.FirstPocPrice) / TickSize : 0;
			e.PocMigrationSign = SignWithTolerance(e.PocMigrationTicks, PocDirectionToleranceTicks);
			e.PriceDeltaDirectionAgreement = e.PriceDirectionSign != 0 && e.PriceDirectionSign == e.DeltaDirectionSign;
			e.PriceDeltaDirectionDisagreement = e.PriceDirectionSign != 0 && e.DeltaDirectionSign != 0 && e.PriceDirectionSign != e.DeltaDirectionSign;
			e.AveragePocLocation = SafeDivide(sumPocLocation, window);
			e.AverageCloseLocation = SafeDivide(sumCloseLocation, window);
			e.AverageCloseToPocTicks = SafeDivide(sumCloseToPocTicks, window);
			e.BuyImbalanceVolumePercent = SafeDivide(e.TotalBuyImbalanceVolume, e.TotalVolume);
			e.SellImbalanceVolumePercent = SafeDivide(e.TotalSellImbalanceVolume, e.TotalVolume);
			e.AverageVolumeConcentrationTop1Percent = SafeDivide(sumTop1, window);
			e.AverageVolumeConcentrationTop3Percent = SafeDivide(sumTop3, window);
			e.AverageVolumeConcentrationTop5Percent = SafeDivide(sumTop5, window);
			e.VolumeConcentrationTrend = last.VolumeConcentrationTop3Percent - first.VolumeConcentrationTop3Percent;
			e.RollingInputsReady = rollingReady;
			if (rollingReady)
			{
				e.AverageVolumeZScore = SafeDivide(sumVolumeZ, window);
				e.MaximumVolumeZScore = maxVolumeZ;
				e.MinimumVolumeZScore = minVolumeZ;
				e.AverageRangeZScore = SafeDivide(sumRangeZ, window);
				e.MaximumRangeZScore = maxRangeZ;
				e.MinimumRangeZScore = minRangeZ;
				e.AverageAbsoluteDeltaZScore = SafeDivide(sumAbsDeltaZ, window);
				e.MaximumAbsoluteDeltaZScore = maxAbsDeltaZ;
				e.AverageVolumePerRangeTickZScore = SafeDivide(sumVolumePerRangeZ, window);
				e.EffortResultScore = e.AverageVolumePerRangeTickZScore - Math.Abs(e.AverageRangeZScore);
			}

			double normalizedPrice = Clamp(SafeDivide(e.NetPriceChangeTicks, Math.Max(1, e.GrossRangeTicks)), -1, 1);
			e.SignedPriceDeltaDivergenceScore = normalizedPrice - e.AggregateDeltaPercent;
			e.PocAgreementScore = normalizedPrice * Clamp(SafeDivide(e.PocMigrationTicks, Math.Max(1, e.GrossRangeTicks)), -1, 1);
			e.ImbalanceAgreementScore = normalizedPrice * (e.BuyImbalanceVolumePercent - e.SellImbalanceVolumePercent);
			SanitizeEvidenceValues(e);
			return e;
		}

		private void ValidateEvidence(xOrderFlowEvidence e)
		{
			StringBuilder message = new StringBuilder();
			if (e.StartBarIndex > e.EndBarIndex)
				message.Append("start index after end index; ");
			if (e.WindowBars != e.EndBarIndex - e.StartBarIndex + 1)
				message.Append("window size/index range mismatch; ");
			if (!e.SourceRangeComplete)
				message.Append("source range incomplete; ");
			if (!e.SourceValidationPassed)
				message.Append("source validation failed; ");
			if (e.TotalBuyVolume + e.TotalSellVolume + e.TotalUnknownVolume != e.TotalVolume)
				message.Append("side volume sum mismatch; ");
			if (e.TotalDelta != e.TotalBuyVolume - e.TotalSellVolume)
				message.Append("total delta mismatch; ");
			if (!Between(e.PriceProgressEfficiency, 0, 1))
				message.Append("price progress efficiency out of range; ");
			if (!Between(e.DirectionalCloseConsistency, 0, 1))
				message.Append("directional close consistency out of range; ");
			if (!Between(e.PocAgreementScore, -1.000001, 1.000001))
				message.Append("POC agreement score out of range; ");
			if (!AllFinite(e))
				message.Append("non-finite numeric value; ");

			e.ValidationPassed = message.Length == 0;
			e.ValidationMessage = e.ValidationPassed ? "OK" : message.ToString().Trim();
		}

		private void StoreEvidence(xEvidenceKey key, xOrderFlowEvidence evidence)
		{
			evidenceByKey[key] = evidence;
			mostRecentEvidenceByWindow[evidence.WindowBars] = evidence;
			evidenceOrder.Enqueue(key);
			mostRecentEvidenceEndIndex = evidence.EndBarIndex;
			while (evidenceOrder.Count > MaxEvidenceObjectsRetained)
			{
				xEvidenceKey removeKey = evidenceOrder.Dequeue();
				xOrderFlowEvidence removed;
				if (evidenceByKey.TryGetValue(removeKey, out removed) && removed == evidence)
					continue;
				evidenceByKey.Remove(removeKey);
			}
		}

		private void TrimSourceFeatures(int currentIndex)
		{
			int keepFrom = currentIndex - LargestEnabledWindow() + 1;
			List<int> remove = new List<int>();
			foreach (int index in sourceFeaturesByIndex.Keys)
				if (index < keepFrom)
					remove.Add(index);
			for (int i = 0; i < remove.Count; i++)
				sourceFeaturesByIndex.Remove(remove[i]);
		}

		private int[] EnabledWindows()
		{
			List<int> windows = new List<int>();
			if (EnableWindow2)
				windows.Add(2);
			if (EnableWindow3)
				windows.Add(3);
			if (EnableWindow5)
				windows.Add(5);
			if (EnableWindow8)
				windows.Add(8);
			return windows.ToArray();
		}

		private int LargestEnabledWindow()
		{
			int largest = 1;
			if (EnableWindow2)
				largest = 2;
			if (EnableWindow3)
				largest = 3;
			if (EnableWindow5)
				largest = 5;
			if (EnableWindow8)
				largest = 8;
			return largest;
		}

		private double DirectionalCloseConsistency(xOrderFlowEvidence e)
		{
			if (e.NetPriceChangeTicks > 0)
				return SafeDivide(e.UpCloseBars, e.WindowBars);
			if (e.NetPriceChangeTicks < 0)
				return SafeDivide(e.DownCloseBars, e.WindowBars);
			return SafeDivide(e.UnchangedCloseBars, e.WindowBars);
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

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xPvaOrderFlowEvidence");
			Directory.CreateDirectory(exportFolder);
			fileInstrument = SanitizeFileName(Instrument != null ? Instrument.FullName : "UnknownInstrument");
			fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
		}

		private void OpenWriter()
		{
			if (!ExportEvidenceCsv)
				return;
			try
			{
				evidenceWriter = new StreamWriter(Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaOrderFlowEvidence_BARS_{0}_{1}.csv", fileInstrument, fileStamp)), false, Encoding.UTF8);
				evidenceWriter.WriteLine("StartBarIndex,EndBarIndex,WindowBars,StartTime,EndTime,SourceRangeComplete,SourceValidationPassed,StartOpen,EndClose,HighestHigh,LowestLow,GrossRangeTicks,NetPriceChangeTicks,AbsoluteNetPriceChangeTicks,UpCloseBars,DownCloseBars,UnchangedCloseBars,TotalVolume,TotalBuyVolume,TotalSellVolume,TotalUnknownVolume,TotalDelta,TotalAbsoluteBarDelta,AggregateDeltaPercent,AverageBarDeltaPercent,AverageAbsoluteDeltaPercent,VolumePerNetPriceTick,AbsoluteDeltaPerNetPriceTick,TotalVolumePerGrossRangeTick,PriceProgressEfficiency,DirectionalCloseConsistency,PriceDirectionSign,DeltaDirectionSign,PocMigrationSign,PriceDeltaDirectionAgreement,PriceDeltaDirectionDisagreement,FirstPocPrice,LastPocPrice,PocMigrationTicks,AveragePocLocation,AverageCloseLocation,AverageCloseToPocTicks,PositivePocMigrationSteps,NegativePocMigrationSteps,FlatPocMigrationSteps,TotalBuyImbalanceCount,TotalSellImbalanceCount,BarsWithBuyImbalances,BarsWithSellImbalances,BarsWithStackedBuyImbalances,BarsWithStackedSellImbalances,MaxObservedBuyStack,MaxObservedSellStack,TotalBuyImbalanceVolume,TotalSellImbalanceVolume,BuyImbalanceVolumePercent,SellImbalanceVolumePercent,AverageVolumeConcentrationTop1Percent,AverageVolumeConcentrationTop3Percent,AverageVolumeConcentrationTop5Percent,VolumeConcentrationTrend,BarsWithZeroSellVolumeAtHigh,BarsWithZeroBuyVolumeAtLow,AverageVolumeZScore,MaximumVolumeZScore,MinimumVolumeZScore,AverageRangeZScore,MaximumRangeZScore,MinimumRangeZScore,AverageAbsoluteDeltaZScore,MaximumAbsoluteDeltaZScore,AverageVolumePerRangeTickZScore,SignedPriceDeltaDivergenceScore,EffortResultScore,PocAgreementScore,ImbalanceAgreementScore,RollingInputsReady,ValidationPassed,ValidationMessage");
			}
			catch (Exception ex)
			{
				Print("xPvaOrderFlowEvidence export open failed: " + ex.Message);
				CloseWriter();
			}
		}

		private void CloseWriter()
		{
			lock (writerLock)
			{
				try
				{
					if (evidenceWriter != null)
					{
						evidenceWriter.Flush();
						evidenceWriter.Close();
						evidenceWriter = null;
					}
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowEvidence export close failed: " + ex.Message);
				}
			}
		}

		private void WriteEvidence(xOrderFlowEvidence e)
		{
			if (evidenceWriter == null)
				return;

			lock (writerLock)
			{
				try
				{
					string[] columns =
					{
						e.StartBarIndex.ToString(CultureInfo.InvariantCulture), e.EndBarIndex.ToString(CultureInfo.InvariantCulture), e.WindowBars.ToString(CultureInfo.InvariantCulture),
						FormatTime(e.StartTime), FormatTime(e.EndTime), e.SourceRangeComplete.ToString(), e.SourceValidationPassed.ToString(),
						FormatDouble(e.StartOpen), FormatDouble(e.EndClose), FormatDouble(e.HighestHigh), FormatDouble(e.LowestLow),
						e.GrossRangeTicks.ToString(CultureInfo.InvariantCulture), FormatDouble(e.NetPriceChangeTicks), FormatDouble(e.AbsoluteNetPriceChangeTicks),
						e.UpCloseBars.ToString(CultureInfo.InvariantCulture), e.DownCloseBars.ToString(CultureInfo.InvariantCulture), e.UnchangedCloseBars.ToString(CultureInfo.InvariantCulture),
						e.TotalVolume.ToString(CultureInfo.InvariantCulture), e.TotalBuyVolume.ToString(CultureInfo.InvariantCulture), e.TotalSellVolume.ToString(CultureInfo.InvariantCulture), e.TotalUnknownVolume.ToString(CultureInfo.InvariantCulture),
						e.TotalDelta.ToString(CultureInfo.InvariantCulture), e.TotalAbsoluteBarDelta.ToString(CultureInfo.InvariantCulture),
						FormatDouble(e.AggregateDeltaPercent), FormatDouble(e.AverageBarDeltaPercent), FormatDouble(e.AverageAbsoluteDeltaPercent),
						FormatDouble(e.VolumePerNetPriceTick), FormatDouble(e.AbsoluteDeltaPerNetPriceTick), FormatDouble(e.TotalVolumePerGrossRangeTick),
						FormatDouble(e.PriceProgressEfficiency), FormatDouble(e.DirectionalCloseConsistency),
						e.PriceDirectionSign.ToString(CultureInfo.InvariantCulture), e.DeltaDirectionSign.ToString(CultureInfo.InvariantCulture), e.PocMigrationSign.ToString(CultureInfo.InvariantCulture),
						e.PriceDeltaDirectionAgreement.ToString(), e.PriceDeltaDirectionDisagreement.ToString(),
						FormatDouble(e.FirstPocPrice), FormatDouble(e.LastPocPrice), FormatDouble(e.PocMigrationTicks),
						FormatDouble(e.AveragePocLocation), FormatDouble(e.AverageCloseLocation), FormatDouble(e.AverageCloseToPocTicks),
						e.PositivePocMigrationSteps.ToString(CultureInfo.InvariantCulture), e.NegativePocMigrationSteps.ToString(CultureInfo.InvariantCulture), e.FlatPocMigrationSteps.ToString(CultureInfo.InvariantCulture),
						e.TotalBuyImbalanceCount.ToString(CultureInfo.InvariantCulture), e.TotalSellImbalanceCount.ToString(CultureInfo.InvariantCulture),
						e.BarsWithBuyImbalances.ToString(CultureInfo.InvariantCulture), e.BarsWithSellImbalances.ToString(CultureInfo.InvariantCulture),
						e.BarsWithStackedBuyImbalances.ToString(CultureInfo.InvariantCulture), e.BarsWithStackedSellImbalances.ToString(CultureInfo.InvariantCulture),
						e.MaxObservedBuyStack.ToString(CultureInfo.InvariantCulture), e.MaxObservedSellStack.ToString(CultureInfo.InvariantCulture),
						e.TotalBuyImbalanceVolume.ToString(CultureInfo.InvariantCulture), e.TotalSellImbalanceVolume.ToString(CultureInfo.InvariantCulture),
						FormatDouble(e.BuyImbalanceVolumePercent), FormatDouble(e.SellImbalanceVolumePercent),
						FormatDouble(e.AverageVolumeConcentrationTop1Percent), FormatDouble(e.AverageVolumeConcentrationTop3Percent), FormatDouble(e.AverageVolumeConcentrationTop5Percent), FormatDouble(e.VolumeConcentrationTrend),
						e.BarsWithZeroSellVolumeAtHigh.ToString(CultureInfo.InvariantCulture), e.BarsWithZeroBuyVolumeAtLow.ToString(CultureInfo.InvariantCulture),
						FormatDouble(e.AverageVolumeZScore), FormatDouble(e.MaximumVolumeZScore), FormatDouble(e.MinimumVolumeZScore),
						FormatDouble(e.AverageRangeZScore), FormatDouble(e.MaximumRangeZScore), FormatDouble(e.MinimumRangeZScore),
						FormatDouble(e.AverageAbsoluteDeltaZScore), FormatDouble(e.MaximumAbsoluteDeltaZScore), FormatDouble(e.AverageVolumePerRangeTickZScore),
						FormatDouble(e.SignedPriceDeltaDivergenceScore), FormatDouble(e.EffortResultScore), FormatDouble(e.PocAgreementScore), FormatDouble(e.ImbalanceAgreementScore),
						e.RollingInputsReady.ToString(), e.ValidationPassed.ToString(), e.ValidationMessage
					};
					string row = ToCsv(columns);
					evidenceWriter.WriteLine(row);
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowEvidence export failed: " + ex.Message);
				}
			}
		}

		private void FlushWriter()
		{
			lock (writerLock)
			{
				try
				{
					if (evidenceWriter != null)
						evidenceWriter.Flush();
				}
				catch (Exception ex)
				{
					Print("xPvaOrderFlowEvidence flush failed: " + ex.Message);
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

			xOrderFlowEvidence w2 = GetMostRecentEvidence(2);
			xOrderFlowEvidence w3 = GetMostRecentEvidence(3);
			xOrderFlowEvidence w5 = GetMostRecentEvidence(5);
			xOrderFlowEvidence recent = w3 ?? w5 ?? w2;
			string text = string.Format(CultureInfo.InvariantCulture,
				"xPvaOrderFlowEvidence\nActive feature publisher found: {0}\nPublisher ambiguity detected: {1}\nMost recent feature bar index: {2}\nMost recent evidence end index: {3}\nEnabled windows: {4}\nWindow 2 net price / delta: {5} / {6}\nWindow 3 net price / delta: {7} / {8}\nWindow 5 net price / delta: {9} / {10}\nWindow 3 POC migration: {11}\nWindow 3 price/delta agreement: {12}\nWindow 3 effort/result score: {13:0.###}\nWindow 3 POC agreement score: {14:0.###}\nWindow 3 imbalance agreement score: {15:0.###}\nValidation status: {16}\nRolling inputs ready: {17}\nRegistry misses: {18}",
				lastActivePublisherState,
				lastAmbiguousState,
				mostRecentFeatureBarIndex,
				mostRecentEvidenceEndIndex,
				EnabledWindowText(),
				w2 != null ? FormatDouble(w2.NetPriceChangeTicks) : "", w2 != null ? w2.TotalDelta.ToString(CultureInfo.InvariantCulture) : "",
				w3 != null ? FormatDouble(w3.NetPriceChangeTicks) : "", w3 != null ? w3.TotalDelta.ToString(CultureInfo.InvariantCulture) : "",
				w5 != null ? FormatDouble(w5.NetPriceChangeTicks) : "", w5 != null ? w5.TotalDelta.ToString(CultureInfo.InvariantCulture) : "",
				w3 != null ? FormatDouble(w3.PocMigrationTicks) : "",
				w3 != null ? w3.PriceDeltaDirectionAgreement.ToString() : "",
				w3 != null ? w3.EffortResultScore : 0,
				w3 != null ? w3.PocAgreementScore : 0,
				w3 != null ? w3.ImbalanceAgreementScore : 0,
				recent != null ? recent.ValidationPassed.ToString() : "",
				recent != null ? recent.RollingInputsReady.ToString() : "",
				registryMisses);
			Draw.TextFixed(this, "xPvaOrderFlowEvidenceDiagnostics", text, TextPosition.BottomRight);
		}

		private string EnabledWindowText()
		{
			int[] windows = EnabledWindows();
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < windows.Length; i++)
			{
				if (i > 0)
					builder.Append('/');
				builder.Append(windows[i].ToString(CultureInfo.InvariantCulture));
			}
			return builder.ToString();
		}

		private int PriceDifferenceToTicks(double priceDifference)
		{
			return TickSize > 0 ? (int)Math.Round(priceDifference / TickSize, MidpointRounding.AwayFromZero) : 0;
		}

		private static int SignWithTolerance(double value, double tolerance)
		{
			if (value > tolerance)
				return 1;
			if (value < -tolerance)
				return -1;
			return 0;
		}

		private double SafeDivide(double numerator, double denominator)
		{
			return denominator != 0 ? numerator / denominator : 0;
		}

		private double Clamp01(double value)
		{
			return Clamp(value, 0, 1);
		}

		private double Clamp(double value, double min, double max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		private bool Between(double value, double min, double max)
		{
			return value >= min - 0.000001 && value <= max + 0.000001;
		}

		private void SanitizeEvidenceValues(xOrderFlowEvidence e)
		{
			e.StartOpen = Clean(e.StartOpen);
			e.EndClose = Clean(e.EndClose);
			e.HighestHigh = Clean(e.HighestHigh);
			e.LowestLow = Clean(e.LowestLow);
			e.NetPriceChangeTicks = Clean(e.NetPriceChangeTicks);
			e.AbsoluteNetPriceChangeTicks = Clean(e.AbsoluteNetPriceChangeTicks);
			e.AggregateDeltaPercent = Clean(e.AggregateDeltaPercent);
			e.AverageBarDeltaPercent = Clean(e.AverageBarDeltaPercent);
			e.AverageAbsoluteDeltaPercent = Clean(e.AverageAbsoluteDeltaPercent);
			e.VolumePerNetPriceTick = Clean(e.VolumePerNetPriceTick);
			e.AbsoluteDeltaPerNetPriceTick = Clean(e.AbsoluteDeltaPerNetPriceTick);
			e.TotalVolumePerGrossRangeTick = Clean(e.TotalVolumePerGrossRangeTick);
			e.PriceProgressEfficiency = Clamp01(Clean(e.PriceProgressEfficiency));
			e.DirectionalCloseConsistency = Clamp01(Clean(e.DirectionalCloseConsistency));
			e.FirstPocPrice = Clean(e.FirstPocPrice);
			e.LastPocPrice = Clean(e.LastPocPrice);
			e.PocMigrationTicks = Clean(e.PocMigrationTicks);
			e.AveragePocLocation = Clean(e.AveragePocLocation);
			e.AverageCloseLocation = Clean(e.AverageCloseLocation);
			e.AverageCloseToPocTicks = Clean(e.AverageCloseToPocTicks);
			e.BuyImbalanceVolumePercent = Clean(e.BuyImbalanceVolumePercent);
			e.SellImbalanceVolumePercent = Clean(e.SellImbalanceVolumePercent);
			e.AverageVolumeConcentrationTop1Percent = Clean(e.AverageVolumeConcentrationTop1Percent);
			e.AverageVolumeConcentrationTop3Percent = Clean(e.AverageVolumeConcentrationTop3Percent);
			e.AverageVolumeConcentrationTop5Percent = Clean(e.AverageVolumeConcentrationTop5Percent);
			e.VolumeConcentrationTrend = Clean(e.VolumeConcentrationTrend);
			e.AverageVolumeZScore = Clean(e.AverageVolumeZScore);
			e.MaximumVolumeZScore = Clean(e.MaximumVolumeZScore);
			e.MinimumVolumeZScore = Clean(e.MinimumVolumeZScore);
			e.AverageRangeZScore = Clean(e.AverageRangeZScore);
			e.MaximumRangeZScore = Clean(e.MaximumRangeZScore);
			e.MinimumRangeZScore = Clean(e.MinimumRangeZScore);
			e.AverageAbsoluteDeltaZScore = Clean(e.AverageAbsoluteDeltaZScore);
			e.MaximumAbsoluteDeltaZScore = Clean(e.MaximumAbsoluteDeltaZScore);
			e.AverageVolumePerRangeTickZScore = Clean(e.AverageVolumePerRangeTickZScore);
			e.SignedPriceDeltaDivergenceScore = Clean(e.SignedPriceDeltaDivergenceScore);
			e.EffortResultScore = Clean(e.EffortResultScore);
			e.PocAgreementScore = Clean(e.PocAgreementScore);
			e.ImbalanceAgreementScore = Clean(e.ImbalanceAgreementScore);
		}

		private bool AllFinite(xOrderFlowEvidence e)
		{
			return IsFinite(e.StartOpen) && IsFinite(e.EndClose) && IsFinite(e.HighestHigh) && IsFinite(e.LowestLow)
				&& IsFinite(e.NetPriceChangeTicks) && IsFinite(e.AbsoluteNetPriceChangeTicks)
				&& IsFinite(e.AggregateDeltaPercent) && IsFinite(e.AverageBarDeltaPercent) && IsFinite(e.AverageAbsoluteDeltaPercent)
				&& IsFinite(e.VolumePerNetPriceTick) && IsFinite(e.AbsoluteDeltaPerNetPriceTick) && IsFinite(e.TotalVolumePerGrossRangeTick)
				&& IsFinite(e.PriceProgressEfficiency) && IsFinite(e.DirectionalCloseConsistency)
				&& IsFinite(e.FirstPocPrice) && IsFinite(e.LastPocPrice) && IsFinite(e.PocMigrationTicks)
				&& IsFinite(e.AveragePocLocation) && IsFinite(e.AverageCloseLocation) && IsFinite(e.AverageCloseToPocTicks)
				&& IsFinite(e.BuyImbalanceVolumePercent) && IsFinite(e.SellImbalanceVolumePercent)
				&& IsFinite(e.AverageVolumeConcentrationTop1Percent) && IsFinite(e.AverageVolumeConcentrationTop3Percent) && IsFinite(e.AverageVolumeConcentrationTop5Percent)
				&& IsFinite(e.VolumeConcentrationTrend)
				&& IsFinite(e.AverageVolumeZScore) && IsFinite(e.MaximumVolumeZScore) && IsFinite(e.MinimumVolumeZScore)
				&& IsFinite(e.AverageRangeZScore) && IsFinite(e.MaximumRangeZScore) && IsFinite(e.MinimumRangeZScore)
				&& IsFinite(e.AverageAbsoluteDeltaZScore) && IsFinite(e.MaximumAbsoluteDeltaZScore) && IsFinite(e.AverageVolumePerRangeTickZScore)
				&& IsFinite(e.SignedPriceDeltaDivergenceScore) && IsFinite(e.EffortResultScore) && IsFinite(e.PocAgreementScore) && IsFinite(e.ImbalanceAgreementScore);
		}

		private bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
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
		[Display(Name = "Registry instance key", GroupName = "Registry", Order = 1)]
		public string RegistryInstanceKey { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable window 2", GroupName = "Windows", Order = 2)]
		public bool EnableWindow2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable window 3", GroupName = "Windows", Order = 3)]
		public bool EnableWindow3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable window 5", GroupName = "Windows", Order = 4)]
		public bool EnableWindow5 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable window 8", GroupName = "Windows", Order = 5)]
		public bool EnableWindow8 { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "Price direction tolerance ticks", GroupName = "Direction", Order = 6)]
		public double PriceDirectionToleranceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Delta direction tolerance", GroupName = "Direction", Order = 7)]
		public long DeltaDirectionTolerance { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "POC direction tolerance ticks", GroupName = "Direction", Order = 8)]
		public double PocDirectionToleranceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Stacked imbalance minimum levels", GroupName = "Imbalance", Order = 9)]
		public int StackedImbalanceMinimumLevels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export evidence CSV", GroupName = "Export", Order = 10)]
		public bool ExportEvidenceCsv { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flush on evidence write", GroupName = "Export", Order = 11)]
		public bool FlushOnEvidenceWrite { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 12)]
		public bool ShowDiagnosticsPanel { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max evidence objects retained", GroupName = "Memory", Order = 13)]
		public int MaxEvidenceObjectsRetained { get; set; }

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
		private xPvaOrderFlow.xPvaOrderFlowEvidence[] cachexPvaOrderFlowEvidence;
		public xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			return xPvaOrderFlowEvidence(Input, registryInstanceKey, enableWindow2, enableWindow3, enableWindow5, enableWindow8, priceDirectionToleranceTicks, deltaDirectionTolerance, pocDirectionToleranceTicks, stackedImbalanceMinimumLevels, exportEvidenceCsv, flushOnEvidenceWrite, showDiagnosticsPanel, maxEvidenceObjectsRetained);
		}

		public xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(ISeries<double> input, string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			if (cachexPvaOrderFlowEvidence != null)
				for (int idx = 0; idx < cachexPvaOrderFlowEvidence.Length; idx++)
					if (cachexPvaOrderFlowEvidence[idx] != null && cachexPvaOrderFlowEvidence[idx].RegistryInstanceKey == registryInstanceKey && cachexPvaOrderFlowEvidence[idx].EnableWindow2 == enableWindow2 && cachexPvaOrderFlowEvidence[idx].EnableWindow3 == enableWindow3 && cachexPvaOrderFlowEvidence[idx].EnableWindow5 == enableWindow5 && cachexPvaOrderFlowEvidence[idx].EnableWindow8 == enableWindow8 && cachexPvaOrderFlowEvidence[idx].PriceDirectionToleranceTicks == priceDirectionToleranceTicks && cachexPvaOrderFlowEvidence[idx].DeltaDirectionTolerance == deltaDirectionTolerance && cachexPvaOrderFlowEvidence[idx].PocDirectionToleranceTicks == pocDirectionToleranceTicks && cachexPvaOrderFlowEvidence[idx].StackedImbalanceMinimumLevels == stackedImbalanceMinimumLevels && cachexPvaOrderFlowEvidence[idx].ExportEvidenceCsv == exportEvidenceCsv && cachexPvaOrderFlowEvidence[idx].FlushOnEvidenceWrite == flushOnEvidenceWrite && cachexPvaOrderFlowEvidence[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexPvaOrderFlowEvidence[idx].MaxEvidenceObjectsRetained == maxEvidenceObjectsRetained && cachexPvaOrderFlowEvidence[idx].EqualsInput(input))
						return cachexPvaOrderFlowEvidence[idx];
			return CacheIndicator<xPvaOrderFlow.xPvaOrderFlowEvidence>(new xPvaOrderFlow.xPvaOrderFlowEvidence(){ RegistryInstanceKey = registryInstanceKey, EnableWindow2 = enableWindow2, EnableWindow3 = enableWindow3, EnableWindow5 = enableWindow5, EnableWindow8 = enableWindow8, PriceDirectionToleranceTicks = priceDirectionToleranceTicks, DeltaDirectionTolerance = deltaDirectionTolerance, PocDirectionToleranceTicks = pocDirectionToleranceTicks, StackedImbalanceMinimumLevels = stackedImbalanceMinimumLevels, ExportEvidenceCsv = exportEvidenceCsv, FlushOnEvidenceWrite = flushOnEvidenceWrite, ShowDiagnosticsPanel = showDiagnosticsPanel, MaxEvidenceObjectsRetained = maxEvidenceObjectsRetained }, input, ref cachexPvaOrderFlowEvidence);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			return indicator.xPvaOrderFlowEvidence(Input, registryInstanceKey, enableWindow2, enableWindow3, enableWindow5, enableWindow8, priceDirectionToleranceTicks, deltaDirectionTolerance, pocDirectionToleranceTicks, stackedImbalanceMinimumLevels, exportEvidenceCsv, flushOnEvidenceWrite, showDiagnosticsPanel, maxEvidenceObjectsRetained);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(ISeries<double> input , string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			return indicator.xPvaOrderFlowEvidence(input, registryInstanceKey, enableWindow2, enableWindow3, enableWindow5, enableWindow8, priceDirectionToleranceTicks, deltaDirectionTolerance, pocDirectionToleranceTicks, stackedImbalanceMinimumLevels, exportEvidenceCsv, flushOnEvidenceWrite, showDiagnosticsPanel, maxEvidenceObjectsRetained);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			return indicator.xPvaOrderFlowEvidence(Input, registryInstanceKey, enableWindow2, enableWindow3, enableWindow5, enableWindow8, priceDirectionToleranceTicks, deltaDirectionTolerance, pocDirectionToleranceTicks, stackedImbalanceMinimumLevels, exportEvidenceCsv, flushOnEvidenceWrite, showDiagnosticsPanel, maxEvidenceObjectsRetained);
		}

		public Indicators.xPvaOrderFlow.xPvaOrderFlowEvidence xPvaOrderFlowEvidence(ISeries<double> input , string registryInstanceKey, bool enableWindow2, bool enableWindow3, bool enableWindow5, bool enableWindow8, double priceDirectionToleranceTicks, long deltaDirectionTolerance, double pocDirectionToleranceTicks, int stackedImbalanceMinimumLevels, bool exportEvidenceCsv, bool flushOnEvidenceWrite, bool showDiagnosticsPanel, int maxEvidenceObjectsRetained)
		{
			return indicator.xPvaOrderFlowEvidence(input, registryInstanceKey, enableWindow2, enableWindow3, enableWindow5, enableWindow8, priceDirectionToleranceTicks, deltaDirectionTolerance, pocDirectionToleranceTicks, stackedImbalanceMinimumLevels, exportEvidenceCsv, flushOnEvidenceWrite, showDiagnosticsPanel, maxEvidenceObjectsRetained);
		}
	}
}

#endregion
