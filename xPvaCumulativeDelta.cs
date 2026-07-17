#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow
{
	public enum xCumulativeDeltaPeriod
	{
		Bar,
		Session
	}

	public enum xHistoricalDeltaDisplayMode
	{
		Hide,
		Zero,
		CloseOnlyIfAvailable
	}

	public enum xCumulativeDeltaDisplayStyle
	{
		Candles,
		Line,
		CandlesAndLine
	}

	public enum xIncompleteBarHandling
	{
		Hide,
		FlatCarryForward,
		PlotWithWarning
	}

	public sealed class xCumulativeDeltaBar
	{
		public int BarIndex { get; set; }
		public DateTime NinjaBarTime { get; set; }
		public DateTime BarOpenTime { get; set; }
		public DateTime BarCloseTime { get; set; }
		public long SourceCapturedVolume { get; set; }
		public long SourceBuyVolume { get; set; }
		public long SourceSellVolume { get; set; }
		public long SourceUnknownVolume { get; set; }
		public long BarDelta { get; set; }
		public long DeltaOpen { get; set; }
		public long DeltaHigh { get; set; }
		public long DeltaLow { get; set; }
		public long DeltaClose { get; set; }
		public bool IsSessionFirstBar { get; set; }
		public bool IsPartialSourceBar { get; set; }
		public bool IsRealtimeCaptured { get; set; }
		public bool SourceCaptureCompletenessPassed { get; set; }
		public long SourceVolumeDifference { get; set; }
		public int SourcePendingEventsAssigned { get; set; }
		public string IncompleteBarHandlingApplied { get; set; }
		public bool ValidationPassed { get; set; }
		public string ValidationMessage { get; set; }
	}

	public class xPvaCumulativeDelta : Indicator
	{
		private readonly Dictionary<int, xCumulativeDeltaBar> barsByIndex = new Dictionary<int, xCumulativeDeltaBar>();
		private readonly HashSet<int> sessionFirstBarIndexes = new HashSet<int>();
		private readonly Queue<int> barOrder = new Queue<int>();
		private readonly object writerLock = new object();
		private StreamWriter writer;
		private string exportFolder;
		private string exportPath;
		private string fileInstrument;
		private string fileStamp;
		private xOrderFlowRegistryKey registryKey;
		private int nextIndexToProcess = -1;
		private bool registryInitialized;
		private bool lastActivePublisherState;
		private bool lastAmbiguousState;
		private bool printedNoPublisher;
		private bool printedPublisherFound;
		private int oldestPublishedIndex = -1;
		private int newestPublishedIndex = -1;
		private int registryMisses;
		private int lastRegistryMissIndex = -1;
		private int mostRecentFinalizedBarIndex = -1;
		private xCumulativeDeltaBar mostRecentFinalizedBar;
		private xCumulativeDeltaBar activeBar;
		private long priorSessionDeltaClose;
		private int sessionResetCount;
		private DateTime lastPanelRefresh;
		private bool historicalMessageShown;
		private bool diagnosticsTextVisible;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaCumulativeDelta";
				Description = "Custom cumulative delta built from xPvaOrderFlowCore registry data.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = false;
				IsAutoScale = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = false;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;
				ScaleJustification = ScaleJustification.Right;

				RegistryInstanceKey = "APVA";
				CumulativePeriod = xCumulativeDeltaPeriod.Session;
				DisplayStyle = xCumulativeDeltaDisplayStyle.Candles;
				HistoricalDisplayMode = xHistoricalDeltaDisplayMode.Hide;
				ShowZeroLine = true;
				ShowDeltaZeroCrossMarkers = false;
				ShowPriceDeltaDisagreementMarkers = false;
				ExportCsv = true;
				FlushOnBarFinalization = true;
				ExportPartialOnTerminate = true;
				MaxBarsRetained = 2000;
				ShowDiagnosticsPanel = false;
				EnableNativeComparisonColumns = false;
				IncompleteBarHandling = xIncompleteBarHandling.Hide;
				UpCandleBrush = Brushes.LimeGreen;
				DownCandleBrush = Brushes.Crimson;
				CandleOutlineBrush = Brushes.DimGray;
				WickBrush = Brushes.DarkGray;
				DeltaLineBrush = Brushes.DodgerBlue;
				ZeroLineBrush = Brushes.Gray;
				CandleBodyWidth = 7;
				CandleOutlineWidth = 1;
				WickWidth = 1;
				DeltaLineWidth = 2;
				ZeroLineWidth = 1;

				AddPlot(Brushes.Transparent, "DeltaOpen");
				AddPlot(Brushes.Transparent, "DeltaHigh");
				AddPlot(Brushes.Transparent, "DeltaLow");
				AddPlot(Brushes.Transparent, "DeltaClose");
				AddPlot(Brushes.Transparent, "BarDelta");
				AddPlot(Brushes.Transparent, "NativeDeltaOpen");
				AddPlot(Brushes.Transparent, "NativeDeltaHigh");
				AddPlot(Brushes.Transparent, "NativeDeltaLow");
				AddPlot(Brushes.Transparent, "NativeDeltaClose");
				AddPlot(Brushes.Transparent, "NativeDeltaDifference");
			}
			else if (State == State.DataLoaded)
			{
				registryKey = BuildRegistryKey();
				EnsureExportFolder();
				OpenWriter();
			}
			else if (State == State.Historical)
			{
				if (!historicalMessageShown)
				{
					Print("xPvaCumulativeDelta: Custom cumulative delta begins with validated realtime order-flow capture. Historical Bid/Ask delta is unavailable unless supplied by a supported recorded source.");
					historicalMessageShown = true;
				}
			}
			else if (State == State.Terminated)
			{
				if (ExportPartialOnTerminate && activeBar != null)
					WriteBar(activeBar);
				CloseWriter();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0)
				return;

			if (Bars.IsFirstBarOfSession)
				sessionFirstBarIndexes.Add(CurrentBar);
			ProcessAvailableFinalizedBars();
			ProcessLiveSnapshot();
			UpdateDiagnosticsPanel(false);
		}

		public Series<double> DeltaOpenSeries { get { return Values[0]; } }
		public Series<double> DeltaHighSeries { get { return Values[1]; } }
		public Series<double> DeltaLowSeries { get { return Values[2]; } }
		public Series<double> DeltaCloseSeries { get { return Values[3]; } }
		public Series<double> BarDeltaSeries { get { return Values[4]; } }
		public Series<double> NativeDeltaOpenSeries { get { return Values[5]; } }
		public Series<double> NativeDeltaHighSeries { get { return Values[6]; } }
		public Series<double> NativeDeltaLowSeries { get { return Values[7]; } }
		public Series<double> NativeDeltaCloseSeries { get { return Values[8]; } }
		public Series<double> NativeDeltaDifferenceSeries { get { return Values[9]; } }

		public bool TryGetCumulativeDeltaBar(int absoluteBarIndex, out xCumulativeDeltaBar bar)
		{
			return barsByIndex.TryGetValue(absoluteBarIndex, out bar);
		}

		public xCumulativeDeltaBar GetMostRecentFinalizedBar()
		{
			return mostRecentFinalizedBar;
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
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta WARNING: publisher ambiguity detected for registry key {0}.", registryKey));
					printedNoPublisher = true;
				}
				return;
			}

			if (!activePublisher)
			{
				if (!printedNoPublisher)
				{
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta: No active xPvaOrderFlowCore publisher found for registry key {0}", registryKey));
					printedNoPublisher = true;
					printedPublisherFound = false;
				}
				return;
			}

			if (!printedPublisherFound)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta: Active core publisher found for registry key {0}", registryKey));
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

				xCumulativeDeltaBar bar = BuildFinalizedBar(source);
				ValidateBar(bar);
				if (!bar.ValidationPassed)
					Print(string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta validation failed: BarIndex={0}, Time={1}, {2}", bar.BarIndex, FormatTime(bar.NinjaBarTime), bar.ValidationMessage));
				if (!HandleIncompleteSourceBar(bar))
				{
					nextIndexToProcess = index + 1;
					continue;
				}
				StoreBar(bar);
				WriteBar(bar);
				SetSeriesForBar(bar);
				nextIndexToProcess = index + 1;
			}
		}

		private void ProcessLiveSnapshot()
		{
			xLiveOrderFlowSnapshot snapshot;
			if (!xPvaOrderFlowRegistry.TryGetLiveSnapshot(registryKey, out snapshot))
				return;
			if (snapshot.BarIndex < 0)
				return;

			activeBar = BuildLiveBar(snapshot);
			SetSeriesForBar(activeBar);
			ForceRefresh();
		}

		private xCumulativeDeltaBar BuildFinalizedBar(xOrderFlowBar source)
		{
			bool firstSessionBar = IsFirstSessionBar(source.BarIndex);
			long open = ResolveDeltaOpen(firstSessionBar);
			long barDelta = source.Delta;
			xCumulativeDeltaBar bar = new xCumulativeDeltaBar
			{
				BarIndex = source.BarIndex,
				NinjaBarTime = source.NinjaBarTime,
				BarOpenTime = source.BarOpenTime,
				BarCloseTime = source.BarCloseTime,
				SourceCapturedVolume = source.CapturedVolume,
				SourceBuyVolume = source.BuyVolume,
				SourceSellVolume = source.SellVolume,
				SourceUnknownVolume = source.UnknownVolume,
				BarDelta = barDelta,
				IsSessionFirstBar = firstSessionBar,
				IsPartialSourceBar = source.IsPartialBar,
				IsRealtimeCaptured = source.IsRealtimeCaptured,
				SourceCaptureCompletenessPassed = source.CaptureCompletenessPassed,
				SourceVolumeDifference = source.VolumeDifference,
				SourcePendingEventsAssigned = source.PendingEventsAssigned,
				IncompleteBarHandlingApplied = "None"
			};

			if (CumulativePeriod == xCumulativeDeltaPeriod.Bar)
			{
				bar.DeltaOpen = 0;
				bar.DeltaHigh = source.RunningDeltaHigh;
				bar.DeltaLow = source.RunningDeltaLow;
				bar.DeltaClose = barDelta;
			}
			else
			{
				bar.DeltaOpen = open;
				bar.DeltaHigh = open + source.RunningDeltaHigh;
				bar.DeltaLow = open + source.RunningDeltaLow;
				bar.DeltaClose = open + barDelta;
			}

			if (source.CapturedVolume == 0)
			{
				bar.DeltaHigh = bar.DeltaOpen;
				bar.DeltaLow = bar.DeltaOpen;
				bar.DeltaClose = bar.DeltaOpen;
			}
			return bar;
		}

		private bool HandleIncompleteSourceBar(xCumulativeDeltaBar bar)
		{
			if (bar.SourceCaptureCompletenessPassed)
				return true;

			bar.ValidationPassed = false;
			bar.ValidationMessage = string.Format(CultureInfo.InvariantCulture,
				"Incomplete source capture; handling={0}; source volume difference={1}; pending assigned={2}",
				IncompleteBarHandling,
				bar.SourceVolumeDifference,
				bar.SourcePendingEventsAssigned);
			bar.IncompleteBarHandlingApplied = IncompleteBarHandling.ToString();
			Print(string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta skipped incomplete source bar {0}: {1}", bar.BarIndex, bar.ValidationMessage));

			if (IncompleteBarHandling == xIncompleteBarHandling.Hide)
				return false;

			if (IncompleteBarHandling == xIncompleteBarHandling.FlatCarryForward)
			{
				long carry = mostRecentFinalizedBar != null ? mostRecentFinalizedBar.DeltaClose : priorSessionDeltaClose;
				bar.BarDelta = 0;
				bar.DeltaOpen = carry;
				bar.DeltaHigh = carry;
				bar.DeltaLow = carry;
				bar.DeltaClose = carry;
				bar.ValidationPassed = true;
				bar.ValidationMessage = "Incomplete source capture flat carry-forward";
				return true;
			}

			return true;
		}

		private xCumulativeDeltaBar BuildLiveBar(xLiveOrderFlowSnapshot snapshot)
		{
			bool firstSessionBar = IsFirstSessionBar(snapshot.BarIndex);
			long open = ResolveDeltaOpen(firstSessionBar);
			long barDelta = snapshot.BuyVolume - snapshot.SellVolume;
			xCumulativeDeltaBar bar = new xCumulativeDeltaBar
			{
				BarIndex = snapshot.BarIndex,
				SourceCapturedVolume = snapshot.CapturedVolume,
				SourceBuyVolume = snapshot.BuyVolume,
				SourceSellVolume = snapshot.SellVolume,
				SourceUnknownVolume = snapshot.UnknownVolume,
				BarDelta = barDelta,
				IsSessionFirstBar = firstSessionBar,
				IsPartialSourceBar = snapshot.IsPartialBar,
				IsRealtimeCaptured = true,
				ValidationPassed = true,
				ValidationMessage = "Live"
			};

			if (CumulativePeriod == xCumulativeDeltaPeriod.Bar)
			{
				bar.DeltaOpen = 0;
				bar.DeltaHigh = snapshot.RunningDeltaHigh;
				bar.DeltaLow = snapshot.RunningDeltaLow;
				bar.DeltaClose = barDelta;
			}
			else
			{
				bar.DeltaOpen = open;
				bar.DeltaHigh = open + snapshot.RunningDeltaHigh;
				bar.DeltaLow = open + snapshot.RunningDeltaLow;
				bar.DeltaClose = open + barDelta;
			}
			return bar;
		}

		private long ResolveDeltaOpen(bool firstSessionBar)
		{
			if (CumulativePeriod == xCumulativeDeltaPeriod.Bar)
				return 0;
			if (firstSessionBar)
				return 0;
			return mostRecentFinalizedBar != null ? mostRecentFinalizedBar.DeltaClose : priorSessionDeltaClose;
		}

		private bool IsFirstSessionBar(int absoluteBarIndex)
		{
			int barsAgo = CurrentBar - absoluteBarIndex;
			if (barsAgo < 0 || barsAgo > CurrentBar)
				return false;
			if (absoluteBarIndex == 0)
				return true;
			if (sessionFirstBarIndexes.Contains(absoluteBarIndex))
				return true;
			if (barsAgo == 0)
				return Bars.IsFirstBarOfSession;
			return false;
		}

		private void ValidateBar(xCumulativeDeltaBar bar)
		{
			StringBuilder message = new StringBuilder();
			if (CumulativePeriod == xCumulativeDeltaPeriod.Bar)
			{
				if (bar.DeltaOpen != 0)
					message.Append("bar-mode open not zero; ");
				if (bar.DeltaClose != bar.BarDelta && bar.SourceCapturedVolume != 0)
					message.Append("bar-mode close mismatch; ");
			}
			else if (bar.DeltaClose != bar.DeltaOpen + bar.BarDelta && bar.SourceCapturedVolume != 0)
				message.Append("session close mismatch; ");
			if (bar.DeltaHigh < Math.Max(bar.DeltaOpen, bar.DeltaClose))
				message.Append("delta high below open/close; ");
			if (bar.DeltaLow > Math.Min(bar.DeltaOpen, bar.DeltaClose))
				message.Append("delta low above open/close; ");
			if (bar.SourceBuyVolume - bar.SourceSellVolume != bar.BarDelta)
				message.Append("source delta mismatch; ");
			if (bar.SourceBuyVolume + bar.SourceSellVolume + bar.SourceUnknownVolume != bar.SourceCapturedVolume)
				message.Append("source volume sum mismatch; ");

			bar.ValidationPassed = message.Length == 0;
			bar.ValidationMessage = bar.ValidationPassed ? "OK" : message.ToString().Trim();
		}

		private void StoreBar(xCumulativeDeltaBar bar)
		{
			if (bar.IsSessionFirstBar && CumulativePeriod == xCumulativeDeltaPeriod.Session)
				sessionResetCount++;
			barsByIndex[bar.BarIndex] = bar;
			barOrder.Enqueue(bar.BarIndex);
			mostRecentFinalizedBar = bar;
			mostRecentFinalizedBarIndex = bar.BarIndex;
			priorSessionDeltaClose = bar.DeltaClose;
			while (barOrder.Count > MaxBarsRetained)
			{
				int remove = barOrder.Dequeue();
				if (remove != bar.BarIndex)
					barsByIndex.Remove(remove);
			}
		}

		private void SetSeriesForBar(xCumulativeDeltaBar bar)
		{
			int barsAgo = CurrentBar - bar.BarIndex;
			if (barsAgo < 0 || barsAgo > CurrentBar)
				return;
			DeltaOpenSeries[barsAgo] = bar.DeltaOpen;
			DeltaHighSeries[barsAgo] = bar.DeltaHigh;
			DeltaLowSeries[barsAgo] = bar.DeltaLow;
			DeltaCloseSeries[barsAgo] = bar.DeltaClose;
			BarDeltaSeries[barsAgo] = bar.BarDelta;
		}

		public override void OnCalculateMinMax()
		{
			base.OnCalculateMinMax();
			if (ChartBars == null)
				return;

			bool found = false;
			double min = ShowZeroLine ? 0 : double.MaxValue;
			double max = ShowZeroLine ? 0 : double.MinValue;
			for (int index = ChartBars.FromIndex; index <= ChartBars.ToIndex; index++)
			{
				xCumulativeDeltaBar bar = GetRenderBar(index);
				if (bar == null)
					continue;

				found = true;
				min = Math.Min(min, Math.Min(bar.DeltaLow, Math.Min(bar.DeltaOpen, bar.DeltaClose)));
				max = Math.Max(max, Math.Max(bar.DeltaHigh, Math.Max(bar.DeltaOpen, bar.DeltaClose)));
			}

			if (!found)
				return;

			if (Math.Abs(max - min) < 1)
			{
				max += 1;
				min -= 1;
			}
			else
			{
				double padding = Math.Max(1, (max - min) * 0.08);
				max += padding;
				min -= padding;
			}

			MinValue = min;
			MaxValue = max;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (ChartBars == null || RenderTarget == null)
				return;

			if (ShowZeroLine)
				DrawZeroLine(chartScale);

			if (DisplayStyle == xCumulativeDeltaDisplayStyle.Candles || DisplayStyle == xCumulativeDeltaDisplayStyle.CandlesAndLine)
				DrawCandles(chartControl, chartScale);
			if (DisplayStyle == xCumulativeDeltaDisplayStyle.Line || DisplayStyle == xCumulativeDeltaDisplayStyle.CandlesAndLine)
				DrawDeltaLine(chartControl, chartScale);
			if (ShowDeltaZeroCrossMarkers || ShowPriceDeltaDisagreementMarkers)
				DrawMarkers(chartControl, chartScale);
		}

		private void DrawCandles(ChartControl chartControl, ChartScale chartScale)
		{
			SharpDX.Direct2D1.Brush up = UpCandleBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush down = DownCandleBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush outline = CandleOutlineBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush wick = WickBrush.ToDxBrush(RenderTarget);
			try
			{
				for (int index = ChartBars.FromIndex; index <= ChartBars.ToIndex; index++)
				{
					xCumulativeDeltaBar bar = GetRenderBar(index);
					if (bar == null)
						continue;
					float x = chartControl.GetXByBarIndex(ChartBars, index);
					float yOpen = chartScale.GetYByValue(bar.DeltaOpen);
					float yHigh = chartScale.GetYByValue(bar.DeltaHigh);
					float yLow = chartScale.GetYByValue(bar.DeltaLow);
					float yClose = chartScale.GetYByValue(bar.DeltaClose);
					RenderTarget.DrawLine(new SharpDX.Vector2(x, yHigh), new SharpDX.Vector2(x, yLow), wick, Math.Max(1, WickWidth));
					float top = Math.Min(yOpen, yClose);
					float bottom = Math.Max(yOpen, yClose);
					if (bottom - top < 1)
						bottom = top + 1;
					float width = Math.Max(1, CandleBodyWidth);
					SharpDX.RectangleF rect = new SharpDX.RectangleF(x - width / 2f, top, width, bottom - top);
					RenderTarget.FillRectangle(rect, bar.DeltaClose >= bar.DeltaOpen ? up : down);
					RenderTarget.DrawRectangle(rect, outline, Math.Max(1, CandleOutlineWidth));
				}
			}
			finally
			{
				up.Dispose();
				down.Dispose();
				outline.Dispose();
				wick.Dispose();
			}
		}

		private void DrawDeltaLine(ChartControl chartControl, ChartScale chartScale)
		{
			SharpDX.Direct2D1.Brush line = DeltaLineBrush.ToDxBrush(RenderTarget);
			try
			{
				bool havePrior = false;
				SharpDX.Vector2 prior = new SharpDX.Vector2();
				for (int index = ChartBars.FromIndex; index <= ChartBars.ToIndex; index++)
				{
					xCumulativeDeltaBar bar = GetRenderBar(index);
					if (bar == null)
					{
						havePrior = false;
						continue;
					}
					SharpDX.Vector2 point = new SharpDX.Vector2(chartControl.GetXByBarIndex(ChartBars, index), chartScale.GetYByValue(bar.DeltaClose));
					if (havePrior)
						RenderTarget.DrawLine(prior, point, line, Math.Max(1, DeltaLineWidth));
					prior = point;
					havePrior = true;
				}
			}
			finally
			{
				line.Dispose();
			}
		}

		private void DrawZeroLine(ChartScale chartScale)
		{
			SharpDX.Direct2D1.Brush zero = ZeroLineBrush.ToDxBrush(RenderTarget);
			try
			{
				float y = chartScale.GetYByValue(0);
				RenderTarget.DrawLine(new SharpDX.Vector2(ChartPanel.X, y), new SharpDX.Vector2(ChartPanel.X + ChartPanel.W, y), zero, Math.Max(1, ZeroLineWidth));
			}
			finally
			{
				zero.Dispose();
			}
		}

		private void DrawMarkers(ChartControl chartControl, ChartScale chartScale)
		{
			SharpDX.Direct2D1.Brush marker = CandleOutlineBrush.ToDxBrush(RenderTarget);
			try
			{
				for (int index = ChartBars.FromIndex; index <= ChartBars.ToIndex; index++)
				{
					xCumulativeDeltaBar bar = GetRenderBar(index);
					if (bar == null)
						continue;

					bool draw = false;
					if (ShowDeltaZeroCrossMarkers && CumulativePeriod == xCumulativeDeltaPeriod.Session)
					{
						xCumulativeDeltaBar prior;
						if (barsByIndex.TryGetValue(index - 1, out prior) && ((prior.DeltaClose < 0 && bar.DeltaClose >= 0) || (prior.DeltaClose > 0 && bar.DeltaClose <= 0)))
							draw = true;
					}

					if (ShowPriceDeltaDisagreementMarkers)
					{
						int barsAgo = CurrentBar - index;
						if (barsAgo >= 0 && barsAgo <= CurrentBar)
						{
							int priceSign = Sign(Closes[0][barsAgo] - Opens[0][barsAgo]);
							int deltaSign = Sign(bar.BarDelta);
							if (priceSign != 0 && deltaSign != 0 && priceSign != deltaSign)
								draw = true;
						}
					}

					if (draw)
					{
						float x = chartControl.GetXByBarIndex(ChartBars, index);
						float y = chartScale.GetYByValue(bar.DeltaClose);
						RenderTarget.FillEllipse(new SharpDX.Direct2D1.Ellipse(new SharpDX.Vector2(x, y), 3, 3), marker);
					}
				}
			}
			finally
			{
				marker.Dispose();
			}
		}

		private xCumulativeDeltaBar GetRenderBar(int absoluteIndex)
		{
			if (activeBar != null && activeBar.BarIndex == absoluteIndex)
				return activeBar;
			xCumulativeDeltaBar bar;
			return barsByIndex.TryGetValue(absoluteIndex, out bar) ? bar : null;
		}

		private int Sign(double value)
		{
			if (value > 0)
				return 1;
			if (value < 0)
				return -1;
			return 0;
		}

		private void WriteBar(xCumulativeDeltaBar bar)
		{
			if (writer == null || bar == null)
				return;
			lock (writerLock)
			{
				try
				{
					string[] columns =
					{
						bar.BarIndex.ToString(CultureInfo.InvariantCulture), FormatTime(bar.NinjaBarTime), FormatTime(bar.BarOpenTime), FormatTime(bar.BarCloseTime),
						CumulativePeriod.ToString(), bar.SourceCapturedVolume.ToString(CultureInfo.InvariantCulture), bar.SourceBuyVolume.ToString(CultureInfo.InvariantCulture),
						bar.SourceSellVolume.ToString(CultureInfo.InvariantCulture), bar.SourceUnknownVolume.ToString(CultureInfo.InvariantCulture), bar.BarDelta.ToString(CultureInfo.InvariantCulture),
						bar.DeltaOpen.ToString(CultureInfo.InvariantCulture), bar.DeltaHigh.ToString(CultureInfo.InvariantCulture), bar.DeltaLow.ToString(CultureInfo.InvariantCulture), bar.DeltaClose.ToString(CultureInfo.InvariantCulture),
						bar.IsSessionFirstBar.ToString(), bar.IsPartialSourceBar.ToString(), bar.IsRealtimeCaptured.ToString(), bar.ValidationPassed.ToString(), bar.ValidationMessage,
						bar.SourceCaptureCompletenessPassed.ToString(), bar.SourceVolumeDifference.ToString(CultureInfo.InvariantCulture), bar.SourcePendingEventsAssigned.ToString(CultureInfo.InvariantCulture), bar.IncompleteBarHandlingApplied
					};
					writer.WriteLine(ToCsv(columns));
					if (FlushOnBarFinalization)
						writer.Flush();
				}
				catch (Exception ex)
				{
					Print("xPvaCumulativeDelta export failed: " + ex.Message);
				}
			}
		}

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xPvaCumulativeDelta");
			Directory.CreateDirectory(exportFolder);
			fileInstrument = SanitizeFileName(Instrument != null ? Instrument.FullName : "UnknownInstrument");
			fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			exportPath = Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xPvaCumulativeDelta_BARS_{0}_{1}.csv", fileInstrument, fileStamp));
		}

		private void OpenWriter()
		{
			if (!ExportCsv)
				return;
			try
			{
				writer = new StreamWriter(exportPath, false, Encoding.UTF8);
				writer.WriteLine("BarIndex,NinjaBarTime,BarOpenTime,BarCloseTime,CumulativePeriod,SourceCapturedVolume,SourceBuyVolume,SourceSellVolume,SourceUnknownVolume,BarDelta,DeltaOpen,DeltaHigh,DeltaLow,DeltaClose,IsSessionFirstBar,IsPartialSourceBar,IsRealtimeCaptured,ValidationPassed,ValidationMessage,SourceCaptureCompletenessPassed,SourceVolumeDifference,SourcePendingEventsAssigned,IncompleteBarHandlingApplied");
			}
			catch (Exception ex)
			{
				Print("xPvaCumulativeDelta export open failed: " + ex.Message);
				CloseWriter();
			}
		}

		private void CloseWriter()
		{
			lock (writerLock)
			{
				try
				{
					if (writer != null)
					{
						writer.Flush();
						writer.Close();
						writer = null;
					}
				}
				catch (Exception ex)
				{
					Print("xPvaCumulativeDelta export close failed: " + ex.Message);
				}
			}
		}

		private xOrderFlowRegistryKey BuildRegistryKey()
		{
			string instrumentFullName = Instrument != null ? Instrument.FullName : string.Empty;
			string masterInstrumentName = Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : string.Empty;
			string tradingHoursName = string.Empty;
			try { if (Bars != null && Bars.TradingHours != null) tradingHoursName = Bars.TradingHours.Name; } catch { tradingHoursName = string.Empty; }
			int value2 = 0;
			try { value2 = BarsPeriod != null ? BarsPeriod.Value2 : 0; } catch { value2 = 0; }
			return new xOrderFlowRegistryKey(instrumentFullName, masterInstrumentName, BarsPeriod != null ? BarsPeriod.BarsPeriodType : BarsPeriodType.Minute, BarsPeriod != null ? BarsPeriod.Value : 0, value2, tradingHoursName, string.Empty, RegistryInstanceKey);
		}

		private void UpdateDiagnosticsPanel(bool force)
		{
			if (!ShowDiagnosticsPanel)
			{
				if (diagnosticsTextVisible)
				{
					RemoveDrawObject("xPvaCumulativeDeltaDiagnostics");
					diagnosticsTextVisible = false;
				}
				RemoveDrawObject("xPvaCumulativeDeltaHistorical");
				return;
			}
			DateTime now = DateTime.Now;
			if (!force && lastPanelRefresh != DateTime.MinValue && (now - lastPanelRefresh).TotalMilliseconds < 500)
				return;
			lastPanelRefresh = now;
			xCumulativeDeltaBar b = activeBar ?? mostRecentFinalizedBar;
			string text = string.Format(CultureInfo.InvariantCulture,
				"xPvaCumulativeDelta\nInstrument: {0}\nConnection/provider: {1}\nRegistry key: {2}\nActive core publisher status: {3}\nPublisher ambiguity: {4}\nCumulative period: {5}\nDisplay style: {6}\nCurrent bar index: {7}\nCurrent bar delta: {8}\nCurrent delta open: {9}\nCurrent delta high: {10}\nCurrent delta low: {11}\nCurrent delta close: {12}\nCurrent captured volume: {13}\nCurrent unknown volume: {14}\nMost recent finalized bar index: {15}\nMost recent validation result: {16}\nSession reset count: {17}\nRegistry misses: {18}\nExport path: {19}",
				Instrument != null ? Instrument.FullName : string.Empty, string.Empty, registryKey.ToString(), lastActivePublisherState, lastAmbiguousState,
				CumulativePeriod, DisplayStyle, CurrentBar, b != null ? b.BarDelta.ToString(CultureInfo.InvariantCulture) : "", b != null ? b.DeltaOpen.ToString(CultureInfo.InvariantCulture) : "",
				b != null ? b.DeltaHigh.ToString(CultureInfo.InvariantCulture) : "", b != null ? b.DeltaLow.ToString(CultureInfo.InvariantCulture) : "", b != null ? b.DeltaClose.ToString(CultureInfo.InvariantCulture) : "",
				b != null ? b.SourceCapturedVolume.ToString(CultureInfo.InvariantCulture) : "", b != null ? b.SourceUnknownVolume.ToString(CultureInfo.InvariantCulture) : "",
				mostRecentFinalizedBarIndex, mostRecentFinalizedBar != null ? mostRecentFinalizedBar.ValidationMessage : "", sessionResetCount, registryMisses, exportPath);
			Draw.TextFixed(this, "xPvaCumulativeDeltaDiagnostics", text, TextPosition.TopLeft);
			diagnosticsTextVisible = true;
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
		[Display(Name = "Cumulative period", GroupName = "Display", Order = 2)]
		public xCumulativeDeltaPeriod CumulativePeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Display style", GroupName = "Display", Order = 3)]
		public xCumulativeDeltaDisplayStyle DisplayStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Historical display mode", GroupName = "Display", Order = 4)]
		public xHistoricalDeltaDisplayMode HistoricalDisplayMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show zero line", GroupName = "Display", Order = 5)]
		public bool ShowZeroLine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show delta zero-cross markers", GroupName = "Markers", Order = 6)]
		public bool ShowDeltaZeroCrossMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show price/delta disagreement markers", GroupName = "Markers", Order = 7)]
		public bool ShowPriceDeltaDisagreementMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export CSV", GroupName = "Export", Order = 8)]
		public bool ExportCsv { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flush on bar finalization", GroupName = "Export", Order = 9)]
		public bool FlushOnBarFinalization { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export partial on terminate", GroupName = "Export", Order = 10)]
		public bool ExportPartialOnTerminate { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max bars retained", GroupName = "Memory", Order = 11)]
		public int MaxBarsRetained { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 12)]
		public bool ShowDiagnosticsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable native comparison columns", GroupName = "Diagnostics", Order = 13)]
		public bool EnableNativeComparisonColumns { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Incomplete bar handling", GroupName = "Integrity", Order = 14)]
		public xIncompleteBarHandling IncompleteBarHandling { get; set; }

		[XmlIgnore]
		[Display(Name = "Up candle brush", GroupName = "Brushes", Order = 20)]
		public Brush UpCandleBrush { get; set; }
		[Browsable(false)]
		public string UpCandleBrushSerializable { get { return Serialize.BrushToString(UpCandleBrush); } set { UpCandleBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Down candle brush", GroupName = "Brushes", Order = 21)]
		public Brush DownCandleBrush { get; set; }
		[Browsable(false)]
		public string DownCandleBrushSerializable { get { return Serialize.BrushToString(DownCandleBrush); } set { DownCandleBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Candle outline brush", GroupName = "Brushes", Order = 22)]
		public Brush CandleOutlineBrush { get; set; }
		[Browsable(false)]
		public string CandleOutlineBrushSerializable { get { return Serialize.BrushToString(CandleOutlineBrush); } set { CandleOutlineBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Wick brush", GroupName = "Brushes", Order = 23)]
		public Brush WickBrush { get; set; }
		[Browsable(false)]
		public string WickBrushSerializable { get { return Serialize.BrushToString(WickBrush); } set { WickBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Delta line brush", GroupName = "Brushes", Order = 24)]
		public Brush DeltaLineBrush { get; set; }
		[Browsable(false)]
		public string DeltaLineBrushSerializable { get { return Serialize.BrushToString(DeltaLineBrush); } set { DeltaLineBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Zero line brush", GroupName = "Brushes", Order = 25)]
		public Brush ZeroLineBrush { get; set; }
		[Browsable(false)]
		public string ZeroLineBrushSerializable { get { return Serialize.BrushToString(ZeroLineBrush); } set { ZeroLineBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Candle body width", GroupName = "Widths", Order = 30)]
		public int CandleBodyWidth { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Candle outline width", GroupName = "Widths", Order = 31)]
		public int CandleOutlineWidth { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Wick width", GroupName = "Widths", Order = 32)]
		public int WickWidth { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Delta line width", GroupName = "Widths", Order = 33)]
		public int DeltaLineWidth { get; set; }
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Zero line width", GroupName = "Widths", Order = 34)]
		public int ZeroLineWidth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaOrderFlow.xPvaCumulativeDelta[] cachexPvaCumulativeDelta;
		public xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			return xPvaCumulativeDelta(Input, registryInstanceKey, cumulativePeriod, displayStyle, historicalDisplayMode, showZeroLine, showDeltaZeroCrossMarkers, showPriceDeltaDisagreementMarkers, exportCsv, flushOnBarFinalization, exportPartialOnTerminate, maxBarsRetained, showDiagnosticsPanel, enableNativeComparisonColumns, incompleteBarHandling, candleBodyWidth, candleOutlineWidth, wickWidth, deltaLineWidth, zeroLineWidth);
		}

		public xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(ISeries<double> input, string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			if (cachexPvaCumulativeDelta != null)
				for (int idx = 0; idx < cachexPvaCumulativeDelta.Length; idx++)
					if (cachexPvaCumulativeDelta[idx] != null && cachexPvaCumulativeDelta[idx].RegistryInstanceKey == registryInstanceKey && cachexPvaCumulativeDelta[idx].CumulativePeriod == cumulativePeriod && cachexPvaCumulativeDelta[idx].DisplayStyle == displayStyle && cachexPvaCumulativeDelta[idx].HistoricalDisplayMode == historicalDisplayMode && cachexPvaCumulativeDelta[idx].ShowZeroLine == showZeroLine && cachexPvaCumulativeDelta[idx].ShowDeltaZeroCrossMarkers == showDeltaZeroCrossMarkers && cachexPvaCumulativeDelta[idx].ShowPriceDeltaDisagreementMarkers == showPriceDeltaDisagreementMarkers && cachexPvaCumulativeDelta[idx].ExportCsv == exportCsv && cachexPvaCumulativeDelta[idx].FlushOnBarFinalization == flushOnBarFinalization && cachexPvaCumulativeDelta[idx].ExportPartialOnTerminate == exportPartialOnTerminate && cachexPvaCumulativeDelta[idx].MaxBarsRetained == maxBarsRetained && cachexPvaCumulativeDelta[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexPvaCumulativeDelta[idx].EnableNativeComparisonColumns == enableNativeComparisonColumns && cachexPvaCumulativeDelta[idx].IncompleteBarHandling == incompleteBarHandling && cachexPvaCumulativeDelta[idx].CandleBodyWidth == candleBodyWidth && cachexPvaCumulativeDelta[idx].CandleOutlineWidth == candleOutlineWidth && cachexPvaCumulativeDelta[idx].WickWidth == wickWidth && cachexPvaCumulativeDelta[idx].DeltaLineWidth == deltaLineWidth && cachexPvaCumulativeDelta[idx].ZeroLineWidth == zeroLineWidth && cachexPvaCumulativeDelta[idx].EqualsInput(input))
						return cachexPvaCumulativeDelta[idx];
			return CacheIndicator<xPvaOrderFlow.xPvaCumulativeDelta>(new xPvaOrderFlow.xPvaCumulativeDelta(){ RegistryInstanceKey = registryInstanceKey, CumulativePeriod = cumulativePeriod, DisplayStyle = displayStyle, HistoricalDisplayMode = historicalDisplayMode, ShowZeroLine = showZeroLine, ShowDeltaZeroCrossMarkers = showDeltaZeroCrossMarkers, ShowPriceDeltaDisagreementMarkers = showPriceDeltaDisagreementMarkers, ExportCsv = exportCsv, FlushOnBarFinalization = flushOnBarFinalization, ExportPartialOnTerminate = exportPartialOnTerminate, MaxBarsRetained = maxBarsRetained, ShowDiagnosticsPanel = showDiagnosticsPanel, EnableNativeComparisonColumns = enableNativeComparisonColumns, IncompleteBarHandling = incompleteBarHandling, CandleBodyWidth = candleBodyWidth, CandleOutlineWidth = candleOutlineWidth, WickWidth = wickWidth, DeltaLineWidth = deltaLineWidth, ZeroLineWidth = zeroLineWidth }, input, ref cachexPvaCumulativeDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			return indicator.xPvaCumulativeDelta(Input, registryInstanceKey, cumulativePeriod, displayStyle, historicalDisplayMode, showZeroLine, showDeltaZeroCrossMarkers, showPriceDeltaDisagreementMarkers, exportCsv, flushOnBarFinalization, exportPartialOnTerminate, maxBarsRetained, showDiagnosticsPanel, enableNativeComparisonColumns, incompleteBarHandling, candleBodyWidth, candleOutlineWidth, wickWidth, deltaLineWidth, zeroLineWidth);
		}

		public Indicators.xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(ISeries<double> input , string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			return indicator.xPvaCumulativeDelta(input, registryInstanceKey, cumulativePeriod, displayStyle, historicalDisplayMode, showZeroLine, showDeltaZeroCrossMarkers, showPriceDeltaDisagreementMarkers, exportCsv, flushOnBarFinalization, exportPartialOnTerminate, maxBarsRetained, showDiagnosticsPanel, enableNativeComparisonColumns, incompleteBarHandling, candleBodyWidth, candleOutlineWidth, wickWidth, deltaLineWidth, zeroLineWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			return indicator.xPvaCumulativeDelta(Input, registryInstanceKey, cumulativePeriod, displayStyle, historicalDisplayMode, showZeroLine, showDeltaZeroCrossMarkers, showPriceDeltaDisagreementMarkers, exportCsv, flushOnBarFinalization, exportPartialOnTerminate, maxBarsRetained, showDiagnosticsPanel, enableNativeComparisonColumns, incompleteBarHandling, candleBodyWidth, candleOutlineWidth, wickWidth, deltaLineWidth, zeroLineWidth);
		}

		public Indicators.xPvaOrderFlow.xPvaCumulativeDelta xPvaCumulativeDelta(ISeries<double> input , string registryInstanceKey, xCumulativeDeltaPeriod cumulativePeriod, xCumulativeDeltaDisplayStyle displayStyle, xHistoricalDeltaDisplayMode historicalDisplayMode, bool showZeroLine, bool showDeltaZeroCrossMarkers, bool showPriceDeltaDisagreementMarkers, bool exportCsv, bool flushOnBarFinalization, bool exportPartialOnTerminate, int maxBarsRetained, bool showDiagnosticsPanel, bool enableNativeComparisonColumns, xIncompleteBarHandling incompleteBarHandling, int candleBodyWidth, int candleOutlineWidth, int wickWidth, int deltaLineWidth, int zeroLineWidth)
		{
			return indicator.xPvaCumulativeDelta(input, registryInstanceKey, cumulativePeriod, displayStyle, historicalDisplayMode, showZeroLine, showDeltaZeroCrossMarkers, showPriceDeltaDisagreementMarkers, exportCsv, flushOnBarFinalization, exportPartialOnTerminate, maxBarsRetained, showDiagnosticsPanel, enableNativeComparisonColumns, incompleteBarHandling, candleBodyWidth, candleOutlineWidth, wickWidth, deltaLineWidth, zeroLineWidth);
		}
	}
}

#endregion
