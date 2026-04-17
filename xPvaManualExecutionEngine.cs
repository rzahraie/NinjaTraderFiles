namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualExecutionEngine
    {
        public static ManualExecutionDecision Evaluate(
            ManualSignalDecision signalDecision,
            ManualContainerAnalysis analysis,
            ManualPositionState position,
            double? entryPrice,
            double? stopPrice,
            double? targetPrice)
        {
            bool wantsLong = signalDecision.Signal == "LONG";
            bool wantsShort = signalDecision.Signal == "SHORT";
            bool wantsTrade = wantsLong || wantsShort;
			
			if (signalDecision.Signal == "SKIP")
			{
			    if (position.Side == ManualPositionSide.Long &&
			        (signalDecision.Phase == ManualSignalTransition.Degrading ||
			         signalDecision.Phase == ManualSignalTransition.Invalidated))
			    {
			        position.Side = ManualPositionSide.Flat;
			        position.LastAction = "EXIT_LONG";
			        return new ManualExecutionDecision("EXIT_LONG", signalDecision.Phase.ToString());
			    }
			
			    if (position.Side == ManualPositionSide.Short &&
			        (signalDecision.Phase == ManualSignalTransition.Degrading ||
			         signalDecision.Phase == ManualSignalTransition.Invalidated))
			    {
			        position.Side = ManualPositionSide.Flat;
			        position.LastAction = "EXIT_SHORT";
			        return new ManualExecutionDecision("EXIT_SHORT", signalDecision.Phase.ToString());
			    }
			}

            if (position.Side == ManualPositionSide.Flat)
            {
                if (!wantsTrade)
                    return new ManualExecutionDecision("HOLD", "FlatNoSignal");

                if (wantsLong)
                {
                    position.Side = ManualPositionSide.Long;
                    position.EntryBar = analysis.FttConfirmedBar;
                    position.EntryPrice = entryPrice;
                    position.StopPrice = stopPrice;
                    position.TargetPrice = targetPrice;
                    position.LastContainerId = analysis.Snapshot.ContainerId;
                    position.LastAction = "ENTER_LONG";

                    return new ManualExecutionDecision("ENTER_LONG", "FreshLongSignal");
                }

                if (wantsShort)
                {
                    position.Side = ManualPositionSide.Short;
                    position.EntryBar = analysis.FttConfirmedBar;
                    position.EntryPrice = entryPrice;
                    position.StopPrice = stopPrice;
                    position.TargetPrice = targetPrice;
                    position.LastContainerId = analysis.Snapshot.ContainerId;
                    position.LastAction = "ENTER_SHORT";

                    return new ManualExecutionDecision("ENTER_SHORT", "FreshShortSignal");
                }
            }

            if (position.Side == ManualPositionSide.Long)
            {
                if (signalDecision.Phase == ManualSignalTransition.Degrading ||
                    signalDecision.Phase == ManualSignalTransition.Invalidated)
                {
                    position.Side = ManualPositionSide.Flat;
                    position.LastAction = "EXIT_LONG";
                    return new ManualExecutionDecision("EXIT_LONG", signalDecision.Phase.ToString());
                }

                if (wantsShort)
                {
                    position.Side = ManualPositionSide.Short;
                    position.EntryBar = analysis.FttConfirmedBar;
                    position.EntryPrice = entryPrice;
                    position.StopPrice = stopPrice;
                    position.TargetPrice = targetPrice;
                    position.LastContainerId = analysis.Snapshot.ContainerId;
                    position.LastAction = "REVERSE_TO_SHORT";

                    return new ManualExecutionDecision("REVERSE_TO_SHORT", "OppositeSignal");
                }

                return new ManualExecutionDecision("HOLD_LONG", "StillValid");
            }

            if (position.Side == ManualPositionSide.Short)
            {
                if (signalDecision.Phase == ManualSignalTransition.Degrading ||
                    signalDecision.Phase == ManualSignalTransition.Invalidated)
                {
                    position.Side = ManualPositionSide.Flat;
                    position.LastAction = "EXIT_SHORT";
                    return new ManualExecutionDecision("EXIT_SHORT", signalDecision.Phase.ToString());
                }

                if (wantsLong)
                {
                    position.Side = ManualPositionSide.Long;
                    position.EntryBar = analysis.FttConfirmedBar;
                    position.EntryPrice = entryPrice;
                    position.StopPrice = stopPrice;
                    position.TargetPrice = targetPrice;
                    position.LastContainerId = analysis.Snapshot.ContainerId;
                    position.LastAction = "REVERSE_TO_LONG";

                    return new ManualExecutionDecision("REVERSE_TO_LONG", "OppositeSignal");
                }

                return new ManualExecutionDecision("HOLD_SHORT", "StillValid");
            }

            return new ManualExecutionDecision("HOLD", "NoRuleMatched");
        }
    }
}
