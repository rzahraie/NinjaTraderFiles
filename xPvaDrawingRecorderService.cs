#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder
{
	public sealed class xPvaDrawingRecorderService : IDisposable
	{
		private static readonly object staticSync = new object();
		private static xPvaDrawingRecorderService instance;
		private readonly object sync = new object();
		private readonly BlockingCollection<xPvaDrawingEventRecord> queue = new BlockingCollection<xPvaDrawingEventRecord>();
		private readonly List<xPvaDrawingEventRecord> retryQueue = new List<xPvaDrawingEventRecord>();
		private readonly Dictionary<string, xPvaDrawingSnapshot> current = new Dictionary<string, xPvaDrawingSnapshot>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, List<xPvaDrawingEventRecord>> history = new Dictionary<string, List<xPvaDrawingEventRecord>>(StringComparer.OrdinalIgnoreCase);
		private readonly xPvaDrawingRecorderDatabase database = new xPvaDrawingRecorderDatabase();
		private readonly Thread worker;
		private readonly string root;
		private readonly string sessionsRoot;
		private readonly string emergencyRoot;
		private string sessionId;
		private string databasePath;
		private DateTime lastReconnectAttemptUtc;
		private long sequence;
		private int refCount;
		private int maxEmergencyQueueRecords = 100000;
		private bool configured;
		private bool disposed;
		private xDrawingDiagnosticLevel diagnosticLevel = xDrawingDiagnosticLevel.Normal;
		private xPvaDrawingRecorderStatus status = new xPvaDrawingRecorderStatus();

		private xPvaDrawingRecorderService()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			root = Path.Combine(documents, "NinjaTrader 8", "apva", "drawings");
			sessionsRoot = Path.Combine(root, "sessions");
			emergencyRoot = Path.Combine(root, "emergency");
			Directory.CreateDirectory(root); Directory.CreateDirectory(sessionsRoot); Directory.CreateDirectory(emergencyRoot);
			worker = new Thread(ProcessQueue) { IsBackground = true, Name = "xPvaDrawingRecorderService" };
			worker.Start();
		}

		public static xPvaDrawingRecorderService Acquire()
		{
			lock (staticSync)
			{
				if (instance == null) instance = new xPvaDrawingRecorderService();
				instance.refCount++; return instance;
			}
		}

		public void Release()
		{
			lock (staticSync) { if (refCount > 0) refCount--; }
		}

		public void Configure(int emergencyLimit, xDrawingDiagnosticLevel level)
		{
			lock (sync)
			{
				maxEmergencyQueueRecords = Math.Max(1, emergencyLimit); diagnosticLevel = level;
				if (configured) return;
				try
				{
					databasePath = Path.Combine(root, "xPvaDrawingRecorder.sqlite");
					database.Open(databasePath); sessionId = Guid.NewGuid().ToString("N"); database.InsertSession(sessionId, level.ToString());
					RestoreDatabaseState(); ImportEmergencyFiles();
					status.DatabaseConnected = true; status.DatabasePath = databasePath; configured = true; UpdateActiveCount();
					LogSystem("ServiceStart", "Database opened and current drawing state restored.");
				}
				catch (Exception ex) { if (string.IsNullOrEmpty(databasePath)) databasePath = Path.Combine(root, "xPvaDrawingRecorder.sqlite"); status.DatabaseConnected = false; status.DatabasePath = databasePath; SetError("Database open failed: " + ex.Message); configured = true; }
			}
		}

		public xPvaDrawingSnapshot Observe(xPvaDrawingSnapshot incoming, string actionSource)
		{
			if (incoming == null) return null;
			lock (sync)
			{
				xPvaDrawingSnapshot existing = FindMatch(incoming, out bool sourceIdentityMatch);
				DateTime utc = DateTime.UtcNow;
				xDrawingEventType eventType;
				if (existing == null)
				{
					incoming.DrawingId = NextDrawingId(incoming.MasterInstrument, utc);
					incoming.Revision = 1; incoming.Status = xDrawingStatus.Active;
					incoming.CreatedUtc = utc; incoming.ModifiedUtc = utc; eventType = xDrawingEventType.Created;
				}
				else
				{
					incoming.DrawingId = existing.DrawingId; incoming.CreatedUtc = existing.CreatedUtc;
					if (MateriallyEqual(existing, incoming, true)) return existing.Clone();
					incoming.Revision = existing.Revision + 1; incoming.ModifiedUtc = utc;
					if (existing.Status == xDrawingStatus.Deleted && sourceIdentityMatch) eventType = xDrawingEventType.Restored;
					else if (existing.Status == xDrawingStatus.Missing || string.Equals(actionSource, "StartupReconciliation", StringComparison.Ordinal)) eventType = xDrawingEventType.Reconciled;
					else eventType = ClassifyChange(existing, incoming);
					incoming.Status = eventType == xDrawingEventType.Reconciled ? xDrawingStatus.Reconciled : xDrawingStatus.Active;
					incoming.DeletedUtc = DateTime.MinValue;
				}

				xPvaDrawingEventRecord record = CreateRecord(incoming, eventType, actionSource, utc);
				ApplyRecord(record); Enqueue(record); return incoming.Clone();
			}
		}

		public void MarkDeleted(string drawingId, xPvaDrawingSnapshot lastSnapshot)
		{
			lock (sync)
			{
				xPvaDrawingSnapshot existing;
				if (string.IsNullOrEmpty(drawingId) || !current.TryGetValue(drawingId, out existing) || existing.Status == xDrawingStatus.Deleted) return;
				xPvaDrawingSnapshot snapshot = lastSnapshot == null ? existing.Clone() : lastSnapshot.Clone();
				snapshot.DrawingId = drawingId; snapshot.Revision = existing.Revision + 1; snapshot.CreatedUtc = existing.CreatedUtc;
				snapshot.ModifiedUtc = DateTime.UtcNow; snapshot.DeletedUtc = snapshot.ModifiedUtc; snapshot.Status = xDrawingStatus.Deleted;
				xPvaDrawingEventRecord record = CreateRecord(snapshot, xDrawingEventType.Deleted, "UserDelete", snapshot.ModifiedUtc);
				ApplyRecord(record); Enqueue(record);
			}
		}

		public void CompleteReconciliation(string chartIdentity, ISet<string> observedDrawingIds)
		{
			lock (sync)
			{
				List<xPvaDrawingSnapshot> missing = new List<xPvaDrawingSnapshot>();
				foreach (xPvaDrawingSnapshot s in current.Values)
					if (s.Status != xDrawingStatus.Deleted && s.Status != xDrawingStatus.Missing
						&& string.Equals(s.ChartIdentity, chartIdentity, StringComparison.Ordinal)
						&& (observedDrawingIds == null || !observedDrawingIds.Contains(s.DrawingId))) missing.Add(s.Clone());
				for (int i = 0; i < missing.Count; i++)
				{
					xPvaDrawingSnapshot s = missing[i]; s.Revision++; s.Status = xDrawingStatus.Missing; s.ModifiedUtc = DateTime.UtcNow;
					xPvaDrawingEventRecord record = CreateRecord(s, xDrawingEventType.Missing, "StartupReconciliation", s.ModifiedUtc);
					ApplyRecord(record); Enqueue(record);
				}
			}
		}

		private xPvaDrawingSnapshot FindMatch(xPvaDrawingSnapshot incoming, out bool sourceIdentityMatch)
		{
			sourceIdentityMatch = false;
			if (!string.IsNullOrEmpty(incoming.SourceStableId))
				foreach (xPvaDrawingSnapshot candidate in current.Values)
					if (string.Equals(candidate.SourceStableId, incoming.SourceStableId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(candidate.Instrument, incoming.Instrument, StringComparison.OrdinalIgnoreCase)) { sourceIdentityMatch = true; return candidate; }

			if (!string.IsNullOrEmpty(incoming.NativeTag))
				foreach (xPvaDrawingSnapshot candidate in current.Values)
					if (candidate.Status != xDrawingStatus.Deleted && string.Equals(candidate.ChartIdentity, incoming.ChartIdentity, StringComparison.Ordinal)
						&& string.Equals(candidate.NativeTag, incoming.NativeTag, StringComparison.Ordinal)
						&& string.Equals(candidate.DrawingToolType, incoming.DrawingToolType, StringComparison.Ordinal)) return candidate;

			xPvaDrawingSnapshot best = null; int matches = 0;
			foreach (xPvaDrawingSnapshot candidate in current.Values)
			{
				if (candidate.Status == xDrawingStatus.Deleted || !string.Equals(candidate.Instrument, incoming.Instrument, StringComparison.OrdinalIgnoreCase)
					|| !string.Equals(candidate.DrawingToolType, incoming.DrawingToolType, StringComparison.Ordinal)) continue;
				if (AnchorsClose(candidate.Anchor1, incoming.Anchor1, incoming.TickSize) && AnchorsClose(candidate.Anchor2, incoming.Anchor2, incoming.TickSize)) { best = candidate; matches++; }
			}
			return matches == 1 ? best : null;
		}

		private bool AnchorsClose(xPvaDrawingAnchor left, xPvaDrawingAnchor right, double tickSize)
		{
			if (left == null || right == null) return left == right;
			double tolerance = tickSize > 0 ? tickSize * 0.01 : 0.0000001;
			return Math.Abs(left.Price - right.Price) <= tolerance && Math.Abs((left.TimeUtc - right.TimeUtc).TotalSeconds) <= 1.0;
		}

		private bool MateriallyEqual(xPvaDrawingSnapshot a, xPvaDrawingSnapshot b, bool includeBarIndexes)
		{
			return AnchorEqual(a.Anchor1, b.Anchor1, includeBarIndexes) && AnchorEqual(a.Anchor2, b.Anchor2, includeBarIndexes)
				&& AnchorEqual(a.Anchor3, b.Anchor3, includeBarIndexes) && a.Role == b.Role && a.StructuralLevel == b.StructuralLevel
				&& string.Equals(a.NativeTag, b.NativeTag, StringComparison.Ordinal) && string.Equals(a.ChartIdentity, b.ChartIdentity, StringComparison.Ordinal)
				&& string.Equals(a.WorkspaceIdentity, b.WorkspaceIdentity, StringComparison.Ordinal) && string.Equals(a.BarsPeriodType, b.BarsPeriodType, StringComparison.Ordinal)
				&& a.BarsPeriodValue == b.BarsPeriodValue && string.Equals(a.TradingHoursTemplate, b.TradingHoursTemplate, StringComparison.Ordinal)
				&& string.Equals(a.DashStyle, b.DashStyle, StringComparison.Ordinal) && Math.Abs(a.Width - b.Width) < 0.0001
				&& a.ExtendLeft == b.ExtendLeft && a.ExtendRight == b.ExtendRight && a.IsRay == b.IsRay
				&& a.IsLocked == b.IsLocked && a.IsVisible == b.IsVisible && string.Equals(a.RawMetadataJson, b.RawMetadataJson, StringComparison.Ordinal)
				&& (a.Status == xDrawingStatus.Active || a.Status == xDrawingStatus.Reconciled);
		}

		private bool AnchorEqual(xPvaDrawingAnchor a, xPvaDrawingAnchor b, bool includeBarIndex)
		{
			if (a == null || b == null) return a == b;
			return a.TimeUtc == b.TimeUtc && Math.Abs(a.Price - b.Price) < 0.000000001 && (!includeBarIndex || a.BarIndexAtCapture == b.BarIndexAtCapture);
		}

		private xDrawingEventType ClassifyChange(xPvaDrawingSnapshot a, xPvaDrawingSnapshot b)
		{
			if (!AnchorEqual(a.Anchor1, b.Anchor1, true) || !AnchorEqual(a.Anchor2, b.Anchor2, true) || !AnchorEqual(a.Anchor3, b.Anchor3, true)) return xDrawingEventType.AnchorChanged;
			if (a.Role != b.Role) return xDrawingEventType.RoleChanged;
			if (a.StructuralLevel != b.StructuralLevel) return xDrawingEventType.StructuralLevelChanged;
			if (!string.Equals(JhMetadataValue(a.RawMetadataJson, "componentRole"), JhMetadataValue(b.RawMetadataJson, "componentRole"), StringComparison.Ordinal)) return xDrawingEventType.RoleChanged;
			if (a.ExtendLeft != b.ExtendLeft || a.ExtendRight != b.ExtendRight || a.IsRay != b.IsRay) return xDrawingEventType.ExtensionChanged;
			if (a.IsVisible != b.IsVisible) return xDrawingEventType.VisibilityChanged;
			if (a.IsLocked != b.IsLocked) return xDrawingEventType.LockedChanged;
			return xDrawingEventType.StyleChanged;
		}

		private string JhMetadataValue(string json, string name)
		{
			if (string.IsNullOrEmpty(json)) return string.Empty; string marker = "\"" + name + "\":\"";
			int start = json.IndexOf(marker, StringComparison.Ordinal); if (start < 0) return string.Empty; start += marker.Length;
			int end = json.IndexOf('"', start); return end < 0 ? string.Empty : json.Substring(start, end - start);
		}

		private xPvaDrawingEventRecord CreateRecord(xPvaDrawingSnapshot snapshot, xDrawingEventType type, string source, DateTime utc)
		{
			xPvaDrawingEventRecord record = new xPvaDrawingEventRecord
			{
				DrawingEventId = Guid.NewGuid().ToString("N"), DrawingId = snapshot.DrawingId,
				Revision = snapshot.Revision, EventType = type, TimestampUtc = utc,
				TimestampLocal = utc.ToLocalTime(), UserActionSource = source ?? string.Empty,
				SequenceNumber = Interlocked.Increment(ref sequence), Snapshot = snapshot.Clone()
			};
			record.Snapshot.LastEventId = record.DrawingEventId; return record;
		}

		private void ApplyRecord(xPvaDrawingEventRecord record)
		{
			current[record.DrawingId] = record.Snapshot.Clone();
			List<xPvaDrawingEventRecord> events;
			if (!history.TryGetValue(record.DrawingId, out events)) { events = new List<xPvaDrawingEventRecord>(); history[record.DrawingId] = events; }
			events.Add(record.Clone()); status.EventCount++; status.LastEventType = record.EventType; UpdateActiveCount();
		}

		private void Enqueue(xPvaDrawingEventRecord record)
		{
			if (!disposed) queue.Add(record.Clone());
		}

		private void ProcessQueue()
		{
			while (!disposed)
			{
				xPvaDrawingEventRecord record;
				if (queue.TryTake(out record, 1000)) Persist(record, true);
				RetryFailed();
			}
		}

		private void Persist(xPvaDrawingEventRecord record, bool retainForRetry)
		{
			try
			{
				if (!database.IsConnected) throw new InvalidOperationException("SQLite is unavailable.");
				database.UpsertChartContext(record.Snapshot); database.PersistEvent(record);
				status.DatabaseConnected = true; WriteJsonSnapshot(record.Snapshot.ChartIdentity, record.Snapshot.Instrument);
			}
			catch (Exception ex)
			{
				status.DatabaseConnected = false; status.JsonCurrent = false; SetError("Drawing persistence failed: " + ex.Message);
				WriteEmergency(record, ex);
				if (retainForRetry) lock (retryQueue) if (retryQueue.Count < maxEmergencyQueueRecords) retryQueue.Add(record.Clone());
			}
		}

		private void RetryFailed()
		{
			if (!database.IsConnected)
			{
				if ((DateTime.UtcNow - lastReconnectAttemptUtc).TotalSeconds < 5) return;
				lastReconnectAttemptUtc = DateTime.UtcNow;
				try
				{
					database.Open(databasePath); if (string.IsNullOrEmpty(sessionId)) sessionId = Guid.NewGuid().ToString("N");
					database.InsertSession(sessionId, diagnosticLevel.ToString());
					lock (sync) { RestoreDatabaseState(); ImportEmergencyFiles(); status.DatabaseConnected = true; }
				}
				catch (Exception ex) { SetError("Database reconnect failed: " + ex.Message); return; }
			}
			xPvaDrawingEventRecord record = null;
			lock (retryQueue) if (retryQueue.Count > 0) { record = retryQueue[0]; retryQueue.RemoveAt(0); }
			if (record != null) Persist(record, true);
		}

		private void RestoreDatabaseState()
		{
			List<xPvaDrawingSnapshot> loaded = database.LoadCurrent();
			for (int i = 0; i < loaded.Count; i++)
			{
				xPvaDrawingSnapshot drawing = loaded[i]; xPvaDrawingSnapshot existing;
				if (!current.TryGetValue(drawing.DrawingId, out existing) || drawing.Revision > existing.Revision) current[drawing.DrawingId] = drawing;
				List<xPvaDrawingEventRecord> databaseEvents = database.LoadHistory(drawing.DrawingId); List<xPvaDrawingEventRecord> localEvents;
				if (!history.TryGetValue(drawing.DrawingId, out localEvents)) { localEvents = new List<xPvaDrawingEventRecord>(); history[drawing.DrawingId] = localEvents; }
				for (int j = 0; j < databaseEvents.Count; j++) if (!ContainsEvent(localEvents, databaseEvents[j].DrawingEventId)) localEvents.Add(databaseEvents[j]);
				localEvents.Sort(delegate(xPvaDrawingEventRecord a, xPvaDrawingEventRecord b) { return a.Revision.CompareTo(b.Revision); });
			}
			status.EventCount = 0; sequence = 0;
			foreach (List<xPvaDrawingEventRecord> events in history.Values)
			{
				status.EventCount += events.Count;
				for (int i = 0; i < events.Count; i++) sequence = Math.Max(sequence, events[i].SequenceNumber);
				if (events.Count > 0) status.LastEventType = events[events.Count - 1].EventType;
			}
			UpdateActiveCount();
		}

		private void ImportEmergencyFiles()
		{
			if (!database.IsConnected || !Directory.Exists(emergencyRoot)) return;
			string[] files = Directory.GetFiles(emergencyRoot, "xPvaDrawingRecorder_EMERGENCY_*.jsonl");
			for (int f = 0; f < files.Length; f++)
			{
				foreach (string line in File.ReadLines(files[f]))
				{
					try
					{
						string marker = "\"payload\":\""; int start = line.IndexOf(marker, StringComparison.Ordinal);
						if (start < 0) continue; start += marker.Length; int end = line.IndexOf('"', start); if (end <= start) continue;
						xPvaDrawingEventRecord record = DeserializeEmergencyPayload(line.Substring(start, end - start));
						List<xPvaDrawingEventRecord> events; if (record == null) continue;
						if (history.TryGetValue(record.DrawingId, out events) && ContainsEvent(events, record.DrawingEventId)) continue;
						xPvaDrawingSnapshot existing; if (current.TryGetValue(record.DrawingId, out existing) && existing.Revision >= record.Revision) continue;
						database.UpsertChartContext(record.Snapshot); database.PersistEvent(record); sequence = Math.Max(sequence, record.SequenceNumber);
						ApplyRecord(record); WriteJsonSnapshot(record.Snapshot.ChartIdentity, record.Snapshot.Instrument);
					}
					catch (Exception ex) { SetError("Emergency import failed: " + ex.Message); }
				}
			}
		}

		private bool ContainsEvent(List<xPvaDrawingEventRecord> events, string eventId)
		{
			for (int i = 0; i < events.Count; i++) if (string.Equals(events[i].DrawingEventId, eventId, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		private void WriteJsonSnapshot(string chartIdentity, string instrument)
		{
			List<xPvaDrawingSnapshot> drawings = new List<xPvaDrawingSnapshot>(); List<xPvaDrawingEventRecord> events = new List<xPvaDrawingEventRecord>();
			lock (sync)
			{
				foreach (xPvaDrawingSnapshot s in current.Values) if (string.Equals(s.ChartIdentity, chartIdentity, StringComparison.Ordinal)) drawings.Add(s.Clone());
				foreach (List<xPvaDrawingEventRecord> list in history.Values) for (int i = 0; i < list.Count; i++) if (list[i].Snapshot != null && string.Equals(list[i].Snapshot.ChartIdentity, chartIdentity, StringComparison.Ordinal)) events.Add(list[i].Clone());
			}
			events.Sort(delegate(xPvaDrawingEventRecord a, xPvaDrawingEventRecord b) { return a.SequenceNumber.CompareTo(b.SequenceNumber); });
			string safeInstrument = SafeFileName(instrument); string shortId = StableShortHash(chartIdentity);
			string path = Path.Combine(sessionsRoot, "xPvaDrawings_" + safeInstrument + "_" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "_" + shortId + ".json");
			string temp = path + ".tmp"; File.WriteAllText(temp, BuildJson(chartIdentity, drawings, events), new UTF8Encoding(false));
			if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
			status.JsonCurrent = true; status.JsonPath = path;
		}

		private string BuildJson(string chartIdentity, List<xPvaDrawingSnapshot> drawings, List<xPvaDrawingEventRecord> events)
		{
			xPvaDrawingSnapshot context = drawings.Count == 0 ? null : drawings[0]; StringBuilder sb = new StringBuilder(4096);
			sb.Append("{\n  \"schemaVersion\": 1,\n  \"generatedUtc\": \"").Append(E(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))).Append("\",");
			sb.Append("\n  \"instrument\": \"").Append(E(context == null ? string.Empty : context.Instrument)).Append("\",");
			sb.Append("\n  \"masterInstrument\": \"").Append(E(context == null ? string.Empty : context.MasterInstrument)).Append("\",");
			sb.Append("\n  \"barsPeriod\": { \"type\": \"").Append(E(context == null ? string.Empty : context.BarsPeriodType)).Append("\", \"value\": ").Append(context == null ? 0 : context.BarsPeriodValue).Append(" },");
			sb.Append("\n  \"tradingHours\": \"").Append(E(context == null ? string.Empty : context.TradingHoursTemplate)).Append("\",");
			sb.Append("\n  \"chartIdentity\": \"").Append(E(chartIdentity)).Append("\",");
			AppendDrawingArray(sb, "activeDrawings", drawings, xDrawingStatus.Active); sb.Append(',');
			AppendDrawingArray(sb, "reconciledDrawings", drawings, xDrawingStatus.Reconciled); sb.Append(',');
			AppendDrawingArray(sb, "missingDrawings", drawings, xDrawingStatus.Missing); sb.Append(',');
			AppendDrawingArray(sb, "deletedDrawings", drawings, xDrawingStatus.Deleted);
			sb.Append(",\n  \"events\": [");
			for (int i = 0; i < events.Count; i++) { if (i > 0) sb.Append(','); sb.Append("\n    { \"eventId\": \"").Append(E(events[i].DrawingEventId)).Append("\", \"drawingId\": \"").Append(E(events[i].DrawingId)).Append("\", \"revision\": ").Append(events[i].Revision).Append(", \"eventType\": \"").Append(events[i].EventType).Append("\", \"timestampUtc\": \"").Append(E(events[i].TimestampUtc.ToString("o", CultureInfo.InvariantCulture))).Append("\", \"sequenceNumber\": ").Append(events[i].SequenceNumber).Append(" }"); }
			sb.Append("\n  ]\n}\n"); return sb.ToString();
		}

		private void AppendDrawingArray(StringBuilder sb, string name, List<xPvaDrawingSnapshot> drawings, xDrawingStatus requiredStatus)
		{
			sb.Append("\n  \"").Append(name).Append("\": ["); bool first = true;
			for (int i = 0; i < drawings.Count; i++)
			{
				xPvaDrawingSnapshot s = drawings[i]; if (s.Status != requiredStatus) continue;
				if (!first) sb.Append(','); first = false; sb.Append("\n    {");
				sb.Append(" \"drawingId\": \"").Append(E(s.DrawingId)).Append("\", \"nativeTag\": \"").Append(E(s.NativeTag)).Append("\", \"revision\": ").Append(s.Revision);
				sb.Append(", \"status\": \"").Append(s.Status).Append("\", \"role\": \"").Append(s.Role).Append("\", \"structuralLevel\": \"").Append(s.StructuralLevel).Append("\", \"drawingToolType\": \"").Append(E(s.DrawingToolType)).Append("\",");
				AppendAnchorJson(sb, "anchor1", s.Anchor1); sb.Append(','); AppendAnchorJson(sb, "anchor2", s.Anchor2); if (s.Anchor3 != null) { sb.Append(','); AppendAnchorJson(sb, "anchor3", s.Anchor3); }
				sb.Append(", \"style\": { \"dashStyle\": \"").Append(E(s.DashStyle)).Append("\", \"width\": ").Append(s.Width.ToString("R", CultureInfo.InvariantCulture)).Append(", \"extendLeft\": ").Append(B(s.ExtendLeft)).Append(", \"extendRight\": ").Append(B(s.ExtendRight)).Append(", \"isRay\": ").Append(B(s.IsRay)).Append(" },");
				sb.Append(" \"createdUtc\": \"").Append(Iso(s.CreatedUtc)).Append("\", \"modifiedUtc\": \"").Append(Iso(s.ModifiedUtc)).Append("\", \"deletedUtc\": \"").Append(Iso(s.DeletedUtc)).Append("\", \"relatedDrawingId\": \"").Append(E(s.RelatedDrawingId)).Append("\", \"relationshipType\": \"").Append(s.RelationshipType).Append("\" }");
			}
			sb.Append("\n  ]");
		}

		private void AppendAnchorJson(StringBuilder sb, string name, xPvaDrawingAnchor a)
		{
			sb.Append(" \"").Append(name).Append("\": "); if (a == null) { sb.Append("null"); return; }
			sb.Append("{ \"timeUtc\": \"").Append(Iso(a.TimeUtc)).Append("\", \"timeLocal\": \"").Append(Iso(a.TimeLocal)).Append("\", \"price\": ").Append(a.Price.ToString("R", CultureInfo.InvariantCulture)).Append(", \"barIndexAtCapture\": ").Append(a.BarIndexAtCapture).Append(" }");
		}

		private void WriteEmergency(xPvaDrawingEventRecord record, Exception ex)
		{
			try
			{
				string path = Path.Combine(emergencyRoot, "xPvaDrawingRecorder_EMERGENCY_" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".jsonl");
				string line = "{\"schemaVersion\":1,\"eventId\":\"" + E(record.DrawingEventId) + "\",\"drawingId\":\"" + E(record.DrawingId) + "\",\"revision\":" + record.Revision.ToString(CultureInfo.InvariantCulture) + ",\"eventType\":\"" + record.EventType + "\",\"sequenceNumber\":" + record.SequenceNumber.ToString(CultureInfo.InvariantCulture) + ",\"payload\":\"" + SerializeEmergencyPayload(record) + "\",\"error\":\"" + E(ex == null ? string.Empty : ex.Message) + "\"}";
				File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
			}
			catch { }
		}

		private string SerializeEmergencyPayload(xPvaDrawingEventRecord record)
		{
			using (MemoryStream stream = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
			{
				writer.Write(1); W(writer, record.DrawingEventId); W(writer, record.DrawingId); writer.Write(record.Revision);
				writer.Write((int)record.EventType); writer.Write(record.TimestampUtc.ToBinary()); writer.Write(record.TimestampLocal.ToBinary());
				W(writer, record.UserActionSource); writer.Write(record.SequenceNumber); xPvaDrawingSnapshot s = record.Snapshot;
				W(writer, s.DrawingId); W(writer, s.SourceStableId); W(writer, s.NativeTag); W(writer, s.DrawingToolType);
				W(writer, s.Instrument); W(writer, s.MasterInstrument); W(writer, s.ChartIdentity); W(writer, s.WorkspaceIdentity);
				W(writer, s.BarsPeriodType); writer.Write(s.BarsPeriodValue); W(writer, s.TradingHoursTemplate); writer.Write(s.TickSize);
				writer.Write(s.Revision); writer.Write((int)s.Status); writer.Write((int)s.Role); writer.Write((int)s.StructuralLevel);
				WriteAnchor(writer, s.Anchor1); WriteAnchor(writer, s.Anchor2); WriteAnchor(writer, s.Anchor3);
				W(writer, s.DashStyle); writer.Write(s.Width); writer.Write(s.ExtendLeft); writer.Write(s.ExtendRight); writer.Write(s.IsRay);
				writer.Write(s.IsLocked); writer.Write(s.IsVisible); writer.Write(s.CreatedUtc.ToBinary()); writer.Write(s.ModifiedUtc.ToBinary());
				writer.Write(s.DeletedUtc.ToBinary()); W(writer, s.LastEventId); W(writer, s.RelatedDrawingId);
				writer.Write((int)s.RelationshipType); W(writer, s.RawMetadataJson); writer.Flush(); return Convert.ToBase64String(stream.ToArray());
			}
		}

		private xPvaDrawingEventRecord DeserializeEmergencyPayload(string payload)
		{
			using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(payload)))
			using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
			{
				if (reader.ReadInt32() != 1) return null;
				xPvaDrawingEventRecord record = new xPvaDrawingEventRecord(); record.DrawingEventId = reader.ReadString();
				record.DrawingId = reader.ReadString(); record.Revision = reader.ReadInt32(); record.EventType = (xDrawingEventType)reader.ReadInt32();
				record.TimestampUtc = DateTime.FromBinary(reader.ReadInt64()); record.TimestampLocal = DateTime.FromBinary(reader.ReadInt64());
				record.UserActionSource = reader.ReadString(); record.SequenceNumber = reader.ReadInt64(); xPvaDrawingSnapshot s = new xPvaDrawingSnapshot();
				s.DrawingId = reader.ReadString(); s.SourceStableId = reader.ReadString(); s.NativeTag = reader.ReadString(); s.DrawingToolType = reader.ReadString();
				s.Instrument = reader.ReadString(); s.MasterInstrument = reader.ReadString(); s.ChartIdentity = reader.ReadString(); s.WorkspaceIdentity = reader.ReadString();
				s.BarsPeriodType = reader.ReadString(); s.BarsPeriodValue = reader.ReadInt32(); s.TradingHoursTemplate = reader.ReadString(); s.TickSize = reader.ReadDouble();
				s.Revision = reader.ReadInt32(); s.Status = (xDrawingStatus)reader.ReadInt32(); s.Role = (xDrawingRole)reader.ReadInt32(); s.StructuralLevel = (xStructuralLevel)reader.ReadInt32();
				s.Anchor1 = ReadAnchor(reader); s.Anchor2 = ReadAnchor(reader); s.Anchor3 = ReadAnchor(reader); s.DashStyle = reader.ReadString(); s.Width = reader.ReadDouble();
				s.ExtendLeft = reader.ReadBoolean(); s.ExtendRight = reader.ReadBoolean(); s.IsRay = reader.ReadBoolean(); s.IsLocked = reader.ReadBoolean(); s.IsVisible = reader.ReadBoolean();
				s.CreatedUtc = DateTime.FromBinary(reader.ReadInt64()); s.ModifiedUtc = DateTime.FromBinary(reader.ReadInt64()); s.DeletedUtc = DateTime.FromBinary(reader.ReadInt64());
				s.LastEventId = reader.ReadString(); s.RelatedDrawingId = reader.ReadString(); s.RelationshipType = (xDrawingRelationshipType)reader.ReadInt32(); s.RawMetadataJson = reader.ReadString();
				record.Snapshot = s; return record;
			}
		}

		private void W(BinaryWriter writer, string value) { writer.Write(value ?? string.Empty); }
		private void WriteAnchor(BinaryWriter writer, xPvaDrawingAnchor anchor) { writer.Write(anchor != null); if (anchor != null) { writer.Write(anchor.TimeLocal.ToBinary()); writer.Write(anchor.Price); writer.Write(anchor.BarIndexAtCapture); } }
		private xPvaDrawingAnchor ReadAnchor(BinaryReader reader) { return reader.ReadBoolean() ? new xPvaDrawingAnchor(DateTime.FromBinary(reader.ReadInt64()), reader.ReadDouble(), reader.ReadInt32()) : null; }

		private string NextDrawingId(string master, DateTime utc)
		{
			string prefix = string.IsNullOrWhiteSpace(master) ? "DRAW" : SafeFileName(master).ToUpperInvariant();
			int counter = 1; string date = utc.ToLocalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			foreach (string id in current.Keys) if (id.StartsWith(prefix + "-" + date + "-DRAW-", StringComparison.OrdinalIgnoreCase)) { int n; if (int.TryParse(id.Substring(id.Length - 6), out n)) counter = Math.Max(counter, n + 1); }
			return prefix + "-" + date + "-DRAW-" + counter.ToString("D6", CultureInfo.InvariantCulture);
		}

		private void UpdateActiveCount() { int count = 0; foreach (xPvaDrawingSnapshot s in current.Values) if (s.Status == xDrawingStatus.Active || s.Status == xDrawingStatus.Reconciled) count++; status.ActiveDrawings = count; }
		private void SetError(string message) { lock (sync) { status.ErrorCount++; status.LastError = message; } }
		private void LogSystem(string type, string message) { if (diagnosticLevel == xDrawingDiagnosticLevel.Verbose) System.Diagnostics.Trace.WriteLine("[xPvaDrawingRecorder] " + type + ": " + message); }

		public xPvaDrawingRecorderStatus GetStatus()
		{
			lock (sync) return new xPvaDrawingRecorderStatus { ActiveDrawings = status.ActiveDrawings, EventCount = status.EventCount,
				LastEventType = status.LastEventType, DatabaseConnected = status.DatabaseConnected, JsonCurrent = status.JsonCurrent,
				ErrorCount = status.ErrorCount, LastError = status.LastError, DatabasePath = status.DatabasePath, JsonPath = status.JsonPath };
		}

		internal static bool TryGetActiveDrawingsShared(string instrument, DateTime timestampUtc, out IReadOnlyList<xPvaDrawingSnapshot> drawings)
		{
			lock (staticSync) { if (instance == null) { drawings = new ReadOnlyCollection<xPvaDrawingSnapshot>(new List<xPvaDrawingSnapshot>()); return false; } return instance.TryGetActiveDrawings(instrument, timestampUtc, out drawings); }
		}

		private bool TryGetActiveDrawings(string instrument, DateTime timestampUtc, out IReadOnlyList<xPvaDrawingSnapshot> drawings)
		{
			List<xPvaDrawingSnapshot> result = new List<xPvaDrawingSnapshot>(); DateTime cutoff = timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc : timestampUtc.ToUniversalTime();
			lock (sync)
			{
				foreach (List<xPvaDrawingEventRecord> events in history.Values)
				{
					xPvaDrawingEventRecord selected = null; for (int i = 0; i < events.Count; i++) if (events[i].TimestampUtc <= cutoff && (selected == null || events[i].Revision > selected.Revision)) selected = events[i];
					if (selected == null || selected.Snapshot == null || !string.Equals(selected.Snapshot.Instrument, instrument, StringComparison.OrdinalIgnoreCase)) continue;
					if (selected.EventType != xDrawingEventType.Deleted && selected.EventType != xDrawingEventType.Missing) result.Add(selected.Snapshot.Clone());
				}
			}
			drawings = new ReadOnlyCollection<xPvaDrawingSnapshot>(result); return result.Count > 0;
		}

		internal static bool TryGetDrawingHistoryShared(string drawingId, out IReadOnlyList<xPvaDrawingEventRecord> events)
		{
			lock (staticSync) { if (instance == null) { events = new ReadOnlyCollection<xPvaDrawingEventRecord>(new List<xPvaDrawingEventRecord>()); return false; } return instance.TryGetDrawingHistory(drawingId, out events); }
		}

		private bool TryGetDrawingHistory(string drawingId, out IReadOnlyList<xPvaDrawingEventRecord> result)
		{
			List<xPvaDrawingEventRecord> copy = new List<xPvaDrawingEventRecord>(); lock (sync) { List<xPvaDrawingEventRecord> events; if (history.TryGetValue(drawingId ?? string.Empty, out events)) for (int i = 0; i < events.Count; i++) copy.Add(events[i].Clone()); }
			result = new ReadOnlyCollection<xPvaDrawingEventRecord>(copy); return copy.Count > 0;
		}

		internal static bool TryGetChartDrawingStateShared(string chartIdentity, out xPvaChartDrawingState state)
		{
			lock (staticSync) { if (instance == null) { state = null; return false; } return instance.TryGetChartDrawingState(chartIdentity, out state); }
		}

		private bool TryGetChartDrawingState(string chartIdentity, out xPvaChartDrawingState state)
		{
			List<xPvaDrawingSnapshot> result = new List<xPvaDrawingSnapshot>(); lock (sync) foreach (xPvaDrawingSnapshot s in current.Values) if (string.Equals(s.ChartIdentity, chartIdentity, StringComparison.Ordinal)) result.Add(s.Clone());
			state = xPvaChartDrawingState.Create(chartIdentity, result); return result.Count > 0;
		}

		private string SafeFileName(string value) { if (string.IsNullOrEmpty(value)) return "Unknown"; foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return value.Replace(' ', '_'); }
		private string StableShortHash(string value) { using (MD5 md5 = MD5.Create()) { byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)); return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty); } }
		private string E(string value) { return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n"); }
		private string Iso(DateTime value) { return value == DateTime.MinValue ? string.Empty : E(value.ToString("o", CultureInfo.InvariantCulture)); }
		private string B(bool value) { return value ? "true" : "false"; }

		public void Dispose()
		{
			if (disposed) return; disposed = true; queue.CompleteAdding();
			try { if (worker != null && worker.IsAlive) worker.Join(2000); } catch { }
			try { if (database.IsConnected && !string.IsNullOrEmpty(sessionId)) database.EndSession(sessionId); } catch { }
			database.Dispose();
		}
	}
}
