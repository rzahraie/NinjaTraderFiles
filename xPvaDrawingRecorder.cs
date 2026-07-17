#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder
{
	public class xPvaDrawingRecorder : Indicator
	{
		private sealed class xTrackedDrawing
		{
			public DrawingTool Tool;
			public string DrawingId;
			public xPvaDrawingSnapshot LastSnapshot;
			public string LastFingerprint;
			public xPvaDrawingSnapshot PendingSnapshot;
			public string PendingFingerprint;
			public DateTime PendingSinceUtc;
			public int MissingScans;
			public bool GeometrySubscribed;
		}

		private static readonly object chartOwnersSync = new object();
		private static readonly Dictionary<ChartControl, xPvaDrawingRecorder> chartOwners = new Dictionary<ChartControl, xPvaDrawingRecorder>();
		private readonly Dictionary<DrawingTool, xTrackedDrawing> tracked = new Dictionary<DrawingTool, xTrackedDrawing>();
		private xPvaDrawingRecorderService service;
		private DispatcherTimer scanTimer;
		private ChartControl recorderChartControl;
		private ChartPanel recorderChartPanel;
		private string chartIdentity;
		private string workspaceIdentity;
		private bool ownsChart;
		private int initialScans;
		private DateTime lastStatusUpdate;
		private string lastStatusText;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "xPvaDrawingRecorder";
				Description = "Automatic durable revision recorder for APVA chart drawings.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true; DisplayInDataBox = false; DrawOnPricePanel = true;
				PaintPriceMarkers = false; IsSuspendedWhileInactive = false;
				CaptureAllDrawingTools = false; ModificationDebounceMilliseconds = 500;
				MaxEmergencyQueueRecords = 100000; ShowStatusPanel = true;
				StatusPanelPosition = TextPosition.BottomRight; DiagnosticLevel = xDrawingDiagnosticLevel.Normal;
			}
			else if (State == State.DataLoaded)
			{
				service = xPvaDrawingRecorderService.Acquire();
				service.Configure(MaxEmergencyQueueRecords, DiagnosticLevel);
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null) ChartControl.Dispatcher.BeginInvoke(new Action(AttachToChart));
			}
			else if (State == State.Terminated)
			{
				DetachFromChart();
				if (service != null) { service.Release(); service = null; }
				RemoveDrawObject("xPvaDrawingRecorderStatus");
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0 && ShowStatusPanel && scanTimer == null) UpdateStatusPanel(false);
		}

		private void AttachToChart()
		{
			if (ChartControl == null || ChartPanel == null || scanTimer != null) return;
			recorderChartControl = ChartControl; recorderChartPanel = ChartPanel;
			lock (chartOwnersSync)
			{
				xPvaDrawingRecorder owner;
				if (chartOwners.TryGetValue(recorderChartControl, out owner) && owner != null && owner != this) { ownsChart = false; UpdateStatusPanel(true); return; }
				chartOwners[recorderChartControl] = this; ownsChart = true;
			}
			workspaceIdentity = ResolveWorkspaceIdentity(); chartIdentity = BuildChartIdentity();
			scanTimer = new DispatcherTimer(DispatcherPriority.Background, recorderChartControl.Dispatcher);
			scanTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(100, Math.Min(1000, ModificationDebounceMilliseconds / 2)));
			scanTimer.Tick += ScanTimerTick; scanTimer.Start(); ScanDrawings();
		}

		private void DetachFromChart()
		{
			Action detach = delegate
			{
				if (scanTimer != null) { scanTimer.Stop(); scanTimer.Tick -= ScanTimerTick; scanTimer = null; }
				foreach (xTrackedDrawing item in tracked.Values) UnsubscribeGeometry(item); tracked.Clear();
				if (recorderChartControl != null) lock (chartOwnersSync) { xPvaDrawingRecorder owner; if (chartOwners.TryGetValue(recorderChartControl, out owner) && owner == this) chartOwners.Remove(recorderChartControl); }
				ownsChart = false;
			};
			try
			{
				if (recorderChartControl != null && !recorderChartControl.Dispatcher.CheckAccess()) recorderChartControl.Dispatcher.BeginInvoke(detach); else detach();
			}
			catch { }
		}

		private void ScanTimerTick(object sender, EventArgs e)
		{
			ScanDrawings(); UpdateStatusPanel(false);
		}

		private void ScanDrawings()
		{
			if (!ownsChart || recorderChartPanel == null || service == null) return;
			HashSet<DrawingTool> seen = new HashSet<DrawingTool>();
			try
			{
				foreach (object chartObject in recorderChartPanel.ChartObjects)
				{
					DrawingTool tool = chartObject as DrawingTool;
					if (tool == null || tool.DrawingState == DrawingState.Building || !IsSupported(tool)) continue;
					seen.Add(tool); ProcessDrawing(tool, initialScans < 3 ? "StartupReconciliation" : "DrawingMonitor");
				}

				List<DrawingTool> removed = new List<DrawingTool>();
				foreach (KeyValuePair<DrawingTool, xTrackedDrawing> pair in tracked)
				{
					if (seen.Contains(pair.Key)) { pair.Value.MissingScans = 0; continue; }
					pair.Value.MissingScans++;
					if (pair.Value.MissingScans >= 2) removed.Add(pair.Key);
				}
				for (int i = 0; i < removed.Count; i++)
				{
					xTrackedDrawing item = tracked[removed[i]]; UnsubscribeGeometry(item);
					service.MarkDeleted(item.DrawingId, item.LastSnapshot); tracked.Remove(removed[i]);
				}

				initialScans++;
				if (initialScans == 3)
				{
					HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (xTrackedDrawing item in tracked.Values) if (!string.IsNullOrEmpty(item.DrawingId)) ids.Add(item.DrawingId);
					service.CompleteReconciliation(chartIdentity, ids);
				}
			}
			catch (Exception ex) { Print("xPvaDrawingRecorder scan error: " + ex.Message); }
		}

		private void ProcessDrawing(DrawingTool tool, string actionSource)
		{
			xPvaDrawingSnapshot snapshot = CaptureSnapshot(tool); if (snapshot == null) return;
			string fingerprint = Fingerprint(snapshot); xTrackedDrawing item;
			if (!tracked.TryGetValue(tool, out item))
			{
				xPvaDrawingSnapshot recorded = service.Observe(snapshot, actionSource);
				item = new xTrackedDrawing { Tool = tool, DrawingId = recorded == null ? string.Empty : recorded.DrawingId,
					LastSnapshot = recorded ?? snapshot, LastFingerprint = fingerprint };
				tracked[tool] = item; SubscribeGeometry(item); return;
			}

			if (string.Equals(fingerprint, item.LastFingerprint, StringComparison.Ordinal))
			{
				item.PendingSnapshot = null; item.PendingFingerprint = null; return;
			}

			if (!string.Equals(fingerprint, item.PendingFingerprint, StringComparison.Ordinal))
			{
				item.PendingSnapshot = snapshot; item.PendingFingerprint = fingerprint; item.PendingSinceUtc = DateTime.UtcNow; return;
			}

			if ((DateTime.UtcNow - item.PendingSinceUtc).TotalMilliseconds < ModificationDebounceMilliseconds) return;
			xPvaDrawingSnapshot updated = service.Observe(item.PendingSnapshot, "UserEdit");
			item.DrawingId = updated == null ? item.DrawingId : updated.DrawingId; item.LastSnapshot = updated ?? item.PendingSnapshot;
			item.LastFingerprint = item.PendingFingerprint; item.PendingSnapshot = null; item.PendingFingerprint = null;
		}

		private void SubscribeGeometry(xTrackedDrawing item)
		{
			xManualContainer container = item.Tool as xManualContainer;
			if (container == null || item.GeometrySubscribed) return;
			container.GeometryChanged += OnJHGeometryChanged; item.GeometrySubscribed = true;
		}

		private void UnsubscribeGeometry(xTrackedDrawing item)
		{
			xManualContainer container = item.Tool as xManualContainer;
			if (container != null && item.GeometrySubscribed) container.GeometryChanged -= OnJHGeometryChanged;
			item.GeometrySubscribed = false;
		}

		private void OnJHGeometryChanged(object sender, EventArgs e)
		{
			// The timer takes the immutable snapshot after geometry remains stable for the debounce interval.
		}

		private bool IsSupported(DrawingTool tool)
		{
			return CaptureAllDrawingTools || tool is CustomLine || tool is xManualContainer;
		}

		private xPvaDrawingSnapshot CaptureSnapshot(DrawingTool tool)
		{
			List<ChartAnchor> anchors = new List<ChartAnchor>(); foreach (ChartAnchor anchor in tool.Anchors) if (anchor != null) anchors.Add(anchor);
			xPvaDrawingSnapshot s = new xPvaDrawingSnapshot(); s.NativeTag = ReadString(tool, "Tag");
			s.DrawingToolType = tool.GetType().FullName; s.Instrument = Instrument == null ? string.Empty : Instrument.FullName;
			s.MasterInstrument = Instrument == null || Instrument.MasterInstrument == null ? string.Empty : Instrument.MasterInstrument.Name;
			s.ChartIdentity = chartIdentity; s.WorkspaceIdentity = workspaceIdentity; s.BarsPeriodType = BarsPeriod == null ? string.Empty : BarsPeriod.BarsPeriodType.ToString();
			s.BarsPeriodValue = BarsPeriod == null ? 0 : BarsPeriod.Value; s.TradingHoursTemplate = Bars == null || Bars.TradingHours == null ? string.Empty : Bars.TradingHours.Name;
			s.TickSize = Instrument == null || Instrument.MasterInstrument == null ? 0 : Instrument.MasterInstrument.TickSize;
			s.Anchor1 = anchors.Count > 0 ? CopyAnchor(anchors[0]) : null; s.Anchor2 = anchors.Count > 1 ? CopyAnchor(anchors[1]) : null;
			s.Anchor3 = anchors.Count > 2 ? CopyAnchor(anchors[anchors.Count - 1]) : null;
			s.Role = ResolveRole(tool, s.NativeTag); s.StructuralLevel = ResolveStructuralLevel(tool);
			object stroke = ReadObject(tool, "LineStroke") ?? ReadObject(tool, "Stroke"); s.DashStyle = ReadString(stroke, "DashStyleHelper");
			if (string.IsNullOrEmpty(s.DashStyle)) s.DashStyle = ReadString(stroke, "DashStyle"); s.Width = ReadDouble(stroke, "Width", 0);
			s.ExtendLeft = ReadBool(tool, new string[] { "ExtendLeft", "IsExtendedLinesLeft", "ExtendLinesLeft" }, false);
			s.ExtendRight = ReadBool(tool, new string[] { "ExtendRight", "IsExtendedLinesRight", "ExtendLinesRight", "AutoExtend" }, false);
			s.IsRay = tool.GetType().Name.IndexOf("Ray", StringComparison.OrdinalIgnoreCase) >= 0; s.IsLocked = ReadBool(tool, new string[] { "IsLocked" }, false);
			s.IsVisible = ReadBool(tool, new string[] { "IsVisible" }, true); s.RelationshipType = xDrawingRelationshipType.None;
			xManualContainer jh = tool as xManualContainer;
			if (jh != null)
			{
				s.Role = xDrawingRole.ContainerBoundary;
				s.SourceStableId = "JH:" + jh.ContainerId;
				s.RawMetadataJson = "{\"jhContainerId\":\"" + Escape(jh.ContainerId) + "\",\"componentRole\":\"" + jh.ComponentRole + "\",\"containerLevel\":" + jh.ContainerLevel.ToString(CultureInfo.InvariantCulture) + ",\"includeInGaussian\":" + (jh.IncludeInGaussian ? "true" : "false") + "}";
			}
			else s.SourceStableId = string.Empty;
			return s;
		}

		private xPvaDrawingAnchor CopyAnchor(ChartAnchor anchor)
		{
			DateTime time = anchor.Time; int index = -1; try { if (Bars != null && time != DateTime.MinValue) index = Bars.GetBar(time); } catch { }
			return new xPvaDrawingAnchor(time, anchor.Price, index);
		}

		private xDrawingRole ResolveRole(DrawingTool tool, string tag)
		{
			string explicitRole = ReadString(tool, "xPvaRole"); if (string.IsNullOrEmpty(explicitRole)) explicitRole = ReadString(tool, "LineRole");
			xDrawingRole role; if (Enum.TryParse<xDrawingRole>(explicitRole, true, out role)) return role;
			string value = (tag ?? string.Empty).TrimStart('@').ToUpperInvariant();
			if (value.StartsWith("RTL-") || value.StartsWith("RTL_")) return xDrawingRole.RTL;
			if (value.StartsWith("LTL-") || value.StartsWith("LTL_")) return xDrawingRole.LTL;
			if (value.StartsWith("VE-") || value.StartsWith("VE_")) return xDrawingRole.VE;
			return xDrawingRole.Unknown;
		}

		private xStructuralLevel ResolveStructuralLevel(DrawingTool tool)
		{
			string explicitLevel = ReadString(tool, "xPvaStructuralLevel"); if (string.IsNullOrEmpty(explicitLevel)) explicitLevel = ReadString(tool, "StructuralLevel");
			xStructuralLevel level; return Enum.TryParse<xStructuralLevel>(explicitLevel, true, out level) ? level : xStructuralLevel.Unknown;
		}

		private string Fingerprint(xPvaDrawingSnapshot s)
		{
			StringBuilder b = new StringBuilder(); AppendAnchorFingerprint(b, s.Anchor1); AppendAnchorFingerprint(b, s.Anchor2); AppendAnchorFingerprint(b, s.Anchor3);
			b.Append('|').Append(s.NativeTag).Append('|').Append(s.Role).Append('|').Append(s.StructuralLevel).Append('|').Append(s.DashStyle).Append('|').Append(s.Width.ToString("R", CultureInfo.InvariantCulture));
			b.Append('|').Append(s.ExtendLeft).Append('|').Append(s.ExtendRight).Append('|').Append(s.IsRay).Append('|').Append(s.IsLocked).Append('|').Append(s.IsVisible).Append('|').Append(s.RawMetadataJson);
			return b.ToString();
		}

		private void AppendAnchorFingerprint(StringBuilder b, xPvaDrawingAnchor a)
		{
			if (a == null) { b.Append("|null"); return; } b.Append('|').Append(a.TimeUtc.Ticks).Append(':').Append(a.Price.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.BarIndexAtCapture);
		}

		private string BuildChartIdentity()
		{
			Window window = recorderChartControl == null ? null : Window.GetWindow(recorderChartControl);
			string windowIdentity = window == null ? string.Empty : (window.Name + "|" + window.Title);
			string controlIdentity = recorderChartControl == null ? string.Empty : recorderChartControl.Name;
			return string.Join("|", new string[] { workspaceIdentity, windowIdentity, controlIdentity,
				Instrument == null ? string.Empty : Instrument.FullName, BarsPeriod == null ? string.Empty : BarsPeriod.BarsPeriodType.ToString(),
				BarsPeriod == null ? "0" : BarsPeriod.Value.ToString(CultureInfo.InvariantCulture), Bars == null || Bars.TradingHours == null ? string.Empty : Bars.TradingHours.Name,
				recorderChartPanel == null ? "0" : recorderChartPanel.PanelIndex.ToString(CultureInfo.InvariantCulture) });
		}

		private string ResolveWorkspaceIdentity()
		{
			try
			{
				Type globals = typeof(NinjaTrader.Core.Globals); string[] names = { "CurrentWorkspace", "WorkspaceName", "ActiveWorkspace" };
				for (int i = 0; i < names.Length; i++) { PropertyInfo p = globals.GetProperty(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); if (p != null) { object value = p.GetValue(null, null); if (value != null) return value.ToString(); } }
			}
			catch { }
			return "WorkspaceUnavailable";
		}

		private void UpdateStatusPanel(bool force)
		{
			if (!ShowStatusPanel) { RemoveDrawObject("xPvaDrawingRecorderStatus"); return; }
			if (!force && (DateTime.UtcNow - lastStatusUpdate).TotalMilliseconds < 1000) return; lastStatusUpdate = DateTime.UtcNow;
			xPvaDrawingRecorderStatus s = service == null ? new xPvaDrawingRecorderStatus() : service.GetStatus();
			lastStatusText = string.Format(CultureInfo.InvariantCulture, "xPvaDrawingRecorder\nActive drawings: {0}\nEvents: {1}\nLast: {2}\nDatabase: {3}\nJSON: {4}\nErrors: {5}{6}",
				s.ActiveDrawings, s.EventCount, s.LastEventType, s.DatabaseConnected ? "Connected" : "Unavailable", s.JsonCurrent ? "Current" : "Pending", s.ErrorCount,
				string.IsNullOrEmpty(s.LastError) ? string.Empty : "\nLast error: " + s.LastError);
			Draw.TextFixed(this, "xPvaDrawingRecorderStatus", lastStatusText, StatusPanelPosition);
		}

		private object ReadObject(object target, string name)
		{
			if (target == null) return null; try { PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance); return p == null ? null : p.GetValue(target, null); } catch { return null; }
		}
		private string ReadString(object target, string name) { object value = ReadObject(target, name); return value == null ? string.Empty : value.ToString(); }
		private double ReadDouble(object target, string name, double fallback) { object value = ReadObject(target, name); try { return value == null ? fallback : Convert.ToDouble(value, CultureInfo.InvariantCulture); } catch { return fallback; } }
		private bool ReadBool(object target, string[] names, bool fallback) { for (int i = 0; i < names.Length; i++) { object value = ReadObject(target, names[i]); if (value != null) try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); } catch { } } return fallback; }
		private string Escape(string value) { return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\""); }

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Capture all drawing tools", GroupName = "Capture", Order = 1)]
		public bool CaptureAllDrawingTools { get; set; }

		[NinjaScriptProperty]
		[Range(100, 5000)]
		[Display(Name = "Modification debounce milliseconds", GroupName = "Capture", Order = 2)]
		public int ModificationDebounceMilliseconds { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max emergency queue records", GroupName = "Durability", Order = 3)]
		public int MaxEmergencyQueueRecords { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show status panel", GroupName = "Display", Order = 4)]
		public bool ShowStatusPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Status panel position", GroupName = "Display", Order = 5)]
		public TextPosition StatusPanelPosition { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Diagnostic level", GroupName = "Diagnostics", Order = 6)]
		public xDrawingDiagnosticLevel DiagnosticLevel { get; set; }

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
		private xPvaDrawingRecorder.xPvaDrawingRecorder[] cachexPvaDrawingRecorder;
		public xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			return xPvaDrawingRecorder(Input, captureAllDrawingTools, modificationDebounceMilliseconds, maxEmergencyQueueRecords, showStatusPanel, statusPanelPosition, diagnosticLevel);
		}

		public xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(ISeries<double> input, bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			if (cachexPvaDrawingRecorder != null)
				for (int idx = 0; idx < cachexPvaDrawingRecorder.Length; idx++)
					if (cachexPvaDrawingRecorder[idx] != null && cachexPvaDrawingRecorder[idx].CaptureAllDrawingTools == captureAllDrawingTools && cachexPvaDrawingRecorder[idx].ModificationDebounceMilliseconds == modificationDebounceMilliseconds && cachexPvaDrawingRecorder[idx].MaxEmergencyQueueRecords == maxEmergencyQueueRecords && cachexPvaDrawingRecorder[idx].ShowStatusPanel == showStatusPanel && cachexPvaDrawingRecorder[idx].StatusPanelPosition == statusPanelPosition && cachexPvaDrawingRecorder[idx].DiagnosticLevel == diagnosticLevel && cachexPvaDrawingRecorder[idx].EqualsInput(input))
						return cachexPvaDrawingRecorder[idx];
			return CacheIndicator<xPvaDrawingRecorder.xPvaDrawingRecorder>(new xPvaDrawingRecorder.xPvaDrawingRecorder(){ CaptureAllDrawingTools = captureAllDrawingTools, ModificationDebounceMilliseconds = modificationDebounceMilliseconds, MaxEmergencyQueueRecords = maxEmergencyQueueRecords, ShowStatusPanel = showStatusPanel, StatusPanelPosition = statusPanelPosition, DiagnosticLevel = diagnosticLevel }, input, ref cachexPvaDrawingRecorder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			return indicator.xPvaDrawingRecorder(Input, captureAllDrawingTools, modificationDebounceMilliseconds, maxEmergencyQueueRecords, showStatusPanel, statusPanelPosition, diagnosticLevel);
		}

		public Indicators.xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(ISeries<double> input , bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			return indicator.xPvaDrawingRecorder(input, captureAllDrawingTools, modificationDebounceMilliseconds, maxEmergencyQueueRecords, showStatusPanel, statusPanelPosition, diagnosticLevel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			return indicator.xPvaDrawingRecorder(Input, captureAllDrawingTools, modificationDebounceMilliseconds, maxEmergencyQueueRecords, showStatusPanel, statusPanelPosition, diagnosticLevel);
		}

		public Indicators.xPvaDrawingRecorder.xPvaDrawingRecorder xPvaDrawingRecorder(ISeries<double> input , bool captureAllDrawingTools, int modificationDebounceMilliseconds, int maxEmergencyQueueRecords, bool showStatusPanel, TextPosition statusPanelPosition, xDrawingDiagnosticLevel diagnosticLevel)
		{
			return indicator.xPvaDrawingRecorder(input, captureAllDrawingTools, modificationDebounceMilliseconds, maxEmergencyQueueRecords, showStatusPanel, statusPanelPosition, diagnosticLevel);
		}
	}
}

#endregion
