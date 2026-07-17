#region Using declarations
using System;
using System.Collections.Concurrent;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaOrderFlow
{
	public readonly struct xOrderFlowFeatureRegistryKey : IEquatable<xOrderFlowFeatureRegistryKey>
	{
		public string InstrumentFullName { get; }
		public BarsPeriodType BarsPeriodType { get; }
		public int BarsPeriodValue { get; }
		public int BarsPeriodValue2 { get; }
		public string TradingHoursName { get; }
		public string UserInstanceKey { get; }

		public xOrderFlowFeatureRegistryKey(string instrumentFullName, BarsPeriodType barsPeriodType, int barsPeriodValue, int barsPeriodValue2, string tradingHoursName, string userInstanceKey)
		{
			InstrumentFullName = instrumentFullName ?? string.Empty;
			BarsPeriodType = barsPeriodType;
			BarsPeriodValue = barsPeriodValue;
			BarsPeriodValue2 = barsPeriodValue2;
			TradingHoursName = tradingHoursName ?? string.Empty;
			UserInstanceKey = userInstanceKey ?? string.Empty;
		}

		public bool Equals(xOrderFlowFeatureRegistryKey other)
		{
			return string.Equals(InstrumentFullName, other.InstrumentFullName, StringComparison.Ordinal)
				&& BarsPeriodType == other.BarsPeriodType
				&& BarsPeriodValue == other.BarsPeriodValue
				&& BarsPeriodValue2 == other.BarsPeriodValue2
				&& string.Equals(TradingHoursName, other.TradingHoursName, StringComparison.Ordinal)
				&& string.Equals(UserInstanceKey, other.UserInstanceKey, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is xOrderFlowFeatureRegistryKey && Equals((xOrderFlowFeatureRegistryKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + HashString(InstrumentFullName);
				hash = hash * 31 + BarsPeriodType.GetHashCode();
				hash = hash * 31 + BarsPeriodValue.GetHashCode();
				hash = hash * 31 + BarsPeriodValue2.GetHashCode();
				hash = hash * 31 + HashString(TradingHoursName);
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
			return string.Format("{0}|{1}:{2}:{3}|TH={4}|Key={5}", InstrumentFullName, BarsPeriodType, BarsPeriodValue, BarsPeriodValue2, TradingHoursName, UserInstanceKey);
		}
	}

	public enum xOrderFlowFeaturePublisherRegistrationResult
	{
		Registered,
		Refreshed,
		DuplicatePublisher,
		Ambiguous
	}

	internal sealed class xOrderFlowFeaturePublishedSeries
	{
		public readonly ConcurrentDictionary<int, xOrderFlowBarFeatures> Features = new ConcurrentDictionary<int, xOrderFlowBarFeatures>();
		public Guid PublisherId;
		public DateTime HeartbeatUtc;
		public int MostRecentPublishedBarIndex = -1;
		public bool Ambiguous;
	}

	public static class xPvaOrderFlowFeatureRegistry
	{
		private static readonly ConcurrentDictionary<xOrderFlowFeatureRegistryKey, xOrderFlowFeaturePublishedSeries> seriesByKey = new ConcurrentDictionary<xOrderFlowFeatureRegistryKey, xOrderFlowFeaturePublishedSeries>();
		private static readonly object gate = new object();

		public static xOrderFlowFeaturePublisherRegistrationResult RegisterPublisher(xOrderFlowFeatureRegistryKey key, Guid publisherId)
		{
			lock (gate)
			{
				xOrderFlowFeaturePublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series))
				{
					series = new xOrderFlowFeaturePublishedSeries { PublisherId = publisherId, HeartbeatUtc = DateTime.UtcNow };
					seriesByKey[key] = series;
					return xOrderFlowFeaturePublisherRegistrationResult.Registered;
				}

				if (series.PublisherId == publisherId)
				{
					series.HeartbeatUtc = DateTime.UtcNow;
					return series.Ambiguous ? xOrderFlowFeaturePublisherRegistrationResult.Ambiguous : xOrderFlowFeaturePublisherRegistrationResult.Refreshed;
				}

				series.Ambiguous = true;
				series.HeartbeatUtc = DateTime.UtcNow;
				return xOrderFlowFeaturePublisherRegistrationResult.DuplicatePublisher;
			}
		}

		public static void Publish(xOrderFlowFeatureRegistryKey key, Guid publisherId, xOrderFlowBarFeatures features)
		{
			if (features == null)
				return;

			lock (gate)
			{
				xOrderFlowFeaturePublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.PublisherId != publisherId)
					return;

				series.Features[features.BarIndex] = features;
				if (features.BarIndex > series.MostRecentPublishedBarIndex)
					series.MostRecentPublishedBarIndex = features.BarIndex;
				series.HeartbeatUtc = DateTime.UtcNow;
			}
		}

		public static bool TryGetFeatures(xOrderFlowFeatureRegistryKey key, int absoluteBarIndex, out xOrderFlowBarFeatures features)
		{
			features = null;
			xOrderFlowFeaturePublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous)
				return false;
			return series.Features.TryGetValue(absoluteBarIndex, out features);
		}

		public static int GetMostRecentPublishedIndex(xOrderFlowFeatureRegistryKey key)
		{
			xOrderFlowFeaturePublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous)
				return -1;
			return series.MostRecentPublishedBarIndex;
		}

		public static bool TryGetPublishedIndexRange(xOrderFlowFeatureRegistryKey key, out int oldestIndex, out int newestIndex)
		{
			oldestIndex = -1;
			newestIndex = -1;
			xOrderFlowFeaturePublishedSeries series;
			if (!seriesByKey.TryGetValue(key, out series) || series.Ambiguous || series.Features.IsEmpty)
				return false;

			foreach (int index in series.Features.Keys)
			{
				if (oldestIndex < 0 || index < oldestIndex)
					oldestIndex = index;
				if (newestIndex < 0 || index > newestIndex)
					newestIndex = index;
			}
			return oldestIndex >= 0 && newestIndex >= 0;
		}

		public static bool HasActivePublisher(xOrderFlowFeatureRegistryKey key)
		{
			xOrderFlowFeaturePublishedSeries series;
			return seriesByKey.TryGetValue(key, out series) && !series.Ambiguous && series.PublisherId != Guid.Empty;
		}

		public static bool IsAmbiguous(xOrderFlowFeatureRegistryKey key)
		{
			xOrderFlowFeaturePublishedSeries series;
			return seriesByKey.TryGetValue(key, out series) && series.Ambiguous;
		}

		public static void UnregisterPublisher(xOrderFlowFeatureRegistryKey key, Guid publisherId)
		{
			lock (gate)
			{
				xOrderFlowFeaturePublishedSeries series;
				if (!seriesByKey.TryGetValue(key, out series) || series.PublisherId != publisherId)
					return;

				xOrderFlowFeaturePublishedSeries removed;
				seriesByKey.TryRemove(key, out removed);
			}
		}
	}
}
