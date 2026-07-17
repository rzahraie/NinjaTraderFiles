#region Using declarations
using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaTradeJournal
{
	public enum xTradeJournalDiagnosticLevel
	{
		Off,
		Errors,
		Normal,
		Verbose
	}

	public enum xTradeJournalLegMatchingMethod
	{
		FIFO,
		LIFO,
		AverageCost
	}

	public enum xTradeJournalReasonTag
	{
		None,
		PriceDeltaAgreement,
		BullishDivergence,
		BearishDivergence,
		DeltaFailure,
		Absorption,
		ContainerBoundary,
		Breakout,
		Reversal,
		Other
	}

	public enum xTradeJournalCampaignStatus
	{
		Open,
		Completed,
		Incomplete,
		Reconstructed,
		Error
	}

	public enum xTradeJournalSourceType
	{
		Unknown,
		ChartTrader,
		SuperDOM,
		ATM,
		NinjaScriptStrategy,
		ManualOrderTicket,
		AddOn,
		Playback,
		Other
	}

	internal enum xTradeJournalEventKind
	{
		Order,
		Execution,
		Position,
		AccountItem,
		AccountStatus,
		System
	}

	public sealed class xTradeJournalStatus
	{
		public string AccountName { get; set; }
		public bool DatabaseConnected { get; set; }
		public bool AccountSubscribed { get; set; }
		public string DatabasePath { get; set; }
		public string OpenCampaignText { get; set; }
		public int TodayCompletedCampaigns { get; set; }
		public double TodayGrossPnL { get; set; }
		public double TodayNetPnL { get; set; }
		public DateTime LastWriteLocal { get; set; }
		public bool ContextAvailable { get; set; }
		public int ErrorCount { get; set; }
		public int EmergencyQueueCount { get; set; }
		public string LastError { get; set; }
	}

	internal sealed class xTradeJournalEvent
	{
		public long SequenceNumber { get; set; }
		public xTradeJournalEventKind Kind { get; set; }
		public DateTime TimestampLocal { get; set; }
		public string Account { get; set; }
		public string Instrument { get; set; }
		public string MasterInstrument { get; set; }
		public DateTime Expiry { get; set; }
		public string ConnectionName { get; set; }
		public string OrderId { get; set; }
		public string ExecutionId { get; set; }
		public string Name { get; set; }
		public string OcoId { get; set; }
		public string Action { get; set; }
		public string OrderType { get; set; }
		public string TimeInForce { get; set; }
		public int Quantity { get; set; }
		public int FilledQuantity { get; set; }
		public double Price { get; set; }
		public double LimitPrice { get; set; }
		public double StopPrice { get; set; }
		public double AverageFillPrice { get; set; }
		public string OrderState { get; set; }
		public string MarketPositionReported { get; set; }
		public string ErrorCode { get; set; }
		public string NativeError { get; set; }
		public double Commission { get; set; }
		public double ExchangeFee { get; set; }
		public double OtherFee { get; set; }
		public double AccountItemValue { get; set; }
		public string AccountItem { get; set; }
		public string Currency { get; set; }
		public string Message { get; set; }
		public double PointValue { get; set; }
		public bool IsSimulated { get; set; }
		public xTradeJournalSourceType SourceType { get; set; }
		public string RawDescription { get; set; }
	}

	internal sealed class xTradeJournalCampaignState
	{
		public string CampaignId { get; set; }
		public string Account { get; set; }
		public string Instrument { get; set; }
		public int DirectionSign { get; set; }
		public DateTime StartTimeUtc { get; set; }
		public DateTime EndTimeUtc { get; set; }
		public double InitialEntryPrice { get; set; }
		public double EntryNotional { get; set; }
		public double ExitNotional { get; set; }
		public int TotalEntryQuantity { get; set; }
		public int TotalExitQuantity { get; set; }
		public int OpenQuantity { get; set; }
		public int MaximumQuantity { get; set; }
		public double GrossPnL { get; set; }
		public double Commission { get; set; }
		public double Fees { get; set; }
		public xTradeJournalCampaignStatus Status { get; set; }
		public string EntrySnapshotId { get; set; }
		public string ExitSnapshotId { get; set; }
		public xTradeJournalReasonTag PrimaryReasonTag { get; set; }
		public double PointValue { get; set; }
		public readonly List<xTradeJournalOpenLot> OpenLots = new List<xTradeJournalOpenLot>();
	}

	internal sealed class xTradeJournalOpenLot
	{
		public int Quantity { get; set; }
		public double Price { get; set; }
	}

	public sealed class xTradeJournalContextSnapshot
	{
		public string SnapshotId { get; set; }
		public string Account { get; set; }
		public string Instrument { get; set; }
		public DateTime TimestampUtc { get; set; }
		public DateTime TimestampLocal { get; set; }
		public string BarsPeriod { get; set; }
		public int BarsPeriodMinutes { get; set; }
		public string TradingHoursName { get; set; }
		public int CurrentBarIndex { get; set; }
		public DateTime CurrentBarTime { get; set; }
		public bool ContextAvailable { get; set; }
		public string ErrorMessage { get; set; }
		public string Json { get; set; }
	}

	public interface xITradeJournalContextProvider
	{
		string AccountName { get; }
		string InstrumentFullName { get; }
		int BarsPeriodMinutes { get; }
		DateTime LastUpdatedUtc { get; }
		xTradeJournalContextSnapshot CaptureSnapshot(string accountName, string instrumentFullName, int contextBars);
	}
}
