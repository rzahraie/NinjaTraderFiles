#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder
{
	internal sealed class xPvaDrawingRecorderDatabase : IDisposable
	{
		private sealed class xSqlParam
		{
			public string Name;
			public object Value;
			public xSqlParam(string name, object value) { Name = name; Value = value; }
		}

		private readonly object sync = new object();
		private object connection;
		private Type connectionType;
		public string DatabasePath { get; private set; }
		public bool IsConnected { get { return connection != null; } }

		public void Open(string path)
		{
			lock (sync)
			{
				try
				{
					DatabasePath = path;
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					connectionType = ResolveConnectionType();
					ConstructorInfo constructor = connectionType.GetConstructor(new Type[] { typeof(string) });
					if (constructor == null)
						throw new MissingMethodException(connectionType.FullName, ".ctor(string)");
					connection = constructor.Invoke(new object[] { "Data Source=" + path + ";Version=3;Journal Mode=WAL;" });
					Invoke(connection, "Open");
					ExecuteNonQuery("PRAGMA journal_mode=WAL;");
					ExecuteNonQuery("PRAGMA synchronous=FULL;");
					CreateSchema();
				}
				catch
				{
					if (connection != null) { try { Invoke(connection, "Close"); } catch { } DisposeObject(connection); connection = null; }
					throw;
				}
			}
		}

		private void CreateSchema()
		{
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS SchemaVersion (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS RecorderSession (SessionId TEXT PRIMARY KEY, StartedUtc TEXT NOT NULL, EndedUtc TEXT, DiagnosticLevel TEXT, DatabasePath TEXT);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS ChartContext (ChartIdentity TEXT PRIMARY KEY, WorkspaceIdentity TEXT, Instrument TEXT, MasterInstrument TEXT, BarsPeriodType TEXT, BarsPeriodValue INTEGER, TradingHoursTemplate TEXT, TickSize REAL, FirstSeenUtc TEXT, LastSeenUtc TEXT);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS DrawingEvent (DrawingEventId TEXT PRIMARY KEY, DrawingId TEXT NOT NULL, Revision INTEGER NOT NULL, EventType TEXT NOT NULL, TimestampUtc TEXT NOT NULL, TimestampLocal TEXT NOT NULL, Instrument TEXT, MasterInstrument TEXT, ChartIdentity TEXT, WorkspaceIdentity TEXT, SourceStableId TEXT, NativeTag TEXT, DrawingToolType TEXT, Anchor1TimeUtc TEXT, Anchor1TimeLocal TEXT, Anchor1Price REAL, Anchor1BarIndex INTEGER, Anchor2TimeUtc TEXT, Anchor2TimeLocal TEXT, Anchor2Price REAL, Anchor2BarIndex INTEGER, Anchor3TimeUtc TEXT, Anchor3TimeLocal TEXT, Anchor3Price REAL, Anchor3BarIndex INTEGER, Role TEXT, StructuralLevel TEXT, DashStyle TEXT, Width REAL, ExtendLeft INTEGER, ExtendRight INTEGER, IsRay INTEGER, IsLocked INTEGER, IsVisible INTEGER, UserActionSource TEXT, RelatedDrawingId TEXT, RelationshipType TEXT, RawMetadataJson TEXT, SequenceNumber INTEGER NOT NULL, UNIQUE(DrawingId, Revision), UNIQUE(SequenceNumber));");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS DrawingCurrent (DrawingId TEXT PRIMARY KEY, CurrentRevision INTEGER NOT NULL, Status TEXT NOT NULL, Instrument TEXT, MasterInstrument TEXT, ChartIdentity TEXT, WorkspaceIdentity TEXT, SourceStableId TEXT, NativeTag TEXT, DrawingToolType TEXT, Role TEXT, StructuralLevel TEXT, Anchor1TimeUtc TEXT, Anchor1TimeLocal TEXT, Anchor1Price REAL, Anchor1BarIndex INTEGER, Anchor2TimeUtc TEXT, Anchor2TimeLocal TEXT, Anchor2Price REAL, Anchor2BarIndex INTEGER, Anchor3TimeUtc TEXT, Anchor3TimeLocal TEXT, Anchor3Price REAL, Anchor3BarIndex INTEGER, DashStyle TEXT, Width REAL, ExtendLeft INTEGER, ExtendRight INTEGER, IsRay INTEGER, IsLocked INTEGER, IsVisible INTEGER, CreatedUtc TEXT, ModifiedUtc TEXT, DeletedUtc TEXT, LastEventId TEXT, RelatedDrawingId TEXT, RelationshipType TEXT, RawMetadataJson TEXT);");
			ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_DrawingEvent_DrawingTime ON DrawingEvent(DrawingId, TimestampUtc);");
			ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_DrawingEvent_InstrumentTime ON DrawingEvent(Instrument, TimestampUtc);");
			ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_DrawingCurrent_Chart ON DrawingCurrent(ChartIdentity, Status);");
			ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_DrawingCurrent_Source ON DrawingCurrent(SourceStableId, Instrument);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS RecorderSystemEvent (SystemEventId TEXT PRIMARY KEY, TimestampUtc TEXT, EventType TEXT, Message TEXT, RawJson TEXT, SequenceNumber INTEGER);");
			object count = ExecuteScalar("SELECT COUNT(*) FROM SchemaVersion WHERE Version=1;");
			if (Convert.ToInt32(count, CultureInfo.InvariantCulture) == 0)
				ExecuteNonQuery("INSERT INTO SchemaVersion(Version,AppliedUtc) VALUES(1,@Utc);", P("@Utc", Iso(DateTime.UtcNow)));
		}

		public void InsertSession(string sessionId, string level)
		{
			ExecuteNonQuery("INSERT OR IGNORE INTO RecorderSession(SessionId,StartedUtc,DiagnosticLevel,DatabasePath) VALUES(@Id,@Utc,@Level,@Path);",
				P("@Id", sessionId), P("@Utc", Iso(DateTime.UtcNow)), P("@Level", level), P("@Path", DatabasePath));
		}

		public void EndSession(string sessionId)
		{
			ExecuteNonQuery("UPDATE RecorderSession SET EndedUtc=@Utc WHERE SessionId=@Id;", P("@Utc", Iso(DateTime.UtcNow)), P("@Id", sessionId));
		}

		public void UpsertChartContext(xPvaDrawingSnapshot s)
		{
			ExecuteNonQuery("INSERT OR REPLACE INTO ChartContext(ChartIdentity,WorkspaceIdentity,Instrument,MasterInstrument,BarsPeriodType,BarsPeriodValue,TradingHoursTemplate,TickSize,FirstSeenUtc,LastSeenUtc) VALUES(@Chart,@Workspace,@Instrument,@Master,@Type,@Value,@Hours,@Tick,COALESCE((SELECT FirstSeenUtc FROM ChartContext WHERE ChartIdentity=@Chart),@Utc),@Utc);",
				P("@Chart", s.ChartIdentity), P("@Workspace", s.WorkspaceIdentity), P("@Instrument", s.Instrument),
				P("@Master", s.MasterInstrument), P("@Type", s.BarsPeriodType), P("@Value", s.BarsPeriodValue),
				P("@Hours", s.TradingHoursTemplate), P("@Tick", s.TickSize), P("@Utc", Iso(DateTime.UtcNow)));
		}

		public void PersistEvent(xPvaDrawingEventRecord e)
		{
			lock (sync)
			{
				object transaction = Invoke(connection, "BeginTransaction");
				try
				{
					ExecuteNonQueryWithTransaction(transaction, EventInsertSql(), EventParameters(e));
					ExecuteNonQueryWithTransaction(transaction, CurrentUpsertSql(), CurrentParameters(e));
					Invoke(transaction, "Commit");
				}
				catch
				{
					try { Invoke(transaction, "Rollback"); } catch { }
					throw;
				}
				finally { DisposeObject(transaction); }
			}
		}

		private string EventInsertSql()
		{
			return "INSERT OR IGNORE INTO DrawingEvent(DrawingEventId,DrawingId,Revision,EventType,TimestampUtc,TimestampLocal,Instrument,MasterInstrument,ChartIdentity,WorkspaceIdentity,SourceStableId,NativeTag,DrawingToolType,Anchor1TimeUtc,Anchor1TimeLocal,Anchor1Price,Anchor1BarIndex,Anchor2TimeUtc,Anchor2TimeLocal,Anchor2Price,Anchor2BarIndex,Anchor3TimeUtc,Anchor3TimeLocal,Anchor3Price,Anchor3BarIndex,Role,StructuralLevel,DashStyle,Width,ExtendLeft,ExtendRight,IsRay,IsLocked,IsVisible,UserActionSource,RelatedDrawingId,RelationshipType,RawMetadataJson,SequenceNumber) VALUES(@EventId,@DrawingId,@Revision,@EventType,@Utc,@Local,@Instrument,@Master,@Chart,@Workspace,@Source,@Tag,@Tool,@A1Utc,@A1Local,@A1Price,@A1Bar,@A2Utc,@A2Local,@A2Price,@A2Bar,@A3Utc,@A3Local,@A3Price,@A3Bar,@Role,@Level,@Dash,@Width,@Left,@Right,@Ray,@Locked,@Visible,@Action,@Related,@Relationship,@Raw,@Sequence);";
		}

		private object[] EventParameters(xPvaDrawingEventRecord e)
		{
			xPvaDrawingSnapshot s = e.Snapshot;
			List<object> p = CommonSnapshotParameters(s);
			p.Add(P("@EventId", e.DrawingEventId)); p.Add(P("@DrawingId", e.DrawingId));
			p.Add(P("@Revision", e.Revision)); p.Add(P("@EventType", e.EventType.ToString()));
			p.Add(P("@Utc", Iso(e.TimestampUtc))); p.Add(P("@Local", Iso(e.TimestampLocal)));
			p.Add(P("@Action", e.UserActionSource)); p.Add(P("@Sequence", e.SequenceNumber));
			return p.ToArray();
		}

		private string CurrentUpsertSql()
		{
			return "INSERT OR REPLACE INTO DrawingCurrent(DrawingId,CurrentRevision,Status,Instrument,MasterInstrument,ChartIdentity,WorkspaceIdentity,SourceStableId,NativeTag,DrawingToolType,Role,StructuralLevel,Anchor1TimeUtc,Anchor1TimeLocal,Anchor1Price,Anchor1BarIndex,Anchor2TimeUtc,Anchor2TimeLocal,Anchor2Price,Anchor2BarIndex,Anchor3TimeUtc,Anchor3TimeLocal,Anchor3Price,Anchor3BarIndex,DashStyle,Width,ExtendLeft,ExtendRight,IsRay,IsLocked,IsVisible,CreatedUtc,ModifiedUtc,DeletedUtc,LastEventId,RelatedDrawingId,RelationshipType,RawMetadataJson) VALUES(@DrawingId,@Revision,@Status,@Instrument,@Master,@Chart,@Workspace,@Source,@Tag,@Tool,@Role,@Level,@A1Utc,@A1Local,@A1Price,@A1Bar,@A2Utc,@A2Local,@A2Price,@A2Bar,@A3Utc,@A3Local,@A3Price,@A3Bar,@Dash,@Width,@Left,@Right,@Ray,@Locked,@Visible,@Created,@Modified,@Deleted,@EventId,@Related,@Relationship,@Raw);";
		}

		private object[] CurrentParameters(xPvaDrawingEventRecord e)
		{
			xPvaDrawingSnapshot s = e.Snapshot;
			List<object> p = CommonSnapshotParameters(s);
			p.Add(P("@DrawingId", e.DrawingId)); p.Add(P("@Revision", e.Revision));
			p.Add(P("@Status", s.Status.ToString())); p.Add(P("@Created", Iso(s.CreatedUtc)));
			p.Add(P("@Modified", Iso(s.ModifiedUtc))); p.Add(P("@Deleted", Iso(s.DeletedUtc)));
			p.Add(P("@EventId", e.DrawingEventId));
			return p.ToArray();
		}

		private List<object> CommonSnapshotParameters(xPvaDrawingSnapshot s)
		{
			List<object> p = new List<object>();
			p.Add(P("@Instrument", s.Instrument)); p.Add(P("@Master", s.MasterInstrument));
			p.Add(P("@Chart", s.ChartIdentity)); p.Add(P("@Workspace", s.WorkspaceIdentity));
			p.Add(P("@Source", s.SourceStableId)); p.Add(P("@Tag", s.NativeTag)); p.Add(P("@Tool", s.DrawingToolType));
			AddAnchor(p, "A1", s.Anchor1); AddAnchor(p, "A2", s.Anchor2); AddAnchor(p, "A3", s.Anchor3);
			p.Add(P("@Role", s.Role.ToString())); p.Add(P("@Level", s.StructuralLevel.ToString()));
			p.Add(P("@Dash", s.DashStyle)); p.Add(P("@Width", s.Width)); p.Add(P("@Left", s.ExtendLeft ? 1 : 0));
			p.Add(P("@Right", s.ExtendRight ? 1 : 0)); p.Add(P("@Ray", s.IsRay ? 1 : 0));
			p.Add(P("@Locked", s.IsLocked ? 1 : 0)); p.Add(P("@Visible", s.IsVisible ? 1 : 0));
			p.Add(P("@Related", s.RelatedDrawingId)); p.Add(P("@Relationship", s.RelationshipType.ToString()));
			p.Add(P("@Raw", s.RawMetadataJson));
			return p;
		}

		private void AddAnchor(List<object> p, string prefix, xPvaDrawingAnchor a)
		{
			p.Add(P("@" + prefix + "Utc", a == null ? string.Empty : Iso(a.TimeUtc)));
			p.Add(P("@" + prefix + "Local", a == null ? string.Empty : Iso(a.TimeLocal)));
			p.Add(P("@" + prefix + "Price", a == null ? 0.0 : a.Price));
			p.Add(P("@" + prefix + "Bar", a == null ? -1 : a.BarIndexAtCapture));
		}

		public List<xPvaDrawingSnapshot> LoadCurrent()
		{
			List<xPvaDrawingSnapshot> rows = new List<xPvaDrawingSnapshot>();
			string sql = "SELECT DrawingId,CurrentRevision,Status,Instrument,MasterInstrument,ChartIdentity,WorkspaceIdentity,SourceStableId,NativeTag,DrawingToolType,Role,StructuralLevel,Anchor1TimeLocal,Anchor1Price,Anchor1BarIndex,Anchor2TimeLocal,Anchor2Price,Anchor2BarIndex,Anchor3TimeLocal,Anchor3Price,Anchor3BarIndex,DashStyle,Width,ExtendLeft,ExtendRight,IsRay,IsLocked,IsVisible,CreatedUtc,ModifiedUtc,DeletedUtc,LastEventId,RelatedDrawingId,RelationshipType,RawMetadataJson FROM DrawingCurrent;";
			ReadRows(sql, delegate(object reader)
			{
				xPvaDrawingSnapshot s = new xPvaDrawingSnapshot();
				s.DrawingId = S(reader, 0); s.Revision = I(reader, 1); s.Status = ParseEnum<xDrawingStatus>(S(reader, 2));
				s.Instrument = S(reader, 3); s.MasterInstrument = S(reader, 4); s.ChartIdentity = S(reader, 5);
				s.WorkspaceIdentity = S(reader, 6); s.SourceStableId = S(reader, 7); s.NativeTag = S(reader, 8);
				s.DrawingToolType = S(reader, 9); s.Role = ParseEnum<xDrawingRole>(S(reader, 10));
				s.StructuralLevel = ParseEnum<xStructuralLevel>(S(reader, 11));
				s.Anchor1 = ReadAnchor(reader, 12); s.Anchor2 = ReadAnchor(reader, 15); s.Anchor3 = ReadAnchor(reader, 18);
				s.DashStyle = S(reader, 21); s.Width = D(reader, 22); s.ExtendLeft = I(reader, 23) != 0;
				s.ExtendRight = I(reader, 24) != 0; s.IsRay = I(reader, 25) != 0; s.IsLocked = I(reader, 26) != 0;
				s.IsVisible = I(reader, 27) != 0; s.CreatedUtc = Dt(reader, 28); s.ModifiedUtc = Dt(reader, 29);
				s.DeletedUtc = Dt(reader, 30); s.LastEventId = S(reader, 31); s.RelatedDrawingId = S(reader, 32);
				s.RelationshipType = ParseEnum<xDrawingRelationshipType>(S(reader, 33)); s.RawMetadataJson = S(reader, 34);
				rows.Add(s);
			});
			return rows;
		}

		public List<xPvaDrawingEventRecord> LoadHistory(string drawingId)
		{
			List<xPvaDrawingEventRecord> rows = new List<xPvaDrawingEventRecord>();
			string sql = "SELECT DrawingEventId,DrawingId,Revision,EventType,TimestampUtc,TimestampLocal,UserActionSource,SequenceNumber,Instrument,MasterInstrument,ChartIdentity,WorkspaceIdentity,SourceStableId,NativeTag,DrawingToolType,Anchor1TimeLocal,Anchor1Price,Anchor1BarIndex,Anchor2TimeLocal,Anchor2Price,Anchor2BarIndex,Anchor3TimeLocal,Anchor3Price,Anchor3BarIndex,Role,StructuralLevel,DashStyle,Width,ExtendLeft,ExtendRight,IsRay,IsLocked,IsVisible,RelatedDrawingId,RelationshipType,RawMetadataJson FROM DrawingEvent WHERE DrawingId=@Id ORDER BY Revision;";
			ReadRows(sql, delegate(object reader)
			{
				xPvaDrawingEventRecord e = new xPvaDrawingEventRecord();
				e.DrawingEventId = S(reader, 0); e.DrawingId = S(reader, 1); e.Revision = I(reader, 2);
				e.EventType = ParseEnum<xDrawingEventType>(S(reader, 3)); e.TimestampUtc = Dt(reader, 4);
				e.TimestampLocal = Dt(reader, 5); e.UserActionSource = S(reader, 6); e.SequenceNumber = L(reader, 7);
				xPvaDrawingSnapshot s = new xPvaDrawingSnapshot { DrawingId = e.DrawingId, Revision = e.Revision,
					Instrument = S(reader, 8), MasterInstrument = S(reader, 9), ChartIdentity = S(reader, 10),
					WorkspaceIdentity = S(reader, 11), SourceStableId = S(reader, 12), NativeTag = S(reader, 13),
					DrawingToolType = S(reader, 14), Anchor1 = ReadAnchor(reader, 15), Anchor2 = ReadAnchor(reader, 18),
					Anchor3 = ReadAnchor(reader, 21), Role = ParseEnum<xDrawingRole>(S(reader, 24)),
					StructuralLevel = ParseEnum<xStructuralLevel>(S(reader, 25)), DashStyle = S(reader, 26), Width = D(reader, 27),
					ExtendLeft = I(reader, 28) != 0, ExtendRight = I(reader, 29) != 0, IsRay = I(reader, 30) != 0,
					IsLocked = I(reader, 31) != 0, IsVisible = I(reader, 32) != 0, RelatedDrawingId = S(reader, 33),
					RelationshipType = ParseEnum<xDrawingRelationshipType>(S(reader, 34)), RawMetadataJson = S(reader, 35) };
				e.Snapshot = s; rows.Add(e);
			}, P("@Id", drawingId));
			return rows;
		}

		private xPvaDrawingAnchor ReadAnchor(object reader, int start)
		{
			string time = S(reader, start);
			if (string.IsNullOrEmpty(time)) return null;
			return new xPvaDrawingAnchor(Dt(reader, start), D(reader, start + 1), I(reader, start + 2));
		}

		private void ReadRows(string sql, Action<object> consume, params object[] parameters)
		{
			lock (sync)
			{
				object cmd = CreateCommand(sql, null);
				try
				{
					AddParameters(cmd, parameters); object reader = Invoke(cmd, "ExecuteReader");
					try { while (Convert.ToBoolean(Invoke(reader, "Read"), CultureInfo.InvariantCulture)) consume(reader); }
					finally { DisposeObject(reader); }
				}
				finally { DisposeObject(cmd); }
			}
		}

		public void Dispose()
		{
			lock (sync)
			{
				if (connection == null) return;
				try { Invoke(connection, "Close"); } finally { DisposeObject(connection); connection = null; }
			}
		}

		private Type ResolveConnectionType()
		{
			Type type = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite", false);
			if (type != null) return type;
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = assembly.GetType("System.Data.SQLite.SQLiteConnection", false);
				if (type != null) return type;
			}
			string[] paths = {
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NinjaTrader 8", "bin", "System.Data.SQLite.dll"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.Data.SQLite.dll") };
			foreach (string path in paths)
				if (File.Exists(path)) { type = Assembly.LoadFrom(path).GetType("System.Data.SQLite.SQLiteConnection", false); if (type != null) return type; }
			throw new InvalidOperationException("System.Data.SQLite.SQLiteConnection could not be loaded.");
		}

		private int ExecuteNonQuery(string sql, params object[] parameters)
		{
			lock (sync) { return ExecuteNonQueryWithTransaction(null, sql, parameters); }
		}

		private int ExecuteNonQueryWithTransaction(object transaction, string sql, params object[] parameters)
		{
			object cmd = CreateCommand(sql, transaction);
			try { AddParameters(cmd, parameters); return Convert.ToInt32(Invoke(cmd, "ExecuteNonQuery"), CultureInfo.InvariantCulture); }
			finally { DisposeObject(cmd); }
		}

		private object ExecuteScalar(string sql, params object[] parameters)
		{
			lock (sync)
			{
				object cmd = CreateCommand(sql, null);
				try { AddParameters(cmd, parameters); return Invoke(cmd, "ExecuteScalar"); }
				finally { DisposeObject(cmd); }
			}
		}

		private object CreateCommand(string sql, object transaction)
		{
			object cmd = Invoke(connection, "CreateCommand"); SetProperty(cmd, "CommandText", sql);
			if (transaction != null) SetProperty(cmd, "Transaction", transaction);
			return cmd;
		}

		private void AddParameters(object cmd, object[] parameters)
		{
			object collection = GetProperty(cmd, "Parameters");
			foreach (object item in parameters)
			{
				xSqlParam direct = item as xSqlParam;
				if (direct != null) Invoke(collection, "AddWithValue", direct.Name, direct.Value ?? DBNull.Value);
				else
				{
					object[] nested = item as object[];
					if (nested != null) AddParameters(cmd, nested);
				}
			}
		}

		private xSqlParam P(string name, object value) { return new xSqlParam(name, value); }
		private string Iso(DateTime value) { return value == DateTime.MinValue ? string.Empty : value.ToString("o", CultureInfo.InvariantCulture); }
		private string S(object r, int i) { object v = Invoke(r, "GetValue", i); return v == null || v == DBNull.Value ? string.Empty : Convert.ToString(v, CultureInfo.InvariantCulture); }
		private int I(object r, int i) { string v = S(r, i); int n; return int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0; }
		private long L(object r, int i) { string v = S(r, i); long n; return long.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0; }
		private double D(object r, int i) { string v = S(r, i); double n; return double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0; }
		private DateTime Dt(object r, int i) { DateTime dt; return DateTime.TryParse(S(r, i), null, DateTimeStyles.RoundtripKind, out dt) ? dt : DateTime.MinValue; }
		private T ParseEnum<T>(string value) where T : struct { T parsed; return Enum.TryParse<T>(value, true, out parsed) ? parsed : default(T); }

		private object Invoke(object target, string name, params object[] args)
		{
			Type type = target is Type ? (Type)target : target.GetType(); object instance = target is Type ? null : target;
			foreach (MethodInfo method in type.GetMethods())
			{
				if (method.Name != name) continue; ParameterInfo[] ps = method.GetParameters();
				if (ps.Length != args.Length) continue; bool match = true;
				for (int i = 0; i < ps.Length; i++) if (args[i] != null && !ps[i].ParameterType.IsAssignableFrom(args[i].GetType()) && ps[i].ParameterType != typeof(object)) { match = false; break; }
				if (match) return method.Invoke(instance, args);
			}
			throw new MissingMethodException(type.FullName, name);
		}

		private object GetProperty(object target, string name) { return ResolveProperty(target.GetType(), name).GetValue(target, null); }
		private void SetProperty(object target, string name, object value) { ResolveProperty(target.GetType(), name).SetValue(target, value, null); }
		private PropertyInfo ResolveProperty(Type type, string name)
		{
			PropertyInfo selected = null; foreach (PropertyInfo property in type.GetProperties()) if (property.Name == name && (selected == null || property.DeclaringType == type)) selected = property;
			if (selected == null) throw new MissingMemberException(type.FullName, name); return selected;
		}
		private void DisposeObject(object target) { IDisposable disposable = target as IDisposable; if (disposable != null) disposable.Dispose(); }
	}
}
