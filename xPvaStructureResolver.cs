namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaStructureResolver
    {
        public sealed class State
        {
            public StructureState LastState = StructureState.Unknown;
            public int LastBarIndex = -1;
            public ContainerDirection LastDirection = ContainerDirection.Unknown;
        }

        public static StructureEvent? Step(
            State s,
            in FttConfirmedEvent ftt,
            in TrendTypeEvent trend,
            in ActionEvent action)
        {
            StructureState state = Classify(ftt, trend, action);

            if (state == StructureState.Unknown)
                return null;

            ContainerDirection direction = Opposite(ftt.PriorDirection);

            if (s.LastBarIndex == ftt.BarIndex &&
                s.LastState == state &&
                s.LastDirection == direction)
            {
                return null;
            }

            s.LastBarIndex = ftt.BarIndex;
            s.LastState = state;
            s.LastDirection = direction;

            return new StructureEvent(
                ftt.BarIndex,
                state,
                trend.Type,
                action.Action,
                direction);
        }

        private static StructureState Classify(
            in FttConfirmedEvent ftt,
            in TrendTypeEvent trend,
            in ActionEvent action)
        {
            if (action.Action == ActionType.Reverse)
                return StructureState.Transition;

            if (trend.Type == TrendType.A && action.Action == ActionType.Enter)
                return StructureState.Building;

            if (trend.Type == TrendType.B &&
                (action.Action == ActionType.StayIn || action.Action == ActionType.Hold))
                return StructureState.Mature;

            if (trend.Type == TrendType.C || trend.Type == TrendType.D ||
                action.Action == ActionType.Sideline)
                return StructureState.Broken;

            return StructureState.Unknown;
        }

        private static ContainerDirection Opposite(ContainerDirection dir)
        {
            if (dir == ContainerDirection.Up)
                return ContainerDirection.Down;

            if (dir == ContainerDirection.Down)
                return ContainerDirection.Up;

            return ContainerDirection.Unknown;
        }
    }
}