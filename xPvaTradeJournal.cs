#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder;
using NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow;
using NinjaTrader.NinjaScript.Indicators.xPvaTradeJournal;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaTradeJournal
{
	public class xPvaTradeJournal : Indicator, xITradeJournalContextProvider
	{
		private xPvaTradeJournalService service;
		private DateTime lastStatusRefresh;
		private DateTime lastUpdatedUtc;
		private string lastStatusText;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaTradeJournal";
				Description = "Automatic APVA trade journal, campaign reconstructor, SQLite recorder, and context snapshot provider.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;

				AccountName = "Sim101";
				CaptureAllAccounts = false;
				ContextBars = 30;
				PreferredBarsPeriodMinutes = 5;
				PostTradeTagWindowSeconds = 300;
				MaxEmergencyQueueRecords = 100000;
				LegMatchingMethod = xTradeJournalLegMatchingMethod.FIFO;
				ShowStatusPanel = true;
				StatusPanelPosition = TextPosition.TopRight;
				DiagnosticLevel = xTradeJournalDiagnosticLevel.Normal;
				RegistryInstanceKey = "APVA";
			}
			else if (State == State.DataLoaded)
			{
				service = xPvaTradeJournalService.Acquire();
				service.Configure(AccountName, CaptureAllAccounts, ContextBars, PreferredBarsPeriodMinutes, PostTradeTagWindowSeconds, MaxEmergencyQueueRecords, DiagnosticLevel, LegMatchingMethod);
				service.RegisterContextProvider(this);
			}
			else if (State == State.Terminated)
			{
				if (service != null)
				{
					service.UnregisterContextProvider(this);
					service.Release();
					service = null;
				}
				RemoveDrawObject("xPvaTradeJournalStatus");
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0)
				return;
			lastUpdatedUtc = DateTime.UtcNow;
			if (service != null)
				service.Configure(AccountName, CaptureAllAccounts, ContextBars, PreferredBarsPeriodMinutes, PostTradeTagWindowSeconds, MaxEmergencyQueueRecords, DiagnosticLevel, LegMatchingMethod);
			UpdateStatusPanel(false);
		}

		private void UpdateStatusPanel(bool force)
		{
			if (!ShowStatusPanel)
			{
				RemoveDrawObject("xPvaTradeJournalStatus");
				return;
			}

			DateTime now = DateTime.Now;
			if (!force && lastStatusRefresh != DateTime.MinValue && (now - lastStatusRefresh).TotalMilliseconds < 1000)
				return;
			lastStatusRefresh = now;

			xTradeJournalStatus s = service != null ? service.GetStatus() : new xTradeJournalStatus { AccountName = AccountName };
			string text = string.Format(CultureInfo.InvariantCulture,
				"xPvaTradeJournal\nAccount: {0}\nDatabase: {1}\nOpen campaign: {2}\nToday: {3} campaigns\nGross: {4}\nNet: {5}\nLast write: {6}\nContext: {7}\nErrors: {8}{9}",
				s.AccountName,
				s.DatabaseConnected ? "Connected" : "Unavailable",
				string.IsNullOrEmpty(s.OpenCampaignText) ? "Flat" : s.OpenCampaignText,
				s.TodayCompletedCampaigns,
				FormatCurrency(s.TodayGrossPnL),
				FormatCurrency(s.TodayNetPnL),
				s.LastWriteLocal == DateTime.MinValue ? "" : s.LastWriteLocal.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
				s.ContextAvailable ? "Available" : "Unavailable",
				s.ErrorCount,
				string.IsNullOrEmpty(s.LastError) ? string.Empty : "\nLast error: " + s.LastError);
			lastStatusText = text;
			Draw.TextFixed(this, "xPvaTradeJournalStatus", text, StatusPanelPosition);
		}

		private string FormatCurrency(double value)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}${1:0}", value < 0 ? "-" : string.Empty, Math.Abs(value));
		}

		public xTradeJournalContextSnapshot CaptureSnapshot(string accountName, string instrumentFullName, int contextBars)
		{
			xTradeJournalContextSnapshot snapshot = new xTradeJournalContextSnapshot();
			snapshot.SnapshotId = Guid.NewGuid().ToString("N");
			snapshot.Account = accountName;
			snapshot.Instrument = instrumentFullName;
			snapshot.TimestampUtc = DateTime.UtcNow;
			snapshot.TimestampLocal = DateTime.Now;
			snapshot.BarsPeriod = BarsPeriod != null ? BarsPeriod.ToString() : string.Empty;
			snapshot.BarsPeriodMinutes = BarsPeriodMinutes;
			snapshot.TradingHoursName = Bars != null && Bars.TradingHours != null ? Bars.TradingHours.Name : string.Empty;
			snapshot.CurrentBarIndex = CurrentBar;
			snapshot.CurrentBarTime = CurrentBar >= 0 ? Time[0] : DateTime.MinValue;

			try
			{
				if (!string.Equals(InstrumentFullName, instrumentFullName, StringComparison.OrdinalIgnoreCase))
				{
					snapshot.ContextAvailable = false;
					snapshot.ErrorMessage = "Provider instrument mismatch";
					snapshot.Json = BuildUnavailableJson(snapshot.ErrorMessage);
					return snapshot;
				}

				StringBuilder json = new StringBuilder();
				json.Append("{\"contextSchemaVersion\":1");
				json.Append(",\"available\":true");
				json.Append(",\"instrument\":\"").Append(EscapeJson(InstrumentFullName)).Append("\"");
				json.Append(",\"barsPeriod\":\"").Append(EscapeJson(snapshot.BarsPeriod)).Append("\"");
				json.Append(",\"tradingHours\":\"").Append(EscapeJson(snapshot.TradingHoursName)).Append("\"");
				AppendPriceBars(json, contextBars);
				AppendOrderFlowContext(json);
				AppendFeatureContext(json);
				AppendDrawingContext(json, snapshot.TimestampUtc);
				json.Append(",\"evidence\":{\"available\":false,\"message\":\"xPvaOrderFlowEvidence has no shared registry in this build\"}");
				json.Append("}");
				snapshot.ContextAvailable = true;
				snapshot.Json = json.ToString();
			}
			catch (Exception ex)
			{
				snapshot.ContextAvailable = false;
				snapshot.ErrorMessage = ex.Message;
				snapshot.Json = BuildUnavailableJson(ex.Message);
			}
			return snapshot;
		}

		private void AppendDrawingContext(StringBuilder json, DateTime timestampUtc)
		{
			IReadOnlyList<xPvaDrawingSnapshot> drawings;
			bool available = xPvaDrawingRegistry.TryGetActiveDrawings(InstrumentFullName, timestampUtc, out drawings);
			json.Append(",\"drawings\":{\"available\":").Append(available ? "true" : "false");
			json.Append(",\"items\":[");
			if (drawings != null)
			{
				for (int i = 0; i < drawings.Count; i++)
				{
					xPvaDrawingSnapshot drawing = drawings[i];
					if (i > 0) json.Append(',');
					json.Append("{\"drawingId\":\"").Append(EscapeJson(drawing.DrawingId)).Append("\"");
					json.Append(",\"revision\":").Append(drawing.Revision.ToString(CultureInfo.InvariantCulture));
					json.Append(",\"role\":\"").Append(drawing.Role).Append("\"");
					json.Append(",\"structuralLevel\":\"").Append(drawing.StructuralLevel).Append("\"");
					json.Append(",\"drawingToolType\":\"").Append(EscapeJson(drawing.DrawingToolType)).Append("\"");
					AppendDrawingAnchor(json, "anchor1", drawing.Anchor1);
					AppendDrawingAnchor(json, "anchor2", drawing.Anchor2);
					AppendDrawingAnchor(json, "anchor3", drawing.Anchor3);
					json.Append('}');
				}
			}
			json.Append("]}");
		}

		private void AppendDrawingAnchor(StringBuilder json, string name, xPvaDrawingAnchor anchor)
		{
			json.Append(",\"").Append(name).Append("\":");
			if (anchor == null)
			{
				json.Append("null");
				return;
			}
			json.Append("{\"timeUtc\":\"").Append(anchor.TimeUtc.ToString("o", CultureInfo.InvariantCulture)).Append("\"");
			json.Append(",\"price\":").Append(anchor.Price.ToString(CultureInfo.InvariantCulture));
			json.Append(",\"barIndexAtCapture\":").Append(anchor.BarIndexAtCapture.ToString(CultureInfo.InvariantCulture)).Append('}');
		}

		private void AppendPriceBars(StringBuilder json, int contextBars)
		{
			json.Append(",\"priceBars\":[");
			int count = Math.Min(Math.Max(0, CurrentBar + 1), contextBars);
			for (int i = count - 1; i >= 0; i--)
			{
				if (i != count - 1)
					json.Append(",");
				json.Append("{\"barIndex\":").Append((CurrentBar - i).ToString(CultureInfo.InvariantCulture));
				json.Append(",\"time\":\"").Append(Time[i].ToString("o", CultureInfo.InvariantCulture)).Append("\"");
				json.Append(",\"open\":").Append(Open[i].ToString(CultureInfo.InvariantCulture));
				json.Append(",\"high\":").Append(High[i].ToString(CultureInfo.InvariantCulture));
				json.Append(",\"low\":").Append(Low[i].ToString(CultureInfo.InvariantCulture));
				json.Append(",\"close\":").Append(Close[i].ToString(CultureInfo.InvariantCulture));
				json.Append(",\"volume\":").Append(Volume[i].ToString(CultureInfo.InvariantCulture));
				json.Append("}");
			}
			json.Append("]");
		}

		private void AppendOrderFlowContext(StringBuilder json)
		{
			xOrderFlowRegistryKey key = BuildOrderFlowRegistryKey();
			int latest = xPvaOrderFlowRegistry.GetMostRecentPublishedIndex(key);
			xOrderFlowBar bar;
			if (latest >= 0 && xPvaOrderFlowRegistry.TryGetBar(key, latest, out bar))
			{
				json.Append(",\"orderFlow\":{\"available\":true");
				json.Append(",\"barIndex\":").Append(bar.BarIndex.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"capturedVolume\":").Append(bar.CapturedVolume.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"buyVolume\":").Append(bar.BuyVolume.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"sellVolume\":").Append(bar.SellVolume.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"delta\":").Append(bar.Delta.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"pocPrice\":").Append(bar.PocPrice.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"captureCompletenessPassed\":").Append(bar.CaptureCompletenessPassed ? "true" : "false");
				json.Append(",\"pendingEventsAssigned\":").Append(bar.PendingEventsAssigned.ToString(CultureInfo.InvariantCulture));
				json.Append("}");
				return;
			}
			json.Append(",\"orderFlow\":{\"available\":false}");
		}

		private void AppendFeatureContext(StringBuilder json)
		{
			xOrderFlowFeatureRegistryKey key = BuildFeatureRegistryKey();
			int latest = xPvaOrderFlowFeatureRegistry.GetMostRecentPublishedIndex(key);
			xOrderFlowBarFeatures f;
			if (latest >= 0 && xPvaOrderFlowFeatureRegistry.TryGetFeatures(key, latest, out f))
			{
				json.Append(",\"features\":{\"available\":true");
				json.Append(",\"barIndex\":").Append(f.BarIndex.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"volumeZScore\":").Append(f.VolumeZScore.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"absoluteDeltaZScore\":").Append(f.AbsoluteDeltaZScore.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"deltaPercent\":").Append(f.DeltaPercent.ToString(CultureInfo.InvariantCulture));
				json.Append(",\"sourceCompletenessPassed\":").Append(f.SourceCaptureCompletenessPassed ? "true" : "false");
				json.Append("}");
				return;
			}
			json.Append(",\"features\":{\"available\":false}");
		}

		private xOrderFlowRegistryKey BuildOrderFlowRegistryKey()
		{
			string tradingHoursName = string.Empty;
			try { if (Bars != null && Bars.TradingHours != null) tradingHoursName = Bars.TradingHours.Name; } catch { tradingHoursName = string.Empty; }
			int value2 = 0;
			try { value2 = BarsPeriod != null ? BarsPeriod.Value2 : 0; } catch { value2 = 0; }
			return new xOrderFlowRegistryKey(
				Instrument != null ? Instrument.FullName : string.Empty,
				Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : string.Empty,
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
			try { if (Bars != null && Bars.TradingHours != null) tradingHoursName = Bars.TradingHours.Name; } catch { tradingHoursName = string.Empty; }
			int value2 = 0;
			try { value2 = BarsPeriod != null ? BarsPeriod.Value2 : 0; } catch { value2 = 0; }
			return new xOrderFlowFeatureRegistryKey(
				Instrument != null ? Instrument.FullName : string.Empty,
				BarsPeriod != null ? BarsPeriod.BarsPeriodType : BarsPeriodType.Minute,
				BarsPeriod != null ? BarsPeriod.Value : 0,
				value2,
				tradingHoursName,
				RegistryInstanceKey);
		}

		private string BuildUnavailableJson(string error)
		{
			return "{\"contextSchemaVersion\":1,\"available\":false,\"error\":\"" + EscapeJson(error) + "\"}";
		}

		private string EscapeJson(string value)
		{
			if (value == null)
				return string.Empty;
			return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		public string InstrumentFullName { get { return Instrument != null ? Instrument.FullName : string.Empty; } }
		public int BarsPeriodMinutes { get { return BarsPeriod != null && BarsPeriod.BarsPeriodType == BarsPeriodType.Minute ? BarsPeriod.Value : 0; } }
		public DateTime LastUpdatedUtc { get { return lastUpdatedUtc; } }

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Account name", GroupName = "Account", Order = 1)]
		public string AccountName { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Capture all accounts", GroupName = "Account", Order = 2)]
		public bool CaptureAllAccounts { get; set; }

		[NinjaScriptProperty]
		[Range(10, 200)]
		[Display(Name = "Context bars", GroupName = "Context", Order = 3)]
		public int ContextBars { get; set; }

		[NinjaScriptProperty]
		[Range(1, 240)]
		[Display(Name = "Preferred bars period minutes", GroupName = "Context", Order = 4)]
		public int PreferredBarsPeriodMinutes { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Post-trade tag window seconds", GroupName = "Tags", Order = 5)]
		public int PostTradeTagWindowSeconds { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max emergency queue records", GroupName = "Durability", Order = 6)]
		public int MaxEmergencyQueueRecords { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Leg matching method", GroupName = "Campaigns", Order = 7)]
		public xTradeJournalLegMatchingMethod LegMatchingMethod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show status panel", GroupName = "Display", Order = 8)]
		public bool ShowStatusPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Status panel position", GroupName = "Display", Order = 9)]
		public TextPosition StatusPanelPosition { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Diagnostic level", GroupName = "Diagnostics", Order = 10)]
		public xTradeJournalDiagnosticLevel DiagnosticLevel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Registry instance key", GroupName = "Context", Order = 11)]
		public string RegistryInstanceKey { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public string LastStatusText { get { return lastStatusText; } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaTradeJournal.xPvaTradeJournal[] cachexPvaTradeJournal;
		public xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			return xPvaTradeJournal(Input, accountName, captureAllAccounts, contextBars, preferredBarsPeriodMinutes, postTradeTagWindowSeconds, maxEmergencyQueueRecords, legMatchingMethod, showStatusPanel, statusPanelPosition, diagnosticLevel, registryInstanceKey);
		}

		public xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(ISeries<double> input, string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			if (cachexPvaTradeJournal != null)
				for (int idx = 0; idx < cachexPvaTradeJournal.Length; idx++)
					if (cachexPvaTradeJournal[idx] != null && cachexPvaTradeJournal[idx].AccountName == accountName && cachexPvaTradeJournal[idx].CaptureAllAccounts == captureAllAccounts && cachexPvaTradeJournal[idx].ContextBars == contextBars && cachexPvaTradeJournal[idx].PreferredBarsPeriodMinutes == preferredBarsPeriodMinutes && cachexPvaTradeJournal[idx].PostTradeTagWindowSeconds == postTradeTagWindowSeconds && cachexPvaTradeJournal[idx].MaxEmergencyQueueRecords == maxEmergencyQueueRecords && cachexPvaTradeJournal[idx].LegMatchingMethod == legMatchingMethod && cachexPvaTradeJournal[idx].ShowStatusPanel == showStatusPanel && cachexPvaTradeJournal[idx].StatusPanelPosition == statusPanelPosition && cachexPvaTradeJournal[idx].DiagnosticLevel == diagnosticLevel && cachexPvaTradeJournal[idx].RegistryInstanceKey == registryInstanceKey && cachexPvaTradeJournal[idx].EqualsInput(input))
						return cachexPvaTradeJournal[idx];
			return CacheIndicator<xPvaTradeJournal.xPvaTradeJournal>(new xPvaTradeJournal.xPvaTradeJournal(){ AccountName = accountName, CaptureAllAccounts = captureAllAccounts, ContextBars = contextBars, PreferredBarsPeriodMinutes = preferredBarsPeriodMinutes, PostTradeTagWindowSeconds = postTradeTagWindowSeconds, MaxEmergencyQueueRecords = maxEmergencyQueueRecords, LegMatchingMethod = legMatchingMethod, ShowStatusPanel = showStatusPanel, StatusPanelPosition = statusPanelPosition, DiagnosticLevel = diagnosticLevel, RegistryInstanceKey = registryInstanceKey }, input, ref cachexPvaTradeJournal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			return indicator.xPvaTradeJournal(Input, accountName, captureAllAccounts, contextBars, preferredBarsPeriodMinutes, postTradeTagWindowSeconds, maxEmergencyQueueRecords, legMatchingMethod, showStatusPanel, statusPanelPosition, diagnosticLevel, registryInstanceKey);
		}

		public Indicators.xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(ISeries<double> input , string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			return indicator.xPvaTradeJournal(input, accountName, captureAllAccounts, contextBars, preferredBarsPeriodMinutes, postTradeTagWindowSeconds, maxEmergencyQueueRecords, legMatchingMethod, showStatusPanel, statusPanelPosition, diagnosticLevel, registryInstanceKey);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			return indicator.xPvaTradeJournal(Input, accountName, captureAllAccounts, contextBars, preferredBarsPeriodMinutes, postTradeTagWindowSeconds, maxEmergencyQueueRecords, legMatchingMethod, showStatusPanel, statusPanelPosition, diagnosticLevel, registryInstanceKey);
		}

		public Indicators.xPvaTradeJournal.xPvaTradeJournal xPvaTradeJournal(ISeries<double> input , string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalLegMatchingMethod legMatchingMethod, bool showStatusPanel, TextPosition statusPanelPosition, xTradeJournalDiagnosticLevel diagnosticLevel, string registryInstanceKey)
		{
			return indicator.xPvaTradeJournal(input, accountName, captureAllAccounts, contextBars, preferredBarsPeriodMinutes, postTradeTagWindowSeconds, maxEmergencyQueueRecords, legMatchingMethod, showStatusPanel, statusPanelPosition, diagnosticLevel, registryInstanceKey);
		}
	}
}

#endregion
