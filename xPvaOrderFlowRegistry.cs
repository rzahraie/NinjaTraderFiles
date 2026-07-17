#region Using declarations
using System;
using System.Collections.Concurrent;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow
{
	public readonly struct xOrderFlowRegistryKey : IEquatable<xOrderFlowRegistryKey>
	{
		public string InstrumentFullName { get; }
		public string MasterInstrumentName { get; }
		public BarsPeriodType BarsPeriodType { get; }
		public int BarsPeriodValue { get; }
		public int BarsPeriodValue2 { get; }
		public string TradingHoursName { get; }
		public string ConnectionName { get; }
		public string UserInstanceKey { get; }

		public xOrderFlowRegistryKey(string instrumentFullName, string masterInstrumentName, BarsPeriodType barsPeriodType, int barsPeriodValue, int barsPeriodValue2, string tradingHoursName, string connectionName, string userInstanceKey)
		{
			InstrumentFullName = instrumentFullName ?? string.Empty;
			MasterInstrumentName = masterInstrumentName ?? string.Empty;
			BarsPeriodType = barsPeriodType;
			BarsPeriodValue = barsPeriodValue;
			BarsPeriodValue2 = barsPeriodValue2;
			TradingHoursName = tradingHoursName ?? string.Empty;
			ConnectionName = connectionName ?? string.Empty;
			UserInstanceKey = userInstanceKey ?? string.Empty;
		}

		public bool Equals(xOrderFlowRegistryKey other)
		{
			return string.Equals(InstrumentFullName, other.InstrumentFullName, StringComparison.Ordinal)
				&& string.Equals(MasterInstrumentName, other.MasterInstrumentName, StringComparison.Ordinal)
				&& BarsPeriodType == other.BarsPeriodType
				&& BarsPeriodValue == other.BarsPeriodValue
				&& BarsPeriodValue2 == other.BarsPeriodValue2
				&& string.Equals(TradingHoursName, other.TradingHoursName, StringComparison.Ordinal)
				&& string.Equals(ConnectionName, other.ConnectionName, StringComparison.Ordinal)
				&& string.Equals(UserInstanceKey, other.UserInstanceKey, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is xOrderFlowRegistryKey && Equals((xOrderFlowRegistryKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + HashString(InstrumentFullName);
				hash = hash * 31 + HashString(MasterInstrumentName);
				hash = hash * 31 + BarsPeriodType.GetHashCode();
				hash = hash * 31 + BarsPeriodValue.GetHashCode();
				hash = hash * 31 + BarsPeriodValue2.GetHashCode();
				hash = hash * 31 + HashString(TradingHoursName);
				hash = hash * 31 + HashString(ConnectionName);
				hash = hash * 31 + HashString(UserInstanceKey);
				return hash;
			}
		}

		private static int HashString(string value)
		{
			return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
		}

		public override string ToString()
		{
			return string.Format("{0}|{1}|{2}:{3}:{4}|TH={5}|Conn={6}|Key={7}", InstrumentFullName, MasterInstrumentName, BarsPeriodType, BarsPeriodValue, BarsPeriodValue2, TradingHoursName, ConnectionName, UserInstanceKey);
		}
	}

	public enum xOrderFlowPublisherRegistrationResult
	{
		Registered,
		Refreshed,
		DuplicatePublisher,
		Ambiguous
	}

	internal sealed class xOrderFlowPublishedSeries
	{
		public readonly ConcurrentDictionary<int, xOrderFlowBar> Bars = new ConcurrentDictionary<int, xOrderFlowBar>();
		public Guid PublisherId;
		public DateTime HeartbeatUtc;
		public int MostRecentPublishedBarIndex = -1;
		public bool Ambiguous;
		public xLiveOrderFlowSnapshot LiveSnapshot;
	}

	public static class xPvaOrderFlowRegistry
	{
		private static readonly ConcurrentDictionary<xOrderFlowRegistryKey, xOrderFlowPublishedSeries> seriesByKey = new ConcurrentDictionary<xOrderFlowRegistryKey, xOrderFlowPublishedSeries>();
		private static readonly object gate = new object();

		public static xOrderFlowPublisherRegistrationResult RegisterPublisher(xOrderFlowRegistryKey key, Guid publisherId)
		{
			lock (gate)
			{
				xOrderFlowPublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series))
				{
					series = new xOrderFlowPublishedSeries { PublisherId = publisherId, HeartbeatUtc = DateTime.UtcNow };
					seriesByKey[key] = series;
					return xOrderFlowPublisherRegistrationResult.Registered;
				}

				if (series.PublisherId == publisherId)
				{
					series.HeartbeatUtc = DateTime.UtcNow;
					return series.Ambiguous ? xOrderFlowPublisherRegistrationResult.Ambiguous : xOrderFlowPublisherRegistrationResult.Refreshed;
				}

				series.Ambiguous = true;
				series.HeartbeatUtc = DateTime.UtcNow;
				return xOrderFlowPublisherRegistrationResult.DuplicatePublisher;
			}
		}

		public static void Publish(xOrderFlowRegistryKey key, Guid publisherId, xOrderFlowBar bar)
		{
			if (bar == null)
				return;

			lock (gate)
			{
				xOrderFlowPublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.PublisherId != publisherId)
					return;

				series.Bars[bar.BarIndex] = bar;
				if (bar.BarIndex > series.MostRecentPublishedBarIndex)
					series.MostRecentPublishedBarIndex = bar.BarIndex;
				series.HeartbeatUtc = DateTime.UtcNow;
			}
		}

		public static void PublishLiveSnapshot(xOrderFlowRegistryKey key, Guid publisherId, xLiveOrderFlowSnapshot snapshot)
		{
			if (snapshot == null)
				return;

			lock (gate)
			{
				xOrderFlowPublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.PublisherId != publisherId)
					return;

				series.LiveSnapshot = snapshot;
				series.HeartbeatUtc = DateTime.UtcNow;
			}
		}

		public static bool TryGetLiveSnapshot(xOrderFlowRegistryKey key, out xLiveOrderFlowSnapshot snapshot)
		{
			snapshot = null;
			xOrderFlowPublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.LiveSnapshot == null)
				return false;
			snapshot = series.LiveSnapshot;
			return true;
		}

		public static bool TryGetBar(xOrderFlowRegistryKey key, int absoluteBarIndex, out xOrderFlowBar bar)
		{
			bar = null;
			xOrderFlowPublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous)
				return false;
			return series.Bars.TryGetValue(absoluteBarIndex, out bar);
		}

		public static int GetMostRecentPublishedIndex(xOrderFlowRegistryKey key)
		{
			xOrderFlowPublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous)
				return -1;
			return series.MostRecentPublishedBarIndex;
		}

		public static bool TryGetPublishedIndexRange(xOrderFlowRegistryKey key, out int oldestIndex, out int newestIndex)
		{
			oldestIndex = -1;
			newestIndex = -1;
			xOrderFlowPublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.Bars.IsEmpty)
				return false;

			foreach (int index in series.Bars.Keys)
			{
				if (oldestIndex < 0 || index < oldestIndex)
					oldestIndex = index;
				if (newestIndex < 0 || index > newestIndex)
					newestIndex = index;
			}
			return oldestIndex >= 0 && newestIndex >= 0;
		}

		public static bool HasActivePublisher(xOrderFlowRegistryKey key)
		{
			xOrderFlowPublishedSeries series;
			return seriesByKey.TryGetValue(key, out series) && !series.Ambiguous && series.PublisherId != Guid.Empty;
		}

		public static bool IsAmbiguous(xOrderFlowRegistryKey key)
		{
			xOrderFlowPublishedSeries series;
			return seriesByKey.TryGetValue(key, out series) && series.Ambiguous;
		}

		public static void UnregisterPublisher(xOrderFlowRegistryKey key, Guid publisherId)
		{
			lock (gate)
			{
				xOrderFlowPublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series) || series.PublisherId != publisherId)
					return;

				xOrderFlowPublishedSeries removed;
				seriesByKey.TryRemove(key, out removed);
			}
		}
	}
}
