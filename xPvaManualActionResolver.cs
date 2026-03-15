namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualStructureResolver
    {
        public static StructureEvent Resolve(
            ContainerGeometrySnapshot container,
            int barIndex)
        {
            return new StructureEvent(
                barIndex,
                container.ContainerId,
                StructureState.Broken,
                TrendType.Unknown,
                container.Direction);
        }
    }
}
