#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder
{
	public static class xPvaDrawingRegistry
	{
		public static bool TryGetActiveDrawings(string instrument, DateTime timestampUtc, out IReadOnlyList<xPvaDrawingSnapshot> drawings)
		{
			return xPvaDrawingRecorderService.TryGetActiveDrawingsShared(instrument, timestampUtc, out drawings);
		}

		public static bool TryGetDrawingHistory(string drawingId, out IReadOnlyList<xPvaDrawingEventRecord> events)
		{
			return xPvaDrawingRecorderService.TryGetDrawingHistoryShared(drawingId, out events);
		}

		public static bool TryGetChartDrawingState(string chartIdentity, out xPvaChartDrawingState state)
		{
			return xPvaDrawingRecorderService.TryGetChartDrawingStateShared(chartIdentity, out state);
		}
	}
}
