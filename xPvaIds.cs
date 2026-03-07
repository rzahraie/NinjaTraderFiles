#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    internal static class xPvaIds
    {
        public const string SchemaVersion = "1.0";

        public static string NewId() => Guid.NewGuid().ToString("D");
    }
}
