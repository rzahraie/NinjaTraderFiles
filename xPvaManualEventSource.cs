namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualEventSource
    {
        public static EngineEvent[] BuildHistoricalEvents(
            ContainerGeometrySnapshot container,
            ManualContainerSnapshot manualSnapshot,
            int currentBarIndex,
            System.Func<int, double> getHigh,
            System.Func<int, double> getLow,
            System.Func<int, double> getClose,
            double tickSize)
        {
            if (!container.Ltl.HasValue || !container.P3.HasValue)
                return new EngineEvent[0];

            for (int idx = container.P3.Value.BarIndex + 1; idx <= currentBarIndex; idx++)
            {
                double ltlNow = container.Ltl.Value.ValueAt(idx);
                double high = getHigh(idx);
                double low = getLow(idx);
                double close = getClose(idx);
                double tolerance = manualSnapshot.BreakToleranceTicks * tickSize;

                bool broke;

                if (manualSnapshot.BreakMode == ManualContainerBreakMode.CloseCross)
                {
                    broke =
                        container.Direction == ContainerDirection.Up
                            ? close < (ltlNow - tolerance)
                            : close > (ltlNow + tolerance);
                }
                else
                {
                    broke =
                        container.Direction == ContainerDirection.Up
                            ? low < (ltlNow - tolerance)
                            : high > (ltlNow + tolerance);
                }

                if (!broke)
                    continue;

                var candidate = new FttCandidateEvent(
                    idx,
                    container.ContainerId,
                    container.Direction,
                    PriceCase.Unknown,
                    0);

                int confirmIdx = idx + 1;
                if (confirmIdx > currentBarIndex)
                    return new[] { EngineEvent.From(candidate) };

                double confirmLtl = container.Ltl.Value.ValueAt(confirmIdx);
                double confirmHigh = getHigh(confirmIdx);
                double confirmLow = getLow(confirmIdx);
                double confirmClose = getClose(confirmIdx);
                double confirmTolerance = manualSnapshot.BreakToleranceTicks * tickSize;

                bool confirmBroke;

                if (manualSnapshot.BreakMode == ManualContainerBreakMode.CloseCross)
                {
                    confirmBroke =
                        container.Direction == ContainerDirection.Up
                            ? confirmClose < (confirmLtl - confirmTolerance)
                            : confirmClose > (confirmLtl + confirmTolerance);
                }
                else
                {
                    confirmBroke =
                        container.Direction == ContainerDirection.Up
                            ? confirmLow < (confirmLtl - confirmTolerance)
                            : confirmHigh > (confirmLtl + confirmTolerance);
                }

                if (!confirmBroke)
                    return new[] { EngineEvent.From(candidate) };

                var confirmed = new FttConfirmedEvent(
                    confirmIdx,
                    container.ContainerId,
                    container.Direction,
                    PriceCase.Unknown,
                    0);

                var structure = xPvaManualStructureResolver.Resolve(container, confirmIdx);
                var action = xPvaManualActionResolver.Resolve(container, structure, confirmIdx);
                var tradeIntent = xPvaManualTradeIntentResolver.Resolve(container, structure, action, confirmIdx);

                return new[]
                {
                    EngineEvent.From(candidate),
                    EngineEvent.From(confirmed),
                    EngineEvent.From(structure),
                    EngineEvent.From(action),
                    EngineEvent.From(tradeIntent)
                };
            }

            return new EngineEvent[0];
        }
    }
}