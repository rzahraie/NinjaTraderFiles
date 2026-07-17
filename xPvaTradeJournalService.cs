#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using NinjaTrader.Cbi;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaTradeJournal
{
	public sealed class xPvaTradeJournalService : IDisposable
	{
		private static readonly object staticSync = new object();
		private static xPvaTradeJournalService instance;

		private readonly object sync = new object();
		private readonly BlockingCollection<xTradeJournalEvent> queue = new BlockingCollection<xTradeJournalEvent>();
		private readonly Dictionary<string, xTradeJournalCampaignState> openCampaigns = new Dictionary<string, xTradeJournalCampaignState>();
		private readonly Dictionary<string, int> dailyCampaignCounters = new Dictionary<string, int>();
		private readonly List<xITradeJournalContextProvider> contextProviders = new List<xITradeJournalContextProvider>();
		private readonly xPvaTradeJournalDatabase database = new xPvaTradeJournalDatabase();
		private readonly Thread worker;
		private readonly string journalRoot;
		private readonly string emergencyRoot;
		private readonly string dailyRoot;
		private Account account;
		private long sequence;
		private int refCount;
		private bool disposed;
		private bool configured;
		private string sessionId;
		private string accountName = "Sim101";
		private bool captureAllAccounts;
		private int contextBars = 30;
		private int preferredBarsPeriodMinutes = 5;
		private int postTradeTagWindowSeconds = 300;
		private int maxEmergencyQueueRecords = 100000;
		private xTradeJournalDiagnosticLevel diagnosticLevel = xTradeJournalDiagnosticLevel.Normal;
		private xTradeJournalLegMatchingMethod legMatchingMethod = xTradeJournalLegMatchingMethod.FIFO;
		private xTradeJournalStatus status = new xTradeJournalStatus();
		private DateTime dailySummaryDate = DateTime.MinValue;

		private xPvaTradeJournalService()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			journalRoot = Path.Combine(documents, "NinjaTrader 8", "apva", "journal");
			emergencyRoot = Path.Combine(journalRoot, "emergency");
			dailyRoot = Path.Combine(journalRoot, "daily");
			Directory.CreateDirectory(journalRoot);
			Directory.CreateDirectory(emergencyRoot);
			Directory.CreateDirectory(dailyRoot);
			worker = new Thread(ProcessQueue);
			worker.IsBackground = true;
			worker.Name = "xPvaTradeJournalService";
			worker.Start();
		}

		public static xPvaTradeJournalService Acquire()
		{
			lock (staticSync)
			{
				if (instance == null)
					instance = new xPvaTradeJournalService();
				instance.refCount++;
				return instance;
			}
		}

		public void Release()
		{
			lock (staticSync)
			{
				if (refCount > 0)
					refCount--;
			}
		}

		public void Configure(string accountName, bool captureAllAccounts, int contextBars, int preferredBarsPeriodMinutes, int postTradeTagWindowSeconds, int maxEmergencyQueueRecords, xTradeJournalDiagnosticLevel diagnosticLevel, xTradeJournalLegMatchingMethod legMatchingMethod)
		{
			lock (sync)
			{
				this.accountName = string.IsNullOrWhiteSpace(accountName) ? "Sim101" : accountName;
				this.captureAllAccounts = captureAllAccounts;
				this.contextBars = Math.Max(10, Math.Min(200, contextBars));
				this.preferredBarsPeriodMinutes = preferredBarsPeriodMinutes <= 0 ? 5 : preferredBarsPeriodMinutes;
				this.postTradeTagWindowSeconds = postTradeTagWindowSeconds;
				this.maxEmergencyQueueRecords = Math.Max(1, maxEmergencyQueueRecords);
				this.diagnosticLevel = diagnosticLevel;
				this.legMatchingMethod = legMatchingMethod;
				status.AccountName = this.accountName;
				if (!configured)
				{
					string path = Path.Combine(journalRoot, "xPvaTradeJournal.sqlite");
					try
					{
						database.Open(path);
						status.DatabaseConnected = true;
						status.DatabasePath = path;
						status.LastError = string.Empty;
						sessionId = BuildSessionId(this.accountName);
						database.InsertJournalSession(sessionId, this.accountName, diagnosticLevel.ToString());
						Log("Database opened: " + path, xTradeJournalDiagnosticLevel.Normal);
						configured = true;
						RefreshDailySummary(DateTime.Now.Date);
					}
					catch (Exception ex)
					{
						status.DatabaseConnected = false;
						SetError("Database open failed: " + ex.Message);
					}
				}
				EnsureAccountSubscription();
			}
		}

		public void RegisterContextProvider(xITradeJournalContextProvider provider)
		{
			if (provider == null)
				return;
			lock (sync)
			{
				if (!contextProviders.Contains(provider))
					contextProviders.Add(provider);
			}
		}

		public void UnregisterContextProvider(xITradeJournalContextProvider provider)
		{
			lock (sync)
			{
				contextProviders.Remove(provider);
			}
		}

		public xTradeJournalStatus GetStatus()
		{
			lock (sync)
			{
				xTradeJournalStatus copy = new xTradeJournalStatus();
				copy.AccountName = status.AccountName;
				copy.DatabaseConnected = status.DatabaseConnected;
				copy.AccountSubscribed = status.AccountSubscribed;
				copy.DatabasePath = status.DatabasePath;
				copy.OpenCampaignText = BuildOpenCampaignText();
				copy.TodayCompletedCampaigns = status.TodayCompletedCampaigns;
				copy.TodayGrossPnL = status.TodayGrossPnL;
				copy.TodayNetPnL = status.TodayNetPnL;
				copy.LastWriteLocal = status.LastWriteLocal;
				copy.ContextAvailable = contextProviders.Count > 0;
				copy.ErrorCount = status.ErrorCount;
				copy.EmergencyQueueCount = queue.Count;
				copy.LastError = status.LastError;
				return copy;
			}
		}

		public void TagCurrentCampaign(string instrumentFullName, xTradeJournalReasonTag tag)
		{
			lock (sync)
			{
				foreach (xTradeJournalCampaignState c in openCampaigns.Values)
				{
					if (!string.IsNullOrEmpty(instrumentFullName) && c.Instrument != instrumentFullName)
						continue;
					c.PrimaryReasonTag = tag;
					try
					{
						database.UpsertCampaign(c);
						database.InsertSystemEvent(new xTradeJournalEvent { SequenceNumber = NextSequence(), TimestampLocal = DateTime.Now, Account = c.Account, Action = "ReasonTag", Message = c.CampaignId + " tagged " + tag, RawDescription = string.Empty });
						status.LastWriteLocal = DateTime.Now;
					}
					catch (Exception ex)
					{
						SetError("Reason tag failed: " + ex.Message);
					}
					return;
				}
			}
		}

		private void EnsureAccountSubscription()
		{
			if (captureAllAccounts)
			{
				Log("CaptureAllAccounts is not enabled in this first implementation; using selected account " + accountName, xTradeJournalDiagnosticLevel.Errors);
			}

			Account selected = null;
			foreach (Account a in Account.All)
			{
				if (string.Equals(a.Name, accountName, StringComparison.OrdinalIgnoreCase))
				{
					selected = a;
					break;
				}
			}

			if (selected == null)
			{
				status.AccountSubscribed = false;
				status.LastError = "Account unavailable: " + accountName;
				return;
			}

			if (account == selected)
				return;

			Unsubscribe();
			account = selected;
			account.OrderUpdate += OnOrderUpdate;
			account.ExecutionUpdate += OnExecutionUpdate;
			account.PositionUpdate += OnPositionUpdate;
			account.AccountItemUpdate += OnAccountItemUpdate;
			Account.AccountStatusUpdate += OnAccountStatusUpdate;
			status.AccountSubscribed = true;
			Log("Subscribed to account " + account.Name, xTradeJournalDiagnosticLevel.Normal);
			ReconstructOpenPositions();
		}

		private void Unsubscribe()
		{
			if (account == null)
				return;
			account.OrderUpdate -= OnOrderUpdate;
			account.ExecutionUpdate -= OnExecutionUpdate;
			account.PositionUpdate -= OnPositionUpdate;
			account.AccountItemUpdate -= OnAccountItemUpdate;
			Account.AccountStatusUpdate -= OnAccountStatusUpdate;
			account = null;
			status.AccountSubscribed = false;
		}

		private void OnOrderUpdate(object sender, OrderEventArgs e)
		{
			if (e == null || e.Order == null)
				return;
			Order o = e.Order;
			xTradeJournalEvent evt = BaseEvent(xTradeJournalEventKind.Order, o.Account, o.Instrument, e.Time);
			evt.OrderId = e.OrderId;
			evt.Name = o.Name;
			evt.OcoId = o.Oco;
			evt.Action = o.OrderAction.ToString();
			evt.OrderType = o.OrderType.ToString();
			evt.TimeInForce = o.TimeInForce.ToString();
			evt.Quantity = e.Quantity;
			evt.FilledQuantity = e.Filled;
			evt.LimitPrice = e.LimitPrice;
			evt.StopPrice = e.StopPrice;
			evt.AverageFillPrice = e.AverageFillPrice;
			evt.OrderState = e.OrderState.ToString();
			evt.ErrorCode = e.Error.ToString();
			evt.NativeError = e.Comment;
			evt.SourceType = InferSource(o);
			evt.RawDescription = o.ToString();
			Enqueue(evt);
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			if (e == null || e.Execution == null)
				return;
			Execution x = e.Execution;
			xTradeJournalEvent evt = BaseEvent(xTradeJournalEventKind.Execution, x.Account, x.Instrument, e.Time);
			evt.OrderId = e.OrderId;
			evt.ExecutionId = e.ExecutionId;
			evt.Action = x.Order != null ? x.Order.OrderAction.ToString() : e.MarketPosition.ToString();
			evt.Quantity = e.Quantity;
			evt.Price = e.Price;
			evt.MarketPositionReported = e.MarketPosition.ToString();
			evt.Commission = x.Commission;
			evt.ExchangeFee = x.Fee;
			evt.ConnectionName = x.ServerName;
			evt.IsSimulated = evt.Account != null && evt.Account.StartsWith("Sim", StringComparison.OrdinalIgnoreCase);
			evt.SourceType = x.Order != null ? InferSource(x.Order) : xTradeJournalSourceType.Unknown;
			evt.RawDescription = x.ToString();
			Enqueue(evt);
		}

		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			if (e == null || e.Position == null)
				return;
			xTradeJournalEvent evt = BaseEvent(xTradeJournalEventKind.Position, e.Position.Account, e.Position.Instrument, DateTime.Now);
			evt.AverageFillPrice = e.AveragePrice;
			evt.Quantity = e.Quantity;
			evt.MarketPositionReported = e.MarketPosition.ToString();
			evt.Action = e.Operation.ToString();
			Enqueue(evt);
		}

		private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
		{
			if (e == null || e.Account == null)
				return;
			xTradeJournalEvent evt = BaseEvent(xTradeJournalEventKind.AccountItem, e.Account, null, e.Time);
			evt.AccountItem = e.AccountItem.ToString();
			evt.Currency = e.Currency.ToString();
			evt.AccountItemValue = e.Value;
			Enqueue(evt);
		}

		private void OnAccountStatusUpdate(object sender, AccountStatusEventArgs e)
		{
			if (e == null || e.Account == null)
				return;
			xTradeJournalEvent evt = BaseEvent(xTradeJournalEventKind.AccountStatus, e.Account, null, DateTime.Now);
			evt.Action = "AccountStatus";
			evt.Message = e.PreviousStatus + " -> " + e.Status + " " + e.Message;
			Enqueue(evt);
		}

		private xTradeJournalEvent BaseEvent(xTradeJournalEventKind kind, Account account, Instrument instrument, DateTime localTime)
		{
			xTradeJournalEvent evt = new xTradeJournalEvent();
			evt.SequenceNumber = NextSequence();
			evt.Kind = kind;
			evt.TimestampLocal = localTime == DateTime.MinValue ? DateTime.Now : localTime;
			evt.Account = account != null ? account.Name : accountName;
			evt.Instrument = instrument != null ? instrument.FullName : string.Empty;
			evt.MasterInstrument = instrument != null && instrument.MasterInstrument != null ? instrument.MasterInstrument.Name : string.Empty;
			evt.Expiry = instrument != null ? instrument.Expiry : DateTime.MinValue;
			evt.PointValue = instrument != null && instrument.MasterInstrument != null ? instrument.MasterInstrument.PointValue : 1;
			evt.ConnectionName = account != null && account.Connection != null ? account.Connection.ToString() : string.Empty;
			return evt;
		}

		private long NextSequence()
		{
			return Interlocked.Increment(ref sequence);
		}

		private void Enqueue(xTradeJournalEvent evt)
		{
			if (disposed)
				return;
			queue.Add(evt);
		}

		private void ProcessQueue()
		{
			foreach (xTradeJournalEvent evt in queue.GetConsumingEnumerable())
			{
				try
				{
					ProcessEvent(evt);
					status.LastWriteLocal = DateTime.Now;
				}
				catch (Exception ex)
				{
					SetError("Journal write failed: " + ex.Message);
					WriteEmergency(evt, ex);
				}
			}
		}

		private void ProcessEvent(xTradeJournalEvent evt)
		{
			if (!database.IsConnected)
			{
				WriteEmergency(evt, null);
				return;
			}

			DateTime eventDate = evt.TimestampLocal == DateTime.MinValue ? DateTime.Now.Date : evt.TimestampLocal.Date;
			if (dailySummaryDate != eventDate)
				RefreshDailySummary(eventDate);

			if (evt.Kind == xTradeJournalEventKind.Order)
				database.InsertOrderEvent(evt);
			else if (evt.Kind == xTradeJournalEventKind.Execution)
			{
				bool inserted = database.InsertExecution(evt);
				if (!inserted)
				{
					Log("Duplicate execution suppressed: " + evt.ExecutionId, xTradeJournalDiagnosticLevel.Verbose);
					return;
				}
				ProcessExecutionCampaign(evt);
			}
			else if (evt.Kind == xTradeJournalEventKind.Position)
				database.InsertPositionEvent(evt);
			else if (evt.Kind == xTradeJournalEventKind.AccountItem)
				database.InsertAccountItemEvent(evt);
			else
				database.InsertSystemEvent(evt);
		}

		private void ProcessExecutionCampaign(xTradeJournalEvent evt)
		{
			int signed = SignedQuantity(evt);
			if (signed == 0 || evt.Quantity <= 0)
				return;

			string key = CampaignKey(evt.Account, evt.Instrument);
			xTradeJournalCampaignState c;
			if (!openCampaigns.TryGetValue(key, out c))
			{
				OpenCampaign(evt, Math.Sign(signed), Math.Abs(signed), out c);
				string snap = CaptureAndStoreSnapshot(evt);
				c.EntrySnapshotId = snap;
				AddEntry(c, evt, Math.Abs(signed));
				database.UpsertCampaign(c);
				database.LinkExecution(evt.Account, evt.Instrument, evt.ExecutionId, c.CampaignId, snap);
				return;
			}

			if (Math.Sign(signed) == c.DirectionSign)
			{
				AddEntry(c, evt, Math.Abs(signed));
				database.UpsertCampaign(c);
				database.LinkExecution(evt.Account, evt.Instrument, evt.ExecutionId, c.CampaignId, string.Empty);
				return;
			}

			int remaining = Math.Abs(signed);
			int closeQty = Math.Min(c.OpenQuantity, remaining);
			double closePnL = AddExit(c, evt, closeQty);
			remaining -= closeQty;
			string exitSnapshot = CaptureAndStoreSnapshot(evt);
			database.InsertCampaignLeg(c.CampaignId, evt, "Exit", closeQty, closePnL);

			if (c.OpenQuantity == 0)
			{
				c.Status = xTradeJournalCampaignStatus.Completed;
				c.EndTimeUtc = evt.TimestampLocal.ToUniversalTime();
				c.ExitSnapshotId = exitSnapshot;
				database.UpsertCampaign(c);
				openCampaigns.Remove(key);
				RefreshDailySummary(evt.TimestampLocal.Date);
			}
			else
				database.UpsertCampaign(c);

			database.LinkExecution(evt.Account, evt.Instrument, evt.ExecutionId, c.CampaignId, exitSnapshot);

			if (remaining > 0)
			{
				xTradeJournalCampaignState next;
				OpenCampaign(evt, Math.Sign(signed), remaining, out next);
				next.EntrySnapshotId = exitSnapshot;
				AddEntry(next, evt, remaining);
				database.UpsertCampaign(next);
				database.InsertSystemEvent(new xTradeJournalEvent { SequenceNumber = NextSequence(), TimestampLocal = evt.TimestampLocal, Account = evt.Account, Action = "ReversalSplit", Message = "Reversal split from " + c.CampaignId + " to " + next.CampaignId, RawDescription = string.Empty });
			}
		}

		private void OpenCampaign(xTradeJournalEvent evt, int directionSign, int quantity, out xTradeJournalCampaignState campaign)
		{
			campaign = new xTradeJournalCampaignState();
			campaign.CampaignId = NextCampaignId(evt.Instrument, evt.TimestampLocal);
			campaign.Account = evt.Account;
			campaign.Instrument = evt.Instrument;
			campaign.DirectionSign = directionSign;
			campaign.StartTimeUtc = evt.TimestampLocal.ToUniversalTime();
			campaign.InitialEntryPrice = evt.Price;
			campaign.PointValue = evt.PointValue <= 0 ? 1 : evt.PointValue;
			campaign.Status = xTradeJournalCampaignStatus.Open;
			campaign.PrimaryReasonTag = xTradeJournalReasonTag.None;
			openCampaigns[CampaignKey(evt.Account, evt.Instrument)] = campaign;
			Log("Opened campaign " + campaign.CampaignId, xTradeJournalDiagnosticLevel.Normal);
		}

		private void AddEntry(xTradeJournalCampaignState c, xTradeJournalEvent evt, int quantity)
		{
			c.EntryNotional += evt.Price * quantity;
			c.TotalEntryQuantity += quantity;
			c.OpenQuantity += quantity;
			if (c.OpenQuantity > c.MaximumQuantity)
				c.MaximumQuantity = c.OpenQuantity;
			c.Commission += evt.Commission;
			c.Fees += evt.ExchangeFee + evt.OtherFee;
			c.OpenLots.Add(new xTradeJournalOpenLot { Quantity = quantity, Price = evt.Price });
			database.InsertCampaignLeg(c.CampaignId, evt, "Entry", quantity, 0);
		}

		private double AddExit(xTradeJournalCampaignState c, xTradeJournalEvent evt, int quantity)
		{
			c.ExitNotional += evt.Price * quantity;
			c.TotalExitQuantity += quantity;
			double gross = MatchedExitPnL(c, evt.Price, quantity);
			c.GrossPnL += gross;
			c.OpenQuantity -= quantity;
			c.Commission += evt.Commission;
			c.Fees += evt.ExchangeFee + evt.OtherFee;
			return gross;
		}

		private double MatchedExitPnL(xTradeJournalCampaignState c, double exitPrice, int quantity)
		{
			if (legMatchingMethod == xTradeJournalLegMatchingMethod.AverageCost || c.OpenLots.Count == 0)
			{
				double avgEntry = c.OpenQuantity == 0 ? c.InitialEntryPrice : OpenLotNotional(c) / c.OpenQuantity;
				double diff = c.DirectionSign > 0 ? exitPrice - avgEntry : avgEntry - exitPrice;
				ConsumeAverageLots(c, quantity);
				return diff * quantity * (c.PointValue <= 0 ? 1 : c.PointValue);
			}

			int remaining = quantity;
			double gross = 0;
			while (remaining > 0 && c.OpenLots.Count > 0)
			{
				int index = legMatchingMethod == xTradeJournalLegMatchingMethod.LIFO ? c.OpenLots.Count - 1 : 0;
				xTradeJournalOpenLot lot = c.OpenLots[index];
				int matched = Math.Min(remaining, lot.Quantity);
				double diff = c.DirectionSign > 0 ? exitPrice - lot.Price : lot.Price - exitPrice;
				gross += diff * matched * (c.PointValue <= 0 ? 1 : c.PointValue);
				lot.Quantity -= matched;
				remaining -= matched;
				if (lot.Quantity <= 0)
					c.OpenLots.RemoveAt(index);
			}
			return gross;
		}

		private double OpenLotNotional(xTradeJournalCampaignState c)
		{
			double total = 0;
			foreach (xTradeJournalOpenLot lot in c.OpenLots)
				total += lot.Price * lot.Quantity;
			return total;
		}

		private void ConsumeAverageLots(xTradeJournalCampaignState c, int quantity)
		{
			int remaining = quantity;
			while (remaining > 0 && c.OpenLots.Count > 0)
			{
				xTradeJournalOpenLot lot = c.OpenLots[0];
				int matched = Math.Min(remaining, lot.Quantity);
				lot.Quantity -= matched;
				remaining -= matched;
				if (lot.Quantity <= 0)
					c.OpenLots.RemoveAt(0);
			}
		}

		private int SignedQuantity(xTradeJournalEvent evt)
		{
			if (evt.Action == "Buy" || evt.Action == "BuyToCover")
				return evt.Quantity;
			if (evt.Action == "Sell" || evt.Action == "SellShort")
				return -evt.Quantity;
			if (evt.MarketPositionReported == "Long")
				return evt.Quantity;
			if (evt.MarketPositionReported == "Short")
				return -evt.Quantity;
			return 0;
		}

		private string CaptureAndStoreSnapshot(xTradeJournalEvent evt)
		{
			xTradeJournalContextSnapshot snap = CaptureSnapshot(evt.Account, evt.Instrument);
			try
			{
				database.InsertContextSnapshot(snap);
				return snap != null ? snap.SnapshotId : string.Empty;
			}
			catch (Exception ex)
			{
				SetError("Context snapshot write failed: " + ex.Message);
				return string.Empty;
			}
		}

		private xTradeJournalContextSnapshot CaptureSnapshot(string accountName, string instrumentFullName)
		{
			xITradeJournalContextProvider best = null;
			lock (sync)
			{
				foreach (xITradeJournalContextProvider p in contextProviders)
				{
					if (!string.Equals(p.InstrumentFullName, instrumentFullName, StringComparison.OrdinalIgnoreCase))
						continue;
					if (best == null)
					{
						best = p;
						continue;
					}
					int bestPenalty = Math.Abs(best.BarsPeriodMinutes - preferredBarsPeriodMinutes);
					int pPenalty = Math.Abs(p.BarsPeriodMinutes - preferredBarsPeriodMinutes);
					if (pPenalty < bestPenalty || (pPenalty == bestPenalty && p.LastUpdatedUtc > best.LastUpdatedUtc))
						best = p;
				}
			}

			if (best != null)
				return best.CaptureSnapshot(accountName, instrumentFullName, contextBars);

			xTradeJournalContextSnapshot unavailable = new xTradeJournalContextSnapshot();
			unavailable.SnapshotId = Guid.NewGuid().ToString("N");
			unavailable.Account = accountName;
			unavailable.Instrument = instrumentFullName;
			unavailable.TimestampUtc = DateTime.UtcNow;
			unavailable.TimestampLocal = DateTime.Now;
			unavailable.ContextAvailable = false;
			unavailable.ErrorMessage = "No chart context provider available";
			unavailable.Json = "{\"contextSchemaVersion\":1,\"available\":false,\"error\":\"No chart context provider available\"}";
			return unavailable;
		}

		private void ReconstructOpenPositions()
		{
			try
			{
				if (account == null)
					return;
				foreach (Position p in account.Positions)
				{
					if (p == null || p.Quantity == 0 || p.MarketPosition == MarketPosition.Flat)
						continue;
					string key = CampaignKey(account.Name, p.Instrument.FullName);
					if (openCampaigns.ContainsKey(key))
						continue;
					xTradeJournalCampaignState c = new xTradeJournalCampaignState();
					c.CampaignId = NextCampaignId(p.Instrument.FullName, DateTime.Now);
					c.Account = account.Name;
					c.Instrument = p.Instrument.FullName;
					c.DirectionSign = p.MarketPosition == MarketPosition.Long ? 1 : -1;
					c.StartTimeUtc = DateTime.UtcNow;
					c.InitialEntryPrice = p.AveragePrice;
					c.EntryNotional = p.AveragePrice * p.Quantity;
					c.TotalEntryQuantity = p.Quantity;
					c.OpenQuantity = p.Quantity;
					c.MaximumQuantity = p.Quantity;
					c.OpenLots.Add(new xTradeJournalOpenLot { Quantity = p.Quantity, Price = p.AveragePrice });
					c.PointValue = p.Instrument.MasterInstrument != null ? p.Instrument.MasterInstrument.PointValue : 1;
					c.Status = xTradeJournalCampaignStatus.Reconstructed;
					openCampaigns[key] = c;
					database.UpsertCampaign(c);
					database.InsertSystemEvent(new xTradeJournalEvent { SequenceNumber = NextSequence(), TimestampLocal = DateTime.Now, Account = account.Name, Action = "StartupReconstruction", Message = "Reconstructed open position " + p.Instrument.FullName + " " + p.MarketPosition + " " + p.Quantity, RawDescription = string.Empty });
				}
			}
			catch (Exception ex)
			{
				SetError("Startup reconstruction failed: " + ex.Message);
			}
		}

		private void RefreshDailySummary(DateTime localDate)
		{
			try
			{
				localDate = localDate.Date;
				List<xTradeJournalCampaignState> campaigns = database.GetCompletedCampaignsForDate(localDate);
				string path = Path.Combine(dailyRoot, "xPvaTradeJournal_" + localDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".csv");
				using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
				{
					writer.WriteLine("Date,CampaignId,Account,Instrument,Direction,StartTime,EndTime,DurationSeconds,MaximumQuantity,WeightedAverageEntry,WeightedAverageExit,GrossPnL,Commission,Fees,NetPnL,ReasonTag,EntryBarDelta,ExitBarDelta,EntryCumulativeDeltaClose,ExitCumulativeDeltaClose,EntryVolumeZScore,EntryDeltaZScore,PriceDeltaRelationship,SourceCompletenessPassed,Status");
					double gross = 0;
					double net = 0;
					foreach (xTradeJournalCampaignState c in campaigns)
					{
						double avgEntry = c.TotalEntryQuantity == 0 ? 0 : c.EntryNotional / c.TotalEntryQuantity;
						double avgExit = c.TotalExitQuantity == 0 ? 0 : c.ExitNotional / c.TotalExitQuantity;
						double campaignNet = c.GrossPnL - c.Commission - c.Fees;
						gross += c.GrossPnL;
						net += campaignNet;
						writer.WriteLine(string.Join(",", new string[]
						{
							localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), c.CampaignId, c.Account, c.Instrument, c.DirectionSign > 0 ? "Long" : "Short",
							c.StartTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
							c.EndTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
							((int)(c.EndTimeUtc - c.StartTimeUtc).TotalSeconds).ToString(CultureInfo.InvariantCulture),
							c.MaximumQuantity.ToString(CultureInfo.InvariantCulture),
							avgEntry.ToString(CultureInfo.InvariantCulture),
							avgExit.ToString(CultureInfo.InvariantCulture),
							c.GrossPnL.ToString(CultureInfo.InvariantCulture),
							c.Commission.ToString(CultureInfo.InvariantCulture),
							c.Fees.ToString(CultureInfo.InvariantCulture),
							campaignNet.ToString(CultureInfo.InvariantCulture),
							c.PrimaryReasonTag.ToString(), "", "", "", "", "", "", "", "true", c.Status.ToString()
						}));
					}
					status.TodayCompletedCampaigns = campaigns.Count;
					status.TodayGrossPnL = gross;
					status.TodayNetPnL = net;
					dailySummaryDate = localDate;
				}
			}
			catch (Exception ex)
			{
				SetError("Daily summary failed: " + ex.Message);
			}
		}

		private void WriteEmergency(xTradeJournalEvent evt, Exception ex)
		{
			try
			{
				if (queue.Count > maxEmergencyQueueRecords)
					return;
				Directory.CreateDirectory(emergencyRoot);
				string path = Path.Combine(emergencyRoot, "xPvaTradeJournal_EMERGENCY_" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".jsonl");
				string json = "{\"sequence\":" + evt.SequenceNumber.ToString(CultureInfo.InvariantCulture) + ",\"kind\":\"" + EscapeJson(evt.Kind.ToString()) + "\",\"account\":\"" + EscapeJson(evt.Account) + "\",\"instrument\":\"" + EscapeJson(evt.Instrument) + "\",\"executionId\":\"" + EscapeJson(evt.ExecutionId) + "\",\"error\":\"" + EscapeJson(ex != null ? ex.Message : "database unavailable") + "\"}";
				File.AppendAllText(path, json + Environment.NewLine, Encoding.UTF8);
			}
			catch
			{
			}
		}

		private string NextCampaignId(string instrument, DateTime localTime)
		{
			string master = instrument;
			if (!string.IsNullOrEmpty(master) && master.IndexOf(' ') > 0)
				master = master.Substring(0, master.IndexOf(' '));
			string date = localTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			string key = master + "-" + date;
			int next;
			if (!dailyCampaignCounters.TryGetValue(key, out next))
				next = 0;
			next++;
			dailyCampaignCounters[key] = next;
			return string.Format(CultureInfo.InvariantCulture, "{0}-{1}-CAMP-{2:000}", master, date, next);
		}

		private string BuildSessionId(string accountName)
		{
			return accountName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
		}

		private string CampaignKey(string account, string instrument)
		{
			return (account ?? string.Empty) + "|" + (instrument ?? string.Empty);
		}

		private string BuildOpenCampaignText()
		{
			foreach (xTradeJournalCampaignState c in openCampaigns.Values)
				return c.Instrument + " " + (c.DirectionSign > 0 ? "Long " : "Short ") + c.OpenQuantity.ToString(CultureInfo.InvariantCulture);
			return "Flat";
		}

		private xTradeJournalSourceType InferSource(Order order)
		{
			if (order == null)
				return xTradeJournalSourceType.Unknown;
			if (!string.IsNullOrEmpty(order.Name) && order.Name.IndexOf("ATM", StringComparison.OrdinalIgnoreCase) >= 0)
				return xTradeJournalSourceType.ATM;
			if (order.IsBacktestOrder)
				return xTradeJournalSourceType.NinjaScriptStrategy;
			return xTradeJournalSourceType.Unknown;
		}

		private void Log(string message, xTradeJournalDiagnosticLevel level)
		{
			if (diagnosticLevel == xTradeJournalDiagnosticLevel.Off)
				return;
			if (diagnosticLevel == xTradeJournalDiagnosticLevel.Errors && level != xTradeJournalDiagnosticLevel.Errors)
				return;
			// This service intentionally avoids direct Print() calls; status panel and SystemEvent carry diagnostics.
			try
			{
				if (database.IsConnected)
					database.InsertSystemEvent(new xTradeJournalEvent { SequenceNumber = NextSequence(), TimestampLocal = DateTime.Now, Account = accountName, Action = "Diagnostic", Message = message, RawDescription = string.Empty });
			}
			catch
			{
			}
		}

		private void SetError(string message)
		{
			lock (sync)
			{
				status.ErrorCount++;
				status.LastError = message;
			}
		}

		private string EscapeJson(string value)
		{
			if (value == null)
				return string.Empty;
			return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			Unsubscribe();
			queue.CompleteAdding();
			try { worker.Join(1000); } catch { }
			database.Dispose();
		}
	}
}
