namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualTradeIntentResolver
    {
        public static TradeIntentEvent Resolve(
            ContainerGeometrySnapshot container,
            StructureEvent structure,
            ActionEvent action,
            int barIndex)
        {
            return new TradeIntentEvent(
                barIndex,
                container.ContainerId,
                TradeIntent.Sideline,
                action.Action,
                structure.State,
                action.TrendType,
                action.TurnType);
        }
    }
}
