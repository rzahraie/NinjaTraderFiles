namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum TradeIntent
    {
        Unknown = 0,
        Enter = 1,
        Reverse = 2,
        Sideline = 3,
        HoldThru = 4,
        EarlyExit = 5,
        ReEntry = 6,
    }

    public readonly struct TradeIntentEvent
    {
        public readonly int BarIndex;
        public readonly int ContainerId;
        public readonly TradeIntent Intent;
        public readonly ActionType ActionType;
        public readonly StructureState StructureState;
        public readonly TrendType TrendType;
        public readonly TurnType TurnType;

        public TradeIntentEvent(
            int barIndex,
            int containerId,
            TradeIntent intent,
            ActionType actionType,
            StructureState structureState,
            TrendType trendType,
            TurnType turnType)
        {
            BarIndex = barIndex;
            ContainerId = containerId;
            Intent = intent;
            ActionType = actionType;
            StructureState = structureState;
            TrendType = trendType;
            TurnType = turnType;
        }
    }

    public sealed class xPvaTradeIntent
    {
        public sealed class State
        {
            public TradeIntent LastIntent = TradeIntent.Unknown;
            public ActionType LastAction = ActionType.Unknown;
            public StructureState LastStructure = StructureState.Unknown;
            public ContainerDirection LastDirection = ContainerDirection.Unknown;
            public int LastContainerId = -1;
            public int LastBarIndex = -1;

            public bool WasRecentlySidelined = false;
        }

        public static TradeIntentEvent? Step(
            State s,
            in StructureEvent structure,
            in ActionEvent action,
            in TurnEvent turn)
        {
            TradeIntent intent = Resolve(s, structure, action, turn);
            if (intent == TradeIntent.Unknown)
                return null;

            if (s.LastBarIndex == action.BarIndex &&
                s.LastContainerId == action.ContainerId &&
                s.LastIntent == intent)
            {
                return null;
            }

            s.LastIntent = intent;
            s.LastAction = action.Action;
            s.LastStructure = structure.State;
            s.LastDirection = structure.Direction;
            s.LastContainerId = action.ContainerId;
            s.LastBarIndex = action.BarIndex;

            s.WasRecentlySidelined = intent == TradeIntent.Sideline;

            return new TradeIntentEvent(
                action.BarIndex,
                action.ContainerId,
                intent,
                action.Action,
                structure.State,
                action.TrendType,
                action.TurnType);
        }

        private static TradeIntent Resolve(
            State s,
            in StructureEvent structure,
            in ActionEvent action,
            in TurnEvent turn)
        {
            if (action.Action == ActionType.Enter && structure.State == StructureState.Transition)
                return s.WasRecentlySidelined ? TradeIntent.ReEntry : TradeIntent.Enter;

            if (action.Action == ActionType.Reverse && structure.State == StructureState.Transition)
                return TradeIntent.Reverse;

            if (action.Action == ActionType.Sideline && structure.State == StructureState.Broken)
                return TradeIntent.Sideline;

            if (action.Action == ActionType.Hold && action.TrendType == TrendType.B)
                return TradeIntent.HoldThru;

            if (action.Action == ActionType.StayIn && s.WasRecentlySidelined)
                return TradeIntent.ReEntry;

            if (action.Action == ActionType.Sideline &&
                structure.State != StructureState.Broken &&
                (s.LastIntent == TradeIntent.Enter ||
                 s.LastIntent == TradeIntent.Reverse ||
                 s.LastIntent == TradeIntent.ReEntry))
            {
                return TradeIntent.EarlyExit;
            }

            if (action.Action == ActionType.Enter)
                return TradeIntent.Enter;

            if (action.Action == ActionType.Reverse)
                return TradeIntent.Reverse;

            if (action.Action == ActionType.Sideline)
                return TradeIntent.Sideline;

            if (action.Action == ActionType.Hold)
                return TradeIntent.HoldThru;

            if (action.Action == ActionType.StayIn)
                return TradeIntent.ReEntry;

            return TradeIntent.Unknown;
        }
    }
}