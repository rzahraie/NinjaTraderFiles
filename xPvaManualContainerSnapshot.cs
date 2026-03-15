namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ManualContainerPoint
    {
        public readonly int BarIndex;
        public readonly double Price;

        public ManualContainerPoint(int barIndex, double price)
        {
            BarIndex = barIndex;
            Price = price;
        }
    }

    public readonly struct ManualContainerSnapshot
    {
        public readonly int ContainerId;
        public readonly bool IsUpContainer;

        public readonly ManualContainerPoint P1;
        public readonly ManualContainerPoint P2;
        public readonly ManualContainerPoint P3;

        public readonly double RtlSlope;
        public readonly double LtlSlope;
		
		public readonly ManualContainerBreakMode BreakMode;
		public readonly double BreakToleranceTicks;

        public ManualContainerSnapshot(
		    int containerId,
		    bool isUpContainer,
		    ManualContainerPoint p1,
		    ManualContainerPoint p2,
		    ManualContainerPoint p3,
		    double rtlSlope,
		    double ltlSlope,
		    ManualContainerBreakMode breakMode,
		    double breakToleranceTicks)
		{
		    ContainerId = containerId;
		    IsUpContainer = isUpContainer;
		    P1 = p1;
		    P2 = p2;
		    P3 = p3;
		    RtlSlope = rtlSlope;
		    LtlSlope = ltlSlope;
		    BreakMode = breakMode;
		    BreakToleranceTicks = breakToleranceTicks;
		}
    }
}
