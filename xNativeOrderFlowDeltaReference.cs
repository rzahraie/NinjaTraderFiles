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
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDiagnostics
{
	public class xNativeOrderFlowDeltaReference : Indicator
	{
		private OrderFlowCumulativeDelta nativeDelta;
		private StreamWriter writer;
		private readonly object writerLock = new object();
		private string exportFolder;
		private string exportPath;
		private string fileInstrument;
		private string fileStamp;
		private int mostRecentExportedBar = -1;
		private double mostRecentDeltaOpen;
		private double mostRecentDeltaHigh;
		private double mostRecentDeltaLow;
		private double mostRecentDeltaClose;
		private DateTime lastPanelRefresh;
		private bool nativeUnavailable;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xNativeOrderFlowDeltaReference";
				Description = "Diagnostic export of NinjaTrader native Order Flow Cumulative Delta values for bar-by-bar comparison.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				ExportCsv = true;
				ShowDiagnosticsPanel = true;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				EnsureExportFolder();
				try
				{
					nativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
					OpenWriter();
				}
				catch (Exception ex)
				{
					nativeUnavailable = true;
					Print("xNativeOrderFlowDeltaReference requires licensed NinjaTrader Order Flow+. Native indicator initialization failed: " + ex.Message);
				}
			}
			else if (State == State.Terminated)
			{
				CloseWriter();
			}
		}

		protected override void OnBarUpdate()
		{
			if (nativeUnavailable || nativeDelta == null)
			{
				UpdateDiagnosticsPanel(false);
				return;
			}

			if (BarsInProgress == 1)
			{
				try
				{
					nativeDelta.Update(nativeDelta.BarsArray[1].Count - 1, 1);
				}
				catch (Exception ex)
				{
					nativeUnavailable = true;
					Print("xNativeOrderFlowDeltaReference native update failed: " + ex.Message);
				}
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 1)
				return;

			ExportCompletedBar(CurrentBar - 1);
			UpdateDiagnosticsPanel(false);
		}

		private void ExportCompletedBar(int barIndex)
		{
			if (barIndex < 0 || barIndex <= mostRecentExportedBar)
				return;

			int barsAgo = CurrentBar - barIndex;
			if (barsAgo < 0 || barsAgo > CurrentBar)
				return;

			try
			{
				double deltaOpen = nativeDelta.DeltaOpen[barsAgo];
				double deltaHigh = nativeDelta.DeltaHigh[barsAgo];
				double deltaLow = nativeDelta.DeltaLow[barsAgo];
				double deltaClose = nativeDelta.DeltaClose[barsAgo];
				mostRecentDeltaOpen = deltaOpen;
				mostRecentDeltaHigh = deltaHigh;
				mostRecentDeltaLow = deltaLow;
				mostRecentDeltaClose = deltaClose;

				if (writer != null)
				{
					string[] columns =
					{
						barIndex.ToString(CultureInfo.InvariantCulture),
						FormatTime(Times[0][barsAgo]),
						FormatTime(ResolveBarOpenTime(barsAgo)),
						FormatTime(Times[0][barsAgo]),
						FormatDouble(Opens[0][barsAgo]),
						FormatDouble(Highs[0][barsAgo]),
						FormatDouble(Lows[0][barsAgo]),
						FormatDouble(Closes[0][barsAgo]),
						ToLongVolume(Volumes[0][barsAgo]).ToString(CultureInfo.InvariantCulture),
						FormatDouble(deltaOpen),
						FormatDouble(deltaHigh),
						FormatDouble(deltaLow),
						FormatDouble(deltaClose),
						FormatDouble(deltaClose - deltaOpen),
						(State == State.Historical).ToString(),
						(State == State.Realtime).ToString()
					};
					string row = ToCsv(columns);
					lock (writerLock)
					{
						writer.WriteLine(row);
						writer.Flush();
					}
				}

				mostRecentExportedBar = barIndex;
			}
			catch (Exception ex)
			{
				Print(string.Format(CultureInfo.InvariantCulture, "xNativeOrderFlowDeltaReference export failed for BarIndex={0}: {1}", barIndex, ex.Message));
			}
		}

		private DateTime ResolveBarOpenTime(int barsAgo)
		{
			DateTime closeTime = Times[0][barsAgo];
			if (BarsPeriod != null && BarsPeriod.BarsPeriodType == BarsPeriodType.Minute)
				return closeTime.AddMinutes(-BarsPeriod.Value);
			if (barsAgo + 1 <= CurrentBar)
				return Times[0][barsAgo + 1];
			return closeTime;
		}

		private void EnsureExportFolder()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			exportFolder = Path.Combine(documents, "NinjaTrader 8", "export", "xNativeOrderFlowDeltaReference");
			Directory.CreateDirectory(exportFolder);
			fileInstrument = SanitizeFileName(Instrument != null ? Instrument.FullName : "UnknownInstrument");
			fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			exportPath = Path.Combine(exportFolder, string.Format(CultureInfo.InvariantCulture, "xNativeOrderFlowDeltaReference_BARS_{0}_{1}.csv", fileInstrument, fileStamp));
		}

		private void OpenWriter()
		{
			if (!ExportCsv || nativeUnavailable)
				return;

			try
			{
				writer = new StreamWriter(exportPath, false, Encoding.UTF8);
				writer.WriteLine("BarIndex,NinjaBarTime,BarOpenTime,BarCloseTime,Open,High,Low,Close,NinjaTraderVolume,NativeDeltaOpen,NativeDeltaHigh,NativeDeltaLow,NativeDeltaClose,NativeDeltaBarChange,IsHistorical,IsRealtime");
			}
			catch (Exception ex)
			{
				Print("xNativeOrderFlowDeltaReference export open failed: " + ex.Message);
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
					Print("xNativeOrderFlowDeltaReference export close failed: " + ex.Message);
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

			string text = string.Format(CultureInfo.InvariantCulture,
				"Native Order Flow Delta Reference\nInstrument: {0}\nCurrent bar index: {1}\nMost recent exported bar: {2}\nNative delta open: {3}\nNative delta high: {4}\nNative delta low: {5}\nNative delta close: {6}\nExport path: {7}",
				Instrument != null ? Instrument.FullName : string.Empty,
				CurrentBar,
				mostRecentExportedBar,
				FormatDouble(mostRecentDeltaOpen),
				FormatDouble(mostRecentDeltaHigh),
				FormatDouble(mostRecentDeltaLow),
				FormatDouble(mostRecentDeltaClose),
				exportPath ?? string.Empty);
			Draw.TextFixed(this, "xNativeOrderFlowDeltaReferenceDiagnostics", text, TextPosition.BottomLeft);
		}

		private long ToLongVolume(double value)
		{
			return (long)Math.Round(value, MidpointRounding.AwayFromZero);
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
			if (double.IsNaN(value) || double.IsInfinity(value))
				value = 0;
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
		[Display(Name = "Export CSV", GroupName = "Export", Order = 1)]
		public bool ExportCsv { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show diagnostics panel", GroupName = "Display", Order = 2)]
		public bool ShowDiagnosticsPanel { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public string ExportPath { get { return exportPath; } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaDiagnostics.xNativeOrderFlowDeltaReference[] cachexNativeOrderFlowDeltaReference;
		public xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(bool exportCsv, bool showDiagnosticsPanel)
		{
			return xNativeOrderFlowDeltaReference(Input, exportCsv, showDiagnosticsPanel);
		}

		public xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(ISeries<double> input, bool exportCsv, bool showDiagnosticsPanel)
		{
			if (cachexNativeOrderFlowDeltaReference != null)
				for (int idx = 0; idx < cachexNativeOrderFlowDeltaReference.Length; idx++)
					if (cachexNativeOrderFlowDeltaReference[idx] != null && cachexNativeOrderFlowDeltaReference[idx].ExportCsv == exportCsv && cachexNativeOrderFlowDeltaReference[idx].ShowDiagnosticsPanel == showDiagnosticsPanel && cachexNativeOrderFlowDeltaReference[idx].EqualsInput(input))
						return cachexNativeOrderFlowDeltaReference[idx];
			return CacheIndicator<xPvaDiagnostics.xNativeOrderFlowDeltaReference>(new xPvaDiagnostics.xNativeOrderFlowDeltaReference(){ ExportCsv = exportCsv, ShowDiagnosticsPanel = showDiagnosticsPanel }, input, ref cachexNativeOrderFlowDeltaReference);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(bool exportCsv, bool showDiagnosticsPanel)
		{
			return indicator.xNativeOrderFlowDeltaReference(Input, exportCsv, showDiagnosticsPanel);
		}

		public Indicators.xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(ISeries<double> input , bool exportCsv, bool showDiagnosticsPanel)
		{
			return indicator.xNativeOrderFlowDeltaReference(input, exportCsv, showDiagnosticsPanel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(bool exportCsv, bool showDiagnosticsPanel)
		{
			return indicator.xNativeOrderFlowDeltaReference(Input, exportCsv, showDiagnosticsPanel);
		}

		public Indicators.xPvaDiagnostics.xNativeOrderFlowDeltaReference xNativeOrderFlowDeltaReference(ISeries<double> input , bool exportCsv, bool showDiagnosticsPanel)
		{
			return indicator.xNativeOrderFlowDeltaReference(input, exportCsv, showDiagnosticsPanel);
		}
	}
}

#endregion
