namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualActionResolver
    {
        public static ActionEvent Resolve(
            ContainerGeometrySnapshot container,
            StructureEvent structure,
            int barIndex)
        {
            ActionType action =
                structure.State == StructureState.Transition
                    ? ActionType.Enter
                    : structure.State == StructureState.Broken
                        ? ActionType.Sideline
                        : ActionType.Hold;

            return new ActionEvent(
                barIndex,
                container.ContainerId,
                action,
                structure.TrendType,
                TurnType.Unknown,
                EndEffectKind.Unknown,
                Band.Unknown,
                0L);
        }
    }
}
