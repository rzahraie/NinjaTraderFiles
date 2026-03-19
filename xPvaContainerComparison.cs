namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ContainerComparison
    {
        public readonly int ManualContainerId;
        public readonly int AutoContainerId;

        public readonly int ManualP1Bar;
        public readonly int AutoP1Bar;
        public readonly int ManualP2Bar;
        public readonly int AutoP2Bar;
        public readonly int ManualP3Bar;
        public readonly int AutoP3Bar;

        public readonly double ManualRtlSlope;
        public readonly double AutoRtlSlope;

        public readonly int P1BarError;
        public readonly int P2BarError;
        public readonly int P3BarError;
        public readonly double RtlSlopeError;

        public ContainerComparison(
            int manualContainerId,
            int autoContainerId,
            int manualP1Bar,
            int autoP1Bar,
            int manualP2Bar,
            int autoP2Bar,
            int manualP3Bar,
            int autoP3Bar,
            double manualRtlSlope,
            double autoRtlSlope)
        {
            ManualContainerId = manualContainerId;
            AutoContainerId = autoContainerId;
            ManualP1Bar = manualP1Bar;
            AutoP1Bar = autoP1Bar;
            ManualP2Bar = manualP2Bar;
            AutoP2Bar = autoP2Bar;
            ManualP3Bar = manualP3Bar;
            AutoP3Bar = autoP3Bar;
            ManualRtlSlope = manualRtlSlope;
            AutoRtlSlope = autoRtlSlope;

            P1BarError = autoP1Bar - manualP1Bar;
            P2BarError = autoP2Bar - manualP2Bar;
            P3BarError = autoP3Bar - manualP3Bar;
            RtlSlopeError = autoRtlSlope - manualRtlSlope;
        }
    }
}