namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaContainerComparer
    {
        public static ContainerComparison Compare(
            ManualContainerSnapshot manualSnapshot,
            ContainerGeometrySnapshot autoSnapshot)
        {
            double autoSlope = 0.0;
            if (autoSnapshot.Rtl.HasValue)
                autoSlope = autoSnapshot.Rtl.Value.Slope;

            return new ContainerComparison(
                manualSnapshot.ContainerId,
                autoSnapshot.ContainerId,
                manualSnapshot.P1.BarIndex,
                autoSnapshot.P1.HasValue ? autoSnapshot.P1.Value.BarIndex : -1,
                manualSnapshot.P2.BarIndex,
                autoSnapshot.P2.HasValue ? autoSnapshot.P2.Value.BarIndex : -1,
                manualSnapshot.P3.BarIndex,
                autoSnapshot.P3.HasValue ? autoSnapshot.P3.Value.BarIndex : -1,
                manualSnapshot.RtlSlope,
                autoSlope);
        }
    }
}