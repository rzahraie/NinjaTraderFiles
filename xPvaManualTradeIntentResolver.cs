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
            TradeIntent intent =
                action.Action == ActionType.Enter ? TradeIntent.Enter :
                action.Action == ActionType.Reverse ? TradeIntent.Reverse :
                action.Action == ActionType.Sideline ? TradeIntent.Sideline :
                action.Action == ActionType.Hold ? TradeIntent.HoldThru :
                action.Action == ActionType.StayIn ? TradeIntent.ReEntry :
                TradeIntent.Unknown;

            return new TradeIntentEvent(
                barIndex,
                container.ContainerId,
                intent,
                action.Action,
                structure.State,
                action.TrendType,
                action.TurnType);
        }
    }
}
