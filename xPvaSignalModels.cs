#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    public enum xPvaSignalType
    {
        None,
        LongEntry,
        ShortEntry
    }

    public sealed class xPvaSignal
    {
        public xPvaSignalType Type { get; set; } = xPvaSignalType.None;
        public int Score { get; set; } = 0; // 0..100
        public double StopPrice { get; set; } = double.NaN;
        public double TargetPrice { get; set; } = double.NaN;
        public List<string> Reasons { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"{Type} score={Score} stop={StopPrice} target={TargetPrice} [{string.Join(",", Reasons)}]";
        }
    }
}
