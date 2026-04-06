namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ManualExecutionSnapshot
    {
        public readonly int ContainerId;
        public readonly string Action;
        public readonly string Reason;
        public readonly string Signal;

        public readonly int? ConfirmedBar;
        public readonly double? EntryPrice;
        public readonly double? StopPrice;
        public readonly double? TargetPrice;

        public ManualExecutionSnapshot(
            int containerId,
            string action,
            string reason,
            string signal,
            int? confirmedBar,
            double? entryPrice,
            double? stopPrice,
            double? targetPrice)
        {
            ContainerId = containerId;
            Action = action;
            Reason = reason;
            Signal = signal;
            ConfirmedBar = confirmedBar;
            EntryPrice = entryPrice;
            StopPrice = stopPrice;
            TargetPrice = targetPrice;
        }
    }
}