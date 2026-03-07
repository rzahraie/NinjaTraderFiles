#region Using declarations
using System;
using NinjaTrader.Cbi;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    internal static class xPvaSession
    {
        public static DateTime ToUtc(DateTime barTimeLocal)
        {
            // NT bar times are usually in the chart's time zone.
            // We store in UTC to avoid session ambiguity.
            // If your chart time zone is already exchange time, this still normalizes.
            return barTimeLocal.ToUniversalTime();
        }

        public static string SafeInstrumentName(Instrument instrument)
        {
            return instrument != null ? instrument.FullName : "Unknown";
        }

        public static string SafeMasterInstrumentName(Instrument instrument)
        {
            return instrument != null && instrument.MasterInstrument != null
                ? instrument.MasterInstrument.Name
                : "Unknown";
        }
    }
}