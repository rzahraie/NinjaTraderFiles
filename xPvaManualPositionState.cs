namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualPositionSide
    {
        Flat = 0,
        Long = 1,
        Short = 2
    }

    public sealed class ManualPositionState
    {
        public ManualPositionSide Side = ManualPositionSide.Flat;

        public int? EntryBar = null;
        public double? EntryPrice = null;

        public double? StopPrice = null;
        public double? TargetPrice = null;

        public int LastContainerId = -1;
        public string LastAction = null;
    }
}