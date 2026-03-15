namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualActionResolver
    {
        public static ActionEvent Resolve(
            ContainerGeometrySnapshot container,
            StructureEvent structure,
            int barIndex)
        {
            return new ActionEvent(
                barIndex,
                container.ContainerId,
                ActionType.Sideline,
                structure.TrendType,
                TurnType.Unknown,
                EndEffectKind.Unknown,
                Band.Unknown,
                0L);
        }
    }
}
