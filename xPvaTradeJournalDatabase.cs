#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaTradeJournal
{
	internal sealed class xPvaTradeJournalDatabase : IDisposable
	{
		private sealed class xSqlParam
		{
			public string Name;
			public object Value;
			public xSqlParam(string name, object value)
			{
				Name = name;
				Value = value;
			}
		}

		private object connection;
		private Type connectionType;
		private readonly object sync = new object();

		public string DatabasePath { get; private set; }
		public bool IsConnected { get { return connection != null; } }

		public void Open(string databasePath)
		{
			lock (sync)
			{
				DatabasePath = databasePath;
				Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
				connectionType = ResolveSQLiteConnectionType();
				connection = CreateSQLiteConnection("Data Source=" + databasePath + ";Version=3;Journal Mode=WAL;");
				Invoke(connection, "Open");
				ExecuteNonQuery("PRAGMA journal_mode=WAL;");
				ExecuteNonQuery("PRAGMA synchronous=NORMAL;");
				CreateSchema();
			}
		}

		public void Dispose()
		{
			lock (sync)
			{
				if (connection != null)
				{
					Invoke(connection, "Close");
					IDisposable disposable = connection as IDisposable;
					if (disposable != null)
						disposable.Dispose();
					connection = null;
				}
			}
		}

		private Type ResolveSQLiteConnectionType()
		{
			Type t = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite", false);
			if (t != null)
				return t;

			foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
			{
				t = loaded.GetType("System.Data.SQLite.SQLiteConnection", false);
				if (t != null)
					return t;
			}

			string[] candidates =
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NinjaTrader 8", "bin", "System.Data.SQLite.dll"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.Data.SQLite.dll"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "System.Data.SQLite.dll")
			};

			string tried = string.Empty;
			foreach (string path in candidates)
			{
				if (string.IsNullOrEmpty(path))
					continue;
				tried += path + "; ";
				if (!File.Exists(path))
					continue;
				Assembly asm = Assembly.LoadFrom(path);
				t = asm.GetType("System.Data.SQLite.SQLiteConnection", false);
				if (t != null)
					return t;
			}

			throw new InvalidOperationException("System.Data.SQLite.SQLiteConnection could not be loaded. Tried: " + tried);
		}

		private object CreateSQLiteConnection(string connectionString)
		{
			ConstructorInfo constructor = connectionType.GetConstructor(new Type[] { typeof(string) });
			if (constructor == null)
				throw new MissingMethodException(connectionType.FullName, ".ctor(string)");
			return constructor.Invoke(new object[] { connectionString });
		}

		private void CreateSchema()
		{
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS SchemaVersion (Version INTEGER NOT NULL, AppliedUtc TEXT NOT NULL);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS JournalSession (SessionId TEXT PRIMARY KEY, StartedUtc TEXT NOT NULL, Account TEXT, DatabasePath TEXT, DiagnosticLevel TEXT);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS OrderEvent (OrderEventId TEXT PRIMARY KEY, TimestampUtc TEXT, TimestampLocal TEXT, Account TEXT, Instrument TEXT, OrderId TEXT, ExecutionId TEXT, Name TEXT, StrategyName TEXT, AtmStrategyId TEXT, OcoId TEXT, Action TEXT, OrderType TEXT, Quantity INTEGER, FilledQuantity INTEGER, LimitPrice REAL, StopPrice REAL, AverageFillPrice REAL, OrderState TEXT, TimeInForce TEXT, ErrorCode TEXT, NativeError TEXT, SourceType TEXT, RawDescription TEXT, SequenceNumber INTEGER);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Execution (ExecutionRecordId TEXT PRIMARY KEY, TimestampUtc TEXT, TimestampLocal TEXT, Account TEXT, Instrument TEXT, OrderId TEXT, ExecutionId TEXT, Action TEXT, Quantity INTEGER, Price REAL, MarketPositionReported TEXT, Commission REAL, ExchangeFee REAL, OtherFee REAL, IsSimulated INTEGER, ConnectionName TEXT, CampaignId TEXT, ContextSnapshotId TEXT, SequenceNumber INTEGER, Imported INTEGER DEFAULT 0, UNIQUE(Account, Instrument, ExecutionId));");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS PositionEvent (PositionEventId TEXT PRIMARY KEY, TimestampUtc TEXT, TimestampLocal TEXT, Account TEXT, Instrument TEXT, AveragePrice REAL, Quantity INTEGER, MarketPosition TEXT, Operation TEXT, SequenceNumber INTEGER);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS TradeCampaign (CampaignId TEXT PRIMARY KEY, Account TEXT, Instrument TEXT, Direction TEXT, StartTimeUtc TEXT, EndTimeUtc TEXT, InitialEntryPrice REAL, WeightedAverageEntryPrice REAL, WeightedAverageExitPrice REAL, MaximumQuantity INTEGER, TotalEntryQuantity INTEGER, TotalExitQuantity INTEGER, GrossPnL REAL, Commission REAL, Fees REAL, NetPnL REAL, Status TEXT, IsSimulated INTEGER, EntrySnapshotId TEXT, ExitSnapshotId TEXT, PrimaryReasonTag TEXT, Notes TEXT, CreatedUtc TEXT, UpdatedUtc TEXT);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CampaignLeg (LegId TEXT PRIMARY KEY, CampaignId TEXT, TimestampUtc TEXT, ExecutionId TEXT, Side TEXT, Quantity INTEGER, Price REAL, GrossPnL REAL, SequenceNumber INTEGER);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS ContextSnapshot (SnapshotId TEXT PRIMARY KEY, TimestampUtc TEXT, TimestampLocal TEXT, Account TEXT, Instrument TEXT, BarsPeriod TEXT, BarsPeriodMinutes INTEGER, TradingHoursName TEXT, CurrentBarIndex INTEGER, CurrentBarTime TEXT, ContextAvailable INTEGER, ErrorMessage TEXT, Json TEXT, ContextSchemaVersion INTEGER);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS ReasonTag (ReasonTagId TEXT PRIMARY KEY, CampaignId TEXT, TimestampUtc TEXT, Account TEXT, Instrument TEXT, ReasonTag TEXT);");
			ExecuteNonQuery("CREATE TABLE IF NOT EXISTS SystemEvent (SystemEventId TEXT PRIMARY KEY, TimestampUtc TEXT, TimestampLocal TEXT, Account TEXT, EventType TEXT, Message TEXT, RawJson TEXT, SequenceNumber INTEGER);");
			object existing = ExecuteScalar("SELECT COUNT(*) FROM SchemaVersion WHERE Version = 1;");
			if (Convert.ToInt32(existing, CultureInfo.InvariantCulture) == 0)
				ExecuteNonQuery("INSERT INTO SchemaVersion (Version, AppliedUtc) VALUES (1, @AppliedUtc);", P("@AppliedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
		}

		public void InsertJournalSession(string sessionId, string account, string diagnosticLevel)
		{
			ExecuteNonQuery("INSERT OR IGNORE INTO JournalSession (SessionId, StartedUtc, Account, DatabasePath, DiagnosticLevel) VALUES (@SessionId, @StartedUtc, @Account, @DatabasePath, @DiagnosticLevel);",
				P("@SessionId", sessionId), P("@StartedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)), P("@Account", account), P("@DatabasePath", DatabasePath), P("@DiagnosticLevel", diagnosticLevel));
		}

		public void InsertOrderEvent(xTradeJournalEvent e)
		{
			ExecuteNonQuery("INSERT OR REPLACE INTO OrderEvent (OrderEventId, TimestampUtc, TimestampLocal, Account, Instrument, OrderId, ExecutionId, Name, StrategyName, AtmStrategyId, OcoId, Action, OrderType, Quantity, FilledQuantity, LimitPrice, StopPrice, AverageFillPrice, OrderState, TimeInForce, ErrorCode, NativeError, SourceType, RawDescription, SequenceNumber) VALUES (@Id,@Utc,@Local,@Account,@Instrument,@OrderId,@ExecutionId,@Name,@Strategy,@Atm,@Oco,@Action,@OrderType,@Qty,@Filled,@Limit,@Stop,@Avg,@State,@Tif,@Error,@Native,@Source,@Raw,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), Common(e, "@Utc", "@Local", "@Account", "@Instrument", "@Seq"),
				P("@OrderId", e.OrderId), P("@ExecutionId", e.ExecutionId), P("@Name", e.Name), P("@Strategy", string.Empty), P("@Atm", string.Empty), P("@Oco", e.OcoId), P("@Action", e.Action), P("@OrderType", e.OrderType), P("@Qty", e.Quantity), P("@Filled", e.FilledQuantity), P("@Limit", e.LimitPrice), P("@Stop", e.StopPrice), P("@Avg", e.AverageFillPrice), P("@State", e.OrderState), P("@Tif", e.TimeInForce), P("@Error", e.ErrorCode), P("@Native", e.NativeError), P("@Source", e.SourceType.ToString()), P("@Raw", e.RawDescription));
		}

		public bool InsertExecution(xTradeJournalEvent e)
		{
			int changed = ExecuteNonQuery("INSERT OR IGNORE INTO Execution (ExecutionRecordId, TimestampUtc, TimestampLocal, Account, Instrument, OrderId, ExecutionId, Action, Quantity, Price, MarketPositionReported, Commission, ExchangeFee, OtherFee, IsSimulated, ConnectionName, CampaignId, ContextSnapshotId, SequenceNumber) VALUES (@Id,@Utc,@Local,@Account,@Instrument,@OrderId,@ExecutionId,@Action,@Qty,@Price,@MP,@Commission,@Fee,@OtherFee,@Sim,@Connection,@Campaign,@Snapshot,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), Common(e, "@Utc", "@Local", "@Account", "@Instrument", "@Seq"),
				P("@OrderId", e.OrderId), P("@ExecutionId", e.ExecutionId), P("@Action", e.Action), P("@Qty", e.Quantity), P("@Price", e.Price), P("@MP", e.MarketPositionReported), P("@Commission", e.Commission), P("@Fee", e.ExchangeFee), P("@OtherFee", e.OtherFee), P("@Sim", e.IsSimulated ? 1 : 0), P("@Connection", e.ConnectionName), P("@Campaign", string.Empty), P("@Snapshot", string.Empty));
			return changed > 0;
		}

		public void LinkExecution(string account, string instrument, string executionId, string campaignId, string snapshotId)
		{
			ExecuteNonQuery("UPDATE Execution SET CampaignId=@CampaignId, ContextSnapshotId=@SnapshotId WHERE Account=@Account AND Instrument=@Instrument AND ExecutionId=@ExecutionId;",
				P("@CampaignId", campaignId), P("@SnapshotId", snapshotId), P("@Account", account), P("@Instrument", instrument), P("@ExecutionId", executionId));
		}

		public void InsertPositionEvent(xTradeJournalEvent e)
		{
			ExecuteNonQuery("INSERT INTO PositionEvent (PositionEventId, TimestampUtc, TimestampLocal, Account, Instrument, AveragePrice, Quantity, MarketPosition, Operation, SequenceNumber) VALUES (@Id,@Utc,@Local,@Account,@Instrument,@Avg,@Qty,@MP,@Op,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), Common(e, "@Utc", "@Local", "@Account", "@Instrument", "@Seq"), P("@Avg", e.AverageFillPrice), P("@Qty", e.Quantity), P("@MP", e.MarketPositionReported), P("@Op", e.Action));
		}

		public void InsertAccountItemEvent(xTradeJournalEvent e)
		{
			ExecuteNonQuery("INSERT INTO SystemEvent (SystemEventId, TimestampUtc, TimestampLocal, Account, EventType, Message, RawJson, SequenceNumber) VALUES (@Id,@Utc,@Local,@Account,@Type,@Msg,@Raw,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), Common(e, "@Utc", "@Local", "@Account", "@Instrument", "@Seq"), P("@Type", "AccountItem:" + e.AccountItem), P("@Msg", e.AccountItem + "=" + e.AccountItemValue.ToString(CultureInfo.InvariantCulture) + " " + e.Currency), P("@Raw", string.Empty));
		}

		public void InsertSystemEvent(xTradeJournalEvent e)
		{
			ExecuteNonQuery("INSERT INTO SystemEvent (SystemEventId, TimestampUtc, TimestampLocal, Account, EventType, Message, RawJson, SequenceNumber) VALUES (@Id,@Utc,@Local,@Account,@Type,@Msg,@Raw,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), Common(e, "@Utc", "@Local", "@Account", "@Instrument", "@Seq"), P("@Type", e.Action), P("@Msg", e.Message), P("@Raw", e.RawDescription));
		}

		public void UpsertCampaign(xTradeJournalCampaignState c)
		{
			double avgEntry = c.TotalEntryQuantity == 0 ? 0 : c.EntryNotional / c.TotalEntryQuantity;
			double avgExit = c.TotalExitQuantity == 0 ? 0 : c.ExitNotional / c.TotalExitQuantity;
			double net = c.GrossPnL - c.Commission - c.Fees;
			ExecuteNonQuery("INSERT OR REPLACE INTO TradeCampaign (CampaignId, Account, Instrument, Direction, StartTimeUtc, EndTimeUtc, InitialEntryPrice, WeightedAverageEntryPrice, WeightedAverageExitPrice, MaximumQuantity, TotalEntryQuantity, TotalExitQuantity, GrossPnL, Commission, Fees, NetPnL, Status, IsSimulated, EntrySnapshotId, ExitSnapshotId, PrimaryReasonTag, Notes, CreatedUtc, UpdatedUtc) VALUES (@CampaignId,@Account,@Instrument,@Direction,@Start,@End,@Initial,@AvgEntry,@AvgExit,@MaxQty,@EntryQty,@ExitQty,@Gross,@Commission,@Fees,@Net,@Status,@Sim,@EntrySnap,@ExitSnap,@Reason,@Notes,@Created,@Updated);",
				P("@CampaignId", c.CampaignId), P("@Account", c.Account), P("@Instrument", c.Instrument), P("@Direction", c.DirectionSign > 0 ? "Long" : "Short"), P("@Start", c.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture)), P("@End", c.EndTimeUtc == DateTime.MinValue ? string.Empty : c.EndTimeUtc.ToString("o", CultureInfo.InvariantCulture)), P("@Initial", c.InitialEntryPrice), P("@AvgEntry", avgEntry), P("@AvgExit", avgExit), P("@MaxQty", c.MaximumQuantity), P("@EntryQty", c.TotalEntryQuantity), P("@ExitQty", c.TotalExitQuantity), P("@Gross", c.GrossPnL), P("@Commission", c.Commission), P("@Fees", c.Fees), P("@Net", net), P("@Status", c.Status.ToString()), P("@Sim", c.Account != null && c.Account.StartsWith("Sim", StringComparison.OrdinalIgnoreCase) ? 1 : 0), P("@EntrySnap", c.EntrySnapshotId), P("@ExitSnap", c.ExitSnapshotId), P("@Reason", c.PrimaryReasonTag.ToString()), P("@Notes", string.Empty), P("@Created", c.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture)), P("@Updated", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
		}

		public void InsertCampaignLeg(string campaignId, xTradeJournalEvent e, string side, int quantity, double grossPnL)
		{
			ExecuteNonQuery("INSERT INTO CampaignLeg (LegId, CampaignId, TimestampUtc, ExecutionId, Side, Quantity, Price, GrossPnL, SequenceNumber) VALUES (@Id,@Campaign,@Utc,@ExecutionId,@Side,@Qty,@Price,@Gross,@Seq);",
				P("@Id", Guid.NewGuid().ToString("N")), P("@Campaign", campaignId), P("@Utc", e.TimestampLocal.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)), P("@ExecutionId", e.ExecutionId), P("@Side", side), P("@Qty", quantity), P("@Price", e.Price), P("@Gross", grossPnL), P("@Seq", e.SequenceNumber));
		}

		public void InsertContextSnapshot(xTradeJournalContextSnapshot s)
		{
			if (s == null)
				return;
			ExecuteNonQuery("INSERT OR REPLACE INTO ContextSnapshot (SnapshotId, TimestampUtc, TimestampLocal, Account, Instrument, BarsPeriod, BarsPeriodMinutes, TradingHoursName, CurrentBarIndex, CurrentBarTime, ContextAvailable, ErrorMessage, Json, ContextSchemaVersion) VALUES (@Id,@Utc,@Local,@Account,@Instrument,@BarsPeriod,@Minutes,@TradingHours,@BarIndex,@BarTime,@Available,@Error,@Json,1);",
				P("@Id", s.SnapshotId), P("@Utc", s.TimestampUtc.ToString("o", CultureInfo.InvariantCulture)), P("@Local", s.TimestampLocal.ToString("o", CultureInfo.InvariantCulture)), P("@Account", s.Account), P("@Instrument", s.Instrument), P("@BarsPeriod", s.BarsPeriod), P("@Minutes", s.BarsPeriodMinutes), P("@TradingHours", s.TradingHoursName), P("@BarIndex", s.CurrentBarIndex), P("@BarTime", s.CurrentBarTime == DateTime.MinValue ? string.Empty : s.CurrentBarTime.ToString("o", CultureInfo.InvariantCulture)), P("@Available", s.ContextAvailable ? 1 : 0), P("@Error", s.ErrorMessage), P("@Json", s.Json));
		}

		public List<xTradeJournalCampaignState> GetCompletedCampaignsForDate(DateTime localDate)
		{
			List<xTradeJournalCampaignState> rows = new List<xTradeJournalCampaignState>();
			DateTime startUtc = localDate.Date.ToUniversalTime();
			DateTime endUtc = localDate.Date.AddDays(1).ToUniversalTime();
			object cmd = CreateCommand("SELECT CampaignId,Account,Instrument,Direction,StartTimeUtc,EndTimeUtc,InitialEntryPrice,WeightedAverageEntryPrice,WeightedAverageExitPrice,MaximumQuantity,TotalEntryQuantity,TotalExitQuantity,GrossPnL,Commission,Fees,NetPnL,Status,PrimaryReasonTag FROM TradeCampaign WHERE Status='Completed' AND StartTimeUtc >= @Start AND StartTimeUtc < @End ORDER BY StartTimeUtc;");
			try
			{
				AddParameters(cmd, new object[] { P("@Start", startUtc.ToString("o", CultureInfo.InvariantCulture)), P("@End", endUtc.ToString("o", CultureInfo.InvariantCulture)) });
				object reader = Invoke(cmd, "ExecuteReader");
				try
				{
					while (Convert.ToBoolean(Invoke(reader, "Read"), CultureInfo.InvariantCulture))
					{
						xTradeJournalCampaignState c = new xTradeJournalCampaignState();
						c.CampaignId = GetString(reader, 0);
						c.Account = GetString(reader, 1);
						c.Instrument = GetString(reader, 2);
						c.DirectionSign = GetString(reader, 3) == "Long" ? 1 : -1;
						c.StartTimeUtc = ParseUtc(GetString(reader, 4));
						c.EndTimeUtc = ParseUtc(GetString(reader, 5));
						c.InitialEntryPrice = GetDouble(reader, 6);
						c.EntryNotional = GetDouble(reader, 7) * GetInt32(reader, 10);
						c.ExitNotional = GetDouble(reader, 8) * GetInt32(reader, 11);
						c.MaximumQuantity = GetInt32(reader, 9);
						c.TotalEntryQuantity = GetInt32(reader, 10);
						c.TotalExitQuantity = GetInt32(reader, 11);
						c.GrossPnL = GetDouble(reader, 12);
						c.Commission = GetDouble(reader, 13);
						c.Fees = GetDouble(reader, 14);
						c.Status = xTradeJournalCampaignStatus.Completed;
						rows.Add(c);
					}
				}
				finally
				{
					DisposeObject(reader);
				}
			}
			finally
			{
				DisposeObject(cmd);
			}
			return rows;
		}

		private xSqlParam[] Common(xTradeJournalEvent e, string utcName, string localName, string accountName, string instrumentName, string seqName)
		{
			return new xSqlParam[]
			{
				P(utcName, e.TimestampLocal.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)),
				P(localName, e.TimestampLocal.ToString("o", CultureInfo.InvariantCulture)),
				P(accountName, e.Account),
				P(instrumentName, e.Instrument),
				P(seqName, e.SequenceNumber)
			};
		}

		private object CreateCommand(string sql)
		{
			object cmd = Invoke(connection, "CreateCommand");
			SetProperty(cmd, "CommandText", sql);
			return cmd;
		}

		private int ExecuteNonQuery(string sql, params object[] parameters)
		{
			lock (sync)
			{
				object cmd = CreateCommand(sql);
				try
				{
					AddParameters(cmd, parameters);
					return Convert.ToInt32(Invoke(cmd, "ExecuteNonQuery"), CultureInfo.InvariantCulture);
				}
				finally
				{
					DisposeObject(cmd);
				}
			}
		}

		private object ExecuteScalar(string sql, params object[] parameters)
		{
			lock (sync)
			{
				object cmd = CreateCommand(sql);
				try
				{
					AddParameters(cmd, parameters);
					return Invoke(cmd, "ExecuteScalar");
				}
				finally
				{
					DisposeObject(cmd);
				}
			}
		}

		private void AddParameters(object cmd, object[] parameters)
		{
			object parameterCollection = GetProperty(cmd, "Parameters");
			foreach (xSqlParam p in Flatten(parameters))
				Invoke(parameterCollection, "AddWithValue", p.Name, p.Value ?? DBNull.Value);
		}

		private IEnumerable<xSqlParam> Flatten(object[] parameters)
		{
			foreach (object item in parameters)
			{
				if (item == null)
					continue;
				xSqlParam direct = item as xSqlParam;
				if (direct != null)
				{
					yield return direct;
					continue;
				}
				xSqlParam[] array = item as xSqlParam[];
				if (array == null)
					continue;
				foreach (xSqlParam p in array)
					if (p != null)
						yield return p;
			}
		}

		private xSqlParam P(string name, object value)
		{
			return new xSqlParam(name, value);
		}

		private object Invoke(object target, string methodName, params object[] args)
		{
			Type type = target is Type ? (Type)target : target.GetType();
			object instance = target is Type ? null : target;
			MethodInfo method = null;
			foreach (MethodInfo candidate in type.GetMethods())
			{
				if (candidate.Name != methodName)
					continue;
				ParameterInfo[] ps = candidate.GetParameters();
				if (ps.Length == args.Length && ParametersCompatible(ps, args))
				{
					method = candidate;
					break;
				}
			}
			if (method == null)
				throw new MissingMethodException(type.FullName, methodName);
			return method.Invoke(instance, args);
		}

		private bool ParametersCompatible(ParameterInfo[] parameters, object[] args)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (args[i] == null)
					continue;
				Type targetType = parameters[i].ParameterType;
				Type valueType = args[i].GetType();
				if (targetType.IsAssignableFrom(valueType))
					continue;
				if (targetType == typeof(object))
					continue;
				return false;
			}
			return true;
		}

		private object GetProperty(object target, string propertyName)
		{
			PropertyInfo property = ResolveProperty(target.GetType(), propertyName);
			return property.GetValue(target, null);
		}

		private void SetProperty(object target, string propertyName, object value)
		{
			PropertyInfo property = ResolveProperty(target.GetType(), propertyName);
			property.SetValue(target, value, null);
		}

		private PropertyInfo ResolveProperty(Type type, string propertyName)
		{
			PropertyInfo selected = null;
			foreach (PropertyInfo property in type.GetProperties())
			{
				if (property.Name != propertyName)
					continue;
				if (selected == null || property.DeclaringType == type)
					selected = property;
			}
			if (selected == null)
				throw new MissingMemberException(type.FullName, propertyName);
			return selected;
		}

		private string GetString(object reader, int index)
		{
			return Convert.ToString(Invoke(reader, "GetString", index), CultureInfo.InvariantCulture);
		}

		private double GetDouble(object reader, int index)
		{
			return Convert.ToDouble(Invoke(reader, "GetDouble", index), CultureInfo.InvariantCulture);
		}

		private int GetInt32(object reader, int index)
		{
			return Convert.ToInt32(Invoke(reader, "GetInt32", index), CultureInfo.InvariantCulture);
		}

		private void DisposeObject(object target)
		{
			IDisposable disposable = target as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}

		private DateTime ParseUtc(string value)
		{
			DateTime dt;
			return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out dt) ? dt.ToUniversalTime() : DateTime.MinValue;
		}
	}
}
